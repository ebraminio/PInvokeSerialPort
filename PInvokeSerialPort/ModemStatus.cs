using PInvokeSerialPort.Win32PInvoke;

namespace PInvokeSerialPort
{
    /// <summary>
    /// Represents the status of the modem control input signals.
    /// </summary>
    public struct ModemStatus
    {
        private readonly uint _status;
        internal ModemStatus(uint val) { _status = val; }
        /// <summary>
        /// Condition of the Clear To Send signal.
        /// </summary>
        public bool Cts { get { return ((_status & Win32Com.MS_CTS_ON) != 0); } }
        /// <summary>
        /// Condition of the Data Set Ready signal.
        /// </summary>
        public bool Dsr { get { return ((_status & Win32Com.MS_DSR_ON) != 0); } }
        /// <summary>
        /// Condition of the Receive Line Status Detection signal.
        /// </summary>
        public bool Rlsd { get { return ((_status & Win32Com.MS_RLSD_ON) != 0); } }
        /// <summary>
        /// Condition of the Ring Detection signal.
        /// </summary>
        public bool Ring { get { return ((_status & Win32Com.MS_RING_ON) != 0); } }
    }
}