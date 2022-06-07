using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace siliu
{
    public class ProtobufEditor
    {
        [MenuItem("Tools/Compile Proto")]
        private static void Compile()
        {
            var protoDir = Path.Combine(Environment.CurrentDirectory, "ext/protobuf");
            if (!Directory.Exists(protoDir))
            {
                return;
            }
            var files = Directory.GetFiles(protoDir, "*.proto", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                return;
            }
            
            var protoc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "protoc.exe" : "protoc";
            protoc = Path.Combine(Application.dataPath, "Editor/Protobuf/protoc", protoc);
            var codeDir = Path.Combine(Application.dataPath, "AutoGen/proto");
            if (!Directory.Exists(codeDir))
            {
                Directory.CreateDirectory(codeDir);
            }
            RunCmd(protoc, $"--csharp_out=\"{codeDir}\" --proto_path=\"{protoDir}\" {string.Join(" ", files)}");
            UnityEngine.Debug.Log("Compile proto files finish");
            AssetDatabase.Refresh();
        }

        public static Process RunCmd(string cmd, string args, string workDir = ".", bool waitExit = true)
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