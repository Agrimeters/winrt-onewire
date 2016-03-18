using System;
using System.Collections;
using System.Windows.Forms;

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
using com.dalsemi.onewire.container;
using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
using OWFileOutputStream = com.dalsemi.onewire.application.file.OWFileOutputStream;
using OWFileNotFoundException = com.dalsemi.onewire.application.file.OWFileNotFoundException;
using OWPath = com.dalsemi.onewire.utils.OWPath;

/// <summary>
/// Main class for a SWING based application that prompts to create
/// a 1-Wire XML tag.  It can create (4) kinds of sensors, (2) kinds
/// of actuators or a branch XML tag.  The tag can be saved on the
/// computer system or to the 1-Wire File system.  The default 1-Wire
/// adapter is used.
/// 
/// TODO:
///    1. Create an ImageIcon to use in all of the dialogs
///    2. Use a parent window?
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class TagCreator
{
   /// <summary>
   /// Method main, that provides the dialog box prompts to create the
   /// 1-Wire XML tag.
   /// </summary>
   /// <param name="args"> command line arguments </param>
   public static void Main(string[] args)
   {
	  OneWireContainer tag_owd;
	  DSPortAdapter adapter = null;
	  ArrayList owd_vect = new ArrayList(5);
	  bool get_min = false, get_max = false, get_channel = false, get_init = false, get_scale = false;
	  string file_type , label , tag_type , method_type = null, cluster ;
	  string min = null, max = null, channel = null, init = null, scale = null;

	  // connect now message
	  JOptionPane.showMessageDialog(null, "Connect the 1-Wire device to Tag onto the Default 1-Wire port", "1-Wire Tag Creator", JOptionPane.INFORMATION_MESSAGE);

	  try
	  {
		 // get the default adapter
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 // find all parts
		 owd_vect = findAllDevices(adapter);

		 // select a device
		 tag_owd = selectDevice(owd_vect,"Select the 1-Wire Device to Tag");

		 // enter the label for this devcie
		 label = JOptionPane.showInputDialog(null, "Enter a human readable label for this device: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 if (string.ReferenceEquals(label, null))
		 {
			throw new InterruptedException("Aborted");
		 }

		 // enter the cluster for this devcie
		 cluster = JOptionPane.showInputDialog(null, "Enter a cluster where this device will reside: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 if (string.ReferenceEquals(cluster, null))
		 {
			throw new InterruptedException("Aborted");
		 }

		 // select the type of device
		 string[] tag_types = new string[] {"sensor", "actuator", "branch"};
		 tag_type = (string)JOptionPane.showInputDialog(null, "Select the Tag Type", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE, null, tag_types, tag_types[0]);
		 if (string.ReferenceEquals(tag_type, null))
		 {
			throw new InterruptedException("Aborted");
		 }

		 // check if branch selected
		 if (string.ReferenceEquals(tag_type, "branch"))
		 {
			get_init = true;
			get_channel = true;
		 }
		 // sensor
		 else if (string.ReferenceEquals(tag_type, "sensor"))
		 {
			string[] sensor_types = new string[] {"Contact", "Humidity", "Event", "Thermal"};
			method_type = (string)JOptionPane.showInputDialog(null, "Select the Sensor Type", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE, null, sensor_types, sensor_types[0]);
			if (string.ReferenceEquals(method_type, null))
			{
			   throw new InterruptedException("Aborted");
			}

			// contact
			if (string.ReferenceEquals(method_type, "Contact"))
			{
			   get_min = true;
			   get_max = true;
			}
			// Event
			else if (string.ReferenceEquals(method_type, "Event"))
			{
			   get_channel = true;
			   get_max = true;
			}
		 }
		 // actuator
		 else
		 {
			string[] actuator_types = new string[] {"Switch", "D2A"};
			method_type = (string)JOptionPane.showInputDialog(null, "Select the Actuator Type", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE, null, actuator_types, actuator_types[0]);
			if (string.ReferenceEquals(method_type, null))
			{
			   throw new InterruptedException("Aborted");
			}

			get_channel = true;
			get_init = true;
			get_min = true;
			get_max = true;
		 }

		 // enter the tags required
		 if (get_min)
		 {
			min = JOptionPane.showInputDialog(null, "Enter the 'min' value: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 }
		 if (string.ReferenceEquals(min, null))
		 {
			get_min = false;
		 }

		 if (get_max)
		 {
			max = JOptionPane.showInputDialog(null, "Enter the 'max' value: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 }
		 if (string.ReferenceEquals(max, null))
		 {
			get_max = false;
		 }

		 if (get_channel)
		 {
			channel = JOptionPane.showInputDialog(null, "Enter the 'channel' value: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 }
		 if (string.ReferenceEquals(channel, null))
		 {
			get_channel = false;
		 }

		 if (get_init)
		 {
			init = JOptionPane.showInputDialog(null, "Enter the 'init' value: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 }
		 if (string.ReferenceEquals(init, null))
		 {
			get_init = false;
		 }

		 if (get_scale)
		 {
			scale = JOptionPane.showInputDialog(null, "Enter the 'scale' value: ", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE);
		 }
		 if (string.ReferenceEquals(scale, null))
		 {
			get_scale = false;
		 }

		 // build the XML file
		 ArrayList xml = new ArrayList(5);
		 xml.Add("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		 xml.Add("<cluster name=\"" + cluster + "\">");
		 xml.Add(" <" + tag_type + " addr=\"" + tag_owd.AddressAsString + "\" type=\"" + method_type + "\">");
		 xml.Add("  <label>" + label + "</label>");
		 if (get_max)
		 {
			xml.Add("  <max>" + max + "</max>");
		 }
		 if (get_min)
		 {
			xml.Add("  <min>" + min + "</min>");
		 }
		 if (get_channel)
		 {
			xml.Add("  <channel>" + channel + "</channel>");
		 }
		 if (get_init)
		 {
			xml.Add("  <init>" + init + "</init>");
		 }
		 if (get_scale)
		 {
			xml.Add("  <scale>" + scale + "</scale>");
		 }
		 xml.Add(" </" + tag_type + ">");
		 xml.Add("</cluster>");

		 // display the XML file
		 JList list = new JList(xml.ToArray());
		 if (MessageBox.Show(null, list, "Is this correct?", MessageBoxButtons.YesNo) != DialogResult.Yes)
		 {
			throw new InterruptedException("Aborted");
		 }

		 // loop until file written
		 bool file_written = false;
		 do
		 {
			// Check if doing desktop or 1-Wire file
			string[] file_types = new string[] {"Desktop File", "1-Wire File"};
			file_type = (string)JOptionPane.showInputDialog(null, "Select where to put this XML 1-Wire Tag file", "1-Wire tag Creator", JOptionPane.INFORMATION_MESSAGE, null, file_types, file_types[0]);
			if (string.ReferenceEquals(file_type, null))
			{
			   throw new InterruptedException("Aborted");
			}

			// save to a PC file
			if (string.ReferenceEquals(file_type, "Desktop File"))
			{
			   JFileChooser chooser = new JFileChooser();
			   int returnVal = chooser.showSaveDialog(null);
			   if (returnVal == JFileChooser.APPROVE_OPTION)
			   {
				  try
				  {
					 PrintWriter writer = new PrintWriter(new System.IO.FileStream(chooser.SelectedFile.CanonicalPath, true));
					 for (int i = 0; i < xml.Count; i++)
					 {
						writer.println(xml[i]);
					 }
					 writer.flush();
					 writer.close();
					 JOptionPane.showMessageDialog(null, "XML File saved to: " + chooser.SelectedFile.CanonicalPath, "1-Wire Tag Creator", JOptionPane.INFORMATION_MESSAGE);
					 file_written = true;
				  }
				  catch (FileNotFoundException e)
				  {
					 Console.WriteLine(e);
					 JOptionPane.showMessageDialog(null, "ERROR saving XML File: " + chooser.SelectedFile.CanonicalPath, "1-Wire Tag Creator", JOptionPane.WARNING_MESSAGE);
				  }
			   }
			}
			// 1-Wire file
			else
			{
			   // search parts again in case the target device was just connected
			   owd_vect = findAllDevices(adapter);

			   // select the 1-Wire device to save the file to
			   tag_owd = selectDevice(owd_vect,"Select the 1-Wire Device to place XML Tag");

			   // attempt to write to the filesystem of this device
			   try
			   {
				  PrintWriter writer = new PrintWriter(new OWFileOutputStream(tag_owd,"TAGX.0"));
				  for (int i = 0; i < xml.Count; i++)
				  {
					 writer.println(xml[i]);
				  }
				  writer.flush();
				  writer.close();
				  JOptionPane.showMessageDialog(null, "XML File saved to: " + tag_owd.AddressAsString + "\\TAGX.000", "1-Wire Tag Creator", JOptionPane.INFORMATION_MESSAGE);
					 file_written = true;
			   }
			   catch (OWFileNotFoundException e)
			   {
				  Console.WriteLine(e);
				  JOptionPane.showMessageDialog(null, "ERROR saving XML File: " + tag_owd.AddressAsString + "\\TAGX.000", "1-Wire Tag Creator", JOptionPane.WARNING_MESSAGE);
			   }
			}

			// check if file not written
			if (!file_written)
			{
			   if (MessageBox.Show(null, "Try to save file again?", "1-Wire Tag Creator", MessageBoxButtons.OKCancel) != DialogResult.OK)
			   {
				  throw new InterruptedException("Aborted");
			   }
			}
		 } while (!file_written);
	  }
	  catch (Exception e)
	  {
		 Console.WriteLine(e);
	  }
	  finally
	  {
		 if (adapter != null)
		 {
			// end exclusive use of adapter
			adapter.endExclusive();

			// free the port used by the adapter
			Console.WriteLine("Releasing adapter port");
			try
			{
			   adapter.freePort();
			}
			catch (OneWireException e)
			{
			   Console.WriteLine(e);
			}
		 }
	  }

	  Environment.Exit(0);
   }

   /// <summary>
   /// Search for all devices on the provided adapter and return
   /// a vector
   /// </summary>
   /// <param name="adapter"> valid 1-Wire adapter
   /// </param>
   /// <returns> Vector or OneWireContainers </returns>
   public static ArrayList findAllDevices(DSPortAdapter adapter)
   {
	  ArrayList owd_vect = new ArrayList(3);
	  OneWireContainer owd;

	  try
	  {
		 // clear any previous search restrictions
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the 1-Wire devices and collect them in a vector
		 for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {
			owd = (OneWireContainer)owd_enum.Current;
			owd_vect.Add(owd);
		 }
	  }
	  catch (Exception e)
	  {
		 Console.WriteLine(e);
	  }

	  return owd_vect;
   }

   /// <summary>
   /// Create a menu from the provided OneWireContainer
   /// Vector and allow the user to select a device.
   /// </summary>
   /// <param name="owd_vect"> vector of devices to choose from
   /// </param>
   /// <returns> OneWireContainer device selected </returns>
   public static OneWireContainer selectDevice(ArrayList owd_vect, string title)
   {
	  // create a menu
	  ArrayList menu = new ArrayList(owd_vect.Count);
	  string temp_str;
	  OneWireContainer owd;
	  int i;

	  for (i = 0; i < owd_vect.Count; i++)
	  {
		 owd = (OneWireContainer)owd_vect[i];
		 temp_str = owd.AddressAsString + " - " + owd.Name;
		 if (owd.AlternateNames.length() > 0)
		 {
			temp_str += "/" + owd.AlternateNames;
		 }

		 menu.Add(temp_str);
	  }

	  string selectedValue = (string)JOptionPane.showInputDialog(null, title, "1-Wire Tag Creator", JOptionPane.INFORMATION_MESSAGE, null, menu.ToArray(), menu.ToArray()[0]);

	  if (!string.ReferenceEquals(selectedValue, null))
	  {
		 return (OneWireContainer)owd_vect[menu.IndexOf(selectedValue)];
	  }
	  else
	  {
		 throw new InterruptedException("Quit in device selection");
	  }
   }
}

