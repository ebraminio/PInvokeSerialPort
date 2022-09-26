namespace PInvokeSerialPort
{
    /// <summary>
    /// Standard handshake methods
    /// </summary>
    public enum Handshake
    {
        /// <summary>
        /// No handshaking
        /// </summary>
        None,
        /// <summary>
        /// Software handshaking using Xon / Xoff
        /// </summary>
        XonXoff,
        /// <summary>
        /// Hardware handshaking using CTS / RTS
        /// </summary>
        CtsRts,
        /// <summary>
        /// Hardware handshaking using DSR / DTR
        /// </summary>
        DsrDtr
    }
}