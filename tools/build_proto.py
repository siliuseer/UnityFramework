# -*- coding: utf-8 -*-
# @Date    : 2021-10-12 10:59:28
# @Author  : siliu

from ast import If
import os
import shutil
import subprocess
from functools import cmp_to_key
import operator

def run_cmd(command):
    ret = subprocess.call(command, shell=True)
    if ret != 0:
        message = "Error running command:[%s]\nreturn code: %s" % (command, str(ret))
        raise Exception(message)

def rmdir(src, excepts = None):
    src = os.path.normpath(src)
    if not os.path.exists(src):
        return True

    if not os.path.isdir(src):
        if excepts is None:
            os.remove(src)
            return True

        keep = False
        __src = src.replace("\\", "/")
        for x in excepts:
            x = x.replace("\\", "/")
            if __src.endswith(x):
                keep = True
                break
        if not keep:
            os.remove(src)
            return True
        else:
            return False

    rm_all = True
    for f in os.listdir(src):
        if not excepts is None and f in excepts:
            rm_all = False
            continue
        rm_all = rmdir(os.path.join(src, f), excepts) and rm_all

    if rm_all:
        shutil.rmtree(src)

    return rm_all


def check_dir(src, clean = False, excepts = None):
    src = os.path.normpath(src)
    if clean:
        rmdir(src, excepts)

    if not os.path.exists(src):
        os.makedirs(src)
        pass

def copy_files(src, dst, rename=None):
    src = os.path.abspath(src)
    dst = os.path.abspath(dst)

    check_dir(dst)

    if not os.path.isdir(src):
        if rename is None:
            rename = os.path.basename(src)
        shutil.copy(src, os.path.join(dst, rename))
        return

    for item in os.listdir(src):
        path = os.path.join(src, item)

        if item.startswith(".") or item.startswith("~"):
            continue

        if os.path.isdir(path):
            copy_files(path, os.path.join(dst, item))
        else:
            copy_files(path, dst, item)

def write_file(lua, lines, join=None):
    if isinstance(lines, str):
        lines = [lines]
    parent = os.path.dirname(lua)
    check_dir(parent, False)

    out_file = open(lua, "w", encoding="utf8")
    if join is None:
        out_file.writelines(lines)
    else:
        out_file.write(str(join).join(lines))
    out_file.close()

def getImport(src, model, module_names=None):
    src = src.replace("\\", "/")
    model = model.replace("\\", "/")
    src_list = src.split("/")
    model_list = model.split("/")

    index = 0
    for x in src_list:
        if x != model_list[index]:
            break

        index = index + 1

    lines = []
    for x in range(index, len(src_list)-1):
        lines.append("..")
    for x in range(index, len(model_list)):
        lines.append(model_list[x])

    model_path = "/".join(lines)
    if not model_path.startswith("."):
        model_path = "./" + model_path
    model_name = os.path.splitext(os.path.basename(model_path))[0]
    if not module_names is None:
        model_name = ", ".join(module_names)
    return "import { %s } from \"%s\";" % (model_name, model_path)

def listProtoJs(input_dir, excepts=[], root = None, pb_list = None):
    if root is None:
        root = input_dir
    if pb_list is None:
        pb_list = []

    input_dir = os.path.abspath(input_dir)
    root = os.path.abspath(root).replace('\\', '/')
    for x in os.listdir(input_dir):
        if x in excepts:
            continue
        path = os.path.abspath(os.path.join(input_dir, x)).replace('\\', '/')
        if os.path.isdir(path):
            listProtoJs(path, excepts, root, pb_list)
            continue
        if not x.endswith(".proto") or x.startswith("enum") or x.startswith("test") or x.startswith("http") or x.startswith("json"):
            continue
        pb_list.append(path)
    return pb_list

def pbjs(input_dir, project_dir, excepts=[]):
    pb_list = listProtoJs(input_dir, excepts)

    if len(pb_list) == 0:
        return

    args = ["static-module"]
    args.append("--no-verify")
    args.append("--no-convert")
    args.append("--no-beautify")
    args.append("--no-comments")
    args.append("--no-create")
    args.append("--no-delimited")
    args.append("--force-long")
    # args.append("--no-encode")
    # args.append("--no-decode")
    out_file = os.path.abspath(os.path.join(project_dir, "src/auto/net/pb.js"))
    check_dir(os.path.dirname(out_file))
    run_cmd("%s/node_modules/.bin/pbjs -w commonjs -t %s -o %s %s" % (project_dir, " ".join(args), out_file, " ".join(pb_list)))

    js_lines = []
    js_file = open(out_file, "r", encoding="utf8")
    for line in js_file:
        if line.find('require("protobufjs/minimal");') > 0:
            line = line.replace('require("protobufjs/minimal");', 'require("protobuf");')

        js_lines.append(line)
    js_file.close()

    write_file(out_file, js_lines)


def unpackMsgList(msg_list):
    for i in range(len(msg_list)-1, -1, -1):
        msg = msg_list[i]
        if "sub_msgs" in msg:
            sub_list = msg["sub_msgs"]
            unpackMsgList(sub_list)
            for j in range(len(sub_list), 0, -1):
                msg_list.insert(i, sub_list[j-1])
            del msg["sub_msgs"]


def getJsType(type_name):
    if type_name == "string":
        return "string", False
    if type_name in ("int32", "enum"):
        return "int", False
    if type_name == "float":
        return "float", False
    if type_name == "double":
        return "double", False
    if type_name == "int64":
        return "long", False
    if type_name == "bool":
        return "bool", False
    if type_name == "bytes":
        return "byte[]", False

    return type_name, True

def convertArgName(name):
    names = name.split("_")
    if len(names) <= 1:
        return name

    for i in range(1, len(names)):
        _name = names[i]
        _first = _name[:1]
        _upper_first = _first.upper()
        if _first == _upper_first:
            names[i] = "_"+_name
        else:
            names[i] = _upper_first + _name[1:]

    return "".join(names)


def convertProtoName(name):
    name = name.strip()
    names = name.strip().split("_")
    if len(names) <= 1:
        return name.capitalize()

    for i in range(len(names)-1, -1, -1):
        _name = names[i].strip()
        if len(_name) == 0:
            del names[i]
            continue
        names[i] = _name.capitalize()

    return "".join(names)

# parse proto file
def parseProto(proto, request_list, msg_dic, excepts=[]):
    if os.path.isdir(proto):
        for x in os.listdir(proto):
            if x in excepts:
                continue
            if x.startswith(".") or x.startswith("~"):
                continue
            parseProto(os.path.join(proto, x), request_list, msg_dic)
        return
    if not proto.endswith(".proto"):
        return

    import_list = []
    proto_dic = {}
    msg_list = []
    msg = None
    enum_list = []
    enum = None
    proto_file = open(proto, "r", encoding="utf8")
    is_proto3 = False
    for x in proto_file:
        _str = x.strip()

        if len(_str) == 0 or _str.startswith("option ") or _str.startswith("//"):
            continue

        # syntax
        if _str.startswith("syntax"):
            is_proto3 = _str[6:].strip(" ;=\"") == "proto3"
            continue

        # package
        if _str.startswith("package"):
            package_name = convertProtoName(_str[7:].strip(" ;"))
            continue

        # import
        if _str.startswith("import"):
            import_name = _str[6:].strip(" ;\"")
            import_list.append(import_name)
            continue

        # /*
        if _str.startswith("/*"):
            proto_dic = {}
            continue

        # */
        if _str.endswith("*/"):
            if "proto" not in proto_dic:
                continue

            proto_id = proto_dic["proto"]
            if not proto_id.isdigit():
                if "http" not in proto_dic:
                    raise Exception("proto id define error: %s" % proto_id)
                proto_dic["proto"] = '"%s"' % proto_id

            if "up" in proto_dic and len(proto_dic["up"]) > 0:
                proto_dic["name"] = proto_dic["up"]
            if "name" not in proto_dic:
                proto_dic["name"] = "Proto_" + proto_id

            proto_dic["package"] = package_name
            request_list.append(proto_dic)

            if "command" not in proto_dic and "down" not in proto_dic and "push" not in proto_dic:
                continue

            command = proto_dic["name"]
            if "down" in proto_dic and len(proto_dic["down"]) > 0:
                command = proto_dic["down"]
            if "push" in proto_dic and len(proto_dic["push"]) > 0:
                command = proto_dic["push"]
            if "command" in proto_dic and len(proto_dic["command"]) > 0:
                command = proto_dic["command"]

            if "module" in proto_dic and len(proto_dic["module"]) > 0:
                command = "net/%s/Proto_%s" % (proto_dic["module"], command)
            else:
                command = "net/Proto_%s" % (command)

            if not command is None:
                proto_dic["command"] = os.path.basename(command)
                proto_dic["command_path"] = command
            continue
        # *
        if _str.startswith("*"):
            _str = _str.strip("*")
            if len(_str) <= 1:
                continue

            is_desc = True
            for _x in ["@up", "@down", "@push", "@command", "@proto", "@module", "@gz", "@repeat", "@http", "@discard"]:
                pos = _str.find(_x)
                if pos < 0:
                    continue
                is_desc = False
                _str = _str[pos+len(_x):].strip()
                if _x == "@proto":
                    if len(_str) == 0:
                        continue

                    try:
                        index = _str.find("//")
                        if index > 0:
                            desc = _str[index+2:].strip()
                            if len(desc):
                                proto_dic["desc"] = desc
                            _str = _str[:index]

                        _str = _str.strip().strip(";").strip('"')
                        index = _str.find("=")
                        if index > 0:
                            res = _str.split("=")
                            proto_dic["name"] = convertProtoName(res[0].strip())
                            proto_dic["proto"] = res[1].strip()
                        else:
                            proto_dic["proto"] = _str
                    except Exception as e:
                        raise Exception("非法定义@proto: %s" % _str)
                elif _x in ["@up", "@down", "@push"]:
                    if len(_str) > 0:
                        proto_dic[_x[1:]] = _str
                else:
                    proto_dic[_x[1:]] = _str
            if is_desc and "desc" not in proto_dic:
                proto_dic["desc"] = _str[1:].strip()
            continue

        # message
        if _str.startswith("message"):
            _end_index = _str.find("{", -1)
            if _end_index < 0:
                _str = _str[7:].strip()
            else:
                _str = _str[7:_end_index].strip()

            _msg = {}
            _msg["name"] = _str
            if msg is None:
                msg = _msg
            else:
                _msg["_parent"] = msg
                msg = _msg
            _msg = None
            _enum_start = False
            continue

        if _str.startswith("enum"):
            _end_index = _str.find("{", -1)
            if _end_index < 0:
                _str = _str[4:].strip()
            else:
                _str = _str[4:_end_index].strip()

            enum = {}
            enum["name"] = _str

            _enum_start = True
            continue

        # {
        if _str == "{":
            continue

        # }
        if _str.endswith("}"):
            if _enum_start:
                _enum_start = False
                enum_list.append(enum)
                enum = None
                continue
            if "_parent" in msg:
                _msg = msg["_parent"]
                del msg["_parent"]
                if "sub_msgs" in _msg:
                    _msg["sub_msgs"].append(msg)
                else:
                    _msg["sub_msgs"] = [msg]
                msg = _msg
                _msg = None
            else:
                msg_list.append(msg)
                msg = None
            continue

        # 解析注释
        _desc_index = _str.find("//")
        if _desc_index >= 0:
            _arg_desc = _str[_desc_index+2:].strip()
            _str = _str[:_desc_index]

        # 解析枚举定义
        if _enum_start:
            _strs = _str.split("=")
            if len(_strs) != 2:
                raise Exception("枚举字段定义出错: " + _str)
            _enum_dic = {
                "name": _strs[0].strip(),
                "id": _strs[1].strip().strip(";")
            }
            if _desc_index >= 0:
                _enum_dic["desc"] = _arg_desc

            if "args" in enum:
                enum_arg_list = enum["args"]
            else:
                enum_arg_list = []
                enum["args"] = enum_arg_list
            enum_arg_list.append(_enum_dic)
            continue

        # 解析字段定义

        # 1. 解析默认值
        _arg_dic = {}
        if _str.find("[") >= 0:
            _l_i = _str.find("[")
            _r_i = _str.find("]")
            _default = _str[_l_i+1:_r_i]
            if _default.find("default") >= 0:
                _arg_dic["default"] = _default[_default.find("=")+1:].strip()
            _str = _str[:_l_i] + _str[_r_i+1:]

        _strs = _str.split("=")
        if len(_strs) != 2:
            raise Exception("字段定义出错: " + _str)

        if "args" in msg:
            arg_list = msg["args"]
        else:
            arg_list = []
            msg["args"] = arg_list

        # 2. 解析索引
        _id_str = _strs[1].strip().strip(";")
        _arg_dic["id"] = _id_str
        if _desc_index >= 0:
            _arg_dic["desc"] = _arg_desc

        # 3. 解析字段定义
        _args = _strs[0].strip().replace("\t", " ").split(" ")
        for i in range(len(_args)-1, -1, -1):
            _t = _args[i].strip()
            if len(_t) == 0:
                del _args[i]
            else:
                _args[i] = _t

        _arg_desc = _args[0]
        if is_proto3 and _arg_desc == "optional" or _arg_desc == "required":
            raise Exception("proto3 not support [%s] see at: %s" % (_arg_desc, _str))

        if not is_proto3 and _arg_desc not in ("optional", "required", "repeated"):
            raise Exception("proto2 must define optional | required | repeated, see at: %s" % _str)

        is_array = _arg_desc == "repeated"
        _arg_dic["optional"] = is_proto3 or is_array or _arg_desc == "optional"
        _arg_dic["array"] = is_array
        if is_array or not is_proto3:
            _arg_start = 1
        else:
            _arg_start = 0
        _arg_dic["type"] = _args[_arg_start]
        _js_type, is_custom_type = getJsType(_args[_arg_start])
        _arg_dic["jstype"] = _js_type
        if is_custom_type:
            _arg_dic["custom_type"] = True
        _arg_dic["name"] = convertArgName(_args[_arg_start+1])

        arg_list.append(_arg_dic)

    unpackMsgList(msg_list)

    msg_dic[os.path.basename(proto)] = {"package": package_name, "msgs": msg_list, "enums": enum_list}

    proto_file.close()
    return import_list

def cmpArg(x, y):
    y_optional = y["optional"] or "default" in y
    x_optional = x["optional"] or "default" in x
    if y_optional != x_optional:
        return y_optional and -1 or 1
    return int(x["id"]) - int(y["id"])

def cmpProto(x, y):
    xp = x["proto"]
    yp = y["proto"]
    if xp.isdigit() and yp.isdigit():
        return int(xp) - int(yp)

    if operator.eq(xp,  yp):
        return 0
    if operator.gt(xp, yp):
        return 1
    return -1

def listProto(input_dir, excepts = [], root = None, pb_list = None):
    if root is None:
        root = input_dir
    if pb_list is None:
        pb_list = []

    input_dir = os.path.abspath(input_dir)
    root = os.path.abspath(root).replace('\\', '/')
    for x in os.listdir(input_dir):
        if x in excepts:
            continue
        path = os.path.abspath(os.path.join(input_dir, x)).replace('\\', '/')
        if os.path.isdir(path):
            listProto(path, excepts, root, pb_list)
            continue
        if not x.endswith(".proto") or x.startswith("test"):
            continue
        pb_list.append(os.path.basename(path))

    return pb_list

def compileJson(input_dir, project_dir, excepts=[]):
    src_dir = os.path.join(project_dir, "Assets/Scripts")
    dts_dir = os.path.join(src_dir, "auto/net/json")
    check_dir(dts_dir, True)

    pb_list = listProto(input_dir, excepts)

    request_list = []
    msg_dic = {}
    parseProto(input_dir, request_list, msg_dic, excepts)

    request_list.sort(key=cmp_to_key(cmpProto))

    ##############################################
    # pb_json.cs
    pbdts_file = os.path.abspath(os.path.join(dts_dir, "pb_json.cs"))
    pkg_dic = {}
    for key in pb_list:
        if key not in msg_dic:
            continue

        msg = msg_dic[key]
        if "msgs" not in msg or "package" not in msg:
            continue

        _msgs = msg["msgs"]
        if len(_msgs) == 0:
            continue

        pkg = msg["package"]
        pkg_lines = pkg_dic.get(pkg)
        if pkg_lines is None:
            pkg_lines = []
            pkg_dic[pkg] = pkg_lines
        pkg_lines.append("\t// %s" % key)
        for m in _msgs:
            pkg_lines.append("\tpublic class %s {" % (m["name"]))
            if "args" in m:
                for arg in m["args"]:
                    pkg_lines.append("\t\t/// <summary> [%s]%s%s </summary>" % (arg["id"], "desc" in arg and arg["desc"] or "", "default" in arg and ", default: "+arg["default"] or ""))
                    pkg_lines.append("\t\tpublic %s%s %s;" % (arg["jstype"], arg["array"] and "[]" or "", arg["name"]))

            # pkg_lines.append("\t\tconstructor(p?: %s.%s);" %(msg["package"], m["name"]))
            # pkg_lines.append("\t\tstatic encode(message: %s.%s, writer?: protobuf.Writer): protobuf.Writer;" % (msg["package"], m["name"]))
            # pkg_lines.append("\t\tstatic decode(reader: (protobuf.Reader|Uint8Array), length?: number): %s.%s;" % (msg["package"], m["name"]))

            pkg_lines.append("\t}")
    msg_lines = []
    for pkg in pkg_dic:
        pkg_lines = pkg_dic[pkg]
        msg_lines.append("namespace %s {" % pkg)
        for line in pkg_lines:
            msg_lines.append(line)
        msg_lines.append("}")

    write_file(pbdts_file, msg_lines, "\n")

    ##############################################
    # 枚举定义
    enum_dir = os.path.join(dts_dir, "enums")
    rmdir(enum_dir)
    for key in pb_list:
        if key not in msg_dic:
            continue

        msg = msg_dic[key]
        if "enums" not in msg or "package" not in msg:
            continue

        _enums = msg["enums"]
        if len(_enums) == 0:
            continue

        for m in _enums:
            enum_lines.append("// %s" % key)
            enum_lines.append("public enum %s {" % m["name"])
            if "args" in m:
                for arg in m["args"]:
                    if "desc" in arg:
                        enum_lines.append("\t/// <summary> %s </summary>" % arg["desc"])
                    enum_lines.append("\t%s = %s," % (arg["name"], arg["id"]))
            enum_lines.append("}")

            write_file(os.path.join(enum_dir, "%s.cs" % m["name"]), enum_lines, "\n")

def compileProto(input_dir, project_dir, excepts=[]):
    src_dir = os.path.join(project_dir, "Assets/Scripts")
    dts_dir = os.path.join(src_dir, "auto/net")
    check_dir(dts_dir, True)

    pb_list = listProto(input_dir, excepts)

    request_list = []
    msg_dic = {}
    parseProto(input_dir, request_list, msg_dic, excepts)

    request_list.sort(key=cmp_to_key(cmpProto))

    ##############################################
    # ProtoSend.ts 消息发送定义
    class_lines = [
        "using siliu.net;",
        "public static class ProtoSend {"
    ]
    for dic in request_list:
        if "push" in dic:
            continue

        name = dic["name"]

        des_lines = []
        if "desc" in dic:
            des_lines.append("\t/// <summary> [%s] %s </summary>" % (dic["proto"], dic["desc"]))
        up_type = ""
        if "up" in dic and dic["up"] != "":
            up_type = "%s.%s" % (dic["package"], dic["up"])
            exists = False
            for mf in msg_dic:
                msg_file = msg_dic[mf]
                if msg_file["package"] != dic["package"]:
                    continue
                for msg in msg_file["msgs"]:
                    if msg["name"] != dic["up"]:
                        continue
                    exists = True
                    des_lines.append("\t/// <param name=\"data\">上行数据 @see %s</param>" % up_type)
                    break
                if exists:
                    break
        if len(des_lines) > 0:
            class_lines.append(des_lines[0])
            if len(up_type) > 0:
                class_lines.append("\t/// <param name=\"data\">上行数据 @see %s</param>" % up_type)
        class_lines.append("\tpublic static SendEntry %s(%s) {" % (name, len(up_type) > 0 and "%s data" % up_type or ""))
        entry = "\t\treturn new %s(%s)" % ("http" in dic and "SendJsonEntry" or "SendProtoEntry", dic["proto"])
        entry_cfgs = []
        if "http" in dic:
            entry_cfgs.append("http = true")
        if "repeat" in dic:
            entry_cfgs.append("repeat = true")
        if len(entry_cfgs) > 0:
            entry += "{ %s }" % ", ".join(entry_cfgs)
        if "up" in dic:
            if len(up_type) > 0:
                entry += ".EncodeData(data)"
        entry += ";"
        class_lines.append(entry)
        class_lines.append("\t}")

    class_lines.append("}")
    write_file(os.path.join(src_dir, "auto/net/ProtoSend.cs"), class_lines, "\n")

    # ReceiveCfgType.ts 协议下行配置类型文件
    receive_cfg_type_file = os.path.abspath(os.path.join(src_dir, "framework/net/ReceiveCfgType"))
    down_msg_entry_file = os.path.abspath(os.path.join(src_dir, "framework/net/DownMsgEntry"))

    # ProtoReceive.ts 协议下行文件
    factory_file = os.path.abspath(os.path.join(src_dir, "auto/net/ProtoReceive.cs"))
    class_lines = [
        "using System.Collections.Generic;",
        "using siliu.net;",
        "public static class ProtoReceive",
        "{",
        "\tpublic static List<IDownCfg> cfgs = new List<IDownCfg>",
        "\t{",
    ]
    for dic in request_list:
        if "discard" in dic:
            continue

        _proto = ["proto%s = %s" % ("http" in dic and "Str" or "", dic["proto"])]
        # gz压缩
        if "gz" in dic:
            _proto.append("gz: true")
        # 下行处理类
        if "command" in dic and dic["command"] != "":
            if "desc" in dic:
                class_lines.append("\t\t// %s" % dic["desc"])
            class_lines.append("\t\tnew DownCfg<%s> { %s }," % (dic["command"], ", ".join(_proto)))
        # # 下行消息体
        # if "down" in dic and dic["down"] != "":
        #     _proto.append("down: %s.%s" % (dic["package"], dic["down"]))
        # # 推送消息体
        # if "push" in dic and dic["push"] != "":
        #     _proto.append("down: %s.%s" % (dic["package"], dic["push"]))


        ##############################################
        # 消息处理文件定义
        if "command" not in dic or "command_path" not in dic or dic["command_path"] == "":
            continue
        command_file = os.path.join(src_dir, dic["command_path"]) + ".cs"
        params = ""
        if "down" in dic:
            params = "%s" % (dic["down"])
        elif "push" in dic:
            params = "%s" % (dic["push"])
        class_str = " %s : %s<%s> " % (dic["command"], "http" in dic and "DownJsonEntry" or "DownProtoEntry", params)
        params += " data"
        if os.path.exists(command_file):
            command_lines = []
            f = open(command_file, "r", encoding="utf8")
            for line in f:
                class_index = line.find("public class ")
                method_index = line.find("OnSuccess(")
                desc_index = line.find("/** [")
                if class_index > -1:
                    command_lines.append(line[:class_index+len("public class")]+class_str+line[line.find("{"):])
                elif desc_index > -1:
                    if "desc" in dic:
                        command_lines.append("%s%s] %s %s" % (line[:desc_index+len("/** [")], dic["proto"].strip('"'), dic["desc"], line[line.find("*/"):]))
                    else:
                        command_lines.append("%s%s] %s" % (line[:desc_index+len("/** [")], dic["proto"].strip('"'), line[line.find("*/"):]))
                elif method_index > -1:
                    command_lines.append(line[:method_index+len("OnSuccess(")]+params+line[line.find(")"):])
                else:
                    command_lines.append(line)
            f.close()
            write_file(command_file, command_lines)
            continue

        command_lines = ["using siliu.net;", "using %s;" % dic["package"], ""]
        if "desc" in dic:
            command_lines.append("/** [%s] %s */" % (dic["proto"].strip('"'), dic["desc"]))
        else:
            command_lines.append("/** [%s] */" % (dic["proto"]))
        command_lines.append("public class%s" % class_str)
        command_lines.append("{")
        command_lines.append("\tprotected override void OnSuccess(%s)" % params)
        command_lines.append("\t{")
        command_lines.append("\t}")
        command_lines.append("}")

        write_file(command_file, command_lines, "\n")

    class_lines.append("\t};")
    class_lines.append("}")
    write_file(factory_file, class_lines, "\n")

def parseJson(input_dir, project_dir):
    pb_list = listProto(input_dir)

    request_list = []
    msg_dic = {}
    parseProto(input_dir, request_list, msg_dic, excepts)

    request_list.sort(key=cmp_to_key(cmpProto))

def build():
    cur_path = os.path.dirname(__file__)
    input_dir = os.path.abspath(os.path.join(cur_path, "../ext/protobuf"))
    project_dir = os.path.abspath(os.path.join(cur_path, ".."))

    if not os.path.exists(input_dir):
        print("proto path not exists: " + input_dir)
        return
    excepts = []
    compileProto(input_dir, project_dir, excepts)
    # compileJson(os.path.join(input_dir, "login"), project_dir)
    print("protobuf build success")

# -------------- main --------------
if __name__ == '__main__':
    build()
