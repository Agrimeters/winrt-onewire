using System;
using System.Collections;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

using DefaultProperties = com.dalsemi.onewire.OneWireAccessProvider;
using System.Reflection;
using System.IO;

/// <summary>
/// Console application to View and change the state of a Switch
/// 
/// History:
/// 
/// @version    0.00, Oct 11 2000
/// @author     DS
/// </summary>
public class SwitchDemo
{
   /// <summary>
   /// Main for 1-Wire Memory utility
   /// </summary>
   public static void Main1(string[] args)
   {
	  ArrayList owd_vect = new ArrayList(5);
	  SwitchContainer sw = null;
	  int ch;
	  bool done = false;
	  DSPortAdapter adapter = null;
	  byte[] state;

      Stream stream = loadResourceFile("Switch.input.txt");
      dis = new StreamReader(stream);

	  Debug.WriteLine("");
	  Debug.WriteLine("1-Wire Switch utility console application: Version 0.00");
	  Debug.WriteLine("");

	  try
	  {
		 // get the default adapter  
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // adapter driver info
		 Debug.WriteLine("=========================================================================");
		 Debug.WriteLine("== Adapter Name: " + adapter.AdapterName);
		 Debug.WriteLine("== Adapter Port description: " + adapter.PortTypeDescription);
		 Debug.WriteLine("== Adapter Version: " + adapter.AdapterVersion);
		 Debug.WriteLine("== Adapter support overdrive: " + adapter.canOverdrive());
		 Debug.WriteLine("== Adapter support hyperdrive: " + adapter.canHyperdrive());
		 Debug.WriteLine("== Adapter support EPROM programming: " + adapter.canProgram());
		 Debug.WriteLine("== Adapter support power: " + adapter.canDeliverPower());
		 Debug.WriteLine("== Adapter support smart power: " + adapter.canDeliverSmartPower());
		 Debug.WriteLine("== Adapter Class Version: " + adapter.ClassVersion);

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 // force first select device
		 int main_selection = MAIN_SELECT_DEVICE;

		 // loop to do menu
		 do
		 {
			// Main menu
			switch (main_selection)
			{
			   case MAIN_DISPLAY_INFO:
				  // display Switch info
				  printSwitchInfo(sw);
				  break;

			   case MAIN_CLEAR_ACTIVITY:
				  sw.clearActivity();
				  state = sw.readDevice();
				  sw.writeDevice(state);
				  // display Switch info
				  printSwitchInfo(sw);
				  break;

			   case MAIN_SET_LATCH:
				  state = sw.readDevice();
				  Debug.Write("Enter the channel number: ");
				  ch = getNumber(0,sw.getNumberChannels(state) - 1);
				  if (menuSelect(stateMenu) == STATE_ON)
				  {
					 sw.setLatchState(ch,true,false,state);
				  }
				  else
				  {
					 sw.setLatchState(ch,false,false,state);
				  }
				  sw.writeDevice(state);
				  // display Switch info
				  printSwitchInfo(sw);
				  break;

			   case MAIN_SELECT_DEVICE:
				  // find all parts
				  owd_vect = findAllSwitchDevices(adapter);
				  // select a device
				  sw = (SwitchContainer)selectDevice(owd_vect);
				  // display device info
				  printDeviceInfo((OneWireContainer)sw);
				  // display the switch info
				  printSwitchInfo(sw);
				  break;

			   case MAIN_QUIT:
				  done = true;
				  break;
			}

			if (!done)
			{
			   main_selection = menuSelect(mainMenu);
			}

		 } while (!done);
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
	  finally
	  {
		 if (adapter != null)
		 {
			// end exclusive use of adapter
			adapter.endExclusive();

			// free the port used by the adapter
			Debug.WriteLine("Releasing adapter port");
			try
			{
			   adapter.freePort();
			}
			catch (OneWireException e)
			{
			   Debug.WriteLine(e);
			}
		 }
	  }

	  Debug.WriteLine("");
      return;
   }

   /// <summary>
   /// Search for all Switch devices on the provided adapter and return
   /// a vector 
   /// </summary>
   /// <param name="adapter"> valid 1-Wire adapter
   /// </param>
   /// <returns> Vector or OneWireContainers </returns>
   public static ArrayList findAllSwitchDevices(DSPortAdapter adapter)
   {
	  ArrayList owd_vect = new ArrayList(3);
	  OneWireContainer owd;

	  try
	  {
		 // clear any previous search restrictions
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the 1-Wire devices and collect them in a vector
		 for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {
			owd = (OneWireContainer)owd_enum.Current;

			// check if a switch
			if (!(owd is SwitchContainer))
			{
			   continue;
			}

			owd_vect.Add(owd);

			// set owd to max possible speed with available adapter, allow fall back
			if (adapter.canOverdrive() && (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
			{
			   owd.setSpeed(owd.MaxSpeed,true);
			}
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }

	  return owd_vect;
   }

   //--------
   //-------- Menu methods
   //--------

   /// <summary>
   /// Create a menu from the provided OneWireContainer
   /// Vector and allow the user to select a device.
   /// </summary>
   /// <param name="owd_vect"> vector of devices to choose from
   /// </param>
   /// <returns> OneWireContainer device selected </returns>
   public static OneWireContainer selectDevice(ArrayList owd_vect)
   {
	  // create a menu
	  string[] menu = new string[owd_vect.Count + 2];
	  OneWireContainer owd;
	  int i;

	  menu[0] = "Device Selection";
	  for (i = 0; i < owd_vect.Count; i++)
	  {
		 owd = (OneWireContainer)owd_vect[i];
		 menu[i + 1] = "(" + i + ") " + owd.AddressAsString + " - " + owd.Name;
		 if (owd.AlternateNames.Length > 0)
		 {
			menu[i + 1] += "/" + owd.AlternateNames;
		 }
	  }
	  menu[i + 1] = "[" + i + "]--Quit";

	  int select = menuSelect(menu);

	  if (select == i)
	  {
		 throw new OperationCanceledException("Quit in device selection");
	  }

	  return (OneWireContainer)owd_vect[select];
   }

   //--------
   //-------- Menu Methods
   //--------

   /// <summary>
   /// Display menu and ask for a selection.
   /// </summary>
   /// <param name="menu"> Array of strings that represents the menu.
   ///        The first element is a title so skip it.
   /// </param>
   /// <returns> numberic value entered from the console. </returns>
   internal static int menuSelect(string[] menu)
   {
	  Debug.WriteLine("");
	  for (int i = 0; i < menu.Length; i++)
	  {
		 Debug.WriteLine(menu[i]);
	  }

	  Debug.Write("Please enter value: ");

	  return getNumber(0, menu.Length - 2);
   }

   /// <summary>
   /// Retrieve user input from the console.
   /// </summary>
   /// <param name="min"> minimum number to accept </param>
   /// <param name="max"> maximum number to accept
   /// </param>
   /// <returns> numberic value entered from the console. </returns>
   internal static int getNumber(int min, int max)
   {
	  int value = -1;
	  bool fNumber = false;

	  while (fNumber == false)
	  {
		 try
		 {
			string str = getString(1);

			value = int.Parse(str);

			if ((value < min) || (value > max))
			{
			   Debug.WriteLine("Invalid value, range must be " + min + " to " + max);
			   Debug.Write("Please enter value again: ");
			}
			else
			{
			   fNumber = true;
			}
		 }
		 catch (System.FormatException e)
		 {
			Debug.WriteLine("Invalid Numeric Value: " + e.ToString());
			Debug.Write("Please enter value again: ");
		 }
	  }

	  return value;
    }

    public static Stream loadResourceFile(string file)
    {
        try
        {
            Assembly asm = typeof(Switch.MainPage).GetTypeInfo().Assembly;
            return asm.GetManifestResourceStream(file);
        }
        catch (Exception)
        {
            Debug.WriteLine("Can't find resource: " + file);
        }
        return null;
    }

    private static System.IO.StreamReader dis = null;

   /// <summary>
   /// Retrieve user input from the console.
   /// </summary>
   /// <param name="minLength"> minumum length of string
   /// </param>
   /// <returns> string entered from the console. </returns>
   internal static string getString(int minLength)
   {
	  string str;
	  bool done = false;

	  try
	  {
		 do
		 {
            str = dis.ReadLine();
			if (str.Length < minLength)
			{
			   Debug.Write("String too short try again:");
			}
			else
			{
			   done = true;
			}
		 } while (!done);

		 return str;
	  }
	  catch (System.IO.IOException e)
	  {
		 Debug.WriteLine("Error in reading from console: " + e);
	  }

	  return "";
   }

   //--------
   //-------- Display Methods
   //--------

   /// <summary>
   /// Display information about the 1-Wire device
   /// </summary>
   /// <param name="owd"> OneWireContainer device </param>
   internal static void printDeviceInfo(OneWireContainer owd)
   {
	  Debug.WriteLine("");
	  Debug.WriteLine("*************************************************************************");
	  Debug.WriteLine("* Device Name: " + owd.Name);
	  Debug.WriteLine("* Device Other Names: " + owd.AlternateNames);
	  Debug.WriteLine("* Device Address: " + owd.AddressAsString);
	  Debug.WriteLine("* Device Max speed: " + ((owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
	  Debug.WriteLine("* iButton Description: " + owd.Description);
   }

   /// <summary>
   /// Display information about the Switch device
   /// </summary>
   /// <param name="owd"> OneWireContainer device </param>
   internal static void printSwitchInfo(SwitchContainer swd)
   {
	  try
	  {
		 byte[] state = swd.readDevice();

		 Debug.WriteLine("");
		 Debug.WriteLine("-----------------------------------------------------------------------");
		 Debug.WriteLine("| Number of channels: " + swd.getNumberChannels(state));
		 Debug.WriteLine("| Is high-side switch: " + swd.HighSideSwitch);
		 Debug.WriteLine("| Has Activity Sensing: " + swd.hasActivitySensing());
		 Debug.WriteLine("| Has Level Sensing: " + swd.hasLevelSensing());
		 Debug.WriteLine("| Has Smart-on: " + swd.hasSmartOn());
		 Debug.WriteLine("| Only 1 channel on at a time: " + swd.onlySingleChannelOn());
		 Debug.WriteLine("");

		 Debug.Write("    Channel          ");
		 for (int ch = 0; ch < swd.getNumberChannels(state); ch++)
		 {
			Debug.Write(ch + "      ");
		 }
		 Debug.WriteLine("");

		 Debug.WriteLine("    -----------------------------------");

		 Debug.Write("    Latch State      ");
		 for (int ch = 0; ch < swd.getNumberChannels(state); ch++)
		 {
			Debug.Write(((swd.getLatchState(ch,state) == true) ? "ON     " : "OFF    "));
		 }
		 Debug.WriteLine("");

		 if (swd.hasLevelSensing())
		 {
			Debug.Write("    Sensed Level     ");
			for (int ch = 0; ch < swd.getNumberChannels(state); ch++)
			{
			   Debug.Write(((swd.getLevel(ch,state) == true) ? "HIGH   " : "LOW    "));
			}
			Debug.WriteLine("");
		 }

		 if (swd.hasActivitySensing())
		 {
			Debug.Write("    Sensed Activity  ");
			for (int ch = 0; ch < swd.getNumberChannels(state); ch++)
			{
			   Debug.Write(((swd.getSensedActivity(ch,state) == true) ? "SET    " : "CLEAR  "));
			}
			Debug.WriteLine("");
		 }
	  }
	  catch (OneWireIOException e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   //--------
   //-------- Menus
   //--------

   internal static readonly string[] mainMenu = new string[] {"MainMenu 1-Wire Switch Demo", "(0) Dislay switch state", "(1) Clear activity ", "(2) Set Latch state", "(3) Select new Device", "(4) Quit"};
   internal const int MAIN_DISPLAY_INFO = 0;
   internal const int MAIN_CLEAR_ACTIVITY = 1;
   internal const int MAIN_SET_LATCH = 2;
   internal const int MAIN_SELECT_DEVICE = 3;
   internal const int MAIN_QUIT = 4;

   internal static readonly string[] stateMenu = new string[] {"Channel State", "(0) Off, non-conducting", "(1) On, conducting"};
   internal const int STATE_OFF = 0;
   internal const int STATE_ON = 1;
}


