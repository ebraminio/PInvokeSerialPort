using System;
using System.Runtime.InteropServices;

namespace PInvokeSerialPort.Win32PInvoke
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