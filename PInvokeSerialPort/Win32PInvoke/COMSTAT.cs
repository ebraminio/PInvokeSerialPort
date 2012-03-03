using System;
using System.Runtime.InteropServices;

namespace PInvokeSerialPort.Win32PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct COMSTAT
    {
        internal const uint fCtsHold = 0x1;
        internal const uint fDsrHold = 0x2;
        internal const uint fRlsdHold = 0x4;
        internal const uint fXoffHold = 0x8;
        internal const uint fXoffSent = 0x10;
        internal const uint fEof = 0x20;
        internal const uint fTxim = 0x40;
        internal UInt32 Flags;
        internal UInt32 cbInQue;
        internal UInt32 cbOutQue;
    }
}