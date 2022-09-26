using System;
using System.Runtime.InteropServices;

namespace PInvokeSerialPort.Win32PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct COMMPROP
    {
        internal UInt16 wPacketLength;
        internal UInt16 wPacketVersion;
        internal UInt32 dwServiceMask;
        internal UInt32 dwReserved1;
        internal UInt32 dwMaxTxQueue;
        internal UInt32 dwMaxRxQueue;
        internal UInt32 dwMaxBaud;
        internal UInt32 dwProvSubType;
        internal UInt32 dwProvCapabilities;
        internal UInt32 dwSettableParams;
        internal UInt32 dwSettableBaud;
        internal UInt16 wSettableData;
        internal UInt16 wSettableStopParity;
        internal UInt32 dwCurrentTxQueue;
        internal UInt32 dwCurrentRxQueue;
        internal UInt32 dwProvSpec1;
        internal UInt32 dwProvSpec2;
        internal Byte wcProvChar;
    }
}