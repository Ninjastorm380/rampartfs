using System.Runtime.CompilerServices;
using Lightning.Diagnostics.Logging;
using Mono.Unix.Native;

namespace RampartFS;

public partial class Filesystem {
    private static void SimulateWrite(in Int64 CurrentLength, in Int64 Offset, in Int64 PreviousLength, out Int64 Difference) {
        Int64 Before = PreviousLength;
        Int64 After = Offset + CurrentLength;
        Difference = Math.Max(0, After - Before);
    }

    private static void SimulateTruncate(in Int64 CurrentLength, in Int64 PreviousLength, out Int64 Difference) {
        Int64 Before = PreviousLength;
        Int64 After = CurrentLength;
        Difference = After - Before;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private Boolean GetAbsoluteStoragePath(String RelativePath, out String AbsoluteStoragePath) {
        ReadOnlySpan<Char> TempBase = BaseStorageFolderPath.AsSpan();
        ReadOnlySpan<Char> TempUncheckedRel = RelativePath.AsSpan().Trim(Path.DirectorySeparatorChar);
        Span<Char> TempScratch = stackalloc Char[RelativePath.Length + BaseStorageFolderPath.Length + 3];
        Int32 TempScratchLength = SpanHelpers.GetFullPath(TempUncheckedRel, TempBase, TempScratch);

        AbsoluteStoragePath = new String(TempScratch[..TempScratchLength]);

        return AbsoluteStoragePath.StartsWith(BaseStorageFolderPath);
    }

    public Filesystem(String StorageFolderPath, String ControlFolderPath, String LogFolderPath, String MountFolderPath, Int64 StorageMaximum, Boolean LogToConsole, Boolean LogToDisk, LogLevel Verbosity, Boolean KernelCaching) {
        BaseKernelCaching = KernelCaching;

        BaseStorageFolderPath = StorageFolderPath;
        BaseControlFolderPath = ControlFolderPath;
        BaseLogFolderPath = LogFolderPath;
        BaseMountFolderPath = MountFolderPath;

        BaseDefaultMounted = 0;
        BaseDefaultStorageMaximum = StorageMaximum;

        BaseMounted = new Surface<Int64>($"{ControlFolderPath}{Path.DirectorySeparatorChar}Mounted", BaseDefaultMounted);
        BaseStorageMaximum = new Surface<Int64>($"{ControlFolderPath}{Path.DirectorySeparatorChar}StorageMaximum", BaseDefaultStorageMaximum);
        BaseStorageCurrent = new Surface<Int64>($"{ControlFolderPath}{Path.DirectorySeparatorChar}StorageCurrent", 0);
        
        Log.Level = Verbosity;

        if (LogToConsole == true) {
            Log.AddLogTarget(new ConsoleLogTarget());
        }

        if (LogToDisk == true) {
            Log.AddLogTarget(new FileLogTarget($"{BaseLogFolderPath}{Path.DirectorySeparatorChar}rampartfs.log"));
        }
    }

    public void Init() {
        foreach (String ChildAbsolutePath in Directory.GetFiles(BaseStorageFolderPath, "*", SearchOption.AllDirectories)) {
            if (Interop.GetPathAttributes(ChildAbsolutePath, out Stat Attributes) == 0) {
                if (Attributes.st_mode.HasFlag(FilePermissions.S_IFREG) == true) {
                    BaseStorageCurrent.Value += Attributes.st_size;
                }
            }
        }

        BaseMounted.Value = 1;
    }

    public Errno CreateDirectory(String FusePath, FilePermissions Permissions) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.CreateDirectory(AbsoluteStoragePath, Permissions);
    }

    public Errno ReadDirectory(String FusePath, List<FolderChildEntry> Paths) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        Paths.Clear();
        foreach (String ChildAbsolutePath in Directory.GetFileSystemEntries(AbsoluteStoragePath)) {
            if (Interop.GetPathAttributes(ChildAbsolutePath, out Stat Attributes) == 0) {
                FolderChildEntry PathsEntry = new FolderChildEntry(Path.GetFileName(ChildAbsolutePath), Attributes);
                Paths.Add(PathsEntry);
            }
        }

        return 0;
    }

    public Errno RemoveDirectory(String FusePath) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }
        
        foreach (String ChildAbsolutePath in Directory.GetFiles(AbsoluteStoragePath, "*", SearchOption.AllDirectories)) {
            if (Interop.GetPathAttributes(ChildAbsolutePath, out Stat Attributes) == 0) {
                if (Attributes.st_mode.HasFlag(FilePermissions.S_IFREG) == true) {
                    BaseStorageCurrent.Value -= Attributes.st_size;
                }
            }
        }

        return Interop.RemoveDirectory(AbsoluteStoragePath);
    }

    public Errno CreateHandle(String FusePath, OpenFlags Flags, FilePermissions Permissions, out Int32 Handle) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            Handle = -1;
            return Errno.EACCES;
        }
        
        return Interop.CreateHandle(AbsoluteStoragePath, Flags | OpenFlags.O_DIRECT, Permissions, out Handle);
    }

    public Errno OpenHandle(String FusePath, OpenFlags Flags, out Int32 Handle) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            Handle = -1;
            return Errno.EACCES;
        }

        return Interop.OpenHandle(AbsoluteStoragePath, Flags | OpenFlags.O_DIRECT, out Handle);
    }

    public Errno ReadHandle(String FusePath, Int32 Handle, Byte[] ReadBuffer, Int64 ReadOffset, out Int64 BytesRead) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            BytesRead = 0;
            return Errno.EACCES;
        }
        
        return Interop.ReadHandle(AbsoluteStoragePath, Handle, ReadOffset, ReadBuffer, out BytesRead);
    }

    public Errno WriteHandle(String FusePath, Int32 Handle, Byte[] WriteBuffer, Int64 WriteOffset, out Int64 BytesWritten) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            BytesWritten = 0;
            return Errno.EACCES;
        }
        
        Errno CheckResult = Interop.GetHandleLengthFast(AbsoluteStoragePath, Handle, out Int64 OldLength);
        
        if (CheckResult != 0) {
            BytesWritten = 0;
            return CheckResult;
        }
        
        SimulateWrite(WriteBuffer.Length, WriteOffset, OldLength, out Int64 StorageDifference);

        if (BaseStorageCurrent.Value + StorageDifference > BaseStorageMaximum.Value) {
            BytesWritten = 0;

            return Errno.ENOSPC;
        }
        
        Errno Result = Interop.WriteHandle(AbsoluteStoragePath, Handle, WriteOffset, WriteBuffer, out BytesWritten);

        if (Result == 0) {
            BaseStorageCurrent.Value += StorageDifference;
        }
        
        return Result;
    }

    public Errno TruncateHandle(String FusePath, Int32 Handle, Int64 Length) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }
        
        Errno CheckResult = Interop.GetHandleLengthFast(AbsoluteStoragePath, Handle, out Int64 OldLength);
        
        if (CheckResult != 0) {
            return CheckResult;
        }
        
        SimulateTruncate(Length, OldLength, out Int64 StorageDifference);
        
        if (BaseStorageCurrent.Value + StorageDifference > BaseStorageMaximum.Value) {
            return Errno.ENOSPC;
        }
        
        Errno Result = Interop.TruncateHandle(AbsoluteStoragePath, Handle, Length);
        
        if (Result == 0) {
            BaseStorageCurrent.Value += StorageDifference;
        }

        return Result;
    }

    public Errno LockHandle(String FusePath, Int32 Handle, FcntlCommand Command, ref Flock Lock) {
        return Interop.LockHandle(Handle, Command, ref Lock);
    }

    public Errno GetHandleAttributes(String FusePath, Int32 Handle, out Stat Attributes) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            Attributes = default;
            return Errno.EACCES;
        }
        return Interop.GetHandleAttributes(AbsoluteStoragePath, Handle, out Attributes);
    }

    public Errno FlushHandle(String FusePath, Int32 Handle) {
        return 0;
    }

    public Errno ReleaseHandle(String FusePath, Int32 Handle) {
        Errno FlushResult = Interop.FlushHandle(Handle);

        if (FlushResult != 0) {
            return FlushResult;
        }
        
        
        return Interop.ReleaseHandle(Handle);
    }

    public Errno RenamePath(String OldFusePath, String NewFusePath) {
        if (GetAbsoluteStoragePath(OldFusePath, out String OldAbsoluteStoragePath) == false | GetAbsoluteStoragePath(NewFusePath, out String NewAbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }
        
        return Interop.RenamePath(OldAbsoluteStoragePath, NewAbsoluteStoragePath);
    }

    public Errno RemoveFile(String FusePath) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }
        
        Errno CheckResult = Interop.GetPathAttributes(AbsoluteStoragePath, out Stat Attributes);
        
        if (CheckResult != 0) {
            return CheckResult;
        }
        
        Errno Result = Interop.RemoveFile(AbsoluteStoragePath);

        if (Result == 0) {
            BaseStorageCurrent.Value -= Attributes.st_size;
        }

        return Result;
    }

    public Errno TruncateFile(String FusePath, Int64 Length) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }
        
        Errno CheckResult = Interop.GetFileLengthFast(AbsoluteStoragePath, out Int64 OldLength);
        
        if (CheckResult != 0) {
            return CheckResult;
        }
        
        SimulateTruncate(Length, OldLength, out Int64 StorageDifference);
        
        if (BaseStorageCurrent.Value + StorageDifference > BaseStorageMaximum.Value) {
            return Errno.ENOSPC;
        }
        
        Errno Result = Interop.TruncateFile(AbsoluteStoragePath, Length);
        
        if (Result == 0) {
            BaseStorageCurrent.Value += StorageDifference;
        }

        return Result;
    }

    public Errno GetPathStatus(String FusePath, out Stat Attributes) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            Attributes = default;
            return Errno.EACCES;
        }

        return Interop.GetPathAttributes(AbsoluteStoragePath, out Attributes);
    }

    public Errno GetPathExtendedAttribute(String FusePath, String AttributeName, Byte[]? AttributeData, out Int64 BytesRead) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            BytesRead = 0;
            return Errno.EACCES;
        }

        return Interop.GetPathExtendedAttribute(AbsoluteStoragePath, AttributeName, AttributeData, out BytesRead);
    }

    public Errno ListPathExtendedAttributes(String FusePath, out String[] Names) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            Names = [];
            return Errno.EACCES;
        }

        return Interop.ListPathExtendedAttributes(AbsoluteStoragePath, out Names);
    }

    public Errno SetPathExtendedAttribute(String FusePath, String AttributeName, Byte[] AttributeData, XattrFlags AttributeFlags) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.SetPathExtendedAttribute(AbsoluteStoragePath, AttributeName, AttributeData, AttributeFlags);
    }

    public Errno RemovePathExtendedAttribute(String FusePath, String AttributeName) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.RemovePathExtendedAttribute(AbsoluteStoragePath, AttributeName);
    }

    public Errno CreateHardLink(String ArbitraryPath, String FusePath) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        if (GetAbsoluteStoragePath(ArbitraryPath, out String AbsoluteStorageArbitraryPath) == false) {
            return Errno.EACCES;
        }

        return Interop.CreateHardLink(AbsoluteStoragePath, AbsoluteStorageArbitraryPath);
    }

    public Errno CreateSymbolicLink(String ArbitraryPath, String FusePath) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.CreateSymbolicLink(AbsoluteStoragePath, ArbitraryPath);
    }

    public Errno ReadSymbolicLink(String FusePath, out String ArbitraryPath) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            ArbitraryPath = String.Empty;
            return Errno.EACCES;
        }

        return Interop.ReadSymbolicLink(AbsoluteStoragePath, out ArbitraryPath);
    }

    public Errno ChangePathPermissions(String FusePath, FilePermissions Permissions) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.ChangePathPermissions(AbsoluteStoragePath, Permissions);
    }

    public Errno AccessPath(String FusePath, AccessModes Mode) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.AccessPath(AbsoluteStoragePath, Mode);
    }

    public Errno ChangePathOwner(String FusePath, UInt32 Owner, UInt32 Group) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.ChangePathOwner(AbsoluteStoragePath, Owner, Group);
    }

    public Errno ChangePathTimes(String FusePath, Timespec AccessedOn, Timespec ModifiedOn) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.ChangePathTimes(AbsoluteStoragePath, AccessedOn, ModifiedOn);
    }

    public Errno CreateSpecialFile(String FusePath, FilePermissions Permissions, UInt64 DeviceDescriptor) {
        if (GetAbsoluteStoragePath(FusePath, out String AbsoluteStoragePath) == false) {
            return Errno.EACCES;
        }

        return Interop.CreateSpecialFile(AbsoluteStoragePath, Permissions, DeviceDescriptor);
    }

    public Errno GetFileSystemStatus(String FusePath, out Statvfs Status) {
        Errno Result = Interop.GetFilesystemStatus(BaseStorageFolderPath, out Status);

        if (Result == 0) {
            Status.f_bsize = 1;
            Status.f_frsize = 1;
            Status.f_blocks = (UInt64)BaseStorageMaximum.Value;
            Status.f_bfree = (UInt64)(BaseStorageMaximum.Value - BaseStorageCurrent);
            Status.f_bavail = Status.f_bfree;
        }

        return Result;
    }

    public void Save() {

    }

    public void Dispose() {
        BaseStorageCurrent.Dispose();
        BaseStorageMaximum.Dispose();

        BaseMounted.Value = 0;

        BaseMounted.Dispose();
        
        Log.Close();
    }
}