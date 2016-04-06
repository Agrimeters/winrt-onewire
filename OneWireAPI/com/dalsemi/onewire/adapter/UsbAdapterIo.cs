using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace com.dalsemi.onewire.adapter
{
    /// <summary>
    /// UsbAdapterIo class handles low level IO with USB device
    /// </summary>
    public class UsbAdapterIo : IDisposable
    {
        /// <summary>
        /// Result code
        /// </summary>
        private const ErrorResult RESULT_SUCCESS = 0;

        /// <summary>
        /// Flag to enable debug messages
        /// </summary>
        private bool doDebugMessages = true;

        /// <summary>
        /// Normal Search, all devices participate </summary>
        private const byte NORMAL_SEARCH_CMD = 0xF0;

        /// <summary>
        /// Conditional Search, only 'alarming' devices participate </summary>
        private const byte ALARM_SEARCH_CMD = 0xEC;

        /// <summary>
        /// Object used for lock
        /// </summary>
        private Object syncObject;

        /// <summary>
        /// handle of USB Device
        /// </summary>
        public UsbDevice usbDevice { get; private set; }

        /// <summary>
        /// Instance ID used to open device
        /// </summary>
        private string deviceId { get; set; }

        /// <summary>
        /// USB State
        /// </summary>
        private UsbAdapterState usbState { get; set; }

        /// <summary>
        /// OneWire State
        /// </summary>
        private OneWireState owState { get; set; }

        /// <summary>
        /// Event used to read interrupt only once
        /// </summary>
        private AutoResetEvent ReadResultWait;

        /// <summary>
        /// Pipe index of the pipe we that we registered for. Only valid if registeredInterrupt is true
        /// </summary>
        private UInt32 registeredInterruptPipeIndex;

        /// <summary>
        /// Registered Interrupt
        /// </summary>
        private bool registeredInterrupt;

        /// <summary>
        /// Interupt Event Handler
        /// </summary>
        private TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs> interruptEventHandler = null;

        public ErrorResult LastError { get; private set; }

        /// <summary>
        /// UsbAdapterIo Constructor called by UsbAdapter class
        /// </summary>
        /// <param name="usbDevice"></param>
        /// <param name="usbstate"></param>
        /// <param name="owstate"></param>
        internal UsbAdapterIo(UsbDevice usbdevice, string deviceid, UsbAdapterState usbstate, OneWireState owstate)
        {
            usbDevice = usbdevice;
            deviceId = deviceid;
            usbState = usbstate;
            owState = owstate;

            syncObject = new object();
            ReadResultWait = new AutoResetEvent(false);
        }

        /// <summary>
        /// Issue Control RESET_DEVICE to USB device
        /// </summary>
        public void Control_ResetDevice()
        {
            SendCommand(
                Ds2490.CMD_TYPE.CONTROL,
                Ds2490.CTL.RESET_DEVICE,
                0,
                "USB Communication: RESET_DEVICE");

            bool DeviceDetected;
            if (RESULT_SUCCESS != ReadStatus(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();
        }

        /// <summary>
        /// Halt execution when DONE
        /// </summary>
        public ErrorResult Control_HalExecWhenDone(out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.CONTROL,
                Ds2490.CTL.HALT_EXE_DONE,
                0,
                "USB Communication: HALT_EXE_DONE");

            if (RESULT_SUCCESS != ReadStatus(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();
            return LastError;
        }

        /// <summary>
        /// Halt execution when IDLE
        /// </summary>
        public ErrorResult Control_HalExecWhenIdle(out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.CONTROL,
                Ds2490.CTL.HALT_EXE_IDLE,
                0,
                "USB Communication: HALT_EXE_IDLE");

            if (RESULT_SUCCESS != ReadStatus(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();
            return LastError;
        }

        /// <summary>
        /// Halt execution when IDLE
        /// </summary>
        public ErrorResult Control_ResumeExec(out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.CONTROL,
                Ds2490.CTL.RESUME_EXE,
                0,
                "USB Communication: RESUME_EXE");

            if (RESULT_SUCCESS != ReadStatus(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();
            return LastError;
        }

        /// <summary>
        /// Issue Communication SetDuration to USB device
        /// </summary>
        /// <param name="type">0 = 5V, 1 = 12V</param>
        /// <param name="duration"></param>
        /// <param name="description"></param>
        public ErrorResult Comm_SetDuration(ushort type, byte duration, string description, out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                (uint)(Ds2490.COMM.SET_DURATION | Ds2490.COMM.IM | type | Ds2490.COMM.NTF),
                (uint)duration,
                "USB Communication: SetDuration - " + description);

            if (RESULT_SUCCESS != ReadResult(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();

            return LastError;
        }

        /// <summary>
        /// Issue Communication 1-Wire RESET to USB device
        /// </summary>
        /// <param name="DeviceDetect"></param>
        /// <returns>Error Result</returns>
        public ErrorResult Comm_OneWireReset(out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                Ds2490.COMM.ONEWIRE_RESET | Ds2490.COMM.F | Ds2490.COMM.IM | Ds2490.COMM.SE | Ds2490.COMM.NTF,
                (usbState.ReqBusCommSpeed != -1) ? (byte)usbState.ReqBusCommSpeed : usbState.BusCommSpeed,
                "USB Communication: One-Wire Reset");

            if (RESULT_SUCCESS != ReadResult(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();

            return LastError;
        }

        /// <summary>
        /// Pulse
        ///
        /// This command is used to generate a strong pullup to 5V in
        /// order to provide extra power for an attached iButton device,
        /// e.g., temperature sensor, EEPROM, SHA-1, or crypto iButton.
        /// The pulse duration is determined by the value in the mode register.
        ///
        /// The HALT EXECUTION WHEN DONE or HALT EXECUTION WHEN IDLE control
        /// commands are used to terminate an infinite duration pulse.
        /// </summary>
        public ErrorResult Comm_Pulse(string description, out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                Ds2490.COMM.PULSE | Ds2490.COMM.IM | Ds2490.COMM.NTF,
                (usbState.ReqBusCommSpeed != -1) ? (byte)usbState.ReqBusCommSpeed : usbState.BusCommSpeed,
                "USB Communication: Pulse - " + description);

            if (RESULT_SUCCESS != ReadResult(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();

            return LastError;
        }

        /// <summary>
        /// Pulse
        ///
        /// This command is used to generate a strong pullup to 5V in
        /// order to provide extra power for an attached iButton device,
        /// e.g., temperature sensor, EEPROM, SHA-1, or crypto iButton.
        /// The pulse duration is determined by the value in the mode register.
        ///
        /// The HALT EXECUTION WHEN DONE or HALT EXECUTION WHEN IDLE control
        /// commands are used to terminate an infinite duration pulse.
        /// </summary>
        public ErrorResult Comm_SearchAccess(string description, bool alarm_only, ushort num_devices, out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                Ds2490.COMM.SEARCH_ACCESS |
                Ds2490.COMM.IM |  // Execute Immediate
                Ds2490.COMM.SM |  // searches for and reports ROM Ids without really
                                  //accessing a particular device.
                Ds2490.COMM.F |   // clears the buffers in case an error occurred
                                  // during the execution of the previous command;
                                  // requires that ICP = 0 in the previous command.
                Ds2490.COMM.RTS | // returns the discrepancy information to the host
                                  // if SM = 1 and there are more devices than could
                                  // be discovered in the current pass.
                Ds2490.COMM.NTF,  // return ErrorResult

                (ushort)
                ((num_devices << 8) |  // the maximum number of devices
                                       // to be discovered in a single
                                       // command call.  A value of 0x00
                                       // indicates that all devices on the 1-Wire
                                       // Network are to be discovered.
                ((alarm_only) ? ALARM_SEARCH_CMD : NORMAL_SEARCH_CMD)),
            // 1-Wire command (Search ROM or
            // Conditional Search ROM)
            "USB Communication: SearchAccess - " + description);

            if (RESULT_SUCCESS != ReadResult(out DeviceDetected))
                PrintErrorResult();

            //TODO            usbState.PrintState();

            return LastError;
        }

        /// <summary>
        /// Enable Pulse
        ///
        /// This command is used to enable or disable a 1-Wire strong pullup pulse to 5V.
        /// One bit position in the parameter byte is used to control the enabled/disabled
        /// state for the pulse. The pulse is enabled when the respective bit is set to a 1
        /// and disabled when set to a 0. The DS2490 power-up default state for strong
        /// pullup is disabled
        /// </summary>
        /// <param name="rail">Pass in Ds2490.ENABLEPULSE_PRGE, or Ds2490.ENABLEPULSE_SPUE</param>
        /// <param name="description"></param>
        /// <returns></returns>
        public ErrorResult Mode_EnablePulse(byte rail, string description, out bool DeviceDetected)
        {
            SendCommand(
                Ds2490.CMD_TYPE.MODE,
                Ds2490.Mode.PULSE_EN | Ds2490.COMM.NTF,
                rail,
                "USB Mode: ENABLE_PULSE - " + description);

            ErrorResult Status = ReadStatus(out DeviceDetected);
            if (RESULT_SUCCESS != Status)
                PrintErrorResult();

            //TODO            usbState.PrintState();

            return LastError;
        }

        /// <summary>
        /// General function to issue command to DS2490 USB device
        /// </summary>
        /// <param name="req"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private uint SendCommand(byte req, uint value, uint index, string description)
        {
            uint result = 0;

            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: " + description);
            }

            try
            {
                if (usbDevice == null)
                {
                    throw new System.IO.IOException("Port Not Open");
                }

                var t = Task<uint>.Run(async () =>
                {
                    UsbSetupPacket setupPacket = new UsbSetupPacket
                    {
                        RequestType = new UsbControlRequestType
                        {
                            Direction = UsbTransferDirection.Out,
                            Recipient = UsbControlRecipient.Device,
                            ControlTransferType = UsbControlTransferType.Vendor
                        },
                        Request = req,
                        Value = value,
                        Index = index,
                        Length = 0
                    };

                    return await usbDevice.SendControlOutTransferAsync(setupPacket);
                });

                t.Wait();

                if (t.Status != TaskStatus.RanToCompletion)
                {
                    Debug.WriteLine("UsbAdapterIo: " + description + " failed to send packet");
                }
                return t.Result;
            }
            catch (System.IO.IOException e)
            {
                if (doDebugMessages)
                {
                    Debug.WriteLine("UsbAdapterIo: " + description + ": " + e);
                }
            }

            return result;
        }

        /// <summary>
        /// General function to issue command to DS2490 USB device
        /// </summary>
        /// <param name="req"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<byte[]> BulkEp_Read(int index, string description)
        {
            try
            {
                if (usbDevice == null)
                {
                    throw new System.IO.IOException("Port Not Open");
                }

                var stream = usbDevice.DefaultInterface.BulkInPipes[index].InputStream;
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync(usbState.OneWireReadBufferStatus);

                    byte[] result = new byte[usbState.OneWireReadBufferStatus];

                    reader.ReadBytes(result);

                    if (doDebugMessages)
                    {
                        Debug.WriteLine("DEBUG: BulkEp_Read - " + description);
                        for (int i = 0; i < result.Length; i++)
                            Debug.Write(" " + result[i].ToString("X"));
                        Debug.WriteLine("");
                    }

                    return result;
                }
            }
            catch (System.IO.IOException e)
            {
                if (doDebugMessages)
                {
                    Debug.WriteLine("BulkEp_Read: " + description + ": " + e);
                }
            }

            return null;
        }

        /// <summary>
        /// General function to issue command to DS2490 USB device
        /// </summary>
        /// <param name="req"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<uint> BulkEp_Write(int index, byte[] data, string description)
        {
            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: BulkEp_Write - " + description);
                for (int i = 0; i < data.Length; i++)
                    Debug.Write(" " + data[i].ToString("X"));
                Debug.WriteLine("");
            }

            try
            {
                if (usbDevice == null)
                {
                    throw new System.IO.IOException("Port Not Open");
                }

                var stream = usbDevice.DefaultInterface.BulkOutPipes[index].OutputStream;
                var writer = new DataWriter(stream);
                writer.WriteBytes(data);

                return await writer.StoreAsync();
            }
            catch (System.IO.IOException e)
            {
                if (doDebugMessages)
                {
                    Debug.WriteLine("BulkEp_Write: " + description + ": " + e);
                }
            }

            return 0;
        }

        /// <summary>
        /// Main Interrupt Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnStatusChangeEventOnce(UsbInterruptInPipe sender, UsbInterruptInEventArgs eventArgs)
        {
            IBuffer buffer = eventArgs.InterruptData;

            if (buffer.Length > 0)
            {
                DataReader reader = DataReader.FromBuffer(buffer);

                usbState.OnStateUpdate(reader, buffer.Length);

                ReadResultWait.Set();
            }
        }

        /// <summary>
        /// Main Interrupt Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnStatusChangeEventResult(UsbInterruptInPipe sender, UsbInterruptInEventArgs eventArgs)
        {
            IBuffer buffer = eventArgs.InterruptData;

            if (buffer.Length > 0)
            {
                DataReader reader = DataReader.FromBuffer(buffer);

                usbState.OnStateUpdate(reader, buffer.Length);

                if (buffer.Length > 0x10)
                    ReadResultWait.Set();
            }
        }

        /// <summary>
        /// Read Status data
        /// </summary>
        /// <param name="nResultRegisters"></param>
        public ErrorResult ReadStatus(out bool DeviceDetected)
        {
            lock (syncObject)
            {
                interruptEventHandler = new TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs>(this.OnStatusChangeEventOnce);

                usbState.Updated = false;
                usbState.CommResultCodes = null;

                RegisterForInterruptEvent(Ds2490.Pipe.InterruptInPipeIndex, interruptEventHandler);

                while (!usbState.Updated)
                    ReadResultWait.WaitOne();

                UnregisterFromInterruptEvent();

                LastError = GetErrorResult(out DeviceDetected);
                return LastError;
            }
        }

        /// <summary>
        /// Read Result data
        /// </summary>
        /// <param name="nResultRegisters"></param>
        public ErrorResult ReadResult(out bool DeviceDetected)
        {
            lock (syncObject)
            {
                interruptEventHandler = new TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs>(this.OnStatusChangeEventResult);

                usbState.Updated = false;
                usbState.CommResultCodes = null;

                RegisterForInterruptEvent(Ds2490.Pipe.InterruptInPipeIndex, interruptEventHandler);

                while (!usbState.Updated)
                    ReadResultWait.WaitOne();

                UnregisterFromInterruptEvent();

                LastError = GetErrorResult(out DeviceDetected);
                return LastError;
            }
        }

        public ErrorResult GetErrorResult(out bool DeviceDetected)
        {
            DeviceDetected = false;

            if (usbState.CommResultCodes != null)
            {
                foreach (ErrorResult item in usbState.CommResultCodes)
                {
                    if ((byte)item == Ds2490.ONEWIREDEVICEDETECT)
                    {
                        DeviceDetected = true;
                        continue;
                    }
                    return item;
                }
            }
            return 0;
        }

        /// <summary>
        /// Register for the interrupt that is triggered when the device sends an interrupt to us
        ///
        /// The DefaultInterface on the the device is the first interface on the device. We navigate to
        /// the InterruptInPipes because that collection contains all the interrupt in pipes for the
        /// selected interface setting.
        ///
        /// Each pipe has a property that links to an EndpointDescriptor. This descriptor can be used to find information about
        /// the pipe (e.g. type, id, etc...). The EndpointDescriptor trys to mirror the EndpointDescriptor that is defined in the Usb Spec.
        ///
        /// The function also saves the event token so that we can unregister from the even later on.
        /// </summary>
        /// <param name="pipeIndex">The index of the pipe found in UsbInterface.InterruptInPipes. It is not the endpoint number</param>
        /// <param name="eventHandler">Event handler that will be called when the event is raised</param>
        private void RegisterForInterruptEvent(UInt32 pipeIndex, TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs> eventHandler)
        {
            var interruptInPipes = usbDevice.DefaultInterface.InterruptInPipes;

            if (!registeredInterrupt && (pipeIndex < interruptInPipes.Count))
            {
                var interruptInPipe = interruptInPipes[(int)pipeIndex];

                registeredInterrupt = true;
                registeredInterruptPipeIndex = pipeIndex;

                // Save the interrupt handler so we can use it to unregister
                interruptEventHandler = eventHandler;

                interruptInPipe.DataReceived += interruptEventHandler;
            }
        }

        /// <summary>
        /// Unregisters from the interrupt event that was registered for in the RegisterForInterruptEvent();
        /// </summary>
        private void UnregisterFromInterruptEvent()
        {
            if (registeredInterrupt)
            {
                // Search for the correct pipe that we know we used to register events
                var interruptInPipe = usbDevice.DefaultInterface.InterruptInPipes[(int)registeredInterruptPipeIndex];
                interruptInPipe.DataReceived -= interruptEventHandler;

                registeredInterrupt = false;
            }
        }

        /// <summary>
        /// Decode the Error Result
        /// </summary>
        /// <param name="value"></param>
        public void PrintErrorResult()
        {
            ErrorResult result = LastError;

            if ((result & ErrorResult.NRS) == ErrorResult.NRS)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET did not reveal a Presence Pulse. SET PATH command did not get a Presence Pulse from the branch that was to be connected. No response from one or more ROM ID bits during a SEARCH ACCESS command.");
            }
            if ((result & ErrorResult.SH) == ErrorResult.SH)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET revealed a short to the 1-Wire bus or the SET PATH command could not successfully connect a branch due to a short.");
            }
            if ((result & ErrorResult.APP) == ErrorResult.APP)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET revealed an Alarming Presence Pulse.");
            }
            if ((result & ErrorResult.VPP) == ErrorResult.VPP)
            {
                Debug.WriteLine("Error Result: During a PULSE with TYPE=1 or WRITE EPROM command the 12V programming pulse not seen on 1-Wire bus.");
            }
            if ((result & ErrorResult.CMP) == ErrorResult.CMP)
            {
                Debug.WriteLine("Error Result: Error when reading the confirmation byte with a SET PATH command. There was a difference between the byte written and then read back with a BYTE I/O command,");
            }
            if ((result & ErrorResult.CRC) == ErrorResult.CRC)
            {
                Debug.WriteLine("Error Result: A CRC error occurred when executing one of the following commands: WRITE SRAM PAGE, READ CRC PROT PAGE, or READ REDIRECT PAGE W/CRC.");
            }
            if ((result & ErrorResult.RDP) == ErrorResult.RDP)
            {
                Debug.WriteLine("Error Result: A READ REDIRECT PAGE WITH/CRC encountered a page that is redirected.");
            }
            if ((result & ErrorResult.EOS) == ErrorResult.EOS)
            {
                Debug.WriteLine("Error Result: A SEARCH ACCESS with SM = 1 ended sooner than expected reporting less ROM ID’s than specified in the “number of devices” parameter.");
            }
        }

        [Flags]
        public enum ErrorResult
        {
            /// <summary>
            /// A value of 1 indicates an error with one of the following: 1-WIRE RESET
            /// did not reveal a Presence Pulse. SET PATH command did not get a Presence
            /// Pulse from the branch that was to be connected. No response from one or
            /// more ROM ID bits during a SEARCH ACCESS command.
            /// </summary>
            NRS = 0x01,

            /// <summary>
            /// A value of 1 indicates that a 1-WIRE RESET revealed a short to the 1-Wire
            /// bus or the SET PATH command could not successfully connect a branch due
            /// to a short.
            /// </summary>
            SH = 0x02,

            /// <summary>
            /// A value of 1 indicates that a 1-WIRE RESET revealed an Alarming Presence Pulse.
            /// </summary>
            APP = 0x04,

            /// <summary>
            /// During a PULSE with TYPE=1 or WRITE EPROM command the 12V programming pulse
            /// not seen on 1-Wire bus
            /// </summary>
            VPP = 0x08,

            /// <summary>
            /// A value of 1 indicates an error with one of the following: Error when reading
            /// the confirmation byte with a SET PATH command. There was a difference between
            /// the byte written and then read back with a BYTE I/O command
            /// </summary>
            CMP = 0x10,

            /// <summary>
            /// A value of 1 indicates that a CRC error occurred when executing one of the
            /// following commands: WRITE SRAM PAGE, READ CRC PROT PAGE, or READ REDIRECT PAGE W/CRC.
            /// </summary>
            CRC = 0x20,

            /// <summary>
            /// A value of 1 indicates that a READ REDIRECT PAGE WITH/CRC encountered
            /// a page that is redirected.
            /// </summary>
            RDP = 0x40,

            /// <summary>
            /// A value of 1 indicates that a SEARCH ACCESS with SM = 1 ended sooner than
            /// expected reporting less ROM ID’s than specified in the “number of devices”
            /// parameter.
            /// </summary>
            EOS = 0x80
        }

        ~UsbAdapterIo()
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
                if (ReadResultWait != null)
                {
                    ReadResultWait.Dispose();
                    ReadResultWait = null;
                }
            }
        }
    }
}