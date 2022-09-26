using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Devices.Communication;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using JetBrains.Annotations;

namespace Nefarius.Peripherals.SerialPort;

/// <summary>
///     PInvokeSerialPort main class.
///     Borrowed from http://msdn.microsoft.com/en-us/magazine/cc301786.aspx ;)
/// </summary>
public sealed class SerialPort : IDisposable
{
    private readonly ManualResetEvent _writeEvent = new(false);
    private bool _auto;
    private bool _checkSends = true;

    private Handshake _handShake;
    private SafeHandle _hPort;
    private bool _online;
    private Exception _rxException;
    private bool _rxExceptionReported;
    private Thread _rxThread;
    private int _stateBrk = 2;
    private int _stateDtr = 2;
    private int _stateRts = 2;
    private int _writeCount;
    private NativeOverlapped _writeOverlapped;


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
    ///     Gets the status of the modem control input signals.
    /// </summary>
    /// <returns>Modem status object</returns>
    public ModemStatus ModemStatus
    {
        get
        {
            CheckOnline();
            if (!PInvoke.GetCommModemStatus(_hPort, out var f)) ThrowException("Unexpected failure");
            return new ModemStatus(f);
        }
    }

    /// <summary>
    ///     Get the status of the queues
    /// </summary>
    /// <returns>Queue status object</returns>
    public unsafe QueueStatus QueueStatus
    {
        get
        {
            COMSTAT cs;
            var cp = new COMMPROP();
            CLEAR_COMM_ERROR_FLAGS er;

            CheckOnline();
            if (!PInvoke.ClearCommError(_hPort, &er, &cs)) ThrowException("Unexpected failure");
            if (!PInvoke.GetCommProperties(_hPort, ref cp)) ThrowException("Unexpected failure");
            return new QueueStatus(cs._bitfield, cs.cbInQue, cs.cbOutQue, cp.dwCurrentRxQueue, cp.dwCurrentTxQueue);
        }
    }

    /// <summary>
    ///     If true, the port will automatically re-open on next send if it was previously closed due
    ///     to an error (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool AutoReopen { get; set; }

    /// <summary>
    ///     Baud Rate (default: 115200)
    /// </summary>
    /// <remarks>Unsupported rates will throw "Bad settings".</remarks>
    [UsedImplicitly]
    public int BaudRate { get; set; } = 115200;

    /// <summary>
    ///     If true, subsequent Send commands wait for completion of earlier ones enabling the results
    ///     to be checked. If false, errors, including timeouts, may not be detected, but performance
    ///     may be better.
    /// </summary>
    [UsedImplicitly]
    public bool CheckAllSends { get; set; } = true;

    /// <summary>
    ///     Number of databits 1..8 (default: 8) unsupported values will throw "Bad settings"
    /// </summary>
    [UsedImplicitly]
    public int DataBits { get; set; } = 8;

    /// <summary>
    ///     The parity checking scheme (default: none)
    /// </summary>
    [UsedImplicitly]
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    ///     If true, Xon and Xoff characters are sent to control the data flow from the remote station (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool RxFlowX { get; set; }

    /// <summary>
    ///     If true, received characters are ignored unless DSR is asserted by the remote station (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool RxGateDsr { get; set; }

    /// <summary>
    ///     The number of free bytes in the reception queue at which flow is disabled (default: 2048)
    /// </summary>
    [UsedImplicitly]
    public int RxHighWater { get; set; } = 2048;

    /// <summary>
    ///     The number of bytes in the reception queue at which flow is re-enabled (default: 512)
    /// </summary>
    [UsedImplicitly]
    public int RxLowWater { get; set; } = 512;

    /// <summary>
    ///     Requested size for receive queue (default: 0 = use operating system default)
    /// </summary>
    [UsedImplicitly]
    public int RxQueue { get; set; }

    /// <summary>
    ///     Constant.  Max time for Send in ms = (Multiplier * Characters) + Constant (default: 0)
    /// </summary>
    [UsedImplicitly]
    public int SendTimeoutConstant { get; set; }

    /// <summary>
    ///     Multiplier. Max time for Send in ms = (Multiplier * Characters) + Constant
    ///     (default: 0 = No timeout)
    /// </summary>
    [UsedImplicitly]
    public int SendTimeoutMultiplier { get; set; }

    /// <summary>
    ///     Number of stop bits (default: one)
    /// </summary>
    [UsedImplicitly]
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    ///     If true, transmission is halted unless CTS is asserted by the remote station (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool TxFlowCts { get; set; }

    /// <summary>
    ///     If true, transmission is halted unless DSR is asserted by the remote station (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool TxFlowDsr { get; set; }

    /// <summary>
    ///     If true, transmission is halted when Xoff is received and restarted when Xon is received (default: false)
    /// </summary>
    [UsedImplicitly]
    public bool TxFlowX { get; set; }

    /// <summary>
    ///     Requested size for transmit queue (default: 0 = use operating system default)
    /// </summary>
    [UsedImplicitly]
    public int TxQueue { get; set; }

    /// <summary>
    ///     If false, transmission is suspended when this station has sent Xoff to the remote station (default: true)
    ///     Set false if the remote station treats any character as an Xon.
    /// </summary>
    [UsedImplicitly]
    public bool TxWhenRxXoff { get; set; } = true;

    /// <summary>
    ///     Specidies the use to which the DTR output is put (default: none)
    /// </summary>
    [UsedImplicitly]
    public HsOutput UseDtr { get; set; } = HsOutput.None;

    /// <summary>
    ///     Specifies the use to which the RTS output is put (default: none)
    /// </summary>
    [UsedImplicitly]
    public HsOutput UseRts { get; set; } = HsOutput.None;

    /// <summary>
    ///     The character used to signal Xoff for X flow control (default: DC3)
    /// </summary>
    [UsedImplicitly]
    public ASCII XoffChar { get; set; } = ASCII.DC3;

    /// <summary>
    ///     The character used to signal Xon for X flow control (default: DC1)
    /// </summary>
    [UsedImplicitly]
    public ASCII XonChar { get; set; } = ASCII.DC1;

    /// <summary>
    ///     True if online.
    /// </summary>
    [UsedImplicitly]
    public bool Online => _online && CheckOnline();

    /// <summary>
    ///     True if the RTS pin is controllable via the RTS property
    /// </summary>
    [UsedImplicitly]
    public bool RtSAvailable => _stateRts < 2;

    /// <summary>
    ///     Set the state of the RTS modem control output
    /// </summary>
    public bool Rts
    {
        set
        {
            if (_stateRts > 1) return;
            CheckOnline();
            if (value)
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.SETRTS))
                    _stateRts = 1;
                else
                    ThrowException("Unexpected Failure");
            }
            else
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.CLRRTS))
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
    public bool DtrAvailable => _stateDtr < 2;

    /// <summary>
    ///     The state of the DTR modem control output
    /// </summary>
    public bool Dtr
    {
        set
        {
            if (_stateDtr > 1) return;
            CheckOnline();
            if (value)
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.SETDTR))
                    _stateDtr = 1;
                else
                    ThrowException("Unexpected Failure");
            }
            else
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.CLRDTR))
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
    public bool IsBreakEnabled
    {
        set
        {
            if (_stateBrk > 1) return;
            CheckOnline();
            if (value)
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.SETBREAK))
                    _stateBrk = 0;
                else
                    ThrowException("Unexpected Failure");
            }
            else
            {
                if (PInvoke.EscapeCommFunction(_hPort, ESCAPE_COMM_FUNCTION.CLRBREAK))
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
    [UsedImplicitly]
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
    [UsedImplicitly]
    public bool Open()
    {
        var portDcb = new DCB();
        var commTimeouts = new COMMTIMEOUTS();

        if (_online) return false;

        _hPort = PInvoke.CreateFile(
            PortName,
            FILE_ACCESS_FLAGS.FILE_GENERIC_READ | FILE_ACCESS_FLAGS.FILE_GENERIC_WRITE,
            0,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
            null
        );

        if (_hPort.IsInvalid)
        {
            if (Marshal.GetLastWin32Error() == (int)WIN32_ERROR.ERROR_ACCESS_DENIED) return false;
            throw new CommPortException("Port Open Failure");
        }

        _online = true;

        commTimeouts.WriteTotalTimeoutConstant = (uint)SendTimeoutConstant;
        commTimeouts.WriteTotalTimeoutMultiplier = (uint)SendTimeoutMultiplier;

        portDcb.Init(
            Parity is Parity.Odd or Parity.Even,
            TxFlowCts,
            TxFlowDsr,
            (int)UseDtr,
            RxGateDsr,
            !TxWhenRxXoff,
            TxFlowX,
            RxFlowX,
            (int)UseRts
        );
        portDcb.BaudRate = (uint)BaudRate;
        portDcb.ByteSize = (byte)DataBits;
        portDcb.Parity = (DCB_PARITY)Parity;
        portDcb.StopBits = (DCB_STOP_BITS)StopBits;
        portDcb.XoffChar = new CHAR((byte)XoffChar);
        portDcb.XonChar = new CHAR((byte)XonChar);
        portDcb.XoffLim = (ushort)RxHighWater;
        portDcb.XonLim = (ushort)RxLowWater;

        if (RxQueue != 0 || TxQueue != 0)
            if (!PInvoke.SetupComm(_hPort, (uint)RxQueue, (uint)TxQueue))
                ThrowException("Bad queue settings");

        if (!PInvoke.SetCommState(_hPort, portDcb)) ThrowException("Bad com settings");
        if (!PInvoke.SetCommTimeouts(_hPort, commTimeouts)) ThrowException("Bad timeout settings");

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

        _writeOverlapped = new NativeOverlapped
        {
            EventHandle = _checkSends ? _writeEvent.SafeWaitHandle.DangerousGetHandle() : IntPtr.Zero
        };

        _writeCount = 0;

        _rxException = null;
        _rxExceptionReported = false;

        _rxThread = new Thread(ReceiveThread)
        {
            Name = "CommBaseRx",
            Priority = ThreadPriority.AboveNormal,
            IsBackground = true
        };

        _rxThread.Start();

        _auto = false;

        Close();

        return false;
    }

    /// <summary>
    ///     Closes the com port.
    /// </summary>
    [UsedImplicitly]
    public void Close()
    {
        if (_online)
        {
            _auto = false;
            InternalClose();
            _rxException = null;
        }
    }

    private void InternalClose()
    {
        PInvoke.CancelIo(_hPort);
        if (_rxThread != null)
        {
            _rxThread.Abort();
            _rxThread = null;
        }

        _hPort.Close();

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
    [UsedImplicitly]
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
    private void ThrowException(string reason)
    {
        if (Thread.CurrentThread == _rxThread) throw new CommPortException(reason);
        if (_online) InternalClose();

        if (_rxException == null) throw new CommPortException(reason);
        throw new CommPortException(_rxException);
    }

    /// <summary>
    ///     Queues bytes for transmission.
    /// </summary>
    /// <param name="toSend">Array of bytes to be sent</param>
    [UsedImplicitly]
    public unsafe void Write(byte[] toSend)
    {
        CheckOnline();
        CheckResult();
        _writeCount = toSend.GetLength(0);

        fixed (byte* ptr = toSend)
        fixed (NativeOverlapped* overlapped = &_writeOverlapped)
        {
            uint sent;
            if (PInvoke.WriteFile(_hPort, ptr, (uint)_writeCount, &sent, overlapped))
            {
                _writeCount -= (int)sent;
            }
            else
            {
                if (Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_IO_PENDING)
                    ThrowException("Unexpected failure");
            }
        }
    }

    /// <summary>
    ///     Queues string for transmission.
    /// </summary>
    /// <param name="toSend">Array of bytes to be sent</param>
    [UsedImplicitly]
    public void Write(string toSend)
    {
        Write(new ASCIIEncoding().GetBytes(toSend));
    }

    /// <summary>
    ///     Queues a single byte for transmission.
    /// </summary>
    /// <param name="toSend">Byte to be sent</param>
    [UsedImplicitly]
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
    [UsedImplicitly]
    public void Write(char toSend)
    {
        Write(toSend.ToString());
    }

    /// <summary>
    ///     Queues string with a new line ("\r\n") for transmission.
    /// </summary>
    /// <param name="toSend">Array of bytes to be sent</param>
    [UsedImplicitly]
    public void WriteLine(string toSend)
    {
        Write(new ASCIIEncoding().GetBytes(toSend + Environment.NewLine));
    }

    private void CheckResult()
    {
        if (_writeCount <= 0) return;

        if (PInvoke.GetOverlappedResult(_hPort, _writeOverlapped, out var sent, _checkSends))
        {
            _writeCount -= (int)sent;
            if (_writeCount != 0) ThrowException("Send Timeout");
        }
        else
        {
            if (Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_IO_PENDING) ThrowException("Unexpected failure");
        }
    }

    /// <summary>
    ///     Sends a protocol byte immediately ahead of any queued bytes.
    /// </summary>
    /// <param name="toSend">Byte to send</param>
    /// <returns>False if an immediate byte is already scheduled and not yet sent</returns>
    public void SendImmediate(byte toSend)
    {
        CheckOnline();
        if (!PInvoke.TransmitCommChar(_hPort, new CHAR(toSend))) ThrowException("Transmission failure");
    }

    /// <summary>
    ///     Override this to process received bytes.
    /// </summary>
    /// <param name="ch">The byte that was received</param>
    private void OnRxChar(byte ch)
    {
        DataReceived?.Invoke(ch);
    }

    private unsafe void ReceiveThread()
    {
        var buf = new byte[1];

        var sg = new AutoResetEvent(false);
        var ov = new NativeOverlapped
        {
            EventHandle = sg.SafeWaitHandle.DangerousGetHandle()
        };

        COMM_EVENT_MASK eventMask = 0;

        try
        {
            while (true)
            {
                if (!PInvoke.SetCommMask(
                        _hPort,
                        COMM_EVENT_MASK.EV_RXCHAR |
                        COMM_EVENT_MASK.EV_TXEMPTY |
                        COMM_EVENT_MASK.EV_CTS |
                        COMM_EVENT_MASK.EV_DSR |
                        COMM_EVENT_MASK.EV_BREAK |
                        COMM_EVENT_MASK.EV_RLSD |
                        COMM_EVENT_MASK.EV_RING |
                        COMM_EVENT_MASK.EV_ERR)
                   )
                    throw new CommPortException("IO Error [001]");

                if (!PInvoke.WaitCommEvent(_hPort, ref eventMask, &ov))
                {
                    if (Marshal.GetLastWin32Error() == (int)WIN32_ERROR.ERROR_IO_PENDING)
                        sg.WaitOne();
                    else
                        throw new CommPortException("IO Error [002]");
                }

                if ((eventMask & COMM_EVENT_MASK.EV_ERR) != 0)
                {
                    CLEAR_COMM_ERROR_FLAGS errs;

                    if (PInvoke.ClearCommError(_hPort, &errs, null))
                    {
                        var s = new StringBuilder("UART Error: ", 40);
                        if ((errs & CLEAR_COMM_ERROR_FLAGS.CE_FRAME) != 0) s = s.Append("Framing,");
                        if ((errs & CLEAR_COMM_ERROR_FLAGS.CE_BREAK) != 0) s = s.Append("Break,");
                        if ((errs & CLEAR_COMM_ERROR_FLAGS.CE_OVERRUN) != 0) s = s.Append("Overrun,");
                        if ((errs & CLEAR_COMM_ERROR_FLAGS.CE_RXOVER) != 0) s = s.Append("Receive Overflow,");
                        if ((errs & CLEAR_COMM_ERROR_FLAGS.CE_RXPARITY) != 0) s = s.Append("Parity,");

                        s.Length = s.Length - 1;

                        throw new CommPortException(s.ToString());
                    }

                    throw new CommPortException("IO Error [003]");
                }

                if ((eventMask & COMM_EVENT_MASK.EV_RXCHAR) != 0)
                {
                    uint gotBytes;
                    do
                    {
                        fixed (byte* ptrBuffer = buf)
                        {
                            if (!PInvoke.ReadFile(_hPort, ptrBuffer, 1, &gotBytes, &ov))
                            {
                                if (Marshal.GetLastWin32Error() == (int)WIN32_ERROR.ERROR_IO_PENDING)
                                {
                                    PInvoke.CancelIo(_hPort);
                                    gotBytes = 0;
                                }
                                else
                                {
                                    throw new CommPortException("IO Error [004]");
                                }
                            }
                        }

                        if (gotBytes == 1) OnRxChar(buf[0]);
                    } while (gotBytes > 0);
                }

                if ((eventMask & COMM_EVENT_MASK.EV_TXEMPTY) != 0) TxDone?.Invoke();
                if ((eventMask & COMM_EVENT_MASK.EV_BREAK) != 0) Break?.Invoke();

                MODEM_STATUS_FLAGS i = 0;

                if ((eventMask & COMM_EVENT_MASK.EV_CTS) != 0) i |= MODEM_STATUS_FLAGS.MS_CTS_ON;
                if ((eventMask & COMM_EVENT_MASK.EV_DSR) != 0) i |= MODEM_STATUS_FLAGS.MS_DSR_ON;
                if ((eventMask & COMM_EVENT_MASK.EV_RLSD) != 0) i |= MODEM_STATUS_FLAGS.MS_RLSD_ON;
                if ((eventMask & COMM_EVENT_MASK.EV_RING) != 0) i |= MODEM_STATUS_FLAGS.MS_RING_ON;

                if (i != 0 || !PInvoke.GetCommModemStatus(_hPort, out var f))
                    throw new CommPortException("IO Error [005]");

                StatusChanged?.Invoke(new ModemStatus(i), new ModemStatus(f));
            }
        }
        catch (Exception e)
        {
            if (e is not ThreadAbortException)
            {
                _rxException = e;
                ThreadException?.Invoke(e);
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
            if (PInvoke.GetHandleInformation(_hPort, out f)) return true;
            ThrowException("Offline");
            return false;
        }

        if (_auto)
            if (Open())
                return true;
        ThrowException("Offline");
        return false;
    }

    public event Action<ModemStatus, ModemStatus> StatusChanged;

    public event Action<byte> DataReceived;

    public event Action TxDone;

    public event Action Break;

    public event Action<Exception> ThreadException;
}