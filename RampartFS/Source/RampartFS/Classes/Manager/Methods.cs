using System.Text;
using FuseDotNet;
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
        BaseController     = new Controller<Int64>(ControlPath);

        BaseController["MaximumStorage", MaximumStorage] = MaximumStorage;
        BaseController["MaximumCache", MaximumCache]     = MaximumCache;
        BaseController["TrimTarget", TrimTarget]         = TrimTarget;
        BaseController.LoadRaw("CachedEntries");
        
        
        BaseCache          = new Cache(RootPath, BaseController);
        BaseCurrentStorage = 0;

        foreach (String AbsoluteFilePath in Directory.GetFiles(RootPath, "*", SearchOption.AllDirectories)) {
            UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsoluteFilePath);
            BaseCurrentStorage = BaseCurrentStorage + Info.Length;
        }
    }
    
    public PosixResult ReadFile (
        in  String     AbsolutePath,
        in  Span<Byte> Buffer,
        in  Int64      Offset,
        out Int32      BytesRead
    ) {
        if (File.Exists(AbsolutePath) == false) {
            BytesRead = 0;
            return PosixResult.ENOENT;
        }
        
        PosixResult Result = BaseCache.ReadFile(AbsolutePath, Buffer, Offset, out BytesRead);
        return Result;
    }
    
    public PosixResult WriteFile (
        in  String             AbsolutePath,
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        out Int32              BytesWritten
    ) {
        PosixResult Result = BaseCache.WriteFile(AbsolutePath, Buffer, Offset, out BytesWritten, out Int64 Difference, BaseCurrentStorage, BaseController["MaximumStorage", 0]);
        Interlocked.Add(ref BaseCurrentStorage, Difference);
        return Result;
    }
    
    public PosixResult TruncateFile (
        in String AbsolutePath,
        in Int64  Length
    ) {
        PosixResult Result = BaseCache.TruncateFile(AbsolutePath, Length, out Int64 Difference, BaseCurrentStorage, BaseController["MaximumStorage", 0]);
        Interlocked.Add(ref BaseCurrentStorage, Difference);
        return Result;
    }
    
    public PosixResult Unlink (
        in String AbsolutePath
    ) {
        UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);
        if (Info.Exists == false) {
            return PosixResult.ENOENT;
        }
        
        if (Info.FileType == FileTypes.RegularFile) {
            BaseCache.ConditionalDrop(AbsolutePath, out Int64 Difference);
            Interlocked.Add(ref BaseCurrentStorage, Difference);
        
            if (Syscall.unlink(AbsolutePath) == 0) return PosixResult.Success;
            Errno Error      = Stdlib.GetLastError();
            Int64 ErrorInt64 = (Int64)Error;
            return (PosixResult)ErrorInt64;
        }
        else {
            if (Syscall.unlink(AbsolutePath) == 0) return PosixResult.Success;
            Errno Error      = Stdlib.GetLastError();
            Int64 ErrorInt64 = (Int64)Error;
            return (PosixResult)ErrorInt64;
        }
    }

    public PosixResult FolderExists (
        in String AbsolutePath
    ) {
        IntPtr Descriptor = Syscall.opendir(AbsolutePath);
        if (Descriptor > 0) {
            if (Syscall.closedir(Descriptor) == 0) {
                return PosixResult.Success;
            }
        }
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult FileExists (
        in String AbsolutePath
    ) {
        Int32 Descriptor = Syscall.open(AbsolutePath, OpenFlags.O_RDONLY);
        if (Descriptor > 0) {
            if (Syscall.close(Descriptor) == 0) {
                return PosixResult.Success;
            }
        }
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
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
        if (Syscall.symlink(ArbitraryPath, AbsolutePath) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult ReadLink (
        in String     AbsolutePath,
        in Span<Byte> ArbitraryPathData
    ) {
        StringBuilder Builder = new StringBuilder(ArbitraryPathData.Length);

        if (Syscall.readlink(AbsolutePath, Builder) != -1) {
            String ArbitraryPath = Builder.ToString();
            Encoding.UTF8.GetBytes(ArbitraryPath.AsSpan(), ArbitraryPathData);
            return PosixResult.Success;
        }
        
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
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
        
        if (Syscall.utimensat(AT_FDCWD, AbsolutePath, Times, 0) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult CreateFolder (
        in String        AbsolutePath,
        in PosixFileMode Mode
    ) {
        if (Syscall.mkdir(AbsolutePath, (FilePermissions)Mode) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult DeleteFolder (
        in String AbsolutePath
    ) {
        if (Syscall.rmdir(AbsolutePath) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult CreateFile (
        in String     AbsolutePath,
        PosixFileMode Mode
    ) {
        Int32 Descriptor = Syscall.open(AbsolutePath, OpenFlags.O_CREAT | OpenFlags.O_WRONLY | OpenFlags.O_TRUNC, (FilePermissions)Mode);
        if (Descriptor > 0) {
            if (Syscall.close(Descriptor) == 0) {
                return PosixResult.Success;
            }
        }
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult ChangeMode (
        in String        AbsolutePath,
        in PosixFileMode Mode
    ) {
        if (Syscall.chmod(AbsolutePath, (FilePermissions)Mode) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult ChangeOwners (
        in String AbsolutePath,
        in Int32  UserID,
        in Int32  GroupID
    ) {
        if (Syscall.chown(AbsolutePath, UserID, GroupID) == 0) return PosixResult.Success;
        Errno Error      = Stdlib.GetLastError();
        Int64 ErrorInt64 = (Int64)Error;
        return (PosixResult)ErrorInt64;
    }
    
    public PosixResult CheckPermissions (
        in String          AbsolutePath,
        in PosixAccessMode Mode
    ) {
        UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);

        if (Mode.HasFlag(PosixAccessMode.Exists) == true & Info.CanAccess(AccessModes.F_OK) == false) {
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Read) == true & Info.CanAccess(AccessModes.R_OK) == false) {
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Write) == true & Info.CanAccess(AccessModes.W_OK) == false) {
            return PosixResult.EACCES;
        }
        
        if (Mode.HasFlag(PosixAccessMode.Execute) == true & Info.CanAccess(AccessModes.X_OK) == false) {
            return PosixResult.EACCES;
        }
        
        return PosixResult.Success;
    }
    
    public PosixResult GetFolderContents (
        in  String                    AbsolutePath,
        out IEnumerable<FuseDirEntry> Entries
    ) {
        if (Directory.Exists(AbsolutePath) == false) {
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
            return PosixResult.ENOENT;
        }
        
        if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFREG) == true) {
            BaseCache.ConditionalSaveFile(OldAbsolutePath);
            
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno Error      = Stdlib.GetLastError();
            Int64 ErrorInt64 = (Int64)Error;
            return (PosixResult)ErrorInt64;
        }
        else if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFDIR) == true) {
            BaseCache.ConditionalSaveFolder(OldAbsolutePath);
            
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno Error      = Stdlib.GetLastError();
            Int64 ErrorInt64 = (Int64)Error;
            return (PosixResult)ErrorInt64;
        }
        else {
            if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) == 0) return PosixResult.Success;
            Errno Error      = Stdlib.GetLastError();
            Int64 ErrorInt64 = (Int64)Error;
            return (PosixResult)ErrorInt64;
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
        BaseController.Dispose();
    }
    
    private PosixResult GetAttributesCore (
        in  String       AbsolutePath,
        out FuseFileStat Attributes
    ) {
        if (Syscall.lstat(AbsolutePath, out Stat InfoStats) == -1) {
            Attributes = new FuseFileStat();
            return PosixResult.ENOENT;
        }

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
            st_dev   = (Int64)InfoStats.st_dev,
        };

        if (IsFileType(InfoStats.st_mode, FilePermissions.S_IFREG) == true) {
            BaseCache.GetAttributes(AbsolutePath, Attributes, out Attributes);
        }
        return PosixResult.Success;
    }
}