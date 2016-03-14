using System;
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


/// <summary>
/// 1-Wire memory Dump demo.
/// 
///  @version    0.01, 15 December 2000
///  @author     DS
/// </summary>
public class OWDump1
{

   /// <summary>
   /// Main for OWDump
   /// </summary>
   public static void Main1(string[] args)
   {
	  Debug.WriteLine("");
	  Debug.WriteLine("OneWire Memory Dump console application: Version 0.01");
	  Debug.WriteLine("");

	  // check for correct command line parameters
	  bool argsOK = false;

	  if ((args.Length >= 1) && (args.Length <= 2))
	  {
		 argsOK = true;

		 // check for valid flag
		 if ((args [0].IndexOf("r", StringComparison.Ordinal) == -1) && (args [0].IndexOf("k", StringComparison.Ordinal) == -1) && (args [0].IndexOf("p", StringComparison.Ordinal) == -1))
		 {
			Debug.WriteLine("Unsupported flag: " + args [0]);

			argsOK = false;
		 }
	  }

	  if (!argsOK)
	  {
		 Debug.WriteLine("");
		 Debug.WriteLine("syntax: OWDump ('r' 'p' 'k') <TIME_TEST>");
		 Debug.WriteLine("   Dump an iButton/1-Wire Device's memory contents");
		 Debug.WriteLine("   'r' 'p' 'k' - required flag: (Raw,Page,pacKet) type dump");
		 Debug.WriteLine("   <TIME_TEST> - optional flag if present will time each read ");
		 Debug.WriteLine("                 of the memory banks and not display the contents");
		 return;
	  }

	  try
	  {

		 // get the default adapter  
		 DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;

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

		 // clear any previous search restrictions
		 adapter.setSearchAllDevices();
		 adapter.targetAllFamilies();
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the iButtons found
		 for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {

			// get the next owd
			OneWireContainer owd = (OneWireContainer) owd_enum.Current;

			Debug.WriteLine("");
			Debug.WriteLine("*************************************************************************");
			Debug.WriteLine("* 1-Wire Device Name: " + owd.Name);
			Debug.WriteLine("* 1-Wire Device Other Names: " + owd.AlternateNames);
			Debug.WriteLine("* 1-Wire Device Address: " + owd.AddressAsString);
			Debug.WriteLine("* 1-Wire Device Max speed: " + ((owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE) ? "Overdrive" : "Normal"));
			Debug.WriteLine("* 1-Wire Device Description: " + owd.Description);

			// set owd to max possible speed with available adapter, allow fall back
			if (adapter.canOverdrive() && (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
			{
			   owd.setSpeed(owd.MaxSpeed, true);
			}

			// dump raw contents of all memory banks
			if (args [0].IndexOf("r", StringComparison.Ordinal) != -1)
			{
			   dumpDeviceRaw(owd, (args.Length == 1));
			}

			// dump page packets of non-volatile general-purpose memory banks
			else if (args [0].IndexOf("k", StringComparison.Ordinal) != -1)
			{
			   dumpDevicePackets(owd, (args.Length == 1));
			}

			// dump pages of memory bank 
			else if (args [0].IndexOf("p", StringComparison.Ordinal) != -1)
			{
			   dumpDevicePages(owd, (args.Length == 1));
			}
			else
			{
			   Debug.WriteLine("No action taken, unsupported flag");
			}
		 }

		 // end exclusive use of adapter
		 adapter.endExclusive();

		 // free the port used by the adapter
		 Debug.WriteLine("Releasing adapter port");
		 adapter.freePort();
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine("Exception: " + e);
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
	  }

	  Debug.WriteLine("");
	  return;
   }

   /// <summary>
   /// Dump all of the 1-Wire readable memory in the provided
   /// MemoryContainer instance.
   /// 
   /// @parameter owd device to check for memory banks.
   /// @parameter showContents flag to indicate if the memory bank contents will
   ///                      be displayed
   /// </summary>
   public static void dumpDeviceRaw(OneWireContainer owd, bool showContents)
   {
	  byte[] read_buf;
	  bool found_bank = false;
	  int i , reps = 10;
      Stopwatch stopWatch = new Stopwatch();

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

		 try
		 {
			read_buf = new byte [bank.Size];

			// get overdrive going so not a factor in time tests
			bank.read(0, false, read_buf, 0, 1);

			// dynamically change number of reps
			reps = 1500 / read_buf.Length;

			if (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE)
			{
			   reps *= 2;
			}

			if ((reps == 0) || showContents)
			{
			   reps = 1;
			}

			if (!showContents)
			{
			   Debug.Write("[" + reps + "]");
			}

			// start timer to time the dump of the bank contents
            stopWatch.Start();

			// read the entire bank
			for (i = 0; i < reps; i++)
			{
			   bank.read(0, false, read_buf, 0, bank.Size);
			}

            stopWatch.Stop();

			Debug.WriteLine("     (time to read RAW = " + (stopWatch.ElapsedMilliseconds / reps).ToString() + "ms)");

			if (showContents)
			{
			   hexPrint(read_buf, 0, bank.Size);
			   Debug.WriteLine("");
			}
		 }
		 catch (Exception e)
		 {
			Debug.WriteLine("Exception in reading raw: " + e + "  TRACE: ");
			Debug.WriteLine(e.ToString());
			Debug.Write(e.StackTrace);
		 }
	  }

	  if (!found_bank)
	  {
		 Debug.WriteLine("XXXX Does not contain any memory bank's");
	  }
   }

   /// <summary>
   /// Dump valid memory packets from general-purpose memory.
   /// in the provided  MemoryContainer instance.
   /// 
   /// @parameter owd device to check for memory banks.
   /// @parameter showContents flag to indicate if the packet memory bank contents will
   ///                      be displayed
   /// </summary>
   public static void dumpDevicePackets(OneWireContainer owd, bool showContents)
   {
	  byte[] read_buf, extra_buf;
	  int read_rslt;
	  bool found_bank = false;
      Stopwatch stopWatch = new Stopwatch();

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

				  if (showContents)
				  {
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
			   Debug.WriteLine(e.ToString());
			   Debug.Write(e.StackTrace);

			   readContinue = false;
			}
		 }

		 stopWatch.Stop();

		 Debug.WriteLine("     (time to read PACKETS = " + stopWatch.ElapsedMilliseconds + "ms)");
	  }

	  if (!found_bank)
	  {
		 Debug.WriteLine("XXXX Does not contain any general-purpose non-volatile page memory bank's");
	  }
   }

   /// <summary>
   /// Dump pages from memory.
   /// in the provided owd instance.
   /// 
   /// @parameter owd device to check for memory banks.
   /// @parameter showContents flag to indicate if the packet memory bank contents will
   ///                      be displayed
   /// </summary>
   public static void dumpDevicePages(OneWireContainer owd, bool showContents)
   {
	  byte[] read_buf, extra_buf;
	  int reps, i, pg, numberPages;
	  bool found_bank = false, hasExtraInfo , hasPageAutoCRC , readContinue ;
      Stopwatch stopWatch = new Stopwatch();

	  // get the port names we can use and try to open, test and close each
	  for (System.Collections.IEnumerator bank_enum = owd.MemoryBanks; bank_enum.MoveNext();)
	  {

		 // get the next memory bank
		 MemoryBank mb = (MemoryBank) bank_enum.Current;

		 // check if has paged services
		 if (!(mb is PagedMemoryBank))
		 {
			continue;
		 }

		 // cast to page bank 
		 PagedMemoryBank bank = (PagedMemoryBank) mb;

		 // found a memory bank
		 found_bank = true;

		 // display bank information
		 displayBankInformation(bank);

		 read_buf = new byte [bank.PageLength];
		 extra_buf = new byte [bank.ExtraInfoLength];

		 // get bank flags
		 hasPageAutoCRC = bank.hasPageAutoCRC();
		 hasExtraInfo = bank.hasExtraInfo();
		 numberPages = bank.NumberPages;

		 // get overdrive going so not a factor in time tests
		 try
		 {
			bank.read(0, false, read_buf, 0, 1);
		 }
		 catch (Exception)
		 {
		 }

		 // dynamically change number of reps
		 reps = 1000 / (read_buf.Length * bank.NumberPages);

		 if (owd.MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE)
		 {
			reps *= 2;
		 }

		 if ((reps == 0) || showContents)
		 {
			reps = 1;
		 }

		 if (!showContents)
		 {
			Debug.Write("[" + reps + "]");
		 }

		 // start timer to time the dump of the bank contents         
         stopWatch.Start();

		 for (i = 0; i < reps; i++)
		 {

			// loop to read all of the pages in bank
			readContinue = false;

			for (pg = 0; pg < numberPages; pg++)
			{
			   try
			   {

				  // read a page (use the most verbose and secure method)
				  if (hasPageAutoCRC)
				  {
					 if (hasExtraInfo)
					 {
						bank.readPageCRC(pg, readContinue, read_buf, 0, extra_buf);
					 }
					 else
					 {
						bank.readPageCRC(pg, readContinue, read_buf, 0);
					 }
				  }
				  else
				  {
					 if (hasExtraInfo)
					 {
						bank.readPage(pg, readContinue, read_buf, 0, extra_buf);
					 }
					 else
					 {
						bank.readPage(pg, readContinue, read_buf, 0);
					 }
				  }

				  readContinue = true;

				  if (showContents)
				  {
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
			   }
			   catch (Exception e)
			   {
				  Debug.WriteLine("Exception in reading page: " + e + "TRACE: ");
				  Debug.WriteLine(e.ToString());
				  Debug.Write(e.StackTrace);

				  readContinue = false;
			   }
			}
		 }

         stopWatch.Stop();

		 Debug.WriteLine("     (time to read PAGES = " + (stopWatch.ElapsedMilliseconds / reps).ToString() + "ms)");
	  }

	  if (!found_bank)
	  {
		 Debug.WriteLine("XXXX Does not contain any general-purpose non-volatile page memory bank's");
	  }
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
}
