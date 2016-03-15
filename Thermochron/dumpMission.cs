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


public class dumpMission
{
   /// <summary>
   /// Method printUsageString
   /// 
   /// 
   /// </summary>
   public static void printUsageString()
   {
	  Debug.WriteLine("DS1921 Thermocron Java Demo: Mission Reading Program.\r\n");
	  Debug.WriteLine("Usage: ");
	  Debug.WriteLine("   java dumpMission ADAPTER_PORT OPTIONS\r\n");
	  Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
	  Debug.WriteLine("adapter you would like to use and the port you would like");
	  Debug.WriteLine("to use, for example: ");
	  Debug.WriteLine("   java dumpMission {DS1410E}_LPT1\r\n");
	  Debug.WriteLine("OPTIONS is a String that includes zero or more of the following:");
	  Debug.WriteLine("   a  Print the alarm violation history for the mission.");
	  Debug.WriteLine("   h  Print the histogram for the mission.");
	  Debug.WriteLine("   l  Print the log of temperatures for the mission.");
	  Debug.WriteLine("   k  Kill the current mission before reading stats.");
	  Debug.WriteLine("   s  Stop the current mission after reading stats.");
	  Debug.WriteLine("Example: ");
	  Debug.WriteLine("   java dumpMission {DS1410E}_LPT1 ahl\r\n");
	  Debug.WriteLine("You can leave ADAPTER_PORT blank to use the default one-wire adapter and port.");
   }

   /// <summary>
   /// Method main
   /// 
   /// </summary>
   /// <param name="args">
   /// </param>
   /// <exception cref="OneWireException"> </exception>
   /// <exception cref="OneWireIOException">
   ///  </exception>
   public static void Main1(string[] args)
   {
	  bool show_history = false;
	  bool show_log = false;
	  bool show_histogram = false;
	  bool pre_kill = false;
	  bool post_kill = false;
	  bool usedefault = false;
	  DSPortAdapter access = null;
	  string adapter_name = null;
	  string port_name = null;
      Stopwatch stopWatch = new Stopwatch();

	  if ((args == null) || (args.Length < 1) || (args [0].IndexOf("_", StringComparison.Ordinal) == -1))
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

	  if ((args != null) && (args.Length > 0) && (usedefault))
	  {
		 string arg = args [0];

		 if (arg.IndexOf("a", StringComparison.Ordinal) != -1)
		 {
			show_history = true;
		 }

		 if (arg.IndexOf("l", StringComparison.Ordinal) != -1)
		 {
			show_log = true;
		 }

		 if (arg.IndexOf("h", StringComparison.Ordinal) != -1)
		 {
			show_histogram = true;
		 }

		 if (arg.IndexOf("k", StringComparison.Ordinal) != -1)
		 {
			pre_kill = true;
		 }

		 if (arg.IndexOf("s", StringComparison.Ordinal) != -1)
		 {
			post_kill = true;
		 }
	  }

	  if (!usedefault)
	  {
         string[] st = args[0].Split(new char[] { '_' });

		 if (st.Length != 2)
		 {
			printUsageString();

			return;
		 }

		 if (args.Length > 1)
		 {
			string arg = args [1];

			if (arg.IndexOf("a", StringComparison.Ordinal) != -1)
			{
			   show_history = true;
			}

			if (arg.IndexOf("l", StringComparison.Ordinal) != -1)
			{
			   show_log = true;
			}

			if (arg.IndexOf("h", StringComparison.Ordinal) != -1)
			{
			   show_histogram = true;
			}

			if (arg.IndexOf("k", StringComparison.Ordinal) != -1)
			{
			   pre_kill = true;
			}

			if (arg.IndexOf("s", StringComparison.Ordinal) != -1)
			{
			   post_kill = true;
			}
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
		 Debug.WriteLine("Could not find any DS1921 Thermocrons!");

		 return;
	  }

	  OneWireContainer21 owc = new OneWireContainer21();

	  owc.setupContainer(access, access.AddressAsLong);

	  //put the part into overdrive...make it sizzle!
	  owc.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, true);

	  //let's gather our information here...
      stopWatch.Start();

	  if (pre_kill)
	  {
		 try
		 {
			owc.disableMission();
		 }
		 catch (Exception e)
		 {
			Debug.WriteLine("Couldn't end mission before reading: " + e.ToString());
		 }
	  }

	  bool mission_in_progress = owc.getFlag(OneWireContainer21.STATUS_REGISTER, OneWireContainer21.MISSION_IN_PROGRESS_FLAG);
	  byte[] state;

	  DateTime cal = new DateTime();

	  // first, check to make sure that the thermochron isn't
	  // sampling, or at least that a sample isn't about to occur.
	  bool isSampling = false;
	  do
	  {
		 state = owc.readDevice();

         cal = new DateTime(owc.getClock(state) * TimeSpan.TicksPerMillisecond);

		 isSampling = mission_in_progress && (owc.getFlag(OneWireContainer21.STATUS_REGISTER, OneWireContainer21.SAMPLE_IN_PROGRESS_FLAG, state) || (cal.Second > 55));

		 if (isSampling)
		 {
		    // wait for current sample to finish
            Thread.Sleep(1000);
		 }
	  } while (isSampling);

	  int mission_count = owc.getMissionSamplesCounter(state);
	  int device_count = owc.getDeviceSamplesCounter(state);
	  long rtc = owc.getClock(state);
	  long next_alarm = owc.getClockAlarm(state);
	  DateTime time_stamp = owc.getMissionTimeStamp(state);
	  int sample_rate = owc.getSampleRate(state);
	  double high_alarm = owc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, state);
	  double low_alarm = owc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, state);
	  int[] histogram = owc.TemperatureHistogram;
	  byte[] log = owc.getTemperatureLog(state);
	  byte[] high_history = owc.getAlarmHistory(OneWireContainer21.TEMPERATURE_HIGH_ALARM);
	  byte[] low_history = owc.getAlarmHistory(OneWireContainer21.TEMPERATURE_LOW_ALARM);
      stopWatch.Stop();
	  bool clock_enabled = owc.isClockRunning(state);
	  bool alarm_enabled = owc.isClockAlarmEnabled(state);
	  bool clock_alarm = owc.isClockAlarming(state);
	  bool rollover = owc.getFlag(OneWireContainer21.CONTROL_REGISTER, OneWireContainer21.ROLLOVER_ENABLE_FLAG, state);
	  double current_temp = 0;
	  string mission_in_progress_string;

	  if (!mission_in_progress)
	  {
		 owc.doTemperatureConvert(state);

		 current_temp = owc.getTemperature(state);
		 mission_in_progress_string = "- NO MISSION IN PROGRESS AT THIS TIME";
	  }
	  else
	  {
		 mission_in_progress_string = "- MISSION IS CURRENTLY RUNNING";
	  }

	  //spew all this data

	  Debug.WriteLine("Dallas Semiconductor DS1921 Thermocron Mission Summary Demo");
	  Debug.WriteLine("-----------------------------------------------------------");
	  Debug.WriteLine("- Device ID : " + owc.AddressAsString);
	  Debug.WriteLine(mission_in_progress_string);

	  if (!mission_in_progress)
	  {
		 Debug.WriteLine("- Current Temperature : " + current_temp);
	  }
	  else
	  {
		 Debug.WriteLine("- Cannot read current temperature with mission in progress");
	  }

	  Debug.WriteLine("-----------------------------------------------------------");
	  Debug.WriteLine("- Number of mission samples: " + mission_count);
	  Debug.WriteLine("- Total number of samples  : " + device_count);
	  Debug.WriteLine("- Real Time Clock          : " + (clock_enabled ? "ENABLED" : "DISABLED"));
	  Debug.WriteLine("- Real Time Clock Value    : " + (new DateTime(rtc * TimeSpan.TicksPerMillisecond)).ToString("r"));
	  Debug.WriteLine("- Clock Alarm              : " + (alarm_enabled ? "ENABLED" : "DISABLED"));

	  if (alarm_enabled)
	  {
		 Debug.WriteLine("- Clock Alarm Status       : " + (clock_alarm ? "ALARMING" : "NOT ALARMING"));
		 Debug.WriteLine("- Next alarm occurs at     : " + (new DateTime(next_alarm * TimeSpan.TicksPerMillisecond)).ToString("r"));
	  }

	  Debug.WriteLine("- Last mission started     : " + time_stamp);
	  Debug.WriteLine("- Sample rate              : Every " + sample_rate + " minutes");
	  Debug.WriteLine("- High temperature alarm   : " + high_alarm);
	  Debug.WriteLine("- Low temperature alarm    : " + low_alarm);
	  Debug.WriteLine("- Rollover enabled         : " + (rollover ? "YES" : "NO"));
	  Debug.WriteLine("- Time to read Thermocron  : " + (stopWatch.ElapsedMilliseconds) + " milliseconds");
	  Debug.WriteLine("-----------------------------------------------------------");

	  if (show_history)
	  {
		 int start_offset, violation_count;

		 Debug.WriteLine("-");
		 Debug.WriteLine("-                   ALARM HISTORY");

		 if (low_history.Length == 0)
		 {
			Debug.WriteLine("- No violations against the low temperature alarm.");
			Debug.WriteLine("-");
		 }

		 for (int i = 0; i < low_history.Length / 4; i++)
		 {
			start_offset = (low_history [i * 4] & 0x0ff) | ((low_history [i * 4 + 1] << 8) & 0x0ff00) | ((low_history [i * 4 + 2] << 16) & 0x0ff0000);
			violation_count = 0x0ff & low_history [i * 4 + 3];

			Debug.WriteLine("- Low alarm started at     : " + (start_offset * sample_rate));
			Debug.WriteLine("-                          : Lasted " + (violation_count * sample_rate) + " minutes");
		 }

		 if (high_history.Length == 0)
		 {
			Debug.WriteLine("- No violations against the high temperature alarm.");
			Debug.WriteLine("-");
		 }

		 for (int i = 0; i < high_history.Length / 4; i++)
		 {
			start_offset = (high_history [i * 4] & 0x0ff) | ((high_history [i * 4 + 1] << 8) & 0x0ff00) | ((high_history [i * 4 + 2] << 16) & 0x0ff0000);
			violation_count = 0x0ff & high_history [i * 4 + 3];

			Debug.WriteLine("- High alarm started at    : " + (start_offset * sample_rate));
			Debug.WriteLine("-                          : Lasted " + (violation_count * sample_rate) + " minutes");
		 }

		 Debug.WriteLine("-----------------------------------------------------------");
	  }

	  if (show_log)
	  {
		 long time = (time_stamp.Ticks/TimeSpan.TicksPerMillisecond) + owc.getFirstLogOffset(state);

		 Debug.WriteLine("-");
		 Debug.WriteLine("-                   TEMPERATURE LOG");


         for (int i = 0; i < log.Length; i++)
		 {
            DateTime gc = new DateTime(time * TimeSpan.TicksPerMillisecond);
			Debug.WriteLine("- Temperature recorded at  : " + gc.ToString("r"));
			Debug.WriteLine("-                     was  : " + owc.decodeTemperature(log [i]) + " C");

			time += sample_rate * 60 * 1000;
		 }

		 Debug.WriteLine("-----------------------------------------------------------");
	  }

	  if (show_histogram)
	  {
		 double resolution = owc.TemperatureResolution;
		 double histBinWidth = owc.HistogramBinWidth;
		 double start = owc.HistogramLowTemperature;

		 Debug.WriteLine("-");
		 Debug.WriteLine("-                   TEMPERATURE HISTOGRAM");

		 for (int i = 0; i < histogram.Length; i++)
		 {
			Debug.WriteLine("- Histogram entry          : " + histogram [i] + " at temperature " + start + " to " + (start + (histBinWidth - resolution)) + " C");

			start += histBinWidth;
		 }
	  }

	  if (post_kill)
	  {
		 try
		 {
			owc.disableMission();
		 }
		 catch (Exception e)
		 {
			Debug.WriteLine("Couldn't end mission after reading: " + e.ToString());
		 }
	  }

	  access.endExclusive();
	  access.freePort();
	  access = null;
	  Debug.WriteLine("Finished.");
   }
}
