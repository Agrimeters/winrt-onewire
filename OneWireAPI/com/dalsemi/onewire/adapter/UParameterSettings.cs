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

	/// <summary>
	/// UParameterSettings contains the parameter settings state for one
	///  speed on the DS2480 based iButton COM port adapter.
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal class UParameterSettings
	{

	   //--------
	   //-------- Finals
	   //--------
	   // Parameter selection

	   /// <summary>
	   /// Parameter selection, pull-down slew rate </summary>
	   public const byte PARAMETER_SLEW = 0x01;

	   /// <summary>
	   /// Parameter selection, 12 volt pulse time </summary>
	   public const byte PARAMETER_12VPULSE = 0x02;

	   /// <summary>
	   /// Parameter selection, 5 volt pulse time </summary>
	   public const byte PARAMETER_5VPULSE = 0x03;

	   /// <summary>
	   /// Parameter selection, write 1 low time </summary>
	   public const byte PARAMETER_WRITE1LOW = 0x04;

	   /// <summary>
	   /// Parameter selection, sample offset </summary>
	   public const byte PARAMETER_SAMPLEOFFSET = 0x05;

        /// <summary>
        /// Parameter selection, load </summary>
//TODO        public const byte PARAMETER_LOAD = 0x06;

        /// <summary>
        /// Parameter selection, baud rate </summary>
        public const byte PARAMETER_BAUDRATE = 0x07;

        /// <summary>
        /// Parameter selection, baud rate read mask
        /// </summary>
        public const byte PARAMETER_BAUDRATE_READ_MASK = 0x70;

        // Pull down slew rate times 

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

	   // 12 Volt programming pulse times 

	   /// <summary>
	   /// 12 Volt programming pulse, time 32us </summary>
	   public const byte TIME12V_32us = 0x00;

	   /// <summary>
	   /// 12 Volt programming pulse, time 64us </summary>
	   public const byte TIME12V_64us = 0x01;

	   /// <summary>
	   /// 12 Volt programming pulse, time 128us </summary>
	   public const byte TIME12V_128us = 0x02;

	   /// <summary>
	   /// 12 Volt programming pulse, time 256us </summary>
	   public const byte TIME12V_256us = 0x03;

	   /// <summary>
	   /// 12 Volt programming pulse, time 512us </summary>
	   public const byte TIME12V_512us = 0x04;

	   /// <summary>
	   /// 12 Volt programming pulse, time 1024us </summary>
	   public const byte TIME12V_1024us = 0x05;

	   /// <summary>
	   /// 12 Volt programming pulse, time 2048us </summary>
	   public const byte TIME12V_2048us = 0x06;

	   /// <summary>
	   /// 12 Volt programming pulse, time (infinite) </summary>
	   public const byte TIME12V_infinite = 0x07;

	   // 5 Volt programming pulse times 

	   /// <summary>
	   /// 5 Volt programming pulse, time 16.4ms </summary>
	   public const byte TIME5V_16p4ms = 0x00;

	   /// <summary>
	   /// 5 Volt programming pulse, time 65.5ms </summary>
	   public const byte TIME5V_65p5ms = 0x01;

	   /// <summary>
	   /// 5 Volt programming pulse, time 131ms </summary>
	   public const byte TIME5V_131ms = 0x02;

	   /// <summary>
	   /// 5 Volt programming pulse, time 262ms </summary>
	   public const byte TIME5V_262ms = 0x03;

	   /// <summary>
	   /// 5 Volt programming pulse, time 524ms </summary>
	   public const byte TIME5V_524ms = 0x04;

	   /// <summary>
	   /// 5 Volt programming pulse, time 1.05s </summary>
	   public const byte TIME5V_1p05s = 0x05;

	   /// <summary>
	   /// 5 Volt programming pulse, time 2.10sms </summary>
//TODO	   public const byte TIME5V_2p10s = 0x06;

	   /// <summary>
	   /// 5 Volt programming pulse, dynamic current detect </summary>
//TODO	   public const byte TIME5V_dynamic = 0x06;

	   /// <summary>
	   /// 5 Volt programming pulse, time (infinite) </summary>
	   public const byte TIME5V_infinite = 0x07;

	   // Write 1 low time 

	   /// <summary>
	   /// Write 1 low time, 8us </summary>
	   public const byte WRITE1TIME_8us = 0x00;

	   /// <summary>
	   /// Write 1 low time, 9us </summary>
	   public const byte WRITE1TIME_9us = 0x01;

	   /// <summary>
	   /// Write 1 low time, 10us </summary>
	   public const byte WRITE1TIME_10us = 0x02;

	   /// <summary>
	   /// Write 1 low time, 11us </summary>
	   public const byte WRITE1TIME_11us = 0x03;

	   /// <summary>
	   /// Write 1 low time, 12us </summary>
	   public const byte WRITE1TIME_12us = 0x04;

	   /// <summary>
	   /// Write 1 low time, 13us </summary>
	   public const byte WRITE1TIME_13us = 0x05;

	   /// <summary>
	   /// Write 1 low time, 14us </summary>
	   public const byte WRITE1TIME_14us = 0x06;

	   /// <summary>
	   /// Write 1 low time, 15us </summary>
	   public const byte WRITE1TIME_15us = 0x07;

	   // Data sample offset and write 0 recovery times 

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 4us </summary>
	   public const byte SAMPLEOFFSET_TIME_3us = 0x00;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 4us </summary>
	   public const byte SAMPLEOFFSET_TIME_4us = 0x01;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 5us </summary>
	   public const byte SAMPLEOFFSET_TIME_5us = 0x02;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 6us </summary>
	   public const byte SAMPLEOFFSET_TIME_6us = 0x03;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 7us </summary>
	   public const byte SAMPLEOFFSET_TIME_7us = 0x04;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 8us </summary>
	   public const byte SAMPLEOFFSET_TIME_8us = 0x05;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 9us </summary>
	   public const byte SAMPLEOFFSET_TIME_9us = 0x06;

	   /// <summary>
	   /// Data sample offset and Write 0 recovery time, 10us </summary>
	   public const byte SAMPLEOFFSET_TIME_10us = 0x07;

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// The pull down slew rate for this mode. <para>
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> SLEWRATE_15Vus
	   ///  <li> SLEWRATE_2p2Vus
	   ///  <li> SLEWRATE_1p65Vus
	   ///  <li> SLEWRATE_1p37Vus
	   ///  <li> SLEWRATE_1p1Vus
	   ///  <li> SLEWRATE_0p83Vus
	   ///  <li> SLEWRATE_0p7Vus
	   ///  <li> SLEWRATE_0p55Vus
	   ///  </ul>
	   /// </para>
	   /// </summary>
	   public byte pullDownSlewRate;

	   /// <summary>
	   /// 12 Volt programming pulse time expressed in micro-seconds.
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> TIME12V_32us
	   ///  <li> TIME12V_64us
	   ///  <li> TIME12V_128us
	   ///  <li> TIME12V_512us
	   ///  <li> TIME12V_1024us
	   ///  <li> TIME12V_2048us
	   ///  <li> TIME12V_infinite
	   ///  </ul>
	   /// </summary>
	   public byte pulse12VoltTime;

	   /// <summary>
	   /// 5 Volt programming pulse time expressed in milli-seconds.
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> TIME5V_16p4ms
	   ///  <li> TIME5V_65p5ms
	   ///  <li> TIME5V_131ms
	   ///  <li> TIME5V_262ms
	   ///  <li> TIME5V_524ms
	   ///  <li> TIME5V_1p05s
	   ///  <li> TIME5V_2p10s
	   ///  <li> TIME5V_infinite
	   ///  </ul>
	   /// </summary>
	   public byte pulse5VoltTime;

	   /// <summary>
	   /// Write 1 low time expressed in micro-seconds.
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> WRITE1TIME_8us
	   ///  <li> WRITE1TIME_9us
	   ///  <li> WRITE1TIME_10us
	   ///  <li> WRITE1TIME_11us
	   ///  <li> WRITE1TIME_12us
	   ///  <li> WRITE1TIME_13us
	   ///  <li> WRITE1TIME_14us
	   ///  <li> WRITE1TIME_15us
	   ///  </ul>
	   /// </summary>
	   public byte write1LowTime;

	   /// <summary>
	   /// Data sample offset and write 0 recovery time expressed in micro-seconds.
	   /// The valid values for this are:
	   ///  <ul>
	   ///  <li> SAMPLEOFFSET_TIME_4us
	   ///  <li> SAMPLEOFFSET_TIME_5us
	   ///  <li> SAMPLEOFFSET_TIME_6us
	   ///  <li> SAMPLEOFFSET_TIME_7us
	   ///  <li> SAMPLEOFFSET_TIME_8us
	   ///  <li> SAMPLEOFFSET_TIME_9us
	   ///  <li> SAMPLEOFFSET_TIME_10us
	   ///  <li> SAMPLEOFFSET_TIME_11us
	   ///  </ul>
	   /// </summary>
	   public byte sampleOffsetTime;

	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Parameter Settings constructor.  The default values are:
	   ///  <para>
	   /// </para>
	   ///  pullDownSlewRate = SLEWRATE_1p37Vus; <para>
	   /// </para>
	   ///  pulse12VoltTime = TIME12V_infinite; <para>
	   /// </para>
	   ///  pulse5VoltTime = TIME5V_infinite; <para>
	   /// </para>
	   ///  write1LowTime = WRITE1TIME_8us; <para>
	   /// </para>
	   ///  sampleOffsetTime = SAMPLEOFFSET_TIME_6us; <para>
	   /// </para>
	   /// </summary>
	   public UParameterSettings()
	   {
		  pullDownSlewRate = SLEWRATE_1p37Vus;
		  pulse12VoltTime = TIME12V_infinite;
		  pulse5VoltTime = TIME5V_infinite;
		  write1LowTime = WRITE1TIME_10us;
		  sampleOffsetTime = SAMPLEOFFSET_TIME_8us;
	   }
	}

}