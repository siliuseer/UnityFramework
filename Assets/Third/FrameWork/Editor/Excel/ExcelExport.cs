using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ExcelDataReader;
using siliu.i18n;
using siliu.tb;
using UnityEditor;
using UnityEngine;

namespace siliu.editor
{
    public static class ExcelExport
    {
        private static readonly char[] Separators = { '|', ',', ';', '&', '#', '$' };

        public static void Export(string excelDir)
        {
            try
            {
                var files = Directory.GetFiles(excelDir, "*.xlsx");
                for (var i = 0; i < files.Length; i++)
                {
                    var filePath = files[i];
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    EditorUtility.DisplayCancelableProgressBar("导表", fileName, i * 1f / files.Length);
                    var type = LoadTbType("tb_" + fileName, typeof(IBaseTb));
                    if (type == null)
                    {
                        continue;
                    }

                    var data = LoadExcel(filePath);
                    SaveTb(type, data);
                }
            }
            finally
            {
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
            Debug.Log($"数据表导出结束: {excelDir}");
        }

        private static void SaveTb(Type type, IReadOnlyList<Dictionary<string, string>> tb)
        {
            var field = type.GetField("Rows");
            var arguments = field.FieldType.GetGenericArguments();
            var genericType = typeof(List<>).MakeGenericType(arguments);
            var list = (IList)Activator.CreateInstance(genericType);

            for (var i = 0; i < tb.Count; i++)
            {
                var rowData = tb[i];
                var add = false;
                var row = Activator.CreateInstance(arguments[0]);
                foreach (var info in row.GetType().GetFields())
                {
                    if (!rowData.TryGetValue(info.Name, out var str))
                    {
                        continue;
                    }

                    add = true;
                    try
                    {
                        info.SetValue(row, Str2Obj(str, info.FieldType));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"导表出错: {type.Name} 第 {i+5} 行, 字段: {info.Name}, 类型: {info.FieldType}, 值: {str}, 错误: {e}");
                    }
                }

                if (add)
                {
                    list.Add(row);
                }
            }

            var data = ScriptableObject.CreateInstance(type);
            field.SetValue(data, list);
            if (!Directory.Exists(Application.dataPath + "/Asset/tb"))
            {
                Directory.CreateDirectory(Application.dataPath + "/Asset/tb");
            }

            AssetDatabase.CreateAsset(data, "Assets/Asset/tb/" + type.Name + ".asset");
            AssetDatabase.SaveAssets();
        }

        private static object Str2Obj(string str, Type fieldType, int sep = 0)
        {
            if (fieldType == typeof(byte)) return byte.Parse(str);
            if (fieldType == typeof(int)) return int.Parse(str);
            if (fieldType == typeof(long)) return long.Parse(str);
            if (fieldType == typeof(float)) return float.Parse(str);
            if (fieldType == typeof(double)) return double.Parse(str);
            if (fieldType == typeof(bool)) return bool.Parse(str);
            if (fieldType == typeof(string)) return str;

            var split = str.Split(Separators[sep]);
            var nextSep = sep + 1;
            if (typeof(Array).IsAssignableFrom(fieldType))
            {
                var setMethod = fieldType.GetMethod("Set");
                if (setMethod == null)
                {
                    return null;
                }

                var array = Activator.CreateInstance(fieldType, split.Length);
                for (var i = 0; i < split.Length; i++)
                {
                    setMethod.Invoke(array, new[] { i, Str2Obj(split[i], fieldType.GetElementType(), nextSep) });
                }

                return array;
            }

            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                var arguments = fieldType.GetGenericArguments();
                var list = (IList)Activator.CreateInstance(fieldType);
                foreach (var t in split)
                {
                    list.Add(Str2Obj(t, arguments[0], nextSep));
                }

                return list;
            }

            if (typeof(IDictionary).IsAssignableFrom(fieldType))
            {
                var arguments = fieldType.GetGenericArguments();
                if (arguments.Length < 2)
                {
                    return null;
                }

                var dic = (IDictionary)Activator.CreateInstance(fieldType);
                var subSep = Separators[nextSep];
                nextSep = sep + 1;
                foreach (var t in split)
                {
                    var e = t.Split(subSep);
                    dic.Add(Str2Obj(e[0], arguments[0], nextSep), Str2Obj(e[1], arguments[1], nextSep));
                }

                return dic;
            }

            if (typeof(I18NKey).FullName == fieldType.FullName || typeof(I18NId).FullName == fieldType.FullName)
            {
                if (split.Length < 2)
                {
                    split = new[] { "lang", split[0] };
                }
            }

            var obj = Activator.CreateInstance(fieldType);
            var fields = fieldType.GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                field.SetValue(obj, Str2Obj(split[i], field.FieldType, nextSep));
            }

            return obj;
        }

        private static Type LoadTbType(string name, Type baseType = null)
        {
            var types = Assembly.Load("Assembly-CSharp").GetTypes();
            foreach (var type in types)
            {
                if (type.Name != name) continue;
                if (baseType != null && !baseType.IsAssignableFrom(type)) continue;
                if (!type.IsPublic) continue;
                if (type.Name.Contains("<") || type.Name.Contains("*")) continue; // 忽略泛型，指针类型

                return type;
            }

            return null;
        }

        private static List<Dictionary<string, string>> LoadExcel(string path, int skipRows = 4, int title = 0)
        {
            var tb = new List<Dictionary<string, string>>();

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataTable = reader.AsDataSet().Tables[0];
            var rows = dataTable.Rows;
            var columns = dataTable.Columns;
            if (skipRows > rows.Count)
            {
                Debug.LogError($"导出空表: {path}");
                return tb;
            }

            var titleRow = rows[title];
            var titles = new Dictionary<string, int>();
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = titleRow[c];
                if (cell is DBNull)
                {
                    continue;
                }

                var key = cell.ToString().Trim();
                if (!titles.TryAdd(key, c))
                {
                    Debug.LogError($"重复key: {key}");
                }
            }

            for (var r = skipRows; r < rows.Count; r++)
            {
                var dic = new Dictionary<string, string>();
                var row = rows[r];
                foreach (var pair in titles)
                {
                    var cell = row[pair.Value];
                    if (cell is DBNull)
                    {
                        continue;
                    }

                    var value = cell.ToString().TrimEnd();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    dic.Add(pair.Key, value);
                }

                if (dic.Count > 0)
                {
                    tb.Add(dic);
                }
            }

            return tb;
        }
    }
}