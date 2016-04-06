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
using com.dalsemi.onewire.application.sha;
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;
using System;
using System.Diagnostics;
using System.Reflection;

public class initrov
{
    internal static byte[] page0 = new byte[] { 0x0F, 0xAA, 0x00, 0x80, 0x03, 0x00, 0x00, 0x00, 0x43, 0x4F, 0x50, 0x52, 0x00, 0x01, 0x01, 0x00, 0x74, 0x9C, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

    /// <summary>
    /// Method printUsageString
    ///
    ///
    /// </summary>
    public static void printUsageString()
    {
        IOHelper.writeLine("SHA iButton C# Demo Transaction Program - 1963S User Initialization.\r\n");
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
    public static void Main2(string[] args)
    {
        //coprocessor
        long coprID = 0;

        // attempt to open the sha.properties file
        sha_properties = new Properties();
        if (!sha_properties.loadLocalFile("sha.properties"))
        {
            Debug.WriteLine("loading default sha.properties!");
            Assembly asm = typeof(SHADebitDemo.MainPage).GetTypeInfo().Assembly;
            sha_properties.loadResourceFile(asm, "SHADebitDemo.sha.properties");
        }

        // ------------------------------------------------------------
        // Instantiate coprocessor containers
        // ------------------------------------------------------------
        SHAiButtonCopr copr = null;
        OneWireContainer18 copr18 = new OneWireContainer18();
        //TODO copr18.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
        copr18.SpeedCheck = false;

        // ------------------------------------------------------------
        // Setup the adapter for the coprocessor
        // ------------------------------------------------------------
        DSPortAdapter coprAdapter = null;
        string coprAdapterName = null, coprPort = null;
        try
        {
            coprAdapterName = OneWireAccessProvider.getProperty("copr.adapter");
            coprPort = OneWireAccessProvider.getProperty("copr.port");

            if (string.ReferenceEquals(coprPort, null) || string.ReferenceEquals(coprAdapterName, null))
            {
                coprAdapter = OneWireAccessProvider.DefaultAdapter;
            }
            else
            {
                coprAdapter = OneWireAccessProvider.getAdapter(coprAdapterName, coprPort);
            }

            Debug.WriteLine("Coprocessor adapter loaded, adapter: " + coprAdapter.AdapterName + " port: " + coprAdapter.PortName);

            coprAdapter.adapterDetected();
            coprAdapter.targetFamily(0x18);
            coprAdapter.beginExclusive(true);
            coprAdapter.reset();
            coprAdapter.setSearchAllDevices();
            coprAdapter.reset();
            coprAdapter.putByte(0x3c);
            //TODO coprAdapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error initializing coprocessor adapter");
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
                            Debug.WriteLine("Loading coprocessor file: " + filename + " from device: " + tmpCoprID.ToString("X"));
                            //IOHelper.writeLineHex(tmpCoprID);

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

        Debug.WriteLine(copr);
        Debug.WriteLine("");

        // ------------------------------------------------------------
        // Create the SHADebit transaction types
        // ------------------------------------------------------------
        //stores DS1963S transaction data
        SHATransaction trans = null;
        string transType = sha_properties.getProperty("transaction.type", "SignedDebit");
        if (transType.ToLower().Equals("unsigneddebit"))
        {
            trans = new SHADebitUnsigned(copr, 10000, 50);
        }
        else
        {
            trans = new SHADebit(copr, 10000, 50);
        }

        // ------------------------------------------------------------
        // Create the User Buttons objects
        // ------------------------------------------------------------
        //holds DS1963S user buttons
        OneWireContainer18 owc18 = new OneWireContainer18();
        //TODO owc18.setSpeed(DSPortAdapter.SPEED_OVERDRIVE, false);
        owc18.SpeedCheck = false;

        // ------------------------------------------------------------
        // Get the adapter for the user
        // ------------------------------------------------------------
        DSPortAdapter adapter = null;
        string userAdapterName = null, userPort = null;
        try
        {
            userAdapterName = OneWireAccessProvider.getProperty("user.adapter");
            userPort = OneWireAccessProvider.getProperty("user.port");

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

            Debug.WriteLine("User adapter loaded, adapter: " + adapter.AdapterName + " port: " + adapter.PortName);

            byte[] families = new byte[] { 0x18 };

            adapter.adapterDetected();
            adapter.targetFamily(families);
            adapter.beginExclusive(false);
            adapter.reset();
            adapter.setSearchAllDevices();
            adapter.reset();
            adapter.putByte(0x3c);
            //TODO adapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
        }
        catch (Exception e)
        {
            IOHelper.writeLine("Error initializing user adapter.");
            Debug.WriteLine(e.ToString());
            Debug.Write(e.StackTrace);
            return;
        }

        //
        //
        //
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
                    owc18.setupContainer(adapter, tmpID);
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
        IOHelper.writeBytesHex(owc18.Address);

        IOHelper.writeLine("How would you like to enter the authentication secret (unlimited bytes)? ");
        byte[] auth_secret = getBytes(0);
        IOHelper.writeBytes(auth_secret);
        IOHelper.writeLine("");
        if (copr.DS1961Scompatible)
        {
            auth_secret = SHAiButtonCopr.reformatFor1961S(auth_secret);
            IOHelper.writeLine("Reformatted for compatibility with 1961S buttons");
            IOHelper.writeBytes(auth_secret);
            IOHelper.writeLine("");
        }

        IOHelper.writeLine("Initial Balance in Cents? ");
        int initialBalance = IOHelper.readInt(100);
        trans.setParameter(SHADebit.INITIAL_AMOUNT, initialBalance);

        SHAiButtonUser user = new SHAiButtonUser18(copr, owc18, true, auth_secret);
        if (trans.setupTransactionData(user))
        {
            IOHelper.writeLine("Transaction data installation succeeded");
        }
        else
        {
            IOHelper.writeLine("Failed to initialize transaction data");
        }

        IOHelper.writeLine(user.ToString());
    }

    public static byte[] getBytes(int cnt)
    {
        IOHelper.writeLine("   1 HEX");
        IOHelper.writeLine("   2 ASCII");
        Debug.Write("  ? ");
        int choice = IOHelper.readInt(2);

        if (choice == 1)
        {
            return IOHelper.readBytesHex(cnt, 0x00);
        }
        else
        {
            return IOHelper.readBytesAsc(cnt, 0x20);
        }
    }

    internal static Properties sha_properties = null;
}