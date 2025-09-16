using System.Buffers;
using Lightning.Diagnostics.Logging;
using Mono.Unix;
using Mono.Unix.Native;

namespace RampartFS;

public partial class Interop {
    static Interop() {
        BaseMetadataCache = new MetadataCache();
    }
    
    public static Errno CreateDirectory(String AbsolutePath, FilePermissions Permissions) {
        if (Syscall.mkdir(AbsolutePath, Permissions) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno RemoveDirectory(String AbsolutePath) {
        if (Syscall.rmdir(AbsolutePath) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno CreateHandle(String AbsolutePath, OpenFlags Flags, FilePermissions Permissions, out Int32 Handle) {
        
        Handle = Syscall.open(AbsolutePath, Flags, Permissions);
        
        if (Handle != -1) {
            if (Syscall.fstat(Handle, out Stat Attributes) != -1) {
                if (Syscall.posix_fadvise(Handle, 0, Attributes.st_size, PosixFadviseAdvice.POSIX_FADV_RANDOM) != -1) {
                    return 0;
                }
            }
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }

    public static Errno OpenHandle(String AbsolutePath, OpenFlags Flags, out Int32 Handle) {
        
        Handle = Syscall.open(AbsolutePath, Flags);

        if (Handle != -1) {
            if (Syscall.fstat(Handle, out Stat Attributes) != -1) {
                if (Syscall.posix_fadvise(Handle, 0, Attributes.st_size, PosixFadviseAdvice.POSIX_FADV_RANDOM) != -1) {
                    return 0;
                }
            }
        }

        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }

    public static Errno ReadHandle(String AbsolutePath, Int32 Handle, Int64 Offset, Byte[] Data, out Int64 Amount) {
        Int64 TempAmount;
        unsafe {
            fixed (void* DataPointer = Data) {
                TempAmount = LibC.pread(Handle, DataPointer, (UInt64)Data.LongLength, Offset);
            }
        }

        if (TempAmount != -1) {
            Amount = TempAmount;

            return 0;
        }

        Amount = 0;
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }

    public static Errno WriteHandle(String AbsolutePath, Int32 Handle, Int64 Offset, Byte[] Data, out Int64 Amount) {
        Int64 TempAmount;
        unsafe {
            fixed (void* DataPointer = Data) {
                TempAmount = LibC.pwrite(Handle, DataPointer, (UInt64)Data.LongLength, Offset);
            }
        }
        
        if (TempAmount != -1) {
            Amount = TempAmount;
            BaseMetadataCache.SetLength(AbsolutePath, Handle, Offset, Amount);
            return 0;
        }

        Amount = 0;
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    
    public static Errno TruncateHandle(String AbsolutePath, Int32 Handle, Int64 Length) {
        if (Syscall.ftruncate(Handle, Length) != -1) {
            BaseMetadataCache.SetLength(AbsolutePath, Handle, Length);
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    
    public static Errno LockHandle(Int32 Handle, FcntlCommand Command, ref Flock FileLock) {
        if (Syscall.fcntl(Handle, Command, ref FileLock) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}'", LogLevel.Warning);
        return Result;
    }
    public static Errno GetHandleAttributes(String AbsolutePath, Int32 Handle, out Stat Attributes) {
        if (Syscall.fstat(Handle, out Attributes) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    
    public static Errno GetHandleLengthFast(String AbsolutePath, Int32 Handle, out Int64 Length) {
        Errno Result = BaseMetadataCache.GetLength(AbsolutePath, Handle, out Length);
        if (Result != 0) {
            Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        }
        return Result;
    }

    public static Errno FlushHandle(Int32 Handle) {
        if (Syscall.fsync(Handle) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}'", LogLevel.Warning);
        return Result;
    }
    public static Errno ReleaseHandle(Int32 Handle) {
        if (Syscall.close(Handle) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}'", LogLevel.Warning);
        return Result;
    }
    public static Errno RenamePath(String OldAbsolutePath, String NewAbsolutePath) {
        if (Stdlib.rename(OldAbsolutePath, NewAbsolutePath) != -1) {
            return BaseMetadataCache.Drop(OldAbsolutePath, NewAbsolutePath);
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}'", LogLevel.Warning);
        return Result;
    }
    public static Errno RemoveFile(String AbsolutePath) {
        if (Syscall.unlink(AbsolutePath) != -1) {
            BaseMetadataCache.Drop(AbsolutePath);
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno TruncateFile(String AbsolutePath, Int64 Length) {
        if (Syscall.truncate(AbsolutePath, Length) != -1) {
            BaseMetadataCache.SetLength(AbsolutePath, Length);
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno GetPathAttributes(String AbsolutePath, out Stat Attributes) {
        if (Syscall.lstat(AbsolutePath, out Attributes) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Info);
        return Result;
    }
    
    public static Errno GetFileLengthFast(String AbsolutePath, out Int64 Length) {
        Errno Result = BaseMetadataCache.GetLength(AbsolutePath, out Length);
        if (Result != 0) {
            Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        }
        return Result;
    }
    
    public static Errno GetPathExtendedAttribute(String AbsolutePath, String AttributeName , Byte[]? AttributeData, out Int64 AttributeLength) {
        UInt64 RequestedLength;
        
        if (AttributeData != null) {
            RequestedLength = (UInt64)AttributeData.LongLength;
        }
        else {
            RequestedLength = 0;
            AttributeData = [];
        }
        
        AttributeLength = Syscall.lgetxattr(AbsolutePath, AttributeName, AttributeData, RequestedLength);
        
        if (AttributeLength != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();   
        return Result;
    }
    public static Errno ListPathExtendedAttributes(String AbsolutePath, out String[] AttributeNames) {
        if (Syscall.llistxattr(AbsolutePath, out AttributeNames) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno SetPathExtendedAttribute(String AbsolutePath, String AttributeName, Byte[] AttributeData, XattrFlags AttributeFlags) {
        if (Syscall.lsetxattr(AbsolutePath, AttributeName, AttributeData, AttributeFlags)!= -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno RemovePathExtendedAttribute(String AbsolutePath, String AttributeName) {
        if (Syscall.lremovexattr(AbsolutePath, AttributeName)!= -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno CreateHardLink(String AbsolutePath, String ArbitraryPath) {
        if (Syscall.link(ArbitraryPath, AbsolutePath)!= -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno CreateSymbolicLink(String AbsolutePath, String ArbitraryPath) {
        if (Syscall.symlink(ArbitraryPath, AbsolutePath)!= -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno ReadSymbolicLink(String AbsolutePath, out String ArbitraryPath) {
        Byte[] Scratch = ArrayPool<Byte>.Shared.Rent(4096);
        
        Int64 ScratchLength = Syscall.readlink(AbsolutePath, Scratch);
        
        if (ScratchLength != -1) {
            ArbitraryPath = System.Text.Encoding.UTF8.GetString(Scratch, 0, (Int32)ScratchLength);
            ArrayPool<Byte>.Shared.Return(Scratch);
            return 0;
        }
        
        ArbitraryPath = String.Empty;
        Errno Result = Stdlib.GetLastError();
        ArrayPool<Byte>.Shared.Return(Scratch);
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno ChangePathPermissions(String AbsolutePath, FilePermissions Permissions) {
        if (Syscall.fchmodat(Syscall.AT_FDCWD, AbsolutePath, Permissions, AtFlags.AT_SYMLINK_NOFOLLOW) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno AccessPath(String AbsolutePath, AccessModes Mode) {
        UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);

        if (Mode.HasFlag(AccessModes.F_OK) == true && Info.CanAccess(AccessModes.F_OK) == false) {
            
            return Errno.ENOENT;
        }
        
        if (Mode.HasFlag(AccessModes.R_OK) == true && Info.CanAccess(AccessModes.R_OK) == false) {
            
            return Errno.EACCES;
        }
        
        if (Mode.HasFlag(AccessModes.W_OK) == true && Info.CanAccess(AccessModes.W_OK) == false) {
            
            return Errno.EACCES;
        }
        
        if (Mode.HasFlag(AccessModes.X_OK) == true && Info.CanAccess(AccessModes.X_OK) == false) { 
            
            return Errno.EACCES;
        }
        
        return 0;
    }
    public static Errno ChangePathOwner(String AbsolutePath, UInt32 Owner, UInt32 Group) {
        if (Syscall.fchownat(Syscall.AT_FDCWD, AbsolutePath, Owner, Group, AtFlags.AT_SYMLINK_NOFOLLOW) != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    
    public static Errno ChangePathTimes(String AbsolutePath, Timespec AccessedOn, Timespec ModifiedOn) {
        unsafe {
            Timespec* Times = stackalloc Timespec[2];
        
            Times[0] = AccessedOn;
            Times[1] = ModifiedOn;
            
            if (LibC.utimensat(Syscall.AT_FDCWD, AbsolutePath, Times, (Int32)AtFlags.AT_SYMLINK_NOFOLLOW) != -1) {
                return 0;
            }
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno CreateSpecialFile(String AbsolutePath, FilePermissions Permissions, UInt64 Handle) {
        if (Syscall.mknod(AbsolutePath, Permissions, Handle)  != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }
    public static Errno GetFilesystemStatus(String AbsolutePath, out Statvfs Status) {
        if (Syscall.statvfs(AbsolutePath, out Status)  != -1) {
            return 0;
        }
        
        Errno Result = Stdlib.GetLastError();
        Log.PrintAsync<Interop>($"Result: '{ToPrintable(Result)}', Path: '{AbsolutePath}'", LogLevel.Warning);
        return Result;
    }

    private static String ToPrintable(Errno Result) {
        String Printable = Result == 0 ? "Success" : Result.ToString();

        return Printable;
    }
}