using System;
using System.Runtime.InteropServices;

namespace Nefarius.Peripherals.SerialPort.Win32PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct COMMTIMEOUTS
    {
        internal Int32 ReadIntervalTimeout;
        internal Int32 ReadTotalTimeoutMultiplier;
        internal Int32 ReadTotalTimeoutConstant;
        internal Int32 WriteTotalTimeoutMultiplier;
        internal Int32 WriteTotalTimeoutConstant;
    }
}