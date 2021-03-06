﻿using System;
using System.Diagnostics;
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
using com.dalsemi.onewire.application.monitor;

/// <summary>
/// Minimal demo to monitor a simple network.
/// 
/// @version    0.00, 25 September 2000
/// @author     DS,BA,SH
/// </summary>
public class OWWatch1 : DeviceMonitorEventListener
{
    /// <summary>
    /// Method main
    /// 
    /// </summary>
    /// <param name="args">
    ///  </param>
    public void Main1(string[] args)
    {
	   int delay;

	   try
	   {    
		  // get the default adapter
		  DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;

		  Debug.WriteLine("");
		  Debug.WriteLine("Adapter: " + adapter.AdapterName + " Port: " + adapter.PortName);
		  Debug.WriteLine("");

		  // clear any previous search restrictions
		  adapter.setSearchAllDevices();
		  adapter.targetAllFamilies();
		  adapter.Speed = DSPortAdapter.SPEED_REGULAR;

          OWWatch1 dm = new OWWatch1(adapter);

		  // sleep for the specified time
		  if (args.Length >= 1)
		  {
 			 delay = Int32.Parse(args [0]);
		  }
		  else
		  {
 			 delay = 20000;
		  }

		  Debug.WriteLine("Monitor run for: " + delay + "ms");
		  Thread.Sleep(delay);

          // clean up
          Debug.WriteLine("Done with monitor run, now cleanup threads");
          dm.killWatch();

          // free port used by adapter
          adapter.freePort();
	   }
	   catch (Exception e)
	   {
		  Debug.WriteLine(e);
	   }

	   return;
    }

    //--------
    //-------- Variables
    //--------

    /// <summary>
    /// DeviceMonitor object used to watch the network
    /// </summary>
    DeviceMonitor dm = null;

    //--------
    //-------- Constructor
    //--------
    public OWWatch1()
    {

    }

    public OWWatch1(DSPortAdapter adapter)
    {
       // create the watcher with this adapter
       dm = new DeviceMonitor(adapter);

       // add this to the event listers
       try
       {
           dm.addDeviceMonitorEventListener(this);
       }
       catch (Exception)
       {
       }

       // start the monitor
       var t = Task.Run(() =>
       {
           dm.run();
       });
    }

    /// <summary>
    /// killWatch thread
    /// </summary>
    public virtual void killWatch()
    {
	   // Kill the OneWireMonitor thread.
	   dm.killMonitor();
    }

    /// <summary>
    /// Arrival event as a NetworkMonitorEventListener
    /// </summary>
    /// <param name="owme"> DeviceMonitorEvent add </param>
    public virtual void deviceArrival(DeviceMonitorEvent devt)
    {
	   for (int i = 0; i < devt.DeviceCount; i++)
	   {
		   Debug.WriteLine("ADD: " + devt.getAddressAsStringAt(i));
	   }
    }

    /// <summary>
    /// Depart event as a NetworkMonitorEventListener
    /// </summary>
    /// <param name="owme"> DeviceMonitorEvent depart </param>
    public virtual void deviceDeparture(DeviceMonitorEvent devt)
    {
	   for (int i = 0; i < devt.DeviceCount; i++)
	   {
		   Debug.WriteLine("REMOVE: " + devt.getAddressAsStringAt(i));
	   }
    }

    /// <summary>
    /// Depart event as a NetworkMonitorEventListener
    /// </summary>
    /// <param name="owme"> DeviceMonitorException depart </param>
    public virtual void networkException(DeviceMonitorException dexc)
    {
	    Debug.WriteLine("ERROR: " + dexc.ToString());
    }
}
