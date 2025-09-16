using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Lightning.Diagnostics.Logging;

public partial class FileLogTarget : ILogTarget {
    public FileLogTarget (
        String AbsoluteFilePath
    ) {
        if (File.Exists(AbsoluteFilePath) == true) {
            DateTime Now          = DateTime.UtcNow;
            String   FileName     = Path.GetFileName(AbsoluteFilePath);
            String   ParentFolder = Directory.GetParent(AbsoluteFilePath)?.FullName ?? String.Empty;
            
            File.Move(AbsoluteFilePath, $"{ParentFolder}{Path.DirectorySeparatorChar}{Now.Year.ToString("0000")}-{Now.Month.ToString("00")}-{Now.Day.ToString("00")}-{Now.Hour.ToString("00")}-{Now.Minute.ToString("00")}-{Now.Second.ToString("00")}-{Now.Millisecond.ToString("0000")}-{Now.Microsecond.ToString("0000")}-{Now.Nanosecond.ToString("0000")}-{FileName}");

            List<String> LogFilePaths = Directory.GetFiles(ParentFolder, "*", SearchOption.TopDirectoryOnly).ToList();
            
            
            if (LogFilePaths.Count > 10) {
                LogFilePaths.Sort(Comparison);

                do {
                    File.Delete(LogFilePaths[0]);
                    LogFilePaths.RemoveAt(0);
                } while (LogFilePaths.Count > 10);
            }
        }
        
        PrintQueue   = new ConcurrentQueue<(LogLevel, String)>();
        OutputStream = new FileStream(AbsoluteFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        OutputWriter = new StreamWriter(OutputStream, Encoding.UTF8, -1, false);
        Running      = true;
        
        Thread PrintQueueThread = new Thread(PrintQueueMethod);
        PrintQueueThread.Start();
    }

    private Int32 Comparison(String X, String Y) {
        FileInfo XInfo = new FileInfo(X);
        FileInfo YInfo = new FileInfo(Y);

        return XInfo.LastWriteTimeUtc.CompareTo(YInfo.LastWriteTimeUtc);
    }

    private void PrintQueueMethod () {
        while (Running == true) {
            while (PrintQueue.TryDequeue(out (LogLevel, String) Args)) {
                OutputWriter.WriteLine(Args.Item2);
                OutputWriter.Flush();
            }
            Thread.Sleep(100);
        }
    }
    
    public void PrintAsync (
        LogLevel Severity,
        String   Message
    ) {
        PrintQueue.Enqueue(new ValueTuple<LogLevel, String>(Severity, Message));
    }

    public void Close () {
        Running = false;
        
        OutputWriter.Close();
        OutputWriter.Dispose();
    }
}