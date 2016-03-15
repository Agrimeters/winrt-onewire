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

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;
using System.IO;
using System.Reflection;

/// <summary>
/// menu driven program to test OneWireContainer with ADContainer interface </summary>
public class ADContainerDemo1
{

   // user main option menu
   internal static Hashtable hashMainMenu = new Hashtable();
   internal static int mainMenuItemCount;
   internal static DSPortAdapter adapter = null;
   internal static int maxNumChan = 1; // maximum number of channel
   internal static bool chanSelected = false; // set to true if user has selected channels
   internal static string ADUnit = " V"; // A/D unit
   internal static System.IO.StreamReader dis = null;

   /// <summary>
   /// Method main
   /// 
   /// </summary>
   /// <param name="args">
   ///  </param>
   public static void Main1(string[] args)
   {
	  OneWireContainer owc = null;
	  ADContainer adc = null;

      Stream stream = loadResourceFile("ADcontainerDemo.input.txt");
      dis = new StreamReader(stream);

	  // find and initialize the first OneWireContainer with
	  // ADContainer interface
	  owc = initContainer();

	  if (!(owc is ADContainer))
	  {
		 cleanup();
		 Debug.WriteLine("*************************************************************************");
		 Debug.WriteLine("No ADContainer found. Exit program.");
		 Debug.WriteLine("");
		 return;
	  }
	  else
	  {
		 adc = (ADContainer) owc;
	  }

	  maxNumChan = adc.NumberADChannels;

	  // array to determine whether a specific channel has been selected
	  bool[] channel = new bool [maxNumChan];

	  for (int i = 0; i < maxNumChan; i++) // default, no channel selected
	  {
		 channel [i] = false;
	  }

	  byte[] state = null; // data from device
	  double[] ranges = null; // A/D ranges
	  double alarmLow = 0; // alarm low value
	  double alarmHigh = 0; // alarm high value
	  bool alarming;

	  // temporary storage for user input
	  int inputInt = 0;
	  double inputDouble = 0.0;
	  double inputLow = 0.0;
	  double inputHigh = 0.0;
	  string inputString = null;
	  bool moreInput = true;
	  int curMenuChoice = 0;

	  initMenu();

	  while (true)
	  {
		 curMenuChoice = getMenuChoice(hashMainMenu, mainMenuItemCount);

		 try
		 {
			switch (curMenuChoice)
			{

			   case 0 : // Select Channel
				  Debug.WriteLine("*************************************************************************");
				  Debug.WriteLine("All previously selectd channels have been cleared.");

				  state = adc.readDevice();

				  for (int i = 0; i < maxNumChan; i++)
				  {

					 // clear all channel selection
					 channel [i] = false;

					 if (adc.hasADAlarms())
					 {

						// disable alarms
						adc.setADAlarmEnable(i, ADContainer_Fields.ALARM_LOW, false, state);
						adc.setADAlarmEnable(i, ADContainer_Fields.ALARM_HIGH, false, state);
					 }
				  }

				  adc.writeDevice(state);

				  chanSelected = false;
				  state = adc.readDevice();

				  int count = 1;

				  moreInput = true;

				  while (moreInput && (count <= maxNumChan))
				  {
					 Debug.Write("Please enter channel # " + count + " ( Enter -1 if no more): ");

					 inputInt = (int) Number;

					 if (inputInt == -1)
					 {
						moreInput = false;
					 }
					 else
					 {
						if (isChannelValid(inputInt))
						{
						   channel [inputInt] = true;

						   count++;

						   chanSelected = true;

						   if (adc.hasADAlarms())
						   {

							  // enable alarms
							  adc.setADAlarmEnable(inputInt, ADContainer_Fields.ALARM_LOW, true, state);
							  adc.setADAlarmEnable(inputInt, ADContainer_Fields.ALARM_HIGH, true, state);
						   }
						}
					 }
				  } // while (moreInput && (count <= maxNumChan))

				  adc.writeDevice(state);
				  Debug.Write("  Channels to monitor = ");

				  if (count == 1)
				  {
					 Debug.WriteLine("NONE");
				  }
				  else
				  {
					 for (int i = 0; i < maxNumChan; i++)
					 {
						if (channel [i])
						{
						   Debug.Write(i + " ");
						}
					 }

					 Debug.WriteLine("");
				  }
				  break;
			   case 1 : // Get A/D Once
				  getVoltage(adc, channel, 1);
				  break;
			   case 2 : // Get A/D Multiple Time
				  Debug.Write("Please enter number of times: ");

				  inputInt = (int) Number;

				  getVoltage(adc, channel, inputInt);
				  break;
			   case 3 : // Get A/D ranges
				  if (!chanSelected)
				  {
					 Debug.WriteLine("No channel selected yet. Cannot get A/D ranges.");
				  }
				  else
				  {
					 state = adc.readDevice();

					 for (int i = 0; i < maxNumChan; i++)
					 {
						if (channel [i])
						{
						   ranges = adc.getADRanges(i);

						   Debug.Write("Ch " + i + " - Available: " + ranges [0] + ADUnit);

						   for (int j = 1; j < ranges.Length; j++)
						   {
							  Debug.Write(", " + ranges [j] + ADUnit);
						   }

						   Debug.WriteLine(". Current: " + adc.getADRange(i, state) + ADUnit);
						}
					 }
				  }
				  break;
			   case 4 : // Set A/D ranges
				  Debug.WriteLine("*************************************************************************");

				  state = adc.readDevice();
				  moreInput = true;

				  while (moreInput)
				  {
					 Debug.Write("Please enter channel number: ");

					 inputInt = (int) Number;

					 if (isChannelValid(inputInt))
					 {
						Debug.Write("Please enter range value: ");

						inputDouble = Number;

						adc.setADRange(inputInt, inputDouble, state);
						adc.writeDevice(state);

						state = adc.readDevice();

						Debug.WriteLine("  Ch" + inputInt + " A/D Ranges set to: " + adc.getADRange(inputInt, state));
						Debug.Write("Set more A/D ranges (Y/N)? ");

						inputString = dis.ReadLine();

						if (!inputString.Trim().ToUpper().Equals("Y"))
						{
						   moreInput = false;
						}
					 }
				  } // while(moreInput)
				  break;
			   case 5 : // Get High and Low Alarms
				  if (!adc.hasADAlarms())
				  {
					 Debug.WriteLine("A/D alarms not supported");
				  }
				  else if (!chanSelected)
				  {
					 Debug.WriteLine("No channel selected yet. Cannot get high and low alarms.");
				  }
				  else
				  {
					 state = adc.readDevice();

					 for (int i = 0; i < maxNumChan; i++)
					 {
						if (channel [i])
						{
						   alarmLow = adc.getADAlarm(i, ADContainer_Fields.ALARM_LOW, state);
						   alarmHigh = adc.getADAlarm(i, ADContainer_Fields.ALARM_HIGH, state);

						   // show results up to 2 decimal places
						   Debug.WriteLine("Ch " + i + " Alarm: High = " + ((int)(alarmHigh * 100)) / 100.0 + ADUnit + ", Low = " + ((int)(alarmLow * 100)) / 100.0 + ADUnit);
						}
					 } // for
				  }
				  break;
			   case 6 : // Set High and Low Alarms
				  if (!adc.hasADAlarms())
				  {
					 Debug.WriteLine("A/D alarms not supported");
				  }
				  else
				  {
					 Debug.WriteLine("*************************************************************************");

					 state = adc.readDevice();
					 moreInput = true;

					 while (moreInput)
					 {
						Debug.Write("Please enter channel number: ");

						inputInt = (int) Number;

						if (isChannelValid(inputInt))
						{
						   bool inputValid = false;

						   while (!inputValid)
						   {
							  Debug.Write("Please enter alarm high value: ");

							  inputHigh = Number;

							  if (inputHigh > adc.getADRange(inputInt, state))
							  {
								 Debug.WriteLine("Current A/D range is: " + adc.getADRange(inputInt, state) + ADUnit + ". Invalid alarm high value.");
							  }
							  else
							  {
								 inputValid = true;
							  }
						   }

						   Debug.Write("Please enter alarm low value: ");

						   inputLow = Number;

						   adc.setADAlarm(inputInt, ADContainer_Fields.ALARM_LOW, inputLow, state);
						   adc.setADAlarm(inputInt, ADContainer_Fields.ALARM_HIGH, inputHigh, state);
						   adc.writeDevice(state);

						   state = adc.readDevice();

						   // show results up to 2 decimal places
						   Debug.WriteLine("  Set Ch" + inputInt + " Alarm: High = " + ((int)(adc.getADAlarm(inputInt, ADContainer_Fields.ALARM_HIGH, state) * 100)) / 100.0 + ADUnit + ", Low = " + ((int)(adc.getADAlarm(inputInt, ADContainer_Fields.ALARM_LOW, state) * 100)) / 100.0 + ADUnit);
						   Debug.Write("Set more A/D alarms (Y/N)? ");

						   inputString = dis.ReadLine();

						   if (!inputString.Trim().ToUpper().Equals("Y"))
						   {
							  moreInput = false;
						   }
						}
					 } // while(moreInput)
				  }
				  break;
			   case 7 : // hasADAlarmed
				  if (!adc.hasADAlarms())
				  {
					 Debug.WriteLine("A/D alarms not supported");
				  }
				  else
				  {
					 alarming = owc.Alarming;

					 if (alarming)
					 {
						Debug.Write("  Alarms: ");

						state = adc.readDevice();

						for (int i = 0; i < maxNumChan; i++)
						{
						   if (channel [i])
						   {
							  if (adc.hasADAlarmed(i, ADContainer_Fields.ALARM_HIGH, state))
							  {
								 Debug.Write("Ch" + i + " alarmed high; ");
							  }

							  if (adc.hasADAlarmed(i, ADContainer_Fields.ALARM_LOW, state))
							  {
								 Debug.Write("Ch" + i + " alarmed low; ");
							  }
						   }
						}

						Debug.WriteLine("");
					 }
					 else
					 {
						Debug.WriteLine("  Not Alarming");
					 }
				  }
				  break;
			   case 8 :
				  cleanup();
				  return;
				  break;
			}
		 }
		 catch (Exception e)
		 {
			printException(e);
		 }
	  } // while
   }

   // find the first OneWireContainer with ADContainer interface
   // if found, initialize the container
   internal static OneWireContainer initContainer()
   {
	  byte[] state = null;
	  OneWireContainer owc = null;
	  ADContainer adc = null;

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the One Wire device found
		 for (System.Collections.IEnumerator owc_enum = adapter.AllDeviceContainers; owc_enum.MoveNext();)
		 {

			// get the next owc
			owc = (OneWireContainer) owc_enum.Current;

			// check for One Wire device that implements ADCotainer interface
			if (owc is ADContainer)
			{
			   adc = (ADContainer) owc;

			   // access One Wire device
			   state = adc.readDevice();

			   double[] range = null;
			   double[] resolution = null;

			   // set resolution
			   for (int channel = 0; channel < adc.NumberADChannels; channel++)
			   {
				  range = adc.getADRanges(channel);
				  resolution = adc.getADResolutions(channel, range [0]);

				  // set to largest range
				  adc.setADRange(channel, range [0], state);

				  // set to highest resolution
				  adc.setADResolution(channel, resolution [resolution.Length - 1], state);

				  if (adc.hasADAlarms())
				  {

					 // disable all alarms
					 adc.setADAlarmEnable(channel, ADContainer_Fields.ALARM_LOW, false, state);
					 adc.setADAlarmEnable(channel, ADContainer_Fields.ALARM_HIGH, false, state);
				  }
			   }

			   adc.writeDevice(state);

			   // print device information
			   Debug.WriteLine("");
			   Debug.WriteLine("*************************************************************************");
			   Debug.WriteLine("* 1-Wire Device Name: " + owc.Name);
			   Debug.WriteLine("* 1-Wire Device Other Names: " + owc.AlternateNames);
			   Debug.WriteLine("* 1-Wire Device Address: " + owc.AddressAsString);
			   Debug.WriteLine("* 1-Wire Device Max speed: " + ((owc.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
			   Debug.WriteLine("* 1-Wire Device Number of Channels: " + adc.NumberADChannels);
			   Debug.WriteLine("* 1-Wire Device Can Read MultiChannels: " + adc.canADMultiChannelRead());
			   Debug.WriteLine("* 1-Wire Device Description: " + owc.Description);
			   Debug.WriteLine("*************************************************************************");
			   Debug.WriteLine("  Hit ENTER to continue...");
			   dis.ReadLine();

			   break;
			}
		 } // enum all owc
	  }
	  catch (Exception e)
	  {
		 printException(e);
	  }

	  return owc;
   }

   // read A/D from device
   internal static void getVoltage(ADContainer adc, bool[] channel, int trial)
   {
	  byte[] state;
	  double[] curVoltage = new double [channel.Length];

	  if (!chanSelected)
	  {
		 Debug.WriteLine("No channel selected yet. Cannot get voltage reading.");

		 return;
	  }

	  while (trial-- > 0)
	  {
		 state = adc.readDevice();

		 if (adc.canADMultiChannelRead())
		 {

			// do all channels together
			adc.doADConvert(channel, state);

			curVoltage = adc.getADVoltage(state);
		 }
		 else
		 {

			// do one channel at a time;
			for (int i = 0; i < maxNumChan; i++)
			{
			   if (channel [i])
			   {
				  adc.doADConvert(i, state);

				  curVoltage [i] = adc.getADVoltage(i, state);
			   }
			}
		 }

		 Debug.Write("  Voltage Reading:");

		 for (int i = 0; i < maxNumChan; i++)
		 {
			if (channel [i]) // show value up to 2 decimal places
			{
			   Debug.Write(" Ch" + i + " = " + ((int)(curVoltage [i] * 10000)) / 10000.0 + ADUnit);
			}
		 }

		 Debug.WriteLine("");
	  }
   }

   /// <summary>
   /// initialize menu choices </summary>
   internal static void initMenu()
   {
	  hashMainMenu[new int?(0)] = "Select Channel";
	  hashMainMenu[new int?(1)] = "Get Voltage Once";
	  hashMainMenu[new int?(2)] = "Get Voltage Multiple Time";
	  hashMainMenu[new int?(3)] = "Get A/D Ranges";
	  hashMainMenu[new int?(4)] = "Set A/D Ranges";
	  hashMainMenu[new int?(5)] = "Get High and Low A/D Alarms";
	  hashMainMenu[new int?(6)] = "Set High and Low A/D Alarms";
	  hashMainMenu[new int?(7)] = "hasADAlarmed";
	  hashMainMenu[new int?(8)] = "Quit";

	  mainMenuItemCount = 9;

	  return;
   }

   /// <summary>
   /// getMenuChoice - retrieve menu choice from the user </summary>
   internal static int getMenuChoice(Hashtable menu, int count)
   {
	  int choice = 0;

	  while (true)
	  {
		 Debug.WriteLine("*************************************************************************");

		 for (int i = 0; i < count; i++)
		 {
			Debug.WriteLine(i + ". " + menu[new int?(i)]);
		 }

		 Debug.Write("Please enter your choice: ");

		 // change input into integer number
		 choice = (int) Number;

		 if (menu[new int?(choice)] == null)
		 {
			 Debug.WriteLine("Invalid menu choice");
		 }
		 else
		 {
			 break;
		 }
	  }

	  return choice;
   }

   /// <summary>
   /// check for valid channel number input </summary>
   internal static bool isChannelValid(int channel)
   {
	  if ((channel < 0) || (channel >= maxNumChan))
	  {
		 Debug.WriteLine("Channel number has to be between 0 and " + (maxNumChan - 1));

		 return false;
	  }
	  else
	  {
		 return true;
	  }
   }

   /// <summary>
   /// Loads resource file to be used as Input Stream to drive program
   /// </summary>
   /// <param name="file"></param>
   /// <returns></returns>
   public static Stream loadResourceFile(string file)
   {
       try
       {
           Assembly asm = typeof(ADContainerDemo.MainPage).GetTypeInfo().Assembly;
           return asm.GetManifestResourceStream(file);
       }
       catch (Exception)
       {
           Debug.WriteLine("Can't find resource: " + file);
       }
       return null;
   }

   /// <summary>
   /// Retrieve user input from the console.
   /// </summary>
   /// <returns> numberic value entered from the console.
   ///  </returns>
   internal static double Number
   {
	   get
	   {
		  double value = -1;
		  string input;
    
		  while (true)
		  {
			 try
			 {
				input = dis.ReadLine();
				value = System.Convert.ToDouble(input);
				break;
			 }
			 catch (System.FormatException e)
			 {
				Debug.WriteLine("Invalid Numeric Value: " + e.ToString());
				Debug.Write("Please enter value again: ");
			 }
			 catch (System.IO.IOException e)
			 {
				Debug.WriteLine("Error in reading from console: " + e);
			 }
			 catch (Exception e)
			 {
				printException(e);
			 }
		  }
    
		  return value;
	   }
   }

   /// <summary>
   /// print out Exception stack trace </summary>
   internal static void printException(Exception e)
   {
	  Debug.WriteLine("***** EXCEPTION *****");
	  Debug.WriteLine(e.ToString());
	  Debug.Write(e.StackTrace);
   }

   /// <summary>
   /// clean up before exiting program </summary>
   internal static void cleanup()
   {
	  try
	  {
		 if (adapter != null)
		 {
			adapter.endExclusive(); // end exclusive use of adapter
			adapter.freePort(); // free port used by adapter
		 }
	  }
	  catch (Exception e)
	  {
		 printException(e);
	  }

	  return;
   }
}
