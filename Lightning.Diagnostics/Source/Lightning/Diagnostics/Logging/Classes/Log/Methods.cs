using System.Threading.Tasks;

namespace Lightning.Diagnostics.Logging;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public partial class Log {
    public static void PrintAsync(String Message, LogLevel LogLevel = LogLevel.Info, [CallerMemberName] String MethodName = "", [CallerFilePath] String FilePath = "", [CallerLineNumber] Int32 LineNumber = 0) {
        if (!((Int32)LogLevel <= (Int32)Level & Level != LogLevel.Silent)) return;
        
        StackFrame CallingFrame = new StackFrame(1); Task.Run(InternalPrintAsync);
        
        return;
        
        void InternalPrintAsync () {
            
            DiagnosticMethodInfo? CallingMethod = DiagnosticMethodInfo.Create(CallingFrame);

            String LevelString = LogLevel switch { 
                LogLevel.Critical => Critical,
                LogLevel.Warning  => Warning,
                LogLevel.Info     => Info,
                LogLevel.Debug    => Debug,
                _                 => String.Empty 
            };

            DateTime Now = DateTime.UtcNow;
            String Logged = $"{LevelString}{CallingMethod?.DeclaringTypeName}{TimeSplit}{Now.ToShortDateString()}{DateTimeSplit}{Now.ToLongTimeString()}{LineSplit}{LineNumber:000000}{EndCap}{Message}";

            if (OutputTargets.Count > 0) {
                for (Int32 Index = 0; Index < OutputTargets.Count; Index++) {
                    OutputTargets[Index].PrintAsync(LogLevel, Logged);
                }
            }
        }
    }

    public static void Close () {
        if (OutputTargets.Count > 0) {
            for (Int32 Index = 0; Index < OutputTargets.Count; Index++) {
                OutputTargets[Index].Close();
            }
        }
    }

    public static void AddLogTarget (
        ILogTarget Target
    ) {
        OutputTargets.Add(Target);
    }
    
    public static void RemoveLogTarget (
        ILogTarget Target
    ) {
        OutputTargets.Remove(Target);
    }
    
    public static Boolean ContainsLogTarget (
        ILogTarget Target
    ) {
        return OutputTargets.Contains(Target);
    }
}
