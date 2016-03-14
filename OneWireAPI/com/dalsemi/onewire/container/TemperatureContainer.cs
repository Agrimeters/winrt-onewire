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
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;


	/// <summary>
	/// 1-Wire temperature interface class for basic temperature measuring
	/// operations. This class should be implemented for each temperature
	/// type 1-Wire device.
	/// 
	/// 
	/// <P>The TemperatureContainer methods can be organized into the following categories: </P>
	/// <UL>
	///   <LI> <B> Information </B>
	///     <UL>
	///       <LI> <seealso cref="#getMaxTemperature                  getMaxTemperature"/>
	///       <LI> <seealso cref="#getMinTemperature                  getMinTemperature"/>
	///       <LI> <seealso cref="#getTemperature                     getTemperature"/>
	///       <LI> <seealso cref="#getTemperatureAlarm                getTemperatureAlarm"/>
	///       <LI> <seealso cref="#getTemperatureAlarmResolution      getTemperatureAlarmResolution"/>
	///       <LI> <seealso cref="#getTemperatureResolution           getTemperatureResolution"/>
	///       <LI> <seealso cref="#getTemperatureResolutions          getTemperatureResolutions"/>
	///       <LI> <seealso cref="#hasSelectableTemperatureResolution hasSelectableTemperatureResolution"/>
	///       <LI> <seealso cref="#hasTemperatureAlarms               hasTemperatureAlarms"/>
	///     </UL>
	///   <LI> <B> Options </B>
	///     <UL>
	///       <LI> <seealso cref="#doTemperatureConvert     doTemperatureConvert"/>
	///       <LI> <seealso cref="#setTemperatureAlarm      setTemperatureAlarm"/>
	///       <LI> <seealso cref="#setTemperatureResolution setTemperatureResolution"/>
	///     </UL>
	///   <LI> <B> I/O </B>
	///     <UL>
	///       <LI> <seealso cref="#readDevice  readDevice"/>
	///       <LI> <seealso cref="#writeDevice writeDevice"/>
	///     </UL>
	///  </UL>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// <DL>
	/// <DD> <H4> Example 1</H4>
	/// Display some features of TemperatureContainer instance '<code>tc</code>':
	/// <PRE> <CODE>
	///   // Read High and Low Alarms
	///   if (!tc.hasTemperatureAlarms())
	///      System.out.println("Temperature alarms not supported");
	///   else
	///   {
	///      byte[] state     = tc.readDevice();
	///      double alarmLow  = tc.getTemperatureAlarm(TemperatureContainer.ALARM_LOW, state);
	///      double alarmHigh = tc.getTemperatureAlarm(TemperatureContainer.ALARM_HIGH, state);
	///      System.out.println("Alarm: High = " + alarmHigh + ", Low = " + alarmLow);
	///   }             }
	/// </CODE> </PRE>
	/// 
	/// <DD> <H4> Example 2</H4>
	/// Gets temperature reading from a TemperatureContainer instance '<code>tc</code>':
	/// <PRE> <CODE>
	///   double lastTemperature;
	/// 
	///   // get the current resolution and other settings of the device (done only once)
	///   byte[] state = tc.readDevice();
	/// 
	///   do // loop to read the temp
	///   {
	///      // perform a temperature conversion
	///      tc.doTemperatureConvert(state);
	/// 
	///      // read the result of the conversion
	///      state = tc.readDevice();
	/// 
	///      // extract the result out of state
	///      lastTemperature = tc.getTemperature(state);
	///      ...
	/// 
	///   }while (!done);
	/// </CODE> </PRE>
	/// 
	/// The reason the conversion and the reading are separated
	/// is that one may want to do a conversion without reading
	/// the result.  One could take advantage of the alarm features
	/// of a device by setting a threshold and doing conversions
	/// until the device is alarming.  For example:
	/// <PRE> <CODE>
	///   // get the current resolution of the device
	///   byte [] state = tc.readDevice();
	/// 
	///   // set the trips
	///   tc.setTemperatureAlarm(TemperatureContainer.ALARM_HIGH, 50, state);
	///   tc.setTemperatureAlarm(TemperatureContainer.ALARM_LOW, 20, state);
	///   tc.writeDevice(state);
	/// 
	///   do // loop on conversions until an alarm occurs
	///   {
	///      tc.doTemperatureConvert(state);
	///   } while (!tc.isAlarming());
	///   </CODE> </PRE>
	/// 
	/// <DD> <H4> Example 3</H4>
	/// Sets the temperature resolution of a TemperatureContainer instance '<code>tc</code>':
	/// <PRE> <CODE>
	///   byte[] state = tc.readDevice();
	///   if (tc.hasSelectableTemperatureResolution())
	///   {
	///      double[] resolution = tc.getTemperatureResolutions();
	///      tc.setTemperatureResolution(resolution [resolution.length - 1], state);
	///      tc.writeDevice(state);
	///   }
	/// </CODE> </PRE>
	/// </DL>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer10 </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer21 </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer26 </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer28 </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer30
	/// 
	/// @version    0.00, 27 August 2000
	/// @author     DS </seealso>
	public interface TemperatureContainer : OneWireSensor
	{
        //--------
        //-------- Static Final Variables
        //--------

        /// <summary>
        /// high temperature alarm </summary>

        /// <summary>
        /// low temperature alarm </summary>

        //--------
        //-------- Temperature Feature methods
        //--------

        /// <summary>
        /// Checks to see if this temperature measuring device has high/low
        /// trip alarms.
        /// </summary>
        /// <returns> <code>true</code> if this <code>TemperatureContainer</code>
        ///         has high/low trip alarms
        /// </returns>
        /// <seealso cref=    #getTemperatureAlarm </seealso>
        /// <seealso cref=    #setTemperatureAlarm </seealso>
        bool hasTemperatureAlarms();

	   /// <summary>
	   /// Checks to see if this device has selectable temperature resolution.
	   /// </summary>
	   /// <returns> <code>true</code> if this <code>TemperatureContainer</code>
	   ///         has selectable temperature resolution
	   /// </returns>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   bool hasSelectableTemperatureResolution();

	   /// <summary>
	   /// Get an array of available temperature resolutions in Celsius.
	   /// </summary>
	   /// <returns> byte array of available temperature resolutions in Celsius with
	   ///         minimum resolution as the first element and maximum resolution
	   ///         as the last element.
	   /// </returns>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   double[] TemperatureResolutions {get;}

	   /// <summary>
	   /// Gets the temperature alarm resolution in Celsius.
	   /// </summary>
	   /// <returns> temperature alarm resolution in Celsius for this 1-wire device
	   /// </returns>
	   /// <exception cref="OneWireException">         Device does not support temperature
	   ///                                  alarms
	   /// </exception>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #getTemperatureAlarm </seealso>
	   /// <seealso cref=    #setTemperatureAlarm
	   ///  </seealso>
	   double TemperatureAlarmResolution {get;}

	   /// <summary>
	   /// Gets the maximum temperature in Celsius.
	   /// </summary>
	   /// <returns> maximum temperature in Celsius for this 1-wire device </returns>
	   double MaxTemperature {get;}

	   /// <summary>
	   /// Gets the minimum temperature in Celsius.
	   /// </summary>
	   /// <returns> minimum temperature in Celsius for this 1-wire device </returns>
	   double MinTemperature {get;}

	   //--------
	   //-------- Temperature I/O Methods
	   //--------

	   /// <summary>
	   /// Performs a temperature conversion.
	   /// </summary>
	   /// <param name="state"> byte array with device state information
	   /// </param>
	   /// <exception cref="OneWireException">         Part could not be found [ fatal ] </exception>
	   /// <exception cref="OneWireIOException">       Data wasn't transferred properly [ recoverable ] </exception>
	   void doTemperatureConvert(byte[] state);

	   //--------
	   //-------- Temperature 'get' Methods
	   //--------

	   /// <summary>
	   /// Gets the temperature value in Celsius from the <code>state</code>
	   /// data retrieved from the <code>readDevice()</code> method.
	   /// </summary>
	   /// <param name="state"> byte array with device state information
	   /// </param>
	   /// <returns> temperature in Celsius from the last
	   ///                     <code>doTemperatureConvert()</code>
	   /// </returns>
	   /// <exception cref="OneWireIOException"> In the case of invalid temperature data </exception>
	   double getTemperature(byte[] state);

	   /// <summary>
	   /// Gets the specified temperature alarm value in Celsius from the
	   /// <code>state</code> data retrieved from the
	   /// <code>readDevice()</code> method.
	   /// </summary>
	   /// <param name="alarmType"> valid value: <code>ALARM_HIGH</code> or
	   ///                   <code>ALARM_LOW</code> </param>
	   /// <param name="state">     byte array with device state information
	   /// </param>
	   /// <returns> temperature alarm trip values in Celsius for this 1-wire device
	   /// </returns>
	   /// <exception cref="OneWireException">         Device does not support temperature
	   ///                                  alarms
	   /// </exception>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #setTemperatureAlarm </seealso>
	   double getTemperatureAlarm(int alarmType, byte[] state);

	   /// <summary>
	   /// Gets the current temperature resolution in Celsius from the
	   /// <code>state</code> data retrieved from the <code>readDevice()</code>
	   /// method.
	   /// </summary>
	   /// <param name="state"> byte array with device state information
	   /// </param>
	   /// <returns> temperature resolution in Celsius for this 1-wire device
	   /// </returns>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   /// <seealso cref=    #setTemperatureResolution </seealso>
	   double getTemperatureResolution(byte[] state);

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
	   /// <exception cref="OneWireException">         Device does not support temperature
	   ///                                  alarms
	   /// </exception>
	   /// <seealso cref=    #hasTemperatureAlarms </seealso>
	   /// <seealso cref=    #getTemperatureAlarm </seealso>
	   void setTemperatureAlarm(int alarmType, double alarmValue, byte[] state);

	   /// <summary>
	   /// Sets the current temperature resolution in Celsius in the provided
	   /// <code>state</code> data.   Use the method <code>writeDevice()</code>
	   /// with this data to finalize the change to the device.
	   /// </summary>
	   /// <param name="resolution"> temperature resolution in Celsius </param>
	   /// <param name="state">      byte array with device state information
	   /// </param>
	   /// <exception cref="OneWireException">         Device does not support selectable
	   ///                                  temperature resolution
	   /// </exception>
	   /// <seealso cref=    #hasSelectableTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolution </seealso>
	   /// <seealso cref=    #getTemperatureResolutions </seealso>
	   void setTemperatureResolution(double resolution, byte[] state);
	}

	public static class TemperatureContainer_Fields
	{
	   public const int ALARM_HIGH = 1;
	   public const int ALARM_LOW = 0;
	}

}