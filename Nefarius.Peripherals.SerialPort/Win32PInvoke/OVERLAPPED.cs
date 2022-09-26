using System;
using System.Runtime.InteropServices;

namespace PInvokeSerialPort.Win32PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct OVERLAPPED
    {
        internal UIntPtr Internal;
        internal UIntPtr InternalHigh;
        internal UInt32 Offset;
        internal UInt32 OffsetHigh;
        internal IntPtr hEvent;
    }
}