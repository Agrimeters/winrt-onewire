using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;
using System.IO;
using System.Reflection;

/// <summary>
/// Console application to utilize the MemoryBank features of the
/// OneWireContainers to write blocks and packets.
/// 
/// @version    0.01, 15 December 2000
/// @author     DS
/// </summary>
public class OWMemUtil1
{

   /// <summary>
   /// Main for 1-Wire Memory utility
   /// </summary>
   public static void Main1(string[] args)
   {
      List<OneWireContainer> owd_vect = new List<OneWireContainer>(5);
	  OneWireContainer owd;
	  int i, len, addr, page;
	  bool done = false;
	  string tstr;
	  DSPortAdapter adapter = null;
	  MemoryBank bank;
	  byte[] data;

      Stream stream = loadResourceFile("OWMemUtil.input.txt");
      dis = new StreamReader(stream);

	  Debug.WriteLine("");
	  Debug.WriteLine("1-Wire Memory utility console application: Version 0.01");
	  Debug.WriteLine("");

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
		 do
		 {
			// Main menu
			switch (menuSelect(mainMenu))
			{

			   case MAIN_SELECT_DEVICE :

				  // find all parts
				  owd_vect = findAllDevices(adapter);

				  // select a device
				  owd = selectDevice(owd_vect);

				  // display device info
				  printDeviceInfo(owd);

				  // select a bank
				  bank = selectBank(owd);

				  // display bank information
				  displayBankInformation(bank);

				  // loop on bank menu
				  do
				  {
					 if (owd is PasswordContainer)
					 {
						switch (menuSelect(bankPswdMenu))
						{
						   case BANK_INFO :
							  // display bank information
							  displayBankInformation(bank);
							  break;

						   case BANK_READ_BLOCK :
							  // read a block
							  Debug.Write("Enter the address to start reading: ");

							  addr = getNumber(0, bank.Size - 1);

							  Debug.Write("Enter the length of data to read: ");

							  len = getNumber(0, bank.Size);

							  Debug.WriteLine("");
							  dumpBankBlock(bank, addr, len);
							  break;

						   case BANK_READ_PAGE :
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to read: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);

								 Debug.WriteLine("");
								 dumpBankPage((PagedMemoryBank) bank, page);
							  }
							  break;

						   case BANK_READ_UDP :
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to read: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);

								 Debug.WriteLine("");
								 dumpBankPagePacket((PagedMemoryBank) bank, page);
							  }
							  break;

						   case BANK_WRITE_BLOCK :
							  // write a block
							  Debug.Write("Enter the address to start writing: ");

							  addr = getNumber(0, bank.Size - 1);
							  data = Data;

							  bankWriteBlock(bank, data, addr);
							  break;

						   case BANK_WRITE_UDP :
							  // write a packet
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to write a UDP to: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);
								 data = Data;

								 bankWritePacket((PagedMemoryBank) bank, data, page);
							  }
							  break;

						   case BANK_NEW_PASS_BANK :
							  // select a bank
							  bank = selectBank(owd);
							  break;

						   case BANK_NEW_MAIN_MENU :
							  done = true;
							  break;

						   case BANK_READONLY_PASS :
							  Debug.WriteLine("Enter data for the read only password.");
							  data = Data;

							  ((PasswordContainer)owd).setDeviceReadOnlyPassword(data, 0);
							  break;

						   case BANK_READWRIT_PASS :
							  Debug.WriteLine("Enter data for the read/write password.");
							  data = Data;

							  ((PasswordContainer)owd).setDeviceReadWritePassword(data, 0);
							  break;

						   case BANK_WRITEONLY_PASS :
							  Debug.WriteLine("Enter data for the write only password.");
							  data = Data;

							  ((PasswordContainer)owd).setDeviceWriteOnlyPassword(data, 0);
							  break;

						   case BANK_BUS_RO_PASS :
							  Debug.WriteLine("Enter data for the read only password.");
							  data = Data;

							  ((PasswordContainer)owd).setContainerReadOnlyPassword(data, 0);
							  break;

						   case BANK_BUS_RW_PASS :
							  Debug.WriteLine("Enter data for the read/write password.");
							  data = Data;

							  ((PasswordContainer)owd).setContainerReadWritePassword(data, 0);
							  break;

						   case BANK_BUS_WO_PASS :
							  Debug.WriteLine("Enter data for the write only password.");
							  data = Data;

							  ((PasswordContainer)owd).setContainerWriteOnlyPassword(data, 0);
							  break;

						   case BANK_ENABLE_PASS :
							  ((PasswordContainer)owd).DevicePasswordEnableAll = true;
							  break;

						   case BANK_DISABLE_PASS :
							  ((PasswordContainer)owd).DevicePasswordEnableAll = false;
							  break;
						}
					 }
					 else
					 {
						switch (menuSelect(bankMenu))
						{

						   case BANK_INFO :

							  // display bank information
							  displayBankInformation(bank);
							  break;
						   case BANK_READ_BLOCK :

							  // read a block
							  Debug.Write("Enter the address to start reading: ");

							  addr = getNumber(0, bank.Size - 1);

							  Debug.Write("Enter the length of data to read: ");

							  len = getNumber(0, bank.Size);

							  Debug.WriteLine("");
							  dumpBankBlock(bank, addr, len);
							  break;
						   case BANK_READ_PAGE :
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to read: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);

								 Debug.WriteLine("");
								 dumpBankPage((PagedMemoryBank) bank, page);
							  }
							  break;
						   case BANK_READ_UDP :
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to read: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);

								 Debug.WriteLine("");
								 dumpBankPagePacket((PagedMemoryBank) bank, page);
							  }
							  break;
						   case BANK_WRITE_BLOCK :

							  // write a block
							  Debug.Write("Enter the address to start writing: ");

							  addr = getNumber(0, bank.Size - 1);
							  data = Data;

							  bankWriteBlock(bank, data, addr);
							  break;
						   case BANK_WRITE_UDP :

							  // write a packet
							  if (!(bank is PagedMemoryBank))
							  {
								 Debug.Write("Bank is not a 'PagedMemoryBank'");
							  }
							  else
							  {
								 Debug.Write("Enter the page number to write a UDP to: ");

								 page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);
								 data = Data;

								 bankWritePacket((PagedMemoryBank) bank, data, page);
							  }
							  break;
						   case BANK_NEW_BANK :

							  // select a bank
							  bank = selectBank(owd);
							  break;
						   case BANK_MAIN_MENU :
							  done = true;
							  break;
						}
					 }
				  } while (!done);

				  done = false;
				  break;
			   case MAIN_TEST :

				  // find all parts
				  owd_vect = findAllDevices(adapter);

				  // test menu
				  switch (menuSelect(testMenu))
				  {

					 case TEST_WRITE_BLOCK :
						Debug.Write("Enter the character to write to the entire device: ");

						tstr = getString(1);

						for (i = 0; i < owd_vect.Count; i++)
						{
						   writeTestBlocks((OneWireContainer) owd_vect[i], (byte) tstr[0]);
						}
						break;
					 case TEST_WRITE_PKTS :
						Debug.Write("Enter the length of data in the packets (0-29): ");

						len = getNumber(0, 29);

						Debug.Write("Enter the character to write to the entire device: ");

						tstr = getString(1);

						for (i = 0; i < owd_vect.Count; i++)
						{
						   writeTestPkts((OneWireContainer) owd_vect[i], (byte) tstr[0], len);
						}
						break;
					 case TEST_READ_RAW :
						for (i = 0; i < owd_vect.Count; i++)
						{
						   dumpDeviceRaw((OneWireContainer) owd_vect[i]);
						}
						break;
					 case TEST_READ_PKTS :
						for (i = 0; i < owd_vect.Count; i++)
						{
						   dumpDevicePackets((OneWireContainer) owd_vect[i]);
						}
						break;
					 case TEST_QUIT :
						done = true;
						break;
				  }
				  break;
			   case MAIN_QUIT :
				  done = true;
				  break;
			}
		 } while (!done);
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
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
   /// Search for all devices on the provided adapter and return
   /// a vector
   /// </summary>
   /// <param name="adapter"> valid 1-Wire adapter
   /// </param>
   /// <returns> Vector or OneWireContainers </returns>
   public static List<OneWireContainer> findAllDevices(DSPortAdapter adapter)
   {
	  List<OneWireContainer> owd_vect = new List<OneWireContainer>(3);
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
			owd = (OneWireContainer) owd_enum.Current;

			owd_vect.Add(owd);

			// set owd to max possible speed with available adapter, allow fall back
			if (adapter.canOverdrive() && (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
			{
			   owd.setSpeed(owd.MaxSpeed, true);
			}
		 }

	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }

	  return owd_vect;
   }

   //--------
   //-------- Read methods
   //--------

   /// <summary>
   /// Read a block from a memory bank and print in hex
   /// </summary>
   /// <param name="bank">  MemoryBank to read a block from </param>
   /// <param name="addr">  address to start reading from </param>
   /// <param name="len">   length of data to read </param>
   public static void dumpBankBlock(MemoryBank bank, int addr, int len)
   {
	  try
	  {
		 byte[] read_buf = new byte [len];

		 // read the entire bank
		 bank.read(addr, false, read_buf, 0, len);
		 hexPrint(read_buf, 0, len);
		 Debug.WriteLine("");
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   /// <summary>
   /// Read a page from a memory bank and print in hex
   /// </summary>
   /// <param name="bank">  PagedMemoryBank to read a page from </param>
   /// <param name="pg">  page to read </param>
   public static void dumpBankPage(PagedMemoryBank bank, int pg)
   {
	  byte[] read_buf = new byte [bank.PageLength];
	  byte[] extra_buf = new byte [bank.ExtraInfoLength];

	  try
	  {

		 // read a page (use the most verbose and secure method)
		 if (bank.hasPageAutoCRC())
		 {
			Debug.WriteLine("Using device generated CRC");

			if (bank.hasExtraInfo())
			{
			   bank.readPageCRC(pg, false, read_buf, 0, extra_buf);
			}
			else
			{
			   bank.readPageCRC(pg, false, read_buf, 0);
			}
		 }
		 else
		 {
			if (bank.hasExtraInfo())
			{
			   bank.readPage(pg, false, read_buf, 0, extra_buf);
			}
			else
			{
			   bank.readPage(pg, false, read_buf, 0);
			}
		 }

		 Debug.Write("Page " + pg + ": ");
		 hexPrint(read_buf, 0, read_buf.Length);
		 Debug.WriteLine("");

		 if (bank.hasExtraInfo())
		 {
			Debug.Write("Extra: ");
			hexPrint(extra_buf, 0, bank.ExtraInfoLength);
			Debug.WriteLine("");
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   /// <summary>
   /// Read a page packet from a memory bank and print in hex
   /// </summary>
   /// <param name="bank">  PagedMemoryBank to read a page from </param>
   /// <param name="pg">  page to read </param>
   public static void dumpBankPagePacket(PagedMemoryBank bank, int pg)
   {
	  byte[] read_buf = new byte [bank.PageLength];
	  byte[] extra_buf = new byte [bank.ExtraInfoLength];
	  int read_rslt;

	  try
	  {

		 // read a page packet (use the most verbose method)
		 if (bank.hasExtraInfo())
		 {
			read_rslt = bank.readPagePacket(pg, false, read_buf, 0, extra_buf);
		 }
		 else
		 {
			read_rslt = bank.readPagePacket(pg, false, read_buf, 0);
		 }

		 Debug.Write("Packet " + pg + ", len " + read_rslt + ": ");
		 hexPrint(read_buf, 0, read_rslt);
		 Debug.WriteLine("");

		 if (bank.hasExtraInfo())
		 {
			Debug.Write("Extra: ");
			hexPrint(extra_buf, 0, bank.ExtraInfoLength);
			Debug.WriteLine("");
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   /// <summary>
   /// Dump all valid memory packets from all general-purpose memory banks.
   /// in the provided OneWireContainer instance.
   /// 
   /// @parameter owd device to check for memory banks.
   /// </summary>
   public static void dumpDevicePackets(OneWireContainer owd)
   {
	  byte[] read_buf, extra_buf;
	  int read_rslt;
	  bool found_bank = false;
      Stopwatch stopWatch = new Stopwatch();

	  // display device info
	  printDeviceInfo(owd);

	  // set to max possible speed
	  owd.setSpeed(owd.MaxSpeed, true);

	  // get the port names we can use and try to open, test and close each
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {

		 // get the next memory bank
		 MemoryBank mb = (MemoryBank) bank_enum.Current;

		 // check if desired type, only look for packets in general non-volatile
		 if (!mb.GeneralPurposeMemory || !mb.NonVolatile)
		 {
			continue;
		 }

		 // check if has paged services
		 if (!(mb is PagedMemoryBank))
		 {
			continue;
		 }

		 // found a memory bank
		 found_bank = true;

		 // cast to page bank
		 PagedMemoryBank bank = (PagedMemoryBank) mb;

		 // display bank information
		 displayBankInformation(bank);

		 read_buf = new byte [bank.PageLength];
		 extra_buf = new byte [bank.ExtraInfoLength];

		 // start timer to time the dump of the bank contents
         stopWatch.Start();

		 // loop to read all of the pages in bank
		 bool readContinue = false;

		 for (int pg = 0; pg < bank.NumberPages; pg++)
		 {
			try
			{

			   // read a page packet (use the most verbose and secure method)
			   if (bank.hasExtraInfo())
			   {
				  read_rslt = bank.readPagePacket(pg, readContinue, read_buf, 0, extra_buf);
			   }
			   else
			   {
				  read_rslt = bank.readPagePacket(pg, readContinue, read_buf, 0);
			   }

			   if (read_rslt >= 0)
			   {
				  readContinue = true;

				  Debug.Write("Packet " + pg + " (" + read_rslt + "): ");
				  hexPrint(read_buf, 0, read_rslt);
				  Debug.WriteLine("");

				  if (bank.hasExtraInfo())
				  {
					 Debug.Write("Extra: ");
					 hexPrint(extra_buf, 0, bank.ExtraInfoLength);
					 Debug.WriteLine("");
				  }
			   }
			   else
			   {
				  Debug.WriteLine("Error reading page : " + pg);

				  readContinue = false;
			   }
			}
			catch (Exception e)
			{
			   Debug.WriteLine("Exception in reading page: " + e + "TRACE: ");

			   readContinue = false;
			}
		 }

         stopWatch.Stop();
		 Debug.WriteLine("     (time to read PACKETS = " + stopWatch.ElapsedMilliseconds + "ms)");
         stopWatch.Reset();
      }

      if (!found_bank)
	  {
		 Debug.WriteLine("XXXX Does not contain any general-purpose non-volatile page memory bank's");
	  }
   }

   /// <summary>
   /// Dump all of the 1-Wire readable memory in the provided
   /// Memory Banks of the OneWireContainer instance.
   /// 
   /// @parameter owd device to check for memory banks.
   /// </summary>
   public static void dumpDeviceRaw(OneWireContainer owd)
   {
	  bool found_bank = false;

	  // display device info
	  printDeviceInfo(owd);

	  // set to max possible speed
	  owd.setSpeed(owd.MaxSpeed, true);

	  // loop through all of the memory banks on device
	  // get the port names we can use and try to open, test and close each
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {

		 // get the next memory bank
		 MemoryBank bank = (MemoryBank) bank_enum.Current;

		 // display bank information
		 displayBankInformation(bank);

		 // found a memory bank
		 found_bank = true;

		 // dump the bank
		 dumpBankBlock(bank, 0, bank.Size);
	  }

	  if (!found_bank)
	  {
		 Debug.WriteLine("XXXX Does not contain any memory bank's");
	  }
   }

   //--------
   //-------- Write methods
   //--------

   /// <summary>
   /// Write a block of data with the provided MemoryBank.
   /// </summary>
   /// <param name="bank">  MemoryBank to write block to </param>
   /// <param name="data">  data to write in a byte array </param>
   /// <param name="addr">  address to start the write </param>
   public static void bankWriteBlock(MemoryBank bank, byte[] data, int addr)
   {
	  try
	  {
		 bank.write(addr, data, 0, data.Length);
		 Debug.WriteLine("");
		 Debug.WriteLine("wrote block length " + data.Length + " at addr " + addr);
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   /// <summary>
   /// Write a UDP packet to the specified page in the
   /// provided PagedMemoryBank.
   /// </summary>
   /// <param name="bank">  PagedMemoryBank to write packet to </param>
   /// <param name="data">  data to write in a byte array </param>
   /// <param name="pg">    page number to write packet to </param>
   public static void bankWritePacket(PagedMemoryBank bank, byte[] data, int pg)
   {
	  try
	  {
		 bank.writePagePacket(pg, data, 0, data.Length);
		 Debug.WriteLine("");
		 Debug.WriteLine("wrote packet length " + data.Length + " on page " + pg);
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }
   }

   //--------
   //-------- Menu methods
   //--------

   /// <summary>
   /// Create a menu from the provided OneWireContainer
   /// Vector and allow the user to select a device.
   /// </summary>
   /// <param name="owd_vect"> vector of devices to choose from
   /// </param>
   /// <returns> OneWireContainer device selected </returns>
   public static OneWireContainer selectDevice(List<OneWireContainer> owd_vect)
   {

	  // create a menu
	  string[] menu = new string [owd_vect.Count + 2];
	  OneWireContainer owd;
	  int i;

	  menu [0] = "Device Selection";

	  for (i = 0; i < owd_vect.Count; i++)
	  {
		 owd = owd_vect[i];
		 menu [i + 1] = "(" + i + ") " + owd.AddressAsString + " - " + owd.Name;

		 if (owd.AlternateNames.Length > 0)
		 {
			menu [i + 1] += "/" + owd.AlternateNames;
		 }
	  }

	  menu [i + 1] = "[" + i + "]--Quit";

	  int select = menuSelect(menu);

	  if (select == i)
	  {
		 throw new OperationCanceledException("Quit in device selection");
	  }

	  return owd_vect[select];
   }

   /// <summary>
   /// Create a menu of memory banks from the provided OneWireContainer
   /// allow the user to select one.
   /// </summary>
   /// <param name="owd"> devices to choose a MemoryBank from
   /// </param>
   /// <returns> MemoryBank memory bank selected </returns>
   public static MemoryBank selectBank(OneWireContainer owd)
   {

	  // create a menu
	  List<MemoryBank> banks = new List<MemoryBank>(3);
	  int i;

	  // get a vector of the banks
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {
		 banks.Add((MemoryBank) bank_enum.Current);
	  }

	  string[] menu = new string [banks.Count + 2];

	  menu [0] = "Memory Bank Selection for " + owd.AddressAsString + " - " + owd.Name;

	  if (owd.AlternateNames.Length > 0)
	  {
		 menu [0] += "/" + owd.AlternateNames;
	  }

	  for (i = 0; i < banks.Count; i++)
	  {
		 menu [i + 1] = "(" + i + ") " + ((MemoryBank) banks[i]).BankDescription;
	  }
	  menu [i + 1] = "[" + i + "]--Quit";

	  int select = menuSelect(menu);

	  if (select == i)
	  {
		 throw new OperationCanceledException("Quit in bank selection");
	  }

	  return banks[select];
   }

   //--------
   //-------- Menu Methods
   //--------

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
		 Debug.WriteLine(menu [i]);
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

   private static System.IO.StreamReader dis = null;

   /// <summary>
   /// Loads resource file to be used as Input Stream to drive program
   /// </summary>
   /// <param name="file"></param>
   /// <returns></returns>
   public static Stream loadResourceFile(string file)
   {
       try
       {
           Assembly asm = typeof(OWMemUtil.MainPage).GetTypeInfo().Assembly;
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
	  catch (System.IO.IOException e)
	  {
		 Debug.WriteLine("Error in reading from console: " + e);
	  }

	  return "";
   }

   /// <summary>
   /// Retrieve user input from the console in the form of hex or text.
   /// </summary>
   /// <returns> byte array of data. </returns>
   public static byte[] Data
   {
	   get
	   {
		  byte[] data = null;
		  bool got_data = false;
    
		  if (menuSelect(modeMenu) == MODE_TEXT)
		  {
			 string tstr = getString(1);
    
             data = new byte[tstr.Length];

             for(int i = 0; i < tstr.Length; i++)
             {
                 data[i] = byte.Parse(tstr.Substring(i, 1), System.Globalization.NumberStyles.HexNumber);
             }
          }
          else
		  {
			 do
			 {
				try
				{

				   string tstr = getString(2);
    
				   data = parseByteString(tstr);
				   got_data = true;
				}
				catch (IndexOutOfRangeException e)
				{
				   Debug.WriteLine(e);
				   Debug.WriteLine("Enter Hex data again");
				}
			 } while (!got_data);
		  }
    
		  Debug.Write("Data to write, len (" + data.Length + ") :");
		  hexPrint(data, 0, data.Length);
		  Debug.WriteLine("");
    
		  return data;
	   }
   }

   //--------
   //-------- Test Methods
   //--------

   /// <summary>
   /// Write a block to every memory bank in this device that is general-purpose
   /// and non-volatile
   /// </summary>
   /// <param name="owd">          device to write block to </param>
   /// <param name="data">         data byte to write </param>
   public static void writeTestBlocks(OneWireContainer owd, byte data)
   {
      Stopwatch stopWatch = new Stopwatch();

	  // display device info
	  printDeviceInfo(owd);

	  // set to max possible speed
	  owd.setSpeed(owd.MaxSpeed, false);

	  // get the port names we can use and try to open, test and close each
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {

		 // get the next memory bank
		 MemoryBank bank = (MemoryBank) bank_enum.Current;

		 // display bank information
		 displayBankInformation(bank);

		 // check if desired type
		 if (!bank.GeneralPurposeMemory || !bank.NonVolatile)
		 {
			Debug.WriteLine("**** Not general-purpose/non-volatile so skipping this bank ****");

			continue;
		 }

		 // write the block
		 try
		 {
			byte[] wr_buf = new byte [bank.Size];

			for (int i = 0; i < wr_buf.Length; i++)
			{
			   wr_buf [i] = (byte) data;
			}

			// start timer to time the dump of the bank contents
            stopWatch.Start();

			bank.write(0, wr_buf, 0, wr_buf.Length);
			Debug.WriteLine("wrote block (" + wr_buf.Length + ") at addr 0");

            stopWatch.Stop();
			Debug.WriteLine("     (time to write = " + stopWatch.ElapsedMilliseconds + "ms)");
            stopWatch.Reset();
		 }
		 catch (Exception e)
		 {
			Debug.WriteLine("Exception writing: " + e + "TRACE: ");
			Debug.WriteLine(e.ToString());
			Debug.Write(e.StackTrace);
		 }
	  }
   }

   /// <summary>
   /// Write a packet to every page in every memory bank in this device that is general-purpose
   /// and non-volatile
   /// </summary>
   /// <param name="owd">          device to write block to </param>
   /// <param name="data">         data byte to write </param>
   /// <param name="len">          length of data to write </param>
   public static void writeTestPkts(OneWireContainer owd, byte data, int len)
   {
      Stopwatch stopWatch = new Stopwatch();

	  // display device info
	  printDeviceInfo(owd);

	  // set to max possible speed
	  owd.setSpeed(owd.MaxSpeed, false);

	  // get the port names we can use and try to open, test and close each
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {

		 // get the next memory bank
		 MemoryBank bank = (MemoryBank) bank_enum.Current;

		 // display bank information
		 displayBankInformation(bank);

		 // check if desired type
		 if (!bank.GeneralPurposeMemory || !bank.NonVolatile)
		 {
			Debug.WriteLine("**** Not general-purpose/non-volatile so skipping this bank ****");

			continue;
		 }

		 // check if has paged services
		 if (!(bank is PagedMemoryBank))
		 {
			Debug.WriteLine("**** Not PagedMemoryBank so skipping this bank ****");

			continue;
		 }

		 // caste to page bank
		 PagedMemoryBank pbank = (PagedMemoryBank) bank;
		 byte[] wr_buf = new byte [pbank.PageLength];

		 for (int i = 0; i < len; i++)
		 {
			wr_buf [i] = (byte) data;
		 }

		 // start timer to time the dump of the bank contents
         stopWatch.Start();

		 // loop to read all of the pages in bank
		 for (int pg = 0; pg < pbank.NumberPages; pg++)
		 {
			try
			{
			   pbank.writePagePacket(pg, wr_buf, 0, len);
			   Debug.WriteLine("wrote " + len + " byte packet on page " + pg);
			}
			catch (Exception e)
			{
			   Debug.WriteLine("Exception writing: " + e + "TRACE: ");
			   Debug.WriteLine(e.ToString());
			   Debug.Write(e.StackTrace);

			   return;
			}
		 }

         stopWatch.Stop();
		 Debug.WriteLine("Time to write: " + stopWatch.ElapsedMilliseconds + "ms");
         stopWatch.Reset();
	  }
   }

   //--------
   //-------- Display Methods
   //--------

   /// <summary>
   /// Display information about the 1-Wire device
   /// </summary>
   /// <param name="owd"> OneWireContainer device </param>
   internal static void printDeviceInfo(OneWireContainer owd)
   {
	  Debug.WriteLine("");
	  Debug.WriteLine("*************************************************************************");
	  Debug.WriteLine("* Device Name: " + owd.Name);
	  Debug.WriteLine("* Device Other Names: " + owd.AlternateNames);
	  Debug.WriteLine("* Device Address: " + owd.AddressAsString);
	  Debug.WriteLine("* Device Max speed: " + ((owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
	  Debug.WriteLine("* iButton Description: " + owd.Description);
   }

   /// <summary>
   /// Display the information about the current memory back provided.
   /// </summary>
   /// <param name="bank"> Memory Bank object. </param>
   public static void displayBankInformation(MemoryBank bank)
   {
	  Debug.WriteLine("|------------------------------------------------------------------------");
	  Debug.WriteLine("| Bank: (" + bank.BankDescription + ")");
	  Debug.Write("| Implements : MemoryBank");

	  if (bank is PagedMemoryBank)
	  {
		 Debug.Write(", PagedMemoryBank");
	  }

	  if (bank is OTPMemoryBank)
	  {
		 Debug.Write(", OTPMemoryBank");
	  }

	  Debug.WriteLine("");
	  Debug.WriteLine("| Size " + bank.Size + " starting at physical address " + bank.StartPhysicalAddress);
	  Debug.Write("| Features:");

	  if (bank.ReadWrite)
	  {
		 Debug.Write(" Read/Write");
	  }

	  if (bank.WriteOnce)
	  {
		 Debug.Write(" Write-once");
	  }

	  if (bank.ReadOnly)
	  {
		 Debug.Write(" Read-only");
	  }

	  if (bank.GeneralPurposeMemory)
	  {
		 Debug.Write(" general-purpose");
	  }
	  else
	  {
		 Debug.Write(" not-general-purpose");
	  }

	  if (bank.NonVolatile)
	  {
		 Debug.Write(" non-volatile");
	  }
	  else
	  {
		 Debug.Write(" volatile");
	  }

	  if (bank.needsProgramPulse())
	  {
		 Debug.Write(" needs-program-pulse");
	  }

	  if (bank.needsPowerDelivery())
	  {
		 Debug.Write(" needs-power-delivery");
	  }

	  // check if has paged services
	  if (bank is PagedMemoryBank)
	  {

		 // caste to page bank
		 PagedMemoryBank pbank = (PagedMemoryBank) bank;

		 // page info
		 Debug.WriteLine("");
		 Debug.Write("| Pages: " + pbank.NumberPages + " pages of length ");
		 Debug.Write(pbank.PageLength + " bytes ");

		 if (bank.GeneralPurposeMemory)
		 {
			Debug.Write("giving " + pbank.MaxPacketDataLength + " bytes Packet data payload");
		 }

		 if (pbank.hasPageAutoCRC())
		 {
			Debug.WriteLine("");
			Debug.Write("| Page Features: page-device-CRC");
		 }

		 // check if has OTP services
		 if (pbank is OTPMemoryBank)
		 {

			// caste to OTP bank
			OTPMemoryBank ebank = (OTPMemoryBank) pbank;

			if (ebank.canRedirectPage())
			{
			   Debug.Write(" pages-redirectable");
			}

			if (ebank.canLockPage())
			{
			   Debug.Write(" pages-lockable");
			}

			if (ebank.canLockRedirectPage())
			{
			   Debug.Write(" redirection-lockable");
			}
		 }

		 if (pbank.hasExtraInfo())
		 {
			Debug.WriteLine("");
			Debug.WriteLine("| Extra information for each page:  " + pbank.ExtraInfoDescription + ", length " + pbank.ExtraInfoLength);
		 }
		 else
		 {
			Debug.WriteLine("");
		 }
	  }
	  else
	  {
		 Debug.WriteLine("");
	  }

	  Debug.WriteLine("|------------------------------------------------------------------------");
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
		 if ((dataBuf [i + offset] & 0x000000FF) < 0x00000010)
		 {
			Debug.Write("0");
			Debug.Write(((int) dataBuf [i + offset] & 0x0000000F).ToString("x").ToUpper());
		 }
		 else
		 {
			Debug.Write(((int) dataBuf [i + offset] & 0x000000FF).ToString("x").ToUpper());
		 }
	  }
   }

   /// <summary>
   /// parse byte string into a byte array
   /// </summary>
   /// <param name="str">  String to parse
   /// </param>
   /// <returns> byte array of data. </returns>
   internal static byte[] parseByteString(string str)
   {
	  // data are entered in "xx xx xx xx" format
	  string dataStr = str.Trim();
      string[] strtok = dataStr.Split(new char[] { ' ' });

      byte[] data = new byte[strtok.Length];
      for(var i = 0; i < data.Length; i++)
      {
         data[i] = byte.Parse(strtok[i], System.Globalization.NumberStyles.HexNumber);
      }

	  return data;
   }

   /// <summary>
   /// convert input to hexidecimal value
   /// </summary>
   /// <param name="c">  hex char to convert
   /// </param>
   /// <returns> int representation of hex character </returns>
   internal static int hexDigitValue(char c)
   {
	  int value = Character.digit(c, 16);


	  if (value == -1)
	  {
		 throw new IndexOutOfRangeException("Invalid Hex value: " + c);
	  }

	  return value;
   }

   //--------
   //-------- Menus
   //--------
   internal static readonly string[] mainMenu = new string[] {"MainMenu 1-Wire Memory Demo", "(0) Select Device", "(1) Test mode", "(2) Quit"};
   internal const int MAIN_SELECT_DEVICE = 0;
   internal const int MAIN_TEST = 1;
   internal const int MAIN_QUIT = 2;
   internal static readonly string[] bankMenu = new string[] {"Bank Operation Menu", "(0) Get Bank information", "(1) Read Block", "(2) Read Page", "(3) Read Page UDP packet", "(4) Write Block", "(5) Write UDP packet", "(6) GOTO MemoryBank Menu", "(7) GOTO MainMenu"};
   internal const int BANK_INFO = 0;
   internal const int BANK_READ_BLOCK = 1;
   internal const int BANK_READ_PAGE = 2;
   internal const int BANK_READ_UDP = 3;
   internal const int BANK_WRITE_BLOCK = 4;
   internal const int BANK_WRITE_UDP = 5;
   internal const int BANK_NEW_BANK = 6;
   internal const int BANK_MAIN_MENU = 7;
   internal static readonly string[] bankPswdMenu = new string[] {"Bank Password Operation Menu", "(0) Get Bank information", "(1) Read Block", "(2) Read Page", "(3) Read Page UDP packet", "(4) Write Block", "(5) Write UDP packet", "(6) Set Read-Only Password", "(7) Set Read/Write password", "(8) Set Write Only Password", "(9) Set Container Read-Only Password", "(10) Set Container Read/Write Password", "(11) Set Container Write-Only Password", "(12) Enable Device Passwords", "(13) Disable Device Passwords", "(14) GOTO MemoryBank Menu", "(15) GOTO MainMenu"};
   internal const int BANK_READONLY_PASS = 6;
   internal const int BANK_READWRIT_PASS = 7;
   internal const int BANK_WRITEONLY_PASS = 8;
   internal const int BANK_BUS_RO_PASS = 9;
   internal const int BANK_BUS_RW_PASS = 10;
   internal const int BANK_BUS_WO_PASS = 11;
   internal const int BANK_ENABLE_PASS = 12;
   internal const int BANK_DISABLE_PASS = 13;
   internal const int BANK_NEW_PASS_BANK = 14;
   internal const int BANK_NEW_MAIN_MENU = 15;

   internal static readonly string[] testMenu = new string[] {"TestMode, for all general-purpose MemoryBanks", "(0) Write entire bank with same value", "(1) Write UDP packets to all pages", "(2) Read all banks", "(3) Read UDP packets on all pages", "(4) Quit"};
   internal const int TEST_WRITE_BLOCK = 0;
   internal const int TEST_WRITE_PKTS = 1;
   internal const int TEST_READ_RAW = 2;
   internal const int TEST_READ_PKTS = 3;
   internal const int TEST_QUIT = 4;
   internal static readonly string[] modeMenu = new string[] {"Data Entry Mode", "(0) Text (single line)", "(1) Hex (XX XX XX XX ...)"};
   internal const int MODE_TEXT = 0;
   internal const int MODE_HEX = 1;
}
