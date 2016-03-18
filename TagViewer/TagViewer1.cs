using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
using com.dalsemi.onewire.application.file;
using com.dalsemi.onewire.application.tag;
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;

/// <summary>
/// Main class for a SWING based application that dynamically reads
/// XML 1-Wire Tags and displays and appropriate window for each
/// 'sensor' or 'actuator' found.  If a 'branch' is found then it
/// is added to the 1-Wire Search Path for looking for more XML
/// files.  Note that XML files can also be supplied from the
/// system by providing 1 or more on the command line.  The default
/// 1-Wire adapter is used.
/// 
/// TODO
///    1. Make change in log file push change to sub-viewers
///    2. Show list of sub-viewers active
///    3. Allow closing of some sub-viewers without ending application
///    4. Remember window locations and options
///    5. Select 1-Wire adapter instead of default
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class TagViewer1
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// XML parser </summary>
   public static TAGParser parser;

   /// <summary>
   /// 1-Wire adapter that will be used for communication </summary>
   public static DSPortAdapter adapter;

   /// <summary>
   /// Hashtable to keep track of what 1-Wire devices have already
   /// been examined for XML files
   /// </summary>
   public static Dictionary<long, ParseContainer> parseLog;

   /// <summary>
   /// Vector of devices that have been 'tagged' </summary>
   public static List<TaggedDevice> taggedDevices;

   /// <summary>
   /// 1-Wire search paths </summary>
   public static List<OWPath> paths;

   /// <summary>
   /// Vector of windows created for each 'tagged' device </summary>
   public static ArrayList deviceFrames;

   /// <summary>
   /// Logfile name string </summary>
   public static string logFile;

   /// <summary>
   /// Main window for this application </summary>
   public static TagMainFrame main_Renamed;

   //--------
   //-------- Methods
   //--------

   /// <summary>
   /// Method main that creates the main window and then polls for
   /// for XML files.
   /// </summary>
   /// <param name="args"> command line arguments </param>
   public static void Main1(string[] args)
   {
	  int path_count = 0;

	  try
	  {
		 // attempt to get the default adapter
		 adapter = OneWireAccessProvider.DefaultAdapter;

         // create the state instances
         parseLog = new Dictionary<long, ParseContainer>();
		 taggedDevices = new List<TaggedDevice>();
		 paths = new List<OWPath>();
		 deviceFrames = new ArrayList();

		 // create dummy 'trunk' search path
		 paths.Add(new OWPath(adapter));

		 // create the main frame
		 main_Renamed = new TagMainFrame();
		 main_Renamed.AdapterLabel = adapter.AdapterName + "_" + adapter.PortName;

		 // get the initial log file
		 logFile = main_Renamed.LogFile;

		 // check for XML files on the command line
		 for (int i = 0; i < args.Length; i++)
		 {
			main_Renamed.Status = "File being parsed:  " + args[i];
			FileStream file_stream = new FileStream(args[i], FileMode.Open, FileAccess.Read);

			// create the tagParser
			parser = new TAGParser(adapter);

			// attempt to parse it
			parseStream(parser, file_stream, new OWPath(adapter), true);
		 }

		 // add the paths to the main window
		 main_Renamed.clearPathList();
		 for (int p = 0; p < paths.Count; p++)
		 {
			main_Renamed.addToPathList(((OWPath)paths[p]).ToString());
		 }

		 // turn off all branches
		 allBranchesOff();

		 // run loop
		 for (;;)
		 {
			// check if scanning 1-Wire for XML files enabled
			if (main_Renamed.ScanChecked)
			{
			   // check if there is a path to search
			   if (path_count < paths.Count)
			   {
				  // only increment if there is nothing else to search for
				  if (pathXMLSearchComplete(path_count))
				  {
					 path_count++;
				  }
			   }
			   else
			   {
				  path_count = 0;
			   }
			}

			// sleep for 1 second
			main_Renamed.Status = "sleeping";
            Thread.Sleep(1000);
		 }
	  }
	  catch (Exception ex)
	  {
		 Debug.WriteLine(ex.ToString());
         Debug.Write(ex.StackTrace);
	  }
   }

   /// <summary>
   /// Search a given path for an XML file.
   /// </summary>
   /// <param name="currentPathIndex"> index into the 'paths' Vector that
   ///        indicates what 1-Wire path to search for XML files </param>
   /// <returns> true if the current path provided has been
   ///         completely checked for XML files.  false if
   ///         if this current path should be searched further </returns>
   public static bool pathXMLSearchComplete(int currentPathIndex)
   {
	  OneWireContainer owd = null, check_owd = null;
	  ParseContainer pc = null, check_pc = null;
	  OWFileInputStream file_stream = null;
	  bool rslt = true;
	  bool xml_parsed = false;

	  try
	  {
		 main_Renamed.Status = "Waiting for 1-Wire available";

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 main_Renamed.Status = "Exclusive 1-Wire aquired";

		 // open the current path to the device
		 OWPath owpath = (OWPath)paths[currentPathIndex];
		 owpath.open();

		 main_Renamed.Status = "Path opened: " + owpath.ToString();

		 // setup search
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // find all devices, update parseLog and get a device filesystem to check
		 for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {
			owd = (OneWireContainer)owd_enum.Current;
			long key = owd.AddressAsLong;

			main_Renamed.Status = "Device Found: " + owd.AddressAsString;

            // check to see if this is in the parseLog, add if not there
            pc = null;
            parseLog.TryGetValue(key, out pc);
			if (pc == null)
			{
			   main_Renamed.Status = "Device is new to parse: " + owd.AddressAsString;
			   pc = new ParseContainer(owd);
			   parseLog.Add(key, pc);
			}
			else
			{
			   main_Renamed.Status = "Device " + owd.AddressAsString + " with count " + pc.attemptCount;
			}

			// if attemptCount is low then check it later for XML files
			if (pc.attemptCount < ParseContainer.MAX_ATTEMPT)
			{
			   check_owd = owd;
			   check_pc = pc;
			}
		 }

		 // check if there is anything to open
		 if (check_owd != null)
		 {
			// result is false because found something to try and open
			rslt = false;

			main_Renamed.Status = "Attempt to open file TAGX.000";

			// attempt to open a 'TAGX.000' file, (if fail update parse_log)
			try
			{
			   file_stream = new OWFileInputStream(check_owd,"TAGX.0");
			   main_Renamed.Status = "Success file TAGX.000 opened";
			}
			catch (OWFileNotFoundException)
			{
			   file_stream = null;
			   check_pc.attemptCount++;
			   main_Renamed.Status = "Could not open TAGX.000 file";
			}
		 }

		 // try to parse the file, (if fail update parse_log)
		 if (file_stream != null)
		 {
			// create the tagParser
			// (this should not be necessary but the parser currently holds state)
			parser = new TAGParser(adapter);

			// attempt to parse it, on success max out the attempt
			if (parseStream(parser, file_stream, owpath, true))
			{
			   xml_parsed = true;
			   check_pc.attemptCount = ParseContainer.MAX_ATTEMPT;
			}
			else
			{
			   check_pc.attemptCount++;
			}

			// close the file
			try
			{
			   file_stream.close();
			}
			catch (IOException)
			{
			   main_Renamed.Status = "Could not close TAGX.000 file";
			}
		 }

		 // close the path
		 owpath.close();
		 main_Renamed.Status = "Path closed";

		 // update the main paths listbox if XML file found
		 if (xml_parsed)
		 {
			// add the paths to the main window
			main_Renamed.clearPathList();
			for (int p = 0; p < paths.Count; p++)
			{
			   main_Renamed.addToPathList(((OWPath)paths[p]).ToString());
			}
		 }
	  }
	  catch (OneWireException e)
	  {
		 Debug.WriteLine(e);
		 main_Renamed.Status = e.ToString();
	  }
	  finally
	  {
		 // end exclusive use of adapter
		 adapter.endExclusive();
		 main_Renamed.Status = "Exclusive 1-Wire aquired released";
	  }

	  return rslt;
   }

   /// <summary>
   /// Parse the provided XML input stream with the provided parser.
   /// Gather the new TaggedDevices and OWPaths into the global vectors
   /// 'taggedDevices' and 'paths'.
   /// </summary>
   /// <param name="parser"> parser to parse 1-Wire XML files </param>
   /// <param name="stream">  XML file stream </param>
   /// <param name="currentPath">  OWPath that was opened to get to this file </param>
   /// <param name="autoSpawnFrames"> true if new DeviceFrames are spawned with
   ///        new taggedDevices discovered </param>
   /// <returns> true an XML file was successfully parsed. </returns>
   public static bool parseStream(TAGParser parser, Stream stream, OWPath currentPath, bool autoSpawnFrames)
   {
	  bool rslt = false;
	  OWPath tempPath;

	  try
	  {
		 // parse the file
		 List<TaggedDevice> new_devices = parser.parse(stream);

		 // get the new paths
		 List<OWPath> new_paths = parser.OWPaths;

		 main_Renamed.Status = "Success, XML parsed with " + new_devices.Count + " devices " + new_paths.Count + " paths";

		 // add the new devices to the old list
		 for (int i = 0; i < new_devices.Count; i++)
		 {
			TaggedDevice current_device = (TaggedDevice) new_devices[i];

			// update this devices OWPath depending on where we got it if its OWPath is empty
			tempPath = current_device.OWPath;
			if (!tempPath.AllOWPathElements.MoveNext())
			{
			   // replace this devices path with the current path
			   tempPath.copy(currentPath);
			   current_device.OWPath = tempPath;
			}

			// check if spawning frames
			if (autoSpawnFrames)
			{
			   if (current_device is TaggedSensor)
			   {
				  main_Renamed.Status = "Spawning Sensor: " + current_device.Label;
				  deviceFrames.Add(new DeviceMonitorSensor(current_device, logFile));
			   }
			   else if (current_device is TaggedActuator)
			   {
				  main_Renamed.Status = "Spawning Actuator: " + current_device.Label;
				  deviceFrames.Add(new DeviceMonitorActuator(current_device, logFile));
			   }
			}

			// add the new device to the device list
			taggedDevices.Add(current_device);
		 }

		 // add the new paths
		 for (int i = 0; i < new_paths.Count; i++)
		 {
			paths.Add(new_paths[i]);
		 }

		 rslt = true;

	  }
	  catch (org.xml.sax.SAXException se)
	  {
		 main_Renamed.Status = "XML error: " + se;
	  }
	  catch (IOException ioe)
	  {
		 main_Renamed.Status = "IO error: " + ioe;
	  }

	  return rslt;
   }

   /// <summary>
   /// Turn off all branches before doing the first search so devices
   /// are not found on the wrong path. (DS2406 and DS2409 specific)
   /// </summary>
   public static void allBranchesOff()
   {
	  byte[] all_lines_off = new byte[] {0xCC, 0x66, 0xFF};
	  byte[] all_flipflop_off = new byte[] {0xCC, 0x55, 0x07, 0x00, 0x73, 0xFF, 0xFF};
	  try
	  {
		 main_Renamed.Status = "Waiting for 1-Wire available";

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 main_Renamed.Status = "Exclusive 1-Wire aquired";

		 adapter.reset();
		 adapter.dataBlock(all_flipflop_off, 0, all_flipflop_off.Length);

		 main_Renamed.Status = "All flip flop off sent";

		 adapter.reset();
		 adapter.dataBlock(all_lines_off, 0, all_lines_off.Length);

		 main_Renamed.Status = "All lines off sent";
	  }
	  catch (OneWireException e)
	  {
		 Debug.WriteLine(e);
		 main_Renamed.Status = e.ToString();
	  }
	  finally
	  {
		 // end exclusive use of adapter
		 adapter.endExclusive();
		 main_Renamed.Status = "Exclusive 1-Wire aquired released";
	  }
   }
}

