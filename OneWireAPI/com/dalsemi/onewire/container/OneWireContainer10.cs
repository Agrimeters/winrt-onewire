using System;
using System.Threading;

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

namespace com.dalsemi.onewire.container
{

	// imports
	using com.dalsemi.onewire;
	using com.dalsemi.onewire.utils;
	using com.dalsemi.onewire.adapter;


	/// <summary>
	/// <P> 1-Wire container for temperature iButton which measures temperatures
	/// from -55&#176C to +100&#176C, DS1920 or DS18S20.  This container encapsulates the
	/// functionality of the iButton family type <B>10</B> (hex)</P>
	/// 
	/// <H3> Features </H3>
	/// <UL>
	///   <LI> Measures temperatures from -55&#176C to +100&#176C in typically 0.2 seconds
	///   <LI> Zero standby power
	///   <LI> 0.5&#176C resolution, digital temperature reading in two�s complement
	///   <LI> Increased resolution through interpolation in internal counters
	///   <LI> 8-bit device-generated CRC for data integrity
	///   <LI> Special command set allows user to skip ROM section and do temperature
	///        measurements simultaneously for all devices on the bus
	///   <LI> 2 bytes of EEPROM to be used either as alarm triggers or user memory
	///   <LI> Alarm search directly indicates which device senses alarming temperatures
	/// </UL>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// <DL>
	/// <DD> See the usage example in
	/// <seealso cref="com.dalsemi.onewire.container.TemperatureContainer TemperatureContainer"/>
	/// for temperature specific operations.
	/// </DL>
	/// 
	/// <H3> DataSheet </H3>
	/// <DL>
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1920.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1920.pdf</A>
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS18S20.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS18S20.pdf</A>
	/// </DL>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.TemperatureContainer
	/// 
	/// @version    1.00, 1 Sep 2000
	/// @author     DS,JK
	/// Converted to use TemperatureContainer interface 9-1-2000 KLA </seealso>
	public class OneWireContainer10 : OneWireContainer, TemperatureContainer
	{
	   private bool normalResolution = true;

	   //--------
	   //-------- Static Final Variables
	   //--------

	   /// <summary>
	   /// default temperature resolution for this <code>OneWireContainer10</code>
	   /// device.
	   /// </summary>
	   public const double RESOLUTION_NORMAL = 0.5;

	   /// <summary>
	   /// maximum temperature resolution for this <code>OneWireContainer10</code>
	   /// device. Use <code>RESOLUTION_MAXIMUM</code> in
	   /// <code>setResolution()</code> if higher resolution is desired.
	   /// </summary>
	   public const double RESOLUTION_MAXIMUM = 0.1;

	   /// <summary>
	   /// DS1920 convert temperature command </summary>
	   private const byte CONVERT_TEMPERATURE_COMMAND = 0x44;

	   /// <summary>
	   /// DS1920 read data from scratchpad command </summary>
	   private static readonly byte READ_SCRATCHPAD_COMMAND = unchecked((byte) 0xBE);

	   /// <summary>
	   /// DS1920 write data to scratchpad command </summary>
	   private static readonly byte WRITE_SCRATCHPAD_COMMAND = (byte) 0x4E;

	   /// <summary>
	   /// DS1920 copy data from scratchpad to EEPROM command </summary>
	   private static readonly byte COPY_SCRATCHPAD_COMMAND = (byte) 0x48;

	   /// <summary>
	   /// DS1920 recall EEPROM command </summary>
	   //TODO private static readonly byte RECALL_EEPROM_COMMAND = unchecked((byte) 0xB8);


	   /// <summary>
	   /// Creates an empty <code>OneWireContainer10</code>.  Must call
	   /// <code>setupContainer()</code> before using this new container.<para>
	   /// 
	   /// This is one of the methods to construct a <code>OneWireContainer10</code>.
	   /// The others are through creating a <code>OneWireContainer10</code> with
	   /// parameters.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,String) </seealso>
	   public OneWireContainer10() : base()
	   {
	   }

	   /// <summary>
	   /// Creates a <code>OneWireContainer10</code> with the provided adapter
	   /// object and the address of this One-Wire device.
	   /// 
	   /// This is one of the methods to construct a <code>OneWireContainer10</code>.
	   /// The others are through creating a <code>OneWireContainer10</code> with
	   /// different parameters types.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this One-Wire device </param>
	   /// <param name="newAddress">        address of this One-Wire device
	   /// </param>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref= #OneWireContainer10() </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,String) </seealso>
	   public OneWireContainer10(DSPortAdapter sourceAdapter, byte[] newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Creates a <code>OneWireContainer10</code> with the provided adapter
	   /// object and the address of this One-Wire device.
	   /// 
	   /// This is one of the methods to construct a <code>OneWireContainer10</code>.
	   /// The others are through creating a <code>OneWireContainer10</code> with
	   /// different parameters types.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this One-Wire device </param>
	   /// <param name="newAddress">        address of this One-Wire device
	   /// </param>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref= #OneWireContainer10() </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,String) </seealso>
	   public OneWireContainer10(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Creates a <code>OneWireContainer10</code> with the provided adapter
	   /// object and the address of this One-Wire device.
	   /// 
	   /// This is one of the methods to construct a <code>OneWireContainer10</code>.
	   /// The others are through creating a <code>OneWireContainer10</code> with
	   /// different parameters types.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this One-Wire device </param>
	   /// <param name="newAddress">        address of this One-Wire device
	   /// </param>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref= #OneWireContainer10() </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer10(DSPortAdapter,long) </seealso>
	   public OneWireContainer10(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   //--------
	   //-------- Information methods
	   //--------

	   /// <summary>
	   /// Retrieves the Dallas Semiconductor part number of this
	   /// <code>OneWireContainer10</code> as a <code>String</code>.
	   /// For example 'DS1920'.
	   /// </summary>
	   /// <returns> this <code>OneWireContainer10</code> name </returns>
	   public override string Name
	   {
		   get
		   {
			  return "DS1920";
		   }
	   }

	   /// <summary>
	   /// Retrieves the alternate Dallas Semiconductor part numbers or names.
	   /// A 'family' of 1-Wire Network devices may have more than one part number
	   /// depending on packaging.  There can also be nicknames such as
	   /// 'Crypto iButton'.
	   /// </summary>
	   /// <returns> this <code>OneWireContainer10</code> alternate names </returns>
	   public override string AlternateNames
	   {
		   get
		   {
			  return "DS18S20";
		   }
	   }

	   /// <summary>
	   /// Retrieves a short description of the function of this
	   /// <code>OneWireContainer10</code> type.
	   /// </summary>
	   /// <returns> <code>OneWireContainer10</code> functional description </returns>
	   public override string Description
	   {
		   get
		   {
			  return "Digital thermometer measures temperatures from " + "-55C to 100C in typically 0.2 seconds.  +/- 0.5C " + "Accuracy between 0C and 70C. 0.5C standard " + "resolution, higher resolution through interpolation.  " + "Contains high and low temperature set points for " + "generation of alarm.";
		   }
	   }

	   //--------
	   //-------- Custom Methods for OneWireContainer10
	   //--------
	   //--------
	   //-------- Temperature Feature methods
	   //--------

	   /// <summary>
	   /// Checks to see if this temperature measuring device has high/low
	   /// trip alarms.
	   /// </summary>
	   /// <returns> <code>true</code> if this <code>OneWireContainer10</code>
	   ///         has high/low trip alarms
	   /// </returns>
	   /// <seealso cref=    #getTemperatureAlarm </seealso>
	   /// <seealso cref=    #setTemperatureAlarm </seealso>
	   public virtual bool hasTemperatureAlarms()
	   {
		  return true;
	   }

	   /// <summary>
	   /// Checks to see if this device has selectable temperature resolution.
	   /// </summary>
	   /// <returns> <code>true</code> if this <code>OneWireContainer10</code>
	   ///         has selectable temperature resolution
	   /// </returns>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   public virtual bool hasSelectableTemperatureResolution()
	   {
		  return true;
	   }

	   /// <summary>
	   /// Gets an array of available temperature resolutions in Celsius.
	   /// </summary>
	   /// <returns> byte array of available temperature resolutions in Celsius for
	   ///         this <code>OneWireContainer10</code>. The minimum resolution is
	   ///         returned as the first element and maximum resolution as the last
	   ///         element.
	   /// </returns>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   public virtual double[] TemperatureResolutions
	   {
		   get
		   {
			  double[] resolutions = new double [2];
    
			  resolutions [0] = RESOLUTION_NORMAL;
			  resolutions [1] = RESOLUTION_MAXIMUM;
    
			  return resolutions;
		   }
	   }

	   /// <summary>
	   /// Gets the temperature alarm resolution in Celsius.
	   /// </summary>
	   /// <returns> temperature alarm resolution in Celsius for this
	   ///         <code>OneWireContainer10</code>
	   /// </returns>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #getTemperatureAlarm </seealso>
	   /// <seealso cref=    #setTemperatureAlarm
	   ///  </seealso>
	   public virtual double TemperatureAlarmResolution
	   {
		   get
		   {
			  return 1.0;
		   }
	   }

	   /// <summary>
	   /// Gets the maximum temperature in Celsius.
	   /// </summary>
	   /// <returns> maximum temperature in Celsius for this
	   ///         <code>OneWireContainer10</code>
	   /// </returns>
	   /// <seealso cref=    #getMinTemperature </seealso>
	   public virtual double MaxTemperature
	   {
		   get
		   {
			  return 100.0;
		   }
	   }

	   /// <summary>
	   /// Gets the minimum temperature in Celsius.
	   /// </summary>
	   /// <returns> minimum temperature in Celsius for this
	   ///         <code>OneWireContainer10</code>
	   /// </returns>
	   /// <seealso cref=    #getMaxTemperature </seealso>
	   public virtual double MinTemperature
	   {
		   get
		   {
			  return -55.0;
		   }
	   }

	   //--------
	   //-------- Temperature I/O Methods
	   //--------

	   /// <summary>
	   /// Performs a temperature conversion on <code>state</code> information.
	   /// </summary>
	   /// <param name="state"> byte array with device state information
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref=    #getTemperature </seealso>
	   public virtual void doTemperatureConvert(byte[] state)
	   {
		  doSpeed();

		  // select the device
		  if (adapter.select(address))
		  {

			 // Setup Power Delivery
			 adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);

			 // send the convert temperature command
			 adapter.putByte(CONVERT_TEMPERATURE_COMMAND);

			 // delay for 750 ms
			 try
			 {
				Thread.Sleep(750);
			 }
			 catch (InterruptedException)
			 {
			 }

			 // Turn power back to normal.
			 adapter.setPowerNormal();

			 // check to see if the temperature conversion is over
			 if (adapter.Byte != 0x0FF)
			 {
				throw new OneWireIOException("OneWireContainer10-temperature conversion not complete");
			 }

			 // read the result
			 byte mode = state [4]; //preserve the resolution in the state

			 adapter.select(address);
			 readScratch(state);

			 state [4] = mode;
		  }
		  else

		  {
			 // device must not have been present
			 throw new OneWireIOException("OneWireContainer10-device not present");
		  }
	   }

	   //--------
	   //-------- Temperature 'get' Methods
	   //--------

	   /// <summary>
	   /// Gets the temperature value in Celsius from the <code>state</code>
	   /// data retrieved from the <code>readDevice()</code> method.
	   /// </summary>
	   /// <param name="state"> byte array with device state information for this
	   ///         <code>OneWireContainer10</code>
	   /// </param>
	   /// <returns> temperature in Celsius from the last
	   ///                     <code>doTemperatureConvert()</code>
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'.
	   /// </exception>
	   /// <seealso cref=    #doTemperatureConvert </seealso>
	   public virtual double getTemperature(byte[] state)
	   {

		  //on some parts, namely the 18S20, you can get invalid readings.
		  //basically, the detection is that all the upper 8 bits should
		  //be the same by sign extension.  the error condition (DS18S20
		  //returns 185.0+) violated that condition
		  if (((state [1] & 0x0ff) != 0x00) && ((state [1] & 0x0ff) != 0x0FF))
		  {
			 throw new OneWireIOException("Invalid temperature data!");
		  }

		  short temp = (short)((state [0] & 0x0ff) | (state [1] << 8));

		  if (state [4] == 1)
		  {
			 temp = (short)(temp >> 1); //lop off the last bit

			 //also takes care of the / 2.0
			 double tmp = (double) temp;
			 double cr = (state [6] & 0x0ff);
			 double cpc = (state [7] & 0x0ff);

			 //just let the thing throw a divide by zero exception
			 tmp = tmp - (double) 0.25 + (cpc - cr) / cpc;

			 return tmp;
		  }
		  else
		  {

			 //do normal resolution
			 return temp / 2.0;
		  }
	   }

	   /// <summary>
	   /// Gets the specified temperature alarm value in Celsius from the
	   /// <code>state</code> data retrieved from the  <code>readDevice()</code>
	   /// method.
	   /// </summary>
	   /// <param name="alarmType"> valid value: <code>ALARM_HIGH</code> or
	   ///                   <code>ALARM_LOW</code> </param>
	   /// <param name="state">     byte array with device state information
	   /// </param>
	   /// <returns> temperature alarm trip values in Celsius for this
	   ///         <code>OneWireContainer10</code>
	   /// </returns>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #setTemperatureAlarm </seealso>
	   public virtual double getTemperatureAlarm(int alarmType, byte[] state)
	   {
		  return (double) state [alarmType == TemperatureContainer_Fields.ALARM_LOW ? 3 : 2];
	   }

	   /// <summary>
	   /// Gets the current temperature resolution in Celsius from the
	   /// <code>state</code> data retrieved from the <code>readDevice()</code>
	   /// method.
	   /// </summary>
	   /// <param name="state"> byte array with device state information
	   /// </param>
	   /// <returns> temperature resolution in Celsius for this
	   ///         <code>OneWireContainer10</code>
	   /// </returns>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   public virtual double getTemperatureResolution(byte[] state)
	   {
		  if (state [4] == 0)
		  {
			 return RESOLUTION_NORMAL;
		  }

		  return RESOLUTION_MAXIMUM;
	   }

	   //--------
	   //-------- Temperature 'set' Methods
	   //--------

	   /// <summary>
	   /// Sets the temperature alarm value in Celsius in the provided
	   /// <code>state</code> data.
	   /// Use the method <code>writeDevice()</code> with
	   /// this data to finalize the change to the device.
	   /// </summary>
	   /// <param name="alarmType">  valid value: <code>ALARM_HIGH</code> or
	   ///                    <code>ALARM_LOW</code> </param>
	   /// <param name="alarmValue"> alarm trip value in Celsius </param>
	   /// <param name="state">      byte array with device state information
	   /// </param>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #getTemperatureAlarm </seealso>
	   public virtual void setTemperatureAlarm(int alarmType, double alarmValue, byte[] state)
	   {
		  if ((alarmType != TemperatureContainer_Fields.ALARM_LOW) && (alarmType != TemperatureContainer_Fields.ALARM_HIGH))
		  {
			 throw new System.ArgumentException("Invalid alarm type.");
		  }

		  if (alarmValue > 100.0 || alarmValue < -55.0)
		  {
			 throw new System.ArgumentException("Value for alarm not in accepted range.  Must be -55 C <-> +100 C.");
		  }

		  state [(alarmType == TemperatureContainer_Fields.ALARM_LOW) ? 3 : 2] = (byte) alarmValue;
	   }

	   /// <summary>
	   /// Sets the current temperature resolution in Celsius in the provided
	   /// <code>state</code> data.   Use the method <code>writeDevice()</code>
	   /// with this data to finalize the change to the device.
	   /// </summary>
	   /// <param name="resolution"> temperature resolution in Celsius. Valid values are
	   ///                    <code>RESOLUTION_NORMAL</code> and
	   ///                    <code>RESOLUTION_MAXIMUM</code>. </param>
	   /// <param name="state">      byte array with device state information
	   /// </param>
	   /// <seealso cref=    #RESOLUTION_NORMAL </seealso>
	   /// <seealso cref=    #RESOLUTION_MAXIMUM </seealso>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   public virtual void setTemperatureResolution(double resolution, byte[] state)
	   {
		  lock (this)
		  {
			 if (resolution == RESOLUTION_NORMAL)
			 {
				normalResolution = true;
			 }
			 else
			 {
				normalResolution = false;
			 }

			 state [4] = (byte)(normalResolution ? 0 : 1);
		  }
	   }

	   /// <summary>
	   /// Retrieves this <code>OneWireContainer10</code> state information.
	   /// The state information is returned as a byte array.  Pass this byte
	   /// array to the '<code>get</code>' and '<code>set</code>' methods.
	   /// If the device state needs to be changed, then call the
	   /// <code>writeDevice()</code> to finalize the changes.
	   /// </summary>
	   /// <returns> <code>OneWireContainer10</code> state information.
	   /// Device state looks like this:
	   /// <pre>
	   ///   0 : temperature LSB
	   ///   1 : temperature MSB
	   ///   2 : trip high
	   ///   3 : trip low
	   ///   4 : reserved (put the resolution here, 0 for normal, 1 for max)
	   ///   5 : reserved
	   ///   6 : count remain
	   ///   7 : count per degree Celsius
	   ///   8 : an 8 bit CRC over the previous 8 bytes of data
	   /// </pre>
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref=    #writeDevice </seealso>
	   public virtual byte[] readDevice()
	   {

		  byte[] data = new byte [8];

		  doSpeed();

		  // select the device
		  if (adapter.select(address))
		  {

			 // construct a block to read the scratchpad
			 byte[] buffer = new byte [10];

			 // read scratchpad command
			 buffer [0] = (byte) READ_SCRATCHPAD_COMMAND;

			 // now add the read bytes for data bytes and crc8
			 for (int i = 1; i < 10; i++)
			 {
				buffer [i] = unchecked((byte) 0x0FF);
			 }

			 // send the block
			 adapter.dataBlock(buffer, 0, buffer.Length);

			 // see if crc is correct
			 if (CRC8.compute(buffer, 1, 9) == 0)
			 {
				Array.Copy(buffer, 1, data, 0, 8);
			 }
			 else
			 {
				throw new OneWireIOException("OneWireContainer10-Error reading CRC8 from device.");
			 }
		  }
		  else
		  {
			 throw new OneWireIOException("OneWireContainer10-Device not found on 1-Wire Network");
		  }

		  //we are just reading normalResolution here, no need to synchronize
		  data [4] = (byte)(normalResolution ? 0 : 1);

		  return data;
	   }

	   /// <summary>
	   /// Writes to this <code>OneWireContainer10</code> <code>state</code>
	   /// information that have been changed by '<code>set</code>' methods.
	   /// Only the state registers that changed are updated.  This is done
	   /// by referencing a field information appended to the state data.
	   /// </summary>
	   /// <param name="state">      byte array with device state information
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref=    #readDevice </seealso>
	   public virtual void writeDevice(byte[] state)
	   {
		  doSpeed();

		  byte[] temp = new byte [2];

		  temp [0] = state [2];
		  temp [1] = state [3];

		  // Write it to the Scratchpad.
		  writeScratchpad(temp);

		  // Place in memory.
		  copyScratchpad();
	   }

	   /// <summary>
	   /// Converts a temperature reading from Celsius to Fahrenheit.
	   /// </summary>
	   /// <param name="celsiusTemperature"> temperature value in Celsius
	   /// </param>
	   /// <returns>  the Fahrenheit conversion of the supplied temperature
	   /// </returns>
	   /// @deprecated Replace with call to com.dalsemi.onewire.utils.Convert.toFahrenheit()
	   /// 
	   /// <seealso cref= com.dalsemi.onewire.utils.Convert#toFahrenheit(double) </seealso>
	   public static double convertToFahrenheit(double celsiusTemperature)
	   {
		  return Convert.toFahrenheit(celsiusTemperature);
	   }

	   /// <summary>
	   /// Converts a temperature reading from Fahrenheit to Celsius.
	   /// </summary>
	   /// <param name="fahrenheitTemperature"> temperature value in Fahrenheit
	   /// </param>
	   /// <returns>  the Celsius conversion of the supplied temperature
	   /// </returns>
	   /// @deprecated Replace with call to com.dalsemi.onewire.utils.Convert.toCelsius()
	   /// 
	   /// <seealso cref= com.dalsemi.onewire.utils.Convert#toCelsius(double) </seealso>
	   public static double convertToCelsius(double fahrenheitTemperature)
	   {
		  return Convert.toCelsius(fahrenheitTemperature);
	   }

	   //--------
	   //-------- Private Methods
	   //--------

	   /// <summary>
	   /// Reads the 8 bytes from the scratchpad and verify CRC8 returned.
	   /// </summary>
	   /// <param name="data">  buffer to store the scratchpad data
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   private void readScratch(byte[] data)
	   {

		  // select the device
		  if (adapter.select(address))
		  {

			 // construct a block to read the scratchpad
			 byte[] buffer = new byte [10];

			 // read scratchpad command
			 buffer [0] = (byte) READ_SCRATCHPAD_COMMAND;

			 // now add the read bytes for data bytes and crc8
			 for (int i = 1; i < 10; i++)
			 {
				buffer [i] = unchecked((byte) 0x0FF);
			 }

			 // send the block
			 adapter.dataBlock(buffer, 0, buffer.Length);

			 // see if crc is correct
			 if (CRC8.compute(buffer, 1, 9) == 0)
			 {
				Array.Copy(buffer, 1, data, 0, 8);
			 }
			 else
			 {
				throw new OneWireIOException("OneWireContainer10-Error reading CRC8 from device.");
			 }
		  }
		  else
		  {
			 throw new OneWireIOException("OneWireContainer10-Device not found on 1-Wire Network");
		  }
	   }

	   /// <summary>
	   /// Writes to the Scratchpad.
	   /// </summary>
	   /// <param name="data"> this is the data to be written to the scratchpad.  Cannot
	   ///             be more than two bytes in size. First byte of data must be
	   ///             the temperature High Trip Point and second byte must be
	   ///             temperature Low Trip Point.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   /// <exception cref="IllegalArgumentException"> when data length is not equal to <code>2</code> </exception>
	   private void writeScratchpad(byte[] data)
	   {

		  // Variables.
		  byte[] write_block = new byte [3];
		  byte[] buffer = new byte [8];

		  // First do some error checking.
		  if (data.Length != 2)
		  {
			 throw new System.ArgumentException("Bad data.  Data must consist of only TWO bytes.");
		  }

		  // Prepare the write_block to be sent.
		  write_block [0] = WRITE_SCRATCHPAD_COMMAND;
		  write_block [1] = data [0];
		  write_block [2] = data [1];

		  // Send the block of data to the DS1920.
		  if (adapter.select(address))
		  {
			 adapter.dataBlock(write_block, 0, 3);
		  }
		  else
		  {
			 throw new OneWireIOException("OneWireContainer10 - Device not found");
		  }

		  // Check data to ensure correctly recived.
		  buffer = new byte [8];

		  readScratch(buffer);

		  // verify data
		  if ((buffer [2] != data [0]) || (buffer [3] != data [1]))
		  {
			 throw new OneWireIOException("OneWireContainer10 - data read back incorrect");
		  }

		  return;
	   }

	   /// <summary>
	   /// Copies the contents of the User bytes of the ScratchPad to the EEPROM.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from this <code>OneWireContainer10</code>.
	   ///         This could be caused by a physical interruption in the 1-Wire
	   ///         Network due to shorts or a newly arriving 1-Wire device issuing a
	   ///         'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   private void copyScratchpad()
	   {

		  // select the device
		  if (adapter.select(address))
		  {

			 // send the copy command
			 adapter.putByte(COPY_SCRATCHPAD_COMMAND);

			 // Setup Power Delivery
			 adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 adapter.startPowerDelivery(DSPortAdapter.CONDITION_NOW);

			 // delay for 10 ms
			 try
			 {
				Thread.Sleep(10);
			 }
			 catch (InterruptedException)
			 {
			 }

			 // Turn power back to normal.
			 adapter.setPowerNormal();
		  }
		  else
		  {
			 throw new OneWireIOException("OneWireContainer10 - device not found");
		  }
	   }
	}

}