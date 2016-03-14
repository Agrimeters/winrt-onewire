﻿using System;
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

using IOHelper = com.dalsemi.onewire.utils.IOHelper;
using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.sha;
using OneWireContainer18 = com.dalsemi.onewire.container.OneWireContainer18;
using OneWireContainer33 = com.dalsemi.onewire.container.OneWireContainer33;
using com.dalsemi.onewire.utils;


public class initrov33
{
   internal static byte[] page0 = new byte[] { 0x0F, 0xAA, 0x00, 0x80, 0x03, 0x00, 0x00, 0x00, 0x43, 0x4F, 0x50, 0x52, 0x00, 0x01, 0x01, 0x00, 0x74, 0x9C, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

   /// <summary>
   /// Method printUsageString
   /// 
   /// 
   /// </summary>
   public static void printUsageString()
   {
	  IOHelper.writeLine("SHA iButton C# Demo Transaction Program - 1961S User Initialization.\r\n");
	  IOHelper.writeLine("Usage: ");
	  IOHelper.writeLine("   java initrov [-pSHA_PROPERTIES_PATH]\r\n");
	  IOHelper.writeLine();
	  IOHelper.writeLine("If you don't specify a path for the sha.properties file, the ");
	  IOHelper.writeLine("current directory and the java lib directory are searched. ");
	  IOHelper.writeLine();
	  IOHelper.writeLine("Here are examples: ");
	  IOHelper.writeLine("   java initrov");
	  IOHelper.writeLine("   java initrov -p sha.properties");
	  IOHelper.writeLine("   java initrov -p\\java\\lib\\sha.properties");
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
   public static void Main(string[] args)
   {
	  //coprocessor
	  long coprID = 0;

	  // ------------------------------------------------------------
	  // Check for valid path to sha.properties file on the cmd line.
	  // ------------------------------------------------------------
	  for (int i = 0; i < args.Length; i++)
	  {
		 string arg = args[i].ToUpper();
		 if (arg.IndexOf("-P", StringComparison.Ordinal) == 0)
		 {
			string sha_props_path;
			if (arg.Length == 2)
			{
			   sha_props_path = args[++i];
			}
			else
			{
			   sha_props_path = arg.Substring(2);
			}

			// attempt to open the sha.properties file
			try
			{
			   System.IO.FileStream prop_file = new System.IO.FileStream(sha_props_path + "sha.properties", System.IO.FileMode.Open, System.IO.FileAccess.Read);
			   sha_properties = new Properties();
			   sha_properties.load(prop_file);
			}
			catch (Exception)
			{
			   sha_properties = null;
			}
		 }
		 else
		 {
			printUsageString();
			return;
		 }
	  }


	  // ------------------------------------------------------------
	  // Instantiate coprocessor containers
	  // ------------------------------------------------------------
	  SHAiButtonCopr copr = null;
	  OneWireContainer18 copr18 = new OneWireContainer18();
	  copr18.setSpeed(DSPortAdapter.SPEED_REGULAR, false);
	  copr18.SpeedCheck = false;

	  // ------------------------------------------------------------
	  // Setup the adapter for the coprocessor
	  // ------------------------------------------------------------
	  DSPortAdapter coprAdapter = null;
	  string coprAdapterName = null, coprPort = null;
	  try
	  {
		 coprAdapterName = getProperty("copr.adapter", "{DS9097U}");
		 coprPort = getProperty("copr.port", "COM1");

		 if (string.ReferenceEquals(coprPort, null) || string.ReferenceEquals(coprAdapterName, null))
		 {
			coprAdapter = OneWireAccessProvider.DefaultAdapter;
		 }
		 else
		 {
			coprAdapter = OneWireAccessProvider.getAdapter(coprAdapterName, coprPort);
		 }

		 IOHelper.writeLine("Coprocessor adapter loaded, adapter: " + coprAdapter.AdapterName + " port: " + coprAdapter.PortName);

		 coprAdapter.adapterDetected();
		 coprAdapter.targetFamily(0x18);
		 coprAdapter.beginExclusive(true);
		 coprAdapter.setSearchAllDevices();
		 coprAdapter.reset();
	  }
	  catch (Exception e)
	  {
		 IOHelper.writeLine("Error initializing coprocessor adapter");
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
		 return;
	  }

	  // ------------------------------------------------------------
	  // Find the coprocessor
	  // ------------------------------------------------------------
	  if (getPropertyBoolean("copr.simulated.isSimulated", false))
	  {
		 string coprVMfilename = getProperty("copr.simulated.filename");
		 // ---------------------------------------------------------
		 // Load emulated coprocessor
		 // ---------------------------------------------------------
		 try
		 {
			copr = new SHAiButtonCoprVM(coprVMfilename);
		 }
		 catch (Exception e)
		 {
			IOHelper.writeLine("Invalid Coprocessor Data File");
			Debug.WriteLine(e.ToString());
			Debug.Write(e.StackTrace);
			return;
		 }
	  }
	  else
	  {
		 // ---------------------------------------------------------
		 // Get the name of the coprocessor service file
		 // ---------------------------------------------------------
		 string filename = getProperty("copr.filename","COPR.0");

		 // ---------------------------------------------------------
		 // Check for hardcoded coprocessor address
		 // ---------------------------------------------------------
		 byte[] coprAddress = getPropertyBytes("copr.address",null);
		 long lookupID = 0;
		 if (coprAddress != null)
		 {
			lookupID = Address.toLong(coprAddress);

			IOHelper.write("Looking for coprocessor: ");
			IOHelper.writeLineHex(lookupID);
		 }

		 // ---------------------------------------------------------
		 // Find hardware coprocessor
		 // ---------------------------------------------------------
		 try
		 {
			bool next = coprAdapter.findFirstDevice();
			while (copr == null && next)
			{
			   try
			   {
				  long tmpCoprID = coprAdapter.AddressAsLong;
				  if (coprAddress == null || tmpCoprID == lookupID)
				  {
					 IOHelper.write("Loading coprocessor file: " + filename + " from device: ");
					 IOHelper.writeLineHex(tmpCoprID);

					 copr18.setupContainer(coprAdapter, tmpCoprID);
					 copr = new SHAiButtonCopr(copr18, filename);

					 //save coprocessor ID
					 coprID = tmpCoprID;
				  }
			   }
			   catch (Exception e)
			   {
				  IOHelper.writeLine(e);
			   }

			   next = coprAdapter.findNextDevice();
			}
		 }
		 catch (Exception)
		 {
			 ;
		 }

	  }

	  if (copr == null)
	  {
		 IOHelper.writeLine("No Coprocessor found!");
		 return;
	  }

	  IOHelper.writeLine(copr);
	  IOHelper.writeLine();


	  // ------------------------------------------------------------
	  // Create the SHADebit transaction types
	  // ------------------------------------------------------------
	  //stores DS1963S transaction data
	  SHATransaction trans = null;
	  string transType = getProperty("ds1961s.transaction.type","Signed");
	  if (transType.ToLower().IndexOf("unsigned", StringComparison.Ordinal) >= 0)
	  {
		 trans = new SHADebitUnsigned(copr,10000,50);
	  }
	  else
	  {
		 trans = new SHADebit(copr,10000,50);
	  }

	  // ------------------------------------------------------------
	  // Create the User Buttons objects
	  // ------------------------------------------------------------
	  //holds DS1963S user buttons
	  OneWireContainer33 owc33 = new OneWireContainer33();
	  owc33.setSpeed(DSPortAdapter.SPEED_REGULAR, false);
	  //owc33.setSpeedCheck(false);

	  // ------------------------------------------------------------
	  // Get the adapter for the user
	  // ------------------------------------------------------------
	  DSPortAdapter adapter = null;
	  string userAdapterName = null, userPort = null;
	  try
	  {
		 userAdapterName = getProperty("user.adapter","{DS9097U}");
		 userPort = getProperty("user.port","COM2");

		 if (string.ReferenceEquals(userPort, null) || string.ReferenceEquals(userAdapterName, null))
		 {
			if (!string.ReferenceEquals(coprAdapterName, null) && !string.ReferenceEquals(coprPort, null))
			{
			   adapter = OneWireAccessProvider.DefaultAdapter;
			}
			else
			{
			   adapter = coprAdapter;
			}
		 }
		 else if (userAdapterName.Equals(coprAdapterName) && userPort.Equals(coprPort))
		 {
			adapter = coprAdapter;
		 }
		 else
		 {
			adapter = OneWireAccessProvider.getAdapter(userAdapterName, userPort);
		 }

		 IOHelper.writeLine("User adapter loaded, adapter: " + adapter.AdapterName + " port: " + adapter.PortName);

		 byte[] families = new byte[]{0x33,unchecked((byte)0xB3)};

		 adapter.adapterDetected();
		 adapter.targetFamily(families);
		 adapter.beginExclusive(false);
		 adapter.setSearchAllDevices();
		 adapter.reset();
	  }
	  catch (Exception e)
	  {
		 IOHelper.writeLine("Error initializing user adapter.");
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
		 return;
	  }

	  // ---------------------------------------------------------------
	  // Search for the button
	  // ---------------------------------------------------------------
	  try
	  {
		 long tmpID = -1;
		 bool next = adapter.findFirstDevice();
		 for (; tmpID == -1 && next; next = adapter.findNextDevice())
		 {
			tmpID = adapter.AddressAsLong;
			if (tmpID == coprID)
			{
			   tmpID = -1;
			}
			else
			{
			   owc33.setupContainer(adapter, tmpID);
			}
		 }

		 if (tmpID == -1)
		 {
			IOHelper.writeLine("No buttons found!");
			return;
		 }
	  }
	  catch (Exception)
	  {
		 IOHelper.writeLine("Adapter error while searching.");
		 return;
	  }

	  IOHelper.write("Setting up user button: ");
	  IOHelper.writeBytesHex(owc33.Address);
	  IOHelper.writeLine();

	  IOHelper.writeLine("How would you like to enter the authentication secret (unlimited bytes)? ");
	  byte[] auth_secret = getBytes(0);
	  IOHelper.writeBytes(auth_secret);
	  IOHelper.writeLine();

	  auth_secret = copr.reformatFor1961S(auth_secret);
	  IOHelper.writeLine("Reformatted for compatibility with 1961S buttons");
	  IOHelper.writeBytes(auth_secret);
	  IOHelper.writeLine("");

	  IOHelper.writeLine("Initial Balance in Cents? ");
	  int initialBalance = IOHelper.readInt(100);
	  trans.setParameter(SHADebit.INITIAL_AMOUNT, initialBalance);

	  SHAiButtonUser user = new SHAiButtonUser33(copr, owc33, true, auth_secret);
	  if (trans.setupTransactionData(user))
	  {
		 IOHelper.writeLine("Transaction data installation succeeded");
	  }
	  else
	  {
		 IOHelper.writeLine("Failed to initialize transaction data");
	  }

	  IOHelper.writeLine(user);
   }

   public static byte[] getBytes(int cnt)
   {
	  IOHelper.writeLine("   1 HEX");
	  IOHelper.writeLine("   2 ASCII");
	  Debug.Write("  ? ");
	  int choice = IOHelper.readInt(2);

	  if (choice == 1)
	  {
		 return IOHelper.readBytesHex(cnt,0x00);
	  }
	  else
	  {
		 return IOHelper.readBytesAsc(cnt,0x20);
	  }
   }

   internal static Properties sha_properties = null;
   /// <summary>
   /// Gets the specfied onewire property.
   /// Looks for the property in the following locations:
   /// <para>
   /// <ul>
   /// <li> In System.properties
   /// <li> In onewire.properties file in current directory
   ///      or < java.home >/lib/ (Desktop) or /etc/ (TINI)
   /// <li> 'smart' default if property is 'onewire.adapter.default'
   ///      or 'onewire.port.default'
   /// </ul>
   /// 
   /// </para>
   /// </summary>
   /// <param name="propName"> string name of the property to read
   /// </param>
   /// <returns>  <code>String</code> representing the property value or <code>null</code> if
   ///          it could not be found (<code>onewire.adapter.default</code> and
   ///          <code>onewire.port.default</code> may
   ///          return a 'smart' default even if property not present) </returns>
   public static string getProperty(string propName)
   {
	  // first, try system properties
	  try
	  {
		 string ret_str = System.getProperty(propName, null);
		 if (!string.ReferenceEquals(ret_str, null))
		 {
			return ret_str;
		 }
	  }
	  catch (Exception)
	  {
		  ;
	  }

	  // if defaults not found then try sha.properties file
	  if (sha_properties == null)
	  {
		 //try to load sha_propreties file
		 System.IO.FileStream prop_file = null;

		 // loop to attempt to open the sha.properties file in two locations
		 // .\sha.properties or <java.home>\lib\sha.properties
		 string path = "";

		 for (int i = 0; i <= 1; i++)
		 {

			// attempt to open the sha.properties file
			try
			{
			   prop_file = new System.IO.FileStream(path + "sha.properties", System.IO.FileMode.Open, System.IO.FileAccess.Read);
			   // attempt to read the onewire.properties
			   try
			   {
				  sha_properties = new Properties();
				  sha_properties.load(prop_file);
			   }
			   catch (Exception)
			   {
				  //so we remember that it failed
				  sha_properties = null;
			   }
			}
			catch (IOException)
			{
			   prop_file = null;
			}

			// check to see if we now have the properties loaded
			if (sha_properties != null)
			{
			   break;
			}

			// try the second path
			path = System.getProperty("java.home") + File.separator + "lib" + File.separator;
		 }
	  }

	  if (sha_properties == null)
	  {
		 IOHelper.writeLine("Can't find sha.properties file");
		 return null;
	  }
	  else
	  {
		 object ret = sha_properties.get(propName);
		 if (ret == null)
		 {
			return null;
		 }
		 else
		 {
			return ret.ToString();
		 }
	  }
   }

   public static string getProperty(string propName, string defValue)
   {
	  string ret = getProperty(propName);
	  return (string.ReferenceEquals(ret, null)) ? defValue : ret;
   }

   public static bool getPropertyBoolean(string propName, bool defValue)
   {
	  string strValue = getProperty(propName);
	  if (!string.ReferenceEquals(strValue, null))
	  {
		 defValue = Convert.ToBoolean(strValue);
	  }
	  return defValue;
   }


   public static byte[] getPropertyBytes(string propName, byte[] defValue)
   {
	  string strValue = getProperty(propName);
	  if (!string.ReferenceEquals(strValue, null))
	  {
		 //only supports up to 128 bytes of data
		 byte[] tmp = new byte[128];

		 //split the string on commas and spaces
		 StringTokenizer strtok = new StringTokenizer(strValue,", ");

		 //how many bytes we got
		 int i = 0;
		 while (strtok.hasMoreElements())
		 {
			//this string could have more than one byte in it
			string multiByteStr = strtok.nextToken();
			int strLen = multiByteStr.Length;

			for (int j = 0; j < strLen; j += 2)
			{
			   //get just two nibbles at a time
			   string byteStr = multiByteStr.Substring(j, Math.Min(2, strLen));

			   long lng = 0;
			   try
			   {
				  //parse the two nibbles into a byte
				  lng = long.Parse(byteStr, 16);
			   }
			   catch (System.FormatException nfe)
			   {
				  Debug.WriteLine(nfe.ToString());
				  Debug.Write(nfe.StackTrace);

				  //no mercy!
				  return defValue;
			   }

			   //store the byte and increment the counter
			   if (i < tmp.Length)
			   {
				  tmp[i++] = unchecked((byte)(lng & 0x0FF));
			   }
			}
		 }
		 if (i > 0)
		 {
			byte[] retVal = new byte[i];
			Array.Copy(tmp, 0, retVal, 0, i);
			return retVal;
		 }
	  }
	  return defValue;
   }

}
