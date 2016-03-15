using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

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
using System.IO;
using System.Reflection;

/// <summary>
/// Console application to utilize the MemoryBank features of the
/// OneWireContainer29 to write blocks and packets and use different
/// features of the part.
/// 
/// History:
/// 
/// @version    0.00, 19 Dec 2000
/// @author     JPE
/// </summary>
public class DS2408Demo1
{
   public static void Main1(string[] args)
   {
	  OneWireContainer29 owd;
	  byte[] data = new byte[8];
	  byte[] sn = new byte[8];
	  byte[] memory = new byte[32];
	  byte[] indata = new byte[32];
	  byte[] hexstr = new byte[32];
	  byte[] state = new byte[3];
	  byte[] register = new byte[3];
	  ArrayList owd_vect = new ArrayList(5);
	  int channel;
	  int i;
	  int addr , len ;
	  MemoryBank bank;
	  DSPortAdapter adapter = null;
	  bool stateRead = false;
	  bool registerRead = false;
	  bool done = false;
	  bool donebank = false;

	  for (i = 0;i < 8;i++)
	  {
		 sn[i] = 0x00;
	  }

      Stream stream = loadResourceFile("DS2408Demo.input.txt");
      dis = new StreamReader(stream);

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 // get exclusive use of adapter
		 adapter.beginExclusive(true);

		 adapter.targetFamily(41);
		 owd_vect = findAllDevices(adapter);

		 // select a device
		 owd = selectDevice(owd_vect);

		 do
		 {

			// Main menu
			switch (menuSelect(mainMenu))
			{

			   case QUIT :
				  done = true;
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

						case BANK_WRITE_BLOCK : // write a block
						   Debug.Write("Enter the address to start writing: ");

						   addr = getNumber(0, bank.Size - 1);
						   data = getData(false);

						   bankWriteBlock(bank, data, addr);
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

			   case GET_LEVEL: // Get level for certain channel
				  if (!stateRead)
				  {
					 state = owd.readDevice();
					 stateRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to check the level on?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  if (owd.getLevel(channel,state))
				  {
					 Debug.WriteLine("The level sensed on channel " + channel + " is high.");
				  }
				  else
				  {
					 Debug.WriteLine("The level sensed on channel " + channel + " is low.");
				  }

				  break;

			   case GET_LATCH_STATE: // Get Latch State
				  if (!stateRead)
				  {
					 state = owd.readDevice();
					 stateRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to check the latch state on?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  if (owd.getLatchState(channel,state))
				  {
					 Debug.WriteLine("The state of the latch on channel " + channel + " is on.");
				  }
				  else
				  {
					 Debug.WriteLine("The state of the latch on channel " + channel + " is off.");
				  }

				  break;

			   case GET_SENSED_ACTIVITY: // Get sensed activity
				  if (!stateRead)
				  {
					 state = owd.readDevice();
					 stateRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to check for activity?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  if (owd.getSensedActivity(channel,state))
				  {
					 Debug.WriteLine("Activity was detected on channel " + channel);
				  }
				  else
				  {
					 Debug.WriteLine("No activity was detected on channel " + channel);
				  }

				  break;

			   case CLEAR_ACTIVITY: // Clear Activity
				  owd.clearActivity();
				  break;

			   case SET_LATCH_STATE_ON: // Set latch state on
				  if (!stateRead)
				  {
					 state = owd.readDevice();
					 stateRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to set on?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setLatchState(channel,true,false,state);
				  owd.writeDevice(state);

				  break;

			   case SET_LATCH_STATE_OFF: // Set latch state off
				  if (!stateRead)
				  {
					 state = owd.readDevice();
					 stateRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to turn off?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setLatchState(channel,false,false,state);
				  owd.writeDevice(state);

				  break;

			   case CURRENT_STATE: // Current state
				  state = owd.readDevice();
				  stateRead = true;

				  Debug.WriteLine("The following is the current level of the channels.");
				  for (i = 0;i < 8;i++)
				  {
					 if (owd.getLevel(i,state))
					 {
						Debug.WriteLine(i + " is high.");
					 }
					 else
					 {
						Debug.WriteLine(i + " is low.");
					 }
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("The following are the latch states of the channels.");
				  for (i = 0;i < 8;i++)
				  {
					 if (owd.getLatchState(i,state))
					 {
						Debug.WriteLine(i + " is on.");
					 }
					 else
					 {
						Debug.WriteLine(i + " is off.");
					 }
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("The following is the activity of the channels.");
				  for (i = 0;i < 8;i++)
				  {
					 if (owd.getSensedActivity(i,state))
					 {
						Debug.WriteLine(i + " Activity");
					 }
					 else
					 {
						Debug.WriteLine(i + " No activity");
					 }
				  }

				  break;

			   case SET_RESET_ON:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.setResetMode(register,true);
				  owd.writeRegister(register);

				  break;

			   case SET_RESET_OFF:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.setResetMode(register,false);
				  owd.writeRegister(register);

				  break;

			   case GET_VCC:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  if (owd.getVCC(register))
				  {
					 Debug.WriteLine("VCC is powered with 5V.");
				  }
				  else
				  {
					 Debug.WriteLine("VCC is grounded.");
				  }

				  break;

			   case CLEAR_POWER_ON_RESET:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.clearPowerOnReset(register);
				  owd.writeRegister(register);

				  break;

			   case OR_CONDITIONAL_SEARCH:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.orConditionalSearch(register);
				  owd.writeRegister(register);

				  break;

			   case AND_CONDITIONAL_SEARCH:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.andConditionalSearch(register);
				  owd.writeRegister(register);

				  break;

			   case PIO_CONDITIONAL_SEARCH:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.pioConditionalSearch(register);
				  owd.writeRegister(register);

				  break;

			   case ACTIVITY_CONDITIONAL_SEARCH:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  owd.activityConditionalSearch(register);
				  owd.writeRegister(register);

				  break;

			   case SET_CHANNEL_MASK:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to set on?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setChannelMask(channel,true,register);
				  owd.writeRegister(register);

				  break;

			   case UNSET_CHANNEL_MASK:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to turn off?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setChannelMask(channel,false,register);
				  owd.writeRegister(register);

				  break;

			   case SET_CHANNEL_POLARITY:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to set the polarity on?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setChannelPolarity(channel,true,register);
				  owd.writeRegister(register);

				  break;

			   case UNSET_CHANNEL_POLARITY:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to turn off the polarity?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  owd.setChannelPolarity(channel,false,register);
				  owd.writeRegister(register);

				  break;

			   case GET_CHANNEL_MASK:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to check?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  if (owd.getChannelMask(channel,register))
				  {
					 Debug.WriteLine("Channel " + channel + " is masked.");
				  }
				  else
				  {
					 Debug.WriteLine("Channel " + channel + " is not masked.");
				  }

				  break;

			   case GET_CHANNEL_POLARITY:
				  if (!registerRead)
				  {
					 register = owd.readRegister();
					 registerRead = true;
				  }

				  Debug.WriteLine("");
				  Debug.WriteLine("Which channel would you like to check?");
				  Debug.WriteLine("");

				  channel = getNumber(0,7);

				  if (owd.getChannelPolarity(channel,register))
				  {
					 Debug.WriteLine("Channel " + channel + " polarity is set.");
				  }
				  else
				  {
					 Debug.WriteLine("Channel " + channel + " polarity is not set.");
				  }

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
           Assembly asm = typeof(DS2408Demo.MainPage).GetTypeInfo().Assembly;
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
	  byte[] zero = new byte[] {0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00};

	  if (menuSelect(modeMenu) == MODE_TEXT)
	  {
		 if (!eight_bytes)
		 {
			string tstr = getString(1);

			data = Encoding.UTF8.GetBytes(tstr);
		 }
		 else
		 {
			do
			{
			   string tstr = getString(1);

			   data = Encoding.UTF8.GetBytes(tstr);

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
   /// Create a menu from the provided OneWireContainer
   /// Vector and allow the user to select a device.
   /// </summary>
   /// <param name="owd_vect"> vector of devices to choose from
   /// </param>
   /// <returns> OneWireContainer device selected </returns>
   public static OneWireContainer29 selectDevice(ArrayList owd_vect)
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

	  return (OneWireContainer29) owd_vect[select];
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
	  int dataLen = dataStr.Length;
	  byte[] buf = new byte [dataLen];
	  int bufLen = 0;
	  int curPos = 0;
	  int savedPos = 0;
	  int count = 0;
	  char c;

	  while (curPos < dataLen)
	  {
		 c = dataStr[curPos];

		 if (!char.IsWhiteSpace(c))
		 {
			savedPos = curPos;
			count = 1;

			while ((curPos < dataLen - 1) && (!char.IsWhiteSpace(dataStr[++curPos])))
			{
			   count++;
			}

			if (count > 2)
			{
			   throw new IndexOutOfRangeException("Invalid Byte String: " + str);
			}

			if (curPos != dataLen - 1)
			{
			   curPos--;
			}

			if (count == 1) // only 1 digit entered
			{
			   buf [bufLen++] = (byte) hexDigitValue(c);
			}
			else
			{
			   buf [bufLen++] = (byte)((hexDigitValue(c) << 4) | (byte) hexDigitValue(dataStr[curPos]));
			}
		 } // if

		 curPos++;
	  } // while

	  byte[] data = new byte [bufLen];

	  Array.Copy(buf, 0, data, 0, bufLen);

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

   //--------
   //-------- Menus
   //--------
   internal static readonly string[] mainMenu = new string[] {"Main Menu", "(0)  Read/Write Memory Bank", "(1)  Get Channel Level", "(2)  Get Latch State", "(3)  Get Sensed Activity", "(4)  Clear Activity", "(5)  Set Latch State On", "(6)  Set Latch State Off", "(7)  The current state of the DS2408", "(8)  Set the reset pin", "(9)  Turn off the reset pin", "(10) Get VCC", "(11) Clear Power On Reset", "(12) Set 'OR' for Conditional Search", "(13) Set 'AND' for Conditional Search", "(14) Set PIO as input for Conditional Search", "(15) Set Activity latches as input for Conditional Search", "(16) Set Channel Mask", "(17) Turn off Channel Mask", "(18) Set Channel Polarity", "(19) Turn off Channel Polarity", "(20) Get Channel Mask", "(21) Get Channel Polarity", "(22) Quit"};
   internal const int RD_WR_MEM = 0;
   internal const int GET_LEVEL = 1;
   internal const int GET_LATCH_STATE = 2;
   internal const int GET_SENSED_ACTIVITY = 3;
   internal const int CLEAR_ACTIVITY = 4;
   internal const int SET_LATCH_STATE_ON = 5;
   internal const int SET_LATCH_STATE_OFF = 6;
   internal const int CURRENT_STATE = 7;
   internal const int SET_RESET_ON = 8;
   internal const int SET_RESET_OFF = 9;
   internal const int GET_VCC = 10;
   internal const int CLEAR_POWER_ON_RESET = 11;
   internal const int OR_CONDITIONAL_SEARCH = 12;
   internal const int AND_CONDITIONAL_SEARCH = 13;
   internal const int PIO_CONDITIONAL_SEARCH = 14;
   internal const int ACTIVITY_CONDITIONAL_SEARCH = 15;
   internal const int SET_CHANNEL_MASK = 16;
   internal const int UNSET_CHANNEL_MASK = 17;
   internal const int SET_CHANNEL_POLARITY = 18;
   internal const int UNSET_CHANNEL_POLARITY = 19;
   internal const int GET_CHANNEL_MASK = 20;
   internal const int GET_CHANNEL_POLARITY = 21;
   internal const int QUIT = 22;

   internal static readonly string[] bankMenu = new string[] {"Bank Operation Menu", "(0) Get Bank information", "(1) Read Block", "(2) Write Block", "(3) GOTO MemoryBank Menu", "(4) GOTO MainMenu"};
   internal const int BANK_INFO = 0;
   internal const int BANK_READ_BLOCK = 1;
   internal const int BANK_WRITE_BLOCK = 2;
   internal const int BANK_NEW_BANK = 3;
   internal const int BANK_MAIN_MENU = 4;

   internal static readonly string[] modeMenu = new string[] {"Data Entry Mode", "(0) Text (single line)", "(1) Hex (XX XX XX XX ...)"};
   internal const int MODE_TEXT = 0;
   internal const int MODE_HEX = 1;
}

