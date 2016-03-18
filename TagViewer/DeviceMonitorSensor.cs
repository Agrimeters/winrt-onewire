using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 2001 Dallas Semiconductor Corporation, All Rights Reserved.
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
using com.dalsemi.onewire.application.tag;
using com.dalsemi.onewire.container;

/// <summary>
/// Thread class to monitor a sensor 
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class DeviceMonitorSensor
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// Frame that is displaying the sensor </summary>
   private DeviceFrameSensor sensorFrame;

   /// <summary>
   /// the tagged device </summary>
   protected internal TaggedDevice sensor;

   /// <summary>
   /// Last poll delay value in seconds </summary>
   protected internal int lastPollDelay;

   /// <summary>
   /// Counter used to calculate the next sensor read </summary>
   protected internal int currentSecondCount;

   /// <summary>
   /// 1-Wire adapter used to access the sensor </summary>
   protected internal DSPortAdapter adapter;

   //--------
   //-------- Constructors
   //--------

   /// <summary>
   /// Don't allow anyone to instantiate without providing device
   /// </summary>
   private DeviceMonitorSensor()
   {
   }

   /// <summary>
   /// Create an sensor monitor.
   /// </summary>
   /// <param name="dev"> Tagged device to monitor </param>
   /// <param name="logFile"> file name to log to </param>
   public DeviceMonitorSensor(TaggedDevice dev, string logFile)
   {
	  // get ref to the contact device
	  sensor = dev;

	  // create the Frame that will display this device
	  sensorFrame = new DeviceFrameSensor(dev,logFile);

	  // init
	  lastPollDelay = 0;
	  currentSecondCount = 0;

	  adapter = sensor.DeviceContainer.Adapter;

	  // start up this service thread
      var t = Task.Run(() =>
      {
          this.run();
      });
   }

   //--------
   //-------- Methods
   //--------

   /// <summary>
   /// Device monitor run method
   /// </summary>
   public virtual void run()
   {
	  // run loop
	  for (;;)
	  {
		 // check for read key press
		 if (sensorFrame.ReadButtonClick)
		 {
			makeSensorReading();
		 }

		 // check for new polldelay rate
		 if (lastPollDelay != sensorFrame.PollDelay)
		 {
			lastPollDelay = sensorFrame.PollDelay;
			currentSecondCount = 0;
		 }

		 // check if time to do a poll reading
		 if ((lastPollDelay != 0) && (currentSecondCount >= lastPollDelay))
		 {
			makeSensorReading();
			currentSecondCount = 0;
		 }

		 // count the seconds
		 currentSecondCount++;

		 // sleep for 1 second
         Thread.Sleep(1000);
	  }
   }

   /// <summary>
   /// Makes a sensor reading 
   /// </summary>
   public virtual void makeSensorReading()
   {
	  try
	  {
		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 // open path to the device
		 sensor.OWPath.open();

		 // read the sensor update 
		 string reading = ((TaggedSensor)sensor).readSensor();
		 sensorFrame.SensorLabel = reading;

		 // close the path to the device
		 sensor.OWPath.close();

		 // display time of last reading
		  sensorFrame.showTime("Last Reading: ");

		 // log if enabled
		 if ((reading.Length > 0) && (sensorFrame.LogChecked))
		 {
			sensorFrame.log(reading);
		 }
	  }
	  catch (OneWireException e)
	  {
		 Debug.WriteLine(e);
		 // log if enabled
		 if (sensorFrame.LogChecked)
		 {
			sensorFrame.log(e.ToString());
		 }
	  }
	  finally
	  {
		 // end exclusive use of adapter
		 adapter.endExclusive();
	  }
   }
}
