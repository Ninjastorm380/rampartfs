using System.Globalization;
using System.Numerics;
using System.Text;

namespace RampartFS;

internal partial class Surface<T> : IDisposable, IEquatable<T> where T : IParsable<T>, IEquatable<T>, INumber<T> {
    protected Boolean Equals(Surface<T> Other) {
        return BaseCachedValue.Equals(Other.BaseCachedValue);
    }

    public override Boolean Equals(Object? Obj) {
        if (Obj is null) {
            return false;
        }

        if (ReferenceEquals(this, Obj)) {
            return true;
        }

        if (Obj.GetType() != GetType()) {
            return false;
        }

        return Equals((Surface<T>)Obj);
    }

    public override Int32 GetHashCode() {
        return base.GetHashCode();
    }

    public Surface(
        String AbsolutePath,
        T Default
    ) {
        BaseCachedValue = Default;
        BaseCachedValueRaw = String.Empty;
        BaseAbsolutePath = AbsolutePath;
        BaseDefaults = Default;
        BaseWatcher = new FileSystemWatcher(Directory.GetParent(BaseAbsolutePath)!.FullName, Path.GetFileName(BaseAbsolutePath));
        BaseWatcher.NotifyFilter = NotifyFilters.LastWrite;
        BaseWatcher.Changed += BaseWatcherOnChanged;
        BaseWatcher.EnableRaisingEvents = true;
        
        lock (BaseWatcher) {
            FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Write | FileShare.Delete);
            Span<Byte> Raw = stackalloc Byte[524287];
            ReadOnlySpan<Char> Chars = BaseDefaults.ToString().AsSpan();
            Int32 RawLength = Encoding.UTF8.GetBytes(Chars, Raw);
            Span<Byte> RawSlice = Raw[..RawLength];
            ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)RawSlice;

            Stream.Write(ReadOnlyBaseRawSlice);
            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
        }
    }

    private void BaseWatcherOnChanged (
        Object              Sender,
        FileSystemEventArgs Args
    ) {
        lock (BaseWatcher) {
            FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write);
        
            Span<Byte> Raw   = stackalloc Byte[(Int32)Stream.Length];
            Span<Char> Chars = stackalloc Char[(Int32)Stream.Length];
        
            Int32 RawLength = Stream.Read(Raw); Stream.Flush(); Stream.Close(); Stream.Dispose();

            Span<Byte>         RawSlice               = Raw[..RawLength];
            ReadOnlySpan<Byte> ReadOnlyBaseRawSlice   = (ReadOnlySpan<Byte>)RawSlice;
            Int32              ValueLength            = Encoding.UTF8.GetChars(ReadOnlyBaseRawSlice, Chars);
            Span<Char>         ValueSlice             = Chars[..ValueLength].Trim(Environment.NewLine.AsSpan());
            ReadOnlySpan<Char> ReadOnlyBaseValueSlice = (ReadOnlySpan<Char>)ValueSlice;


            BaseCachedValueRaw = new String(ReadOnlyBaseValueSlice);
            if (T.TryParse(BaseCachedValueRaw, CultureInfo.InvariantCulture, out BaseCachedValue!) == false) {
                BaseCachedValue = BaseDefaults;
            }
        }
    }

    public void MutateValue(T Difference) {
        lock (BaseWatcher) {
            BaseCachedValue = BaseCachedValue + Difference;
            
            ReadOnlySpan<Char> LocalValue = BaseCachedValue.ToString().AsSpan();
            Span<Byte>         LocalRaw   = stackalloc Byte[LocalValue.Length * 2];
  
            Int32              RawLength        = Encoding.UTF8.GetBytes(LocalValue, LocalRaw);
            Span<Byte>         RawSlice         = LocalRaw[..RawLength];
            ReadOnlySpan<Byte> ReadOnlyBaseRawSlice = (ReadOnlySpan<Byte>)RawSlice;


            FileStream Stream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Write);
            Stream.Write(ReadOnlyBaseRawSlice); Stream.Flush(); Stream.Close(); Stream.Dispose();
        }
    }

    
    public void Dispose () {
        BaseWatcher.Dispose();
    }
    
    public Boolean Equals(T? Other) {
        return Other != null && Other.Equals(BaseCachedValue);
    }
    
    public static Boolean operator ==(Surface<T> Left, T Right) {
        return Left.Equals(Right);
    }

    public static Boolean operator !=(Surface<T> Left, T Right) {
        return !Left.Equals(Right);
    }
    
    public static Boolean operator ==(T Left, Surface<T> Right) {
        return Right.Equals(Left);
    }

    public static Boolean operator !=(T Left, Surface<T> Right) {
        return !Right.Equals(Left);
    }

    public static implicit operator T(Surface<T> Surface) {
        return Surface.Value;
    }
}