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


	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;
	using Address = com.dalsemi.onewire.utils.Address;
	using OWPath = com.dalsemi.onewire.utils.OWPath;

	/// <summary>
	/// Represents a group of 1-Wire addresses that have either
	/// arrived to or departed from the 1-Wire network.
	/// 
	/// @author SH
	/// @version 1.00
	/// </summary>
	public class DeviceMonitorEvent
	{
	   /// <summary>
	   /// enum for arrival/departure event types </summary>
	   public const int ARRIVAL = 0, DEPARTURE = 1;

	   /// <summary>
	   /// The type of event (ARRIVAL|DEPARTURE) </summary>
	   protected internal int eventType = -1;
	   /// <summary>
	   /// The monitor which generated the event </summary>
	   protected internal AbstractDeviceMonitor monitor = null;
	   /// <summary>
	   /// The DSPortAdapter the monitor was using at the time of event </summary>
	   protected internal DSPortAdapter adapter = null;
	   /// <summary>
	   /// Vector of addresses for devices </summary>
	   protected internal List<long> vDeviceAddress = null;

	   /// <summary>
	   /// Creates a new DeviceMonitor event with the specified characteristics.
	   /// </summary>
	   /// <param name="eventType"> The type of event (ARRIVAL | DEPARTURE) </param>
	   /// <param name="source"> The monitor which generated the event </param>
	   /// <param name="adapter"> The DSPortAdapter the monitor was using </param>
	   /// <param name="addresses"> Vector of addresses for devices </param>
	   internal DeviceMonitorEvent(int eventType, AbstractDeviceMonitor source, DSPortAdapter adapter, List<long> addresses)
	   {

		  if (eventType != ARRIVAL && eventType != DEPARTURE)
		  {
			 throw new System.ArgumentException("Invalid event type: " + eventType);
		  }
		  this.eventType = eventType;
		  this.monitor = source;
		  this.adapter = adapter;
		  this.vDeviceAddress = addresses;
	   }

	   /// <summary>
	   /// Returns the event type (ARRIVAL | DEPARTURE)
	   /// </summary>
	   /// <returns> the event type (ARRIVAL | DEPARTURE) </returns>
	   public virtual int EventType
	   {
		   get
		   {
			  return this.eventType;
		   }
	   }

	   /// <summary>
	   /// Returns the monitor which generated this event
	   /// </summary>
	   /// <returns> the monitor which generated this event </returns>
	   public virtual AbstractDeviceMonitor Monitor
	   {
		   get
		   {
			  return this.monitor;
		   }
	   }

	   /// <summary>
	   /// Returns DSPortAdapter the monitor was using when the event was generated
	   /// </summary>
	   /// <returns> DSPortAdapter the monitor was using </returns>
	   public virtual DSPortAdapter Adapter
	   {
		   get
		   {
			  return this.adapter;
		   }
	   }

	   /// <summary>
	   /// Returns the number of devices associated with this event
	   /// </summary>
	   /// <returns> the number of devices associated with this event </returns>
	   public virtual int DeviceCount
	   {
		   get
		   {
			  return this.vDeviceAddress.Count;
		   }
	   }

	   /// <summary>
	   /// Returns the OneWireContainer for the address at the specified index
	   /// </summary>
	   /// <returns> the OneWireContainer for the address at the specified index </returns>
	   public virtual OneWireContainer getContainerAt(int index)
	   {
		  long? longAddress = (long?)this.vDeviceAddress[index];
		  return AbstractDeviceMonitor.getDeviceContainer(adapter, longAddress.Value);
	   }

	   /// <summary>
	   /// Returns the Path object for the device at the specified index
	   /// </summary>
	   /// <returns> the Path object for the device at the specified index </returns>
	   public virtual OWPath getPathForContainerAt(int index)
	   {
		  long? longAddress = (long?)this.vDeviceAddress[index];
		  return this.monitor.getDevicePath(longAddress.Value);
	   }

	   /// <summary>
	   /// Returns the device address at the specified index as a primitive long.
	   /// </summary>
	   /// <returns> the device address at the specified index </returns>
	   public virtual long getAddressAsLongAt(int index)
	   {
		  return ((long?)this.vDeviceAddress[index]).Value;
	   }

	   /// <summary>
	   /// Returns the device address at the specified index as a byte array.
	   /// </summary>
	   /// <returns> the device address at the specified index </returns>
	   public virtual byte[] getAddressAt(int index)
	   {
		  return Address.toByteArray(getAddressAsLongAt(index));
	   }

	   /// <summary>
	   /// Returns the device address at the specified index as a String.
	   /// </summary>
	   /// <returns> the device address at the specified index </returns>
	   public virtual string getAddressAsStringAt(int index)
	   {
		  return Address.ToString(getAddressAsLongAt(index));
	   }
	}
}