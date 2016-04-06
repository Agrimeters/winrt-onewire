using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */

namespace com.dalsemi.onewire.adapter
{
    // imports
    using com.dalsemi.onewire.container;
    using com.dalsemi.onewire.utils;
    using logging;
    using ErrorResult = UsbAdapterIo.ErrorResult;

    /// <summary>
    /// <para>This <code>DSPortAdapter</code> class was designed to be used for
    /// the iB-IDE's emulator.  The <code>DumbAdapter</code> allows
    /// programmers to add and remove <code>OneWireContainer</code>
    /// objects that will be found in its search.  The Java iButton
    /// emulator works by creating a class that subclasses all of
    /// <code>OneWireContainer16</code>'s relevant methods and redirecting them
    /// to the emulation code.  That object is then added to this class's
    /// list of <code>OneWireContainer</code>s.</para>
    ///
    /// <para>Note that methods such as <code>selectPort</code> and
    /// <code>beginExclusive</code> by default do nothing.  This class is
    /// mainly meant for debugging using an emulated iButton.  It will do
    /// a poor job of debugging any multi-threading, port-sharing issues.
    ///
    /// </para>
    /// </summary>
    /// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer
    ///
    /// @version    0.00, 16 Mar 2001
    /// @author     K </seealso>
    public class UsbAdapter : DSPortAdapter, IDisposable
    {
        //--------
        //-------- Variables
        //--------

        /// <summary>
        /// Enable/disable debug messages </summary>
        private static bool doDebugMessages = true;

        /// <summary>
        /// The DeviceInformation device id used to open the USB port</summary>
        private string deviceId;

        /// <summary>
        /// USB Adapter packet builder </summary>
        internal UsbPacketBuilder UsbBuild;

        /// <summary>
        /// State of the OneWire </summary>
        private OneWireState owState;

        /// <summary>
        /// USB Adapter state </summary>
        private UsbAdapterState UsbState;

        /// <summary>
        /// Pointer to hold the USB IO class
        /// </summary>
        private UsbAdapterIo UsbIo;

        /// <summary>
        /// Flag to indicate have a local begin/end Exclusive use of serial </summary>
        private bool haveLocalUse;

        private object syncObject;

        /// <summary>
        /// String name of the current opened port </summary>
        private bool adapterPresent;

        /// <summary>
        /// Flag to indicate more than expected byte received in a transaction </summary>
        private bool extraBytesReceived;

        /// <summary>
        /// Vector of thread hash codes that have done an open but no close </summary>
        private readonly List<int> users = new List<int>(4);

        internal int containers_index = 0;

        private List<OneWireContainer> containers = new List<OneWireContainer>();

        public UsbAdapter()
        {
            owState = new OneWireState();
            UsbState = new UsbAdapterState(owState);
            UsbBuild = new UsbPacketBuilder(UsbState);
            adapterPresent = false;
            haveLocalUse = false;
            syncObject = new object();
        }

        /// <summary>
        /// Adds a <code>OneWireContainer</code> to the list of containers that
        /// this adapter object will find.
        /// </summary>
        /// <param name="c"> represents a 1-Wire device that this adapter will report from a search </param>
        public virtual void addContainer(OneWireContainer c)
        {
            lock (containers)
            {
                containers.Add(c);
            }
        }

        /// <summary>
        /// Removes a <code>OneWireContainer</code> from the list of containers that
        /// this adapter object will find.
        /// </summary>
        /// <param name="c"> represents a 1-Wire device that this adapter should no longer
        ///        report as found by a search </param>
        public virtual void removeContainer(OneWireContainer c)
        {
            lock (containers)
            {
                containers.Remove(c);
            }
        }

        /// <summary>
        /// Hashtable to contain the user replaced OneWireContainers
        /// </summary>
        private Hashtable registeredOneWireContainerClasses = new Hashtable(5);

        /// <summary>
        /// Byte array of families to include in search
        /// </summary>
        private byte[] include = null;

        /// <summary>
        /// Byte array of families to exclude from search
        /// </summary>
        private byte[] exclude = null;

        //--------
        //-------- Methods
        //--------

        /// <summary>
        /// Retrieves the name of the port adapter as a string.  The 'Adapter'
        /// is a device that connects to a 'port' that allows one to
        /// communicate with an iButton or other 1-Wire device.  As example
        /// of this is 'DS9097U'.
        /// </summary>
        /// <returns>  <code>String</code> representation of the port adapter. </returns>
        public override string AdapterName
        {
            get
            {
                return "UsbAdapter";
            }
        }

        /// <summary>
        /// Retrieves a description of the port required by this port adapter.
        /// An example of a 'Port' would 'serial communication port'.
        /// </summary>
        /// <returns>  <code>String</code> description of the port type required. </returns>
        public override string PortTypeDescription
        {
            get
            {
                return "DS2490 based master";
            }
        }

        /// <summary>
        /// Retrieves a version string for this class.
        /// </summary>
        /// <returns>  version string </returns>
        public override string ClassVersion
        {
            get
            {
                return "1.00";
            }
        }

        //--------
        //-------- Port Selection
        //--------

        /// <summary>
        /// Retrieves a list of the platform appropriate port names for this
        /// adapter.  A port must be selected with the method 'selectPort'
        /// before any other communication methods can be used.  Using
        /// a communcation method before 'selectPort' will result in
        /// a <code>OneWireException</code> exception.
        /// </summary>
        /// <returns>  <code>Enumeration</code> of type <code>String</code> that contains the port
        /// names </returns>
        public override System.Collections.IEnumerator PortNames
        {
            get
            {
                var t = Task<IEnumerator>.Run(async () =>
                {
                    string aqs = UsbDevice.GetDeviceSelector(Ds2490.DeviceVid, Ds2490.DevicePid);
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
                    throw new System.Exception("Unable to retrieve list of USB devices");
                }
            }
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        ///
        /// </summary>
        public override void registerOneWireContainerClass(int family, Type OneWireContainerClass)
        {
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="portName">  name of the target port, retrieved from
        /// getPortNames()
        /// </param>
        /// <returns> always returns <code>true</code> </returns>
        public override bool selectPort(string portName)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            if (doDebugMessages)
            {
                Debug.WriteLine("USBAdapter.selectPort() called");
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            if (doDebugMessages)
            {
                Debug.WriteLine("USBAdapter.selectPort: System.Enivronment.CurrentManagedThreadId()=" + Environment.CurrentManagedThreadId);
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            // record this thread as an owner
            if (users.IndexOf(Environment.CurrentManagedThreadId) == -1)
            {
                users.Add(Environment.CurrentManagedThreadId);
            }

            if (PortOpen)
                return true;

            try
            {
                var t = Task<UsbDevice>.Run(async () =>
                {
                    // Input -> @"USB\VID_04FA&PID_2490\6&F0F8E95&0&2"
                    string[] st = portName.Split(new char[] { '\\' });
                    StringBuilder deviceInstance = new StringBuilder();
                    deviceInstance.Append(@"\\?\");
                    deviceInstance.Append(st[0]);
                    deviceInstance.Append('#');
                    deviceInstance.Append(st[1]);
                    deviceInstance.Append('#');
                    deviceInstance.Append(st[2].ToLower());
                    deviceInstance.Append('#');
                    deviceInstance.Append("{dee824ef-729b-4a0e-9c14-b7117d33a817}");
                    // Output -> @"\\?\USB#VID_04FA&PID_2490#6&f0f8e95&0&2#{dee824ef-729b-4a0e-9c14-b7117d33a817}"

                    deviceId = deviceInstance.ToString();
                    return await UsbDevice.FromIdAsync(deviceId);
                });

                t.Wait();
                if (t.Status != TaskStatus.RanToCompletion)
                {
                    throw new System.IO.IOException("Failed to open (" + portName + ")");
                }

                UsbIo = new UsbAdapterIo(t.Result, deviceId, UsbState, owState);

                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
                if (doDebugMessages)
                {
                    Debug.WriteLine("UsbAdapter.openPort: Port Openend (" + portName + ")");
                }
                //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
            }
            catch (Exception e)
            {
                // close the port if we have an object
                freePort();

                throw new System.IO.IOException("Failed to open (" + portName + ") :" + e);
            }

            return true;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        public override void freePort()
        {
            if (UsbIo != null)
            {
                if (UsbIo.usbDevice != null)
                {
                    UsbIo.usbDevice.Dispose();
                    UsbIo = null;
                }
            }
        }

        /// <summary>
        /// Retrieves the name of the selected port as a <code>String</code>.
        /// </summary>
        /// <returns>  the deviceID of port </returns>
        public override string PortName
        {
            get
            {
                return deviceId;
            }
        }

        /// <summary>
        /// Retrieves the open state of the selected port as a <code>bool</code>.
        /// </summary>
        /// <returns>  bool indicating port open state </returns>
        public virtual bool PortOpen
        {
            get
            {
                lock (this)
                {
                    if (UsbIo != null)
                        return (UsbIo.usbDevice != null) ? true : false;
                    else
                        return false;
                }
            }
        }

        //--------
        //-------- Adapter detection
        //--------

        /// <summary>
        /// Check to see if there is a short on the 1-Wire bus.
        /// </summary>
        /// <param name="present"></param>
        /// <param name="vpp"></param>
        /// <returns>
        /// true = DS2490 1-Wire is not shorted
        /// false = DS2490 1-Wire is shorted</returns>
        private bool ShortCheck(out bool present, out bool vpp)
        {
            present = true;
            bool DeviceDetected;

            // read VPP state

            UsbIo.ReadStatus(out DeviceDetected);
            vpp = UsbState.ProgrammingVoltagePresent;

            // check if 1-Wire bus is shorted
            UsbIo.Comm_OneWireReset(out DeviceDetected);
            if (0 != UsbIo.LastError)
            {
                ErrorResult err = UsbIo.LastError;

                if ((err & ErrorResult.NRS) == ErrorResult.NRS)
                {
                    present = false;
                }

                if ((err & ErrorResult.SH) == ErrorResult.SH)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies if 1-Wire bus is shorted, VPP, and if devices are present
        /// </summary>
        /// <returns>
        /// true = DS2490 1-Wire shorted
        /// false = DS2490 1-Wire is not shorted
        /// </returns>
        private bool UsbVerify()
        {
            try
            {
                bool present = false;
                bool vpp = false;

                return ShortCheck(out present, out vpp);
            }
            catch (IOException ioe)
            {
                if (doDebugMessages)
                {
                    Debug.WriteLine("UsbAdapter-UsbVerify: " + ioe);
                }
            }
            catch (OneWireIOException e)
            {
                if (doDebugMessages)
                {
                    Debug.WriteLine("UsbAdapter-UsbVerify: " + e);
                }
            }

            return false;
        }

        /// <summary>
        /// Write the packet to EP2
        /// </summary>
        /// <param name="tempBuild">  the USB Packet Build where the packet to send
        ///                     resides
        /// </param>
        /// <returns>  the result array
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        private byte[] UsbTransaction(UsbPacketBuilder tempBuild, bool alarm_only)
        {
            byte[] ret_buffer = null;

            try
            {
                // loop to send all of the packets
                for (IEnumerator packet_enum = tempBuild.Packets; packet_enum.MoveNext();)
                {
                    // get the next packet
                    RawSendPacket pkt = (RawSendPacket)packet_enum.Current;

                    // bogus packet to indicate need to wait for long DS2480 alarm reset
                    if ((pkt.buffer.Length == 0) && (pkt.returnLength == 0))
                    {
                        Thread.Sleep(6);
                        continue;
                    }

                    // send the packet
                    var t = Task.Run(async () =>
                    {
                        pkt.writer.Flush();
                        await UsbIo.BulkEp_Write(0, pkt.buffer.ToArray(),
                            "USB Transaction: Write EP, count=" + pkt.buffer.Length);
                    });
                    t.Wait();
                }

                // Issue Search Command
                bool DeviceDetected;
                ErrorResult result = UsbIo.Comm_SearchAccess("UsbTransaction", alarm_only, 1, out DeviceDetected);

                if (ErrorResult.NRS == (result & ErrorResult.NRS))
                {
                    Debug.WriteLine("Nothing found!");
                    Debugger.Break();
                }

                int i;
                do
                {
                    for (i = 0; i < 200; i++)
                    {
                        result = UsbIo.ReadStatus(out DeviceDetected);
                        if (ErrorResult.NRS == (result & ErrorResult.NRS))
                        {
                            Debug.WriteLine("Nothing found!");
                            Debugger.Break();
                        }
                        else
                        {
                            UsbIo.PrintErrorResult();
                            break;
                        }

                        Thread.Sleep(1);
                    }
                } while (!UsbState.Idle);

                // we have something to read
                if (UsbState.OneWireReadBufferStatus > 0)
                {
                    // send the packet
                    var t = Task.Run(async () =>
                    {
                        ret_buffer = await UsbIo.BulkEp_Read(0, "USB Transaction: Read EP");
                    });
                    t.Wait();
                }

                return ret_buffer;
            }
            catch (IOException e)
            {
                // need to check on adapter
                adapterPresent = false;

                // pass it on
                throw new OneWireIOException(e.ToString());
            }
        }

        /// <summary>
        /// Verify that the DS2480 based adapter is present on the open port.
        /// </summary>
        /// <returns> 'true' if adapter present
        /// </returns>
        /// <exception cref="OneWireException"> - if port not selected </exception>
        private bool uAdapterPresent()
        {
            bool rt = true;

            // check if adapter has already be verified to be present
            if (!adapterPresent)
            {
                // do a master reset
                UsbMasterReset();

                // attempt to verify
                if (!UsbVerify())
                {
                    // do a master reset and try again
                    UsbMasterReset();

                    if (!UsbVerify())
                    {
                        rt = false;
                    }
                }
            }

            adapterPresent = rt;

            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: AdapterPresent result: " + rt);
            }

            return rt;
        }

        /// <summary>
        /// Do a master reset on the DS2490.  This initiates
        /// a master rest cycle.
        /// </summary>
        private void UsbMasterReset()
        {
            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: UsbMasterReset");
            }

            bool DeviceDetected;

            UsbIo.Control_ResetDevice();
            PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
            UsbIo.Comm_SetDuration(Ds2490.COMM.TYPE, 0x40, "12V pullup, 512us", out DeviceDetected);
            UsbIo.Mode_EnablePulse(Ds2490.ENABLEPULSE_PRGE, "Disable 5V Strong PU, Enable 12V Program Pulse", out DeviceDetected);
        }

        /// <summary>
        /// Detect adapter presence on the selected port.
        /// </summary>
        /// <returns>  <code>true</code> if the adapter is confirmed to be connected to
        /// the selected port, <code>false</code> if the adapter is not connected.
        /// </returns>
        /// <exception cref="OneWireIOException"> </exception>
        /// <exception cref="OneWireException"> </exception>
        public override bool adapterDetected()
        {
            bool rt;

            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();
                uAdapterPresent();

                rt = UsbVerify();
            }
            catch (OneWireException)
            {
                rt = false;
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }

            return rt;
        }

        //--------
        //-------- Adapter features
        //--------

        /// <summary>
        /// Applications might check this method and not attempt operation unless this method
        /// returns <code>true</code>. To make sure that a wide variety of applications can use this class,
        /// this method always returns <code>true</code>.
        /// </summary>
        /// <returns>  <code>true</code>
        ///  </returns>
        public override bool canOverdrive()
        {
            return true;
        }

        /// <summary>
        /// Applications might check this method and not attempt operation unless this method
        /// returns <code>true</code>. To make sure that a wide variety of applications can use this class,
        /// this method always returns <code>true</code>.
        /// </summary>
        /// <returns>  <code>true</code> </returns>
        public override bool canFlex()
        {
            return true;
        }

        /// <summary>
        /// Returns whether the adapter can physically support strong 5 volt power
        /// mode.
        /// </summary>
        /// <returns>  <code>true</code> if this port adapter can do strong 5 volt
        /// mode, <code>false</code> otherwise.
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire
        ///         adapter </exception>
        public override bool canDeliverPower()
        {
            return true;
        }

        //--------
        //-------- Finding iButtons and 1-Wire devices
        //--------

        /// <summary>
        /// Returns <code>true</code> if the first iButton or 1-Wire device
        /// is found on the 1-Wire Network.
        /// If no devices are found, then <code>false</code> will be returned.
        /// </summary>
        /// <returns>  <code>true</code> if an iButton or 1-Wire device is found. </returns>
        public override bool findFirstDevice()
        {
            // reset the current search
            owState.searchLastDiscrepancy = 0;
            owState.searchFamilyLastDiscrepancy = 0;
            owState.searchLastDevice = false;

            // search for the first device using next
            return findNextDevice();
        }

        /// <summary>
        /// Returns <code>true</code> if the next iButton or 1-Wire device
        /// is found. The previous 1-Wire device found is used
        /// as a starting point in the search.  If no more devices are found
        /// then <code>false</code> will be returned.
        /// </summary>
        /// <returns>  <code>true</code> if an iButton or 1-Wire device is found. </returns>
        public override bool findNextDevice()
        {
            bool search_result;

            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();

                // check for previous last device
                if (owState.searchLastDevice)
                {
                    owState.searchLastDiscrepancy = 0;
                    owState.searchFamilyLastDiscrepancy = 0;
                    owState.searchLastDevice = false;

                    return false;
                }

                // check for 'first' and only 1 target
                if ((owState.searchLastDiscrepancy == 0) && (owState.searchLastDevice == false) && (owState.searchIncludeFamilies.Length == 1))
                {
                    // set the search to find the 1 target first
                    owState.searchLastDiscrepancy = 64;

                    // create an id to set
                    byte[] new_id = new byte[8];

                    // set the family code
                    new_id[0] = owState.searchIncludeFamilies[0];

                    // clear the rest
                    for (int i = 1; i < 8; i++)
                    {
                        new_id[i] = 0;
                    }

                    // set this new ID
                    Array.Copy(new_id, 0, owState.ID, 0, 8);
                }

                // loop until the correct type is found or no more devices
                do
                {
                    // perform a search and keep the result
                    search_result = search(owState);

                    if (search_result)
                    {
                        // check if not in exclude list
                        bool is_excluded = false;

                        for (int i = 0; i < owState.searchExcludeFamilies.Length; i++)
                        {
                            if (owState.ID[0] == owState.searchExcludeFamilies[i])
                            {
                                is_excluded = true;

                                break;
                            }
                        }

                        // if not in exclude list then check for include list
                        if (!is_excluded)
                        {
                            // loop through the include list
                            bool is_included = false;

                            for (int i = 0; i < owState.searchIncludeFamilies.Length; i++)
                            {
                                if (owState.ID[0] == owState.searchIncludeFamilies[i])
                                {
                                    is_included = true;

                                    break;
                                }
                            }

                            // check if include list or there is no include list
                            if (is_included || (owState.searchIncludeFamilies.Length == 0))
                            {
                                return true;
                            }
                        }
                    }

                    // skip the current type if not last device
                    if ((!owState.searchLastDevice) && (owState.searchFamilyLastDiscrepancy != 0))
                    {
                        owState.searchLastDiscrepancy = owState.searchFamilyLastDiscrepancy;
                        owState.searchFamilyLastDiscrepancy = 0;
                        owState.searchLastDevice = false;
                    }

                    // end of search so reset and return
                    else
                    {
                        owState.searchLastDiscrepancy = 0;
                        owState.searchFamilyLastDiscrepancy = 0;
                        owState.searchLastDevice = false;
                        search_result = false;
                    }
                } while (search_result);

                // device not found
                return false;
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        /// <summary>
        /// Copies the 'current' 1-Wire device address being used by the adapter into
        /// the array.  This address is the last iButton or 1-Wire device found
        /// in a search (findNextDevice()...).
        /// This method copies into a user generated array to allow the
        /// reuse of the buffer.  When searching many iButtons on the one
        /// wire network, this will reduce the memory burn rate.
        /// </summary>
        /// <param name="address"> An array to be filled with the current iButton address. </param>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        public override void getAddress(byte[] address)
        {
            Array.Copy(owState.ID, 0, address, 0, 8);
        }

        /// <summary>
        /// Copies the provided 1-Wire device address into the 'current'
        /// array.  This address will then be used in the getDeviceContainer()
        /// method.  Permits the adapter instance to create containers
        /// of devices it did not find in a search.
        /// </summary>
        /// <param name="address"> An array to be copied into the current iButton
        ///         address. </param>
        public virtual byte[] Address
        {
            set
            {
                Array.Copy(value, 0, owState.ID, 0, 8);
            }
        }

        ///// <summary>
        ///// Gets the 'current' 1-Wire device address being used by the adapter as a long.
        ///// This address is the last iButton or 1-Wire device found
        ///// in a search (findNextDevice()...).
        ///// </summary>
        ///// <returns> <code>long</code> representation of the iButton address </returns>
        ///// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override long AddressAsLong
        //{
        // get
        // {
        // byte[] address = new byte [8];

        // getAddress(address);

        // return Address.toLong(address);
        // }
        //}

        ///// <summary>
        ///// Gets the 'current' 1-Wire device address being used by the adapter as a String.
        ///// This address is the last iButton or 1-Wire device found
        ///// in a search (findNextDevice()...).
        ///// </summary>
        ///// <returns> <code>String</code> representation of the iButton address </returns>
        ///// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override string AddressAsString
        //{
        // get
        // {
        // byte[] address = new byte [8];

        // getAddress(address);

        // return Address.ToString(address);
        // }
        //}

        /// <summary>
        /// Verifies that the iButton or 1-Wire device specified is present on
        /// the 1-Wire Network. This does not affect the 'current' device
        /// state information used in searches (findNextDevice...).
        /// </summary>
        /// <param name="address">  device address to verify is present
        /// </param>
        /// <returns>  <code>true</code> if device is present, else
        ///         <code>false</code>.
        /// </returns>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        public override bool isPresent(byte[] address)
        {
            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();

                // make sure adapter is present
                if (adapterDetected())
                {
                    // check for pending power conditions
                    if (owState.oneWireLevel != LEVEL_NORMAL)
                    {
                        setPowerNormal();
                    }

                    // if in overdrive, then use the block method in super
                    if (owState.oneWireSpeed == SPEED_OVERDRIVE)
                    {
                        return blockIsPresent(address, false);
                    }

                    // create a private OneWireState
                    OneWireState onewire_state = new OneWireState();

                    // set the ID to the ID of the iButton passes to this method
                    Array.Copy(address, 0, onewire_state.ID, 0, 8);

                    // set the state to find the specified device
                    onewire_state.searchLastDiscrepancy = 64;
                    onewire_state.searchFamilyLastDiscrepancy = 0;
                    onewire_state.searchLastDevice = false;
                    onewire_state.searchOnlyAlarmingButtons = false;

                    // perform a search
                    if (search(onewire_state))
                    {
                        // compare the found device with the desired device
                        for (int i = 0; i < 8; i++)
                        {
                            if (address[i] != onewire_state.ID[i])
                            {
                                return false;
                            }
                        }

                        // must be the correct device
                        return true;
                    }

                    // failed to find device
                    return false;
                }
                else
                {
                    throw new OneWireIOException("Error communicating with adapter");
                }
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        /// <summary>
        /// Verifies that the iButton or 1-Wire device specified is present on
        /// the 1-Wire Network. This does not affect the 'current' device
        /// state information used in searches (findNextDevice...).
        /// </summary>
        /// <param name="address">  device address to verify is present
        /// </param>
        /// <returns>  <code>true</code> if device is present, else
        ///         <code>false</code>.
        /// </returns>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        public override bool isPresent(long address)
        {
            lock (containers)
            {
                for (int i = 0; i < containers.Count; i++)
                {
                    OneWireContainer temp = (OneWireContainer)containers[i];
                    long addr = temp.AddressAsLong;
                    if (addr == address)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Verifies that the iButton or 1-Wire device specified is present
        /// on the 1-Wire Network and in an alarm state. This does not
        /// affect the 'current' device state information used in searches
        /// (findNextDevice...).
        /// </summary>
        /// <param name="address">  device address to verify is present and alarming
        /// </param>
        /// <returns>  <code>true</code> if device is present and alarming else
        /// <code>false</code>.
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
        /// </exception>
        /// <seealso cref=    com.dalsemi.onewire.utils.Address </seealso>
        public override bool isAlarming(byte[] address)
        {
            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();

                // make sure adapter is present
                if (uAdapterPresent())
                {
                    // check for pending power conditions
                    if (owState.oneWireLevel != LEVEL_NORMAL)
                    {
                        setPowerNormal();
                    }

                    // if in overdrive, then use the block method in super
                    if (owState.oneWireSpeed == SPEED_OVERDRIVE)
                    {
                        return blockIsPresent(address, true);
                    }

                    // create a private OneWireState
                    OneWireState onewire_state = new OneWireState();

                    // set the ID to the ID of the iButton passes to this method
                    Array.Copy(address, 0, onewire_state.ID, 0, 8);

                    // set the state to find the specified device (alarming)
                    onewire_state.searchLastDiscrepancy = 64;
                    onewire_state.searchFamilyLastDiscrepancy = 0;
                    onewire_state.searchLastDevice = false;
                    onewire_state.searchOnlyAlarmingButtons = true;

                    // perform a search
                    if (search(onewire_state))
                    {
                        // compare the found device with the desired device
                        for (int i = 0; i < 8; i++)
                        {
                            if (address[i] != onewire_state.ID[i])
                            {
                                return false;
                            }
                        }

                        // must be the correct device
                        return true;
                    }

                    // failed to find any alarming device
                    return false;
                }
                else
                {
                    throw new OneWireIOException("Error communicating with adapter");
                }
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        /// <summary>
        /// Selects the specified iButton or 1-Wire device by broadcasting its
        /// address.  With a <code>DumbAdapter</code>, this method simply
        /// returns true.
        ///
        /// Warning, this does not verify that the device is currently present
        /// on the 1-Wire Network (See isPresent).
        /// </summary>
        /// <param name="address">    address of iButton or 1-Wire device to select
        /// </param>
        /// <returns>  <code>true</code> if device address was sent, <code>false</code>
        /// otherwise.
        /// </returns>
        /// <seealso cref= #isPresent(byte[]) </seealso>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        public override bool select(byte[] address)
        {
            return isPresent(address);
        }

        /// <summary>
        /// Selects the specified iButton or 1-Wire device by broadcasting its
        /// address.  With a <code>DumbAdapter</code>, this method simply
        /// returns true.
        ///
        /// Warning, this does not verify that the device is currently present
        /// on the 1-Wire Network (See isPresent).
        /// </summary>
        /// <param name="address">    address of iButton or 1-Wire device to select
        /// </param>
        /// <returns>  <code>true</code> if device address was sent, <code>false</code>
        /// otherwise.
        /// </returns>
        /// <seealso cref= #isPresent(byte[]) </seealso>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override bool select(long address)
        //{
        //return select(Address.toByteArray(address));
        //}

        /// <summary>
        /// Selects the specified iButton or 1-Wire device by broadcasting its
        /// address.  With a <code>DumbAdapter</code>, this method simply
        /// returns true.
        ///
        /// Warning, this does not verify that the device is currently present
        /// on the 1-Wire Network (See isPresent).
        /// </summary>
        /// <param name="address">    address of iButton or 1-Wire device to select
        /// </param>
        /// <returns>  <code>true</code> if device address was sent, <code>false</code>
        /// otherwise.
        /// </returns>
        /// <seealso cref=    #isPresent(byte[]) </seealso>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override bool select(string address)
        //{
        //return select(Address.toByteArray(address));
        //}

        //--------
        //-------- Support methods
        //--------

        /// <summary>
        /// Peform a search using the oneWire state provided
        /// </summary>
        ///  <param name="mState">  current OneWire state used to do the search
        /// </param>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
        private bool search(OneWireState mState)
        {
            int reset_offset = 0;

            // make sure adapter is present
            if (uAdapterPresent())
            {
                // check for pending power conditions
                if (owState.oneWireLevel != LEVEL_NORMAL)
                {
                    setPowerNormal();
                }

                // add a reset/ search command
                if (!mState.skipResetOnSearch)
                {
                    reset_offset = oneWireReset();
                }

                UsbBuild.restart();

                // add search sequence based on mState
                int search_offset = UsbBuild.search(mState);

                // send/receive the search
                byte[] result_array = UsbTransaction(UsbBuild, mState.searchOnlyAlarmingButtons);

                return UsbBuild.interpretSearch(mState, result_array, 0);
            }
            else
            {
                throw new OneWireIOException("Error communicating with adapter");
            }
        }

        /// <summary>
        /// Perform a 'strongAccess' with the provided 1-Wire address.
        /// 1-Wire Network has already been reset and the 'search'
        /// command sent before this is called.
        /// </summary>
        /// <param name="address">  device address to do strongAccess on </param>
        /// <param name="alarmOnly">  verify device is present and alarming if true
        /// </param>
        /// <returns>  true if device participated and was present
        ///         in the strongAccess search </returns>
        private bool blockIsPresent(byte[] address, bool alarmOnly)
        {
            byte[] send_packet = new byte[24];
            int i;

            // reset the 1-Wire
            reset();

            // send search command
            if (alarmOnly)
            {
                //TODO                putByte(ALARM_SEARCH_CMD);
            }
            else
            {
                //TODO                putByte(NORMAL_SEARCH_CMD);
            }

            // set all bits at first
            for (i = 0; i < 24; i++)
            {
                send_packet[i] = 0xFF;
            }

            // now set or clear apropriate bits for search
            for (i = 0; i < 64; i++)
            {
                Bit.arrayWriteBit(Bit.arrayReadBit(i, 0, address), (i + 1) * 3 - 1, 0, send_packet);
            }

            // send to 1-Wire Net
            dataBlock(send_packet, 0, 24);

            // check the results of last 8 triplets (should be no conflicts)
            int cnt = 56, goodbits = 0, tst, s;

            for (i = 168; i < 192; i += 3)
            {
                tst = (Bit.arrayReadBit(i, 0, send_packet) << 1) | Bit.arrayReadBit(i + 1, 0, send_packet);
                s = Bit.arrayReadBit(cnt++, 0, address);

                if (tst == 0x03) // no device on line
                {
                    goodbits = 0; // number of good bits set to zero

                    break; // quit
                }

                if (((s == 0x01) && (tst == 0x02)) || ((s == 0x00) && (tst == 0x01))) // correct bit
                {
                    goodbits++; // count as a good bit
                }
            }

            // check too see if there were enough good bits to be successful
            return (goodbits >= 8);
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <seealso cref= #setNoResetSearch </seealso>
        public override void setSearchOnlyAlarmingDevices()
        {
            owState.searchOnlyAlarmingButtons = true;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        ///
        /// </summary>
        public override void setNoResetSearch()
        {
            owState.skipResetOnSearch = true;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <seealso cref= #setNoResetSearch </seealso>
        public override void setSearchAllDevices()
        {
            owState.searchOnlyAlarmingButtons = false;
            owState.skipResetOnSearch = false;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <seealso cref=    #targetFamily </seealso>
        /// <seealso cref=    #targetFamily(byte[]) </seealso>
        /// <seealso cref=    #excludeFamily </seealso>
        /// <seealso cref=    #excludeFamily(byte[]) </seealso>
        public override void targetAllFamilies()
        {
            // clear the include and exclude family search lists
            owState.searchIncludeFamilies = new byte[0];
            owState.searchExcludeFamilies = new byte[0];
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="family">   the code of the family type to target for searches </param>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        /// <seealso cref=    #targetAllFamilies </seealso>
        public override void targetFamily(int family)
        {
            // replace include family array with 1 element array
            owState.searchIncludeFamilies = new byte[1];
            owState.searchIncludeFamilies[0] = (byte)family;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="family">  array of the family types to target for searches </param>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        /// <seealso cref=    #targetAllFamilies </seealso>
        public override void targetFamily(byte[] family)
        {
            // replace include family array with new array
            owState.searchIncludeFamilies = new byte[family.Length];

            Array.Copy(family, 0, owState.searchIncludeFamilies, 0, family.Length);
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="family">   the code of the family type NOT to target in searches </param>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        /// <seealso cref=    #targetAllFamilies </seealso>
        public override void excludeFamily(int family)
        {
            // replace exclude family array with 1 element array
            owState.searchExcludeFamilies = new byte[1];
            owState.searchExcludeFamilies[0] = (byte)family;
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="family">  array of family cods NOT to target for searches </param>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        /// <seealso cref=    #targetAllFamilies </seealso>
        public override void excludeFamily(byte[] family)
        {
            // replace exclude family array with new array
            owState.searchExcludeFamilies = new byte[family.Length];

            Array.Copy(family, 0, owState.searchExcludeFamilies, 0, family.Length);
        }

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        /// <summary>
        /// begin exclusive access
        /// </summary>
        /// <param name="blocking"></param>
        /// <returns>
        /// true
        /// </returns>
        public override bool beginExclusive(bool blocking)
        {
            //DEBUG!!! RIGHT NOW THIS IS NOT IMPLEMENTED!!!
            return true;
        }

        /// <summary>
        /// end exclusive
        /// </summary>
        public override void endExclusive()
        {
            //DEBUG!!! RIGHT NOW THIS IS NOT IMPLEMENTED!!!
        }

        /// <summary>
        /// Gets exclusive use of the 1-Wire to communicate with an iButton or
        /// 1-Wire Device if it is not already done.  Used to make methods
        /// thread safe.
        /// </summary>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
        private void beginLocalExclusive()
        {
            // check if there is no such port
            if (!PortOpen)
            {
                throw new OneWireException("UsbAdapter: port not selected ");
            }

            // check if already have exclusive use
            if (haveExclusive())
            {
                return;
            }
            else
            {
                while (!haveLocalUse)
                {
                    lock (syncObject)
                    {
                        haveLocalUse = beginExclusive(false);
                    }
                    if (!haveLocalUse)
                    {
                        try
                        {
                            Thread.Sleep(50);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Relinquishes local exclusive control of the 1-Wire Network.  This
        /// just checks if we did our own 'beginExclusive' block and frees it.
        /// </summary>
        private void endLocalExclusive()
        {
            lock (syncObject)
            {
                if (haveLocalUse)
                {
                    haveLocalUse = false;

                    endExclusive();
                }
            }
        }

        //TODO
        internal bool haveExclusive()
        {
            return true;
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        /// <summary>
        /// Sends a bit to the 1-Wire Network.
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="bitValue">  the bit value to send to the 1-Wire Network. </param>
        public override void putBit(bool bitValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// gets a bit on the 1-Wire network
        /// </summary>
        public override bool getBit
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="byteValue">  the byte value to send to the 1-Wire Network. </param>
        public override void putByte(int byteValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <returns> the value 0x0ff </returns>
        public override int Byte
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="len">  length of data bytes to receive
        /// </param>
        /// <returns> a new byte array of length <code>len</code> </returns>
        public override byte[] getBlock(int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="arr">     array in which to write the received bytes </param>
        /// <param name="len">     length of data bytes to receive </param>
        public override void getBlock(byte[] arr, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="arr">     array in which to write the received bytes </param>
        /// <param name="off">     offset into the array to start </param>
        /// <param name="len">     length of data bytes to receive </param>
        public override void getBlock(byte[] arr, int off, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="dataBlock">  array of data to transfer to and from the 1-Wire Network. </param>
        /// <param name="off">        offset into the array of data to start </param>
        /// <param name="len">        length of data to send / receive starting at 'off' </param>
        public override void dataBlock(byte[] dataBlock, int off, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends a Reset to the 1-Wire Network.
        /// </summary>
        /// <returns>  the result of the reset. Potential results are:
        /// <ul>
        /// <li> 0 (RESET_NOPRESENCE) no devices present on the 1-Wire Network.
        /// <li> 1 (RESET_PRESENCE) normal presence pulse detected on the 1-Wire
        ///        Network indicating there is a device present.
        /// <li> 2 (RESET_ALARM) alarming presence pulse detected on the 1-Wire
        ///        Network indicating there is a device present and it is in the
        ///        alarm condition.  This is only provided by the DS1994/DS2404
        ///        devices.
        /// <li> 3 (RESET_SHORT) inticates 1-Wire appears shorted.  This can be
        ///        transient conditions in a 1-Wire Network.  Not all adapter types
        ///        can detect this condition.
        /// </ul>
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
        public override int reset()
        {
            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();

                // make sure adapter is present
                if (uAdapterPresent())
                {
                    // check for pending power conditions
                    if (owState.oneWireLevel != LEVEL_NORMAL)
                    {
                        setPowerNormal();
                    }

                    bool DeviceDetect;

                    ErrorResult result = UsbIo.Comm_OneWireReset(out DeviceDetect);

                    if (DeviceDetect)
                    {
                        return DSPortAdapter.RESET_PRESENCE;
                    }
                    else if (ErrorResult.NRS == (result & ErrorResult.NRS))
                    {
                        return DSPortAdapter.RESET_NOPRESENCE;
                    }
                    if (ErrorResult.APP == (result & ErrorResult.APP))
                    {
                        return DSPortAdapter.RESET_ALARM;
                    }
                    if (ErrorResult.SH == (result & ErrorResult.SH))
                    {
                        return DSPortAdapter.RESET_SHORT;
                    }

                    throw new OneWireIOException("USBAdapter-reset: no return byte form 1-Wire reset");
                }
                else
                {
                    throw new OneWireIOException("Error communicating with adapter");
                }
            }
            catch (IOException ioe)
            {
                throw new OneWireIOException(ioe.ToString());
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        /// <summary>
        /// Sets the duration of the 5V Strong Pullup
        /// </summary>
        /// <param name="timeFactor">
        /// <ul>
        /// <li>   0 (DELIVERY_HALF_SECOND) provide power for 1/2 second.
        /// <li>   1 (DELIVERY_ONE_SECOND) provide power for 1 second.
        /// <li>   2 (DELIVERY_TWO_SECONDS) provide power for 2 seconds.
        /// <li>   3 (DELIVERY_FOUR_SECONDS) provide power for 4 seconds.
        /// <li>   4 (DELIVERY_SMART_DONE) provide power until the
        ///          the device is no longer drawing significant power.
        /// <li>   5 (DELIVERY_INFINITE) provide power until the
        ///          setPowerNormal() method is called.
        /// </ul> </param>
        public override int PowerDuration
        {
            set
            {
                bool DeviceDetected;

                switch (value)
                {
                    case DSPortAdapter.DELIVERY_HALF_SECOND:
                        UsbIo.Comm_SetDuration(0, 0x1F, // .5/.016 = 31.25
                            "DSPortAdapter.DELIVERY_HALF_SECOND", out DeviceDetected);
                        break;

                    case DSPortAdapter.DELIVERY_ONE_SECOND:
                        UsbIo.Comm_SetDuration(0, 0x3E, // 1/.016 = 62.5
                            "DSPortAdapter.DELIVERY_ONE_SECOND", out DeviceDetected);
                        break;

                    case DSPortAdapter.DELIVERY_TWO_SECONDS:
                        UsbIo.Comm_SetDuration(0, 0x7D, // 2/.016 = 125
                            "DSPortAdapter.DELIVERY_TWO_SECOND", out DeviceDetected);
                        break;

                    case DSPortAdapter.DELIVERY_FOUR_SECONDS:
                        UsbIo.Comm_SetDuration(0, 0xFA, // 4/.016 = 250
                            "DSPortAdapter.DELIVERY_FOUR_SECOND", out DeviceDetected);
                        break;

                    case DSPortAdapter.DELIVERY_INFINITE:
                        UsbIo.Comm_SetDuration(0, 0x00, "DSPortAdapter.DELIVERY_INFINITE", out DeviceDetected);
                        break;

                    case DSPortAdapter.DELIVERY_SMART_DONE:
                        throw new NotSupportedException();
                    default:
                        UsbIo.Comm_SetDuration(0, UsbAdapterState.GetPullUpDurationByte(value),
                            "PowerDuration Variable", out DeviceDetected);
                        break;
                }
            }
        }

        /// <summary>
        /// This method does nothing in <code>DumbAdapter</code>.
        /// </summary>
        /// <param name="changeCondition">
        /// <ul>
        /// <li>   0 (CONDITION_NOW) operation should occur immediately.
        /// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
        ///           execution immediately after the next bit is sent.
        /// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
        ///           execution immediately after next byte is sent.
        /// </ul>
        /// </param>
        /// <returns> <code>true</code> </returns>
        public override bool startPowerDelivery(int changeCondition)
        {
            try
            {
                bool DeviceDetected;

                // acquire exclusive use of the port
                beginLocalExclusive();

                if (changeCondition == CONDITION_AFTER_BIT)
                {
                    owState.levelChangeOnNextBit = true;
                    owState.primedLevelValue = LEVEL_POWER_DELIVERY;
                }
                else if (changeCondition == CONDITION_AFTER_BYTE)
                {
                    owState.levelChangeOnNextByte = true;
                    owState.primedLevelValue = LEVEL_POWER_DELIVERY;
                }
                else if (changeCondition == CONDITION_NOW)
                {
                    // make sure adapter is present
                    if (uAdapterPresent())
                    {
                        // check for pending power conditions
                        if (owState.oneWireLevel != LEVEL_NORMAL)
                        {
                            setPowerNormal();
                        }

                        UsbIo.Mode_EnablePulse(Ds2490.ENABLEPULSE_SPUE, "startPowerDelivery", out DeviceDetected);
                        UsbIo.Comm_Pulse("startPowerDelivery", out DeviceDetected);

                        return UsbState.StrongPullup;
                    }
                    else
                    {
                        throw new OneWireIOException("Error communicating with adapter");
                    }
                }
                else
                {
                    throw new OneWireException("Invalid power delivery condition");
                }

                return false;
            }
            catch (IOException ioe)
            {
                throw new OneWireIOException(ioe.ToString());
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        /// <summary>
        /// Terminate Pulse
        /// An infinite duration pulse is terminated by using either of the
        /// HALT EXECUTION Control commands. To resume 1-Wire activity after
        /// the termination, use the RESUME EXECUTION Control command
        /// </summary>
        private void TerminatePulse()
        {
            bool DeviceDetected;

            do
            {
                UsbIo.Control_HalExecWhenIdle(out DeviceDetected);
                UsbIo.Control_ResumeExec(out DeviceDetected);
            } while (UsbState.StrongPullup);
        }

        /// <summary>
        /// Sets the 1-Wire Network voltage to normal level.  This method is used
        /// to disable 1-Wire conditions created by startPowerDelivery and
        /// startProgramPulse.  This method will automatically be called if
        /// a communication method is called while an outstanding power
        /// command is taking place.
        /// </summary>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
        ///         or the adapter does not support this operation </exception>
        public override void setPowerNormal()
        {
            try
            {
                // acquire exclusive use of the port
                beginLocalExclusive();

                if (owState.oneWireLevel == LEVEL_POWER_DELIVERY)
                {
                    // make sure adapter is present
                    if (uAdapterPresent())
                    {
                        TerminatePulse();
                    }
                }
            }
            catch (IOException ioe)
            {
                throw new OneWireIOException(ioe.ToString());
            }
            finally
            {
                // release local exclusive use of port
                endLocalExclusive();
            }
        }

        //--------
        //-------- 1-Wire Network operation append methods
        //--------

        /// <summary>
        /// Add the command to reset the OneWire at the current speed.
        /// </summary>
        ///  <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int oneWireReset()
        {
            bool DeviceDetected;

            // disable strong pull ups prior to reset
            UsbIo.Comm_OneWireReset(out DeviceDetected);

            //// append the reset command at the current speed
            //packet.writer.Write((byte)(FUNCTION_RESET | UsbState.uSpeedMode)); //TODO .Append

            //// check if not streaming resets
            //if (!UsbState.streamResets)
            //{
            //    newPacket();
            //}

            //// check for 2480 wait on extra bytes packet
            //if (UsbState.longAlarmCheck && ((UsbState.UsbSpeedMode == UsbAdapterState.USPEED_REGULAR) || (UsbState.uSpeedMode == UsbAdapterState.USPEED_FLEX)))
            //{
            //    newPacket();
            //}

            //return totalReturnLength - 1;
            return 5;
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        /// <summary>
        /// This method takes an int representing the new speed of data
        /// transfer on the 1-Wire Network. <para>
        ///
        /// </para>
        /// </summary>
        /// <param name="speed">
        /// <ul>
        /// <li>     0 (SPEED_REGULAR) set to normal communciation speed
        /// <li>     1 (SPEED_FLEX) set to flexible communciation speed used
        ///            for long lines
        /// <li>     2 (SPEED_OVERDRIVE) set to normal communciation speed to
        ///            overdrive
        /// <li>     3 (SPEED_HYPERDRIVE) set to normal communciation speed to
        ///            hyperdrive
        /// <li>    >3 future speeds
        /// </ul>
        /// </param>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
        ///         or the adapter does not support this operation </exception>
        public override int Speed
        {
            set
            {
                try
                {
                    // acquire exclusive use of the port
                    beginLocalExclusive();

                    // check for valid value
                    if ((value == SPEED_REGULAR) || (value == SPEED_OVERDRIVE) || (value == SPEED_FLEX))
                    {
                        // change 1-Wire value
                        owState.oneWireSpeed = (byte)value;

                        // set adapter to communicate at this new value
                        if (value == SPEED_OVERDRIVE)
                        {
                            UsbState.ReqBusCommSpeed = (sbyte)UsbAdapterState.BUSCOMSPEED_OVERDRIVE;
                        }
                        else //if (value == SPEED_FLEX)
                        {
                            UsbState.ReqBusCommSpeed = (sbyte)UsbAdapterState.BUSCOMSPEED_FLEX;
                        }
                        //else if (value == SPEED_REGULAR)
                        //{
                        //    UsbState.ReqBusCommSpeed = (sbyte)UsbAdapterState.BUSCOMSPEED_REGULAR;
                        //}
                    }
                    else
                    {
                        throw new OneWireException("Requested speed is not supported by this adapter");
                    }
                }
                finally
                {
                    // release local exclusive use of port
                    endLocalExclusive();
                }
            }
            get
            {
                return owState.oneWireSpeed;
            }
        }

        //--------
        //-------- Misc
        //--------

        /// <summary>
        /// Gets the container from this adapter whose address matches the address of a container
        /// in the <code>DumbAdapter</code>'s internal <code>java.util.Vector</code>.
        /// </summary>
        /// <param name="address">  device address with which to find a container
        /// </param>
        /// <returns>  The <code>OneWireContainer</code> object, or <code>null</code> if no match could be found. </returns>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        // public override OneWireContainer getDeviceContainer(byte[] address)
        // {
        //long addr = Address.toLong(address);
        //lock (containers)
        //{
        //	for (int i = 0;i < containers.Count;i++)
        //	{
        //		if (((OneWireContainer)containers[i]).AddressAsLong == addr)
        //		{
        //			return (OneWireContainer)containers[i];
        //		}
        //	}
        //}
        //return null;

        // }

        /// <summary>
        /// Gets the container from this adapter whose address matches the address of a container
        /// in the <code>DumbAdapter</code>'s internal <code>java.util.Vector</code>.
        /// </summary>
        /// <param name="address">  device address with which to find a container
        /// </param>
        /// <returns>  The <code>OneWireContainer</code> object, or <code>null</code> if no match could be found. </returns>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override OneWireContainer getDeviceContainer(long address)
        //{
        //return getDeviceContainer(Address.toByteArray(address));
        //}

        /// <summary>
        /// Gets the container from this adapter whose address matches the address of a container
        /// in the <code>DumbAdapter</code>'s internal <code>java.util.Vector</code>.
        /// </summary>
        /// <param name="address">  device address with which to find a container
        /// </param>
        /// <returns>  The <code>OneWireContainer</code> object, or <code>null</code> if no match could be found. </returns>
        /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
        //public override OneWireContainer getDeviceContainer(string address)
        //{
        //return getDeviceContainer(Address.toByteArray(address));
        //}

        /// <summary>
        /// Returns a <code>OneWireContainer</code> object using the current 1-Wire network address.
        /// The internal state of the port adapter keeps track of the last
        /// address found and is able to create container objects from this
        /// state.
        /// </summary>
        /// <returns>  the <code>OneWireContainer</code> object </returns>
        public override OneWireContainer DeviceContainer
        {
            get
            {
                // Mask off the upper bit.
                byte[] address = new byte[8];

                getAddress(address);

                return getDeviceContainer(address);
            }
        }

        /// <summary>
        /// Checks to see if the family found is in the desired
        /// include group.
        /// </summary>
        /// <returns>  <code>true</code> if in include group </returns>
        protected internal override bool isValidFamily(byte[] address)
        {
            byte familyCode = address[0];

            if (exclude != null)
            {
                for (int i = 0; i < exclude.Length; i++)
                {
                    if (familyCode == exclude[i])
                    {
                        return false;
                    }
                }
            }

            if (include != null)
            {
                for (int i = 0; i < include.Length; i++)
                {
                    if (familyCode == include[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        ~UsbAdapter()
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
                if (UsbBuild != null)
                {
                    UsbBuild.Dispose();
                    UsbBuild = null;
                }

                try
                {
                    freePort();
                }
                catch (Exception)
                {
                    ;
                }
            }
        }
    }
}