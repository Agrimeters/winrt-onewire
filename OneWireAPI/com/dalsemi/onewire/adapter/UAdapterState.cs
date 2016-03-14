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
	/// UAdapterState contains the communication state of the DS2480
	///  based COM port adapter.
	///  //\\//\\ This class is very preliminary and not all
	///           functionality is complete or debugged.  This
	///           class is subject to change.                  //\\//\\
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal class UAdapterState
	{

	   //--------
	   //-------- Finals
	   //--------
	   //------- DS9097U brick baud rates expressed for the DS2480 ichip  

	   /// <summary>
	   /// DS9097U brick baud rate expressed for the DS2480 ichip, 9600 baud </summary>
	   public const byte BAUD_9600 = 0x00;

	   /// <summary>
	   /// DS9097U brick baud rate expressed for the DS2480 ichip, 19200 baud </summary>
	   public const byte BAUD_19200 = 0x01;

	   /// <summary>
	   /// DS9097U brick baud rate expressed for the DS2480 ichip, 57600 baud </summary>
	   public const byte BAUD_57600 = 0x02;

	   /// <summary>
	   /// DS9097U brick baud rate expressed for the DS2480 ichip, 115200 baud </summary>
	   public const byte BAUD_115200 = 0x03;

        /// <summary>
        /// DS9097U brick baud rate value mask</summary>
        public const byte BAUD_VALUE_MASK = (0x07 << 1);

        //------- DS9097U speed modes

        /// <summary>
        /// DS9097U speed mode, regular speed </summary>
        public const byte USPEED_REGULAR = 0x00;

	   /// <summary>
	   /// DS9097U speed mode, flexible speed for long lines </summary>
	   public const byte USPEED_FLEX = 0x04;

	   /// <summary>
	   /// DS9097U speed mode, overdrive speed </summary>
	   public const byte USPEED_OVERDRIVE = 0x08;

	   /// <summary>
	   /// DS9097U speed mode, pulse, for program and power delivery </summary>
	   public const byte USPEED_PULSE = 0x0C;

	   //------- DS9097U modes

	   /// <summary>
	   /// DS9097U data mode </summary>
	   public const byte MODE_DATA = 0xE1;

	   /// <summary>
	   /// DS9097U command mode </summary>
	   public const byte MODE_COMMAND = 0xE3;

	   /// <summary>
	   /// DS9097U pulse mode </summary>
	   public const byte MODE_STOP_PULSE = 0xF1;

	   /// <summary>
	   /// DS9097U special mode (in revision 1 silicon only) </summary>
	   public const byte MODE_SPECIAL = 0xF3;

	   //------- DS9097U chip revisions and state

	   /// <summary>
	   /// DS9097U chip revision 1 </summary>
	   public const byte CHIP_VERSION1 = 0x01;

	   /// <summary>
	   /// DS9097U chip revision mask </summary>
	   public const byte CHIP_VERSION_MASK = 0x1C;

	   /// <summary>
	   /// DS9097U program voltage available mask </summary>
	   public const byte PROGRAM_VOLTAGE_MASK = 0x20;

	   /// <summary>
	   /// DS9097U program voltage available mask </summary>
	   public const int MAX_ALARM_COUNT = 3000;

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Parameter settings for the three logical modes
	   /// </summary>
	   public UParameterSettings[] uParameters;

	   /// <summary>
	   /// The OneWire State object reference
	   /// </summary>
	   public OneWireState oneWireState;

	   /// <summary>
	   /// Flag true if can stream bits
	   /// </summary>
	   public bool streamBits;

	   /// <summary>
	   /// Flag true if can stream bytes
	   /// </summary>
	   public bool streamBytes;

	   /// <summary>
	   /// Flag true if can stream search
	   /// </summary>
	   public bool streamSearches;

	   /// <summary>
	   /// Flag true if can stream resets
	   /// </summary>
	   public bool streamResets;

	   /// <summary>
	   /// Current baud rate that we are communicating with the DS9097U
	   /// expressed for the DS2480 ichip. <para>
	   /// Valid values can be:
	   ///  <ul>
	   ///  <li> BAUD_9600
	   ///  <li> BAUD_19200
	   ///  <li> BAUD_57600
	   ///  <li> BAUD_115200
	   ///  </ul>
	   /// </para>
	   /// </summary>
	   public byte ubaud;

	   /// <summary>
	   /// This is the current 'real' speed that the OneWire is operating at.
	   /// This is used to represent the actual mode that the DS2480 is operting
	   /// in.  For example the logical speed might be USPEED_REGULAR but for
	   /// RF emission reasons we may put the actual DS2480 in SPEED_FLEX. <para>
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> USPEED_REGULAR
	   ///  <li> USPEED_FLEX
	   ///  <li> USPEED_OVERDRIVE
	   ///  <li> USPEED_PULSE
	   ///  </ul>
	   /// </para>
	   /// </summary>
	   public byte uSpeedMode;

	   /// <summary>
	   /// This is the current state of the DS2480 adapter on program
	   /// voltage availablity.  'true' if available.
	   /// </summary>
	   public bool programVoltageAvailable;

	   /// <summary>
	   /// True when DS2480 is currently in command mode.  False when it is in
	   /// data mode.
	   /// </summary>
	   public bool inCommandMode;

	   /// <summary>
	   /// The DS2480 revision number.  The current value values are 1 and 2.
	   /// </summary>
	   public byte revision;

	   /// <summary>
	   /// Flag to indicate need to search for long alarm check
	   /// </summary>
	   protected internal bool longAlarmCheck;

	   /// <summary>
	   /// Count of how many resets have been seen without Alarms
	   /// </summary>
	   protected internal int lastAlarmCount;

	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Construct the state of the U brick with the defaults
	   /// </summary>
	   public UAdapterState(OneWireState newOneWireState)
	   {

		  // get a pointer to the OneWire state object
		  oneWireState = newOneWireState;

		  // set the defaults
		  ubaud = BAUD_9600;
		  uSpeedMode = USPEED_FLEX;
		  revision = 0;
		  inCommandMode = true;
		  streamBits = true;
		  streamBytes = true;
		  streamSearches = true;
		  streamResets = false;
		  programVoltageAvailable = false;
		  longAlarmCheck = false;
		  lastAlarmCount = 0;

		  // create the three speed logical parameter settings
		  uParameters = new UParameterSettings [4];
		  uParameters [0] = new UParameterSettings();
		  uParameters [1] = new UParameterSettings();
		  uParameters [2] = new UParameterSettings();
		  uParameters [3] = new UParameterSettings();

		  // adjust flex time 
		  uParameters [DSPortAdapter.SPEED_FLEX].pullDownSlewRate = UParameterSettings.SLEWRATE_0p83Vus;
		  uParameters [DSPortAdapter.SPEED_FLEX].write1LowTime = UParameterSettings.WRITE1TIME_12us;
		  uParameters [DSPortAdapter.SPEED_FLEX].sampleOffsetTime = UParameterSettings.SAMPLEOFFSET_TIME_10us;
	   }
	}

}