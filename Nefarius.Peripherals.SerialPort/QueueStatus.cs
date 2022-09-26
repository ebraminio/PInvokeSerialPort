using System.Text;
using PInvokeSerialPort.Win32PInvoke;

namespace PInvokeSerialPort
{
    /// <summary>
    ///     Represents the current condition of the port queues.
    /// </summary>
    public struct QueueStatus
    {
        private readonly uint _status;
        private readonly uint _inQueue;
        private readonly uint _outQueue;
        private readonly uint _inQueueSize;
        private readonly uint _outQueueSize;

        internal QueueStatus(uint stat, uint inQ, uint outQ, uint inQs, uint outQs)
        {
            _status = stat;
            _inQueue = inQ;
            _outQueue = outQ;
            _inQueueSize = inQs;
            _outQueueSize = outQs;
        }

        /// <summary>
        ///     Output is blocked by CTS handshaking.
        /// </summary>
        public bool CtsHold => (_status & COMSTAT.fCtsHold) != 0;

        /// <summary>
        ///     Output is blocked by DRS handshaking.
        /// </summary>
        public bool DsrHold => (_status & COMSTAT.fDsrHold) != 0;

        /// <summary>
        ///     Output is blocked by RLSD handshaking.
        /// </summary>
        public bool RlsdHold => (_status & COMSTAT.fRlsdHold) != 0;

        /// <summary>
        ///     Output is blocked because software handshaking is enabled and XOFF was received.
        /// </summary>
        public bool XoffHold => (_status & COMSTAT.fXoffHold) != 0;

        /// <summary>
        ///     Output was blocked because XOFF was sent and this station is not yet ready to receive.
        /// </summary>
        public bool XoffSent => (_status & COMSTAT.fXoffSent) != 0;

        /// <summary>
        ///     There is a character waiting for transmission in the immediate buffer.
        /// </summary>
        public bool ImmediateWaiting => (_status & COMSTAT.fTxim) != 0;

        /// <summary>
        ///     Number of bytes waiting in the input queue.
        /// </summary>
        public long InQueue => _inQueue;

        /// <summary>
        ///     Number of bytes waiting for transmission.
        /// </summary>
        public long OutQueue => _outQueue;

        /// <summary>
        ///     Total size of input queue (0 means information unavailable)
        /// </summary>
        public long InQueueSize => _inQueueSize;

        /// <summary>
        ///     Total size of output queue (0 means information unavailable)
        /// </summary>
        public long OutQueueSize => _outQueueSize;

        public override string ToString()
        {
            var m = new StringBuilder("The reception queue is ", 60);
            if (_inQueueSize == 0)
                m.Append("of unknown size and ");
            else
                m.Append(_inQueueSize + " bytes long and ");
            if (_inQueue == 0)
            {
                m.Append("is empty.");
            }
            else if (_inQueue == 1)
            {
                m.Append("contains 1 byte.");
            }
            else
            {
                m.Append("contains ");
                m.Append(_inQueue.ToString());
                m.Append(" bytes.");
            }

            m.Append(" The transmission queue is ");
            if (_outQueueSize == 0)
                m.Append("of unknown size and ");
            else
                m.Append(_outQueueSize + " bytes long and ");
            if (_outQueue == 0)
            {
                m.Append("is empty");
            }
            else if (_outQueue == 1)
            {
                m.Append("contains 1 byte. It is ");
            }
            else
            {
                m.Append("contains ");
                m.Append(_outQueue.ToString());
                m.Append(" bytes. It is ");
            }

            if (_outQueue > 0)
            {
                if (CtsHold || DsrHold || RlsdHold || XoffHold || XoffSent)
                {
                    m.Append("holding on");
                    if (CtsHold) m.Append(" CTS");
                    if (DsrHold) m.Append(" DSR");
                    if (RlsdHold) m.Append(" RLSD");
                    if (XoffHold) m.Append(" Rx XOff");
                    if (XoffSent) m.Append(" Tx XOff");
                }
                else
                {
                    m.Append("pumping data");
                }
            }

            m.Append(". The immediate buffer is ");
            if (ImmediateWaiting)
                m.Append("full.");
            else
                m.Append("empty.");
            return m.ToString();
        }
    }
}