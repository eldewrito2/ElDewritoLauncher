using System;
using System.Runtime.InteropServices;

namespace EDLauncher.Utility
{
    public class ConsoleScope : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        public static void Allocate()
        {
            AllocConsole();
        }

        public void Dispose()
        {
            FreeConsole();
        }
    }
}
