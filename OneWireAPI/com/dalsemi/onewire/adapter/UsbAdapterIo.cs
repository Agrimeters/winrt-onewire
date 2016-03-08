using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Usb;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Threading;

namespace com.dalsemi.onewire.adapter
{
    /// <summary>
    /// UsbAdapterIo class handles low level IO with USB device
    /// </summary>
    public class UsbAdapterIo
    {
        /// <summary>
        /// Flag to enable debug messages
        /// </summary>
        private bool doDebugMessages = true;
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

            byte nRegs;
            ReadStatus(out nRegs);
            usbState.PrintState();
        }

        /// <summary>
        /// Issue Communication SetDuration to USB device
        /// </summary>
        /// <param name="type"></param>
        /// <param name="duration"></param>
        /// <param name="description"></param>
        public void Comm_SetDuration(ushort type, uint duration, string description)
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                (uint)(Ds2490.COMM.SET_DURATION | Ds2490.COMM.IM | type | Ds2490.COMM.NTF),
                duration,
                "USB Communication: SetDuration - " + description);

            int nRegs;
            ReadResult(out nRegs);

            if (nRegs > 0)
            {
                for (int i = 0; i < nRegs; i++)
                    Debug.WriteLine("{0:X02} ", usbState.CommResultCodes[i]);
            }

            usbState.PrintState();
        }

        /// <summary>
        /// Issue Communication 1-Wire RESET to USB device
        /// </summary>
        public void Comm_OneWireReset()
        {
            SendCommand(
                Ds2490.CMD_TYPE.COMM,
                Ds2490.COMM.ONEWIRE_RESET | Ds2490.COMM.F | Ds2490.COMM.IM | Ds2490.COMM.SE | Ds2490.COMM.NTF,
                usbState.BusCommSpeed,
                "USB Communication: One-Wire Reset");

            int nRegs;
            ReadResult(out nRegs);
            usbState.PrintState();
        }

        /// <summary>
        /// Issue Mode Pulse to USB device
        /// </summary>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <param name="description"></param>
        public void Mode_Pulse(uint index, uint length, string description)
        {
            SendCommand(
                Ds2490.CMD_TYPE.MODE,
                Ds2490.Mode.PULSE_EN,
                Ds2490.ENABLEPULSE_PRGE,
                "USB Mode: PULSE_EN - " + description);

            byte nRegs;
            ReadStatus(out nRegs);
            usbState.PrintState();
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
        public void ReadStatus(out byte nResultRegisters)
        {
            lock(syncObject)
            {
                interruptEventHandler = new TypedEventHandler<UsbInterruptInPipe, UsbInterruptInEventArgs>(this.OnStatusChangeEventOnce);

                usbState.Updated = false;
                usbState.CommResultCodes = null;

                RegisterForInterruptEvent(Ds2490.Pipe.InterruptInPipeIndex, interruptEventHandler);

                while (!usbState.Updated)
                    ReadResultWait.WaitOne();

                UnregisterFromInterruptEvent();

                if (usbState.CommResultCodes != null)
                    nResultRegisters = (byte)usbState.CommResultCodes.Length;
                else
                    nResultRegisters = 0;
            }
        }

        /// <summary>
        /// Read Result data
        /// </summary>
        /// <param name="nResultRegisters"></param>
        public void ReadResult(out int nResultRegisters)
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

                if (usbState.CommResultCodes != null)
                {
                    nResultRegisters = usbState.CommResultCodes.Length;
                    foreach(byte item in usbState.CommResultCodes)
                    {
                        if (item == 0xA5)
                        {
                            Debug.WriteLine("1-Wire Device Detect Byte");
                        }
                        else if (item != 0)
                        {
                            usbState.PrintErrorResult(item);
                        }
                    }
                }
                else
                {
                    nResultRegisters = 0;
                }
            }
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

    }
}
