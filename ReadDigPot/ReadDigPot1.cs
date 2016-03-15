using System;
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


public class ReadDigPot1
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
	  Debug.WriteLine("Potentiometer Container Demo\r\n");
	  Debug.WriteLine("Usage: ");
	  Debug.WriteLine("   java ReadDigPot ADAPTER_PORT\r\n");
	  Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
	  Debug.WriteLine("adapter you would like to use and the port you would like");
	  Debug.WriteLine("to use, for example: ");
	  Debug.WriteLine("   java ReadDigPot {DS1410E}_LPT1\r\n");
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

		 bool isPotContainer = false;
		 PotentiometerContainer pc = null;

		 try
		 {
			pc = (PotentiometerContainer) owc;
			isPotContainer = true;
		 }
		 catch (InvalidCastException)
		 {
			pc = null;
			isPotContainer = false; //just to reiterate
		 }

		 if (isPotContainer)
		 {
			Debug.WriteLine("= This device is a " + owc.Name);
			Debug.WriteLine("= Also known as a " + owc.AlternateNames);
			Debug.WriteLine("=");
			Debug.WriteLine("= It is a Potentiometer Container");

			byte[] state = pc.readDevice();
			bool charge = pc.isChargePumpOn(state);
			int wiper = pc.getCurrentWiperNumber(state);
			int position = pc.WiperPosition;
			bool linear = pc.isLinear(state);
			int numPots = pc.numberOfPotentiometers(state);
			int numSettings = pc.numberOfWiperSettings(state);
			int resistance = pc.potentiometerResistance(state);

			Debug.WriteLine("=");
			Debug.WriteLine("= Charge pump is " + (charge ? "ON" : "OFF"));
			Debug.WriteLine("= This device has " + numPots + " potentiometer" + ((numPots > 1) ? "s" : ""));
			Debug.WriteLine("= This device has a " + (linear ? "linear" : "logarithmic") + " potentiometer");
			Debug.WriteLine("= It uses " + numSettings + " potentiometer wiper settings");
			Debug.WriteLine("= The potentiometer resistance is " + resistance + " kOhms");
			Debug.WriteLine("= CURRENT WIPER NUMBER  : " + wiper);
			Debug.WriteLine("= CURRENT WIPER POSITION: " + position);
			Debug.WriteLine("= Trying to toggle the charge pump...");
			pc.setChargePump(!charge, state);
			pc.writeDevice(state);

			state = pc.readDevice();

			if (charge == pc.isChargePumpOn(state))
			{
			   Debug.WriteLine("= Could not toggle charge pump.  Must have external power supplied.");
			}
			else
			{
			   Debug.WriteLine("= Toggled charge pump successfully");
			}

			//int newwiper = (int)((DateTime.Now.Ticks & 0x0ff00) >> 8);
			//pc.WiperPosition = newwiper;
			//Debug.WriteLine("= Setting wiper position to " + (newwiper));
			Debug.WriteLine("= CURRENT WIPER POSITION: " + pc.WiperPosition);
		 }
		 else
		 {
			Debug.WriteLine("= This device is not a potentiometer device.");
			Debug.WriteLine("=");
			Debug.WriteLine("=");
		 }

		 next = access.findNextDevice();
	  }
   }
}
