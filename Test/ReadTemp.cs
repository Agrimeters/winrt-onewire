using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using System.Threading.Tasks;

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
using System.Collections.Generic;
using System.Collections;
using Windows.Foundation;
using System.Text;

namespace Test
{

    public sealed class ReadTemp
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
            Debug.WriteLine("Temperature Container Demo\r\n");
            Debug.WriteLine("Usage: ");
            Debug.WriteLine("   java ReadTemp ADAPTER_PORT\r\n");
            Debug.WriteLine("ADAPTER_PORT is a String that contains the name of the");
            Debug.WriteLine("adapter you would like to use and the port you would like");
            Debug.WriteLine("to use, for example: ");
            Debug.WriteLine("   java ReadTemp {DS1410E}_LPT1\r\n");
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
        
        public static void Main1([ReadOnlyArray()] string[] args)
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

                bool isTempContainer = false;
                TemperatureContainer tc = null;

                try
                {
                    tc = (TemperatureContainer)owc;
                    isTempContainer = true;
                }
                catch (Exception)
                {
                    tc = null;
                    isTempContainer = false; //just to reiterate
                }

                if (isTempContainer)
                {
                    Debug.WriteLine("= This device is a " + owc.Name);
                    Debug.WriteLine("= Also known as a " + owc.AlternateNames);
                    Debug.WriteLine("=");
                    Debug.WriteLine("= It is a Temperature Container");

                    double max = tc.MaxTemperature;
                    double min = tc.MinTemperature;
                    bool hasAlarms = tc.hasTemperatureAlarms();

                    Debug.WriteLine("= This device " + (hasAlarms ? "has" : "does not have") + " alarms");
                    Debug.WriteLine("= Maximum temperature: " + max);
                    Debug.WriteLine("= Minimum temperature: " + min);

                    double high = 0.0;
                    double low = 0.0;
                    byte[] state = tc.readDevice();

                    if (hasAlarms)
                    {
                        high = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, state);
                        low = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, state);

                        Debug.WriteLine("= High temperature alarm set to : " + high);
                        Debug.WriteLine("= Low temperature alarm set to  : " + low);
                    }

                    double resol = 0.0;
                    bool selectable = tc.hasSelectableTemperatureResolution();

                    if (hasAlarms)
                    {
                        resol = tc.TemperatureAlarmResolution;

                        Debug.WriteLine("= Temperature alarm resolution  : " + resol);
                    }

                    double tempres = tc.getTemperatureResolution(state);
                    double[] resolution = null;

                    Debug.WriteLine("= Temperature resolution        : " + tempres);
                    Debug.WriteLine("= Resolution is selectable      : " + selectable);

                    if (selectable)
                    {
                        try
                        {
                            resolution = tc.TemperatureResolutions;

                            for (int i = 0; i < resolution.Length; i++)
                            {
                                Debug.WriteLine("= Available resolution " + i + "        : " + resolution[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("= Could not get available resolutions : " + e.ToString());
                        }
                    }

                    if (hasAlarms)
                    {
                        Debug.WriteLine("= Setting high temperature alarm to 28.0 C...");
                        tc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, 28.0, state);
                        Debug.WriteLine("= Setting low temperature alarm to 23.0 C...");
                        tc.setTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, 23.0, state);
                    }

                    if (selectable)
                    {
                        try
                        {
                            Debug.WriteLine("= Setting temperature resolution to " + resolution[0] + "...");
                            tc.setTemperatureResolution(resolution[0], state);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("= Could not set resolution: " + e.ToString());
                        }
                    }

                    try
                    {
                        tc.writeDevice(state);
                        Debug.WriteLine("= Device state written.");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("= Could not write device state, all changes lost.");
                        Debug.WriteLine("= Exception occurred: " + e.ToString());
                    }

                    Debug.WriteLine("= Doing temperature conversion...");

                    try
                    {
                        tc.doTemperatureConvert(state);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("= Could not complete temperature conversion...");
                    }

                    state = tc.readDevice();

                    if (hasAlarms)
                    {
                        high = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_HIGH, state);
                        low = tc.getTemperatureAlarm(TemperatureContainer_Fields.ALARM_LOW, state);

                        Debug.WriteLine("= High temperature alarm set to : " + high);
                        Debug.WriteLine("= Low temperature alarm set to  : " + low);
                    }

                    double temp = tc.getTemperature(state);

                    Debug.WriteLine("= Reported temperature: " + temp);
                }
                else
                {
                    Debug.WriteLine("= This device is not a temperature device.");
                    Debug.WriteLine("=");
                    Debug.WriteLine("=");
                }

                next = access.findNextDevice();
            }
        }
    }
}
