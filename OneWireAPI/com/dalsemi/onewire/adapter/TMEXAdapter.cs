using System;
using System.Collections;
using System.Diagnostics;

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


    /// <summary>
    /// The DSPortAdapter class for all TMEX native adapters (Win32).
    /// 
    /// Instances of valid DSPortAdapter's are retrieved from methods in
    /// <seealso cref="com.dalsemi.onewire.OneWireAccessProvider OneWireAccessProvider"/>.
    /// 
    /// <P>The TMEXAdapter methods can be organized into the following categories: </P>
    /// <UL>
    ///   <LI> <B> Information </B>
    ///     <UL>
    ///       <LI> <seealso cref="#getAdapterName() getAdapterName"/>
    ///       <LI> <seealso cref="#getPortTypeDescription() getPortTypeDescription"/>
    ///       <LI> <seealso cref="#getClassVersion() getClassVersion"/>
    ///       <LI> <seealso cref="#adapterDetected() adapterDetected"/>
    ///       <LI> <seealso cref="#getAdapterVersion() getAdapterVersion"/>
    ///       <LI> <seealso cref="#getAdapterAddress() getAdapterAddress"/>
    ///     </UL>
    ///   <LI> <B> Port Selection </B>
    ///     <UL>
    ///       <LI> <seealso cref="#getPortNames() getPortNames"/>
    ///       <LI> <seealso cref="#selectPort(String) selectPort"/>
    ///       <LI> <seealso cref="#getPortName() getPortName"/>
    ///       <LI> <seealso cref="#freePort() freePort"/>
    ///     </UL>
    ///   <LI> <B> Adapter Capabilities </B>
    ///     <UL>
    ///       <LI> <seealso cref="#canOverdrive() canOverdrive"/>
    ///       <LI> <seealso cref="#canHyperdrive() canHyperdrive"/>
    ///       <LI> <seealso cref="#canFlex() canFlex"/>
    ///       <LI> <seealso cref="#canProgram() canProgram"/>
    ///       <LI> <seealso cref="#canDeliverPower() canDeliverPower"/>
    ///       <LI> <seealso cref="#canDeliverSmartPower() canDeliverSmartPower"/>
    ///       <LI> <seealso cref="#canBreak() canBreak"/>
    ///     </UL>
    ///   <LI> <B> 1-Wire Network Semaphore </B>
    ///     <UL>
    ///       <LI> <seealso cref="#beginExclusive(bool) beginExclusive"/>
    ///       <LI> <seealso cref="#endExclusive() endExclusive"/>
    ///     </UL>
    ///   <LI> <B> 1-Wire Device Discovery </B>
    ///     <UL>
    ///       <LI> Selective Search Options
    ///         <UL>
    ///          <LI> <seealso cref="#targetAllFamilies() targetAllFamilies"/>
    ///          <LI> <seealso cref="#targetFamily(int) targetFamily(int)"/>
    ///          <LI> <seealso cref="#targetFamily(byte[]) targetFamily(byte[])"/>
    ///          <LI> <seealso cref="#excludeFamily(int) excludeFamily(int)"/>
    ///          <LI> <seealso cref="#excludeFamily(byte[]) excludeFamily(byte[])"/>
    ///          <LI> <seealso cref="#setSearchOnlyAlarmingDevices() setSearchOnlyAlarmingDevices"/>
    ///          <LI> <seealso cref="#setNoResetSearch() setNoResetSearch"/>
    ///          <LI> <seealso cref="#setSearchAllDevices() setSearchAllDevices"/>
    ///         </UL>
    ///       <LI> Search With Automatic 1-Wire Container creation
    ///         <UL>
    ///          <LI> <seealso cref="#getAllDeviceContainers() getAllDeviceContainers"/>
    ///          <LI> <seealso cref="#getFirstDeviceContainer() getFirstDeviceContainer"/>
    ///          <LI> <seealso cref="#getNextDeviceContainer() getNextDeviceContainer"/>
    ///         </UL>
    ///       <LI> Search With NO 1-Wire Container creation
    ///         <UL>
    ///          <LI> <seealso cref="#findFirstDevice() findFirstDevice"/>
    ///          <LI> <seealso cref="#findNextDevice() findNextDevice"/>
    ///          <LI> <seealso cref="#getAddress(byte[]) getAddress(byte[])"/>
    ///          <LI> <seealso cref="#getAddressAsLong() getAddressAsLong"/>
    ///          <LI> <seealso cref="#getAddressAsString() getAddressAsString"/>
    ///         </UL>
    ///       <LI> Manual 1-Wire Container creation
    ///         <UL>
    ///          <LI> <seealso cref="#getDeviceContainer(byte[]) getDeviceContainer(byte[])"/>
    ///          <LI> <seealso cref="#getDeviceContainer(long) getDeviceContainer(long)"/>
    ///          <LI> <seealso cref="#getDeviceContainer(String) getDeviceContainer(String)"/>
    ///          <LI> <seealso cref="#getDeviceContainer() getDeviceContainer()"/>
    ///         </UL>
    ///     </UL>
    ///   <LI> <B> 1-Wire Network low level access (usually not called directly) </B>
    ///     <UL>
    ///       <LI> Device Selection and Presence Detect
    ///         <UL>
    ///          <LI> <seealso cref="#isPresent(byte[]) isPresent(byte[])"/>
    ///          <LI> <seealso cref="#isPresent(long) isPresent(long)"/>
    ///          <LI> <seealso cref="#isPresent(String) isPresent(String)"/>
    ///          <LI> <seealso cref="#isAlarming(byte[]) isAlarming(byte[])"/>
    ///          <LI> <seealso cref="#isAlarming(long) isAlarming(long)"/>
    ///          <LI> <seealso cref="#isAlarming(String) isAlarming(String)"/>
    ///          <LI> <seealso cref="#select(byte[]) select(byte[])"/>
    ///          <LI> <seealso cref="#select(long) select(long)"/>
    ///          <LI> <seealso cref="#select(String) select(String)"/>
    ///         </UL>
    ///       <LI> Raw 1-Wire IO
    ///         <UL>
    ///          <LI> <seealso cref="#reset() reset"/>
    ///          <LI> <seealso cref="#putBit(bool) putBit"/>
    ///          <LI> <seealso cref="#getBit() getBit"/>
    ///          <LI> <seealso cref="#putByte(int) putByte"/>
    ///          <LI> <seealso cref="#getByte() getByte"/>
    ///          <LI> <seealso cref="#getBlock(int) getBlock(int)"/>
    ///          <LI> <seealso cref="#getBlock(byte[], int) getBlock(byte[], int)"/>
    ///          <LI> <seealso cref="#getBlock(byte[], int, int) getBlock(byte[], int, int)"/>
    ///          <LI> <seealso cref="#dataBlock(byte[], int, int) dataBlock(byte[], int, int)"/>
    ///         </UL>
    ///       <LI> 1-Wire Speed and Power Selection
    ///         <UL>
    ///          <LI> <seealso cref="#setPowerDuration(int) setPowerDuration"/>
    ///          <LI> <seealso cref="#startPowerDelivery(int) startPowerDelivery"/>
    ///          <LI> <seealso cref="#setProgramPulseDuration(int) setProgramPulseDuration"/>
    ///          <LI> <seealso cref="#startProgramPulse(int) startProgramPulse"/>
    ///          <LI> <seealso cref="#startBreak() startBreak"/>
    ///          <LI> <seealso cref="#setPowerNormal() setPowerNormal"/>
    ///          <LI> <seealso cref="#setSpeed(int) setSpeed"/>
    ///          <LI> <seealso cref="#getSpeed() getSpeed"/>
    ///         </UL>
    ///     </UL>
    ///   <LI> <B> Advanced </B>
    ///     <UL>
    ///        <LI> <seealso cref="#registerOneWireContainerClass(int, Class) registerOneWireContainerClass"/>
    ///     </UL>
    ///  </UL>
    /// </summary>
    /// <seealso cref= com.dalsemi.onewire.OneWireAccessProvider </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer
    /// 
    /// @version    0.01, 20 March 2001
    /// @author     DS </seealso>
    public class TMEXAdapter : DSPortAdapter
	{

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// flag to intidate if native driver got loaded </summary>
	   private static bool driverLoaded = false;

	   /// <summary>
	   /// TMEX port type number (0-15) </summary>
	   protected internal int portType;

	   /// <summary>
	   /// Current 1-Wire Network Address </summary>
	   protected internal sbyte[] RomDta = new sbyte [8];

	   /// <summary>
	   /// Flag to indicate next search will look only for alarming devices </summary>
	   private bool doAlarmSearch = false;

	   /// <summary>
	   /// Flag to indicate next search will be a 'first' </summary>
	   private bool resetSearch = true;

	   /// <summary>
	   /// Flag to indicate next search will not be preceeded by a 1-Wire reset </summary>
	   private bool skipResetOnSearch = false;

	   //--------
	   //-------- Constructors/Destructor
	   //--------

	   /// <summary>
	   /// Constructs a default adapter
	   /// </summary>
	   /// <exception cref="ClassNotFoundException"> </exception>
	   public TMEXAdapter()
	   {

		  // check if native driver got loaded
		  if (!driverLoaded)
		  {
			 throw new System.IO.FileNotFoundException("native driver 'ibtmjava.dll' not loaded");
		  }

		  // set default port type
		  portType = getDefaultTypeNumber();

		  // attempt to set the portType, will throw exception if does not exist
		  if (!setPortType_Native(portType))
		  {
			 throw new System.TypeLoadException("TMEX adapter type does not exist");
		  }
	   }

	   /// <summary>
	   /// Constructs with a specified port type
	   /// 
	   /// </summary>
	   /// <param name="newPortType"> </param>
	   /// <exception cref="ClassNotFoundException"> </exception>
	   public TMEXAdapter(int newPortType)
	   {

		  // set default port type
		  portType = newPortType;

		  // check if native driver got loaded
		  if (!driverLoaded)
		  {
			 throw new System.IO.FileNotFoundException("native driver 'ibtmjava.dll' not loaded");
		  }

		  // attempt to set the portType, will throw exception if does not exist
		  if (!setPortType_Native(portType))
		  {
			 throw new System.TypeLoadException("TMEX adapter type does not exist");
		  }
	   }

	   /// <summary>
	   /// Finalize to Cleanup native
	   /// </summary>
	   ~TMEXAdapter()
	   {
		  cleanup_Native();
	   }

	   //--------
	   //-------- Methods
	   //--------

	   /// <summary>
	   /// Retrieve the name of the port adapter as a string.  The 'Adapter'
	   /// is a device that connects to a 'port' that allows one to
	   /// communicate with an iButton or other 1-Wire device.  As example
	   /// of this is 'DS9097U'.
	   /// </summary>
	   /// <returns>  <code>String</code> representation of the port adapter. </returns>
	   //TODO [DllImport("unknown")]
	   public extern String getAdapterName();

	   /// <summary>
	   /// Retrieve a description of the port required by this port adapter.
	   /// An example of a 'Port' would 'serial communication port'.
	   /// </summary>
	   /// <returns>  <code>String</code> description of the port type required. </returns>
	   //TODO [DllImport("unknown")]
	   public extern String getPortTypeDescription();

	   /// <summary>
	   /// Retrieve a version string for this class.
	   /// </summary>
	   /// <returns>  version string </returns>
	   public override string ClassVersion
	   {
		   get
		   {
			  return "0.01, native: " + getVersion_Native();
		   }
	   }

	   //--------
	   //-------- Port Selection
	   //--------

	   /// <summary>
	   /// Retrieve a list of the platform appropriate port names for this
	   /// adapter.  A port must be selected with the method 'selectPort'
	   /// before any other communication methods can be used.  Using
	   /// a communcation method before 'selectPort' will result in
	   /// a <code>OneWireException</code> exception.
	   /// </summary>
	   /// <returns>  enumeration of type <code>String</code> that contains the port
	   /// names </returns>
	   public override System.Collections.IEnumerator PortNames
	   {
		   get
		   {
			  ArrayList portVector = new ArrayList();
			  string header = getPortNameHeader_Native();
    
			  for (int i = 0; i < 16; i++)
			  {
				 portVector.Add(header + Convert.ToString(i));
			  }
    
			  return (portVector.GetEnumerator());
		   }
	   }

	   /// <summary>
	   /// Specify a platform appropriate port name for this adapter.  Note that
	   /// even though the port has been selected, it's ownership may be relinquished
	   /// if it is not currently held in a 'exclusive' block.  This class will then
	   /// try to re-aquire the port when needed.  If the port cannot be re-aquired
	   /// ehen the exception <code>PortInUseException</code> will be thrown.
	   /// </summary>
	   /// <param name="portName">  name of the target port, retrieved from
	   /// getPortNames()
	   /// </param>
	   /// <returns> <code>true</code> if the port was aquired, <code>false</code>
	   /// if the port is not available.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> If port does not exist, or unable to communicate with port. </exception>
	   /// <exception cref="OneWireException"> If port does not exist </exception>
	   //TODO [DllImport("unknown")]
	   //public extern bool selectPort(string portName);

	   /// <summary>
	   /// Free ownership of the selected port if it is currently owned back
	   /// to the system.  This should only be called if the recently
	   /// selected port does not have an adapter or at the end of
	   /// your application's use of the port.
	   /// </summary>
	   /// <exception cref="OneWireException"> If port does not exist </exception>
	   //TODO [DllImport("unknown")]
	   //public extern void freePort();

	   /// <summary>
	   /// Retrieve the name of the selected port as a <code>String</code>.
	   /// </summary>
	   /// <returns>  <code>String</code> of selected port
	   /// </returns>
	   /// <exception cref="OneWireException"> if valid port not yet selected </exception>
	   //TODO [DllImport("unknown")]
	   public extern String getPortName();

	   //--------
	   //-------- Adapter detection
	   //--------

	   /// <summary>
	   /// Detect adapter presence on the selected port.
	   /// </summary>
	   /// <returns>  <code>true</code> if the adapter is confirmed to be connected to
	   /// the selected port, <code>false</code> if the adapter is not connected.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   //TODO [DllImport("unknown")]
	   //public extern bool adapterDetected();

	   /// <summary>
	   /// Retrieve the version of the adapter.
	   /// </summary>
	   /// <returns>  <code>String</code> of the adapter version.  It will return
	   /// "<na>" if the adapter version is not or cannot be known.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         no device present.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern String getAdapterVersion();

	   /// <summary>
	   /// Retrieve the address of the adapter if it has one.
	   /// </summary>
	   /// <returns>  <code>String</code> of the adapter address.  It will return "<na>" if
	   /// the adapter does not have an address.  The address is a string representation of an
	   /// 1-Wire address.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         no device present.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   /// <seealso cref=    com.dalsemi.onewire.utils.Address </seealso>
	   public override string AdapterAddress
	   {
		   get
		   {
			  return "<na>"; //??? implement later
		   }
	   }

	   //--------
	   //-------- Adapter features
	   //--------

	   /* The following interogative methods are provided so that client code
	    * can react selectively to underlying states without generating an
	    * exception.
	    */

	   /// <summary>
	   /// Returns whether adapter can physically support overdrive mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do OverDrive,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canOverdrive();

	   /// <summary>
	   /// Returns whether the adapter can physically support hyperdrive mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do HyperDrive,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canHyperdrive();

	   /// <summary>
	   /// Returns whether the adapter can physically support flex speed mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do flex speed,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canFlex();

	   /// <summary>
	   /// Returns whether adapter can physically support 12 volt power mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do Program voltage,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canProgram();

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
	   //TODO [DllImport("unknown")]
	   public extern bool canDeliverPower();

	   /// <summary>
	   /// Returns whether the adapter can physically support "smart" strong 5
	   /// volt power mode.  "smart" power delivery is the ability to deliver
	   /// power until it is no longer needed.  The current drop it detected
	   /// and power delivery is stopped.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do "smart" strong
	   /// 5 volt mode, <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canDeliverSmartPower();

	   /// <summary>
	   /// Returns whether adapter can physically support 0 volt 'break' mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do break,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool canBreak();

	   //--------
	   //-------- Finding iButtons and 1-Wire devices
	   //--------

	   /// <summary>
	   /// Returns <code>true</code> if the first iButton or 1-Wire device
	   /// is found on the 1-Wire Network.
	   /// If no devices are found, then <code>false</code> will be returned.
	   /// </summary>
	   /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool findFirstDevice()
	   {

		  // reset the internal rom buffer
		  resetSearch = true;

		  return findNextDevice();
	   }

	   /// <summary>
	   /// Returns <code>true</code> if the next iButton or 1-Wire device
	   /// is found. The previous 1-Wire device found is used
	   /// as a starting point in the search.  If no more devices are found
	   /// then <code>false</code> will be returned.
	   /// </summary>
	   /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool findNextDevice()
	   {
		  bool retval;

		  while (true)
		  {
			 retval = romSearch_Native(skipResetOnSearch, resetSearch, doAlarmSearch, RomDta);

			 if (retval)
			 {
				resetSearch = false;

				// check if this is an OK family type
				if (isValidFamily(RomDta))
				{
				   return true;
				}

				// Else, loop to the top and do another search.
			 }
			 else
			 {
				resetSearch = true;

				return false;
			 }
		  }
	   }

	   /// <summary>
	   /// Copies the 'current' iButton address being used by the adapter into
	   /// the array.  This address is the last iButton or 1-Wire device found
	   /// in a search (findNextDevice()...).
	   /// This method copies into a user generated array to allow the
	   /// reuse of the buffer.  When searching many iButtons on the one
	   /// wire network, this will reduce the memory burn rate.
	   /// </summary>
	   /// <param name="address"> An array to be filled with the current iButton address. </param>
	   /// <seealso cref=    com.dalsemi.onewire.utils.Address </seealso>
	   public override void getAddress(sbyte[] address)
	   {
		  Array.Copy(RomDta, 0, address, 0, 8);
	   }

	   /// <summary>
	   /// Verifies that the iButton or 1-Wire device specified is present on
	   /// the 1-Wire Network. This does not affect the 'current' device
	   /// state information used in searches (findNextDevice...).
	   /// </summary>
	   /// <param name="address">  device address to verify is present
	   /// </param>
	   /// <returns>  <code>true</code> if device is present else
	   ///         <code>false</code>.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   /// </exception>
	   /// <seealso cref=    com.dalsemi.onewire.utils.Address </seealso>
	   //TODO [DllImport("unknown")]
	   public extern bool isPresent(sbyte[] address);

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
	   //TODO [DllImport("unknown")]
	   public extern bool isAlarming(sbyte[] address);

	   /// <summary>
	   /// Selects the specified iButton or 1-Wire device by broadcasting its
	   /// address.  This operation is refered to a 'MATCH ROM' operation
	   /// in the iButton and 1-Wire device data sheets.  This does not
	   /// affect the 'current' device state information used in searches
	   /// (findNextDevice...).
	   /// 
	   /// Warning, this does not verify that the device is currently present
	   /// on the 1-Wire Network (See isPresent).
	   /// </summary>
	   /// <param name="address">     iButton to select
	   /// </param>
	   /// <returns>  <code>true</code> if device address was sent,<code>false</code>
	   /// otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   /// </exception>
	   /// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#isPresent(byte[] address) </seealso>
	   /// <seealso cref=  com.dalsemi.onewire.utils.Address </seealso>
	   //TODO [DllImport("unknown")]
	   public extern bool select(sbyte[] address);

	   //--------
	   //-------- Finding iButton/1-Wire device options
	   //--------

	   /// <summary>
	   /// Set the 1-Wire Network search to find only iButtons and 1-Wire
	   /// devices that are in an 'Alarm' state that signals a need for
	   /// attention.  Not all iButton types
	   /// have this feature.  Some that do: DS1994, DS1920, DS2407.
	   /// This selective searching can be canceled with the
	   /// 'setSearchAllDevices()' method.
	   /// </summary>
	   /// <seealso cref= #setNoResetSearch </seealso>
	   public override void setSearchOnlyAlarmingDevices()
	   {
		  doAlarmSearch = true;
	   }

	   /// <summary>
	   /// Set the 1-Wire Network search to not perform a 1-Wire
	   /// reset before a search.  This feature is chiefly used with
	   /// the DS2409 1-Wire coupler.
	   /// The normal reset before each search can be restored with the
	   /// 'setSearchAllDevices()' method.
	   /// </summary>
	   public override void setNoResetSearch()
	   {
		  skipResetOnSearch = true;
	   }

	   /// <summary>
	   /// Set the 1-Wire Network search to find all iButtons and 1-Wire
	   /// devices whether they are in an 'Alarm' state or not and
	   /// restores the default setting of providing a 1-Wire reset
	   /// command before each search. (see setNoResetSearch() method).
	   /// </summary>
	   /// <seealso cref= #setNoResetSearch </seealso>
	   public override void setSearchAllDevices()
	   {
		  doAlarmSearch = false;
		  skipResetOnSearch = false;
	   }

	   //--------
	   //-------- 1-Wire Network Semaphore methods
	   //--------

	   /// <summary>
	   /// Gets exclusive use of the 1-Wire to communicate with an iButton or
	   /// 1-Wire Device.
	   /// This method should be used for critical sections of code where a
	   /// sequence of commands must not be interrupted by communication of
	   /// threads with other iButtons, and it is permissible to sustain
	   /// a delay in the special case that another thread has already been
	   /// granted exclusive access and this access has not yet been
	   /// relinquished. <para>
	   /// 
	   /// It can be called through the OneWireContainer
	   /// class by the end application if they want to ensure exclusive
	   /// use.  If it is not called around several methods then it
	   /// will be called inside each method.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="blocking"> <code>true</code> if want to block waiting
	   ///                 for an excluse access to the adapter </param>
	   /// <returns> <code>true</code> if blocking was false and a
	   ///         exclusive session with the adapter was aquired
	   /// </returns>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   //public extern bool beginExclusive(bool blocking);

	   /// <summary>
	   /// Relinquishes exclusive control of the 1-Wire Network.
	   /// This command dynamically marks the end of a critical section and
	   /// should be used when exclusive control is no longer needed.
	   /// </summary>
	   //TODO [DllImport("unknown")]
	   //public extern void endExclusive();

	   //--------
	   //-------- Primitive 1-Wire Network data methods
	   //--------

	   /// <summary>
	   /// Sends a bit to the 1-Wire Network.
	   /// </summary>
	   /// <param name="bitValue">  the bit value to send to the 1-Wire Network.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void putBit(bool bitValue)
	   {
		  if (dataBit_Native(bitValue) != bitValue)
		  {
			 throw new OneWireIOException("Error during putBit()");
		  }
	   }

	   /// <summary>
	   /// Gets a bit from the 1-Wire Network.
	   /// </summary>
	   /// <returns>  the bit value recieved from the the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool Bit
	   {
		   get
		   {
			  return dataBit_Native(true);
		   }
	   }

	   /// <summary>
	   /// Sends a byte to the 1-Wire Network.
	   /// </summary>
	   /// <param name="byteValue">  the byte value to send to the 1-Wire Network.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void putByte(int byteValue)
	   {
		  if (dataByte_Native(byteValue & 0x00FF) != ((0x00FF) & byteValue))
		  {
			 throw new OneWireIOException("Error during putByte(), echo was incorrect ");
		  }
	   }

	   /// <summary>
	   /// Gets a byte from the 1-Wire Network.
	   /// </summary>
	   /// <returns>  the byte value received from the the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int Byte
	   {
		   get
		   {
			  return dataByte_Native(0x00FF);
		   }
	   }

	   /// <summary>
	   /// Get a block of data from the 1-Wire Network.
	   /// </summary>
	   /// <param name="len">  length of data bytes to receive
	   /// </param>
	   /// <returns>  the data received from the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override sbyte[] getBlock(int len)
	   {
		  sbyte[] barr = new sbyte [len];

		  getBlock(barr, 0, len);

		  return barr;
	   }

	   /// <summary>
	   /// Get a block of data from the 1-Wire Network and write it into
	   /// the provided array.
	   /// </summary>
	   /// <param name="arr">     array in which to write the received bytes </param>
	   /// <param name="len">     length of data bytes to receive
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void getBlock(sbyte[] arr, int len)
	   {
		  getBlock(arr, 0, len);
	   }

	   /// <summary>
	   /// Get a block of data from the 1-Wire Network and write it into
	   /// the provided array.
	   /// </summary>
	   /// <param name="arr">     array in which to write the received bytes </param>
	   /// <param name="off">     offset into the array to start </param>
	   /// <param name="len">     length of data bytes to receive
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   //public extern void getBlock(sbyte[] arr, int off, int len);

	   /// <summary>
	   /// Sends a block of data and returns the data received in the same array.
	   /// This method is used when sending a block that contains reads and writes.
	   /// The 'read' portions of the data block need to be pre-loaded with 0xFF's.
	   /// It starts sending data from the index at offset 'off' for length 'len'.
	   /// </summary>
	   /// <param name="dataBlock">  array of data to transfer to and from the 1-Wire Network. </param>
	   /// <param name="off">        offset into the array of data to start </param>
	   /// <param name="len">        length of data to send / receive starting at 'off'
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   //public extern void dataBlock(sbyte[] dataBlock, int off, int len);

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
	   //TODO [DllImport("unknown")]
	   //public extern int reset();

	   //--------
	   //-------- 1-Wire Network power methods
	   //--------

	   /// <summary>
	   /// Sets the duration to supply power to the 1-Wire Network.
	   /// This method takes a time parameter that indicates the program
	   /// pulse length when the method startPowerDelivery().<para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() and canDeliverSmartPower()
	   /// </para>
	   /// method to check it's availability. <para>
	   /// 
	   /// </para>
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
	   /// </ul>
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int PowerDuration
	   {
		   set
		   {
    
			  // Right now we only support infinite pull up.
			  if (value != DELIVERY_INFINITE)
			  {
				 throw new OneWireException("No support for other than infinite power duration");
			  }
		   }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to supply power to an iButton device.
	   /// This method takes a time parameter that indicates whether the
	   /// power delivery should be done immediately, or after certain
	   /// conditions have been met. <para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() and canDeliverSmartPower()
	   /// </para>
	   /// method to check it's availability. <para>
	   /// 
	   /// </para>
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
	   /// <returns> <code>true</code> if the voltage change was successful,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   public extern bool startPowerDelivery(int changeCondition);

	   /// <summary>
	   /// Sets the duration for providing a program pulse on the
	   /// 1-Wire Network.
	   /// This method takes a time parameter that indicates the program
	   /// pulse length when the method startProgramPulse().<para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() method to check it's
	   /// </para>
	   /// availability. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="timeFactor">
	   /// <ul>
	   /// <li>   6 (DELIVERY_EPROM) provide program pulse for 480 microseconds
	   /// <li>   5 (DELIVERY_INFINITE) provide power until the
	   ///          setPowerNormal() method is called.
	   /// </ul>
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int ProgramPulseDuration
	   {
		   set
		   {
			  if (value != DELIVERY_EPROM)
			  {
				 throw new OneWireException("Only support EPROM length program pulse duration");
			  }
		   }
	   }

        public override string AdapterName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string PortTypeDescription
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string PortName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sets the 1-Wire Network voltage to eprom programming level.
        /// This method takes a time parameter that indicates whether the
        /// power delivery should be done immediately, or after certain
        /// conditions have been met. <para>
        /// 
        /// Note: to avoid getting an exception,
        /// use the canProgram() method to check it's
        /// </para>
        /// availability. <para>
        /// 
        /// </para>
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
        /// <returns> <code>true</code> if the voltage change was successful,
        /// <code>false</code> otherwise.
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
        /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
        ///         or the adapter does not support this operation </exception>
        //TODO [DllImport("unknown")]
        public extern bool startProgramPulse(int changeCondition);

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to 0 volts.  This method is used
	   /// rob all 1-Wire Network devices of parasite power delivery to force
	   /// them into a hard reset.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   //TODO [DllImport("unknown")]
	   public extern void startBreak();

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
	   //TODO [DllImport("unknown")]
	   public extern void setPowerNormal();

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
	   /// <param name="desiredSpeed">
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   //TODO [DllImport("unknown")]
	   public extern void setSpeed(int desiredSpeed);

	   /// <summary>
	   /// This method returns the current data transfer speed through a
	   /// port to a 1-Wire Network. <para>
	   /// 
	   /// @return
	   /// <ul>
	   /// <li>     0 (SPEED_REGULAR) set to normal communication speed
	   /// <li>     1 (SPEED_FLEX) set to flexible communication speed used
	   ///            for long lines
	   /// <li>     2 (SPEED_OVERDRIVE) set to normal communication speed to
	   ///            overdrive
	   /// <li>     3 (SPEED_HYPERDRIVE) set to normal communication speed to
	   ///            hyperdrive
	   /// <li>    >3 future speeds
	   /// </ul>
	   /// </para>
	   /// </summary>
	   //TODO [DllImport("unknown")]
	   public extern int getSpeed();

	   //--------
	   //-------- Misc
	   //--------

	   /// <summary>
	   /// Select the TMEX specified port type (0 to 15)  Use this
	   /// method if the constructor with the PortType cannot be used.
	   /// 
	   /// </summary>
	   /// <param name="newPortType"> </param>
	   /// <returns>  true if port type valid.  Instance is only usable
	   ///          if this returns false. </returns>
	   public virtual bool setTMEXPortType(int newPortType)
	   {

		  // set default port type
		  portType = newPortType;

		  // attempt to set the portType, return result
		  return setPortType_Native(portType);
	   }

	   //--------
	   //-------- Additional Native Methods
	   //--------

	   /// <summary>
	   /// CleanUp the native state for classes owned by the provided
	   /// thread.
	   /// </summary>
	   //TODO [DllImport("unknown")]
	   //public static extern void CleanUpByThread(System.Threading.Thread thread);

	   /// <summary>
	   /// Get the default Adapter Name.
	   /// </summary>
	   /// <returns>  String containing the name of the default adapter </returns>
	   //TODO [DllImport("unknown")]
	   public static extern String getDefaultAdapterName();

	   /// <summary>
	   /// Get the default Adapter Port name.
	   /// </summary>
	   /// <returns>  String containing the name of the default adapter port </returns>
	   //TODO [DllImport("unknown")]
	   public static extern String getDefaultPortName();

	   /// <summary>
	   /// Get the default Adapter Type number.
	   /// </summary>
	   /// <returns>  int, the default adapter type </returns>
	   //TODO [DllImport("unknown")]
	   private static extern int getDefaultTypeNumber();

	   /// <summary>
	   /// Attempt to set the desired TMEX Port type.  This native
	   /// call will attempt to get a session handle to verify that
	   /// the portType exists.
	   /// </summary>
	   /// <returns>  true if portType exists, false if not </returns>
	   //TODO [DllImport("unknown")]
	   private extern bool setPortType_Native(int portType);

	   /// <summary>
	   /// Perform a 1-Wire bit operation
	   /// </summary>
	   /// <param name="bitValue">  bool bit value, true=1, false=0 to send
	   ///                   to 1-Wire net
	   /// </param>
	   /// <returns>  bool true for 1 return , false for 0 return
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   private extern bool dataBit_Native(bool bitValue);

	   /// <summary>
	   /// Perform a 1-Wire byte operation
	   /// </summary>
	   /// <param name="byteValue">  integer with ls byte containing the 8 bits value
	   ///                    to send to the 1-Wire net
	   /// </param>
	   /// <returns>  int containing the 1-Wire return 8 bits in the ls byte.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   private extern int dataByte_Native(int byteValue);

	   /// <summary>
	   /// Get the TMEX main and porttype version strings concatinated
	   /// </summary>
	   /// <returns>  string containing the TMEX version </returns>
	   //TODO [DllImport("unknown")]
	   private extern String getVersion_Native();

	   /// <summary>
	   /// Peform a search
	   /// </summary>
	   /// <param name="skipResetOnSearch">  bool, true to skip 1-Wire reset on search </param>
	   /// <param name="resetSearch">  bool, true to reset search (First) </param>
	   /// <param name="doAlarmSearch"> bool, true if only want to find alarming </param>
	   /// <param name="RomDta">       byte array to hold ROM of device found
	   /// </param>
	   /// <returns>  bool, true if search found a device else false
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   //TODO [DllImport("unknown")]
	   private extern bool romSearch_Native(bool skipResetOnSearch, bool resetSearch, bool doAlarmSearch, sbyte[] RomDta);

	   /// <summary>
	   /// Return the port name header (taken from porttype version)
	   /// </summary>
	   /// <returns>  String containing the port name header </returns>
	   //TODO [DllImport("unknown")]
	   private extern String getPortNameHeader_Native();

	   /// <summary>
	   /// Cleanup native (called on finalize of this instance)
	   /// </summary>
	   //TODO [DllImport("unknown")]
	   private extern void cleanup_Native();

        public override bool selectPort(string portName)
        {
            throw new NotImplementedException();
        }

        public override void freePort()
        {
            throw new NotImplementedException();
        }

        public override bool adapterDetected()
        {
            throw new NotImplementedException();
        }

        public override bool beginExclusive(bool blocking)
        {
            throw new NotImplementedException();
        }

        public override void endExclusive()
        {
            throw new NotImplementedException();
        }

        public override void getBlock(sbyte[] arr, int off, int len)
        {
            throw new NotImplementedException();
        }

        public override void dataBlock(sbyte[] dataBlock, int off, int len)
        {
            throw new NotImplementedException();
        }

        public override int reset()
        {
            throw new NotImplementedException();
        }

        //--------
        //-------- Native driver loading
        //--------

        /// <summary>
        /// Static method called before instance is created.  Attempt
        /// verify native driver's installed and to load the
        /// driver (IBTMJAVA.DLL).
        /// </summary>
        static TMEXAdapter()
	   {
		  driverLoaded = false;

		  // check if on OS that can have native TMEX drivers
		  if ((System.Environment.GetEnvironmentVariable("os.arch").IndexOf("86") != -1) && 
              (System.Environment.GetEnvironmentVariable("os.name").IndexOf("Windows") != -1))
		  {

//			 // check if TMEX native drivers installed
//			 int index = 0, last_index = 0;
//			 string search_path = System.Environment.GetEnvironmentVariable("java.library.path");
//			 string path;
//			 File file;
//			 bool tmex_loaded = false;

//			 // check for a path to search
//			 if (!string.ReferenceEquals(search_path, null))
//			 {
//				// loop to look through the library search path
//				do
//				{
//				   index = search_path.IndexOf(System.IO.Path.PathSeparator, last_index);

//				   if (index > -1)
//				   {
//					  path = search_path.Substring(last_index, index - last_index);

//					  // look to see if IBFS32.DLL is in this path
//					  file = new File(path + File.separator + "IBFS32.DLL");

//					  if (file.exists())
//					  {
//						 tmex_loaded = true;

//						 break;
//					  }
//				   }

//				   last_index = index + 1;
//				} while (index > -1);
//			 }
//			 // jdk must not support "java.library.path" so assume it is loaded
//			 else
//			 {
//				tmex_loaded = true;
//			 }

//			 if (tmex_loaded)
//			 {
//				try
//				{
////TODO				   System.LoadLibrary("ibtmjava");
//				   driverLoaded = true;
//				}
//				catch (System.Exception) //UnsatisfiedLinkError
//                    {
//				   if (!string.ReferenceEquals(search_path, null))
//				   {
//					  Debug.WriteLine("Could not load Java to TMEX-native bridge driver: ibtmjava.dll");
//				   }
//				   else
//				   {
//					  Debug.WriteLine("Native drivers not found, download iButton-TMEX RTE Win32 from www.ibutton.com");
//				   }
//				}
//			 }
//			 else
			 {
				Debug.WriteLine("Native drivers not found, download iButton-TMEX RTE Win32 from www.ibutton.com");
			 }
		  }
	   }
	}

}