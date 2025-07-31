using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace RampartFS;

internal partial class ControllerSurface<T> : IDisposable where T : IParsable<T> {
    public ControllerSurface (
        String AbsolutePath,
        T      Default
    ) {
        BaseCachedValue                 =  Default;
        BaseCachedValueRaw              =  String.Empty;
        BaseAbsolutePath                =  AbsolutePath;
        BaseDefaults                    =  Default;
        BaseWatcher                     =  new FileSystemWatcher(Directory.GetParent(BaseAbsolutePath)!.FullName, Path.GetFileName(BaseAbsolutePath));
        BaseWatcher.NotifyFilter        =  NotifyFilters.LastWrite;
        BaseWatcher.Changed             += BaseWatcherOnChanged;
        BaseWatcher.EnableRaisingEvents =  true;
        
        FileStream         Stream               = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Write | FileShare.Delete);
        Span<Byte>         BaseRaw              = stackalloc Byte[524287];
        ReadOnlySpan<Char> BaseValue            = BaseDefaults.ToString().AsSpan();
        Int32              BaseRawLength        = Encoding.UTF8.GetBytes(BaseValue, BaseRaw);
        Span<Byte>         BaseRawSlice         = BaseRaw[..BaseRawLength];
        ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)BaseRawSlice;
        
        Stream.Write(ReadOnlyBaseRawSlice);
        Stream.Flush();
        Stream.Close();
        Stream.Dispose();
    }
    
    public ControllerSurface (
        String AbsolutePath
    ) {
        BaseAbsolutePath = AbsolutePath;
        
        Span<Byte> BaseRaw   = stackalloc Byte[524287];
        Span<Char> BaseValue = stackalloc Char[524287];
        
        FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read | FileShare.Write);
        Int32 BaseRawLength = Stream.Read(BaseRaw); Stream.Flush(); Stream.Close(); Stream.Dispose();

        Span<Byte>         BaseRawSlice           = BaseRaw[..BaseRawLength];
        ReadOnlySpan<Byte> ReadOnlyBaseRawSlice   = (ReadOnlySpan<Byte>)BaseRawSlice;
        Int32              BaseValueLength        = Encoding.UTF8.GetChars(ReadOnlyBaseRawSlice, BaseValue);
        Span<Char>         BaseValueSlice         = BaseValue[..BaseValueLength].Trim(Environment.NewLine.AsSpan());
        ReadOnlySpan<Char> ReadOnlyBaseValueSlice = (ReadOnlySpan<Char>)BaseValueSlice;

        BaseCachedValueRaw = new String(ReadOnlyBaseValueSlice);
        
        
        
        BaseCachedValue                 =  default;
        BaseDefaults                    =  default;
        BaseWatcher                     =  new FileSystemWatcher(Directory.GetParent(BaseAbsolutePath)!.FullName, Path.GetFileName(BaseAbsolutePath));
        BaseWatcher.NotifyFilter        =  NotifyFilters.LastWrite;
        BaseWatcher.Changed             += BaseWatcherOnChanged;
        BaseWatcher.EnableRaisingEvents =  true;
    }

    private void BaseWatcherOnChanged (
        Object              Sender,
        FileSystemEventArgs Args
    ) {
        Span<Byte> BaseRaw   = stackalloc Byte[524287];
        Span<Char> BaseValue = stackalloc Char[524287];
        
        FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write);
        Int32 BaseRawLength = Stream.Read(BaseRaw); Stream.Flush(); Stream.Close(); Stream.Dispose();

        Span<Byte>         BaseRawSlice           = BaseRaw[..BaseRawLength];
        ReadOnlySpan<Byte> ReadOnlyBaseRawSlice   = (ReadOnlySpan<Byte>)BaseRawSlice;
        Int32              BaseValueLength        = Encoding.UTF8.GetChars(ReadOnlyBaseRawSlice, BaseValue);
        Span<Char>         BaseValueSlice         = BaseValue[..BaseValueLength].Trim(Environment.NewLine.AsSpan());
        ReadOnlySpan<Char> ReadOnlyBaseValueSlice = (ReadOnlySpan<Char>)BaseValueSlice;

        BaseCachedValueRaw = new String(ReadOnlyBaseValueSlice);
        if (T.TryParse(BaseCachedValueRaw, CultureInfo.InvariantCulture, out BaseCachedValue) == false) {
            BaseCachedValue = BaseDefaults;
        }
    }

    public void Dispose () {
        BaseWatcher.Dispose();
    }
}