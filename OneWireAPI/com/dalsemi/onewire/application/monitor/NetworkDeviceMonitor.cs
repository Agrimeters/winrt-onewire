using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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
    using com.dalsemi.onewire.adapter;
    using com.dalsemi.onewire.container;
    using com.dalsemi.onewire.utils;
    
    /// <summary>
    /// Class NetworkDeviceMonitor represents the monitor that searches the
    /// 1-Wire net, including the traversal of branches, looing for new arrivals
    /// and departures.
    /// 
    /// @author SH
    /// @version 1.00
    /// </summary>
    public class NetworkDeviceMonitor : AbstractDeviceMonitor
	{
	   /// <summary>
	   /// hashtable for holding the OWPath objects for each device container. </summary>
       protected internal readonly Dictionary<long, OWPath> devicePathHash = new Dictionary<long, OWPath>();
       /// <summary>
       /// A vector of paths, or branches, to search </summary>
        protected internal List<OWPath> paths = null;
	   /// <summary>
	   /// indicates whether or not branches are automatically traversed </summary>
	   protected internal bool branchAutoSearching = true;


	   /// <summary>
	   /// Create a complex monitor that does search branches
	   /// </summary>
	   /// <param name="the"> DSPortAdapter this monitor should search </param>
	   public NetworkDeviceMonitor(DSPortAdapter adapter)
	   {
		  Adapter = adapter;
	   }

	   /// <summary>
	   /// Sets this monitor to search a new DSPortAdapter
	   /// </summary>
	   /// <param name="the"> DSPortAdapter this monitor should search </param>
	   public override DSPortAdapter Adapter
	   {
		   set
		   {
			  if (value == null)
			  {
				 throw new System.ArgumentException("Adapter cannot be null");
			  }
    
			  lock (sync_flag)
			  {
				 this.adapter = value;
    
				 if (this.paths == null)
				 {
					this.paths = new List<OWPath>();
				 }
				 else
				 {
					this.paths.Clear();
				 }
				 this.paths.Add(new OWPath(value));
    
				 resetSearch();
			  }
		   }
           get
           {
                return this.adapter;
           }
	   }

	   /// <summary>
	   /// Indicates whether or not branches are automatically traversed.  If false,
	   /// new branches must be indicated using the "addBranch" method.
	   /// </summary>
	   /// <param name="enabled"> if true, all branches are automatically traversed during a
	   /// search operation. </param>
	   public virtual bool BranchAutoSearching
	   {
		   set
		   {
			  this.branchAutoSearching = value;
		   }
		   get
		   {
			  return this.branchAutoSearching;
		   }
	   }

       /// <summary>
       /// Adds a branch for searching.  Must be used to traverse branches if
       /// auto-searching is disabled.
       /// </summary>
       /// <param name="path"> A branch to be searched during the next search routine </param>
       public virtual void addBranch(OWPath path)
	   {
		  paths.Add(path);
	   }

	   /// <summary>
	   /// Returns the OWPath of the device with the given address.
	   /// </summary>
	   /// <param name="address"> a Long object representing the address of the device </param>
	   /// <returns> The OWPath representing the network path to the device. </returns>
	   public override OWPath getDevicePath(long address)
	   {
		  lock (devicePathHash)
		  {
             OWPath val = null;
             devicePathHash.TryGetValue(address, out val);
             return val;
		  }
	   }

	   /// <summary>
	   /// The device monitor will internally cache OWPath objects for each
	   /// 1-Wire device.  Use this method to clean up all stale OWPath objects.
	   /// A stale path object is a OWPath which references a branching path to a
	   /// 1-Wire device address which has not been seen by a recent search.
	   /// This will be essential in a touch-contact environment which could run
	   /// for some time and needs to conserve memory.
	   /// </summary>
	   public override void cleanUpStalePathReferences()
	   {
		  lock (devicePathHash)
		  {
			 IEnumerator e = devicePathHash.Keys.GetEnumerator();
			 while (e.MoveNext())
			 {
				long o = (long)e.Current;
				if (!deviceAddressHash.ContainsKey(o))
				{
				   devicePathHash.Remove(o);
				}
			 }
		  }
	   }

	   /// <summary>
	   /// Performs a search of the 1-Wire network, with branch searching
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

				// close any opened branches
				for (int j = 0; j < paths.Count; j++)
				{
				   try
				   {
					  ((OWPath)paths[j]).close();
				   }
				   catch (System.Exception)
				   {
					   ;
				   }
				}

				// search through all of the paths
				for (int i = 0; i < paths.Count; i++)
				{
				   // set searches to not use reset
				   adapter.setNoResetSearch();

				   // find the first device on this branch
				   bool search_result = false;
				   OWPath path = (OWPath)paths[i];
				   try
				   {
					  // try to open the current path
					  path.open();
				   }
				   catch (System.Exception)
				   {
					  // if opening the path failed, continue on to the next path
					  continue;
				   }

				   search_result = adapter.findFirstDevice();

				   // loop while devices found
				   while (search_result)
				   {
					  // get the 1-Wire address
					  long longAddress = adapter.AddressAsLong;
					  // check if the device allready exists in our hashtable
					  if (!deviceAddressHash.ContainsKey(longAddress))
					  {
						 OneWireContainer owc = getDeviceContainer(adapter, longAddress);
						 // check to see if it's a switch and if we are supposed
						 // to automatically search down branches
						 if (this.branchAutoSearching && (owc is SwitchContainer))
						 {
							SwitchContainer sc = (SwitchContainer)owc;
							byte[] state = sc.readDevice();
							for (int j = 0; j < sc.getNumberChannels(state); j++)
							{
							   OWPath tmp = new OWPath(adapter, path);
							   tmp.add(owc, j);
							   if (!paths.Contains(tmp))
							   {
								  paths.Add(tmp);
							   }
							}
						 }

						 lock (devicePathHash)
						 {
							devicePathHash[longAddress] = path;
						 }
						 if (arrivals != null)
						 {
							arrivals.Add(longAddress);
						 }
					  }
					  // check if the existing device moved
					  else if (!path.Equals(devicePathHash[longAddress]))
					  {
						 lock (devicePathHash)
						 {
							devicePathHash[longAddress] = path;
						 }
						 if (departures != null)
						 {
							departures.Add(longAddress);
						 }
						 if (arrivals != null)
						 {
							arrivals.Add(longAddress);
						 }
					  }

					  // update count
					  deviceAddressHash[longAddress] = max_state_count;

                      // find the next device on this branch
                      path.open();
					  search_result = adapter.findNextDevice();
				   }
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
			 if (departures != null && departures.Count > 0)
			 {
				fireDepartureEvent(adapter, departures);
			 }
			 if (arrivals != null && arrivals.Count > 0)
			 {
				fireArrivalEvent(adapter, arrivals);
			 }
		  }
	   }

	}

 }