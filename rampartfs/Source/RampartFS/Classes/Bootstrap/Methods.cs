namespace RampartFS;

public abstract class Bootstrap { 
    public static void Main(String[] Args) {
        if (Chainloader.TryLaunch(Args, out Filesystem? Filesystem) == false) {
            return;
        }

        Driver Driver = new Driver(Filesystem);
        Driver.Init();
        Driver.Start();
        Driver.Dispose();
    }
}