using FuseDotNet;
using LTRData.Extensions.Native.Memory;
using Mono.Unix;

namespace RampartFS;

internal partial class Driver : IFuseOperations {
    public Driver (
        String RootPath,
        String ControlPath,
        Int64 MaximumStorage,
        Int64 MaximumCache,
        Int64 MinimumTrim
    ) {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        
        BaseManager   = new Manager(RootPath, ControlPath, MaximumStorage, MaximumCache, MinimumTrim);
        BasePathCache = new PathCache(RootPath);
    }
    
    private void CurrentDomainOnProcessExit (
        Object?   Sender,
        EventArgs Args
    ) {
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomainOnProcessExit;
        BaseManager.Flush();
    }
    
    private static String GetStringFromRemoteMemory (
        in ReadOnlyNativeMemory<Byte> MemoryTarget
    ) {
        return System.Text.Encoding.UTF8.GetString(MemoryTarget.Span);
    }
    
    void IDisposable.Dispose () {
        
    }

    PosixResult IFuseOperations.OpenDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.FolderExists(AbsolutePath);
    }

    PosixResult IFuseOperations.GetAttr (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        out FuseFileStat     stat,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.GetAttributes(AbsolutePath, out stat);
    }

    PosixResult IFuseOperations.Read (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        NativeMemory<Byte> buffer,
        Int64                position,
        out Int32            readLength,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.ReadFile(AbsolutePath, buffer.Span, position, out readLength);
    }

    PosixResult IFuseOperations.ReadDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        out IEnumerable<FuseDirEntry> entries,
        ref FuseFileInfo     fileInfo,
        Int64                offset,
        FuseReadDirFlags     flags
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.GetFolderContents(AbsolutePath, out entries);
    }

    PosixResult IFuseOperations.Open (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.FileExists(AbsolutePath);
    }

    void IFuseOperations.Init (
        ref FuseConnInfo fuse_conn_info
    ) {
        
    }

    PosixResult IFuseOperations.Access (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        PosixAccessMode      mask
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.CheckPermissions(AbsolutePath, mask);
    }

    PosixResult IFuseOperations.StatFs (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        out FuseVfsStat      statvfs
    ) {
        return BaseManager.FilesystemStatus(out statvfs);
    }

    PosixResult IFuseOperations.FSyncDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        Boolean              datasync,
        ref FuseFileInfo     fileInfo
    ) {
        return PosixResult.Success;
    }

    PosixResult IFuseOperations.ReadLink (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        NativeMemory<Byte> target
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.ReadLink(AbsolutePath, target.Span);
    }

    PosixResult IFuseOperations.ReleaseDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ref FuseFileInfo     fileInfo
    ) {
        return PosixResult.ENOTSUP;
    }

    PosixResult IFuseOperations.Link (
        ReadOnlyNativeMemory<Byte> from,
        ReadOnlyNativeMemory<Byte> to
    ) {
        String RelativePath  = GetStringFromRemoteMemory(to);
        String ArbitraryPath = GetStringFromRemoteMemory(from);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.SymbolicLink(AbsolutePath, ArbitraryPath);
    }

    PosixResult IFuseOperations.MkDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        PosixFileMode        mode
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.CreateFolder(AbsolutePath, mode);
    }

    PosixResult IFuseOperations.Release (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ref FuseFileInfo     fileInfo
    ) {
        return PosixResult.ENOTSUP;
    }

    PosixResult IFuseOperations.RmDir (
        ReadOnlyNativeMemory<Byte> fileNamePtr
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.DeleteFolder(AbsolutePath);
    }

    PosixResult IFuseOperations.FSync (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        Boolean              datasync,
        ref FuseFileInfo     fileInfo
    ) {
        return PosixResult.Success;
    }

    PosixResult IFuseOperations.Unlink (
        ReadOnlyNativeMemory<Byte> fileNamePtr
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.Unlink(AbsolutePath);
    }

    PosixResult IFuseOperations.Write (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ReadOnlyNativeMemory<Byte> buffer,
        Int64                position,
        out Int32            writtenLength,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.WriteFile(AbsolutePath, buffer.Span, position, out writtenLength);
    }

    PosixResult IFuseOperations.SymLink (
        ReadOnlyNativeMemory<Byte> from,
        ReadOnlyNativeMemory<Byte> to
    ) {
        String RelativePath  = GetStringFromRemoteMemory(to);
        String ArbitraryPath = GetStringFromRemoteMemory(from);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.SymbolicLink(AbsolutePath, ArbitraryPath);
    }

    PosixResult IFuseOperations.Flush (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        ref FuseFileInfo     fileInfo
    ) {
        return PosixResult.Success;
    }

    PosixResult IFuseOperations.Rename (
        ReadOnlyNativeMemory<Byte> from,
        ReadOnlyNativeMemory<Byte> to
    ) {
        String OldRelativePath = GetStringFromRemoteMemory(from);
        String NewRelativePath = GetStringFromRemoteMemory(to);
        BasePathCache.GetAbsolutePath(OldRelativePath, out String OldAbsolutePath);
        BasePathCache.GetAbsolutePath(NewRelativePath, out String NewAbsolutePath);

        return BaseManager.Rename(OldAbsolutePath, NewAbsolutePath);
    }

    PosixResult IFuseOperations.Truncate (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        Int64                size
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.TruncateFile(AbsolutePath, size);
    }

    PosixResult IFuseOperations.UTime (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        TimeSpec             atime,
        TimeSpec             mtime,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.UpdateTime(AbsolutePath, atime, mtime);
    }

    PosixResult IFuseOperations.Create (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        Int32                mode,
        ref FuseFileInfo     fileInfo
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.CreateFile(AbsolutePath, (PosixFileMode)mode);
    }

    PosixResult IFuseOperations.IoCtl (
        ReadOnlyNativeMemory<Byte> fileNamePtr,
        Int32                cmd,
        IntPtr               arg,
        ref FuseFileInfo     fileInfo,
        FuseIoctlFlags       flags,
        IntPtr               data
    ) {
        return PosixResult.ENOTSUP;
    }

    PosixResult IFuseOperations.ChMod (
        NativeMemory<Byte> fileNamePtr,
        PosixFileMode mode
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.ChangeMode(AbsolutePath, mode);
    }

    PosixResult IFuseOperations.ChOwn (
        NativeMemory<Byte> fileNamePtr,
        Int32        uid,
        Int32        gid
    ) {
        String RelativePath = GetStringFromRemoteMemory(fileNamePtr);
        BasePathCache.GetAbsolutePath(RelativePath, out String AbsolutePath);
        return BaseManager.ChangeOwners(AbsolutePath, uid, gid);
    }

    PosixResult IFuseOperations.FAllocate (
        NativeMemory<Byte> fileNamePtr,
        FuseAllocateMode mode,
        Int64            offset,
        Int64            length,
        ref FuseFileInfo fileInfo
    ) {
        return PosixResult.ENOTSUP;
    }
}