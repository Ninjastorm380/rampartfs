using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Lightning.Diagnostics.Logging;

public partial class ConsoleLogTarget : ILogTarget {
    static ConsoleLogTarget () {
        PrintQueue = new ConcurrentQueue<(LogLevel, String)>();

        Running = true;
        Thread PrintQueueThread = new Thread(PrintQueueMethod);
        PrintQueueThread.Start();
    }

    private static void PrintQueueMethod () {
        while (Running == true) {
            ConsoleColor CurrentColor = Console.ForegroundColor;
            ConsoleColor PreviousColor = ConsoleColor.White;
            while (PrintQueue.TryDequeue(out (LogLevel, String) Args)) {
                ConsoleColor SelectedColor = Args.Item1 switch {
                    LogLevel.Critical => ConsoleColor.Red,
                    LogLevel.Warning  => ConsoleColor.Yellow,
                    LogLevel.Info     => ConsoleColor.White,
                    LogLevel.Debug    => ConsoleColor.Blue,
                    _                 => PreviousColor
                };
                
                if (PreviousColor != SelectedColor) {
                    PreviousColor           = SelectedColor;
                    Console.ForegroundColor = SelectedColor;
                }
                
                Console.WriteLine(Args.Item2);
            }
            Console.ForegroundColor = CurrentColor;
            Thread.Sleep(100);
        }
    }

    public void PrintAsync (
        LogLevel Severity,
        String Message
    ) {
        PrintQueue.Enqueue(new ValueTuple<LogLevel, String>(Severity, Message));
    }

    public void Close () {
        Running = false;
    }
}