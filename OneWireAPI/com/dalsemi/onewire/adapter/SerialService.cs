using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace com.dalsemi.onewire.adapter
{
    using com.dalsemi.onewire.logging;

    internal partial class SerialService : IDisposable
    {
        /// <summary>
        /// The serial port name of this object (e.g. COM1, /dev/ttyS0) </summary>
        private readonly string comPortName;

        /// <summary>
        /// The serial port object for setting serial port parameters </summary>
        private SerialDevice serialPort = null;

        /// <summary>
        /// Reader
        /// </summary>
        private DataReader reader = null;

        /// <summary>
        /// Writer
        /// </summary>
        private DataWriter writer = null;

        /// <summary>
        /// The hash code of the thread that currently owns this serial port </summary>
        //private int currentThreadHash = 0;
        /// <summary>
        /// temporary array, used for converting characters to bytes </summary>
        private byte[] tempArray = new byte[128];

        /// <summary>
        /// Vector of thread hash codes that have done an open but no close </summary>
        private readonly List<int> users = new List<int>();

        /// <summary>
        /// Vector of serial port ID strings (i.e. "COM1", "COM2", etc) </summary>
        private static readonly List<string> vPortIDs = new List<string>(2);

        /// <summary>
        /// static list of threadIDs to the services they are using </summary>
        private static Hashtable knownServices = new Hashtable();

        /// <summary>
        /// static list of all unique SerialService classes </summary>
        private static Hashtable uniqueServices = new Hashtable();

        public SerialDevice port
        {
            get
            {
                return serialPort;
            }
        }

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
                OneWireEventSource.Log.Debug("SerialService.getSerialService called: strComPort=" + strComPort);

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
                    return serialPort.IsRequestToSendEnabled;
                }
            }
            set
            {
                lock (this)
                {
                    serialPort.IsRequestToSendEnabled = true;
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
                OneWireEventSource.Log.Debug("sendBreak " + duration + "ms");

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

                        OneWireEventSource.Log.Debug("SerialService.setBaudRate: baudRate=" + value);
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

                    List<string> list = new List<string>();
                    foreach (var item in DeviceList)
                    {
                        list.Add(item.Id);
                        OneWireEventSource.Log.Debug("\t" + item.Id);
                    }

                    return (list.GetEnumerator());
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
            //OneWireEventSource.Log.Debug("SerialService.flush");

            if (!PortOpen)
            {
                throw new System.IO.IOException("Port Not Open");
            }

            //await serialPort.OutputStream.FlushAsync();
            // thows: "Exception thrown: 'System.NotImplementedException' in OneWireAPI.dll"
        }

        public virtual void write(byte data)
        {
            var t = Task.Run(async () =>
            {
                writer.WriteByte(data);
                await writer.StoreAsync();
                //debug.Debug.debug(serialPort.PortName + " Tx", new byte[] { data });
                //OneWireEventSource.Log.Debug(serialPort.PortName + " Tx: " + com.dalsemi.onewire.utils.Convert.toHexString(data));
            });
            t.Wait();
            if (t.Status != TaskStatus.RanToCompletion)
            {
                OneWireEventSource.Log.Critical("Error writing to serial port!");
            }
        }

        public virtual void write(byte[] data)
        {
            var t = Task.Run(async () =>
            {
                writer.WriteBytes(data);
                await writer.StoreAsync();
                //debug.Debug.debug(serialPort.PortName + " Tx", data);
                //OneWireEventSource.Log.Debug(serialPort.PortName + " Tx: " + com.dalsemi.onewire.utils.Convert.toHexString(data, " "));
            });
            t.Wait();
            if (t.Status != TaskStatus.RanToCompletion)
            {
                OneWireEventSource.Log.Critical("Error writing to serial port!");
            }
        }

        /// <summary>
        /// read with timeout implementation
        /// </summary>
        /// <param name="size"></param>
        /// <returns>byte[]</returns>
        public virtual byte[] readWithTimeout(uint size)
        {
            try
            {
                // determine number of bytes to load, if any
                byte[] res = null;

                if (size > reader.UnconsumedBufferLength)
                {
                    uint len = size - reader.UnconsumedBufferLength;

                    var t = Task<uint>.Run(async () =>
                    {
                        CancellationTokenSource cts = new CancellationTokenSource(1000);
                        DataReaderLoadOperation read = reader.LoadAsync(len);
                        return await read.AsTask<uint>(cts.Token);
                    });
                    t.Wait();
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        if (t.Result > 0)
                        {
                            res = new byte[size];
                            for (var i = 0; i < size; i++)
                            {
                                if (reader.UnconsumedBufferLength > 0)
                                {
                                    res[i] = reader.ReadByte();
                                }
                                else
                                {
                                    byte[] tmp = new byte[i + 1];
                                    Array.Copy(res, 0, tmp, 0, i + 1);
                                    return tmp;
                                }
                            }
                            //debug.Debug.debug(serialPort.PortName + " Rx", result);
                            //OneWireEventSource.Log.Debug(serialPort.PortName + " Rx: " + com.dalsemi.onewire.utils.Convert.toHexString(result, " "));
                        }
                    }
                    return res;
                }

                res = new byte[size];
                for (var i = 0; i < size; i++)
                {
                    if (reader.UnconsumedBufferLength > 0)
                    {
                        res[i] = reader.ReadByte();
                    }
                    else
                    {
                        byte[] tmp = new byte[i + 1];
                        Array.Copy(res, 0, tmp, 0, i + 1);
                        return tmp;
                    }
                }
                //debug.Debug.debug(serialPort.PortName + " Rx", result);
                //OneWireEventSource.Log.Debug(serialPort.PortName + " Rx: " + com.dalsemi.onewire.utils.Convert.toHexString(result, " "));

                return res;
            }
            catch (System.Threading.Tasks.TaskCanceledException e)
            {
                OneWireEventSource.Log.Debug("readWithTimeout() Timeout!\r\n" + e.ToString());
            }
            catch (Exception e)
            {
                OneWireEventSource.Log.Debug("readWithTimeout(): " + e.ToString());
            }

            return null;
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
                OneWireEventSource.Log.Debug("SerialService.closePort");
                closePortByThreadID(Environment.CurrentManagedThreadId);
            }
        }

        public async Task<DeviceInformation> GetDeviceInformation(string PortName)
        {
            string aqs = SerialDevice.GetDeviceSelector(PortName);
            DeviceInformationCollection devList =
                await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);

            return devList[0];
        }

        public virtual void openPort()
        {
            OneWireEventSource.Log.Debug("SerialService.openPort() called");
            OneWireEventSource.Log.Debug("SerialService.openPort: System.Enivronment.CurrentManagedThreadId()=" + Environment.CurrentManagedThreadId);

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
                var t = Task<SerialDevice>.Run(async () =>
                {
                    SerialDevice device;
                    DeviceInformation devInfo = null;

                    if (comPortName.Contains("\\"))
                    {
                        device = await SerialDevice.FromIdAsync(comPortName);

                        if (device == null)
                            throw new System.IO.IOException("Failed to open PortName: " + comPortName);
                    }
                    else
                    {
                        devInfo = await GetDeviceInformation(comPortName);

                        if (devInfo == null)
                            throw new System.IO.IOException("Failed to open PortName: " + comPortName);

                        device = await SerialDevice.FromIdAsync(devInfo.Id);
                    }

                    if (device == null)
                    {
                        var deviceAccessStatus = DeviceAccessInformation.CreateFromId(devInfo.Id).CurrentStatus;
                        if (deviceAccessStatus == DeviceAccessStatus.DeniedByUser)
                        {
                            OneWireEventSource.Log.Critical("Access to the device was blocked by the user : " + devInfo.Id);
                        }
                        else if (deviceAccessStatus == DeviceAccessStatus.DeniedBySystem)
                        {
                            OneWireEventSource.Log.Critical("Possible failure with Package.appamnifgest declaration");
                            OneWireEventSource.Log.Critical("Check your Package.appxmanifest Capabilities section:");
                            OneWireEventSource.Log.Critical("<DeviceCapability Name = \"serialcommunication\">");
                            OneWireEventSource.Log.Critical("  <Device Id = \"any\">");
                            OneWireEventSource.Log.Critical("    <Function Type = \"name:serialPort\"/>");
                            OneWireEventSource.Log.Critical("  </Device>");
                            OneWireEventSource.Log.Critical("</DeviceCapability>");
                        }
                        else
                        {
                            OneWireEventSource.Log.Critical("Unkown error, possibly open by another app : " + devInfo.Id);
                        }

                        throw new System.IO.IOException("Failed to open (" + comPortName + ") check log file!");
                    }

                    writer = new DataWriter(device.OutputStream);
                    reader = new DataReader(device.InputStream);
                    reader.InputStreamOptions = InputStreamOptions.Partial;

                    return device;
                });

                t.Wait();
                if (t.Status != TaskStatus.RanToCompletion)
                {
                    throw new System.IO.IOException("Failed to open (" + comPortName + ")");
                }

                serialPort = t.Result;

                // flow i/o
                // This generates an exception on Keyspan USA-19HS
                //TODO                    serialPort.Handshake = SerialHandshake.None;

                // bug workaround
                write((byte)0);

                // settings
                serialPort.ReadTimeout = new System.TimeSpan(0, 0, 0, 0, 100);
                serialPort.WriteTimeout = new System.TimeSpan(0, 0, 0, 0, 100);

                // set baud rate
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.Parity = SerialParity.None;

                // power adapter
                serialPort.IsDataTerminalReadyEnabled = true;
                serialPort.IsRequestToSendEnabled = true;

                OneWireEventSource.Log.Debug("SerialService.openPort: Port Openend (" + comPortName + ")");
            }
            catch (Exception e)
            {
                this.Close();
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
                OneWireEventSource.Log.Debug("SerialService.closePortByThreadID(ManagedThreadId), ManagedThreadId" + t);

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
                    OneWireEventSource.Log.Debug("SerialService.closePortByThreadID(Thread): calling serialPort.removeEventListener() and .close()");

                    this.Close();
                }
                else
                {
                    OneWireEventSource.Log.Debug("SerialService.closePortByThreadID(Thread): can't close port, owned by another thread");
                }
            }
        }

        ~SerialService()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (serialPort != null)
                {
                    serialPort.Dispose();
                    serialPort = null;
                }
            }
        }
    }
}