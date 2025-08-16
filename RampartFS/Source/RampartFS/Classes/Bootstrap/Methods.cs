using System.Diagnostics;
using FuseDotNet;
using FuseDotNet.Logging;
using Lightning.Diagnostics.Logging;

namespace RampartFS;

internal abstract partial class Bootstrap {
    static void Main (
        String[] Args
    ) {
        if (Args.Length < 2) {
            Console.WriteLine($"usage:");
            Console.WriteLine($"  {Path.GetFileName(Environment.ProcessPath)} <String:BackingDirectoryPath> <String:ControlDirectoryPath> <String:MountPointPath> [UInt64:FileSystemCapacity=107374182400UL] [UInt64:FileSystemCacheCapacity=346030080UL] [UInt64:FileSystemCacheTrimTarget=312475648L] [Boolean:AsyncLaunch=True] [Boolean:LogToConsole=False] [Boolean:LogToDisk=True] [LogLevel:Verbosity=LogLevel.Warning]");

            return;
        }
        


        Int64    FileSystemCapacity        = 107374182400L;
        Int64    FileSystemCacheCapacity   = 346030080L;
        Int64    FileSystemCacheTrimTarget = 1048576L;
        Boolean  AsyncLaunch               = true;
        Boolean  LogToConsole              = false;
        Boolean  LogToDisk                 = true;
        LogLevel Verbosity                 = LogLevel.Warning;
        
        String BaseDirectory    = Path.GetFullPath(Args[0]);
        String ControlDirectory = Path.GetFullPath(Args[1]);
        String MountPoint       = Path.GetFullPath(Args[2]);
        String LogFolder        = Path.GetFullPath(Args[3]);

        if (Args.Length > 4 && Int64.TryParse(Args[4], out FileSystemCapacity) == false) {
            FileSystemCapacity = 107374182400L;
        }
        
        if (Args.Length > 5 && Int64.TryParse(Args[5], out FileSystemCacheCapacity) == false) {
            FileSystemCacheCapacity = 346030080L;
        }
        
        if (Args.Length > 6 && Int64.TryParse(Args[6], out FileSystemCacheTrimTarget) == false) {
            FileSystemCacheTrimTarget = 312475648L;
        }
        
        if (Args.Length > 7 && Boolean.TryParse(Args[7], out AsyncLaunch) == false) {
            AsyncLaunch = true;
        }
        
        if (Args.Length > 8 && Boolean.TryParse(Args[8], out LogToConsole) == false) {
            LogToConsole = false;
        }
        
        if (Args.Length > 9 && Boolean.TryParse(Args[9], out LogToDisk) == false) {
            LogToDisk = true;
        }
        
        if (Args.Length > 10 && Enum.TryParse(Args[10], out Verbosity) == false) {
            Verbosity = LogLevel.Warning;
        }
        
        FileSystemCacheTrimTarget = Math.Max(33554432L, FileSystemCacheTrimTarget);
        FileSystemCacheCapacity   = Math.Max(134217728L, FileSystemCacheCapacity);

        Log.Level = Verbosity;
        
        if (LogToConsole == true) {
            Log.AddLogTarget(new ConsoleLogTarget());
        }
        
        if (LogToDisk == true) {
            
            if (Directory.Exists(LogFolder) == false) {
                Directory.CreateDirectory(LogFolder);
            }
            Log.AddLogTarget(new FileLogTarget($"{LogFolder}{Path.DirectorySeparatorChar}rampart.log"));
        }
        
        if (AsyncLaunch == false) {
            Log.PrintAsync("Loading driver");
            Log.PrintAsync($"Driver args -> {{ BackingDirectoryPath: '{BaseDirectory}', ControlDirectoryPath: '{ControlDirectory}', MountPointPath: '{MountPoint}', FileSystemCapacity: '{FileSystemCapacity}', FileSystemCacheCapacity: '{FileSystemCacheCapacity}', FileSystemCacheTrimTarget: '{FileSystemCacheTrimTarget}', LogToConsole: '{LogToConsole}', LogToDisk: '{LogToDisk}', Verbosity: '{Verbosity}' }}", LogLevel.Debug);
            
            Driver Driver = new Driver(BaseDirectory, ControlDirectory, FileSystemCapacity, FileSystemCacheCapacity, FileSystemCacheTrimTarget);
            String? ExecutableName = Path.GetFileName(Environment.ProcessPath);
            
            if (ExecutableName == null) {
                Log.PrintAsync($"Executable name is null. not launching", LogLevel.Critical);

                return;
            }
            
            String[] Arguments = [
                ExecutableName,
                "-f",
                "-o", "allow_other",
                MountPoint
            ];
                
            Log.PrintAsync($"Driver mount args -> {{ ExecutableName: '{ExecutableName}', Flags: '-f, -o allow_other', MountPointPath: '{MountPoint}' }}", LogLevel.Debug);
                
            

            
            
            try { Driver.Mount(Arguments); }
            catch (Exception Error) {
                Console.WriteLine(Error);

                throw;
            }
            
            try { Log.Close(); }
            catch (Exception Error) {
                Console.WriteLine(Error);
                
                throw;
            }
        }
        else {
            Log.PrintAsync($"Launching driver using process forking for async start", LogLevel.Debug);
            
            String[] Arguments = [
                Args[0],
                Args[1],
                Args[2],
                Args[3],
                FileSystemCapacity.ToString(),
                FileSystemCacheCapacity.ToString(),
                FileSystemCacheTrimTarget.ToString(),
                "False",
                LogToConsole.ToString(),
                LogToDisk.ToString(),
                Verbosity.ToString()
            ];
            
            ForkSelfSpawn(Arguments);
        }
    }
    
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
}