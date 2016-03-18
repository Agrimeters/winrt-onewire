using System;
using System.Threading;
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
using System.Threading.Tasks;

/// <summary>
/// Thread class to monitor an actuator 
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class DeviceMonitorActuator
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// Frame that is displaying the actuator </summary>
   private DeviceFrameActuator actuatorFrame;

   /// <summary>
   /// The actual tagged device </summary>
   protected internal TaggedDevice actuator;

   /// <summary>
   /// Last selection to see if it has changed </summary>
   protected internal string lastSelection;

   /// <summary>
   /// 1-Wire adapter used to access the actuator </summary>
   protected internal DSPortAdapter adapter;

   /// <summary>
   /// Did initialization flag </summary>
   protected internal bool didInit;

   //--------
   //-------- Constructors
   //--------

   /// <summary>
   /// Don't allow anyone to instantiate without providing device
   /// </summary>
   private DeviceMonitorActuator()
   {
   }

   /// <summary>
   /// Create an actuator monitor.
   /// </summary>
   /// <param name="dev"> Tagged device to monitor </param>
   /// <param name="logFile"> file name to log to </param>
   public DeviceMonitorActuator(TaggedDevice dev, string logFile)
   {
	  // get ref to the contact device
	  actuator = dev;

	  // create the Frame that will display this device
	  actuatorFrame = new DeviceFrameActuator(dev,logFile);

	  // hide the read items since this is an actuator
	  actuatorFrame.hideReadItems();

	  // get adapter ref
	  adapter = actuator.DeviceContainer.Adapter;

	  // init last selection
	  lastSelection = "";
	  didInit = false;

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
		 // check if did init
		 if (!didInit)
		 {
			// initialize the actuator
			didInit = initialize();
		 }

		 // check for change in selection
		 string new_selection = actuatorFrame.Selection;

		 // check if these is a selection and it is different
		 if (!string.ReferenceEquals(new_selection, null))
		 {
			if (!lastSelection.Equals(new_selection))
			{
			   Selection = new_selection;
			   lastSelection = new_selection;
			}
		 }

		 // sleep for 200 milliseconds
         Thread.Sleep(200);
	  }
   }

   /// <summary>
   /// Sets the selection in the device to match the window.
   /// </summary>
   /// <param name="selection"> string  </param>
   public virtual string Selection
   {
	   set
	   {
		  try
		  {
			 // get exclusive use of adapter
			 adapter.beginExclusive(true);
    
			 // open path to the device
			 actuator.OWPath.open();
    
			 // set the actuator to the value 
			 ((TaggedActuator)actuator).Selection = value;
    
			 // close the path to the device
			 actuator.OWPath.close();
    
			 // display time of last reading
			 actuatorFrame.showTime("Action Serviced at: ");
    
			 // log if enabled
			 if (actuatorFrame.LogChecked)
			 {
				actuatorFrame.log(value);
			 }
		  }
		  catch (OneWireException e)
		  {
			 Debug.WriteLine(e);
			 // log exception if enabled
			 if (actuatorFrame.LogChecked)
			 {
				actuatorFrame.log(e.ToString());
			 }
		  }
		  finally
		  {
			 // end exclusive use of adapter
			 adapter.endExclusive();
		  }
	   }
   }

   /// <summary>
   /// Initialize the actuator
   /// </summary>
   /// <returns> 'true' if initialization was successful  </returns>
   public virtual bool initialize()
   {
	  bool rslt = false;

	  try
	  {
		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 // open path to the device
		 actuator.OWPath.open();

		 // initialize the actuator 
		 ((TaggedActuator)actuator).initActuator();

		 // close the path to the device
		 actuator.OWPath.close();

		 // display time of last reading
		 actuatorFrame.showTime("Initialized at: ");

		 rslt = true;
	  }
	  catch (OneWireException e)
	  {
		 Debug.WriteLine(e);
		 // log exception if enabled
		 if (actuatorFrame.LogChecked)
		 {
			actuatorFrame.log(e.ToString());
		 }
	  }
	  finally
	  {
		 // end exclusive use of adapter
		 adapter.endExclusive();
	  }

	  return rslt;
   }
}
