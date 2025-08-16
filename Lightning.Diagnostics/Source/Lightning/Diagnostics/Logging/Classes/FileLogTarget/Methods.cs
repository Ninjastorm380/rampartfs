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
            
            File.Move(AbsoluteFilePath, $"{ParentFolder}{Path.DirectorySeparatorChar}{Now.Year}-{Now.Month}-{Now.Day}-{Now.Hour}-{Now.Minute}-{Now.Second}-{Now.Millisecond}-{Now.Microsecond}-{Now.Nanosecond}-{FileName}");
        }
        
        PrintQueue   = new ConcurrentQueue<(LogLevel, String)>();
        OutputStream = new FileStream(AbsoluteFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        OutputWriter = new StreamWriter(OutputStream, Encoding.UTF8, -1, true);
        Running      = true;
        
        Thread PrintQueueThread = new Thread(PrintQueueMethod);
        PrintQueueThread.Start();
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
            
        OutputWriter.Flush();
        OutputWriter.Close();
        OutputWriter.Dispose();
            
        OutputStream.Flush();
        OutputStream.Close();
        OutputStream.Dispose();
    }
}