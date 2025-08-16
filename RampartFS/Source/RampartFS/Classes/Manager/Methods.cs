using FuseDotNet;
using Lightning.Diagnostics.Logging;
using Mono.Unix;
using Mono.Unix.Native;

namespace RampartFS;

internal partial class Manager {
    public Manager (
        String RootPath,
        String ControlPath,
        Int64  MaximumStorage,
        Int64  MaximumCache,
        Int64  TrimTarget
    ) {
        BaseController = new Controller<Int64>(ControlPath);
        
        BaseController["MaximumStorage", MaximumStorage] = MaximumStorage;
        BaseController["MaximumCache", MaximumCache]     = MaximumCache;
        BaseController["TrimTarget", TrimTarget]         = TrimTarget;
        
        BaseController.LoadRaw("CachedEntries");
        
        BaseCache = new Cache(RootPath, BaseController);
        
        BaseCurrentStorage = 0;
        foreach (String AbsoluteFilePath in Directory.GetFiles(RootPath, "*", SearchOption.AllDirectories)) {
            UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsoluteFilePath);
            BaseCurrentStorage = BaseCurrentStorage + Info.Length;
        }
        
        BaseController["Closed", 0] = 0;
    }
    
    public PosixResult ReadFile (
        in  String     AbsolutePath,
        in  Span<Byte> Buffer,
        in  Int64      Offset,
        out Int32      BytesRead
    ) {
        if (File.Exists(AbsolutePath) == false) {
            Log.PrintAsync($"File '{AbsolutePath}' does not exist", LogLevel.Warning);
            BytesRead = 0;
            return PosixResult.ENOENT;
        }
        
        Log.PrintAsync($"Reading {Buffer.Length} bytes from '{AbsolutePath}'", LogLevel.Debug);
        PosixResult Result = BaseCache.ReadFile(AbsolutePath, Buffer, Offset, out BytesRead);
        Log.PrintAsync($"Read {BytesRead} bytes from '{AbsolutePath}'", LogLevel.Debug);
        
        return Result;
    }
    
    public PosixResult WriteFile (
        in  String             AbsolutePath,
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        out Int32              BytesWritten
    ) {
        Log.PrintAsync($"Writing {Buffer.Length} bytes to '{AbsolutePath}'", LogLevel.Debug);
        PosixResult Result = BaseCache.WriteFile(AbsolutePath, Buffer, Offset, out BytesWritten, out Int64 Difference, BaseCurrentStorage, BaseController["MaximumStorage", 0]);
        Log.PrintAsync($"Wrote {BytesWritten} bytes to '{AbsolutePath}'", LogLevel.Debug);
        
        Interlocked.Add(ref BaseCurrentStorage, Difference);
        Log.PrintAsync($"Used capacity changed by {Difference} bytes, now at {BaseCurrentStorage} bytes used", LogLevel.Debug);
        
        return Result;
    }
    
    public PosixResult TruncateFile (
        in String AbsolutePath,
        in Int64  Length
    ) {
        Log.PrintAsync($"Resizing '{AbsolutePath}' to {Length} bytes", LogLevel.Debug);
        PosixResult Result = BaseCache.TruncateFile(AbsolutePath, Length, out Int64 Difference, BaseCurrentStorage, BaseController["MaximumStorage", 0]);
        Log.PrintAsync($"Resized '{AbsolutePath}' to {Length} bytes", LogLevel.Debug);
        
        Interlocked.Add(ref BaseCurrentStorage, Difference);
        Log.PrintAsync($"Used capacity changed by {Difference} bytes, now at {BaseCurrentStorage} bytes used", LogLevel.Debug);
        
        return Result;
    }
    
    public PosixResult Unlink (
        in String AbsolutePath
    ) {
        UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);
        if (Info.Exists == false) {
            Log.PrintAsync($"Entry '{AbsolutePath}' does not exist", LogLevel.Warning);
            return PosixResult.ENOENT;
        }
        
        if (Info.FileType == FileTypes.RegularFile) {
            BaseCache.ConditionalDrop(AbsolutePath, out Int64 Difference);
            
            Interlocked.Add(ref BaseCurrentStorage, Difference);
            Log.PrintAsync($"Used capacity changed by {Difference} bytes, now at {BaseCurrentStorage} bytes used", LogLevel.Debug);
        }
        
        if (Syscall.unlink(AbsolutePath) != -1) {
            return PosixResult.Success;
        }

        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
            
        return ErrorResult;
    }

    public PosixResult FolderExists (
        in String AbsolutePath
    ) {
        IntPtr Descriptor = Syscall.opendir(AbsolutePath);
        if (Descriptor > 0) {
            if (Syscall.closedir(Descriptor) != -1) {
                return PosixResult.Success;
            }
        }
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult FileExists (
        in String AbsolutePath
    ) {
        Int32 Descriptor = Syscall.open(AbsolutePath, OpenFlags.O_RDONLY);
        if (Descriptor > 0) {
            if (Syscall.close(Descriptor) != -1) {
                return PosixResult.Success;
            }
        }
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    private static bool IsFileType (
        FilePermissions mode,
        FilePermissions type
    ) {
        return (mode & FilePermissions.S_IFMT) == type;
    }
    
    public PosixResult GetAttributes (
        in String AbsolutePath,
        out FuseFileStat Attributes
    ) {
        return GetAttributesCore(AbsolutePath, out Attributes);
    }
    
    public PosixResult SymbolicLink (
        in String AbsolutePath,
        in String ArbitraryPath
    ) {
        if (Syscall.symlink(ArbitraryPath, AbsolutePath) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult ReadLink (
        in String     AbsolutePath,
        in Span<Byte> ArbitraryPathData
    ) {
        Byte[] Buffer = new Byte[1048576];
        Int32  Length = (Int32)Syscall.readlink(AbsolutePath, Buffer);

        if (Length != -1) {
            Span<Byte> BufferSpan = Buffer.AsSpan().Slice(0, Length + 1);
            BufferSpan[BufferSpan.Length - 1] = 0;
        
            BufferSpan.CopyTo(ArbitraryPathData);
        
            return PosixResult.Success;
        }
        
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult UpdateTime (
        in String    AbsolutePath,
        in TimeSpec  AccessedOn,
        in TimeSpec  ModifiedOn
    ) {
        Timespec[] Times = [
            new Timespec { tv_sec = AccessedOn.tv_sec, tv_nsec = AccessedOn.tv_nsec },
            new Timespec { tv_sec = ModifiedOn.tv_sec, tv_nsec = ModifiedOn.tv_nsec }
        ];
        
        if (Syscall.utimensat(AT_FDCWD, AbsolutePath, Times, 0) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult CreateFolder (
        in String        AbsolutePath,
        in PosixFileMode Mode
    ) {
        if (Syscall.mkdir(AbsolutePath, (FilePermissions)Mode) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult DeleteFolder (
        in String AbsolutePath
    ) {
        if (Syscall.rmdir(AbsolutePath) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult CreateFile (
        in String     AbsolutePath,
        PosixFileMode Mode
    ) {
        Int32 Descriptor = Syscall.open(AbsolutePath, OpenFlags.O_CREAT | OpenFlags.O_WRONLY | OpenFlags.O_TRUNC, (FilePermissions)Mode);
        if (Descriptor != -1) {
            if (Syscall.close(Descriptor) != -1) {
                return PosixResult.Success;
            }
        }
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult ChangeMode (
        in String        AbsolutePath,
        in PosixFileMode Mode
    ) {
        if (Syscall.chmod(AbsolutePath, (FilePermissions)Mode) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult ChangeOwners (
        in String AbsolutePath,
        in Int32  UserID,
        in Int32  GroupID
    ) {
        if (Syscall.chown(AbsolutePath, UserID, GroupID) != -1) return PosixResult.Success;
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
    
    public PosixResult CheckPermissions (
        in String          AbsolutePath,
        in PosixAccessMode Mode
    ) {
        UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);

        if (Mode.HasFlag(PosixAccessMode.Exists) == true & Info.CanAccess(AccessModes.F_OK) == false) {
            Log.PrintAsync($"Entry '{AbsolutePath}' does not exist", LogLevel.Warning);
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Read) == true & Info.CanAccess(AccessModes.R_OK) == false) {
            Log.PrintAsync($"Entry '{AbsolutePath}' can not be read", LogLevel.Warning);
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Write) == true & Info.CanAccess(AccessModes.W_OK) == false) {
            Log.PrintAsync($"Entry '{AbsolutePath}' can not be written", LogLevel.Warning);
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Execute) == true & Info.CanAccess(AccessModes.X_OK) == false) {
            Log.PrintAsync($"Entry '{AbsolutePath}' can not be executed", LogLevel.Warning);
            return PosixResult.EACCES;
        }
        
        return PosixResult.Success;
    }
    
    public PosixResult GetFolderContents (
        in  String                    AbsolutePath,
        out IEnumerable<FuseDirEntry> Entries
    ) {
        if (Directory.Exists(AbsolutePath) == false) {
            Log.PrintAsync($"Folder '{AbsolutePath}' does not exist", LogLevel.Warning);
            Entries = new List<FuseDirEntry>();
            return PosixResult.ENOENT;
        }
        
        List<FuseDirEntry> Contents = new List<FuseDirEntry>(256);
        foreach (String ChildAbsolutePath in Directory.GetFileSystemEntries(AbsolutePath, "*", SearchOption.TopDirectoryOnly)) {
            if (GetAttributesCore(ChildAbsolutePath, out FuseFileStat ChildAttributes) == PosixResult.Success) {
                FuseDirEntry ChildEntry = new FuseDirEntry() {
                    Stat   = ChildAttributes,
                    Name   = Path.GetFileName(ChildAbsolutePath),
                    Offset = 0,
                    Flags  = FuseFillDirFlags.FillDirPlus
                };
            
                Contents.Add(ChildEntry);
            }
        }

        Entries = Contents;
        return PosixResult.Success;
    }

    public PosixResult Rename (
        in String OldAbsolutePath,
        in String NewAbsolutePath
    ) {
        if (Syscall.lstat(OldAbsolutePath, out Stat InfoStats) == -1) {
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            Log.PrintAsync($"Syscall returned failure '{Error}' for rename from '{OldAbsolutePath}' to '{NewAbsolutePath}'", LogLevel.Warning);
            return ErrorResult;
        }
        
        if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFREG) == true) {
            BaseCache.ConditionalSaveFile(OldAbsolutePath);
            
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            Log.PrintAsync($"Syscall returned failure '{Error}' for rename from '{OldAbsolutePath}' to '{NewAbsolutePath}'", LogLevel.Warning);
            return ErrorResult;
        }
        else if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFDIR) == true) {
            BaseCache.ConditionalSaveFolder(OldAbsolutePath);
            
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            Log.PrintAsync($"Syscall returned failure '{Error}' for rename from '{OldAbsolutePath}' to '{NewAbsolutePath}'", LogLevel.Warning);
            return ErrorResult;
        }
        else {
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            Log.PrintAsync($"Syscall returned failure '{Error}' for rename from '{OldAbsolutePath}' to '{NewAbsolutePath}'", LogLevel.Warning);
            return ErrorResult;
        }
    }

    public PosixResult FilesystemStatus (
        out FuseVfsStat Stats
    ) {
        Int64 MaximumStorage = BaseController["MaximumStorage", 0];
        Stats = new FuseVfsStat {
            f_bsize   = 1,
            f_frsize  = 1,
            f_blocks  = (UInt64)(MaximumStorage),
            f_bfree   = (UInt64)(MaximumStorage - Math.Min(BaseCurrentStorage, MaximumStorage)),
            f_bavail  = (UInt64)(MaximumStorage - Math.Min(BaseCurrentStorage, MaximumStorage)),
            f_files   = 99999999,
            f_ffree   = 99999999,
            f_favail  = 99999999,
            f_flag    = 0,
            f_namemax = 255,
        };
        
        return PosixResult.Success;
    }

    public void Flush () {
        BaseCache.ConditionalSaveAll(); 
        BaseController["Closed", 0] = 1;
        BaseController.Dispose();
    }
    
    private PosixResult GetAttributesCore (
        in  String       AbsolutePath,
        out FuseFileStat Attributes
    ) {
        if (Syscall.lstat(AbsolutePath, out Stat InfoStats) != -1) {
            Attributes = new FuseFileStat() {
                st_atim  = new TimeSpec(InfoStats.st_atim.tv_sec * 1000),
                st_mtim  = new TimeSpec(InfoStats.st_mtim.tv_sec * 1000),
                st_ctim  = new TimeSpec(InfoStats.st_ctim.tv_sec * 1000),
                st_size  = InfoStats.st_size,
                st_mode  = (PosixFileMode)InfoStats.st_mode,
                st_gid   = InfoStats.st_gid,
                st_uid   = InfoStats.st_uid,
                st_nlink = (Int64)InfoStats.st_nlink,
                st_rdev  = (Int64)InfoStats.st_rdev,
                st_dev   = (Int64)InfoStats.st_dev
            };

            if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFREG) == true) {
                BaseCache.GetAttributes(AbsolutePath, Attributes, out Attributes);
            }
            return PosixResult.Success;
        }
        Attributes = new FuseFileStat();
            
        Errno       Error       = Stdlib.GetLastError();
        Int64       ErrorInt64  = (Int64)Error;
        PosixResult ErrorResult = (PosixResult)ErrorInt64;
        Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
        return ErrorResult;
    }
}