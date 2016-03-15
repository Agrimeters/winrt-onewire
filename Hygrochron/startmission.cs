using System;
using System.Text;
using System.Diagnostics;

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

/// <summary>
/// Starts a new mission on a DS1922/DS2422 temperature and A-D/Humidity
/// data-logger.  If this is a DS1922H, all data alarm values specified
/// will be considered humidity values and converted to the appropriate
/// A-D value before being written to the device.
/// 
/// @version 1.00, 14 Aug, 2003
/// @author  shughes
/// </summary>
public class startmission
{
   /// <summary>
   /// usage string, all available command-line switches </summary>
   internal static string[][] usageString = new string[][]
   {
	   new string[] {"-sample", "N", "sample rate for mission in seconds, N=decimal (required)"},
	   new string[] {"-tempBytes", "N", "resolution (in # of bytes) for temperature channel. 0-none 1-low 2-high"},
	   new string[] {"-dataBytes", "N", "resolution (in # of bytes) for data/humidity channel. 0-none 1-low 2-high"},
	   new string[] {"-tempAlarmHigh", "D", "value for temperature channel high alarm. D=double"},
	   new string[] {"-tempAlarmLow", "D", "value for temperature channel low alarm. D=double"},
	   new string[] {"-dataAlarmHigh", "D", "value for data/humidity channel high alarm. D=double"},
	   new string[] {"-dataAlarmLow", "D", "value for data/humidity channel low alarm. D=double"},
	   new string[] {"-startUponTempAlarm", "", "if present, mission SUTA bit is set"},
	   new string[] {"-rollover", "", "if present, rollover is enabled"},
	   new string[] {"-startDelay", "N", "number of minutes before mission should start, N=decimal"},
	   new string[] {"-readWritePass", "H", "This is the read/write password to use for starting the mission, H=8 bytes of ascii-encoded hex"}
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
   /// the main routine, parses the input switches, starts the mission </summary>
   public static void Main2(string[] args)
   {
	  DSPortAdapter adapter = null;

	  bool startUponTempAlarm = false;
	  bool rollover = false;
	  int sampleRate = -1;
	  int tempBytes = 1;
	  int dataBytes = 1;
	  int startDelay = 0;
	  double tempAlarmHigh = -1, tempAlarmLow = -1;
	  double dataAlarmHigh = -1, dataAlarmLow = -1;
	  byte[] readWritePass = null;

	  if (args.Length == 0)
	  {
		 usage();
	  }

	  for (int i = 0; i < args.Length; i++)
	  {
		 string arg = args[i].ToUpper();
		 if (arg.IndexOf(usageString[0][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			sampleRate = int.Parse(arg.Substring(usageString[0][0].Length));
		 }
		 else if (arg.IndexOf(usageString[1][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			tempBytes = int.Parse(arg.Substring(usageString[1][0].Length));
			if (tempBytes > 2)
			{
			   tempBytes = 2;
			}
			else if (tempBytes < 0)
			{
			   tempBytes = 0;
			}
		 }
		 else if (arg.IndexOf(usageString[2][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			dataBytes = int.Parse(arg.Substring(usageString[2][0].Length));
			if (dataBytes > 2)
			{
			   dataBytes = 2;
			}
			else if (dataBytes < 0)
			{
			   dataBytes = 0;
			}
		 }
		 else if (arg.IndexOf(usageString[3][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {

			tempAlarmHigh = toDouble(arg.Substring(usageString[3][0].Length));
		 }
		 else if (arg.IndexOf(usageString[4][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			tempAlarmLow = toDouble(arg.Substring(usageString[4][0].Length));
		 }
		 else if (arg.IndexOf(usageString[5][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			dataAlarmHigh = toDouble(arg.Substring(usageString[5][0].Length));
		 }
		 else if (arg.IndexOf(usageString[6][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			dataAlarmLow = toDouble(arg.Substring(usageString[5][0].Length));
		 }
		 else if (arg.Equals(usageString[7][0].ToUpper()))
		 {
			startUponTempAlarm = true;
		 }
		 else if (arg.Equals(usageString[8][0].ToUpper()))
		 {
			rollover = true;
		 }
		 else if (arg.IndexOf(usageString[9][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			startDelay = int.Parse(arg.Substring(usageString[9][0].Length));
		 }
		 else if (arg.IndexOf(usageString[10][0].ToUpper(), StringComparison.Ordinal) == 0)
		 {
			readWritePass = Encoding.UTF8.GetBytes(arg.Substring(usageString[10][0].Length));
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

	  if (sampleRate == -1)
	  {
		 Debug.WriteLine("You must provide a sample rate");
		 usage();
	  }

	  Debug.WriteLine("Sample Rate (seconds) = " + sampleRate);
	  Debug.WriteLine("Temperature Bytes = " + tempBytes);
	  Debug.WriteLine("Humidity Bytes = " + dataBytes);
	  if (tempAlarmHigh != -1)
	  {
		 Debug.WriteLine("Temperature Alarm High = " + tempAlarmHigh);
	  }
	  if (tempAlarmLow != -1)
	  {
		 Debug.WriteLine("Temperature Alarm Low = " + tempAlarmLow);
	  }
	  if (dataAlarmHigh != -1)
	  {
		 Debug.WriteLine("Data Alarm High = " + dataAlarmHigh);
	  }
	  if (dataAlarmLow != -1)
	  {
		 Debug.WriteLine("Data Alarm Low = " + dataAlarmLow);
	  }
	  Debug.WriteLine("Start Upon Temp Alarm = " + startUponTempAlarm);
	  Debug.WriteLine("Rollover Enabled = " + rollover);
	  Debug.WriteLine("Start Delay (minutes) = " + startDelay);
	  Debug.WriteLine("");

	  if (startUponTempAlarm && (tempAlarmHigh == -1 || tempAlarmLow == -1))
	  {
		 Debug.WriteLine("You selected a SUTA mission, but didn't specify high and low temp alarms");
		 usage();
	  }

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 adapter.beginExclusive(true);
		 adapter.targetFamily(0x41);

		 OneWireContainer41 owc = (OneWireContainer41)adapter.FirstDeviceContainer;

		 if (readWritePass != null)
		 {
			owc.setContainerReadWritePassword(readWritePass, 0);
		 }

		 byte[] state = owc.readDevice(); //read to set container variables

		 if (owc != null)
		 {
			Debug.WriteLine("Found " + owc.ToString());
			Debug.WriteLine("Stopping current mission, if there is one");
			if (owc.MissionRunning)
			{
			   owc.stopMission();
			}

			Debug.WriteLine("Starting a new mission");

			if (tempBytes == 1)
			{
			   owc.setMissionResolution(0, owc.getMissionResolutions(0)[0]);
			}
			else
			{
			   owc.setMissionResolution(0, owc.getMissionResolutions(0)[1]);
			}

			if (dataBytes == 1)
			{
			   owc.setMissionResolution(1, owc.getMissionResolutions(1)[0]);
			}
			else
			{
			   owc.setMissionResolution(1, owc.getMissionResolutions(1)[1]);
			}


			if (tempAlarmHigh != -1)
			{
			   owc.setMissionAlarm(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_HIGH, tempAlarmHigh);
			   owc.setMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_HIGH, true);
			}
			else
			{
			   owc.setMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_HIGH, false);
			}

			if (tempAlarmLow != -1)
			{
			   owc.setMissionAlarm(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_LOW, tempAlarmLow);
			   owc.setMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_LOW, true);
			}
			else
			{
			   owc.setMissionAlarmEnable(OneWireContainer41.TEMPERATURE_CHANNEL, TemperatureContainer_Fields.ALARM_LOW, false);
			}

			if (dataAlarmHigh != -1)
			{
			   owc.setMissionAlarm(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_HIGH, dataAlarmHigh);
			   owc.setMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_HIGH, true);
			}
			else
			{
			   owc.setMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_HIGH, false);
			}

			if (dataAlarmLow != -1)
			{
			   owc.setMissionAlarm(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_LOW, dataAlarmLow);
			   owc.setMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_LOW, true);
			}
			else
			{
			   owc.setMissionAlarmEnable(OneWireContainer41.DATA_CHANNEL, ADContainer_Fields.ALARM_LOW, false);
			}

			owc.StartUponTemperatureAlarmEnable = startUponTempAlarm;

			owc.startNewMission(sampleRate, startDelay, rollover, true, new bool[]{tempBytes > 0, dataBytes > 0});

			Debug.WriteLine("Mission Started");
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

   // can't use Double.parseDouble on TINI
   // only allows 1 decimal place
   private static double toDouble(string dubbel)
   {
	  int dot = dubbel.IndexOf(".", StringComparison.Ordinal);
	  if (dot < 0)
	  {
		 return int.Parse(dubbel);
	  }

	  int wholePart = 0;
	  if (dot > 0)
	  {
		 wholePart = int.Parse(dubbel.Substring(0,dot));
	  }

	  int fractionPart = int.Parse(dubbel.Substring(dot + 1, 1 - (dot + 1)));

	  return (double)wholePart + fractionPart / 10.0d;
   }
}
