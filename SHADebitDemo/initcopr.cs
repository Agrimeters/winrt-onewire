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

//using com.dalsemi.onewire.utils;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

public class initcopr
{
    internal static bool EMULATE_COPR = false;
    internal static string SHA_COPR_FILENAME = "sha_copr";

    internal static readonly byte[] DEFAULT_COPR_ROMID = new byte[] { 0x18, 0xFB, 0x13, 0, 0, 0, 0, 0xB2 };

    internal static Properties sha_properties = null;

    /// <summary>
    /// Method printUsageString
    ///
    ///
    /// </summary>
    public static void printUsageString()
    {
        Debug.WriteLine("SHA iButton C# Demo Transaction Program - Copr Initialization.\r\n");
        Debug.WriteLine("Usage: ");
        Debug.WriteLine("   java initcopr [-pSHA_PROPERTIES_PATH]\r\n");
        Debug.WriteLine("");
        Debug.WriteLine("If you don't specify a path for the sha.properties file, the ");
        Debug.WriteLine("current directory and the java lib directory are searched. ");
        Debug.WriteLine("");
        Debug.WriteLine("Here are examples: ");
        Debug.WriteLine("   java initcopr");
        Debug.WriteLine("   java initcopr -p sha.properties");
        Debug.WriteLine("   java initcopr -p\\java\\lib\\sha.properties");
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
        byte[] sign_secret = null;
        byte[] auth_secret = null;

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

        // ---------------------------------------------------------
        // Get the name of the coprocessor service file
        // ---------------------------------------------------------
        string filename = sha_properties.getProperty("copr.filename", "COPR.0");

        bool next = false;
        bool vmSaveSecrets = true;
        string coprVMfilename = null;

        // ------------------------------------------------------------
        // Find the coprocessor
        // ------------------------------------------------------------
        if (sha_properties.getPropertyBoolean("copr.simulated.isSimulated", false))
        {
            coprVMfilename = sha_properties.getProperty("copr.simulated.filename");
            vmSaveSecrets = sha_properties.getPropertyBoolean("copr.simulated.saveSecrets", true);

            // ---------------------------------------------------------
            // Load emulated coprocessor
            // ---------------------------------------------------------
            Debug.WriteLine("Setting up simulated Copressor.");
            Debug.WriteLine("Would you like to emulate another coprocessor? (y)");
            //if (IOHelper.readLine().ToUpper()[0] == 'Y')
            //{
            //    //    OneWireContainer18 ibc = new OneWireContainer18();
            //    ibc.SpeedCheck = false;
            //    try
            //    {
            //        next = coprAdapter.findFirstDevice();
            //        while (next && copr == null)
            //        {
            //            try
            //            {
            //                Debug.WriteLine(coprAdapter.AddressAsLong);
            //                ibc.setupContainer(coprAdapter, coprAdapter.AddressAsLong);
            //                copr = new SHAiButtonCopr(ibc, filename);
            //            }
            //            catch (Exception e)
            //            {
            //                Debug.WriteLine(e.ToString());
            //                Debug.Write(e.StackTrace);
            //            }
            //            next = coprAdapter.findNextDevice();
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        ;
            //    }
            //    if (copr == null)
            //    {
            //        Debug.WriteLine("No coprocessor found to emulate");
            //        return;
            //    }

            //    Debug.WriteLine("");
            //    Debug.WriteLine("Emulating Coprocessor: " + ibc.AddressAsString);
            //    Debug.WriteLine("");

            //    //now that we got all that, we need a signing secret and an authentication secret
            //    Debug.WriteLine("How would you like to enter the signing secret (unlimited bytes)? ");
            //    sign_secret = new byte[] { 0, 0, 0, 0 }; //TODO
            //    IOHelper.writeBytes(sign_secret);

            //    Debug.WriteLine("");
            //    Debug.WriteLine("How would you like to enter the authentication secret (unlimited bytes)? ");
            //    auth_secret = new byte[] { 0, 0, 0, 0 }; //TODO
            //    IOHelper.writeBytes(auth_secret);

            //    Debug.WriteLine("");
            //    if (copr.DS1961Scompatible)
            //    {
            //        //reformat the auth_secret
            //        auth_secret = SHAiButtonCopr.reformatFor1961S(auth_secret);
            //        IOHelper.writeBytes(auth_secret);
            //    }
            //    Debug.WriteLine("");
            //    copr = new SHAiButtonCoprVM(ibc, filename, sign_secret, auth_secret);

            //    ((SHAiButtonCoprVM)copr).save(coprVMfilename, true);

            //    Debug.WriteLine(copr);
            //    return;
            //}
        }
        //else
        {
            // ---------------------------------------------------------
            // Check for hardcoded coprocessor address
            // ---------------------------------------------------------
            byte[] coprAddress = sha_properties.getPropertyBytes("copr.address", null);
            long lookupID = 0, coprID = -1;
            if (coprAddress != null)
            {
                lookupID = com.dalsemi.onewire.utils.Address.toLong(coprAddress);

                Debug.Write("Looking for coprocessor: " + lookupID.ToString("x"));
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
                            Debug.WriteLine("Loading coprocessor file: " + filename + " from device: " + tmpCoprID.ToString("x"));

                            copr18.setupContainer(coprAdapter, tmpCoprID);

                            //save coprocessor ID
                            coprID = tmpCoprID;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }

                    next = coprAdapter.findNextDevice();
                }
            }
            catch (Exception)
            {
                ;
            }

            if (coprID == -1)
            {
                Debug.WriteLine("No Coprocessor found!");
                return;
            }

            Debug.WriteLine("Setting up DS1963S as Coprocessor: " + com.dalsemi.onewire.utils.Address.ToString(copr18.Address));
        }

        // Now that we've got a suitable button for creating a coprocessor,
        // let's ask the user for all the necessary paramters.

        Debug.Write("Enter the name of the coprocessor file (usually 'COPR') : ");
        byte[] coprname = { (byte)'C', (byte)'O', (byte)'P', (byte)'R' }; //com.dalsemi.onewire.utils.IOHelper.readBytesAsc(4, ' ');

        Debug.Write("Enter the file extension of the coprocessor file (0) : ");
        int coprext = 0; // com.dalsemi.onewire.utils.IOHelper.readInt(0);

        Debug.Write("Enter the name of the service file (4 characters) : ");
        byte[] name = { (byte)'S', (byte)'V', (byte)'C', (byte)'F' }; // com.dalsemi.onewire.utils.IOHelper.readBytesAsc(4, ' ');

        Debug.Write("Enter the file extension of the service file (102 for Money) : ");
        byte ext = 102; // (byte)com.dalsemi.onewire.utils.IOHelper.readInt(102);

        Debug.Write("Enter authentication page number (7) : ");
        int auth_page = 7; // com.dalsemi.onewire.utils.IOHelper.readInt(7);
        if (auth_page < 7)
        {
            Debug.WriteLine("Authentication page too low, default to 7");
            auth_page = 7;
        }
        if (auth_page == 8)
        {
            Debug.WriteLine("Page already taken, default to 7");
            auth_page = 7;
        }

        Debug.Write("Enter workspace page number (9) : ");
        int work_page = 9; //TODO com.dalsemi.onewire.utils.IOHelper.readInt(9);
        if (work_page < 7)
        {
            Debug.WriteLine("Workspace page too low, default to 9");
            work_page = 9;
        }
        if ((work_page == 8) || (work_page == auth_page))
        {
            Debug.WriteLine("Page already taken, default to 9");
            work_page = 9;
        }

        Debug.Write("Enter version number (1) : ");
        int version = 1; //TODO com.dalsemi.onewire.utils.IOHelper.readInt(1);

        Debug.WriteLine("How would you like to enter the binding data (32 bytes)? ");
        byte[] bind_data = getBytes(32);

        Debug.WriteLine("How would you like to enter the binding code (7 bytes)? ");
        byte[] bind_code = getBytes(7);

        // This could be done on the button
        //java.util.Random random = new java.util.Random();
        //random.nextBytes(chlg);
        //  Need to know what the challenge is so that I can reproduce it!
        byte[] chlg = new byte[] { 0x00, 0x00, 0x00 };

        Debug.WriteLine("Enter a human-readable provider name: ");
        string provider_name = com.dalsemi.onewire.utils.IOHelper.readLine();

        Debug.WriteLine("Enter an initial signature in HEX (all 0' default): ");
        byte[] sig_ini = com.dalsemi.onewire.utils.IOHelper.readBytesHex(20, 0);

        Debug.WriteLine("Enter any additional text you would like store on the coprocessor: ");
        string aux_data = com.dalsemi.onewire.utils.IOHelper.readLine();

        Debug.WriteLine("Enter an encryption code (0): ");
        int enc_code = com.dalsemi.onewire.utils.IOHelper.readInt(0);

        //now that we got all that, we need a signing secret and an authentication secret
        Debug.WriteLine("How would you like to enter the signing secret (unlimited bytes)? ");
        sign_secret = new byte[] { 0, 0, 0, 0 }; //TODO
        com.dalsemi.onewire.utils.IOHelper.writeBytes(sign_secret);

        Debug.WriteLine("");
        Debug.WriteLine("How would you like to enter the authentication secret (unlimited bytes)? ");
        auth_secret = new byte[] { 0, 0, 0, 0 }; //TODO
        com.dalsemi.onewire.utils.IOHelper.writeBytes(auth_secret);

        Debug.WriteLine("");
        Debug.WriteLine("Would you like to reformat the authentication secret for the 1961S? (y or n)");
        string s = com.dalsemi.onewire.utils.IOHelper.readLine();
        if (s.ToUpper()[0] == 'Y')
        {
            //reformat the auth_secret
            auth_secret = SHAiButtonCopr.reformatFor1961S(auth_secret);
            com.dalsemi.onewire.utils.IOHelper.writeLine("authentication secret");
            com.dalsemi.onewire.utils.IOHelper.writeBytes(auth_secret);
            com.dalsemi.onewire.utils.IOHelper.writeLine();
        }

        // signing page must be 8, using secret 0
        int sign_page = 8;

        if (!string.ReferenceEquals(coprVMfilename, null))
        {
            byte[] RomID = new byte[] { 0x18, 0x20, 0xAF, 0x02, 0x00, 0x00, 0x00, 0xE7 };
            RomID = sha_properties.getPropertyBytes("copr.simulated.address", RomID);

            copr = new SHAiButtonCoprVM(RomID, sign_page, auth_page, work_page, version, enc_code, ext, name, Encoding.UTF8.GetBytes(provider_name), bind_data, bind_code, Encoding.UTF8.GetBytes(aux_data), sig_ini, chlg, sign_secret, auth_secret);
            ((SHAiButtonCoprVM)copr).save(coprVMfilename, vmSaveSecrets);
        }
        else
        {
            string coprFilename = coprname.ToString() + "." + coprext;
            // initialize this OneWireContainer18 as a valid coprocessor
            copr = new SHAiButtonCopr(copr18, coprFilename, true, sign_page, auth_page, work_page, version, enc_code, ext, name, Encoding.UTF8.GetBytes(provider_name), bind_data, bind_code, Encoding.UTF8.GetBytes(aux_data), sig_ini, chlg, sign_secret, auth_secret);
        }
        Debug.WriteLine("Initialized Coprocessor");
        Debug.WriteLine(copr.ToString());

        Debug.WriteLine("done");
    }

    public static byte[] getBytes(int cnt)
    {
        Debug.WriteLine("   1 HEX");
        Debug.WriteLine("   2 ASCII");
        Debug.Write("  ? ");
        int choice = com.dalsemi.onewire.utils.IOHelper.readInt(2);

        if (choice == 1)
        {
            return com.dalsemi.onewire.utils.IOHelper.readBytesHex(cnt, 0x00);
        }
        else
        {
            return com.dalsemi.onewire.utils.IOHelper.readBytesAsc(cnt, 0x00);
        }
    }
}