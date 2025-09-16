using System.Runtime.CompilerServices;

namespace RampartFS;

public static class SpanHelpers {
    public static Int32 GetFullPath(ReadOnlySpan<Char> Relative, ReadOnlySpan<Char> Path, Span<Char> Absolute) {
        Builder Builder = new Builder(Absolute);

        if (System.IO.Path.IsPathRooted(Relative)) {
            Collapse(Relative, ref Builder);
        }
        else {
            Builder.Append(Path);
            
            if (Builder.Length > 0 && Builder[^1] != System.IO.Path.DirectorySeparatorChar)
                Builder.Append(System.IO.Path.DirectorySeparatorChar);
            
            Builder.Append(Relative);
            
            ReadOnlySpan<Char> Combined = Builder.AsSpan();
            Builder.Length = 0;
            Collapse(Combined, ref Builder);
        }

        return Builder.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] private static void Collapse(ReadOnlySpan<Char> Path, ref Builder Buffer) {
        Int32 Index = 0; while (Index < Path.Length) {
            if (Path[Index] == System.IO.Path.DirectorySeparatorChar) {
                if (Buffer.Length == 0 || Buffer[^1] != System.IO.Path.DirectorySeparatorChar) {
                    Buffer.Append(System.IO.Path.DirectorySeparatorChar);
                }
                    
                Index++;
                
                continue;
            }
            
            Int32 Start = Index;
            
            while (Index < Path.Length && Path[Index] != System.IO.Path.DirectorySeparatorChar) {
                Index++;
            }
                
            ReadOnlySpan<Char> Segment = Path.Slice(Start, Index - Start);

            switch (Segment) {
                case ".": break;
                case "..":
                    Int32 Subindex = Buffer.Length - 1;
                    if (Subindex > 0) {
                        while (Subindex > 0 && Buffer[Subindex - 1] != System.IO.Path.DirectorySeparatorChar) Subindex--;
                        Buffer.Length = Subindex;
                    }

                    break;
                default:
                    Buffer.Append(Segment);
                    Buffer.Append(System.IO.Path.DirectorySeparatorChar);

                    break;
            }
        }

        if (Buffer.Length > 1 && Buffer[^1] == System.IO.Path.DirectorySeparatorChar) {
            Buffer.Length--;
        }
    }
}