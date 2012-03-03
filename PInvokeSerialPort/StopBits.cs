namespace PInvokeSerialPort
{
    /// <summary>
    /// Stop bit settings
    /// </summary>
    public enum StopBits
    {
        /// <summary>
        /// Line is asserted for 1 bit duration at end of each character
        /// </summary>
        one = 0,
        /// <summary>
        /// Line is asserted for 1.5 bit duration at end of each character
        /// </summary>
        onePointFive = 1,
        /// <summary>
        /// Line is asserted for 2 bit duration at end of each character
        /// </summary>
        two = 2
    };
}