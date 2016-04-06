using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace SHAApp
{
    using com.dalsemi.onewire;
    using com.dalsemi.onewire.adapter;
    using com.dalsemi.onewire.container;
    using Windows.Devices.Enumeration;

    internal class shatest
    {
        /// <summary>
        /// Method printUsageString
        ///
        ///
        /// </summary>
        public static void printUsageString()
        {
            Debug.WriteLine("SHA Container Demo\r\n");
            Debug.WriteLine("Usage: ");
            Debug.WriteLine("   java shatest ADAPTER_PORT\r\n");
            Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
            Debug.WriteLine("adapter you would like to use and the port you would like");
            Debug.WriteLine("to use, for example: ");
            Debug.WriteLine("   java shatest {DS1410E}_LPT1\r\n");
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

        public void Main([ReadOnlyArray()] string[] args)
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
                //parse device instance

                //string[] st = args[0].Split(new char[] { '_' });

                if (args.Length != 2)
                {
                    printUsageString();

                    return;
                }

                adapter_name = args[0];
                port_name = args[1];

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
                        DSPortAdapter temp = (DSPortAdapter)en.Current;

                        Debug.WriteLine("Adapter: " + temp.AdapterName);

                        System.Collections.IEnumerator f = temp.PortNames;

                        while (f.MoveNext())
                        {
                            Debug.WriteLine("   Port name : " + ((DeviceInformation)f.Current).Id);
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

                bool isShaContainer = false;
                OneWireContainer33 sc = null;

                try
                {
                    sc = (OneWireContainer33)owc;
                    isShaContainer = true;
                }
                catch (System.InvalidCastException)
                {
                    sc = null;
                    isShaContainer = false; //just to reiterate
                }

                if (isShaContainer)
                {
                    Debug.WriteLine("= This device is a " + owc.Name);
                    Debug.WriteLine("= Also known as a " + owc.AlternateNames);
                    Debug.WriteLine("=");
                    Debug.WriteLine("= It is a 1K-Bit protected 1-Wire EEPROM with SHA-1 Engine device.");

                    byte[] statusPage = null;
                    ReadMemoryBank(sc.StatusPageMemoryBank, out statusPage);

                    Debug.WriteLine("Status Page");
                    for (var i = 0; i < statusPage.Length; i++)
                    {
                        Debug.Write(statusPage[i].ToString("X") + " ");
                    }
                    Debug.WriteLine("");

                    byte[] mem = new byte[8];
                    Array.Copy(statusPage, 8, mem, 0, 8);

                    if ((mem[0] == 0xAA) || (mem[0] == 0x55))
                    {
                        Debug.WriteLine("SECRET IS WRITE PROTECTED - CANNOT LOAD FIRST SECRET");
                    }
                    else
                    {
                        Debug.WriteLine("SECRET IS NOT WRITE PROTECTED!");
                    }

                    if ((mem[1] == 0xAA) || (mem[1] == 0x55))
                    {
                        Debug.WriteLine("memoryPages[0].readOnly = true");
                        Debug.WriteLine("memoryPages[1].readOnly = true");
                        Debug.WriteLine("memoryPages[2].readOnly = true");
                    }
                    else
                    {
                        Debug.WriteLine("memoryPages[0].readWrite = true");
                        Debug.WriteLine("memoryPages[1].readWrite = true");
                        Debug.WriteLine("memoryPages[2].readWrite = true");
                    }

                    if ((mem[4] == 0xAA) || (mem[4] == 0x55))
                    {
                        Debug.WriteLine("memoryPages[1].writeOnce = true");
                    }
                    else
                    {
                        Debug.WriteLine("memoryPages[1].writeOnce = false");
                    }

                    if ((mem[5] == 0xAA) || (mem[5] == 0x55))
                    {
                        Debug.WriteLine("memoryPages[0].readOnly = true");
                    }
                    else
                    {
                        Debug.WriteLine("memoryPages[0].readWrite = true");
                    }

                    Debug.WriteLine("mem[0] " + mem[0].ToString("X"));
                    Debug.WriteLine("mem[1] " + mem[1].ToString("X"));
                    Debug.WriteLine("mem[3] " + mem[3].ToString("X"));

                    if (((mem[0] != 0xAA) && (mem[0] != 0x55)) &&
                       ((mem[3] != 0xAA) && (mem[3] != 0x55)))
                    {
                        Debug.WriteLine("Case 1");

                        //// Clear all memory to 0xFFh
                        //for (i = 0; i < 16; i++)
                        //{
                        //    if (!LoadFirSecret(portnum, (ushort)(i * 8), data, 8))
                        //        printf("MEMORY ADDRESS %d DIDN'T WRITE\n", i * 8);
                        //}

                        //printf("Current Bus Master Secret Is:\n");
                        //for (i = 0; i < 8; i++)
                        //    printf("%02X ", secret[i]);
                        //printf("\n");
                    }
                    else if ((mem[1] != 0xAA) || (mem[1] != 0x55))
                    {
                        Debug.WriteLine("Case 2");

                        //printf("Please Enter the Current Secret\n");
                        //printf("AA AA AA AA AA AA AA AA  <- Example\n");
                        //scanf("%s %s %s %s %s %s %s %s", &hexstr[0], &hexstr[2], &hexstr[4],
                        //       &hexstr[6], &hexstr[8], &hexstr[10], &hexstr[12], &hexstr[14]);

                        //if (!ParseData(hexstr, strlen(hexstr), data, 16))
                        //    printf("DIDN'T PARSE\n");
                        //else
                        //{
                        //    printf("The secret read was:\n");
                        //    for (i = 0; i < 8; i++)
                        //    {
                        //        secret[i] = data[i];
                        //        printf("%02X ", secret[i]);
                        //        data[i] = 0xFF;
                        //    }
                        //    printf("\n");
                        //}

                        //printf("\n");

                        //if ((indata[13] == 0xAA) || (indata[13] == 0x55))
                        //    skip = 4;
                        //else
                        //    skip = 0;

                        //for (i = (ushort)skip; i < 16; i++)
                        //{
                        //    ReadMem(portnum, (ushort)(((i * 8) / ((ushort)32)) * 32), memory);

                        //    if (WriteScratchSHAEE(portnum, (ushort)(i * 8), &data[0], 8))
                        //        CopyScratchSHAEE(portnum, (ushort)(i * 8), secret, sn, memory);
                        //}
                    }
                    else
                    {
                        Debug.WriteLine("Case 3");

                        //printf("Please Enter the Current Secret\n");
                        //printf("AA AA AA AA AA AA AA AA  <- Example\n");
                        //scanf("%s %s %s %s %s %s %s %s", &hexstr[0], &hexstr[2], &hexstr[4],
                        //       &hexstr[6], &hexstr[8], &hexstr[10], &hexstr[12], &hexstr[14]);
                        //if (!ParseData(hexstr, strlen(hexstr), secret, 16))
                        //    printf("DIDN'T PARSE\n");
                        //else
                        //{
                        //    printf("The secret that was read:\n");
                        //    for (i = 0; i < 8; i++)
                        //    {
                        //        printf("%02X ", secret[i]);
                        //    }
                        //    printf("\n");
                        //}
                    }
                }
                else
                {
                    Debug.WriteLine("= Not a SHA-1 Engine device.");
                    Debug.WriteLine("=");
                    Debug.WriteLine("=");
                }

                next = access.findNextDevice();
            }
        }

        private void ReadMemoryBank(MemoryBank mb, out byte[] readBuf)
        {
            int size = mb.Size;

            readBuf = new byte[size];

            byte[][] extraInfo = null;
            try
            {
                Debug.WriteLine("Reading memory...");

                if (mb is PagedMemoryBank)
                {
                    PagedMemoryBank pmb = (PagedMemoryBank)mb;
                    int len = pmb.PageLength;
                    int numPgs = (size / len) + (size % len > 0 ? 1 : 0);
                    bool hasExtra = pmb.hasExtraInfo();
                    int extraSize = pmb.ExtraInfoLength;
                    if (hasExtra)
                    {
                        extraInfo[numPgs] = new byte[numPgs];
                        for (var i = 0; i < numPgs; i++)
                            extraInfo[0] = new byte[extraSize];
                    }
                    int retryCnt = 0;
                    for (int i = 0; i < numPgs;)
                    {
                        try
                        {
                            bool readContinue = (i > 0) && (retryCnt == 0);
                            if (hasExtra)
                            {
                                pmb.readPage(i, readContinue, readBuf, i * len, extraInfo[i]);
                                Debug.WriteLine("Read Extra Info!");
                            }
                            else
                            {
                                pmb.readPage(i, readContinue, readBuf, i * len);
                            }
                            i++;
                            retryCnt = 0;
                        }
                        catch (Exception e)
                        {
                            if (++retryCnt > 15)
                            {
                                throw e;
                            }
                        }
                    }
                }
                else
                {
                    int retryCnt = 0;
                    while (true)
                    {
                        try
                        {
                            mb.read(0, false, readBuf, 0, size);
                            break;
                        }
                        catch (Exception e)
                        {
                            if (++retryCnt > 15)
                            {
                                throw e;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Debug.Write(e.StackTrace);
                return;
            }

            Debug.WriteLine("Done Reading memory...");
        }
    }
}