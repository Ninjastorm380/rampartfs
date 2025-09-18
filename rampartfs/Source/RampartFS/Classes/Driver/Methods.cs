using System.Diagnostics.CodeAnalysis;
using Mono.Fuse.NETStandard;
using Mono.Unix.Native;

namespace RampartFS;

public partial class Driver : FileSystem {
    public Driver(Filesystem Filesystem) {
        BaseFilesystem = Filesystem;
        
        AllowAccessToOthers = true;
        MultiThreaded = true;
        EnableKernelPermissionChecking = true;
        EnableDirectIO = false;
        
        MountPoint = BaseFilesystem.MountFolderPath;
        EnableKernelCache = BaseFilesystem.KernelCaching;
    }

    public void Init() {
        BaseFilesystem.Init();
    }
    
    protected override Errno OnCreateDirectory(String Path, FilePermissions Mode) {
        Errno Result = BaseFilesystem.CreateDirectory(Path, Mode);

        return Result;
    }
    protected override Errno OnReadDirectory(String Path, OpenedPathInfo PathInfo, [UnscopedRef] out IEnumerable<DirectoryEntry> Paths) {
        List<FolderChildEntry> Entries = [];

        Errno Result = BaseFilesystem.ReadDirectory(Path, Entries);
        
        Paths = Entries.Select(Entry => new DirectoryEntry(Entry.Name) { Stat = Entry.Attributes });
        
        return Result;
    }
    protected override Errno OnRemoveDirectory(String Path) {
        Errno Result = BaseFilesystem.RemoveDirectory(Path);

        return Result;
    }
    protected override Errno OnCreateHandle(String Path, OpenedPathInfo PathInfo, FilePermissions Mode) {
        Errno Result = BaseFilesystem.CreateHandle(Path, PathInfo.OpenFlags, Mode, out Int32 Handle);
        
        if (Result == 0) {
            PathInfo.Handle = Handle;
        }
        
        return Result;
    }
    protected override Errno OnOpenHandle(String Path, OpenedPathInfo PathInfo) {
        Errno Result = BaseFilesystem.OpenHandle(Path, PathInfo.OpenFlags, out Int32 Handle);
        
        if (Result == 0) {
            PathInfo.Handle = Handle;
        }
        
        return Result;
    }
    protected override Errno OnReadHandle(String Path, OpenedPathInfo PathInfo, Byte[] ReadBuffer, Int64 ReadOffset, [UnscopedRef] out Int32 BytesRead) {
        Errno Result = BaseFilesystem.ReadHandle(Path, (Int32)PathInfo.Handle, ReadBuffer, ReadOffset, out Int64 Amount);
        BytesRead = (Int32)Amount;
        return Result;
    }
    protected override Errno OnWriteHandle(String Path, OpenedPathInfo PathInfo, Byte[] WriteBuffer, Int64 WriteOffset, [UnscopedRef] out Int32 BytesWritten) {
        Errno Result = BaseFilesystem.WriteHandle(Path, (Int32)PathInfo.Handle, WriteBuffer, WriteOffset, out Int64 Amount);
        BytesWritten = (Int32)Amount;
        return Result;
    }
    protected override Errno OnTruncateHandle(String Path, OpenedPathInfo PathInfo, Int64 Length) {
        Errno Result = BaseFilesystem.TruncateHandle(Path, (Int32)PathInfo.Handle, Length);

        return Result;
    }
    protected override Errno OnLockHandle(String Path, OpenedPathInfo PathInfo, FcntlCommand Command, ref Flock Lock) {
        Errno Result = BaseFilesystem.LockHandle(Path, (Int32)PathInfo.Handle, Command, ref Lock);

        return Result;
    }
    protected override Errno OnGetHandleStatus(String Path, OpenedPathInfo PathInfo, [UnscopedRef] out Stat Attributes) {
        Errno Result = BaseFilesystem.GetHandleAttributes(Path, (Int32)PathInfo.Handle, out Attributes);

        return Result;
    }
    protected override Errno OnFlushHandle(String Path, OpenedPathInfo PathInfo) {
        Errno Result = BaseFilesystem.FlushHandle(Path, (Int32)PathInfo.Handle);

        return Result;
    }
    protected override Errno OnReleaseHandle(String Path, OpenedPathInfo PathInfo) {
        Errno Result = BaseFilesystem.ReleaseHandle(Path, (Int32)PathInfo.Handle);

        if (Result == 0) {
            PathInfo.Handle = -1;
        }
        
        return Result;
    }
    protected override Errno OnRenamePath(String OldPath, String NewPath) {
        Errno Result = BaseFilesystem.RenamePath(OldPath, NewPath);
        
        return Result;
    }
    protected override Errno OnRemoveFile(String Path) {
        Errno Result = BaseFilesystem.RemoveFile(Path);
        
        return Result;
    }
    protected override Errno OnTruncateFile(String Path, Int64 Length) {
        Errno Result = BaseFilesystem.TruncateFile(Path, Length);
        
        return Result;
    }
    protected override Errno OnGetPathStatus(String Path, [UnscopedRef] out Stat Attributes) {
        Errno Result = BaseFilesystem.GetPathStatus(Path, out Attributes);
        
        return Result;
    }
    protected override Errno OnGetPathExtendedAttribute(String Path, String AttributeName , Byte[]? AttributeData, [UnscopedRef] out Int32 BytesRead) {
        Errno Result = BaseFilesystem.GetPathExtendedAttribute(Path, AttributeName, AttributeData, out Int64 Amount);
        BytesRead = (Int32)Amount;
        return Result;
    }
    protected override Errno OnListPathExtendedAttributes(String Path, [UnscopedRef] out String[] Names) {
        Errno Result = BaseFilesystem.ListPathExtendedAttributes(Path, out Names);
        
        return Result;
    }
    protected override Errno OnSetPathExtendedAttribute(String Path, String AttributeName, Byte[] AttributeData, XattrFlags AttributeFlags) {
        Errno Result = BaseFilesystem.SetPathExtendedAttribute(Path, AttributeName, AttributeData, AttributeFlags);
        
        return Result;
    }
    protected override Errno OnRemovePathExtendedAttribute(String Path, String AttributeName) {
        Errno Result = BaseFilesystem.RemovePathExtendedAttribute(Path, AttributeName);
        
        return Result;
    }
    protected override Errno OnCreateHardLink(String RelativeArbitraryPath, String Path) {
        Errno Result = BaseFilesystem.CreateHardLink(RelativeArbitraryPath, Path);
        
        return Result;
    }
    protected override Errno OnCreateSymbolicLink(String ArbitraryPath, String Path) {
        Errno Result = BaseFilesystem.CreateSymbolicLink(ArbitraryPath, Path);
        
        return Result;
    }
    protected override Errno OnReadSymbolicLink(String Path, [UnscopedRef] out String ArbitraryPath) {
        Errno Result = BaseFilesystem.ReadSymbolicLink(Path, out ArbitraryPath);
        
        return Result;
    }
    protected override Errno OnChangePathPermissions(String Path, FilePermissions Mode) {
        Errno Result = BaseFilesystem.ChangePathPermissions(Path, Mode);
        
        return Result;
    }
    protected override Errno OnAccessPath(String Path, AccessModes Mode) {
        Errno Result = BaseFilesystem.AccessPath(Path, Mode);
        
        return Result;
    }
    protected override Errno OnChangePathOwner(String Path, Int64 Owner, Int64 Group) {
        Errno Result = BaseFilesystem.ChangePathOwner(Path, (UInt32)Owner, (UInt32)Group);
        
        return Result;
    }
    protected override Errno OnChangePathTimes(String Path, ref Utimbuf TimeBuffer) {
        Errno Result = BaseFilesystem.ChangePathTimes(Path, new Timespec { tv_sec = TimeBuffer.actime, tv_nsec = 0 }, new Timespec { tv_sec = TimeBuffer.modtime, tv_nsec = 0 });
        
        return Result;
    }
    protected override Errno OnCreateSpecialFile(String Path, FilePermissions Permissions, UInt64 DeviceDescriptor) {
        Errno Result = BaseFilesystem.CreateSpecialFile(Path, Permissions, DeviceDescriptor);
        
        return Result;
    }
    protected override Errno OnGetFileSystemStatus(String Path, [UnscopedRef] out Statvfs Status) {
        Errno Result = BaseFilesystem.GetFileSystemStatus(Path, out Status);
        
        return Result;
    }
    
    protected override void Dispose(Boolean Disposing) {
        BaseFilesystem.Save();
        BaseFilesystem.Dispose();
        base.Dispose(Disposing);
    }
}

