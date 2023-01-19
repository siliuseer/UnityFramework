using System;
using System.Diagnostics;
using System.IO;

public class BatchSystem
{
    private string m_Argument;
    private string m_Exe;
    private string m_WorkingDirectory;

    public Action<string> m_InfoCallBack;

    public int ExitCode { get; set; }
    public bool IsSuccess { get { return ExitCode == 0; } }


    public BatchSystem(string exe)
    {
        m_Exe = exe;
    }

    public void SetInfo(Action<string> infoCall)
    {
        m_InfoCallBack = infoCall;
    }

    public BatchSystem SetArg(string argument)
    {
        m_Argument = argument;
        return this;
    }

    public BatchSystem SetWorkingDirectory(string path)
    {
        m_WorkingDirectory = path;
        return this;
    }

    public void Start()
    {
        try
        {
            ProcessStartInfo si = new ProcessStartInfo();
            if (m_InfoCallBack != null)
            {
                si.UseShellExecute = false;
                //si.RedirectStandardInput = true;
                si.RedirectStandardOutput = true;
                si.CreateNoWindow = true;
            }
            si.Arguments = m_Argument;
            si.FileName = m_Exe;
            si.WorkingDirectory = m_WorkingDirectory ?? Path.GetDirectoryName(m_Exe);
            Process p = Process.Start(si);
            p.EnableRaisingEvents = true;

            if (m_InfoCallBack != null)
            {
                while (!p.StandardOutput.EndOfStream)
                {
                    m_InfoCallBack(p.StandardOutput.ReadLine());
                }
            }
            p.WaitForExit();
            ExitCode = p.ExitCode;
            p.Close();

        }
        catch (Exception e)
        {
            ExitCode = -1;
            UnityEngine.Debug.LogError("BatBatch错误:" + e.ToString());
        }
    }

    public static BatchSystem BatBatch(string exe, string argument = null, string WorkingDirectory = null)
    {
        BatchSystem bs = new BatchSystem(exe);
        bs.SetArg(argument).SetWorkingDirectory(WorkingDirectory);
        return bs;
    }
}