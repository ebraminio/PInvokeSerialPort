namespace Nefarius.Peripherals.SerialPort
{
    /// <summary>
    ///     Stop bit settings
    /// </summary>
    public enum StopBits
    {
        /// <summary>
        ///     Line is asserted for 1 bit duration at end of each character
        /// </summary>
        One = 0,

        /// <summary>
        ///     Line is asserted for 1.5 bit duration at end of each character
        /// </summary>
        OnePointFive = 1,

        /// <summary>
        ///     Line is asserted for 2 bit duration at end of each character
        /// </summary>
        Two = 2
    }
}