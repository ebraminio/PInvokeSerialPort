using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace PInvokeSerialPort.Test
{
    /// <summary>
    /// Test class. 
    /// Attention: Run it just in test debug.
    /// </summary>
    [TestClass]
    public class PInvokeSerialPortTest
    {
        dynamic _sender;
        dynamic _reciever;
        StringBuilder _stringBuilder;

        public void OpenWriteDoWaitClose(Action action)
        {
            const string testSting = "test";
            _stringBuilder = new StringBuilder();
            _sender.Open();
            _reciever.Open();

            action();
            
            _sender.Write(testSting);
            Thread.Sleep(100);
            Assert.AreEqual(testSting, _stringBuilder.ToString());

            _sender.Close();
            _reciever.Close();
        }

        [TestMethod]
        public void OverallTest1()
        {
            _sender = new SerialPort("com1");
            _reciever = new SerialPort("com2");
            OpenWriteDoWaitClose(() =>
                {
                    ((SerialPort)_reciever).DataReceived += x => _stringBuilder.Append((char)x);
                });
        }

        [TestMethod]
        public void OverallTest2()
        {
            _sender = new System.IO.Ports.SerialPort("com1");
            
            _reciever = new SerialPort("com2");
            
            OpenWriteDoWaitClose(() =>
                {
                    ((SerialPort)(object)_reciever).DataReceived += x => _stringBuilder.Append((char)x);
                });
        }

        [TestMethod]
        public void OverallTest3()
        {
            _sender = new SerialPort("com1");
            _reciever = new System.IO.Ports.SerialPort("com2");

            OpenWriteDoWaitClose(() =>
                {
                    ((System.IO.Ports.SerialPort)_reciever).DataReceived += (x, y) => _stringBuilder.Append(_reciever.ReadExisting());
                });
        }

        [TestMethod]
        public void OverallTest4() // this is not really a PInvokeSerialTest :D
        {
            _sender = new System.IO.Ports.SerialPort("com1");
            _reciever = new System.IO.Ports.SerialPort("com2");
            
            OpenWriteDoWaitClose(() =>
                {
                    ((System.IO.Ports.SerialPort)_reciever).DataReceived += (x, y) => _stringBuilder.Append(_reciever.ReadExisting());
                });
        }
    }
}
