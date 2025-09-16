namespace RampartFS;

public partial class Filesystem {
    public String StorageFolderPath {
        get {
            return BaseStorageFolderPath;
        }
    }

    public String ControlFolderPath {
        get {
            return BaseControlFolderPath;
        }
    }

    public String LogFolderPath {
        get {
            return BaseLogFolderPath;
        }
    }

    public String MountFolderPath {
        get {
            return BaseMountFolderPath;
        }
    }

    public Boolean KernelCaching {
        get {
            return BaseKernelCaching;
        }
    }

    public Int64 StorageCurrent {
        get {
            return BaseStorageCurrent;
        }
    }
}