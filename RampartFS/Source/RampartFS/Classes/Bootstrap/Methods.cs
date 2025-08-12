using System.Diagnostics;
using FuseDotNet;

namespace RampartFS;

internal abstract partial class Bootstrap {
    static void Main (
        String[] Args
    ) {
        if (Args.Length < 2) {
            Console.WriteLine($"usage:");
            Console.WriteLine($"  {Path.GetFileName(Environment.ProcessPath)} <String:BackingDirectoryPath> <String:ControlDirectoryPath> <String:MountPointPath> [UInt64:FileSystemCapacity=107374182400UL] [UInt64:FileSystemCacheCapacity=346030080UL] [UInt64:FileSystemCacheTrimTarget=312475648L] [Boolean:AsyncLaunch=True] [Boolean:Debug=False]");

            return;
        }
        
        String BaseDirectory    = Args[0];
        String ControlDirectory = Args[1];
        String MountPoint       = Args[2];

        Int64   FileSystemCapacity             = 107374182400L;
        Int64   FileSystemCacheCapacity        = 346030080L;
        Int64   FileSystemCacheTrimTarget      = 1048576L;
        Boolean Spawner                        = true;
        Boolean Debug                          = false;

        if (Args.Length > 3 && Int64.TryParse(Args[3], out FileSystemCapacity) == false) {
            FileSystemCapacity = 107374182400L;
        }
        
        if (Args.Length > 4 && Int64.TryParse(Args[4], out FileSystemCacheCapacity) == false) {
            FileSystemCacheCapacity = 346030080L;
        }
        
        if (Args.Length > 5 && Int64.TryParse(Args[5], out FileSystemCacheTrimTarget) == false) {
            FileSystemCacheTrimTarget = 312475648L;
        }
        
        if (Args.Length > 6 && Boolean.TryParse(Args[6], out Spawner) == false) {
            Spawner = true;
        }
        
        if (Args.Length > 7 && Boolean.TryParse(Args[7], out Debug) == false) {
            Debug = false;
        }

        FileSystemCacheTrimTarget = Math.Max(33554432L, FileSystemCacheTrimTarget);
        FileSystemCacheCapacity   = Math.Max(134217728L, FileSystemCacheCapacity);

        if (Spawner == false) {
            if (Debug == true) {
                Driver Driver = new Driver(BaseDirectory, ControlDirectory, FileSystemCapacity, FileSystemCacheCapacity, FileSystemCacheTrimTarget);
                String? ExecutableName = Path.GetFileName(Environment.ProcessPath);
            
                if (ExecutableName == null) {
                    return;
                }
            
                String[] Arguments = [
                    ExecutableName,
                    "-f",
                    "-d",
                    "-o", "allow_other",
                    MountPoint
                ];
            
                Driver.Mount(Arguments); 
            }
            else {
                Driver Driver = new Driver(BaseDirectory, ControlDirectory, FileSystemCapacity, FileSystemCacheCapacity, FileSystemCacheTrimTarget);
                String? ExecutableName = Path.GetFileName(Environment.ProcessPath);
            
                if (ExecutableName == null) {
                    return;
                }
            
                String[] Arguments = [
                    ExecutableName,
                    "-f",
                    "-o", "allow_other",
                    MountPoint
                ];
            
                Driver.Mount(Arguments);
            }
        }
        else {
            String[] Arguments = [
                Args[0],
                Args[1],
                Args[2],
                FileSystemCapacity.ToString(),
                FileSystemCacheCapacity.ToString(),
                FileSystemCacheTrimTarget.ToString(),
                "False",
                Debug.ToString()
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