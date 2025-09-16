using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Lightning.Diagnostics.Logging;

namespace RampartFS;

public abstract partial class Chainloader {
    private static void ForkSelfSpawn (
        String[] Args
    ) {
        String? ProcessPath = Environment.ProcessPath;

        if (ProcessPath == null) return;

        ProcessStartInfo Info = new ProcessStartInfo(ProcessPath, Args) {
            UseShellExecute = true
        };

        Process.Start(Info);
    }
    
    private static void PrintHelp() {
        String DefaultParameters = $"<String:StorageFolderPath> <String:ControlFolderPath> <String:LogFolderPath> <String:MountFolderPath> [Int64:StorageMaximum = {DefaultStorageMaximum}] [Boolean:LogToConsole = {DefaultLogToConsole}] [Boolean:LogToDisk = {DefaultLogToDisk}] [LogLevel:Verbosity = {DefaultVerbosity}] [Boolean:Async = {DefaultAsync}]";
        Console.Write($"rampartfs - a high performance folder redirection filesystem with runtime configurable storage limits.{Environment.NewLine}  args: {DefaultParameters}");
    }
    
    public static Boolean TryLaunch(String[] Args, [NotNullWhen(true)] out Filesystem? System) {
        if (Args.Length < 4 || Args[0].Equals("help", StringComparison.OrdinalIgnoreCase) == true) {
            PrintHelp();
            
            System = null;
            return false;
        }
        
        String StorageFolderPath = Path.GetFullPath(Args[0]);
        String ControlFolderPath = Path.GetFullPath(Args[1]);
        String LogFolderPath = Path.GetFullPath(Args[2]);
        String MountFolderPath = Path.GetFullPath(Args[3]);
        
        if (Directory.Exists(StorageFolderPath) == false) {
            Directory.CreateDirectory(StorageFolderPath);
        }
        
        if (Directory.Exists(ControlFolderPath) == false) {
            Directory.CreateDirectory(ControlFolderPath);
        }
        
        if (Directory.Exists(LogFolderPath) == false) {
            Directory.CreateDirectory(LogFolderPath);
        }
        
        if (Directory.Exists(MountFolderPath) == false) {
            Directory.CreateDirectory(MountFolderPath);
        }
        
        Int64 StorageMaximum = DefaultStorageMaximum;
        Boolean LogToConsole = DefaultLogToConsole;
        Boolean LogToDisk = DefaultLogToDisk;
        LogLevel Verbosity = DefaultVerbosity;
        Boolean Async = DefaultAsync;
        
        if (Args.Length > 4 && Int64.TryParse(Args[4], out StorageMaximum) == false) {
            StorageMaximum = DefaultStorageMaximum;
        }
        
        if (Args.Length > 5 && Boolean.TryParse(Args[5], out LogToConsole) == false) {
            LogToConsole = DefaultLogToConsole;
        }
        
        if (Args.Length > 6 && Boolean.TryParse(Args[6], out LogToDisk) == false) {
            LogToDisk = DefaultLogToDisk;
        }
        
        if (Args.Length > 7 ) {
            Verbosity = Log.ToLogLevel(Args[7]);
        }
        
        if (Args.Length > 8 && Boolean.TryParse(Args[8], out Async) == false) {
            Async = DefaultAsync;
        }

        if (Async == true) {
            String[] Arguments = [
                Args[0],
                Args[1],
                Args[2],
                Args[3],
                StorageMaximum.ToString(),
                LogToConsole.ToString(),
                LogToDisk.ToString(),
                Verbosity.ToString(),
                "False"
            ];
            
            ForkSelfSpawn(Arguments);
            
            System = null;
            return false;
        }
        
        Filesystem Driver = new Filesystem(StorageFolderPath, ControlFolderPath, LogFolderPath, MountFolderPath, StorageMaximum, LogToConsole, LogToDisk, Verbosity, true);
        
        System = Driver;
        return true;
    }
}