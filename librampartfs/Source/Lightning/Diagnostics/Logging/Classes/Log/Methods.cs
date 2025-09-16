using System.Threading.Tasks;

namespace Lightning.Diagnostics.Logging;

using System;
using System.Runtime.CompilerServices;

public static partial class Log {
    [MethodImpl(MethodImplOptions.NoInlining)] public static void PrintAsync<T>(String Message, LogLevel LogLevel = LogLevel.Info, [CallerMemberName] String MethodName = "", [CallerFilePath] String FilePath = "", [CallerLineNumber] Int32 LineNumber = 0) {
        switch (Level) {
            case LogLevel.Critical:
                switch (LogLevel) {
                    case LogLevel.Critical: break;
                    case LogLevel.Warning:
                    case LogLevel.Info:
                    case LogLevel.Debug:
                    case LogLevel.Silent:
                    default: return;
                }
                break;
            case LogLevel.Warning:
                switch (LogLevel) {
                    case LogLevel.Critical: break;
                    case LogLevel.Warning: break;
                    case LogLevel.Info:
                    case LogLevel.Debug:
                    case LogLevel.Silent:
                    default: return;
                }
                break;
            case LogLevel.Info:
                switch (LogLevel) {
                    case LogLevel.Critical: break;
                    case LogLevel.Warning: break;
                    case LogLevel.Info: break;
                    case LogLevel.Debug:
                    case LogLevel.Silent:
                    default: return;
                }
                break;
            case LogLevel.Debug:
                switch (LogLevel) {
                    case LogLevel.Critical: break;
                    case LogLevel.Warning: break;
                    case LogLevel.Info: break;
                    case LogLevel.Debug: break;
                    case LogLevel.Silent:
                    default: return;
                }
                break;
            case LogLevel.Silent:
            default: return;
        }
        
        Task.Run(InternalPrintAsync);
        
        return;
        
        void InternalPrintAsync () {
            
            String LevelString = LogLevel switch { 
                LogLevel.Critical => Critical,
                LogLevel.Warning  => Warning,
                LogLevel.Info     => Info,
                LogLevel.Debug    => Debug,
                _                 => String.Empty 
            };
            
            if (OutputTargets.Count > 0) {
                DateTime Now = DateTime.UtcNow;

                Type CallingType = typeof(T);
                String Logged = $"{LevelString}On '{Now.ToShortDateString()}{DateTimeSplit}{Now.ToLongTimeString()}' At '{CallingType.FullName}.{MethodName}{LineSplit}{LineNumber:000000}'{EndCap}{Message}";
                foreach (ILogTarget Target in OutputTargets) {
                    Target.PrintAsync(LogLevel, Logged);
                }
            }
        }
    }

    public static void Close () {
        if (OutputTargets.Count > 0) {
            foreach (ILogTarget Target in OutputTargets) {
                Target.Close();
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

    public static LogLevel ToLogLevel(String Value) {
        switch (Value.ToLower()) {
            case "debug": return LogLevel.Debug;
            case "info": return LogLevel.Info;
            case "warn": return LogLevel.Warning;
            case "warning": return LogLevel.Warning;
            case "crit": return LogLevel.Critical;
            case "critical": return LogLevel.Critical;
            case "quiet": return LogLevel.Silent;
            case "silent": return LogLevel.Silent;
            default: return LogLevel.Warning;
        }
    }
}
