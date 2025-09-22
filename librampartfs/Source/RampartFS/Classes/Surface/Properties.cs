using System.Numerics;
using System.Text;

namespace RampartFS;

internal partial class Surface<T> : IDisposable, IEquatable<T> where T : IParsable<T>, IEquatable<T>, INumber<T> {
    public T Value {
        get {
            lock (BaseWatcher) {
                return BaseCachedValue;
            }
        }
        set {
            lock (BaseWatcher) {
                BaseCachedValue = value;
                T TempValue = value;
            
                ReadOnlySpan<Char> LocalValue = TempValue.ToString().AsSpan();
                Span<Byte>         LocalRaw   = stackalloc Byte[LocalValue.Length * 2];
  
                Int32              RawLength        = Encoding.UTF8.GetBytes(LocalValue, LocalRaw);
                Span<Byte>         RawSlice         = LocalRaw[..RawLength];
                ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)RawSlice;


                FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Write);
                Stream.Write(ReadOnlyBaseRawSlice); Stream.Flush(); Stream.Close(); Stream.Dispose();
            }
        }
    }
}