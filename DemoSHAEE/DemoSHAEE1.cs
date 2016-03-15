using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999 Dallas Semiconductor Corporation, All Rights Reserved.
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

/// <summary>
/// Console application to utilize the MemoryBank features of the
/// OneWireContainer33 to write blocks and packets and use different
/// features of the part.
/// 
/// History:
/// 
/// @version    0.00, 19 Dec 2000
/// @author     DS
/// </summary>

public class DemoSHAEE1
{
   public static void Main1(string[] args)
   {
	  byte[] data = new byte[8];
	  byte[] sn = new byte[8];
	  byte[] secret = new byte[8];
	  byte[] partialsec = new byte[8];
	  byte[] memory = new byte[32];
	  byte[] indata = new byte[32];
	  byte[] hexstr = new byte[32];
	  byte[] family = new byte[2];
	  byte[] extra_info = new byte[20];
	  ArrayList owd_vect = new ArrayList(5);
	  int i;
	  int page , addr , len ;
	  int add;
	  MemoryBank bank;
	  DSPortAdapter adapter = null;
	  OneWireContainer33 owd;
	  bool done = false;
	  bool donebank = false;

	  family[0] = 0x33;
	  family[1] = 0xB3;

	  for (i = 0; i < 8; i++)
	  {
		 secret[i] = 0xFF;
		 data[i] = 0xFF;
		 partialsec[i] = 0x00;
	  }

      Stream stream = loadResourceFile("DemoSHAEE.input.txt");
      dis = new StreamReader(stream);

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 adapter.targetFamily(family);
		 owd_vect = findAllDevices(adapter);
		 // select a device
		 owd = selectDevice(owd_vect);

		 sn = owd.Address;

		 do
		 {
			// Main menu
			switch (menuSelect(mainMenu))
			{

			   case QUIT :
				  done = true;
				  break;

			   case PRNT_BUS_SECRET: // Print Bus Master Secret
				  Debug.WriteLine("");
				  Debug.WriteLine("The Current Bus Master Secret is:");
				  hexPrint(secret,0,8);
				  Debug.WriteLine("");
				  break;

			   case RD_WR_MEM: // Read/Write Memory of Bank
				  // select a bank
				  bank = selectBank(owd);

				  // display bank information
				  displayBankInformation(bank);

				  // loop on bank menu
				  do
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
							  dumpBankPage(owd, (PagedMemoryBank) bank, page);
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

						case BANK_WRITE_BLOCK : // write a block
						   Debug.Write("Enter the address to start writing: ");

						   addr = getNumber(0, bank.Size - 1);
						   data = getData(false);

						   bankWriteBlock(bank, data, addr);
						   break;

						case BANK_WRITE_UDP : // write a packet
						   if (!(bank is PagedMemoryBank))
						   {
							  Debug.Write("Bank is not a 'PagedMemoryBank'");
						   }
						   else
						   {
							  Debug.Write("Enter the page number to write a UDP to: ");

							  page = getNumber(0, ((PagedMemoryBank) bank).NumberPages - 1);
							  data = getData(false);

							  bankWritePacket((PagedMemoryBank) bank, data, page);
						   }
						   break;

						case BANK_NEW_BANK : // select a bank
						   bank = selectBank(owd);
						   break;

						case BANK_MAIN_MENU :
						   donebank = true;
						   break;
					 }
				  } while (!donebank);
				  donebank = false;
				  break;

			   case FIRST_SECRET: // Load First Secret
				  Debug.WriteLine("");
				  Debug.WriteLine("Enter the 8 bytes of data to be written.");
				  Debug.WriteLine("AA AA AA AA AA AA AA AA  <- Example");

				  // get secret data
				  data = getData(true);

				  if (owd.loadFirstSecret(data, 0))
				  {
					 Debug.WriteLine("First Secret was Loaded.");
					 Array.Copy(data, 0, secret, 0, 8);
				  }

				  break;

			   case COMPUTE_NEXT: // Compute Next Secret
				  Debug.WriteLine("");
				  Debug.WriteLine("Enter the address for the page you want to calculate the next secret with");

				  // reading in address
				  add = getNumber(0,128);

				  Debug.WriteLine("");
				  Debug.WriteLine("Enter the 8 byte partial secret.");

				  // reading the partial secret
				  partialsec = getData(true);

				  // computing next secret
				  owd.computeNextSecret(add, partialsec, 0);

				  Debug.WriteLine("");
				  Debug.WriteLine("Next Secret Computed");
				  Debug.WriteLine("");

				  break;

			   case NEW_BUS_SECRET: // Change Bus Master Secret
				  Debug.WriteLine("");
				  Debug.WriteLine("Enter the 8 bytes of data to be written.");
				  Debug.WriteLine("AA AA AA AA AA AA AA AA  <- Example");

				  // get secret data
				  data = getData(true);

				  Array.Copy(data, 0, secret, 0, 8);

				  Debug.WriteLine("Bus Master Secret Changed.");

				  owd.setContainerSecret(data, 0);

				  break;

			   case LOCK_SECRET: // Lock Secret
				  owd.writeProtectSecret();
				  Debug.WriteLine("Secret Locked.");

				  break;

			   case NEW_CHALLENGE: // New Challenge to Input
				  Debug.WriteLine("");
				  Debug.WriteLine("Enter 8 bytes for the challenge");
				  Debug.WriteLine("AA AA AA AA AA AA AA AA  <- Example");

				  // get the challenge
				  data = getData(true);

				  owd.setChallenge(data, 0);

				  break;

			   case WR_PRO_0THR3: // Write-protect pages 0 to 3
				  owd.writeProtectAll();
				  Debug.WriteLine("Pages 0 to 3 write protected.");

				  break;

			   case PAGE_1_EEPROM: // Set page 1 to EPROM mode
				  owd.setEPROMModePageOne();
				  Debug.WriteLine("EPROM mode control activated for page 1.");

				  break;

			   case WR_PRO_0: // Write protect page 0
				  owd.writeProtectPageZero();
				  Debug.WriteLine("Page 0 Write-protected.");

				  break;

			   default:
				  break;
			}

		 }while (!done);

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

   private static StreamReader dis = null;

    /// <summary>
    /// Loads resource file to be used as Input Stream to drive program
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Stream loadResourceFile(string file)
    {
        try
        {
            Assembly asm = typeof(DemoSHAEE.MainPage).GetTypeInfo().Assembly;
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
   public static byte[] getData(bool eight_bytes)
   {
	  byte[] data = null;
	  bool got_data = false;
	  byte[] zero = new byte[] { 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00 };

	  if (menuSelect(modeMenu) == MODE_TEXT)
	  {
		 if (!eight_bytes)
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
			   string tstr = getString(2);
    
			   data = parseByteString(tstr);

			   if (data.Length > 8)
			   {
			        Debug.WriteLine("Entry is too long, must be 8 bytes of data or less.");
			   }
			} while (data.Length > 8);

			if (data.Length < 8)
			{
			   Array.Copy(data,0,zero,0,data.Length);
			   data = zero;
			}
		 }
	  }
	  else
	  {
		 if (eight_bytes)
		 {
			do
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

			   if (data.Length > 8)
			   {
				  Debug.WriteLine("Entry is too long, must be 8 bytes of data or less.");
			   }
			} while (data.Length > 8);

			if (data.Length < 8)
			{
			   Array.Copy(data,0,zero,0,data.Length);
			   data = zero;
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
	  }

	  Debug.Write("Data to write, len (" + data.Length + ") :");
	  hexPrint(data, 0, data.Length);
	  Debug.WriteLine("");

	  return data;
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
		 Debug.WriteLine(menu [i]);
	  }

	  Debug.Write("Please enter value: ");

	  return getNumber(0, menu.Length - 2);
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
	  ArrayList banks = new ArrayList(3);
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

	  return (MemoryBank) banks[select];
   }

   /// <summary>
   /// Read a page from a memory bank and print in hex
   /// </summary>
   /// <param name="bank">  PagedMemoryBank to read a page from </param>
   /// <param name="pg">  page to read </param>
   public static void dumpBankPage(OneWireContainer33 owd, PagedMemoryBank bank, int pg)
   {
	  byte[] read_buf = new byte [bank.PageLength];
	  byte[] extra_buf = new byte [bank.ExtraInfoLength];
	  byte[] challenge = new byte [8];
	  byte[] secret = new byte [8];
	  byte[] sernum = new byte [8];
	  bool macvalid = false;

	  try
	  {

		 // read a page (use the most verbose and secure method)
		 if (bank.hasPageAutoCRC())
		 {
			Debug.WriteLine("Using device generated CRC");

			if (bank.hasExtraInfo())
			{
			   bank.readPageCRC(pg, false, read_buf, 0, extra_buf);

			   owd.getChallenge(challenge, 0);
			   owd.getContainerSecret(secret, 0);
			   sernum = owd.Address;
			   macvalid = OneWireContainer33.isMACValid(bank.StartPhysicalAddress + pg * bank.PageLength, sernum,read_buf,extra_buf,challenge,secret);
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

			if (macvalid)
			{
			   Debug.WriteLine("Data validated with correct MAC.");
			}
			else
			{
			   Debug.WriteLine("Data not validated because incorrect MAC.");
			}
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
		 adapter.Speed = DSPortAdapter.SPEED_REGULAR;

		 // enumerate through all the 1-Wire devices and collect them in a vector
		 for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
		 {
			Debug.WriteLine("one device found.");
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

   /// <summary>
   /// Create a menu from the provided OneWireContainer
   /// Vector and allow the user to select a device.
   /// </summary>
   /// <param name="owd_vect"> vector of devices to choose from
   /// </param>
   /// <returns> OneWireContainer device selected </returns>
   public static OneWireContainer33 selectDevice(ArrayList owd_vect)
   {

	  // create a menu
	  string[] menu = new string [owd_vect.Count + 2];
	  OneWireContainer owd;
	  int i;

	  menu [0] = "Device Selection";

	  for (i = 0; i < owd_vect.Count; i++)
	  {
		 owd = (OneWireContainer) owd_vect[i];
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

	  return (OneWireContainer33) owd_vect[select];
   }

   //--------
   //-------- Menus
   //--------
   internal static readonly string[] mainMenu = new string[] {"Main Menu", "(0)  Read/Write Memory Bank", "(1)  Load First Secret", "(2)  Compute Next Secret", "(3)  Change Bus Master Secret", "(4)  Lock Secret", "(5)  Input new challenge for Read Authenticate", "(6)  Write Protect page 0-3", "(7)  Set Page 1 to EEPROM mode", "(8)  Write Protect Page 0", "(9)  Print Current Bus Master Secret", "(10) Quit"};
   internal const int RD_WR_MEM = 0;
   internal const int FIRST_SECRET = 1;
   internal const int COMPUTE_NEXT = 2;
   internal const int NEW_BUS_SECRET = 3;
   internal const int LOCK_SECRET = 4;
   internal const int NEW_CHALLENGE = 5;
   internal const int WR_PRO_0THR3 = 6;
   internal const int PAGE_1_EEPROM = 7;
   internal const int WR_PRO_0 = 8;
   internal const int PRNT_BUS_SECRET = 9;
   internal const int QUIT = 10;

   internal static readonly string[] bankMenu = new string[] {"Bank Operation Menu", "(0) Get Bank information", "(1) Read Block", "(2) Read Page", "(3) Read Page UDP packet", "(4) Write Block", "(5) Write UDP packet", "(6) GOTO MemoryBank Menu", "(7) GOTO MainMenu"};
   internal const int BANK_INFO = 0;
   internal const int BANK_READ_BLOCK = 1;
   internal const int BANK_READ_PAGE = 2;
   internal const int BANK_READ_UDP = 3;
   internal const int BANK_WRITE_BLOCK = 4;
   internal const int BANK_WRITE_UDP = 5;
   internal const int BANK_NEW_BANK = 6;
   internal const int BANK_MAIN_MENU = 7;

   internal static readonly string[] modeMenu = new string[] {"Data Entry Mode", "(0) Text (single line)", "(1) Hex (XX XX XX XX ...)"};
   internal const int MODE_TEXT = 0;
   internal const int MODE_HEX = 1;
}

