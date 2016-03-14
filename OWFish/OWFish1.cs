using System;
using System.Collections;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

using OneWireException = com.dalsemi.onewire.OneWireException;
using OneWireAccessProvider = com.dalsemi.onewire.OneWireAccessProvider;
using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;
using OWFileDescriptor = com.dalsemi.onewire.application.file.OWFileDescriptor;
using OWFileInputStream = com.dalsemi.onewire.application.file.OWFileInputStream;
using OWFileOutputStream = com.dalsemi.onewire.application.file.OWFileOutputStream;
using OWFile = com.dalsemi.onewire.application.file.OWFile;
using OWSyncFailedException = com.dalsemi.onewire.application.file.OWSyncFailedException;
using MemoryBank = com.dalsemi.onewire.container.MemoryBank;
using PagedMemoryBank = com.dalsemi.onewire.container.PagedMemoryBank;
using System.IO;
using System.Reflection;

/// <summary>
/// Console application to demonstrate file IO on 1-Wire devices.
/// 
/// @version    0.01, 1 June 2001
/// @author     DS
/// </summary>
public class OWFish1
{
   /// <summary>
   /// Main for 1-Wire File Shell (OWFish)
   /// </summary>
   public static void Main1(string[] args)
   {
	  ArrayList owd_vect = new ArrayList(5);
	  OneWireContainer[] owd = null;
	  DSPortAdapter adapter = null;
	  int selection, len;
      long start_time, end_time;
      Stopwatch stopWatch = new Stopwatch();
      FileStream fos;
	  FileStream fis;
//TODO	  FileDescriptor fd;
	  OWFileOutputStream owfos;
	  OWFileInputStream owfis;
	  OWFileDescriptor owfd;
	  OWFile owfile, new_owfile;
	  byte[] block = new byte[32];

	  Debug.WriteLine("");
	  Debug.WriteLine("1-Wire File Shell (OWFish): Version 0.00");
	  Debug.WriteLine("");

      // load the simulated console input stream
      Stream stream = loadResourceFile("OWFish.input.txt");
      dis = new StreamReader(stream);

      stopWatch.Start();

	  try
	  {
		 // get the default adapter
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // adapter driver info
		 Debug.WriteLine("=========================================================================");
		 Debug.WriteLine("== Adapter Name: " + adapter.AdapterName);
		 Debug.WriteLine("== Adapter Port description: " + adapter.PortTypeDescription);
		 Debug.WriteLine("== Adapter Version: " + adapter.AdapterVersion);
		 Debug.WriteLine("== Adapter support overdrive: " + adapter.canOverdrive());
		 Debug.WriteLine("== Adapter support hyperdrive: " + adapter.canHyperdrive());
		 Debug.WriteLine("== Adapter support EPROM programming: " + adapter.canProgram());
		 Debug.WriteLine("== Adapter support power: " + adapter.canDeliverPower());
		 Debug.WriteLine("== Adapter support smart power: " + adapter.canDeliverSmartPower());
		 Debug.WriteLine("== Adapter Class Version: " + adapter.ClassVersion);

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 // loop to do menu
		 selection = MAIN_SELECT_DEVICE;
		 do
		 {
			start_time = 0;
			try
			{
			   switch (selection)
			   {
				  case MAIN_SELECT_DEVICE:
					 // find all parts
					 owd_vect = findAllDevices(adapter);
					 // select a device
					 owd = selectDevice(owd_vect);
					 // check for quite
					 if (owd == null)
					 {
						selection = MAIN_QUIT;
					 }
					 else
					 {
						// display device info
						Debug.WriteLine("");
						Debug.WriteLine("  Device(s) selected: ");
						printDeviceInfo(owd,false);
					 }
					 break;
				  case MAIN_FORMAT:
					 if (menuSelect(verifyMenu) == VERIFY_YES)
					 {
						// start time of operation
						start_time = stopWatch.ElapsedMilliseconds;
						// create a 1-Wire file at root
						owfile = new OWFile(owd, "");
						// format Filesystem
						owfile.format();
						// get 1-Wire File descriptor to flush to device
						owfd = owfile.FD;
						syncFileDescriptor(owfd);
						// close the 1-Wire file to release
						owfile.close();
					 }
					 break;
				  case MAIN_LIST:
					 Debug.Write("Enter the directory to list on (/ for root): ");
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, getString(1));
					 Debug.WriteLine("");
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // list the files without recursion
					 listDir(owfile, 1, false);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
				  case MAIN_RLIST:
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, "");
					 Debug.WriteLine("");
					 // recursive list
					 listDir(owfile, 1, true);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
				  case MAIN_MKDIR:
					 Debug.Write("Enter the directory to create (from root): ");
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, getString(1));
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // make the directories
					 if (owfile.mkdirs())
					 {
						Debug.WriteLine("Success!");
					 }
					 else
					 {
						Debug.WriteLine("-----------------------------------------------");
						Debug.WriteLine("Could not create directories, out of memory or invalid directory/file");
						Debug.WriteLine("-----------------------------------------------");
					 }
					 // get 1-Wire File descriptor to flush to device
					 owfd = owfile.FD;
					 syncFileDescriptor(owfd);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
				  case MAIN_COPYTO:
					 // system SOURCE file
					 Debug.Write("Enter the path/file of the SOURCE file on the system: ");
					 fis = new FileStream(getString(1), FileMode.Open, FileAccess.Read);
					 // 1-Wire DESTINATION file
					 Debug.Write("Enter the path/file of the DESTINATION on 1-Wire device: ");
					 owfos = new OWFileOutputStream(owd, getString(1));
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // loop to copy block from SOURCE to DESTINATION
					 do
					 {
						len = fis.Read(block, 0, block.Length);
						if (len > 0)
						{
						   owfos.write(block,0,len);
						}
					 } while (len > 0);
					 // get 1-Wire File descriptor to flush to device
					 owfd = owfos.FD;
					 syncFileDescriptor(owfd);
					 // close the files
					 owfos.close();
                     fis.Dispose();
					 break;
				  case MAIN_COPYFROM:
					 // 1-Wire SOURCE file
					 Debug.Write("Enter the path/file of the SOURCE file on 1-Wire device: ");
					 owfis = new OWFileInputStream(owd, getString(1));
					 // system DESTINATION file
					 Debug.Write("Enter the path/file of the DESTINATION on system: ");
					 fos = new FileStream(getString(1), FileMode.Create, FileAccess.Write);
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // loop to copy block from SOURCE to DESTINATION
					 do
					 {
						len = owfis.read(block);
						if (len > 0)
						{
						   fos.Write(block,0,len);
						}
					 } while (len > 0);
					 // get 1-Wire File descriptor to flush to device
//TODO					 fd = fos.FD;
//TODO					 fd.sync();
					 // close the files
					 owfis.close();
                     fos.Flush();
                     fos.Dispose();
					 break;
				  case MAIN_CAT:
					 // 1-Wire file
					 Debug.Write("Enter the path/file of the file to display: ");
					 owfis = new OWFileInputStream(owd, getString(1));
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 Debug.WriteLine("");
					 Debug.WriteLine("---FILE START---");
					 // loop to read and display file
					 do
					 {
						len = owfis.read(block);
						if (len > 0)
						{
                           com.dalsemi.onewire.debug.Debug.debug("file cat", block, 0, len);
						}
					 } while (len > 0);
					 Debug.WriteLine("");
					 Debug.WriteLine("---FILE END---");
					 // close the file
					 owfis.close();
					 break;
				  case MAIN_DELETE:
					 Debug.Write("Enter the directory/file delete: ");
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, getString(1));
					 Debug.WriteLine("");
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // delete the directory/file
					 if (owfile.delete())
					 {
						Debug.WriteLine("Success!");
					 }
					 else
					 {
						Debug.WriteLine("-----------------------------------------------");
						Debug.WriteLine("Could not delete, if it is a directory make sure it is empty");
						Debug.WriteLine("-----------------------------------------------");
					 }
					 // get 1-Wire File descriptor to flush to device
					 owfd = owfile.FD;
					 syncFileDescriptor(owfd);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
				  case MAIN_RENAME:
					 Debug.Write("Enter the OLD directory/file name: ");
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, getString(1));
					 Debug.WriteLine("");
					 Debug.Write("Enter the NEW directory/file name: ");
					 // get the directory and create a file on it
					 new_owfile = new OWFile(owd, getString(1));
					 Debug.WriteLine("");
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // rename the directory/file
					 if (owfile.renameTo(new_owfile))
					 {
						Debug.WriteLine("Success!");
					 }
					 else
					 {
						Debug.WriteLine("-----------------------------------------------");
						Debug.WriteLine("Could not rename, make sure parents of new directory exist");
						Debug.WriteLine("-----------------------------------------------");
					 }
					 // get 1-Wire File descriptor to flush to device
					 owfd = owfile.FD;
					 syncFileDescriptor(owfd);
					 // close the 1-Wire file to release
					 owfile.close();
					 new_owfile.close();
					 break;
				  case MAIN_DETAILS:
					 Debug.Write("Enter the directory/file to view details: ");
					 // get the directory and create a file on it
					 owfile = new OWFile(owd, getString(1));
					 Debug.WriteLine("");
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // show the file details
					 showDetails(owfile);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
				  case MAIN_FREEMEM:
					 // start time of operation
					 start_time = stopWatch.ElapsedMilliseconds;
					 // create a 1-Wire file at root
					 owfile = new OWFile(owd, "");
					 // get free memory
					 Debug.WriteLine("");
					 Debug.WriteLine("  free memory: " + owfile.FreeMemory + " (bytes)");
					 // get the devices participating
					 owd = owfile.OneWireContainers;
					 Debug.WriteLine("");
					 Debug.WriteLine("  Filesystem consists of: ");
					 printDeviceInfo(owd,true);
					 // close the 1-Wire file to release
					 owfile.close();
					 break;
			   };
			}
			catch (IOException e)
			{
			   Debug.WriteLine("");
			   Debug.WriteLine("-----------------------------------------------");
			   Debug.WriteLine(e);
			   Debug.WriteLine("-----------------------------------------------");
			}

			end_time = stopWatch.ElapsedMilliseconds;
			Debug.WriteLine("");
			if (start_time > 0)
			{
			   Debug.WriteLine((end_time - start_time) + "ms");
			}
			Debug.WriteLine("");

			if (selection != MAIN_QUIT)
			{
			   selection = menuSelect(mainMenu);
			}
			Debug.WriteLine("");
		 } while (selection != MAIN_QUIT);

	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
	  finally
	  {
		 if (adapter != null)
		 {
			// end exclusive use of adapter
			adapter.endExclusive();

			// free the port used by the adapter
			Debug.WriteLine("Releasing adapter port");
			try
			{
			   adapter.freePort();
			}
			catch (OneWireException e)
			{
			   Debug.WriteLine(e);
			}
		 }
	  }

	  Debug.WriteLine("");
      return;
   }

   /// <summary>
   /// List the files/directory entries on the provided 1-Wire File.  Format the
   /// output depending on the recursive and depth parameters.
   /// </summary>
   /// <param name="depth"> recursive depth </param>
   /// <param name="recursive"> call this method recursivly if true </param>
   public static void listDir(OWFile owfile, int depth, bool recursive)
   {
	  OWFile[] owfile_list;

	  // get the list
	  owfile_list = owfile.listFiles();
	  // check for bad directory
	  if (owfile_list == null)
	  {
		 Debug.WriteLine("Directory given was not valid or error in Filesystem!");
	  }
	  else
	  {
		 // display
		 for (int i = 0; i < owfile_list.Length; i++)
		 {
			if (recursive)
			{
			   Debug.Write("|");
			   if (owfile_list[i].Directory)
			   {
				  for (int j = 0; j < (depth - 1); j++)
				  {
					 Debug.Write("    ");
				  }

				  if (depth == 1)
				  {
					 Debug.Write("----");
				  }
				  else
				  {
					 Debug.Write("|---");
				  }
			   }
			   else
			   {
				  for (int j = 0; j < depth; j++)
				  {
						Debug.Write("    ");
				  }
			   }
			}

			Debug.Write(owfile_list[i].Name);
			if (owfile_list[i].Directory)
			{
			   if (recursive)
			   {
				  Debug.WriteLine("");
			   }
			   else
			   {
				  Debug.WriteLine("    <dir>");
			   }
			   if (recursive)
			   {
				  listDir(owfile_list[i], depth + 1, true);
			   }
			}
			else
			{
			   Debug.WriteLine("  (" + owfile_list[i].length() + " bytes)");
			}

			owfile_list[i].close();
		 }
	  }
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

			// set owd to max possible speed with available adapter, allow fall back
			if (adapter.canOverdrive() && (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
			{
			   owd.setSpeed(owd.MaxSpeed,true);
			}
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
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
   public static OneWireContainer[] selectDevice(ArrayList owd_vect)
   {
	  // create a menu
	  string[] menu = new string[owd_vect.Count + 3];
	  ArrayList rewrite = new ArrayList(1);
	  OneWireContainer owd;
	  int i;
	  OneWireContainer[] oca;
	  MemoryBank mb;

	  menu[0] = "Device Selection";
	  for (i = 0; i < owd_vect.Count; i++)
	  {
		 owd = (OneWireContainer)owd_vect[i];
		 menu[i + 1] = "(" + i + ") " + owd.AddressAsString + " - " + owd.Name;
		 if (owd.AlternateNames.Length > 0)
		 {
			menu[i + 1] += "/" + owd.AlternateNames;
		 }

		 // collect a list of re-writable devices
		 for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
		 {
			// get the next memory bank
			mb = (MemoryBank)bank_enum.Current;

			// check if desired type
			if (!mb.WriteOnce && mb.GeneralPurposeMemory && mb.NonVolatile && (mb is PagedMemoryBank))
			{
			   rewrite.Add(owd);
			   break;
			}
		 }
	  }
	  menu[i + 1] = "[" + i + "]--Select All re-writable devices as a multi-device Filesystem";
	  menu[i + 2] = "[" + (i + 1) + "]--Quit";

	  int select = menuSelect(menu);

	  // quit
	  if (select == (i + 1))
	  {
		 return null;
	  }

	  // all re-writable devices
	  if (select == i)
	  {
		 if (rewrite.Count == 0)
		 {
			return null;
		 }
		 oca = new OneWireContainer[rewrite.Count];
		 for (i = 0; i < oca.Length; i++)
		 {
			oca[i] = (OneWireContainer)rewrite[i];
		 }
	  }
	  // single device
	  else
	  {
		 oca = new OneWireContainer[1];
		 oca[0] = (OneWireContainer)owd_vect[select];
	  }

	  return oca;
   }

   /// <summary>
   /// Sync's the file Descriptor, prompts for retry if there is
   /// an exception.
   /// </summary>
   /// <param name="fd"> OWFileDescriptor of Filesystem to sync </param>
   internal static void syncFileDescriptor(OWFileDescriptor fd)
   {
	  for (;;)
	  {
		 try
		 {
			fd.sync();
			return;
		 }
		 catch (OWSyncFailedException e)
		 {
			Debug.WriteLine("");
			Debug.WriteLine("-----------------------------------------------");
			Debug.WriteLine(e);
			Debug.WriteLine("-----------------------------------------------");

			// prompt to try again
			if (menuSelect(retryMenu) == RETRY_NO)
			{
			   return;
			}
		 }
	  }
   }

   /// <summary>
   /// Display information about the 1-Wire device
   /// </summary>
   /// <param name="owd"> OneWireContainer device </param>
   /// <param name="showMultiType"> <code> true </code> if want to show designations
   ///        in a multi-device Filesystem (MASTER/SATELLITE) </param>
   internal static void printDeviceInfo(OneWireContainer[] owd, bool showMultiType)
   {
	  for (int dev = 0; dev < owd.Length; dev++)
	  {
		 if (showMultiType && (owd.Length > 1))
		 {
			if (dev == 0)
			{
			   Debug.Write("   MASTER:      ");
			}
			else
			{
			   Debug.Write("   SATELITE(" + dev + "): ");
			}
		 }
		 else
		 {
			Debug.Write("   ");
		 }

		 Debug.WriteLine(owd[dev].Name + ", " + owd[dev].AddressAsString + ", (maxspeed) " + ((owd[dev].MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
	  }
   }

   /// <summary>
   /// Display file details
   /// </summary>
   /// <param name="owfile"> 1-Wire file to show details of </param>
   internal static void showDetails(OWFile owfile)
   {
	  int[] page_list;
	  int local_pg;
	  PagedMemoryBank pmb;

	  // check if this directory/file exists
	  if (!owfile.exists())
	  {
		 Debug.WriteLine("-----------------------------------------------");
		 Debug.WriteLine("Directory/file not found!");
		 Debug.WriteLine("-----------------------------------------------");
		 return;
	  }

	  // get the list of pages that make up this directory/file
	  Debug.WriteLine("Page allocation of " + (owfile.File ? "file:" : "directory:"));

	  try
	  {
		 page_list = owfile.PageList;
	  }
	  catch (IOException e)
	  {
		 Debug.WriteLine(e);
		 return;
	  }

	  // loop to display info and contents of each page
	  for (int pg = 0; pg < page_list.Length; pg++)
	  {
		 local_pg = owfile.getLocalPage(page_list[pg]);
		 Debug.WriteLine("Filesystem page=" + page_list[pg] + ", local PagedMemoryBank page=" + owfile.getLocalPage(page_list[pg]));
		 pmb = owfile.getMemoryBankForPage(page_list[pg]);
		 byte[] read_buf = new byte[pmb.PageLength];
		 try
		 {
			pmb.readPage(local_pg, false, read_buf, 0);
			hexPrint(read_buf,0,read_buf.Length);
			Debug.WriteLine("");
		 }
		 catch (OneWireException e)
		 {
			Debug.WriteLine(e);
		 }
	  }
   }

   /// <summary>
   /// Display menu and ask for a selection.
   /// </summary>
   /// <param name="menu"> Array of strings that represents the menu.
   ///        The first element is a title so skip it.
   /// </param>
   /// <returns> numberic value entered from the console. </returns>
   internal static int menuSelect(string[] menu)
   {
	  Debug.WriteLine("");
	  for (int i = 0; i < menu.Length; i++)
	  {
		 Debug.WriteLine(menu[i]);
	  }

	  Debug.Write("Please enter value: ");

	  return getNumber(0, menu.Length - 2);
   }

   /// <summary>
   /// Retrieve user input from the console.
   /// </summary>
   /// <param name="min"> minimum number to accept </param>
   /// <param name="max"> maximum number to accept
   /// </param>
   /// <returns> numberic value entered from the console. </returns>
   internal static int getNumber(int min, int max)
   {
	  int value = -1;
	  bool fNumber = false;

	  while (fNumber == false)
	  {
		 try
		 {
			string str = getString(1);

			value = int.Parse(str);

			if ((value < min) || (value > max))
			{
			   Debug.WriteLine("Invalid value, range must be " + min + " to " + max);
			   Debug.Write("Please enter value again: ");
			}
			else
			{
			   fNumber = true;
			}
		 }
		 catch (System.FormatException e)
		 {
			Debug.WriteLine("Invalid Numeric Value: " + e.ToString());
			Debug.Write("Please enter value again: ");
		 }
	  }

	  return value;
   }

   private static StreamReader dis = null;

   /// <summary>
   /// loads manifest resource file for use as Console Input stream
   /// </summary>
   /// <param name="file"></param>
   /// <returns></returns>
   public static Stream loadResourceFile(string file)
   {
       try
       {
           Assembly asm = typeof(OWFish.MainPage).GetTypeInfo().Assembly;
           return asm.GetManifestResourceStream(file);
       }
       catch (Exception)
       {
           Debug.WriteLine("Can't find resource: " + file);
       }
       return null;
   }

   /// <summary>
   /// Retrieve user input from the console.
   /// </summary>
   /// <param name="minLength"> minumum length of string
   /// </param>
   /// <returns> string entered from the console. </returns>
   internal static string getString(int minLength)
   {
	  string str;
	  bool done = false;

	  try
	  {
		 do
		 {
			str = dis.ReadLine();
			if (str.Length < minLength)
			{
			   Debug.Write("String too short try again:");
			}
			else
			{
			   done = true;
			}
		 } while (!done);

		 return str;
	  }
	  catch (IOException e)
	  {
		 Debug.WriteLine("Error in reading from console: " + e);
	  }

	  return "";
   }

   /// <summary>
   /// Print an array of bytes in hex to standard out.
   /// </summary>
   /// <param name="dataBuf"> data to print </param>
   /// <param name="offset">  offset into dataBuf to start </param>
   /// <param name="len">     length of data to print </param>
   public static void hexPrint(byte[] dataBuf, int offset, int len)
   {
	  for (int i = 0; i < len; i++)
	  {
		 if ((dataBuf[i + offset] & 0x000000FF) < 0x00000010)
		 {
			Debug.Write("0");
			Debug.Write(((int)dataBuf[i + offset] & 0x0000000F).ToString("x").ToUpper());
		 }
		 else
		 {
			Debug.Write(((int)dataBuf[i + offset] & 0x000000FF).ToString("x").ToUpper());
		 }
	  }
   }

   //--------
   //-------- Menus
   //--------

   internal static readonly string[] mainMenu = new string[] {"----  1-Wire File Shell ----", "(0) Select Device", "(1) Directory list", "(2) Directory list (recursive)", "(3) Make Directory", "(4) Copy file TO 1-Wire Filesystem", "(5) Copy file FROM 1-Wire Filesystem", "(6) Display contents of file", "(7) Delete 1-Wire directory/file", "(8) Rename 1-Wire directory/file", "(9) Memory available, and Filesystem info", "(10) Show File details", "(11) Format Filesystem on 1-Wire device", "[12]-Quit"};

   internal const int MAIN_SELECT_DEVICE = 0;
   internal const int MAIN_LIST = 1;
   internal const int MAIN_RLIST = 2;
   internal const int MAIN_MKDIR = 3;
   internal const int MAIN_COPYTO = 4;
   internal const int MAIN_COPYFROM = 5;
   internal const int MAIN_CAT = 6;
   internal const int MAIN_DELETE = 7;
   internal const int MAIN_RENAME = 8;
   internal const int MAIN_FREEMEM = 9;
   internal const int MAIN_DETAILS = 10;
   internal const int MAIN_FORMAT = 11;
   internal const int MAIN_QUIT = 12;

   internal static readonly string[] verifyMenu = new string[] {"Format Filesystem on 1-Wire device(s)?", "(0) NO", "(1) YES (delete all files/directories)"};

   internal const int VERIFY_NO = 0;
   internal const int VERIFY_YES = 1;

   internal static readonly string[] retryMenu = new string[] {"RETRY to SYNC with Filesystem on 1-Wire device(s)?", "(0) NO", "(1) YES"};

   internal const int RETRY_NO = 0;
   internal const int RETRY_YES = 1;
}
