# !/usr/bin/python
# -*- coding: UTF-8 -*-

import os
import sys
import json
import re
try:
    import xml.etree.cElementTree as ElementTree
except ImportError:
    import xml.etree.ElementTree as ElementTree

def getImport(src, target, module_names=None):
    """
    导入模块
        src: 当前文件
        target: 需要导入模块所在文件
        module_names: 需要导入的模块列表, 默认为None, 导入目标文件同名模块
    """

    src = os.path.abspath(src).replace("\\", "/")
    target = os.path.abspath(target).replace("\\", "/")
    src_list = src.split("/")
    target_list = target.split("/")

    index = 0
    for x in src_list:
        if x != target_list[index]:
            break

        index = index + 1

    lines = []
    for x in range(index, len(src_list)-1):
        lines.append("..")
    for x in range(index, len(target_list)):
        lines.append(target_list[x])

    target_path = "/".join(lines)
    if not target_path.startswith("."):
        target_path = "./" + target_path
    module_name = os.path.splitext(os.path.basename(target_path))[0]
    if not module_names is None:
        module_name = ", ".join(module_names)
    return "import { %s } from \"%s\";" % (module_name, target_path)

def load_json(path):
    """
    加载json数据
        path: json文件路径
    """
    if not os.path.isfile(path):
        return {}

    f = open(path, "r", encoding="utf8")
    json_map = json.load(f)
    f.close()
    return json_map

def write_file(f, lines, join=None):
    """
    写文件
        f: 保存文件路径
        lines: string[] 需要写入的内容
        join: 拼接字符串
    """
    if isinstance(lines, str):
        lines = [lines]
    parent = os.path.dirname(f)
    if not os.path.exists(parent):
        os.makedirs(parent)

    out_file = open(f, "w", encoding="utf8")
    if join is None:
        out_file.writelines(lines)
    else:
        out_file.write(str(join).join(lines))
    out_file.close()


def replace_line(path, text, start, end):
    lines = []
    f = open(path, "r", encoding="utf8")
    for line in f:
        start_i = line.find(start)
        end_i = line.find(end)
        if start_i >= 0:
            line = "%s%s%s" % (line[:start_i+len(start)], text, line[end_i:])
        lines.append(line)
    f.close()
    write_file(path, lines)

def firstUpper(name):
    if len(name) <= 1:
        return name.upper()
    return name[:1].upper() + name[1:]

class NamePinYin():
    """ 中文拼音转换 """
    def __init__(self, path):
        self.map = []
        if not os.path.exists(path):
            return

        f = open(path, "r", encoding="utf8")
        for x in f:
            lines = x.split("|")
            for i in range(0, len(lines)/2):
                key = lines[2*i]
                value = lines[2*i+1]
                self.map.append((key, value))
        f.close()

    def convert(self, s):
        if re.match("\w+", s):
            return s

        for (k, v) in self.map:
            if k == s:
                return v

        return s

class Resource():
    """所有资源"""
    def __init__(self, el, pkg, classNamePrefix):
        self.pkg = pkg
        self.tag = el.tag
        self.res_id = el.get("id")
        self.id = "%s%s" % (pkg.id, self.res_id)
        self.path = ("%s%s"%(el.get("path"), el.get("name"))).strip("/")
        self.className = pkg.pinyin.convert(os.path.splitext(os.path.basename(self.path))[0])#"%s%s" % (classNamePrefix, pkg.pinyin.convert(os.path.splitext(os.path.basename(self.path))[0]))

    def isComponent(self):
        return self.tag == "component"

    def fullPath(self):
        root = self.pkg.root
        return os.path.join(root, self.pkg.pkg, self.path)

    def parseDepends(self, pkg_map, res_map, depends = []):
        if depends is None:
            depends = []

        pkg_url = self.pkg.url()
        if depends.count(pkg_url) == 0:
            depends.append(pkg_url)

        xml = self.fullPath()
        if not os.path.exists(xml):
            print("> not exists: %s" % xml)
            return depends

        if not self.isComponent():
            return depends

        xml_tree = ElementTree.parse(xml)
        xml_root = xml_tree.getroot()
        display = xml_root.find("displayList")
        if display is None:
            return depends

        for e in list(display):
            font = e.get("font")
            defaultItem = e.get("defaultItem")
            pkg_id = e.get("pkg") or self.pkg.id
            src_id = e.get("src")
            depend_id = None
            if not defaultItem is None:
                if defaultItem.startswith("ui://"):
                    depend_id = defaultItem[len("ui://"):]
            elif not font is None:
                if font.startswith("ui://"):
                    depend_id = font[len("ui://"):]
            elif not src_id is None:
                depend_id = "%s%s" % (pkg_id, src_id)

            if depend_id is None:
                if e.tag == "list":
                    list_items = e.findall("item")
                    if len(list_items) > 0:
                        for c in list_items:
                            item_url = c.get("url")
                            if item_url is None and len(item_url) == 0:
                                continue

                            if item_url.startswith("ui://"):
                                item_url = item_url[len("ui://"):]

                            item_depend_res = res_map.get(item_url)
                            if item_depend_res is None:
                                print("> res miss [%s] tag: %s, pkg: %s, src: %s, font: %s" % (os.path.join(self.pkg.pkg, self.path), c.tag, pkg_id, src_id, font))
                                continue

                            item_depend_res.parseDepends(pkg_map, res_map, depends)
                continue

            depend_res = res_map.get(depend_id)
            if depend_res is None:
                print("> res miss [%s] tag: %s, pkg: %s, src: %s, font: %s" % (os.path.join(self.pkg.pkg, self.path), e.tag, pkg_id, src_id, font))
                continue

            depend_res.parseDepends(pkg_map, res_map, depends)

        return depends

    def modifyDepends(self, code_path, pkg_map, res_map):
        depends = []
        self.parseDepends(pkg_map, res_map, depends)
        if depends is None:
            return

        cpath = os.path.join(code_path, "FuiCfg.cs")
        if not os.path.exists(cpath):
            return

        depends.sort()

        replace_line(cpath, "\"%s\""%"\",\"".join(depends), '{"fui.%s.%s", new []{' % (self.pkg.pkg_name, self.className), '}},')

class Package():
    def __init__(self, root, pkg_dir, ui_proj_root, res_out, bin_path, pinyin):
        self.pinyin = pinyin
        self.root = root
        self.pkg = pkg_dir
        self.res_name = pkg_dir
        self.pkg_name = self.pinyin.convert(pkg_dir)

        self.res_out = res_out
        self.proj_path = ui_proj_root
        self.bin_path = bin_path

    def url(self):
        ui_path = os.path.abspath(os.path.join(self.proj_path, self.res_out))
        pkg_prefix = ui_path[len(self.bin_path)+1:]
        if len(pkg_prefix) > 0 and not pkg_prefix.endswith("/"):
            pkg_prefix += "/"

        return "%s%s" % (pkg_prefix, self.res_name)

    def parseFromXml(self, res_map, classNamePrefix):
        xml = os.path.join(self.root, self.pkg, "package.xml")
        if not os.path.exists(xml):
            return False

        xml_tree = ElementTree.parse(xml)
        xml_root = xml_tree.getroot()

        self.id = xml_root.get("id")

        # 资源名
        el_publish = xml_root.find("publish")
        if el_publish is None:
            print("no publish: "+xml)
            return False
        res_name = el_publish.get("name")
        if not res_name is None and len(res_name) > 0:
            self.res_name = res_name
            self.pkg_name = self.pinyin.convert(res_name)

        res_out_path = el_publish.get("path")
        if not res_out_path is None and len(res_out_path) > 0:
            self.res_out = res_out_path

        # 解析资源
        self.components = []
        el_resources = xml_root.find("resources")
        for el_r in list(el_resources):
            res = Resource(el_r, self, classNamePrefix)
            if res.isComponent():
                self.components.append(res)

            res_map[res.id] = res

        return True

def parse_binders(binder_ts, ui_binders):
    binder_ts = os.path.abspath(binder_ts)
    if not os.path.exists(binder_ts):
        # print("不存在: %s" % binder_ts)
        return

    base_path = os.path.dirname(binder_ts)
    f = open(binder_ts, "r", encoding="utf8")
    for line in f:
        line = line.strip()
        if not line.startswith("import"):
            continue
        path = os.path.abspath(os.path.join(base_path, line[line.find("\""):].strip('";')))
        ui_binders.append(path)

    f.close()

def fix_fgui(UI_PROJ, BIN_PATH):
    print(UI_PROJ)
    if not os.path.isdir(UI_PROJ):
        print("not exists fgui proj: " + UI_PROJ)
        return
    pinyin = NamePinYin(os.path.join(UI_PROJ, ".objs/convertmap.md"))
    ui_publish_cfg = load_json(os.path.join(UI_PROJ, "settings/Publish.json"))

    ui_path = ui_publish_cfg.get("path")
    ui_branch_path = ui_publish_cfg.get("branchPath")
    if ui_branch_path is None or ui_branch_path == "" or ui_path == ui_branch_path:
        ui_branch_path = None
    else:
        ui_branch_path = os.path.abspath(os.path.join(UI_PROJ, ui_branch_path))

    ui_code_path = os.path.abspath(os.path.join(UI_PROJ, ui_publish_cfg.get("codeGeneration").get("codePath")))

    classNamePrefix = ui_publish_cfg.get("codeGeneration").get("classNamePrefix")
    fileExtension = ui_publish_cfg.get("fileExtension")

    assets_root = os.path.join(UI_PROJ, "assets")
    pkg_map = {}
    res_map = {}
    for x in os.listdir(assets_root):
        f = os.path.join(assets_root, x, "package.xml")
        if not os.path.exists(f):
            continue

        pkg = Package(assets_root, x, UI_PROJ, ui_path, BIN_PATH, pinyin)
        if pkg.parseFromXml(res_map, classNamePrefix):
            pkg_map[pkg.id] = pkg

    for (pkg_id, pkg) in list(pkg_map.items()):
        for comp in pkg.components:
            comp.modifyDepends(ui_code_path, pkg_map, res_map)

def build():
    cur_path = os.path.dirname(__file__)
    proj_path = os.path.abspath(os.path.join(cur_path, ".."))
    bin_path = os.path.join(proj_path, "Assets/Asset/fgui")

    fix_fgui(os.path.abspath(os.path.join(proj_path, "FguiProj")), bin_path)

# -------------- main --------------
if __name__ == '__main__':
    build()
