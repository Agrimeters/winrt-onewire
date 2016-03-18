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
/// DeviceFrame to contain an actuator
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class DeviceFrameActuator : DeviceFrame
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// Combo box that contains the choices for this actuator </summary>
   protected internal JComboBox actuatorCombo;

   /// <summary>
   /// Panel to display the selection combobox </summary>
   protected internal JPanel selectPanel;

   //--------
   //-------- Constructor
   //--------

   /// <summary>
   /// Constructor a deviceFrame with additional features for the actuator
   /// </summary>
   public DeviceFrameActuator(TaggedDevice dev, string logFile) : base(dev,logFile)
   {
	  // construct the super

	  // create select panel
	  selectPanel = new JPanel();
	  selectPanel.AlignmentX = Component.LEFT_ALIGNMENT;
	  selectPanel.Border = BorderFactory.createTitledBorder(BorderFactory.createEtchedBorder(), "Select Actuator State");

	  // create combo box
	  actuatorCombo = new JComboBox(((TaggedActuator)dev).Selections);
	  Dimension actuatorComboDimension = new Dimension(170, 23);
	  actuatorCombo.PreferredSize = actuatorComboDimension;
	  actuatorCombo.Editable = false;
	  actuatorCombo.AlignmentX = Component.LEFT_ALIGNMENT;
	  actuatorCombo.addActionListener(this);

	  // add combo box to select panel
	  selectPanel.add(actuatorCombo);

	  // add select panel to the center panel
	  centerPanel.add(selectPanel);
   }

   //--------
   //-------- Methods
   //--------

   /// <summary>
   /// Gets the current selection as a string
   /// </summary>
   public virtual string Selection
   {
	   get
	   {
		  return (string)actuatorCombo.getItemAt(actuatorCombo.SelectedIndex);
	   }
   }

}


