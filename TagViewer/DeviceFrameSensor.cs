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

using com.dalsemi.onewire.application.tag;

/// <summary>
/// DeviceFrame to contain a sensor's data 
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class DeviceFrameSensor : DeviceFrame
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// Label to display the last reading of the sensor </summary>
   protected internal JLabel sensorLabel;

   //--------
   //-------- Constructor
   //--------

   /// <summary>
   /// Constructor a frame to contain the device data
   /// </summary>
   public DeviceFrameSensor(TaggedDevice dev, string logFile) : base(dev,logFile)
   {
	  // construct the super

	  // add to the center panel
	  sensorLabel = new JLabel("            ");
	  sensorLabel.HorizontalAlignment = JLabel.CENTER;
	  sensorLabel.Font = new Font("SansSerif",Font.PLAIN,20);
	  centerPanel.add(sensorLabel);
   }

   //--------
   //-------- Methods
   //--------

   /// <summary>
   /// Sets the sensor label
   /// </summary>
   public virtual string SensorLabel
   {
	   set
	   {
		  sensorLabel.Text = value;
	   }
   }
}


