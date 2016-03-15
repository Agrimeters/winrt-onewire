using System;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.container;
using System.IO;
using System.Reflection;

public class initMission
{
   internal static int parseInt(System.IO.StreamReader din, int def)
   {
	  try
	  {
		 return int.Parse(din.ReadLine());
	  }
	  catch (Exception)
	  {
		 return def;
	  }
   }

   public static Stream loadResourceFile(string file)
   {
       try
       {
           Assembly asm = typeof(Thermochron.MainPage).GetTypeInfo().Assembly;
           return asm.GetManifestResourceStream(file);
       }
       catch (Exception)
       {
           Debug.WriteLine("Can't find resource: " + file);
       }
       return null;
   }

   /// <summary>
   /// Method printUsageString
   /// 
   /// 
   /// </summary>
   public static void printUsageString()
   {
	  Debug.WriteLine("DS1921 Thermochron Mission Initialization Program.\r\n");
	  Debug.WriteLine("Usage: ");
	  Debug.WriteLine("   java initcopr ADAPTER_PORT\r\n");
	  Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
	  Debug.WriteLine("adapter you would like to use and the port you would like");
	  Debug.WriteLine("to use, for example: ");
	  Debug.WriteLine("   java initcopr {DS1410E}_LPT1");
	  Debug.WriteLine("You can leave ADAPTER_PORT blank to use the default one-wire adapter and port.");
   }

   /// <summary>
   /// Method main
   /// 
   /// </summary>
   /// <param name="args">
   /// </param>
   /// <exception cref="IOException"> </exception>
   /// <exception cref="OneWireException"> </exception>
   /// <exception cref="OneWireIOException">
   ///  </exception>
   public static void Main1(string[] args)
   {
	  bool usedefault = false;
	  DSPortAdapter access = null;
	  string adapter_name = null;
	  string port_name = null;

      if ((args == null) || (args.Length < 1))
	  {
		 try
		 {
			access = OneWireAccessProvider.DefaultAdapter;

			if (access == null)
			{
			   throw new Exception();
			}
		 }
		 catch (Exception)
		 {
			Debug.WriteLine("Couldn't get default adapter!");
			printUsageString();

			return;
		 }

		 usedefault = true;
	  }

	  if (!usedefault)
	  {
         string[] st = args[0].Split(new char[] { '_' });

		 if (st.Length != 2)
		 {
			printUsageString();

			return;
		 }

		 adapter_name = st[0];
		 port_name = st[1];

		 Debug.WriteLine("Adapter Name: " + adapter_name);
		 Debug.WriteLine("Port Name: " + port_name);
	  }

	  if (access == null)
	  {
		 try
		 {
			access = OneWireAccessProvider.getAdapter(adapter_name, port_name);
		 }
		 catch (Exception)
		 {
			Debug.WriteLine("That is not a valid adapter/port combination.");

			System.Collections.IEnumerator en = OneWireAccessProvider.enumerateAllAdapters();

			while (en.MoveNext())
			{
			   DSPortAdapter temp = (DSPortAdapter) en.Current;

			   Debug.WriteLine("Adapter: " + temp.AdapterName);

			   System.Collections.IEnumerator f = temp.PortNames;

			   while (f.MoveNext())
			   {
				  Debug.WriteLine("   Port name : " + ((string) f.Current));
			   }
			}

			return;
		 }
	  }

	  access.adapterDetected();
	  access.targetFamily(0x21);
	  access.beginExclusive(true);
	  access.reset();
	  access.setSearchAllDevices();

	  bool next = access.findFirstDevice();

	  if (!next)
	  {
		 Debug.WriteLine("Could not find any DS1921 Thermochrons!");

		 return;
	  }

	  OneWireContainer21 owc = new OneWireContainer21();

	  owc.setupContainer(access, access.AddressAsLong);

      Stream stream = loadResourceFile("Thermochron.input.txt");
      System.IO.StreamReader din = new StreamReader(stream);

	  //the following section of code jus gets all these options from the command line
	  //to see how to actually talk to the iButton, scroll down until you find
	  //the code "disableMission()
	  Debug.WriteLine("Dallas Semiconductor DS1921 Thermochron Demo");
	  Debug.WriteLine("--------------------------------------------");
	  Debug.WriteLine("Initializing mission on iButton " + owc.AddressAsString + "\r\n");

	  string tstr;
	  bool rollover = false;

	  Debug.Write("Enable rollover (y or n)? ");

	  tstr = din.ReadLine();

	  if (tstr.Equals("y", StringComparison.CurrentCultureIgnoreCase) || tstr.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
	  {
		 rollover = true;
	  }

	  Debug.Write("Enter low temperature alarm in celsius (23) : ");

	  int low = parseInt(din, 23);

	  Debug.Write("Enter high temperature alarm in celsius (28) : ");

	  int high = parseInt(din, 28);
	  bool clockalarm = false;

	  Debug.Write("Enable clock alarm (y or n)? ");

	  tstr = din.ReadLine();

	  if (tstr.Equals("y", StringComparison.CurrentCultureIgnoreCase) || tstr.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
	  {
		 clockalarm = true;
	  }

	  int second = 0;
	  int minute = 0;
	  int hour = 0;
	  int day = 0;
	  int frequency = -1;

	  if (clockalarm)
	  {
		 Debug.WriteLine("Clock alarm enabled.  Enter alarm frequency: ");
		 Debug.WriteLine("   0  Once per second");
		 Debug.WriteLine("   1  Once per minute"); //need second
		 Debug.WriteLine("   2  Once per hour"); //need second, minute
		 Debug.WriteLine("   3  Once per day"); //need hour, minute, second
		 Debug.WriteLine("   4  Once per week"); //need hour, minute, second, day
		 Debug.Write("   ? ");

		 int x = parseInt(din, -1);

		 if ((x < 0) || (x > 4))
		 {
			Debug.WriteLine("That is not a valid clock alarm frequency.");

			return;
		 }

		 switch (x) //noteice no breaks!
		 {

			case 4 :
			   if (frequency == -1)
			   {
				  frequency = OneWireContainer21.ONCE_PER_WEEK;
			   }

			   Debug.Write("Day of week to alarm (1==Sunday) : ");

			   day = parseInt(din, 1);
				goto case 3;
			case 3 :
			   if (frequency == -1)
			   {
				  frequency = OneWireContainer21.ONCE_PER_DAY;
			   }

			   Debug.Write("Hour of day to alarm (0 - 23) : ");

			   hour = parseInt(din, 0);
				goto case 2;
			case 2 :
			   if (frequency == -1)
			   {
				  frequency = OneWireContainer21.ONCE_PER_HOUR;
			   }

			   Debug.Write("Minute of hour to alarm (0 - 59) : ");

			   minute = parseInt(din, 0);
				goto case 1;
			case 1 :
			   if (frequency == -1)
			   {
				  frequency = OneWireContainer21.ONCE_PER_MINUTE;
			   }

			   Debug.Write("Second of minute to alarm (0 - 59) : ");

			   second = parseInt(din, 0);
				goto case 0;
			case 0 :
			   if (frequency == -1)
			   {
				  frequency = OneWireContainer21.ONCE_PER_SECOND;
			   }
				  break;
		 }
	  }

	  bool synchronize = false;

	  Debug.Write("Set thermochron clock to system clock (y or n)? ");

	  tstr = din.ReadLine();

	  if (tstr.Equals("y", StringComparison.CurrentCultureIgnoreCase) || tstr.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
	  {
		 synchronize = true;
	  }

	  Debug.Write("Start the mission in how many minutes? ");

	  int delay = 0;

	  delay = parseInt(din, 0);

	  Debug.Write("Sampling Interval in minutes (1 to 255)? ");

	  int rate = 1;

	  rate = parseInt(din, 1);

	  //now do some bounds checking
	  if (rate < 1)
	  {
		 rate = 1;
	  }

	  if (rate > 255)
	  {
		 rate = 255;
	  }

	  delay = delay & 0x0ffff;

	  int physicalLow = (int) owc.PhysicalRangeLowTemperature;
	  int physicalHigh = (int) owc.PhysicalRangeHighTemperature;
	  if (low < physicalLow)
	  {
		 low = physicalLow;
	  }

	  if (low > physicalHigh)
	  {
		 low = physicalHigh;
	  }

	  if (high < physicalLow)
	  {
		 high = physicalLow;
	  }

	  if (high > physicalHigh)
	  {
		 high = physicalHigh;
	  }

	  if (day < 1)
	  {
		 day = 1;
	  }

	  if (day > 7)
	  {
		 day = 7;
	  }

	  second = second % 60;
	  minute = minute % 60;
	  hour = hour % 24;

	  //some regurgitation first....
	  Debug.WriteLine("\r\n\r\nSummary---------------------");
	  Debug.WriteLine("Rollover enabled              : " + rollover);
	  Debug.WriteLine("Low temperature alarm trigger : " + low);
	  Debug.WriteLine("High temperature alarm trigger: " + high);
	  Debug.WriteLine("Clock alarm enabled           : " + clockalarm);

	  if (clockalarm)
	  {
		 Debug.Write("Alarm frequency               : ");

		 switch (frequency)
		 {

			case OneWireContainer21.ONCE_PER_SECOND :
			   Debug.WriteLine("Once per second");
			   break;
			case OneWireContainer21.ONCE_PER_MINUTE :
			   Debug.WriteLine("Once per minute");
			   break;
			case OneWireContainer21.ONCE_PER_HOUR :
			   Debug.WriteLine("Once per hour");
			   break;
			case OneWireContainer21.ONCE_PER_DAY :
			   Debug.WriteLine("Once per day");
			   break;
			case OneWireContainer21.ONCE_PER_WEEK :
			   Debug.WriteLine("Once per week");
			   break;
			default :
			   Debug.WriteLine("Unknown alarm frequency!!! Bailing!!!");

			   return;
		 }

		 Debug.Write("Alarm setting                 : " + hour + ":" + minute + ":" + second + " ");

		 switch (day)
		 {

			case 1 :
			   Debug.WriteLine("Sunday");
			   break;
			case 2 :
			   Debug.WriteLine("Monday");
			   break;
			case 3 :
			   Debug.WriteLine("Tuesday");
			   break;
			case 4 :
			   Debug.WriteLine("Wednesday");
			   break;
			case 5 :
			   Debug.WriteLine("Thursday");
			   break;
			case 6 :
			   Debug.WriteLine("Friday");
			   break;
			case 7 :
			   Debug.WriteLine("Saturday");
			   break;
			default :
			   Debug.WriteLine("Unknown day of week! Bailing!");

			   return;
		 }
	  }

	  Debug.WriteLine("Synchonizing with host clock  : " + synchronize);
	  Debug.WriteLine("Mission starts in (minutes)   : " + delay);
	  Debug.WriteLine("Sampling rate (minutes)       : " + rate);
	  Debug.WriteLine("-------------------------------\r\n");

	  //now let's start talking to the thermochron
	  //first lets put it into overdrive
	  Debug.WriteLine("Putting the part into overdrive...");
	  owc.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, true);
	  Debug.WriteLine("Disabling current mission...");
	  owc.disableMission();
	  Debug.WriteLine("Clearing memory...");
	  owc.clearMemory();
	  Debug.WriteLine("Reading device state...");

	  byte[] state = owc.readDevice();

	  Debug.WriteLine("Setting rollover flag in state...");
	  owc.setFlag(OneWireContainer21.CONTROL_REGISTER, OneWireContainer21.ROLLOVER_ENABLE_FLAG, rollover, state);
	  Debug.WriteLine("Setting high alarm in state...");
	  owc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, (double) high, state);
	  Debug.WriteLine("Setting low alarm in state...");
	  owc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, (double) low, state);

	  if (clockalarm)
	  {
		 Debug.WriteLine("Setting clock alarm in state...");
		 owc.setClockAlarm(hour, minute, second, day, frequency, state);
		 Debug.WriteLine("Enabling clock alarm in state...");
		 owc.setClockAlarmEnable(true, state);
	  }

	  if (synchronize)
	  {
		 Debug.WriteLine("Synchonizing with host clock in state...");
		 owc.setClock(DateTime.Now.Ticks * TimeSpan.TicksPerMillisecond, state);
	  }

	  Debug.WriteLine("Setting mission delay in state...");
	  owc.setMissionStartDelay(delay, state);
	  Debug.WriteLine("Enabling the clock oscillator in state...");
	  owc.setClockRunEnable(true, state);
	  Debug.WriteLine("Writing state back to Thermochron...");
	  owc.writeDevice(state);
	  Debug.WriteLine("Enabling mission...");
	  owc.enableMission(rate);
	  Debug.WriteLine("Initialization Complete.");

	  //       state = owc.readDevice();
	  //       for (int i=0;i<state.length;i++)
	  //           System.out.println("State["+(i < 0x10 ? "0" : "")+Integer.toHexString(i)+"] == "+Integer.toHexString(0x0ff & state[i]));
   }
}
