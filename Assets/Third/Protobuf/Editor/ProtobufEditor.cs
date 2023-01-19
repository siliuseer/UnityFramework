using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

namespace siliu
{
    public class ProtobufEditor
    {
        [MenuItem("Tools/编译Protobuf")]
        private static void Compile()
        {
            var projRoot = Environment.CurrentDirectory;
            var protoDir = Path.Combine(projRoot, "ext/protobuf");
            if (!Directory.Exists(protoDir))
            {
                return;
            }

            var files = Directory.GetFiles(protoDir, "*.proto", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                return;
            }
            var list = new List<string>();
            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file).StartsWith("http_"))
                {
                    continue;
                }
                list.Add(file);
            }
            
            var protoc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "protoc.exe" : "protoc";
            protoc = Path.Combine(projRoot, "Assets/Third/Protobuf/Editor/protoc", protoc).Replace('\\', '/');
            var codeDir = Path.Combine(projRoot, "Assets/Scripts/auto/proto").Replace('\\', '/');
            if (!Directory.Exists(codeDir))
            {
                Directory.CreateDirectory(codeDir);
            }
            RunCmd(protoc, $"--csharp_out=\"{codeDir}\" --proto_path=\"{protoDir}\" {string.Join(" ", list)}");
            UnityEngine.Debug.Log("Compile proto files finish");
            RunCmd("python", "tools/build_proto.py");
            UnityEngine.Debug.Log("Gen proto code finish");
            AssetDatabase.Refresh();
        }

        private static Process RunCmd(string cmd, string args, string workDir = ".", bool waitExit = true)
        {
            try
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var redirectStandardOutput = !isWindows || waitExit;
                var redirectStandardError = !isWindows || waitExit;
                var useShellExecute = isWindows && !waitExit;
                var info = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = useShellExecute,
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError
                };
                var process = Process.Start(info);
                if (waitExit)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"{process.StandardOutput.ReadToEnd()} {process.StandardError.ReadToEnd()}");
                    }
                }

                return process;
            }
            catch (Exception e)
            {
                throw new Exception($"dir: {Path.GetFullPath(workDir)}, command: {cmd} {args}", e);
            }
        }
    }
}