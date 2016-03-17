/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2002 Dallas Semiconductor Corporation, All Rights Reserved.
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

	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using com.dalsemi.onewire.container;


	/// <summary>
	/// This class provides a default object for the Level type of a tagged 1-Wire device.
	/// </summary>
	public class Level : TaggedDevice, TaggedSensor
	{

	   /// <summary>
	   /// Creates an object for the device.
	   /// </summary>
	   public Level() : base()
	   {
	   }

	   /// <summary>
	   /// Creates an object for the device with the supplied address and device type connected
	   /// to the supplied port adapter. </summary>
	   /// <param name="adapter"> The adapter serving the sensor. </param>
	   /// <param name="netAddress"> The 1-Wire network address of the sensor.
	   ///  </param>
	   public Level(DSPortAdapter adapter, string netAddress) : base(adapter, netAddress)
	   {
	   }

	   /// <summary>
	   /// The readSensor method returns the <max> or <min> string of the Sensor (in 
	   /// this case, a switch).  The elements <max> and <min> represent conducting 
	   /// and non-conducting states of the switch, respectively. 
	   /// 
	   /// @param--none.
	   /// </summary>
	   /// <returns> String  The <max> string is associated with the conducting switch state,
	   ///                 and the <min> string is associated with the non-conducting state 
	   ///                 of the 1-Wire switch. </returns>
	   public virtual string readSensor()
	   {
		  string returnString = "";
		  byte[] switchState;
		  int switchChannel = Channel;
		  SwitchContainer Container;
		  Container = (SwitchContainer) _DeviceContainer;

		  if (Container.hasLevelSensing()) // if it can sense levels, read it.
		  {
			 switchState = Container.readDevice();
			 if (Container.getLevel(switchChannel, switchState))
			 {
				returnString = Max;
			 }
			 else
			 {
				returnString = Min;
			 }
		  }
		  return returnString;
	   }
	}

}