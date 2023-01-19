using System;
using System.IO;
using UnityEngine;

public static class LogUtil
{
    private static StreamWriter _writer;
    private static FileStream _stream;
    public static void Init(string room)
    {
        var file = $"{room}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
        var dir = Application.dataPath + "/../logout";
        Directory.CreateDirectory(dir);
        _stream = File.Open(dir+"/"+file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        _writer = new StreamWriter(_stream);
        _writer.AutoFlush = true;
        Debug.Log(file);
    }

    public static void Dispose()
    {
        _writer?.Close();
        _writer = null;
        _stream?.Close();
        _stream = null;
    }
    public static void Log(object condition)
    {
        Print(condition, LogType.Log);
    }

    public static void Warning(object condition)
    {
        Print(condition, LogType.Warning);
    }

    public static void Error(object condition)
    {
        Print(condition, LogType.Error);
    }

    private static void Print(object condition, LogType type)
    {
        var line = $"[{type}] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {condition}";
        Debug.unityLogger.Log(type, line);
        _writer.WriteLine(line);
        _writer.Flush();
    }
}