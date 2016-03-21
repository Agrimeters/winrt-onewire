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

using TemperatureContainer = com.dalsemi.onewire.container.TemperatureContainer;
using System.IO;
using System.Reflection;

/// <summary>
///   menu driven program to test OneWireContainer with
///   TemperatureContainerDemo interface
/// </summary>
public class TemperatureContainerDemo1
{

   // constant for temperature display option
   internal const int CELSIUS = 0x01;
   internal const int FAHRENHEIT = 0x02;

   // user main option menu
   internal static Hashtable hashMainMenu = new Hashtable();
   internal static int mainMenuItemCount;

   // temperature display mode
   internal static int tempMode = CELSIUS;

   // temperature unit
   internal static string tempUnit = " C";
   internal static DSPortAdapter adapter = null;
   internal static System.IO.StreamReader dis = null;

   /// <summary>
   /// Method main
   /// 
   /// </summary>
   /// <param name="args">
   ///  </param>
   public static void Main1(string[] args)
   {
	  byte[] state = null;
	  double alarmLow;
	  double alarmHigh;
	  bool alarming;
	  OneWireContainer owc = null;
	  TemperatureContainer tc = null;

      Stream stream = loadResourceFile("TemperatureContainerDemo.input.txt");
      dis = new StreamReader(stream);

	  // find and initialize the first OneWireContainer with
	  // TemperatureContainerDemo interface
	  owc = initContainer();

	  if (!(owc is TemperatureContainer))
	  {
		 cleanup();
		 Debug.WriteLine("*************************************************************************");
		 Debug.WriteLine("No TemperatureContainerDemo found. Exit program.");
		 Debug.WriteLine("");
		 return;
	  }
	  else
	  {
		 tc = (TemperatureContainer) owc;
	  }

	  initMenu();

	  int curMenuChoice = 0;

	  while (true)
	  {
		 curMenuChoice = getMenuChoice(hashMainMenu, mainMenuItemCount);

		 try
		 {
			switch (curMenuChoice)
			{

			   case 0 : // Read Temperature Once
				  getTemperature(tc, 1);
				  break;
			   case 1 : // Read Temperature Multiple Time
				  Debug.Write("Please enter number of times: ");

				  int trial = (int) Number;

				  getTemperature(tc, trial);
				  break;
			   case 2 : // Read High and Low Alarms
				  if (!tc.hasTemperatureAlarms())
				  {
					 Debug.WriteLine("Temperature alarms not supported");
				  }
				  else
				  {
					 state = tc.readDevice();
					 alarmLow = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, state);
					 alarmHigh = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, state);

					 if (tempMode == FAHRENHEIT)
					 {
						alarmHigh = convertToFahrenheit(alarmHigh);
						alarmLow = convertToFahrenheit(alarmLow);
					 }

					 Debug.WriteLine("  Alarm: High = " + alarmHigh + tempUnit + ", Low = " + alarmLow + tempUnit);
				  }
				  break;
			   case 3 : // Set  High and Low Alarms
				  Debug.WriteLine("*************************************************************************");

				  if (!tc.hasTemperatureAlarms())
				  {
					 Debug.WriteLine("Temperature alarms not supported");
				  }
				  else
				  {
					 if (tempMode == CELSIUS)
					 {
						Debug.WriteLine("*** Temperature value in Celsius ***");
					 }
					 else
					 {
						Debug.WriteLine("*** Temperature value in Fehrenheit ***");
					 }

					 Debug.Write("Enter alarm high value: ");

					 double inputHigh = Number;

					 Debug.Write("Enter alarm low value: ");

					 double inputLow = Number;

					 if (tempMode == CELSIUS)
					 {
						alarmHigh = inputHigh;
						alarmLow = inputLow;
					 }
					 else
					 {
						alarmHigh = convertToCelsius(inputHigh);
						alarmLow = convertToCelsius(inputLow);
					 }

					 state = tc.readDevice();

					 tc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, alarmHigh, state);
					 tc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, alarmLow, state);
					 tc.writeDevice(state);

					 state = tc.readDevice();
					 alarmLow = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, state);
					 alarmHigh = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, state);

					 if (tempMode == FAHRENHEIT)
					 {
						alarmHigh = convertToFahrenheit(alarmHigh);
						alarmLow = convertToFahrenheit(alarmLow);
					 }

					 Debug.WriteLine("  Set Alarm: High = " + alarmHigh + tempUnit + ", Low = " + alarmLow + tempUnit);
				  }
				  break;
			   case 4 : // isAlarming
				  if (!tc.hasTemperatureAlarms())
				  {
					 Debug.WriteLine("Temperature alarms not supported");
				  }
				  else
				  {
					 alarming = owc.Alarming;

					 if (alarming)
					 {
						Debug.WriteLine("  Alarming");
					 }
					 else
					 {
						Debug.WriteLine("  Not Alarming");
					 }
				  }
				  break;
			   case 5 : // Temperature Display Option
				  Debug.WriteLine("*************************************************************************");
				  Debug.WriteLine("1. Celsius");
				  Debug.WriteLine("2. Fehrenheit");
				  Debug.Write("Please enter temperature display option: ");

				  int choice = (int) Number;

				  if (choice == 2)
				  {
					 tempMode = FAHRENHEIT;
					 tempUnit = " F";

					 Debug.WriteLine("  Set to Fehrenheit display mode ");
				  }
				  else
				  {
					 tempMode = CELSIUS;
					 tempUnit = " C";

					 Debug.WriteLine("  Set to Celsius display mode (default) ");
				  }
				  break;
			   case 6 :
				  cleanup();
				  return;
			} //switch
		 }
		 catch (Exception e)
		 {
			printException(e);
		 }
	  } // while
   }

   // find the first OneWireContainer with TemperatureContainerDemo
   // interface. If found, initialize the container
   internal static OneWireContainer initContainer()
   {
	  byte[] state = null;
	  OneWireContainer owc = null;
	  TemperatureContainer tc = null;

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the one wire device found
		 for (System.Collections.IEnumerator owc_enum = adapter.AllDeviceContainers; owc_enum.MoveNext();)
		 {

			// get the next owc
			owc = (OneWireContainer) owc_enum.Current;

			// check for one wire device that implements TemperatureCotainer interface
			if (owc is TemperatureContainer)
			{
			   tc = (TemperatureContainer) owc;

			   // access One Wire device
			   state = tc.readDevice();

			   if (tc.hasSelectableTemperatureResolution())
			   {
				  double[] resolution = tc.TemperatureResolutions;

				  tc.setTemperatureResolution(resolution [resolution.Length - 1], state);
			   }

			   tc.writeDevice(state);

			   // print device information
			   Debug.WriteLine("");
			   Debug.WriteLine("*************************************************************************");
			   Debug.WriteLine("* 1-Wire Device Name: " + owc.Name);
			   Debug.WriteLine("* 1-Wire Device Other Names: " + owc.AlternateNames);
			   Debug.WriteLine("* 1-Wire Device Address: " + owc.AddressAsString);
			   Debug.WriteLine("* 1-Wire Device Max speed: " + ((owc.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
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

   // read temperature from device
   internal static void getTemperature(TemperatureContainer tc, int trial)
   {
	  // get the current resolution and other settings of the device
	  byte[] state = tc.readDevice();
	  double lastTemp;

	  while (trial-- > 0)
	  {
		 // perform a temperature conversion
		 tc.doTemperatureConvert(state);

		 // read the result of the conversion
		 state = tc.readDevice();

		 // extract the result out of state
		 lastTemp = tc.getTemperature(state);

		 if (tempMode == FAHRENHEIT)
		 {
			lastTemp = convertToFahrenheit(lastTemp);
		 }

		 // show results up to 2 decimal places
		 Debug.WriteLine("  Temperature = " + ((int)(lastTemp * 100)) / 100.0 + tempUnit);
	  }
   }

   /// <summary>
   /// initialize menu choices </summary>
   internal static void initMenu()
   {
	  hashMainMenu[new int?(0)] = "Read Temperature Once";
	  hashMainMenu[new int?(1)] = "Read Temperature Multiple Time";
	  hashMainMenu[new int?(2)] = "Read High and Low Alarms";
	  hashMainMenu[new int?(3)] = "Set  High and Low Alarms";
	  hashMainMenu[new int?(4)] = "isAlarming";
	  hashMainMenu[new int?(5)] = "Temperature Display Option";
	  hashMainMenu[new int?(6)] = "Quit";

	  mainMenuItemCount = 7;

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
   /// Convert a temperature from Celsius to Fahrenheit. </summary>
   internal static double convertToFahrenheit(double celsiusTemperature)
   {
	  return (double)(celsiusTemperature * 9.0 / 5.0 + 32.0);
   }

   /// <summary>
   /// Convert a temperature from Fahrenheit to Celsius. </summary>
   internal static double convertToCelsius(double fahrenheitTemperature)
   {
	  return (double)((fahrenheitTemperature - 32.0) * 5.0 / 9.0);
   }

   /// <summary>
   /// print out Exception stack trace </summary>
   internal static void printException(Exception e)
   {
	  Debug.WriteLine("***** EXCEPTION *****");
	  Debug.WriteLine(e.ToString());
	  Debug.Write(e.StackTrace);
   }

   public static Stream loadResourceFile(string file)
   {
       try
       {
           Assembly asm = typeof(TemperatureContainerDemo.MainPage).GetTypeInfo().Assembly;
           return asm.GetManifestResourceStream(file);
       }
       catch (Exception)
       {
           Debug.WriteLine("Can't find resource: " + file);
       }
       return null;
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
