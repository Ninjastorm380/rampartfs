using System.Collections.Concurrent;

namespace RampartFS;

public partial class Driver : Mono.Fuse.NETStandard.FileSystem {
    private readonly Filesystem BaseFilesystem;
}