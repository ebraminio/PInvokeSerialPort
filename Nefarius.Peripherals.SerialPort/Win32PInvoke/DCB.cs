using System;
using System.Runtime.InteropServices;

namespace PInvokeSerialPort.Win32PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DCB
    {
        internal Int32 DCBlength;
        internal Int32 BaudRate;
        internal Int32 PackedValues;
        internal Int16 wReserved;
        internal Int16 XonLim;
        internal Int16 XoffLim;
        internal Byte ByteSize;
        internal Byte Parity;
        internal Byte StopBits;
        internal Byte XonChar;
        internal Byte XoffChar;
        internal Byte ErrorChar;
        internal Byte EofChar;
        internal Byte EvtChar;
        internal Int16 wReserved1;

        internal void Init(bool parity, bool outCts, bool outDsr, int dtr, bool inDsr, bool txc, bool xOut,
                           bool xIn, int rts)
        {
            DCBlength = 28; PackedValues = 0x8001;
            if (parity) PackedValues |= 0x0002;
            if (outCts) PackedValues |= 0x0004;
            if (outDsr) PackedValues |= 0x0008;
            PackedValues |= ((dtr & 0x0003) << 4);
            if (inDsr) PackedValues |= 0x0040;
            if (txc) PackedValues |= 0x0080;
            if (xOut) PackedValues |= 0x0100;
            if (xIn) PackedValues |= 0x0200;
            PackedValues |= ((rts & 0x0003) << 12);

        }
    }
}