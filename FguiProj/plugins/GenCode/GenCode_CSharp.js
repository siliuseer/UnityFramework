"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GenCode_CSharp = void 0;
const csharp_1 = require("csharp");
const CodeWriter_1 = require("./CodeWriter");
function GenCode_CSharp(handler) {
    const codeExt = "cs";
    let publishSettings = handler.project.GetSettings("Publish");
    let settings = publishSettings.codeGeneration;
    let codePkgName = handler.ToFilename(handler.pkg.name); //convert chinese to pinyin, remove special chars etc.
    let exportCodePath = handler.exportCodePath + '/' + codePkgName;
    let namespaceName = codePkgName;
    let isMonoGame = handler.project.type == csharp_1.FairyEditor.ProjectType.MonoGame;
    if (settings.packageName)
        namespaceName = settings.packageName + '.' + namespaceName;
    else
        namespaceName = "fui." + namespaceName;
    //CollectClasses(stripeMemeber, stripeClass, fguiNamespace)
    let classes = handler.CollectClasses(settings.ignoreNoname, settings.ignoreNoname, null);
    handler.SetupCodeFolder(exportCodePath, codeExt); //check if target folder exists, and delete old files
    let getMemberByName = settings.getMemberByName;
    let classCnt = classes.Count;
    let writer = new CodeWriter_1.default();
    for (let i = 0; i < classCnt; i++) {
        let classInfo = classes.get_Item(i);
        let clsName = classInfo.className.substring(settings.classNamePrefix.length);
        let members = classInfo.members;
        writer.reset();
        writer.writeln('using FairyGUI;');
        writer.writeln('using FairyGUI.Utils;');
        writer.writeln();
        writer.writeln('namespace %s', namespaceName);
        writer.startBlock();
        writer.writeln('public class %s : %s', clsName, classInfo.superClassName);
        writer.startBlock();
        writer.writeln('public const string URL = "ui://%s%s";', handler.pkg.id, classInfo.resId);
        writer.writeln();
        let memberCnt = members.Count;
        for (let j = 0; j < memberCnt; j++) {
            let memberInfo = members.get_Item(j);
            writer.writeln('public %s %s;', memberInfo.type, memberInfo.varName);
        }
        // writer.writeln('public static %s CreateInstance()', clsName);
        // writer.startBlock();
        // writer.writeln('return (%s)UIPackage.CreateObject("%s", "%s");', clsName, handler.pkg.name, classInfo.resName);
        // writer.endBlock();
        // writer.writeln();
        if (isMonoGame) {
            writer.writeln("protected override void OnConstruct()");
            writer.startBlock();
        }
        else {
            writer.writeln('public override void ConstructFromXML(XML xml)');
            writer.startBlock();
            writer.writeln('base.ConstructFromXML(xml);');
            writer.writeln();
        }
        for (let j = 0; j < memberCnt; j++) {
            let memberInfo = members.get_Item(j);
            if (memberInfo.group == 0) {
                if (getMemberByName)
                    writer.writeln('%s = (%s)GetChild("%s");', memberInfo.varName, memberInfo.type, memberInfo.name);
                else
                    writer.writeln('%s = (%s)GetChildAt(%s);', memberInfo.varName, memberInfo.type, memberInfo.index);
            }
            else if (memberInfo.group == 1) {
                if (getMemberByName)
                    writer.writeln('%s = GetController("%s");', memberInfo.varName, memberInfo.name);
                else
                    writer.writeln('%s = GetControllerAt(%s);', memberInfo.varName, memberInfo.index);
            }
            else {
                if (getMemberByName)
                    writer.writeln('%s = GetTransition("%s");', memberInfo.varName, memberInfo.name);
                else
                    writer.writeln('%s = GetTransitionAt(%s);', memberInfo.varName, memberInfo.index);
            }
        }
        writer.endBlock();
        writer.endBlock(); //class
        writer.endBlock(); //namepsace
        writer.save(exportCodePath + '/' + clsName + '.cs');
    }
    genBinder(handler, codeExt, settings.classNamePrefix, publishSettings.fileExtension, writer);
}
function genBinder(handler, codeExt, classNamePrefix, fileExtension, writer) {
    writer.reset();
    writer.writeln('using System;');
    writer.writeln('using System.Collections.Generic;');
    const binders = [];
    const depends = [];
    const pkgs = handler.project.allPackages;
    const projCodePath = handler.exportCodePath;
    for (let i = 0; i < pkgs.Count; i++) {
        const pkg = pkgs.get_Item(i);
        const pkgName = handler.ToFilename(pkg.name);
        const items = pkg.items;
        for (let j = 0; j < items.Count; j++) {
            const item = items.get_Item(j);
            if ("component" !== item.type) {
                continue;
            }
            const itemName = handler.ToFilename(item.name);
            const className = itemName;
            const pkgClassName = pkgName + "/" + className;
            const codePath = projCodePath + '/' + pkgClassName + "." + codeExt;
            console.log(codePath);
            if (!csharp_1.System.IO.File.Exists(codePath)) {
                continue;
            }
            const fullName = pkgName + "." + className;
            binders.push('{'+fullName+'.URL, typeof('+fullName+')},');
            depends.push('{"fui.'+fullName+'", new []{"'+pkgName+'"}},');
        }
    }
    writer.writeln();
    writer.writeln('namespace fui');
    writer.startBlock();
    writer.writeln("public static class FuiCfg");
    writer.startBlock();
    // writer.writeln('public const string pkgFileExtension = "%s";', fileExtension);
    
    writer.writeln('public static readonly Dictionary<string, Type> Binders = new Dictionary<string, Type>');
    writer.startBlock();
    for (let i = 0; i < binders.length; i++) {
        writer.writeln(binders[i]);
    }
    writer.endBlock(); //binders
    writer.writeln(';');
    
    writer.writeln('public static readonly Dictionary<string, string[]> Depends = new Dictionary<string, string[]>');
    writer.startBlock();
    for (let i = 0; i < depends.length; i++) {
        writer.writeln(depends[i]);
    }
    writer.endBlock(); //Depends
    writer.writeln(';');

    writer.endBlock(); //class
    writer.endBlock(); //namespace
    writer.save(projCodePath + '/FuiCfg.' + codeExt);
}

exports.GenCode_CSharp = GenCode_CSharp;
