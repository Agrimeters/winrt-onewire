using System;
using System.Collections;
using System.Threading;
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
using CRC16 = com.dalsemi.onewire.utils.CRC16;


/* author KLA */
public class ReadSwitch
{
   internal static int parseInt(System.IO.StreamReader @in, int def)
   {
	  try
	  {
		 return int.Parse(@in.ReadLine());
	  }
	  catch (Exception)
	  {
		 return def;
	  }
   }

   /// <summary>
   /// Method printUsageString
   /// 
   /// 
   /// </summary>
   public static void printUsageString()
   {
	  Debug.WriteLine("Switch Container Demo\r\n");
	  Debug.WriteLine("Usage: ");
	  Debug.WriteLine("   java ReadSitch ADAPTER_PORT\r\n");
	  Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
	  Debug.WriteLine("adapter you would like to use and the port you would like");
	  Debug.WriteLine("to use, for example: ");
	  Debug.WriteLine("   java ReadSwitch {DS1410E}_LPT1\r\n");
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
	  access.targetAllFamilies();
	  access.beginExclusive(true);
	  access.reset();
	  access.setSearchAllDevices();

	  bool next = access.findFirstDevice();

	  if (!next)
	  {
		 Debug.WriteLine("Could not find any iButtons!");

		 return;
	  }

	  while (next)
	  {
		 OneWireContainer owc = access.DeviceContainer;

		 Debug.WriteLine("====================================================");
		 Debug.WriteLine("= Found One Wire Device: " + owc.AddressAsString + "          =");
		 Debug.WriteLine("====================================================");
		 Debug.WriteLine("=");

		 bool isSwitchContainer = false;
		 SwitchContainer sc = null;

		 try
		 {
			sc = (SwitchContainer) owc;
			isSwitchContainer = true;
		 }
		 catch (InvalidCastException)
		 {
			sc = null;
			isSwitchContainer = false; //just to reiterate
		 }

		 if (isSwitchContainer)
		 {
			Debug.WriteLine("= This device is a " + owc.Name);
			Debug.WriteLine("= Also known as a " + owc.AlternateNames);
			Debug.WriteLine("=");
			Debug.WriteLine("= It is a Switch Container");
			if (sc.hasActivitySensing())
			{
			   sc.clearActivity();
			}

			byte[] state = sc.readDevice();
			int channels = sc.getNumberChannels(state);
			bool activity = sc.hasActivitySensing();
			bool level = sc.hasLevelSensing();
			bool smart = sc.hasSmartOn();

			Debug.WriteLine("= This device has " + channels + " channel" + (channels > 1 ? "s" : ""));
			Debug.WriteLine("= It " + (activity ? "has" : "does not have") + " activity sensing abilities");
			Debug.WriteLine("= It " + (level ? "has" : "does not have") + " level sensing abilities");
			Debug.WriteLine("= It " + (smart ? "is" : "is not") + " smart-on capable");

			for (int ch = 0; ch < channels; ch++)
			{
			   Debug.WriteLine("======================");
			   Debug.WriteLine("=   Channel " + ch + "        =");
			   Debug.WriteLine("=--------------------=");

			   bool latchstate = sc.getLatchState(ch, state);

			   Debug.WriteLine("= State " + (latchstate ? "ON " : "OFF") + "          =");

			   if (level)
			   {
				  bool sensedLevel = sc.getLevel(ch, state);

				  Debug.WriteLine("= Level " + (sensedLevel ? "HIGH" : "LOW ") + "         =");
			   }

			   if (activity)
			   {
				  bool sensedActivity = sc.getSensedActivity(ch, state);

				  Debug.WriteLine("= Activity " + (sensedActivity ? "YES" : "NO ") + "       =");
			   }

			   Debug.WriteLine("= Toggling switch... =");

			   try
			   {
				  Thread.Sleep(500);
			   }
			   catch (Exception)
			   {

				  /*drain it*/
			   }

			   sc.setLatchState(ch, !latchstate, smart, state);
			   sc.writeDevice(state);

			   state = sc.readDevice();

			   if (latchstate == sc.getLatchState(ch, state))
			   {
				  Debug.WriteLine("= Toggle Failed      =");
			   }
			   else
			   {
				  try
				  {
					 Thread.Sleep(500);
				  }
				  catch (Exception)
				  {

					 /*drain it*/
				  }

				  Debug.WriteLine("= Toggling back...   =");
				  sc.setLatchState(ch, latchstate, smart, state);
				  sc.writeDevice(state);
			   }
			}
		 }
		 else
		 {
			Debug.WriteLine("= This device is not a Switch device.");
			Debug.WriteLine("=");
			Debug.WriteLine("=");
		 }

		 next = access.findNextDevice();
	  }
   }
}
