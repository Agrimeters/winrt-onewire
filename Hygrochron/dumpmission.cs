using System;
using System.Diagnostics;
using System.Text;

/*---------------------------------------------------------------------------
 * Copyright (C) 2001 Dallas Semiconductor Corporation, All Rights Reserved.
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
using com.dalsemi.onewire.utils;

/// <summary>
/// Dumps the mission from a DS1922/DS2422 temperature and A-D/Humidity
/// data-logger.  If this is a DS1922H, all data values will be converted
/// and displayed as humidity values.
/// 
/// @version 1.00, 14 Aug, 2003
/// @author  shughes
/// </summary>
public class dumpmission
{
   /// <summary>
   /// usage string, all available command-line switches </summary>
   internal static string[][] usageString = new string[][]
   {
	   new string[] {"-hideTemp", "", "if present, temperature values will be suppressed in output"},
	   new string[] {"-hideData", "", "if present, data values will be suppressed in output"},
	   new string[] {"-stopMission", "", "if present, mission will be stopped before the data is retrieved"},
	   new string[] {"-useOverdrive", "", "if present, mission data will be collected in overdrive speed"},
	   new string[] {"-readWritePass", "H", "This is the read/write password to use for reading the mission, H=8 bytes of ascii-encoded hex"},
	   new string[] {"-readOnlyPass", "H", "This is the read-only password to use for reading the mission, H=8 bytes of ascii-encoded hex"}
   };

   /// <summary>
   /// prints out a usage string and exits </summary>
   public static void usage()
   {
	  Debug.WriteLine("");
	  Debug.WriteLine("switches:");
	  for (int i = 0; i < usageString.Length; i++)
	  {
		 Debug.WriteLine("   " + usageString[i][0] + usageString[i][1]);
		 Debug.WriteLine("      " + usageString[i][2]);
	  }
      return;
   }

   /// <summary>
   /// the main routine, parses the input switches, dumps the mission data </summary>
   public static void Main1(string[] args)
   {
	  DSPortAdapter adapter = null;

	  bool showHumidity = true;
	  bool showTemperature = true;
	  bool stopMission = false;
	  bool useOverdrive = false;
	  byte[] readWritePass = null, readOnlyPass = null;

	  try
	  {

		 if (args.Length > 0)
		 {
			for (int i = 0; i < args.Length; i++)
			{
			   string arg = args[i].ToUpper();
			   if (arg.Equals(usageString[0][0].ToUpper()))
			   {
				  showTemperature = false;
			   }
			   else if (arg.Equals(usageString[1][0].ToUpper()))
			   {
				  showHumidity = false;
			   }
			   else if (arg.Equals(usageString[2][0].ToUpper()))
			   {
				  stopMission = true;
			   }
			   else if (arg.Equals(usageString[3][0].ToUpper()))
			   {
				  useOverdrive = true;
			   }
			   else if (arg.IndexOf(usageString[4][0].ToUpper(), StringComparison.Ordinal) == 0)
			   {
//TODO confirm this conversion
				  readWritePass = Encoding.UTF8.GetBytes(arg.Substring(usageString[4][0].Length));
			   }
			   else if (arg.IndexOf(usageString[5][0].ToUpper(), StringComparison.Ordinal) == 0)
			   {
//TODO confirm this conversion
				  readOnlyPass = Encoding.UTF8.GetBytes(arg.Substring(usageString[5][0].Length));
			   }
			   else if (arg.Equals("-H"))
			   {
				  usage();
			   }
			   else
			   {
				  Debug.WriteLine("bad argument: '" + args[i] + "'");
				  usage();
			   }
			}
		 }

		 adapter = OneWireAccessProvider.DefaultAdapter;

		 adapter.beginExclusive(true);
		 adapter.targetFamily(0x41);

		 OneWireContainer41 owc = (OneWireContainer41)adapter.FirstDeviceContainer;

		 if (owc != null)
		 {
			Debug.WriteLine("Found " + owc.ToString());
			if (useOverdrive)
			{
			   Debug.WriteLine("setting speed as overdrive");
			   owc.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, true);
			}

			if (readWritePass != null)
			{
			   owc.setContainerReadWritePassword(readWritePass, 0);
			}
			if (readOnlyPass != null)
			{
			   owc.setContainerReadOnlyPassword(readOnlyPass, 0);
			}

			byte[] state = owc.readDevice(); //read to set container variables

			if (stopMission)
			{
			   Debug.WriteLine("Stopping mission");
			   bool missionStopped = false;
			   while (!missionStopped)
			   {
				  try
				  {
					 if (!owc.MissionRunning)
					 {
						Debug.WriteLine("Mission is stopped");
						missionStopped = true;
					 }
					 else
					 {
						owc.stopMission();
					 }
				  }
				  catch (Exception)
				  {
					  ;
				  }
			   }
			}

			bool loadResults = false;
			while (!loadResults)
			{
			   try
			   {
				  Debug.WriteLine("loading mission results");
				  owc.loadMissionResults();
				  loadResults = true;
			   }
			   catch (Exception e)
			   {
				  Debug.WriteLine(e.ToString());
				  Debug.Write(e.StackTrace);
			   }
			}

			Debug.WriteLine("Is Mission Running: " + owc.MissionRunning);

			if (owc.MissionSUTA)
			{
			   Debug.WriteLine("Start Upon Temperature Alarm: " + (owc.MissionWFTA?"Waiting for Temperature Alarm":"Got Temperature Alarm, Mission Started"));
			}

			Debug.WriteLine("Sample Rate: " + owc.getMissionSampleRate(0) + " secs");

			Debug.WriteLine("Mission Start Time: " + (new DateTime(owc.getMissionTimeStamp(0))));

			Debug.WriteLine("Mission Sample Count: " + owc.getMissionSampleCount(0));

			Debug.WriteLine("Rollover Enabled: " + owc.MissionRolloverEnabled);

			if (owc.MissionRolloverEnabled)
			{
			   Debug.WriteLine("Rollover Occurred: " + owc.hasMissionRolloverOccurred());

			   if (owc.hasMissionRolloverOccurred())
			   {
				  Debug.WriteLine("First Sample Timestamp: " + (new DateTime(owc.getMissionSampleTimeStamp(OneWireContainer41.TEMPERATURE_CHANNEL,0) | owc.getMissionSampleTimeStamp(OneWireContainer41.DATA_CHANNEL,0))));
				  Debug.WriteLine("Total Mission Samples: " + owc.getMissionSampleCountTotal(0));
			   }
			}

			Debug.WriteLine("Temperature Logging: " + (!owc.getMissionChannelEnable(OneWireContainer41.TEMPERATURE_CHANNEL)? "Disabled": owc.getMissionResolution(OneWireContainer41.TEMPERATURE_CHANNEL) + " bit"));
			Debug.WriteLine("Temperature Low Alarm: " + (!owc.getMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, 0)? "Disabled": owc.getMissionAlarm(OneWireContainer41.TEMPERATURE_CHANNEL, 0) + "C (" + (owc.hasMissionAlarmed(OneWireContainer41.TEMPERATURE_CHANNEL, 0)? "ALARM)":"no alarm)")));
			Debug.WriteLine("Temperature High Alarm: " + (!owc.getMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, 1)? "Disabled": owc.getMissionAlarm(OneWireContainer41.TEMPERATURE_CHANNEL, 1) + "C (" + (owc.hasMissionAlarmed(OneWireContainer41.TEMPERATURE_CHANNEL, 1)? "ALARM)":"no alarm)")));

			Debug.WriteLine(owc.getMissionLabel(OneWireContainer41.DATA_CHANNEL) + " Logging: " + (!owc.getMissionChannelEnable(OneWireContainer41.DATA_CHANNEL)? "Disabled": owc.getMissionResolution(OneWireContainer41.DATA_CHANNEL) + " bit"));
			Debug.WriteLine(owc.getMissionLabel(OneWireContainer41.DATA_CHANNEL) + " Low Alarm: " + (!owc.getMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, 0)? "Disabled": owc.getMissionAlarm(OneWireContainer41.DATA_CHANNEL, 0) + "% RH (" + (owc.hasMissionAlarmed(OneWireContainer41.DATA_CHANNEL, 0)? "ALARM)":"no alarm)")));
			Debug.WriteLine(owc.getMissionLabel(OneWireContainer41.DATA_CHANNEL) + " High Alarm: " + (!owc.getMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, 1)? "Disabled": owc.getMissionAlarm(OneWireContainer41.DATA_CHANNEL, 1) + "% RH (" + (owc.hasMissionAlarmed(OneWireContainer41.DATA_CHANNEL, 1)? "ALARM)":"no alarm)")));

			Debug.WriteLine("Total Device Samples: " + owc.DeviceSampleCount);

			if (showTemperature)
			{
			   Debug.WriteLine("Temperature Readings");
			   if (!owc.getMissionChannelEnable(OneWireContainer41.TEMPERATURE_CHANNEL))
			   {
				  Debug.WriteLine("Temperature Mission Not enabled");
			   }
			   else
			   {
				  int dataCount = owc.getMissionSampleCount(OneWireContainer41.TEMPERATURE_CHANNEL);
				  Debug.WriteLine("SampleCount = " + dataCount);
				  for (int i = 0; i < dataCount; i++)
				  {
					 Debug.WriteLine(owc.getMissionSample(OneWireContainer41.TEMPERATURE_CHANNEL, i));
				  }
			   }
			}

			if (showHumidity)
			{
			   Debug.WriteLine(owc.getMissionLabel(OneWireContainer41.DATA_CHANNEL) + " Readings");
			   if (!owc.getMissionChannelEnable(OneWireContainer41.DATA_CHANNEL))
			   {
				  Debug.WriteLine(owc.getMissionLabel(OneWireContainer41.DATA_CHANNEL) + " Mission Not enabled");
			   }
			   else
			   {
				  int dataCount = owc.getMissionSampleCount(OneWireContainer41.DATA_CHANNEL);
				  Debug.WriteLine("SampleCount = " + dataCount);
				  for (int i = 0; i < dataCount; i++)
				  {
					Debug.WriteLine(owc.getMissionSample(OneWireContainer41.DATA_CHANNEL, i));
				  }
			   }
			}
		 }
		 else
		 {
			Debug.WriteLine("No DS1922/DS2422 device found");
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
	  }
	  finally
	  {
		 if (adapter != null)
		 {
			adapter.endExclusive();
		 }
	  }
   }

}