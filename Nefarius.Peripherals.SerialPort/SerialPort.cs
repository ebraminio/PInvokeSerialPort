using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Nefarius.Peripherals.SerialPort.Win32PInvoke;

namespace Nefarius.Peripherals.SerialPort
{
    /// <summary>
    ///     PInvokeSerialPort main class.
    ///     Borrowed from http://msdn.microsoft.com/en-us/magazine/cc301786.aspx ;)
    /// </summary>
    public class SerialPort : IDisposable
    {
        #region Private fields

        private readonly ManualResetEvent _writeEvent = new ManualResetEvent(false);
        private bool _auto;
        private bool _checkSends = true;

        private Handshake _handShake;
        private IntPtr _hPort;
        private bool _online;
        private IntPtr _ptrUwo = IntPtr.Zero;
        private Exception _rxException;
        private bool _rxExceptionReported;
        private Thread _rxThread;
        private int _stateBrk = 2;
        private int _stateDtr = 2;
        private int _stateRts = 2;
        private int _writeCount;

        #endregion

        #region Public properties

        /// <summary>
        ///     Class constructor
        /// </summary>
        public SerialPort(string portName)
        {
            PortName = portName;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Class constructor
        /// </summary>
        public SerialPort(string portName, int baudRate) : this(portName)
        {
            BaudRate = baudRate;
        }

        /// <summary>
        ///     If true, the port will automatically re-open on next send if it was previously closed due
        ///     to an error (default: false)
        /// </summary>
        public bool AutoReopen { get; set; }

        /// <summary>
        ///     Baud Rate (default: 115200)
        /// </summary>
        /// <remarks>Unsupported rates will throw "Bad settings".</remarks>
        public int BaudRate { get; set; } = 115200;

        /// <summary>
        ///     If true, subsequent Send commands wait for completion of earlier ones enabling the results
        ///     to be checked. If false, errors, including timeouts, may not be detected, but performance
        ///     may be better.
        /// </summary>
        public bool CheckAllSends { get; set; } = true;

        /// <summary>
        ///     Number of databits 1..8 (default: 8) unsupported values will throw "Bad settings"
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        ///     The parity checking scheme (default: none)
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        ///     If true, Xon and Xoff characters are sent to control the data flow from the remote station (default: false)
        /// </summary>
        public bool RxFlowX { get; set; }

        /// <summary>
        ///     If true, received characters are ignored unless DSR is asserted by the remote station (default: false)
        /// </summary>
        public bool RxGateDsr { get; set; }

        /// <summary>
        ///     The number of free bytes in the reception queue at which flow is disabled (default: 2048)
        /// </summary>
        public int RxHighWater { get; set; } = 2048;

        /// <summary>
        ///     The number of bytes in the reception queue at which flow is re-enabled (default: 512)
        /// </summary>
        public int RxLowWater { get; set; } = 512;

        /// <summary>
        ///     Requested size for receive queue (default: 0 = use operating system default)
        /// </summary>
        public int RxQueue { get; set; }

        /// <summary>
        ///     Constant.  Max time for Send in ms = (Multiplier * Characters) + Constant (default: 0)
        /// </summary>
        public int SendTimeoutConstant { get; set; }

        /// <summary>
        ///     Multiplier. Max time for Send in ms = (Multiplier * Characters) + Constant
        ///     (default: 0 = No timeout)
        /// </summary>
        public int SendTimeoutMultiplier { get; set; }

        /// <summary>
        ///     Number of stop bits (default: one)
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        ///     If true, transmission is halted unless CTS is asserted by the remote station (default: false)
        /// </summary>
        public bool TxFlowCts { get; set; }

        /// <summary>
        ///     If true, transmission is halted unless DSR is asserted by the remote station (default: false)
        /// </summary>
        public bool TxFlowDsr { get; set; }

        /// <summary>
        ///     If true, transmission is halted when Xoff is received and restarted when Xon is received (default: false)
        /// </summary>
        public bool TxFlowX { get; set; }

        /// <summary>
        ///     Requested size for transmit queue (default: 0 = use operating system default)
        /// </summary>
        public int TxQueue { get; set; }

        /// <summary>
        ///     If false, transmission is suspended when this station has sent Xoff to the remote station (default: true)
        ///     Set false if the remote station treats any character as an Xon.
        /// </summary>
        public bool TxWhenRxXoff { get; set; } = true;

        /// <summary>
        ///     Specidies the use to which the DTR output is put (default: none)
        /// </summary>
        public HsOutput UseDtr { get; set; } = HsOutput.None;

        /// <summary>
        ///     Specifies the use to which the RTS output is put (default: none)
        /// </summary>
        public HsOutput UseRts { get; set; } = HsOutput.None;

        /// <summary>
        ///     The character used to signal Xoff for X flow control (default: DC3)
        /// </summary>
        public ASCII XoffChar { get; set; } = ASCII.DC3;

        /// <summary>
        ///     The character used to signal Xon for X flow control (default: DC1)
        /// </summary>
        public ASCII XonChar { get; set; } = ASCII.DC1;

        /// <summary>
        ///     True if online.
        /// </summary>
        public bool Online => _online && CheckOnline();

        /// <summary>
        ///     True if the RTS pin is controllable via the RTS property
        /// </summary>
        protected bool RtSavailable => _stateRts < 2;

        /// <summary>
        ///     Set the state of the RTS modem control output
        /// </summary>
        protected bool Rts
        {
            set
            {
                if (_stateRts > 1) return;
                CheckOnline();
                if (value)
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.SETRTS))
                        _stateRts = 1;
                    else
                        ThrowException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.CLRRTS))
                        _stateRts = 1;
                    else
                        ThrowException("Unexpected Failure");
                }
            }
            get => _stateRts == 1;
        }

        /// <summary>
        ///     True if the DTR pin is controllable via the DTR property
        /// </summary>
        protected bool DtrAvailable => _stateDtr < 2;

        /// <summary>
        ///     The state of the DTR modem control output
        /// </summary>
        protected bool Dtr
        {
            set
            {
                if (_stateDtr > 1) return;
                CheckOnline();
                if (value)
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.SETDTR))
                        _stateDtr = 1;
                    else
                        ThrowException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.CLRDTR))
                        _stateDtr = 0;
                    else
                        ThrowException("Unexpected Failure");
                }
            }
            get => _stateDtr == 1;
        }

        /// <summary>
        ///     Assert or remove a break condition from the transmission line
        /// </summary>
        protected bool Break
        {
            set
            {
                if (_stateBrk > 1) return;
                CheckOnline();
                if (value)
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.SETBREAK))
                        _stateBrk = 0;
                    else
                        ThrowException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(_hPort, Win32Com.CLRBREAK))
                        _stateBrk = 0;
                    else
                        ThrowException("Unexpected Failure");
                }
            }
            get => _stateBrk == 1;
        }


        /// <summary>
        ///     Port Name
        /// </summary>
        public string PortName { get; set; }

        public Handshake Handshake
        {
            get => _handShake;
            set
            {
                _handShake = value;
                switch (_handShake)
                {
                    case Handshake.None:
                        TxFlowCts = false;
                        TxFlowDsr = false;
                        TxFlowX = false;
                        RxFlowX = false;
                        UseRts = HsOutput.Online;
                        UseDtr = HsOutput.Online;
                        TxWhenRxXoff = true;
                        RxGateDsr = false;
                        break;
                    case Handshake.XonXoff:
                        TxFlowCts = false;
                        TxFlowDsr = false;
                        TxFlowX = true;
                        RxFlowX = true;
                        UseRts = HsOutput.Online;
                        UseDtr = HsOutput.Online;
                        TxWhenRxXoff = true;
                        RxGateDsr = false;
                        XonChar = ASCII.DC1;
                        XoffChar = ASCII.DC3;
                        break;
                    case Handshake.CtsRts:
                        TxFlowCts = true;
                        TxFlowDsr = false;
                        TxFlowX = false;
                        RxFlowX = false;
                        UseRts = HsOutput.Handshake;
                        UseDtr = HsOutput.Online;
                        TxWhenRxXoff = true;
                        RxGateDsr = false;
                        break;
                    case Handshake.DsrDtr:
                        TxFlowCts = false;
                        TxFlowDsr = true;
                        TxFlowX = false;
                        RxFlowX = false;
                        UseRts = HsOutput.Online;
                        UseDtr = HsOutput.Handshake;
                        TxWhenRxXoff = true;
                        RxGateDsr = false;
                        break;
                }
            }
        }

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     For IDisposable
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        ///     Opens the com port and configures it with the required settings
        /// </summary>
        /// <returns>false if the port could not be opened</returns>
        public bool Open()
        {
            var portDcb = new DCB();
            var commTimeouts = new COMMTIMEOUTS();
            var wo = new OVERLAPPED();

            if (_online) return false;

            _hPort = Win32Com.CreateFile(PortName, Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                Win32Com.OPEN_EXISTING, Win32Com.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (_hPort == (IntPtr) Win32Com.INVALID_HANDLE_VALUE)
            {
                if (Marshal.GetLastWin32Error() == Win32Com.ERROR_ACCESS_DENIED) return false;
                throw new CommPortException("Port Open Failure");
            }

            _online = true;

            commTimeouts.ReadIntervalTimeout = 0;
            commTimeouts.ReadTotalTimeoutConstant = 0;
            commTimeouts.ReadTotalTimeoutMultiplier = 0;
            commTimeouts.WriteTotalTimeoutConstant = SendTimeoutConstant;
            commTimeouts.WriteTotalTimeoutMultiplier = SendTimeoutMultiplier;
            portDcb.Init(Parity == Parity.Odd || Parity == Parity.Even, TxFlowCts, TxFlowDsr,
                (int) UseDtr, RxGateDsr, !TxWhenRxXoff, TxFlowX, RxFlowX, (int) UseRts);
            portDcb.BaudRate = BaudRate;
            portDcb.ByteSize = (byte) DataBits;
            portDcb.Parity = (byte) Parity;
            portDcb.StopBits = (byte) StopBits;
            portDcb.XoffChar = (byte) XoffChar;
            portDcb.XonChar = (byte) XonChar;
            portDcb.XoffLim = (short) RxHighWater;
            portDcb.XonLim = (short) RxLowWater;
            if (RxQueue != 0 || TxQueue != 0)
                if (!Win32Com.SetupComm(_hPort, (uint) RxQueue, (uint) TxQueue))
                    ThrowException("Bad queue settings");
            if (!Win32Com.SetCommState(_hPort, ref portDcb)) ThrowException("Bad com settings");
            if (!Win32Com.SetCommTimeouts(_hPort, ref commTimeouts)) ThrowException("Bad timeout settings");

            _stateBrk = 0;
            switch (UseDtr)
            {
                case HsOutput.None:
                    _stateDtr = 0;
                    break;
                case HsOutput.Online:
                    _stateDtr = 1;
                    break;
            }

            switch (UseRts)
            {
                case HsOutput.None:
                    _stateRts = 0;
                    break;
                case HsOutput.Online:
                    _stateRts = 1;
                    break;
            }

            _checkSends = CheckAllSends;
            wo.Offset = 0;
            wo.OffsetHigh = 0;
            wo.hEvent = _checkSends ? _writeEvent.SafeWaitHandle.DangerousGetHandle() : IntPtr.Zero;
            _ptrUwo = Marshal.AllocHGlobal(Marshal.SizeOf(wo));
            Marshal.StructureToPtr(wo, _ptrUwo, true);
            _writeCount = 0;

            _rxException = null;
            _rxExceptionReported = false;
            
            // TODO: utilize Task Parallel Library here
            _rxThread = new Thread(ReceiveThread)
            {
                Name = "CommBaseRx", Priority = ThreadPriority.AboveNormal, IsBackground = true
            };
            
            _rxThread.Start();
            Thread.Sleep(1); //Give rx thread time to start. By documentation, 0 should work, but it does not!

            _auto = false;
            if (AfterOpen())
            {
                _auto = AutoReopen;
                return true;
            }

            Close();
            return false;
        }

        /// <summary>
        ///     Closes the com port.
        /// </summary>
        public void Close()
        {
            if (_online)
            {
                _auto = false;
                BeforeClose(false);
                InternalClose();
                _rxException = null;
            }
        }

        private void InternalClose()
        {
            Win32Com.CancelIo(_hPort);
            if (_rxThread != null)
            {
                _rxThread.Abort();
                _rxThread = null;
            }

            Win32Com.CloseHandle(_hPort);
            if (_ptrUwo != IntPtr.Zero) Marshal.FreeHGlobal(_ptrUwo);
            _stateRts = 2;
            _stateDtr = 2;
            _stateBrk = 2;
            _online = false;
        }

        /// <summary>
        ///     Destructor (just in case)
        /// </summary>
        ~SerialPort()
        {
            Close();
        }

        /// <summary>
        ///     Block until all bytes in the queue have been transmitted.
        /// </summary>
        public void Flush()
        {
            CheckOnline();
            CheckResult();
        }

        /// <summary>
        ///     Use this to throw exceptions in derived classes. Correctly handles threading issues
        ///     and closes the port if necessary.
        /// </summary>
        /// <param name="reason">Description of fault</param>
        protected void ThrowException(string reason)
        {
            if (Thread.CurrentThread == _rxThread) throw new CommPortException(reason);
            if (_online)
            {
                BeforeClose(true);
                InternalClose();
            }

            if (_rxException == null) throw new CommPortException(reason);
            throw new CommPortException(_rxException);
        }

        /// <summary>
        ///     Queues bytes for transmission.
        /// </summary>
        /// <param name="toSend">Array of bytes to be sent</param>
        public void Write(byte[] toSend)
        {
            uint sent;
            CheckOnline();
            CheckResult();
            _writeCount = toSend.GetLength(0);
            if (Win32Com.WriteFile(_hPort, toSend, (uint) _writeCount, out sent, _ptrUwo))
            {
                _writeCount -= (int) sent;
            }
            else
            {
                if (Marshal.GetLastWin32Error() != Win32Com.ERROR_IO_PENDING) ThrowException("Unexpected failure");
            }
        }

        /// <summary>
        ///     Queues string for transmission.
        /// </summary>
        /// <param name="toSend">Array of bytes to be sent</param>
        public void Write(string toSend)
        {
            Write(new ASCIIEncoding().GetBytes(toSend));
        }

        /// <summary>
        ///     Queues a single byte for transmission.
        /// </summary>
        /// <param name="toSend">Byte to be sent</param>
        public void Write(byte toSend)
        {
            var b = new byte[1];
            b[0] = toSend;
            Write(b);
        }

        /// <summary>
        ///     Queues a single char for transmission.
        /// </summary>
        /// <param name="toSend">Byte to be sent</param>
        public void Write(char toSend)
        {
            Write(toSend.ToString());
        }

        /// <summary>
        ///     Queues string with a new line ("\r\n") for transmission.
        /// </summary>
        /// <param name="toSend">Array of bytes to be sent</param>
        public void WriteLine(string toSend)
        {
            Write(new ASCIIEncoding().GetBytes(toSend + Environment.NewLine));
        }

        private void CheckResult()
        {
            if (_writeCount <= 0) return;
            uint sent;
            if (Win32Com.GetOverlappedResult(_hPort, _ptrUwo, out sent, _checkSends))
            {
                _writeCount -= (int) sent;
                if (_writeCount != 0) ThrowException("Send Timeout");
            }
            else
            {
                if (Marshal.GetLastWin32Error() != Win32Com.ERROR_IO_PENDING) ThrowException("Unexpected failure");
            }
        }

        /// <summary>
        ///     Sends a protocol byte immediately ahead of any queued bytes.
        /// </summary>
        /// <param name="tosend">Byte to send</param>
        /// <returns>False if an immediate byte is already scheduled and not yet sent</returns>
        public void SendImmediate(byte tosend)
        {
            CheckOnline();
            if (!Win32Com.TransmitCommChar(_hPort, tosend)) ThrowException("Transmission failure");
        }

        /// <summary>
        ///     Gets the status of the modem control input signals.
        /// </summary>
        /// <returns>Modem status object</returns>
        protected ModemStatus GetModemStatus()
        {
            uint f;

            CheckOnline();
            if (!Win32Com.GetCommModemStatus(_hPort, out f)) ThrowException("Unexpected failure");
            return new ModemStatus(f);
        }


        /// <summary>
        ///     Get the status of the queues
        /// </summary>
        /// <returns>Queue status object</returns>
        protected QueueStatus GetQueueStatus()
        {
            COMSTAT cs;
            COMMPROP cp;
            uint er;

            CheckOnline();
            if (!Win32Com.ClearCommError(_hPort, out er, out cs)) ThrowException("Unexpected failure");
            if (!Win32Com.GetCommProperties(_hPort, out cp)) ThrowException("Unexpected failure");
            return new QueueStatus(cs.Flags, cs.cbInQue, cs.cbOutQue, cp.dwCurrentRxQueue, cp.dwCurrentTxQueue);
        }

        /// <summary>
        ///     Override this to provide processing after the port is opened (i.e. to configure remote
        ///     device or just check presence).
        /// </summary>
        /// <returns>false to close the port again</returns>
        protected virtual bool AfterOpen()
        {
            return true;
        }

        /// <summary>
        ///     Override this to provide processing prior to port closure.
        /// </summary>
        /// <param name="error">True if closing due to an error</param>
        protected virtual void BeforeClose(bool error)
        {
        }

        public event Action<byte> DataReceived;

        /// <summary>
        ///     Override this to process received bytes.
        /// </summary>
        /// <param name="ch">The byte that was received</param>
        protected void OnRxChar(byte ch)
        {
            DataReceived?.Invoke(ch);
        }

        /// <summary>
        ///     Override this to take action when transmission is complete (i.e. all bytes have actually
        ///     been sent, not just queued).
        /// </summary>
        protected virtual void OnTxDone()
        {
        }

        /// <summary>
        ///     Override this to take action when a break condition is detected on the input line.
        /// </summary>
        protected virtual void OnBreak()
        {
        }

        /// <summary>
        ///     Override this to take action when a ring condition is signaled by an attached modem.
        /// </summary>
        protected virtual void OnRing()
        {
        }

        /// <summary>
        ///     Override this to take action when one or more modem status inputs change state
        /// </summary>
        /// <param name="mask">The status inputs that have changed state</param>
        /// <param name="state">The state of the status inputs</param>
        protected virtual void OnStatusChange(ModemStatus mask, ModemStatus state)
        {
        }

        /// <summary>
        ///     Override this to take action when the reception thread closes due to an exception being thrown.
        /// </summary>
        /// <param name="e">The exception which was thrown</param>
        protected virtual void OnRxException(Exception e)
        {
        }

        private void ReceiveThread()
        {
            var buf = new byte[1];

            var sg = new AutoResetEvent(false);
            var ov = new OVERLAPPED();
            var unmanagedOv = Marshal.AllocHGlobal(Marshal.SizeOf(ov));
            ov.Offset = 0;
            ov.OffsetHigh = 0;
            ov.hEvent = sg.SafeWaitHandle.DangerousGetHandle();
            Marshal.StructureToPtr(ov, unmanagedOv, true);

            uint eventMask = 0;
            var uMask = Marshal.AllocHGlobal(Marshal.SizeOf(eventMask));

            try
            {
                while (true)
                {
                    if (!Win32Com.SetCommMask(_hPort,
                        Win32Com.EV_RXCHAR | Win32Com.EV_TXEMPTY | Win32Com.EV_CTS | Win32Com.EV_DSR
                        | Win32Com.EV_BREAK | Win32Com.EV_RLSD | Win32Com.EV_RING | Win32Com.EV_ERR))
                        throw new CommPortException("IO Error [001]");
                    Marshal.WriteInt32(uMask, 0);
                    if (!Win32Com.WaitCommEvent(_hPort, uMask, unmanagedOv))
                    {
                        if (Marshal.GetLastWin32Error() == Win32Com.ERROR_IO_PENDING)
                            sg.WaitOne();
                        else
                            throw new CommPortException("IO Error [002]");
                    }

                    eventMask = (uint) Marshal.ReadInt32(uMask);
                    if ((eventMask & Win32Com.EV_ERR) != 0)
                    {
                        uint errs;
                        if (Win32Com.ClearCommError(_hPort, out errs, IntPtr.Zero))
                        {
                            var s = new StringBuilder("UART Error: ", 40);
                            if ((errs & Win32Com.CE_FRAME) != 0) s = s.Append("Framing,");
                            if ((errs & Win32Com.CE_IOE) != 0) s = s.Append("IO,");
                            if ((errs & Win32Com.CE_OVERRUN) != 0) s = s.Append("Overrun,");
                            if ((errs & Win32Com.CE_RXOVER) != 0) s = s.Append("Receive Overflow,");
                            if ((errs & Win32Com.CE_RXPARITY) != 0) s = s.Append("Parity,");
                            if ((errs & Win32Com.CE_TXFULL) != 0) s = s.Append("Transmit Overflow,");
                            s.Length = s.Length - 1;
                            throw new CommPortException(s.ToString());
                        }

                        throw new CommPortException("IO Error [003]");
                    }

                    if ((eventMask & Win32Com.EV_RXCHAR) != 0)
                    {
                        uint gotbytes;
                        do
                        {
                            if (!Win32Com.ReadFile(_hPort, buf, 1, out gotbytes, unmanagedOv))
                            {
                                if (Marshal.GetLastWin32Error() == Win32Com.ERROR_IO_PENDING)
                                {
                                    Win32Com.CancelIo(_hPort);
                                    gotbytes = 0;
                                }
                                else
                                {
                                    throw new CommPortException("IO Error [004]");
                                }
                            }

                            if (gotbytes == 1) OnRxChar(buf[0]);
                        } while (gotbytes > 0);
                    }

                    if ((eventMask & Win32Com.EV_TXEMPTY) != 0) OnTxDone();
                    if ((eventMask & Win32Com.EV_BREAK) != 0) OnBreak();

                    uint i = 0;
                    if ((eventMask & Win32Com.EV_CTS) != 0) i |= Win32Com.MS_CTS_ON;
                    if ((eventMask & Win32Com.EV_DSR) != 0) i |= Win32Com.MS_DSR_ON;
                    if ((eventMask & Win32Com.EV_RLSD) != 0) i |= Win32Com.MS_RLSD_ON;
                    if ((eventMask & Win32Com.EV_RING) != 0) i |= Win32Com.MS_RING_ON;
                    if (i != 0)
                    {
                        uint f;
                        if (!Win32Com.GetCommModemStatus(_hPort, out f)) throw new CommPortException("IO Error [005]");
                        OnStatusChange(new ModemStatus(i), new ModemStatus(f));
                    }
                }
            }
            catch (Exception e)
            {
                if (uMask != IntPtr.Zero) Marshal.FreeHGlobal(uMask);
                if (unmanagedOv != IntPtr.Zero) Marshal.FreeHGlobal(unmanagedOv);
                if (!(e is ThreadAbortException))
                {
                    _rxException = e;
                    OnRxException(e);
                }
            }
        }

        private bool CheckOnline()
        {
            if (_rxException != null && !_rxExceptionReported)
            {
                _rxExceptionReported = true;
                ThrowException("rx");
            }

            if (_online)
            {
                uint f;
                if (Win32Com.GetHandleInformation(_hPort, out f)) return true;
                ThrowException("Offline");
                return false;
            }

            if (_auto)
                if (Open())
                    return true;
            ThrowException("Offline");
            return false;
        }
    }
}