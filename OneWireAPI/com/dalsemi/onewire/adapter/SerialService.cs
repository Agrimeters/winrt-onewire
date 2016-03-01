using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using System.Text;

namespace com.dalsemi.onewire.adapter
{
    internal partial class SerialService
    {
        private const bool DEBUG = true;
        /// <summary>
        /// The serial port name of this object (e.g. COM1, /dev/ttyS0) </summary>
        private readonly string comPortName;
        /// <summary>
        /// The serial port object for setting serial port parameters </summary>
        private SerialDevice serialPort = null;
        /// <summary>
        /// The DeviceInformation device id used to open the serial port</summary>
        private string deviceId;
        /// <summary>
        /// The input stream, for reading data from the serial port </summary>
        private DataReader serialInputStream = null;
        /// <summary>
        /// The output stream, for writing data to the serial port </summary>
        //private DataWriter serialOutputStream = null;
        /// <summary>
        /// The hash code of the thread that currently owns this serial port </summary>
        //private int currentThreadHash = 0;
        /// <summary>
        /// temporary array, used for converting characters to bytes </summary>
        private byte[] tempArray = new byte[128];
        /// <summary>
        /// used to end the Object.wait loop in readWithTimeout method </summary>
        //private bool dataAvailable = false;

        /// <summary>
        /// Vector of thread hash codes that have done an open but no close </summary>
        private readonly ArrayList users = new ArrayList(4);

        /// <summary>
        /// Vector of serial port ID strings (i.e. "COM1", "COM2", etc) </summary>
        private static readonly ArrayList vPortIDs = new ArrayList(2);
        /// <summary>
        /// static list of threadIDs to the services they are using </summary>
        private static Hashtable knownServices = new Hashtable();
        /// <summary>
        /// static list of all unique SerialService classes </summary>
        private static Hashtable uniqueServices = new Hashtable();

        /// <summary>
        /// do not use default constructor
        /// user getSerialService(String) instead.
        /// </summary>
        private SerialService()
        {
            this.comPortName = null;
            
        }

        /// <summary>
        /// this constructor only for use in the static method:
        /// getSerialService(String)
        /// </summary>
        protected internal SerialService(string strComPort)
        {
            this.comPortName = strComPort;
        }

        public static SerialService getSerialService(string strComPort)
        {
            lock (uniqueServices)
            {
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (DEBUG)
                {
                    Debug.WriteLine("SerialService.getSerialService called: strComPort=" + strComPort);
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                string strLowerCaseComPort = strComPort.ToLower();
                object o = uniqueServices[strLowerCaseComPort];
                if (o != null)
                {
                    return (SerialService)o;
                }
                else
                {
                    SerialService sps = new SerialService(strComPort);
                    uniqueServices[strLowerCaseComPort] = sps;
                    return sps;
                }
            }
        }

        public virtual bool DTR
        {
            get
            {
                lock (this)
                {
                    return serialPort.IsDataTerminalReadyEnabled;
                }
            }
            set
            {
                lock (this)
                {
                    serialPort.IsDataTerminalReadyEnabled = value;
                }
            }
        }

        public virtual bool RTS
        {
            get
            {
                lock (this)
                {
                    return ((serialPort.Handshake == SerialHandshake.RequestToSend) ? true : false);
                }
            }
            set
            {
                lock (this)
                {
                    if (value)
                    {
                        serialPort.Handshake = SerialHandshake.RequestToSend;
                    }
                    else
                    {
                        serialPort.Handshake = SerialHandshake.None;
                    }
                }
            }
        }

        /// <summary>
        /// Send a break on this serial port
        /// </summary>
        /// <param name="duration"> - break duration in ms </param>
        public virtual void sendBreak(int duration)
        {
            lock (this)
            {
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (DEBUG)
                {
                    System.Diagnostics.Debug.WriteLine("sendBreak {0}ms", duration);
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                serialPort.BreakSignalState = true;
                Thread.Sleep(duration);
                serialPort.BreakSignalState = false;
            }
        }

        public virtual int BaudRate
        {
            get
            {
                lock (this)
                {
                    return (int)serialPort.BaudRate;
                }
            }
            set
            {
                lock (this)
                {
                    if (!PortOpen)
                    {
                        throw new System.IO.IOException("Port Not Open");
                    }

                    try
                    {
                        // set baud rate
                        serialPort.BaudRate = (uint)value;
                        serialPort.DataBits = 8;
                        serialPort.StopBits = SerialStopBitCount.One;
                        serialPort.Parity = SerialParity.None;

                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                        if (DEBUG)
                        {
                            Debug.WriteLine("SerialService.setBaudRate: baudRate=" + value);
                        }
                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                    }
                    catch (System.Exception uncoe)
                    {
                        throw new System.IO.IOException("Failed to set baud rate: " + uncoe);
                    }
                }
            }
        }


        public virtual bool NotifyOnDataAvailable
        {
            set
            {
                lock (this)
                {
                    //TODO serialPort.notifyOnDataAvailable(value);
                }
            }
        }

        public static IEnumerator SerialPortIdentifiers
        {
            get
            {
                var t = Task<IEnumerator>.Run(async () =>
                {
                    string aqs = SerialDevice.GetDeviceSelector();
                    var myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);
                    DeviceInformationCollection DeviceList = myDevices;
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                    if (DEBUG)
                    {
                        foreach (var item in DeviceList)
                            Debug.WriteLine("\t" + item.Id);
                    }
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                    return ((IEnumerable<DeviceInformation>)DeviceList).GetEnumerator();
                });

                t.Wait();

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    return t.Result;
                }
                else
                {
                    throw new System.Exception("Unable to retrieve list of Serial Communication devices");
                }
            }
        }


        public virtual void flush()
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            if (DEBUG)
            {
                Debug.WriteLine("SerialService.flush");
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

            if (!PortOpen)
            {
                throw new System.IO.IOException("Port Not Open");
            }

            // thows: "Exception thrown: 'System.NotImplementedException' in OneWireAPI.dll"
            //TODO await serialOutputStream.FlushAsync();
        }

        public virtual void write(byte data)
        {
            var t = Task.Run(async () =>
            {
                using (var writer = new DataWriter(serialPort.OutputStream))
                {
                    System.Diagnostics.Debug.WriteLine("WriteTimeout: " + serialPort.WriteTimeout.Milliseconds);

                    writer.WriteByte(data);
                    var count = await writer.StoreAsync();
                    Debug.WriteLine("BytesWritten = " + count);
                    System.Diagnostics.Debug.WriteLine("\t0x{0:X02}", data);
                    writer.DetachStream();
                }
            });
            t.Wait();
            if(t.Status != TaskStatus.RanToCompletion)
            {
                Debug.WriteLine("Error writing to serial port!");
            }
        }

        public virtual void write(byte[] data)
        {
            var t = Task.Run(async() =>
            {
                using (var writer = new DataWriter(serialPort.OutputStream))
                {
                    System.Diagnostics.Debug.WriteLine("WriteTimeout: " + serialPort.WriteTimeout.Milliseconds);

                    writer.WriteBytes(data);
                    var count = await writer.StoreAsync();
                    Debug.WriteLine("BytesWritten = " + count);
                    foreach (var item in data)
                        System.Diagnostics.Debug.WriteLine("\t0x{0:X02}", item);
                    writer.DetachStream();
                }
            });
            t.Wait();
            if(t.Status != TaskStatus.RanToCompletion)
            {
                Debug.WriteLine("Error writing to serial port!");
            }
        }

        // Track Read Operation
        private CancellationTokenSource ReadCancellationTokenSource;
        private Object ReadCancelLock = new Object();

        private Boolean IsReadTaskPending;
        private uint ReadBytesCounter = 0;

        private async Task<byte[]> ReadAsync(uint ReadBufferLength, CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            // Don't start any IO if we canceled the task
            lock (ReadCancelLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Cancellation Token will be used so we can stop the task operation explicitly
                // The completion function should still be called so that we can properly handle a canceled task
                serialInputStream.InputStreamOptions = InputStreamOptions.Partial;
                loadAsyncTask = serialInputStream.LoadAsync(ReadBufferLength).AsTask(cancellationToken);
            }

            System.Diagnostics.Debug.WriteLine("ReadTimeout: " + serialPort.ReadTimeout.Milliseconds);

            byte[] bytes = null;
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                bytes = new byte[bytesRead];
                serialInputStream.ReadBytes(bytes);
            }

            Debug.WriteLine("Read completed - " + bytesRead.ToString() + " bytes were read");
            return bytes;
        }

        public virtual byte[] readWithTimeout(int length)
        {
            ReadCancellationTokenSource = new CancellationTokenSource();

            var t = Task<byte[]>.Run(async () =>
            {
                byte[] result = null;
                try
                {
                    Debug.WriteLine("Reading...");
                    IsReadTaskPending = true;
                    serialInputStream = new DataReader(serialPort.InputStream);
                    result = await ReadAsync(6, ReadCancellationTokenSource.Token);
                }
                catch(Exception exception)
                {
                    Debug.WriteLine(exception.Message.ToString());
                }
                finally
                {
                    IsReadTaskPending = false;
                    serialInputStream.DetachStream();
                    serialInputStream = null;
                }
                return result;
            });

            t.Wait();

            if (t.Status != TaskStatus.RanToCompletion)
            {
                Debug.WriteLine("Error reading from serial port!");
            }

            return t.Result;
        }

        private bool exclusive = false;

        internal bool beginExclusive(bool v)
        {
            if (!exclusive)
            {
                exclusive = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void endExclusive()
        {
            exclusive = false;
        }

        internal bool haveExclusive()
        {
            return true;
        }

        public virtual string PortName
        {
            get
            {
                lock (this)
                {
                    return serialPort.PortName;
                }
            }
        }

        public virtual bool PortOpen
        {
            get
            {
                lock (this)
                {
                    return (serialPort != null) ? true : false;
                }
            }
        }

        /// <summary>
        /// Close this serial port.
        /// </summary>
        /// <exception cref="IOException"> - if port is in use by another application </exception>
        public virtual void closePort()
        {
            lock (this)
            {
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (DEBUG)
                {
                    Debug.WriteLine("SerialService.closePort");
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                closePortByThreadID(Environment.CurrentManagedThreadId);
            }
        }

        public virtual void openPort()
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            if (DEBUG)
            {
                Debug.WriteLine("SerialService.openPort() called");
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            if (DEBUG)
            {
                Debug.WriteLine("SerialService.openPort: System.Enivronment.CurrentManagedThreadId()=" + Environment.CurrentManagedThreadId);
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            // record this thread as an owner
            if (users.IndexOf(Environment.CurrentManagedThreadId) == -1)
            {
                users.Add(Environment.CurrentManagedThreadId);
            }

            if (PortOpen)
            {
                return;
            }

            try
            {
                var t = Task<SerialDevice>.Run(async() =>
                {
                    string aqs = SerialDevice.GetDeviceSelector(comPortName);
                    DeviceInformationCollection myDevices = 
                        await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);

                    if (myDevices.Count == 0)
                        throw new System.IO.IOException("Failed to open (" + comPortName + ")");

                    deviceId = myDevices[0].Id;
                    return await SerialDevice.FromIdAsync(deviceId);
                });

                t.Wait();
                if(t.Status != TaskStatus.RanToCompletion)
                {
                    throw new System.IO.IOException("Failed to open (" + comPortName + ")");
                }

                serialPort = t.Result;

                Debug.WriteLine(serialPort.PortName + " Opened");

                // flow i/o
                serialPort.Handshake = SerialHandshake.None;

                //serialInputStream = new DataReader(serialPort.InputStream);
                //serialOutputStream = new DataWriter(serialPort.OutputStream);
                // bug workaround
                //TODO write((byte)0);

                // settings
                System.Diagnostics.Debug.WriteLine("ReadTimeout: " + serialPort.ReadTimeout.Milliseconds);
                System.Diagnostics.Debug.WriteLine("WriteTimeout: " + serialPort.WriteTimeout.Milliseconds);
                serialPort.ReadTimeout = new System.TimeSpan(0,0,0,0,100);
                serialPort.WriteTimeout = new System.TimeSpan(0,0,0,0,100);
                System.Diagnostics.Debug.WriteLine("ReadTimeout: " + serialPort.ReadTimeout.Milliseconds);
                System.Diagnostics.Debug.WriteLine("WriteTimeout: " + serialPort.WriteTimeout.Milliseconds);

                // set baud rate
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.Parity = SerialParity.None;

                serialPort.IsDataTerminalReadyEnabled = true;
                serialPort.IsRequestToSendEnabled = true;

                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (DEBUG)
                {
                    Debug.WriteLine("SerialService.openPort: Port Openend (" + comPortName + ")");
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            }
            catch (Exception e)
            {
                // close the port if we have an object
                if (serialPort != null)
                {
                    serialPort.Dispose();
                }

                serialPort = null;

                throw new System.IO.IOException("Failed to open (" + comPortName + ") :" + e);
            }
        }

        /// <summary>
        /// Allows clean up port by thread
        /// </summary>
        public virtual void closePortByThreadID(int t)
        {
            lock (this)
            {
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (DEBUG)
                {
                    Debug.WriteLine("SerialService.closePortByThreadID(ManagedThreadId), ManagedThreadId" + t);
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                // added singleUser object for case where one thread creates the adapter
                // (like the main thread), and another thread closes it (like the AWT event)
                bool singleUser = (users.Count == 1);

                // remove this thread as an owner
                users.Remove(t);

                // if this is the last owner then close the port
                if (singleUser || users.Count == 0)
                {
                    // if don't own a port then just return
                    if (!PortOpen)
                    {
                        return;
                    }

                    // close the port
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                    if (DEBUG)
                    {
                        Debug.WriteLine("SerialService.closePortByThreadID(Thread): calling serialPort.removeEventListener() and .close()");
                    }
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

                    serialPort.Dispose();
                }
                else
                {
                    //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                    if (DEBUG)
                    {
                        Debug.WriteLine("SerialService.closePortByThreadID(Thread): can't close port, owned by another thread");
                    }
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            }
        }

    }
}