using System.Collections.Generic;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.application.tag
{

    using com.dalsemi.onewire.adapter;
    using com.dalsemi.onewire.container;
    using System;
    /// <summary>
    /// This class provides a default object for the Switch type of a tagged 1-Wire device.
    /// </summary>
    public class Switch : TaggedDevice, TaggedActuator
	{
	   /// <summary>
	   /// Creates an object for the device.
	   /// </summary>
	   public Switch() : base()
	   {
		  ActuatorSelections = new List<string>();
	   }

	   /// <summary>
	   /// Creates an object for the device with the supplied address connected
	   /// to the supplied port adapter. </summary>
	   /// <param name="adapter"> The adapter serving the actuator. </param>
	   /// <param name="netAddress"> The 1-Wire network address of the actuator. </param>
	   public Switch(DSPortAdapter adapter, string netAddress) : base(adapter, netAddress)
	   {
		  ActuatorSelections = new List<string>();
	   }

	   /// <summary>
	   /// Get the possible selection states of this actuator
	   /// </summary>
	   /// <returns> Vector of Strings representing selection states. </returns>
	   public virtual List<string> Selections
	   {
		   get
		   {
			  return ActuatorSelections;
		   }
	   }

	   /// <summary>
	   /// Set the selection of this actuator
	   /// </summary>
	   /// <param name="The"> selection string.
	   /// </param>
	   /// <exception cref="OneWireException">
	   ///  </exception>
	   public virtual string Selection
	   {
		   set
		   {
			  SwitchContainer switchcontainer = DeviceContainer as SwitchContainer;
			  int Index = 0;
			  int channelValue = Channel;
			  Index = ActuatorSelections.IndexOf(value);
			  bool switch_state = false;
    
			  if (Index > -1) // means value is in the vector
			  {
				 // initialize switch-state variable
				 if (Index > 0)
				 {
					 switch_state = true;
				 }
				 // write to the device (but, read it first to get state)
				 byte[] state = switchcontainer.readDevice();
				 // set the switch's state to the value specified
				 switchcontainer.setLatchState(channelValue,switch_state,false,state);
				 switchcontainer.writeDevice(state);
			  }
		   }
	   }

	   // Selections for the Switch actuator:
	   // element 0 -> Means "disconnected" or "open circuit" (init = 0) and is 
	   //              associated with the "min" message.
	   // element 1 -> Means "connect" or "close the circuit", (init = 1) and is 
	   //              associated with the "max" message.

	   /// <summary>
	   /// Initializes the actuator </summary>
	   /// <param name="Init"> The initialization string.
	   /// </param>
	   /// <exception cref="OneWireException">
	   ///  </exception>
	   public virtual void initActuator()
	   {
		  SwitchContainer switchcontainer = DeviceContainer as SwitchContainer;
		  // initialize the ActuatorSelections Vector
		  ActuatorSelections.Add(Min); // for switch, use min and max
		  ActuatorSelections.Add(Max);
		  // Now, initialize the switch to the desired condition.
		  // This condition is in the <init> tag and, of course, the  
		  // <channel> tag is also needed to know which channel to 
		  // to open or close.
		  int channelValue;
		  int switchStateIntValue = 0;
          int initValue = Int32.Parse(Init);
		  channelValue = Channel;

		  byte[] state = switchcontainer.readDevice();
		  bool switch_state = switchcontainer.getLatchState(channelValue, state);
		  if (switch_state)
		  {
			  switchStateIntValue = 1;
		  }
		  else
		  {
			  switchStateIntValue = 0;
		  }
		  if (initValue != switchStateIntValue)
		  {
			 // set the switch's state to the value specified in XML file
			 switchcontainer.setLatchState(channelValue,!switch_state,false,state);
			 switchcontainer.writeDevice(state);
		  }
	   }

	   /// <summary>
	   /// Keeps the selections of this actuator
	   /// </summary>
	   private List<string> ActuatorSelections;
	}

}