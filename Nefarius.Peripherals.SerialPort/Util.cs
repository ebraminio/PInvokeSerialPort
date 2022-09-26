using Windows.Win32.Devices.Communication;

namespace Nefarius.Peripherals.SerialPort;

internal static class DCBExtensions
{
    public static void Init(this DCB dcb, bool parity, bool outCts, bool outDsr, int dtr, bool inDsr, bool txc,
        bool xOut,
        bool xIn, int rts)
    {
        dcb.DCBlength = 28;
        dcb._bitfield = 0x8001;
        if (parity) dcb._bitfield |= 0x0002;
        if (outCts) dcb._bitfield |= 0x0004;
        if (outDsr) dcb._bitfield |= 0x0008;
        dcb._bitfield |= (uint)((dtr & 0x0003) << 4);
        if (inDsr) dcb._bitfield |= 0x0040;
        if (txc) dcb._bitfield |= 0x0080;
        if (xOut) dcb._bitfield |= 0x0100;
        if (xIn) dcb._bitfield |= 0x0200;
        dcb._bitfield |= (uint)((rts & 0x0003) << 12);
    }
}