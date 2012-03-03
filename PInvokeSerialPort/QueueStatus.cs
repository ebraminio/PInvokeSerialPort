using System.Text;
using PInvokeSerialPort.Win32PInvoke;

namespace PInvokeSerialPort
{
    /// <summary>
    /// Represents the current condition of the port queues.
    /// </summary>
    public struct QueueStatus
    {
        private uint status;
        private uint inQueue;
        private uint outQueue;
        private uint inQueueSize;
        private uint outQueueSize;

        internal QueueStatus(uint stat, uint inQ, uint outQ, uint inQs, uint outQs)
        { status = stat; inQueue = inQ; outQueue = outQ; inQueueSize = inQs; outQueueSize = outQs; }
        /// <summary>
        /// Output is blocked by CTS handshaking.
        /// </summary>
        public bool ctsHold { get { return ((status & COMSTAT.fCtsHold) != 0); } }
        /// <summary>
        /// Output is blocked by DRS handshaking.
        /// </summary>
        public bool dsrHold { get { return ((status & COMSTAT.fDsrHold) != 0); } }
        /// <summary>
        /// Output is blocked by RLSD handshaking.
        /// </summary>
        public bool rlsdHold { get { return ((status & COMSTAT.fRlsdHold) != 0); } }
        /// <summary>
        /// Output is blocked because software handshaking is enabled and XOFF was received.
        /// </summary>
        public bool xoffHold { get { return ((status & COMSTAT.fXoffHold) != 0); } }
        /// <summary>
        /// Output was blocked because XOFF was sent and this station is not yet ready to receive.
        /// </summary>
        public bool xoffSent { get { return ((status & COMSTAT.fXoffSent) != 0); } }

        /// <summary>
        /// There is a character waiting for transmission in the immediate buffer.
        /// </summary>
        public bool immediateWaiting { get { return ((status & COMSTAT.fTxim) != 0); } }

        /// <summary>
        /// Number of bytes waiting in the input queue.
        /// </summary>
        public long InQueue { get { return inQueue; } }
        /// <summary>
        /// Number of bytes waiting for transmission.
        /// </summary>
        public long OutQueue { get { return outQueue; } }
        /// <summary>
        /// Total size of input queue (0 means information unavailable)
        /// </summary>
        public long InQueueSize { get { return inQueueSize; } }
        /// <summary>
        /// Total size of output queue (0 means information unavailable)
        /// </summary>
        public long OutQueueSize { get { return outQueueSize; } }

        public override string ToString()
        {
            var m = new StringBuilder("The reception queue is ", 60);
            if (inQueueSize == 0)
            {
                m.Append("of unknown size and ");
            }
            else
            {
                m.Append(inQueueSize.ToString() + " bytes long and ");
            }
            if (inQueue == 0)
            {
                m.Append("is empty.");
            }
            else if (inQueue == 1)
            {
                m.Append("contains 1 byte.");
            }
            else
            {
                m.Append("contains ");
                m.Append(inQueue.ToString());
                m.Append(" bytes.");
            }
            m.Append(" The transmission queue is ");
            if (outQueueSize == 0)
            {
                m.Append("of unknown size and ");
            }
            else
            {
                m.Append(outQueueSize.ToString() + " bytes long and ");
            }
            if (outQueue == 0)
            {
                m.Append("is empty");
            }
            else if (outQueue == 1)
            {
                m.Append("contains 1 byte. It is ");
            }
            else
            {
                m.Append("contains ");
                m.Append(outQueue.ToString());
                m.Append(" bytes. It is ");
            }
            if (outQueue > 0)
            {
                if (ctsHold || dsrHold || rlsdHold || xoffHold || xoffSent)
                {
                    m.Append("holding on");
                    if (ctsHold) m.Append(" CTS");
                    if (dsrHold) m.Append(" DSR");
                    if (rlsdHold) m.Append(" RLSD");
                    if (xoffHold) m.Append(" Rx XOff");
                    if (xoffSent) m.Append(" Tx XOff");
                }
                else
                {
                    m.Append("pumping data");
                }
            }
            m.Append(". The immediate buffer is ");
            if (immediateWaiting)
                m.Append("full.");
            else
                m.Append("empty.");
            return m.ToString();
        }
    }
}