using System.Collections.Generic;
using System.Linq;

/*---------------------------------------------------------------------------
 * Copyright (C) 2002 Dallas Semiconductor Corporation, All Rights Reserved.
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
namespace com.dalsemi.onewire.application.monitor
{

	using com.dalsemi.onewire.utils;
	using com.dalsemi.onewire.adapter;

	/// <summary>
	/// <P>Class DeviceMonitor represents the monitor that searches the 1-Wire net
	/// for new arrivals.  This monitor performs a simple search, meaning that
	/// no branches are explicitly traversed.  If a branch is activated/deactivated
	/// between search cycles, this monitor will see the arrival/departure of
	/// new devices without reference to the branch which they lie on.</P>
	/// 
	/// @author SH
	/// @version 1.00
	/// </summary>
	public class DeviceMonitor : AbstractDeviceMonitor
	{
	   private OWPath defaultPath = null;

	   /// <summary>
	   /// Create a simple monitor that does not search branches
	   /// </summary>
	   /// <param name="the"> DSPortAdapter this monitor should search </param>
	   public DeviceMonitor(DSPortAdapter adapter)
	   {
		  Adapter = adapter;
	   }

	   /// <summary>
	   /// Sets this monitor to search a new DSPortAdapter
	   /// </summary>
	   /// <param name="the"> DSPortAdapter this monitor should search </param>
	   public override DSPortAdapter Adapter
	   {
           get
           {
              return base.adapter;
           }

           set
		   {
			  if (value == null)
			  {
				 throw new System.ArgumentException("Adapter cannot be null");
			  }
    
			  lock (sync_flag)
			  {
				 this.adapter = value;
				 defaultPath = new OWPath(value);
    
				 resetSearch();
			  }
		   }
	   }

	   /// <summary>
	   /// Returns the OWPath of the device with the given address.
	   /// </summary>
	   /// <param name="address"> a Long object representing the address of the device </param>
	   /// <returns> The OWPath representing the network path to the device. </returns>
	   public override OWPath getDevicePath(long address)
	   {
		  return defaultPath;
	   }

	   /// <summary>
	   /// Performs a search of the 1-Wire network without searching branches
	   /// </summary>
	   /// <param name="arrivals"> A vector of Long objects, represent new arrival addresses. </param>
	   /// <param name="departures"> A vector of Long objects, represent departed addresses. </param>
	   public override void search(List<long> arrivals, List<long> departures)
	   {
		  lock (sync_flag)
		  {
			 try
			 {
				// aquire the adapter
				adapter.beginExclusive(true);

				// setup the search
				adapter.setSearchAllDevices();
				adapter.targetAllFamilies();
				adapter.Speed = DSPortAdapter.SPEED_REGULAR;

				bool search_result = adapter.findFirstDevice();

				// loop while devices found
				while (search_result)
				{
				   // get the 1-Wire address
				   long longAddress = adapter.AddressAsLong; //new long?(
				   if (!deviceAddressHash.ContainsKey(longAddress) && arrivals != null)
				   {
					  arrivals.Add(longAddress);
				   }

				   deviceAddressHash[longAddress] = max_state_count; //new int?(

				   // search for the next device
				   search_result = adapter.findNextDevice();
				}
			 }
			 finally
			 {
				adapter.endExclusive();
			 }

             // remove any devices that have not been seen
             foreach (var address in deviceAddressHash.Keys.Where(kv => deviceAddressHash[kv] <= 0).ToList())
             {
                 // device entry is stale, should be removed
                 deviceAddressHash.Remove(address);
                 if (departures != null)
                 {
                     departures.Add(address);
                 }
             }

             foreach (var address in deviceAddressHash.Keys.Where(kv => deviceAddressHash[kv] > 0).ToList())
             {
                 // device entry isn't stale, it stays
                 deviceAddressHash[address] -= 1;
             }


             // fire notification events
             if (arrivals != null && arrivals.Count > 0)
			 {
				fireArrivalEvent(adapter, arrivals);
			 }
			 if (departures != null && departures.Count > 0)
			 {
				fireDepartureEvent(adapter, departures);
			 }
		  }
	   }
	}

}