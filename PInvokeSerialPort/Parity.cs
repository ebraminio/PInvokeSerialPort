namespace PInvokeSerialPort
{
    /// <summary>
    /// Parity settings
    /// </summary>
    public enum Parity
    {
        /// <summary>
        /// Characters do not have a parity bit.
        /// </summary>
        None = 0,
        /// <summary>
        /// If there are an odd number of 1s in the data bits, the parity bit is 1.
        /// </summary>
        Odd = 1,
        /// <summary>
        /// If there are an even number of 1s in the data bits, the parity bit is 1.
        /// </summary>
        Even = 2,
        /// <summary>
        /// The parity bit is always 1.
        /// </summary>
        Mark = 3,
        /// <summary>
        /// The parity bit is always 0.
        /// </summary>
        Space = 4
    };
}