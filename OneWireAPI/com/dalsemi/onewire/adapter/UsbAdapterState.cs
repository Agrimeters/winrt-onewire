/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
 * Copyright (C) 2016 Joel Winarske, All Rights Reserved.
 *---------------------------------------------------------------------------
 */

using System;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace com.dalsemi.onewire.adapter
{
	/// <summary>
	/// UsbAdapterState contains the communication state of the DS2490
	///  based USB adapter.
    ///	
	///  @version    0.00, 1 Mar 2016
	///  @author     JW
	/// </summary>
	internal class UsbAdapterState
	{
        private Object syncObject;

        //------- DS2490 speed modes

        /// <summary>
        /// 65us time slot (15.4kbps) </summary>
        public const byte BUSCOMSPEED_REGULAR = 0x00;
        /// <summary>
        /// 65us to 72us time slot (13.9kbps to 15.4kbps) </summary>
        public const byte BUSCOMSPEED_FLEX = 0x01;
        /// <summary>
        /// 10us time slot (100kbps) </summary>
	    public const byte BUSCOMSPEED_OVERDRIVE = 0x02;

        //------- DS2490 Slew Rate Values

        /// <summary>
        /// Pull down slew rate, 15V/us </summary>
        public const byte SLEWRATE_15Vus = 0x00;
        /// <summary>
        /// Pull down slew rate, 2.2V/us </summary>
        public const byte SLEWRATE_2p2Vus = 0x01;
        /// <summary>
        /// Pull down slew rate, 1.65V/us </summary>
        public const byte SLEWRATE_1p65Vus = 0x02;
        /// <summary>
        /// Pull down slew rate, 1.37V/us </summary>
        public const byte SLEWRATE_1p37Vus = 0x03;
        /// <summary>
        /// Pull down slew rate, 1.1V/us </summary>
        public const byte SLEWRATE_1p1Vus = 0x04;
        /// <summary>
        /// Pull down slew rate, 0.83V/us </summary>
        public const byte SLEWRATE_0p83Vus = 0x05;
        /// <summary>
        /// Pull down slew rate, 0.7V/us </summary>
        public const byte SLEWRATE_0p7Vus = 0x06;
        /// <summary>
        /// Pull down slew rate, 0.55V/us </summary>
        public const byte SLEWRATE_0p55Vus = 0x07;

        ////------- DS2490 Flexible Speed Write-1 Low Time Values

        /// <summary>
        /// Flexible Speed Write-1 Low Time, 4us </summary>
        public const byte FLEXWRITE1LOWTIME_4us = 0;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 5us </summary>
        public const byte FLEXWRITE1LOWTIME_5us = 1;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 6us </summary>
        public const byte FLEXWRITE1LOWTIME_6us = 2;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 7us </summary>
        public const byte FLEXWRITE1LOWTIME_7us = 3;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 8us </summary>
        public const byte FLEXWRITE1LOWTIME_8us = 4;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 9us </summary>
        public const byte FLEXWRITE1LOWTIME_9us = 5;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 10us </summary>
        public const byte FLEXWRITE1LOWTIME_10us = 6;
        /// <summary>
        /// Flexible Speed Write-1 Low Time, 11us </summary>
        public const byte FLEXWRITE1LOWTIME_11us = 7;

        ////------- DS2490 Data Sample Offset/Write-0 Recovery Time Values

        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 10us </summary>
        public const byte DSOW0RECOVERYTIME_10us = 0;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 12us </summary>
        public const byte DSOW0RECOVERYTIME_12us = 1;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 14us </summary>
        public const byte DSOW0RECOVERYTIME_14us = 2;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 16us </summary>
        public const byte DSOW0RECOVERYTIME_16us = 3;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 18us </summary>
        public const byte DSOW0RECOVERYTIME_18us = 4;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 20us </summary>
        public const byte DSOW0RECOVERYTIME_20us = 5;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 22us </summary>
        public const byte DSOW0RECOVERYTIME_22us = 6;
        /// <summary>
        /// Data Sample Offset/Write-0 Recovery Time, 24us </summary>
        public const byte DSOW0RECOVERYTIME_24us = 7;

        ////------- DS2490 modes


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
        /// Strong Pullup to 5V
        /// </summary>
        public bool StrongPullup;

        /// <summary>
        /// Dynamic Speed Change
        /// </summary>
        public bool DynamicSpeedChange;

        /// <summary>
        /// This is the current 'real' speed that the OneWire is operating at.
        /// This is used to represent the actual mode that the DS2490 is operting
        /// in.  For example the logical speed might be BUSCOMMSPEED_REGULAR but for
        /// RF emission reasons we may put the actual DS2490 in BUSCOMMSPEED_FLEX. <para>
        /// The valid values for this are:
        ///  <ul>
        ///  <li> BUSCOMMSPEED_REGULAR
        ///  <li> BUSCOMMSPEED_FLEX
        ///  <li> BUSCOMMSPEED_OVERDRIVE
        ///  </ul>
        /// </para>
        /// </summary>
        public byte BusCommSpeed;

        /// <summary>
        /// 5V Strong Pullup Duration
        /// </summary>
        public byte StrongPullupDuration;

        /// <summary>
        /// Program Pulse Detection
        /// </summary>
        public byte ProgPullupDuration;

        /// <summary>
        /// Pulldown Slew Rate Control
        /// </summary>
        public byte PulldownSlewRateControl;

        /// <summary>
        /// Flexible Speed Write-1 Low Time
        /// </summary>
        public byte Write1LowTime;

        /// <summary>
        /// Flexible Speed Write-1 Low Time
        /// </summary>
        public byte DSOW0RecoveryTime;


        [Flags]
        private enum EnableFlags
        {
            /// <summary>
            /// Strong Pullup to 5V enabled
            /// </summary>
            SPUE = 0x01,
            /// <summary>
            /// Dynamic 1-Wire bus speed change through a Communication command is enabled
            /// </summary>
            SPCE = 0x04,
        }

        [Flags]
        private enum StatusFlags
        {
            /// <summary>
            /// Strong Pullup Active
            /// if set to 1, the strong pullup to 5V is currently active, 
            /// if set to 0, it is inactive
            /// </summary>
            SPUA = 0x01,
            /// <summary>
            /// Programming Voltage (12V) Present
            /// if set to 1, the 12V programming rail is active, 
            /// if set to 0, it is inactive
            /// </summary>
            PVP = 0x04,
            /// <summary>
            /// Power Mode
            /// if set to 1, the DS2490 is powered from USB and external sources, 
            /// if set to 0, all DS2490 power is provided from USB. 
            /// </summary>
            PMOD = 0x08,
            /// <summary>
            /// Halt
            /// if set to 1, the DS2490 is currently halted, if set to 0, the device is not halted.
            /// </summary>
            HALT = 0x10,
            /// <summary>
            /// Idle
            /// if set to 1, the DS2490 is currently idle, if set to 0, the device is not idle.
            /// </summary>
            IDLE = 0x20,
            /// <summary>
            /// EP0 FIFO Status
            /// If EP0F is set to 1, the Endpoint 0 FIFO was full when a new control transfer 
            /// setup packet was received. This is an error condition in that the setup packet 
            /// received is discarded due to the full condition. To recover from this state the 
            /// USB host must send a CTL_RESET_DEVICE command; the device will also recover with 
            /// a power on reset cycle. Note that the DS2490 will accept and process a 
            /// CTL_RESET_DEVICE command if the EP0F = 1 state occurs. If EP0F = 0, no FIFO error 
            /// condition exists. 
            /// </summary>
            EP0F = 0x80
        }

        [Flags]
        public enum CommCmdErrorResult
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

        /// <summary>
        /// Strong Pullup Active
        /// if set to 1, the strong pullup to 5V is currently active, 
        /// if set to 0, it is inactive. 
        /// </summary>
        public bool StrongPullup1;

        /// <summary>
        /// Programming Voltage Present
        /// if set to 1, the 12V programming rail is active, 
        /// if set to 0, it is inactive. 
        /// </summary>
        public bool ProgrammingVoltagePresent;

        /// <summary>
        /// Power Mode
        /// if set to 1, the DS2490 is powered from USB and external sources, 
        /// if set to 0, all DS2490 power is provided from USB. 
        /// </summary>
        public bool PowerMode;

        /// <summary>
        /// Halt
        /// if set to 1, the DS2490 is currently halted, if set to 0, the device is not halted.
        /// </summary>
        public bool Halt;

        /// <summary>
        /// Idle
        /// if set to 1, the DS2490 is currently idle, if set to 0, the device is not idle.
        /// </summary>
        public bool Idle;

        /// <summary>
        /// EP0 FIFO Status
        /// If EP0F is set to 1, the Endpoint 0 FIFO was full when a new control transfer 
        /// setup packet was received. This is an error condition in that the setup packet 
        /// received is discarded due to the full condition. To recover from this state the 
        /// USB host must send a CTL_RESET_DEVICE command; the device will also recover with 
        /// a power on reset cycle. Note that the DS2490 will accept and process a 
        /// CTL_RESET_DEVICE command if the EP0F = 1 state occurs. If EP0F = 0, no FIFO error 
        /// condition exists. 
        /// </summary>
        public bool Ep0Fifo;

        /// <summary>
        /// Communication command currently being processed.
        /// If the device is idle, a register value of 0x00 is sent.
        /// </summary>
        public ushort Cmd;

        /// <summary>
        /// Number of data bytes currently contained in the 16-byte FIFO
        /// used to hold communication commands.
        /// </summary>
        public byte CmdBufferStatus;

        /// <summary>
        /// Number of data bytes currently contained in the 128-byte FIFO
        /// used to write data to the 1-Wire bus.
        /// </summary>
        public byte OneWireWriteBufferStatus;

        /// <summary>
        /// Number of data bytes currently contained in the 128-byte 
        /// command FIFO used to read data from the 1-Wire bus
        /// </summary>
        public byte OneWireReadBufferStatus;

        /// <summary>
        /// Communication Result Codes
        /// </summary>
        public byte[] CommResultCodes { get; set; }

        /// <summary>
        /// Flag used to indicate Interupt has been handled
        /// </summary>
        public bool Updated { get; set; }

        /// <summary>
        /// Convert a number to range usable for the 5V Strong Pullup duration
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static byte GetPullUpDurationByte(double duration)
        {
            var val = duration / .016;
            byte result = Convert.ToByte(val);
            if (result > 0xFE)
                result = 0xFE;
            return result;
        }

        /// <summary>
        /// Get a floating point value from byte returned by interrupt
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double GetPullUpDurationValue(byte value)
        {
            return value * .016;
        }

        //--------
        //-------- Constructors
        //--------

        /// <summary>
        /// Construct the state of the USB interface with the defaults
        /// </summary>
        public UsbAdapterState(OneWireState newOneWireState)
	    {
            syncObject = new object();

            // get a pointer to the OneWire state object
            oneWireState = newOneWireState;

		    // Power-On defaults
            StrongPullup = false;
            DynamicSpeedChange = false;
		    BusCommSpeed = BUSCOMSPEED_REGULAR;
            StrongPullupDuration = GetPullUpDurationByte(.512);
            PulldownSlewRateControl = SLEWRATE_0p83Vus;
            Write1LowTime = FLEXWRITE1LOWTIME_8us;
            DSOW0RecoveryTime = DSOW0RECOVERYTIME_18us;
          

		    //// create the three speed logical parameter settings
		    //uParameters = new UParameterSettings [3];
		    //uParameters [0] = new UParameterSettings();
		    //uParameters [1] = new UParameterSettings();
		    //uParameters [2] = new UParameterSettings();

		    //// adjust flex time 
		    //uParameters [DSPortAdapter.SPEED_FLEX].pullDownSlewRate = UParameterSettings.SLEWRATE_0p83Vus;
		    //uParameters [DSPortAdapter.SPEED_FLEX].write1LowTime = UParameterSettings.WRITE1TIME_12us;
		    //uParameters [DSPortAdapter.SPEED_FLEX].sampleOffsetTime = UParameterSettings.SAMPLEOFFSET_TIME_10us;
	    }

        /// <summary>
        /// Interrupt Handler
        /// </summary>
        /// <param name="m"></param>
        /// <param name="length"></param>
        public void OnStateUpdate(DataReader m, uint length)
        {
            lock (syncObject)
            {
                EnableFlags enableflags = (EnableFlags)m.ReadByte();

                StrongPullup = ((enableflags & EnableFlags.SPUE) == EnableFlags.SPUE) ? true : false;
                oneWireState.oneWireLevel = (byte)((StrongPullup == true) ? 1 : 0);
                DynamicSpeedChange = ((enableflags & EnableFlags.SPCE) == EnableFlags.SPCE) ? true : false;
                BusCommSpeed = m.ReadByte();
                oneWireState.oneWireSpeed = BusCommSpeed;
                StrongPullupDuration = m.ReadByte();
                ProgPullupDuration = m.ReadByte();
                PulldownSlewRateControl = m.ReadByte();
                Write1LowTime = m.ReadByte();
                DSOW0RecoveryTime = m.ReadByte();
                var rsvd1 = m.ReadByte();

                StatusFlags statusflags = (StatusFlags)m.ReadByte();

                StrongPullup1 = ((statusflags & StatusFlags.SPUA) == StatusFlags.SPUA) ? true : false;
                ProgrammingVoltagePresent = ((statusflags & StatusFlags.PVP) == StatusFlags.PVP) ? true : false;
                oneWireState.canProgram = ProgrammingVoltagePresent;
                PowerMode = ((statusflags & StatusFlags.PMOD) == StatusFlags.PMOD) ? true : false;
                Halt = ((statusflags & StatusFlags.HALT) == StatusFlags.HALT) ? true : false;
                Idle = ((statusflags & StatusFlags.IDLE) == StatusFlags.IDLE) ? true : false;
                Ep0Fifo = ((statusflags & StatusFlags.EP0F) == StatusFlags.EP0F) ? true : false;

                Cmd = m.ReadUInt16();
                CmdBufferStatus = m.ReadByte();

                OneWireWriteBufferStatus = m.ReadByte();
                OneWireReadBufferStatus = m.ReadByte();

                var rsvd2 = m.ReadByte();
                var rsvd3 = m.ReadByte();

                var len = length - 16;
                if (len > 0)
                {
                    byte[] buff = new byte[len];
                    for (int i = 0; i < len; i++)
                        buff[i] = m.ReadByte();
                    CommResultCodes = buff;
                }
                else
                {
                    CommResultCodes = null;
                }

                Updated = true;
            }
        }

        /// <summary>
        /// Decode the Error Result
        /// </summary>
        /// <param name="value"></param>
        public void PrintErrorResult(byte value)
        {
            if (value == Ds2490.ONEWIREDEVICEDETECT)
            {
                Debug.WriteLine("1-Wire Device Detect Byte");
                return;
            }

            CommCmdErrorResult result = (CommCmdErrorResult)value;

            if ((result & CommCmdErrorResult.NRS) == CommCmdErrorResult.NRS)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET did not reveal a Presence Pulse. SET PATH command did not get a Presence Pulse from the branch that was to be connected. No response from one or more ROM ID bits during a SEARCH ACCESS command.");
            }
            if ((result & CommCmdErrorResult.SH) == CommCmdErrorResult.SH)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET revealed a short to the 1-Wire bus or the SET PATH command could not successfully connect a branch due to a short.");
            }
            if ((result & CommCmdErrorResult.APP) == CommCmdErrorResult.APP)
            {
                Debug.WriteLine("Error Result: A 1-WIRE RESET revealed an Alarming Presence Pulse.");
            }
            if ((result & CommCmdErrorResult.VPP) == CommCmdErrorResult.VPP)
            {
                Debug.WriteLine("Error Result: During a PULSE with TYPE=1 or WRITE EPROM command the 12V programming pulse not seen on 1-Wire bus.");
            }
            if ((result & CommCmdErrorResult.CMP) == CommCmdErrorResult.CMP)
            {
                Debug.WriteLine("Error Result: Error when reading the confirmation byte with a SET PATH command. There was a difference between the byte written and then read back with a BYTE I/O command,");
            }
            if ((result & CommCmdErrorResult.CRC) == CommCmdErrorResult.CRC)
            {
                Debug.WriteLine("Error Result: A CRC error occurred when executing one of the following commands: WRITE SRAM PAGE, READ CRC PROT PAGE, or READ REDIRECT PAGE W/CRC.");
            }
            if ((result & CommCmdErrorResult.RDP) == CommCmdErrorResult.RDP)
            {
                Debug.WriteLine("Error Result: A READ REDIRECT PAGE WITH/CRC encountered a page that is redirected.");
            }
            if ((result & CommCmdErrorResult.EOS) == CommCmdErrorResult.EOS)
            {
                Debug.WriteLine("Error Result: A SEARCH ACCESS with SM = 1 ended sooner than expected reporting less ROM ID’s than specified in the “number of devices” parameter.");
            }
        }

        /// <summary>
        /// Prints the state of the device
        /// </summary>
        public void PrintState()
        {
            lock (syncObject)
            {
                Debug.WriteLine("5V Strong Pullup: " + StrongPullup);
                Debug.WriteLine("Dynamic Speed Change: " + DynamicSpeedChange);
                switch (BusCommSpeed)
                {
                    case BUSCOMSPEED_REGULAR:
                        Debug.WriteLine("1-Wire Speed: REGULAR");
                        break;
                    case BUSCOMSPEED_OVERDRIVE:
                        Debug.WriteLine("1-Wire Speed: OVERDRIVE");
                        break;
                    case BUSCOMSPEED_FLEX:
                        Debug.WriteLine("1-Wire Speed: FLEXIBLE");
                        break;
                }
                var duration = GetPullUpDurationValue(StrongPullupDuration);
                Debug.WriteLine("1-Wire Strong Pullup Duration: " + ((duration == 0) ? "INFINITE" : (duration + "ms")));
                Debug.WriteLine("12V Pullup Duration: {0:X02}h", ProgPullupDuration);
                Debug.Write("PulldownSlewRateControl: ");
                switch (PulldownSlewRateControl)
                {
                    case 0x00:
                        Debug.WriteLine("15V/us");
                        break;
                    case 0x01:
                        Debug.WriteLine("2.20V/us");
                        break;
                    case 0x02:
                        Debug.WriteLine("1.65V/us");
                        break;
                    case 0x03:
                        Debug.WriteLine("1.37V/us");
                        break;
                    case 0x04:
                        Debug.WriteLine("1.10V/us");
                        break;
                    case 0x05:
                        Debug.WriteLine("0.83V/us");
                        break;
                    case 0x06:
                        Debug.WriteLine("0.70V/us");
                        break;
                    case 0x07:
                        Debug.WriteLine("0.55V/us");
                        break;
                }
                Debug.Write("Write1LowTime: ");
                switch (Write1LowTime)
                {
                    case 0x00:
                        Debug.WriteLine("4us");
                        break;
                    case 0x01:
                        Debug.WriteLine("5us");
                        break;
                    case 0x02:
                        Debug.WriteLine("6us");
                        break;
                    case 0x03:
                        Debug.WriteLine("7us");
                        break;
                    case 0x04:
                        Debug.WriteLine("8us");
                        break;
                    case 0x05:
                        Debug.WriteLine("9us");
                        break;
                    case 0x06:
                        Debug.WriteLine("10us");
                        break;
                    case 0x07:
                        Debug.WriteLine("11us");
                        break;
                }
                Debug.Write("DSOW0RecoveryTime: ");
                switch (DSOW0RecoveryTime)
                {
                    case 0x00:
                        Debug.WriteLine("10us");
                        break;
                    case 0x01:
                        Debug.WriteLine("12us");
                        break;
                    case 0x02:
                        Debug.WriteLine("14us");
                        break;
                    case 0x03:
                        Debug.WriteLine("16us");
                        break;
                    case 0x04:
                        Debug.WriteLine("18us");
                        break;
                    case 0x05:
                        Debug.WriteLine("20us");
                        break;
                    case 0x06:
                        Debug.WriteLine("22us");
                        break;
                    case 0x07:
                        Debug.WriteLine("24us");
                        break;
                }

                Debug.WriteLine("StrongPullup1: " + StrongPullup1);
                Debug.WriteLine("ProgrammingVoltagePresent: " + ProgrammingVoltagePresent);
                Debug.WriteLine("PowerMode: " + PowerMode);
                Debug.WriteLine("Halt: " + Halt);
                Debug.WriteLine("Idle: " + Idle);
                Debug.WriteLine("Ep0Fifo: " + Ep0Fifo);

                Debug.WriteLine("Cmd: {0:X04}h", Cmd);
                Debug.WriteLine("CmdBufferStatus: {0:X02}h", CmdBufferStatus);
                Debug.WriteLine("1-Wire Write Buffer Status: " + OneWireWriteBufferStatus);
                Debug.WriteLine("1-Wire Read Buffer Status: " + OneWireReadBufferStatus);
            }
        }

    }
}