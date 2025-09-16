using System.Runtime.InteropServices;
using Mono.Unix.Native;

namespace RampartFS;

public class LibC {
    [DllImport("c", EntryPoint = "pread", SetLastError = true)]
    public static extern unsafe Int64 pread(Int32 Fd, void* Buf, UInt64 Count, Int64 Offset);

    [DllImport("c", EntryPoint = "pwrite", SetLastError = true)]
    public static extern unsafe Int64 pwrite(Int32 Fd, void* Buf, UInt64 Count, Int64 Offset);
    
    [DllImport("c", EntryPoint = "utimensat", SetLastError = true)]
    public static extern unsafe Int32 utimensat(Int32 Dirfd, String Pathname, Timespec* Times, Int32 Flags);
}