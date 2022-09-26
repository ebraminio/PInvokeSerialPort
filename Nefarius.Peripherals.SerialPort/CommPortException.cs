using System;

namespace PInvokeSerialPort
{
    /// <summary>
    /// Exception used for all errors.
    /// </summary>
    public class CommPortException : ApplicationException
    {
        /// <summary>
        /// Constructor for raising direct exceptions
        /// </summary>
        /// <param name="desc">Description of error</param>
        public CommPortException(string desc) : base(desc) { }

        /// <summary>
        /// Constructor for re-raising exceptions from receive thread
        /// </summary>
        /// <param name="e">Inner exception raised on receive thread</param>
        public CommPortException(Exception e) : base("Receive Thread Exception", e) { }
    }
}