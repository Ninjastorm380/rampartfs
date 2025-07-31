using System.Text;

namespace RampartFS;

internal partial class ControllerSurface<T> : IDisposable where T : IParsable<T> {
    public T? Value {
        get {
            if (BaseCachedValue != null) {
                return BaseCachedValue;
            }
            return BaseDefaults;
        }
        set {
            T? TempValue = value;
            
            if (TempValue == null) {
                TempValue = BaseDefaults;
            }
            
            if (TempValue == null) {
                return;
            }
            
            Span<Byte>         BaseRaw              = stackalloc Byte[524287];
            ReadOnlySpan<Char> BaseValue            = TempValue.ToString().AsSpan();
            Int32              BaseRawLength        = Encoding.UTF8.GetBytes(BaseValue, BaseRaw);
            Span<Byte>         BaseRawSlice         = BaseRaw[..BaseRawLength];
            ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)BaseRawSlice;

            FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Write);
            Stream.Write(ReadOnlyBaseRawSlice); Stream.Flush(); Stream.Close(); Stream.Dispose();
        }
    }
    
    public String Raw {
        get {
            return BaseCachedValueRaw;
        }
        set {
            Span<Byte>         BaseRaw              = stackalloc Byte[524287];
            ReadOnlySpan<Char> BaseValue            = value.AsSpan();
            Int32              BaseRawLength        = Encoding.UTF8.GetBytes(BaseValue, BaseRaw);
            Span<Byte>         BaseRawSlice         = BaseRaw[..BaseRawLength];
            ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)BaseRawSlice;

            FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Write);
            Stream.Write(ReadOnlyBaseRawSlice); Stream.Flush(); Stream.Close(); Stream.Dispose();
        }
    }
}