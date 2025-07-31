using System.Collections.Concurrent;

namespace RampartFS;

internal partial class Controller<T> : IDisposable where T : IParsable<T> {
    public Controller (String RootPath) {
        BaseController     = new ConcurrentDictionary<String, ControllerSurface<T>>();
        BaseRootPath       = RootPath;
        BasePathTranslator = new PathCache(BaseRootPath);
    }
    
    public T? this[String Identifier, T Defaults] {
        get {
            ControllerSurface<T> Surface = BaseController.GetOrAdd(Identifier, ValueFactory, new ValueTuple<T, PathCache>(Defaults, BasePathTranslator));
            
            return Surface.Value;
        }
        set {
            ControllerSurface<T> Surface = BaseController.GetOrAdd(Identifier, ValueFactory, new ValueTuple<T, PathCache>(Defaults, BasePathTranslator));
            Surface.Value = value;
        }
    }
    
    public String? GetRaw(String Identifier) {
        if (BaseController.TryGetValue(Identifier, out ControllerSurface<T>? Surface) == false) {
            return null;
        }
        
        return Surface.Raw;
    }
    
    public String? LoadRaw(String Identifier) {
        ControllerSurface<T> Surface = BaseController.GetOrAdd(Identifier, ValueFactory, new ValueTuple<PathCache>(BasePathTranslator));
        
        return Surface.Raw;
    }
    
    public void SetRaw(String Identifier, String Value) {
        if (BaseController.TryGetValue(Identifier, out ControllerSurface<T>? Surface) == false) {
            return;
        }

        Surface.Raw = Value;
    }

    private static ControllerSurface<T> ValueFactory (
        String                   Identifier,
        ValueTuple<T, PathCache> Arguments
    ) {
        T         Defaults       = Arguments.Item1;
        PathCache PathTranslator = Arguments.Item2;
        
        PathTranslator.GetAbsolutePath(Identifier, out String AbsolutePath);
        ControllerSurface<T> Surface = new ControllerSurface<T>(AbsolutePath, Defaults);
        return Surface;
    }
    
    private static ControllerSurface<T> ValueFactory (
        String                   Identifier,
        ValueTuple<PathCache> Arguments
    ) {
        PathCache PathTranslator = Arguments.Item1;
        
        PathTranslator.GetAbsolutePath(Identifier, out String AbsolutePath);
        ControllerSurface<T> Surface = new ControllerSurface<T>(AbsolutePath);
        return Surface;
    }

    public void Dispose () {
        foreach (KeyValuePair<String, ControllerSurface<T>> Entry in BaseController) {
            Entry.Value.Dispose();
        } BaseController.Clear();
    }
}