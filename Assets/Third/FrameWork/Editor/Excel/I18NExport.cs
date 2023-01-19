using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using siliu.i18n;
using UnityEditor;
using UnityEngine;

namespace siliu.editor
{
    public static class I18NExport
    {
        [MenuItem("Tools/语言包/开发", priority = 2000)]
        private static void Menu()
        {
            Export(ExcelType.dev);
        }

        private static void Export(ExcelType excel)
        {
            try
            {
                var srcDir = Application.dataPath+"/Scripts/auto/i18n";
                Directory.CreateDirectory(srcDir);
                var assetDir = Application.dataPath+"/Asset/i18n";
                Directory.CreateDirectory(assetDir);
                var map = new Dictionary<string, I18NTb>();
                var files = Directory.GetFiles($"{ExcelExport.ExtDir}/{excel}/i18n", "*.xlsx");
                var idLines = new List<string>();
                for (var i = 0; i < files.Length; i++)
                {
                    var filePath = files[i];
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    EditorUtility.DisplayCancelableProgressBar("导语言包", fileName, i * 1f / files.Length);
                    var table = LoadExcel(filePath);
                    var titleRow = table.Rows[0];
                    var useIdKey = titleRow[0].ToString().Trim() == "id";

                    var langs = new Dictionary<int, string>();
                    for (var column = 1; column < table.Columns.Count; column++)
                    {
                        var lang = titleRow[column].ToString().Trim();
                        if (string.IsNullOrEmpty(lang))
                        {
                            continue;
                        }

                        langs.Add(column, lang);
                    }

                    var lines = new List<string>();

                    foreach (var (column, lang) in langs)
                    {
                        if (!map.TryGetValue(lang, out var tb))
                        {
                            tb = ScriptableObject.CreateInstance<I18NTb>();
                            map.Add(lang, tb);
                        }

                        for (var row = 1; row < table.Rows.Count; row++)
                        {
                            var keyCell = table.Rows[row][0];
                            if (keyCell is DBNull)
                            {
                                Debug.LogError($"{fileName} 语言: {lang}, 第[{row}]行缺少key");
                                continue;
                            }

                            var cell = table.Rows[row][column];
                            if (cell is DBNull)
                            {
                                Debug.LogError($"{fileName} key: {keyCell} 缺少语言: {lang}");
                                continue;
                            }

                            var key = keyCell.ToString().Trim();
                            var value = cell.ToString().TrimEnd();

                            if (useIdKey)
                            {
                                if (!idLines.Contains(fileName))
                                {
                                    idLines.Add(fileName);
                                }
                                if (!tb.TryAdd(fileName, int.Parse(key), value))
                                {
                                    Debug.LogError($"{fileName} 语言: {lang}, 重复key: {key}");
                                }
                            }
                            else
                            {
                                if (lang == I18NType.SimplifiedChinese.ToString())
                                {
                                    lines.Add($"    /// <summary> {value} </summary>");
                                    lines.Add($"    public static string {key} => I18N.Find(\"{fileName}\", \"{key}\");");
                                }

                                if (!tb.TryAdd(fileName, key, value))
                                {
                                    Debug.LogError($"{fileName} 语言: {lang}, 重复key: {key}");
                                }
                            }
                        }
                    }

                    if (lines.Count > 0)
                    {
                        lines.Insert(0, $"public class {fileName} {{");
                        lines.Insert(0, "using siliu.i18n;");
                        lines.Add("}");
                        File.WriteAllLines($"{srcDir}/{fileName}.cs", lines, Encoding.UTF8);
                    }
                }

                if (idLines.Count > 0)
                {
                    var lines = new List<string>
                    {
                        "using siliu.i18n;",
                        "public class I18NIds {",
                    };
                    foreach (var id in idLines)
                    {
                        lines.Add($"    public static string {id}(int key) {{ return I18N.Find(\"{id}\", key); }}");
                    }
                    lines.Add("}");
                    File.WriteAllLines(Application.dataPath+"/Scripts/auto/i18n/I18NIds.cs", lines, Encoding.UTF8);
                }

                foreach (var pair in map)
                {
                    Debug.Log("语言类型: " + pair.Key);
                    AssetDatabase.CreateAsset(pair.Value, "Assets/Asset/i18n/" + pair.Key + ".asset");
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
        }

        public static DataTable LoadExcel(string path)
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            return reader.AsDataSet().Tables[0];
        }
    }
}