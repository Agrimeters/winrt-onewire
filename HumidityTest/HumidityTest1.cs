using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
/// Minimal demo to read the 1-Wire Humidity Sensor
/// 
/// @version    1.00, 31 August 2001
/// @author     DS
/// </summary>
public class HumidityTest1
{
   public static void Main1(string[] args)
   {
	  try
	  {
		 List<OneWireContainer> humidity_devices = new List<OneWireContainer>(1);

		 // get the default adapter and show header
		 DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;
		 Debug.WriteLine("");
		 Debug.WriteLine("Adapter: " + adapter.AdapterName + " Port: " + adapter.PortName);
		 Debug.WriteLine("");
		 Debug.WriteLine("Devices Found:");
		 Debug.WriteLine("--------------");

		 // get exclusive use of adapter/port
		 adapter.beginExclusive(true);

		 // find all devices
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 if (adapter.canFlex())
		 {
			adapter.Speed = DSPortAdapter.SPEED_FLEX;
		 }

		 // enumerate through all the 1-Wire devices found to find
		 // containers that implement HumidityContainer
		 for (IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {
			OneWireContainer owd = (OneWireContainer)owd_enum.Current;
			Debug.Write(owd.AddressAsString);
			if (owd is HumidityContainer)
			{
			   humidity_devices.Add(owd);
			   Debug.WriteLine("  Humidity Sensor, Relative=" + ((HumidityContainer)owd).Relative);
			}
			else
			{
			   Debug.WriteLine("  NOT Humidity Sensor");
			}
		 }

		 if (humidity_devices.Count == 0)
		 {
			throw new Exception("No Humitiy devices found!");
		 }

		 // display device found
		 Debug.WriteLine("");
		 Debug.WriteLine("Hit ENTER to stop reading humidity");

         Stream stream = loadResourceFile("HumidityTest.input.txt");
         StreamReader keyboard = new StreamReader(stream);
         string str = null;

		 // loop and read RH or ENTER to quit
		 for (;;)
		 {
			// read each RH temp found
			for (int i = 0; i < humidity_devices.Count; i++)
			{
			   HumidityContainer hc = (HumidityContainer)humidity_devices[i];
			   byte[] state = hc.readDevice();
			   hc.doHumidityConvert(state);
			   Debug.WriteLine(((OneWireContainer)hc).AddressAsString + " humidity = " + hc.getHumidity(state) + "%");
			}

			// check for ENTER
			str = keyboard.ReadLine();

			if (str == null)
			{
			   break;
			}
		 }

		 // end exclusive use of adapter
		 adapter.endExclusive();
		 // free port used by adapter
		 adapter.freePort();
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }

	  return;
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
           Assembly asm = typeof(HumidityTest.MainPage).GetTypeInfo().Assembly;
           return asm.GetManifestResourceStream(file);
       }
       catch (Exception)
       {
           Debug.WriteLine("Can't find resource: " + file);
       }
       return null;
   }
}
