using System;
using PInvokeSerialPort;

namespace PInvokeSerialPort.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var serialPort = new SerialPort("com1", 14400);
            serialPort.DataReceived += x => Console.Write((char)x);
            serialPort.Open();
            while (true)
            {
                serialPort.Write(Console.ReadKey().KeyChar);
            }
        }
    }
}
