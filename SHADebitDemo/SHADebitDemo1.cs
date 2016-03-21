using System;
using System.Diagnostics;
using System.Reflection;

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

using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.sha;
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;
using com.dalsemi.onewire;

/// <summary>
/// Title:        SHA Debit Demo
/// Description:
/// Copyright:    Copyright (c) 2001
/// Company:      Maxim/Dallas Semiconductor
/// @author SKH
/// @version 1.0
/// </summary>
public class SHADebitDemo1
{

   /// <summary>
   /// turns on DEBUG messages </summary>
   internal const bool DEBUG = true;

    private static Properties sha_properties = null;

    /// <summary>
    /// Method printUsageString
    /// 
    /// 
    /// </summary>
    public static void printUsageString()
   {
	  IOHelper.writeLine("SHA iButton Java Demo Transaction Program.\r\n");
	  IOHelper.writeLine("Usage: ");
	  IOHelper.writeLine("   java SHADebitDemo [-pSHA_PROPERTIES_PATH]\r\n");
	  IOHelper.writeLine();
	  IOHelper.writeLine("If you don't specify a path for the sha.properties file, the ");
	  IOHelper.writeLine("current directory and the java lib directory are searched. ");
	  IOHelper.writeLine();
	  IOHelper.writeLine("Here are examples: ");
	  IOHelper.writeLine("   java SHADebitDemo");
	  IOHelper.writeLine("   java SHADebitDemo -p sha.properties");
	  IOHelper.writeLine("   java SHADebitDemo -p\\java\\lib\\sha.properties");
   }

   public static void Main0(string[] args)
   {
	  //coprocessor
	  long coprID = 0;
      long lookupID = 0;
      bool next;

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
            sha_properties = new Properties();
            if(!sha_properties.loadLocalFile("sha.properties"))
            {
               Debug.WriteLine("loading default sha.properties!");
               Assembly asm = typeof(SHADebitDemo.MainPage).GetTypeInfo().Assembly;
               sha_properties.loadResourceFile(asm, "SHADebitDemo.sha.properties");
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
	  copr18.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
	  copr18.SpeedCheck = false;

	  // ------------------------------------------------------------
	  // Setup the adapter for the coprocessor
	  // ------------------------------------------------------------
	  DSPortAdapter coprAdapter = null;
	  string coprAdapterName = null, coprPort = null;
	  try
	  {
		 coprAdapterName = sha_properties.getProperty("copr.adapter");
		 coprPort = sha_properties.getProperty("copr.port");

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
		 coprAdapter.reset();
		 coprAdapter.setSearchAllDevices();
		 coprAdapter.reset();
		 coprAdapter.putByte(0x3c);
		 coprAdapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
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
	  if (sha_properties.getPropertyBoolean("copr.simulated.isSimulated", false))
	  {
		 string coprVMfilename = sha_properties.getProperty("copr.simulated.filename");
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
		 string filename = sha_properties.getProperty("copr.filename", "COPR.0");

		 // ---------------------------------------------------------
		 // Check for hardcoded coprocessor address
		 // ---------------------------------------------------------
		 byte[] coprAddress = sha_properties.getPropertyBytes("copr.address", null);
		 lookupID = 0;
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
			next = coprAdapter.findFirstDevice();
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
	  //stores DS1963S transaction type
	  SHATransaction debit18 = null;

	  string transType18 = sha_properties.getProperty("transaction.type", "signed");
	  transType18 = transType18.ToLower();
	  if (transType18.Equals("signed"))
	  {
		 debit18 = new SHADebit(copr,10000,50);
	  }
	  else
	  {
		 debit18 = new SHADebitUnsigned(copr,10000,50);
	  }

	  //stores DS1961S transaction type
	  SHATransaction debit33 = null;

	  string transType33 = sha_properties.getProperty("ds1961s.transaction.type", "unsigned");
	  transType33 = transType33.ToLower();
	  if (transType33.Equals(transType18))
	  {
		 debit33 = debit18;
	  }
	  else if (transType33.IndexOf("unsigned", StringComparison.Ordinal) >= 0)
	  {
		 //everything is seemless if you use the authentication secret
		 //as the button's secret.
		 debit33 = new SHADebitUnsigned(copr,10000,50);
	  }
	  else
	  {
		 //if the 1961S uses the authentication secret,
		 //you'll need another button for generating the
		 //write authorization MAC, but you can share the
		 //1963S debit code for signed data.
		 debit33 = new SHADebit(copr,10000,50);
	  }

	  //Transaction super class, swap variable
	  SHATransaction trans = null;

	  // ------------------------------------------------------------
	  // Get the write-authorization coprocessor for ds1961S
	  // ------------------------------------------------------------
	  //first get the write authorization adapter
	  DSPortAdapter authAdapter = null;
	  string authAdapterName = null, authPort = null;
	  try
	  {
		 authAdapterName = sha_properties.getProperty("ds1961s.copr.adapter");
		 authPort = sha_properties.getProperty("ds1961s.copr.port");

		 if (string.ReferenceEquals(authAdapterName, null) || string.ReferenceEquals(authAdapterName, null))
		 {
			if (!string.ReferenceEquals(coprAdapterName, null) && !string.ReferenceEquals(coprPort, null))
			{
			   authAdapter = OneWireAccessProvider.DefaultAdapter;
			}
			else
			{
			   authAdapter = coprAdapter;
			}
		 }
		 else if (coprAdapterName.Equals(authAdapterName) && coprPort.Equals(authPort))
		 {
			authAdapter = coprAdapter;
		 }
		 else
		 {
			authAdapter = OneWireAccessProvider.getAdapter(authAdapterName, authPort);
		 }

		 IOHelper.writeLine("Write-Authorization adapter loaded, adapter: " + authAdapter.AdapterName + " port: " + authAdapter.PortName);

		 byte[] families = new byte[]{0x18};

		 authAdapter.adapterDetected();
		 authAdapter.targetFamily(families);
		 authAdapter.beginExclusive(false);
		 authAdapter.reset();
		 authAdapter.setSearchAllDevices();
		 authAdapter.reset();
		 authAdapter.putByte(0x3c);
		 authAdapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
	  }
	  catch (Exception e)
	  {
		 IOHelper.writeLine("Error initializing write-authorization adapter.");
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
		 return;
	  }

	  //now find the coprocessor
	  SHAiButtonCopr authCopr = null;

	  // -----------------------------------------------------------
	  // Check for hardcoded write-authorizaton coprocessor address
	  // -----------------------------------------------------------
	  byte[] authCoprAddress = sha_properties.getPropertyBytes("ds1961s.copr.address",null);
	  lookupID = 0;
	  if (authCoprAddress != null)
	  {
		 lookupID = Address.toLong(authCoprAddress);

		 IOHelper.write("Looking for coprocessor: ");
		 IOHelper.writeLineHex(lookupID);
	  }
	  if (lookupID == coprID)
	  {
		 //it's the same as the standard coprocessor.
		 //valid only if we're not signing the data
		 authCopr = copr;
	  }
	  else
	  {
		 // ---------------------------------------------------------
		 // Find write-authorization coprocessor
		 // ---------------------------------------------------------
		 try
		 {
			string filename = sha_properties.getProperty("ds1961s.copr.filename","COPR.1");
			OneWireContainer18 auth18 = new OneWireContainer18();
			auth18.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
			auth18.SpeedCheck = false;

			next = authAdapter.findFirstDevice();
			while (authCopr == null && next)
			{
			   try
			   {
				  long tmpAuthID = authAdapter.AddressAsLong;
				  if (authCoprAddress == null || tmpAuthID == lookupID)
				  {
					 IOHelper.write("Loading coprocessor file: " + filename + " from device: ");
					 IOHelper.writeLineHex(tmpAuthID);

					 auth18.setupContainer(authAdapter, tmpAuthID);
					 authCopr = new SHAiButtonCopr(auth18, filename);
				  }
			   }
			   catch (Exception e)
			   {
				  IOHelper.writeLine(e);
			   }

			   next = authAdapter.findNextDevice();
			}
		 }
		 catch (Exception e)
		 {
			IOHelper.writeLine(e);
		 }
		 if (authCopr == null)
		 {
			IOHelper.writeLine("no write-authorization coprocessor found");
			if (copr is SHAiButtonCoprVM)
			{
			   authCopr = copr;
			   IOHelper.writeLine("Re-using SHAiButtonCoprVM");
			}
		 }
	  }

	  IOHelper.writeLine(authCopr);
	  IOHelper.writeLine();

	  // ------------------------------------------------------------
	  // Create the User Buttons objects
	  // ------------------------------------------------------------
	  //holds DS1963S user buttons
	  SHAiButtonUser18 user18 = new SHAiButtonUser18(copr);
	  OneWireContainer18 owc18 = new OneWireContainer18();
	  owc18.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
	  owc18.SpeedCheck = false;

	  //holds DS1961S user buttons
	  SHAiButtonUser33 user33 = new SHAiButtonUser33(copr, authCopr);
	  OneWireContainer33 owc33 = new OneWireContainer33();
	  owc33.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
	  //owc33.setSpeedCheck(false);

	  //Holds generic user type, swap variable
	  SHAiButtonUser user = null;

	  // ------------------------------------------------------------
	  // Get the adapter for the user
	  // ------------------------------------------------------------
	  DSPortAdapter adapter = null;
	  string userAdapterName = null, userPort = null;
	  try
	  {
		 userAdapterName = sha_properties.getProperty("user.adapter");
		 userPort = sha_properties.getProperty("user.port");

		 if (string.ReferenceEquals(userPort, null) || string.ReferenceEquals(userAdapterName, null))
		 {
			if (!string.ReferenceEquals(coprAdapterName, null) && !string.ReferenceEquals(coprPort, null))
			{
			   if (!string.ReferenceEquals(authAdapterName, null) && !string.ReferenceEquals(authPort, null))
			   {
				  adapter = OneWireAccessProvider.DefaultAdapter;
			   }
			   else
			   {
				  adapter = authAdapter;
			   }
			}
			else
			{
			   adapter = coprAdapter;
			}
		 }
		 else if (userAdapterName.Equals(authAdapterName) && userPort.Equals(authPort))
		 {
			adapter = authAdapter;
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

		 byte[] families = new byte[]{0x18,0x33};
		 families = sha_properties.getPropertyBytes("transaction.familyCodes", families);
		 IOHelper.write("Supporting the following family codes: ");
		 IOHelper.writeBytesHex(" ", families, 0, families.Length);

		 adapter.adapterDetected();
		 adapter.targetFamily(families);
		 adapter.beginExclusive(true);
		 adapter.reset();
		 adapter.setSearchAllDevices();
		 adapter.reset();
		 adapter.putByte(0x3c);
		 adapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
	  }
	  catch (Exception e)
	  {
		 IOHelper.writeLine("Error initializing user adapter.");
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
		 return;
	  }

	  //timing variables
	  long t0 = 0, t1 = 0, t2 = 0, t3 = 0, t4 = 0, t5 = 0;

	  //address of current device
	  byte[] address = new byte[8];

	  //result of findNextDevice/findFirstDevice
	  next = false;

	  //holds list of known buttons
	  long[] buttons = new long[16];
	  //count of how many buttons are in buttons array
	  int index = 0;

	  //temporary id representing current button
	  long tmpID = -1;
	  //array of buttons looked at during this search
	  long[] temp = new long[16];
	  //count of how many buttons in temp array
	  int cIndex = 0;

	  //flag indiciating whether or not temp array represents
	  //the complete list of buttons on the network.
	  bool wholeList = false;

	  Debug.WriteLine("");
	  Debug.WriteLine("");
	  Debug.WriteLine("**********************************************************");
	  Debug.WriteLine("   Beginning The Main Application Loop (Search & Debit)");
	  Debug.WriteLine("           Press Enter to Exit Application");
	  Debug.WriteLine("**********************************************************");
	  Debug.WriteLine("");

	  //application infinite loop
	  for (bool applicationFinished = false; !applicationFinished;)
	  {
		 try
		 {
			if (coprAdapter != adapter)
			{
			   //in case coprocessor communication got hosed, make sure
			   //the coprocessor adapter is in overdrive
			   coprAdapter.Speed = DSPortAdapter.SPEED_REGULAR;
			   coprAdapter.reset();
			   coprAdapter.putByte(0x3c); //overdrive skip rom
			   coprAdapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
			}
			if (authAdapter != coprAdapter && authAdapter != adapter)
			{
			   //in case coprocessor communication with the write-
			   //authorization coprocessor got hosed, make sure
			   //the coprocessor adapter is in overdrive
			   authAdapter.Speed = DSPortAdapter.SPEED_REGULAR;
			   authAdapter.reset();
			   authAdapter.putByte(0x3c); //overdrive skip rom
			   authAdapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
			}
		 }
		 catch (Exception)
		 {
			 ;
		 }

		 // ---------------------------------------------------------
		 // Search for new buttons
		 // ---------------------------------------------------------
		 bool buttonSearch = true;

		 //Button search loop, waits forever until new button appears.
		 while (buttonSearch && !applicationFinished)
		 {
			wholeList = false;
            t0 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;
			try
			{
			   //Go into overdrive
			   adapter.Speed = DSPortAdapter.SPEED_REGULAR;
			   adapter.reset();
			   adapter.putByte(0x3c); //overdrive skip rom
			   adapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;

			   // Begin search for new buttons
			   if (!next)
			   {
				  wholeList = true;
				  next = adapter.findFirstDevice();
			   }

			   for (tmpID = -1, cIndex = 0; next && (tmpID == -1); next = adapter.findNextDevice())
			   {
				  tmpID = adapter.AddressAsLong;
				  if (tmpID != coprID)
				  {
					 temp[cIndex++] = tmpID;
					 for (int i = 0; i < index; i++)
					 {
						if (buttons[i] == tmpID)
						{ //been here all along
						   tmpID = -1;
						   i = index;
						}
					 }

					 if (tmpID != -1)
					 {
						//populate address array
						adapter.getAddress(address);
					 }
				  }
				  else
				  {
					 tmpID = -1;
				  }
			   }

			   //if we found a new button
			   if (tmpID != -1)
			   {
				  //add it to the main list
				  buttons[index++] = tmpID;

				  //quite searching, we got one!
				  buttonSearch = false;
			   }
			   else if (wholeList)
			   {
				  //went through whole list with nothing new
				  //update the main list of buttons
				  buttons = temp;
				  index = cIndex;

				  //if user presses the enter key, we'll quit and clean up nicely
//TODO				  applicationFinished = (System.in.available() > 0);
			   }
			}
			catch (Exception e)
			{
			   if (DEBUG)
			   {
				  IOHelper.writeLine("adapter hiccup while searching");
				  Debug.WriteLine(e.ToString());
				  Debug.Write(e.StackTrace);
			   }
			}
		 }

		 if (applicationFinished)
		 {
			continue;
		 }

		 // ---------------------------------------------------------
		 // Perform the transaction
		 // ---------------------------------------------------------
		 try
		 {
            t1 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;

			//de-ref the user
			user = null;

			//check for button family code
			if ((tmpID & 0x18) == (byte)0x18)
			{
			   //get transactions for ds1963s
			   trans = debit18;
			   owc18.setupContainer(adapter, address);
			   if (user18.setiButton18(owc18))
			   {
				  user = user18;
			   }
			}
			else if ((tmpID & 0x33) == (byte)0x33)
			{
			   //get transactions for 1961S
			   trans = debit33;
			   owc33.setupContainer(adapter, address);
			   if (user33.setiButton33(owc33))
			   {
				  user = user33;
			   }
			}

			if (user != null)
			{
			   Debug.WriteLine("");
			   Debug.WriteLine(user.ToString());
               t2 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;
			   if (trans.verifyUser(user))
			   {
                  t3 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;
				  if (trans.verifyTransactionData(user))
				  {
                     t4 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;
					 if (!trans.executeTransaction(user, true))
					 {
						Debug.WriteLine("Execute Transaction Failed");
					 }
                     t5 = Stopwatch.GetTimestamp() * TimeSpan.TicksPerMillisecond;

					 Debug.WriteLine("  Debit Amount: $00.50");
					 Debug.Write("User's balance: $");
					 int balance = trans.getParameter(SHADebit.USER_BALANCE);
					 Debug.WriteLine(com.dalsemi.onewire.utils.Convert.ToString(balance / 100d, 2));

				  }
				  else
				  {
					 Debug.WriteLine("Verify Transaction Data Failed");
				  }
			   }
			   else
			   {
				  Debug.WriteLine("Verify User Authentication Failed");
			   }
			}
			else
			{
			   Debug.WriteLine("Not a SHA user of this service");
			}

			Debug.Write("Total time: ");
			Debug.WriteLine(t5 - t0);
			Debug.Write("Executing transaction took: ");
			Debug.WriteLine(t5 - t4);
			Debug.Write("Verifying data took: ");
			Debug.WriteLine(t4 - t3);
			Debug.Write("Verifying user took: ");
			Debug.WriteLine(t3 - t2);
			Debug.Write("Loading user data took: ");
			Debug.WriteLine(t2 - t1);
			Debug.Write("Finding user took: ");
			Debug.WriteLine(t1 - t0);

			//report all errors
			if (trans.LastError != 0)
			{
			   IOHelper.writeLine("Last Error Code: ");
			   IOHelper.writeLine(trans.LastError);
			   if (trans.LastError == SHATransaction.COPROCESSOR_FAILURE)
			   {
				  IOHelper.writeLine("COPR Error Code: ");
				  IOHelper.writeLine(copr.LastError);
			   }
			}
		 }
		 catch (Exception e)
		 {
			Debug.WriteLine("Transaction failed!");
			Debug.WriteLine(e.ToString());
			Debug.Write(e.StackTrace);
		 }
	  }

	  // --------------------------------------------------------------
	  // Some Friendly Cleanup
	  // --------------------------------------------------------------
	  adapter.endExclusive();
	  coprAdapter.endExclusive();
	  authAdapter.endExclusive();
   }
}