using System;
using System.Collections;

//TODO.....

namespace com.dalsemi.onewire.adapter
{
    internal class SerialService
    {
        public static IEnumerator SerialPortIdentifiers { get; internal set; }
        public bool DTR { get; internal set; }
        public bool RTS { get; internal set; }
        public string PortName { get; internal set; }
        public int BaudRate { get; internal set; }

        internal void write(char send_byte)
        {
            throw new NotImplementedException();
        }

        internal void write(char[] send_buffer)
        {
            throw new NotImplementedException();
        }

        internal void write(sbyte[] send_buffer)
        {
            throw new NotImplementedException();
        }

        internal char[] readWithTimeout(int v)
        {
            throw new NotImplementedException();
        }

        internal void flush()
        {
            throw new NotImplementedException();
        }

        internal void sendBreak(int v)
        {
            throw new NotImplementedException();
        }

        internal void openPort()
        {
            throw new NotImplementedException();
        }

        internal bool beginExclusive(bool v)
        {
            throw new NotImplementedException();
        }

        internal void endExclusive()
        {
            throw new NotImplementedException();
        }

        internal bool haveExclusive()
        {
            throw new NotImplementedException();
        }

        internal static SerialService getSerialService(string newPortName)
        {
            throw new NotImplementedException();
        }

        internal void closePort()
        {
            throw new NotImplementedException();
        }
    }
}