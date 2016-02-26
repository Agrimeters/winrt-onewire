using System;
using System.Collections;
using System.Diagnostics;

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

namespace com.dalsemi.onewire.application.file
{

	// imports
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;
	using PagedMemoryBank = com.dalsemi.onewire.container.PagedMemoryBank;
	using MemoryBank = com.dalsemi.onewire.container.MemoryBank;
	using OTPMemoryBank = com.dalsemi.onewire.container.OTPMemoryBank;
	using CRC16 = com.dalsemi.onewire.utils.CRC16;
	using Bit = com.dalsemi.onewire.utils.Bit;


	/// <summary>
	/// Class to provide read/write cache services to a 1-Wire memory
	/// device.  Writes are only performed when this classes <code>sync()</code>
	/// method is called.  Provides page bitmap services for OTP devices.
	/// 
	/// <para>Objectives:
	/// <ul>
	/// <li> Cache read/written pages
	/// <li> write only on sync()
	/// <li> write order is oldest to newest.
	/// <li> Collect redirection information when appropriate
	/// </ul>
	/// 
	/// @author  DS
	/// @version 0.01, 1 June 2001
	/// </para>
	/// </summary>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFile </seealso>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFileInputStream </seealso>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFileOutputStream </seealso>
	internal class MemoryCache
	{

	   //--------
	   //-------- Static Final Variables
	   //--------

	   /// <summary>
	   /// cache pageState's </summary>
	   private const int NOT_READ = 0;
	   private const int READ_CRC = 1;
	   private const int READ_NO_CRC = 2;
	   private const int VERIFY = 3;
	   private const int REDIRECT = 4;
	   private const int WRITE = 5;

	   /// <summary>
	   /// Flag to indicate the writeLog entry is empty </summary>
	   private const int EMPTY = -1;

	   /// <summary>
	   /// Field NONE - flag to indicate last page read is not known </summary>
	   private const int NONE = -100;

	   /// <summary>
	   /// Field USED - flag to indicate page bitmap file used </summary>
	   private const int USED = 0;

	   /// <summary>
	   /// Field NOT_USED - flag to indicated page bitmap file un-used </summary>
	   private const int NOT_USED = 1;

	   /// <summary>
	   /// Enable/disable debug messages </summary>
	   private const bool doDebugMessages = true;

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Field owd - 1-Wire container that containes this memory to cache </summary>
	   private OneWireContainer[] owd;

	   /// <summary>
	   /// Field cache -  2 dimentional array to contain the cache </summary>
	   private sbyte[][] cache;

	   /// <summary>
	   /// Field len - array of lengths of packets found </summary>
	   private int[] len;

	   /// <summary>
	   /// Field pageState - array of flags to indicate the page has been changed </summary>
	   private int[] pageState;

	   /// <summary>
	   /// Field banks - vector of memory banks that contain the Filesystem </summary>
	   private ArrayList banks;

	   /// <summary>
	   /// Field totalPages - total pages in this Filesystem </summary>
	   private int totalPages;

	   /// <summary>
	   /// Field lastPageRead - last page read by this cache </summary>
	   private int lastPageRead;

	   /// <summary>
	   /// Field maxPacketDataLength - maximum data length on a page </summary>
	   private int maxPacketDataLength;

	   /// <summary>
	   /// Field bankPages - array of the number of pages in vector of memory banks </summary>
	   private int[] bankPages;

	   /// <summary>
	   /// Field startPages - array of the number of start pages for device list </summary>
	   private int[] startPages;

	   /// <summary>
	   /// Field writeLog - array to track the order of pages written to the cache </summary>
	   private int[] writeLog;

	   /// <summary>
	   /// Field tempExtra - temporary buffer used to to read the extra information from a page read </summary>
	   private sbyte[] tempExtra;

	   /// <summary>
	   /// Field tempPage - temporary buffer the size of a page </summary>
	   private sbyte[] tempPage;

	   /// <summary>
	   /// Field redirect - array of redirection bytes </summary>
	   private int[] redirect;

	   /// <summary>
	   /// Field owners - vector of classes that are using this cache </summary>
	   private ArrayList owners;

	   /// <summary>
	   /// Field openedToWrite - vector of files that have been opened to write on this filesystem </summary>
	   private ArrayList openedToWrite;

	   /// <summary>
	   /// Field canRedirect - flag to indicate page redirection information must be gathered </summary>
	   private bool canRedirect;

	   /// <summary>
	   /// Field pbmBank - memory bank used for the page bitmap </summary>
	   private OTPMemoryBank pbmBank;

	   /// <summary>
	   /// Field pbmByteOffset - byte offset into page bitmap buffer </summary>
	   private int pbmByteOffset;

	   /// <summary>
	   /// Field pbmBitOffset - bit offset into page bitmap buffer </summary>
	   private int pbmBitOffset;

	   /// <summary>
	   /// Field pbmCache - buffer to cache the page bitmap </summary>
	   private sbyte[] pbmCache;

	   /// <summary>
	   /// Field pbmCacheModified - modifified version of the page bitmap </summary>
	   private sbyte[] pbmCacheModified;

	   /// <summary>
	   /// Field pbmRead - flag indicating that the page bitmap has been read </summary>
	   private bool pbmRead;

	   /// <summary>
	   /// Field lastFreePage - last free page found in the page bitmap </summary>
	   private int lastFreePage;

	   /// <summary>
	   /// Field lastDevice - last device read/written </summary>
	   private int lastDevice;

	   /// <summary>
	   /// Field autoOverdrive - flag to indicate if we need to do auto-ovedrive </summary>
	   private bool autoOverdrive;

	   //--------
	   //-------- Constructor
	   //--------

	   /// <summary>
	   /// Construct a new memory cache for provided 1-wire container device.
	   /// </summary>
	   /// <param name="device"> 1-Wire container </param>
	   public MemoryCache(OneWireContainer device)
	   {
		  OneWireContainer[] devices = new OneWireContainer[1];
		  devices[0] = device;

		  init(devices);
	   }

	   /// <summary>
	   /// Construct a new memory cache for provided 1-wire container device.
	   /// </summary>
	   /// <param name="device"> 1-Wire container </param>
	   public MemoryCache(OneWireContainer[] devices)
	   {
		  init(devices);
	   }

	   /// <summary>
	   /// Initializes this memory cache for provided 1-wire container device(s).
	   /// </summary>
	   /// <param name="devices"> 1-Wire container(s) </param>
	   private void init(OneWireContainer[] devices)
	   {
		  owd = devices;
		  int mem_size = 0;

		  PagedMemoryBank pmb = null;

		  banks = new ArrayList(1);
		  owners = new ArrayList(1);
		  openedToWrite = new ArrayList(1);
		  startPages = new int[owd.Length];
		  lastDevice = 0;

		  // check to see if adapter supports overdrive
		  try
		  {
			 autoOverdrive = devices[0].Adapter.canOverdrive();
		  }
		  catch (OneWireException)
		  {
			 autoOverdrive = false;
		  }

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___Constructor MemoryCache: " + devices[0].AddressAsString + " num " + devices.Length);
		  }

		  // loop through all of the devices in the array
		  totalPages = 0;
		  for (int dev = 0; dev < owd.Length; dev++)
		  {
			 // check to make sure each device can do Overdrive
			 if (owd[dev].MaxSpeed != DSPortAdapter.SPEED_OVERDRIVE)
			 {
				autoOverdrive = false;
			 }

			 // record the start page offset for each device
			 startPages[dev] = totalPages;

			 // enumerate through the memory banks and collect the
			 // general purpose banks in a vector
			 for (System.Collections.IEnumerator bank_enum = owd[dev].MemoryBanks; bank_enum.MoveNext();)
			 {
				// get the next memory bank
				MemoryBank mb = (MemoryBank) bank_enum.Current;

				// look for pbm memory bank (used in file structure)
				if (mb.WriteOnce && !mb.GeneralPurposeMemory && mb.NonVolatile && (mb is OTPMemoryBank))
				{
				   // if more then 1 device with a OTP then error
				   if (owd.Length > 1)
				   {
					  totalPages = 0;
					  return;
				   }

				   // If only 128 bytes then have DS2502 or DS2406 which have bitmap included
				   // in the only status page.  All other EPROM devices have a special memory
				   // bank that has 'Bitmap' in the title.
				   if ((mem_size == 128) || (mb.BankDescription.IndexOf("Bitmap", StringComparison.Ordinal) != -1))
				   {
					  pbmBank = (OTPMemoryBank) mb;

					  if (mem_size == 128)
					  {
						 pbmBitOffset = 4;
					  }

					  pbmByteOffset = 0;
					  canRedirect = true;

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("_Paged BitMap MemoryBank: " + mb.BankDescription + " with bit offset " + pbmBitOffset);
					  }
				   }
				}

				// check regular memory bank
				if (!mb.GeneralPurposeMemory || !mb.NonVolatile || !(mb is PagedMemoryBank))
				{
				   continue;
				}

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("_Using MemoryBank: " + mb.BankDescription);
				}

				banks.Add(mb);
				mem_size += mb.Size;
				totalPages += ((PagedMemoryBank)mb).NumberPages;
			 }
		  }

		  // count total bankPages
		  bankPages = new int [banks.Count];
		  totalPages = 0;

		  for (int b = 0; b < banks.Count; b++)
		  {
			 pmb = (PagedMemoryBank) banks[b];
			 bankPages [b] = pmb.NumberPages;
			 totalPages += bankPages [b];
		  }

		  // create the cache
		  len = new int [totalPages];
		  pageState = new int [totalPages];
		  writeLog = new int [totalPages];
		  redirect = new int [totalPages];
		  if (pmb != null)
		  {
			 maxPacketDataLength = pmb.MaxPacketDataLength;
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: cache = new sbyte [totalPages][pmb.PageLength];
			 cache = RectangularArrays.ReturnRectangularSbyteArray(totalPages, pmb.PageLength);
			 tempPage = new sbyte [pmb.PageLength];
		  }

		  // initialize some of the flag arrays
		  for (int p = 0; p < totalPages; p++)
		  {
			 pageState [p] = NOT_READ;
			 len [p] = 0;
			 writeLog [p] = EMPTY;
		  }

		  // if getting redirection information, create necessarey arrays
		  if (canRedirect)
		  {
			 tempExtra = new sbyte [pmb.ExtraInfoLength];
			 pbmCache = new sbyte [pbmBank.Size];
			 pbmCacheModified = new sbyte [pbmBank.Size];
			 pbmRead = false;
		  }
		  else
		  {
			 pbmRead = true;
		  }

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("_Total Pages: " + totalPages + ", get Redirection = " + canRedirect);
		  }
	   }

	   /// <summary>
	   /// Gets the number of pages in this cache
	   /// </summary>
	   /// <returns> number of pages in the cache </returns>
	   public virtual int NumberPages
	   {
		   get
		   {
			  return totalPages;
		   }
	   }

	   /// <summary>
	   /// Gets the number of pages in the specified bank number
	   /// </summary>
	   /// <param name="bankNum"> bank number to retrieve number of pages
	   /// </param>
	   /// <returns> number of pages in the bank </returns>
	   public virtual int getNumberPagesInBank(int bankNum)
	   {
		  if (totalPages > 0)
		  {
			 return bankPages[bankNum];
		  }
		  else
		  {
			 return 0;
		  }
	   }

	   /// <summary>
	   /// Gets the page number of the first page on the specified
	   /// device.  If the device number is not valid then return 0.
	   /// </summary>
	   /// <param name="deviceNum"> device number to retrieve page offset
	   /// </param>
	   /// <returns> page number of first page on device </returns>
	   public virtual int getPageOffsetForDevice(int deviceNum)
	   {
		  return startPages[deviceNum];
	   }

	   /// <summary>
	   /// Gets the maximum number of bytes for data in each page.
	   /// </summary>
	   /// <returns> max number of data bytes per page </returns>
	   public virtual int MaxPacketDataLength
	   {
		   get
		   {
			  return maxPacketDataLength;
		   }
	   }

	   /// <summary>
	   /// Check if this memory device is write-once.  If this is true then
	   /// the page bitmap facilities in this class will be used.
	   /// </summary>
	   /// <returns> true if this device is write-once </returns>
	   public virtual bool WriteOnce
	   {
		   get
		   {
			  return canRedirect;
		   }
	   }

	   /// <summary>
	   /// Read a page packet.  If the page is available in the cache
	   /// then return that data.
	   /// </summary>
	   /// <param name="page">  page to read </param>
	   /// <param name="readBuf"> buffer to place the data in </param>
	   /// <param name="offset"> offset into the read buffer
	   /// </param>
	   /// <returns> the number byte in the packet
	   /// </returns>
	   /// <exception cref="OneWireException"> when the adapter is not setup properly </exception>
	   /// <exception cref="OneWireIOException"> when an 1-Wire IO error occures </exception>
	   public virtual int readPagePacket(int page, sbyte[] readBuf, int offset)
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___readPagePacket (" + page + ") ");
		  }

		  // check if have a cache (any memory banks)
		  if (totalPages == 0)
		  {
			 throw new OneWireException("1-Wire Filesystem does not have memory");
		  }

		  // check if out of range
		  if (page >= totalPages)
		  {
			 throw new OneWireException("Page requested is not in memory space");
		  }

		  // check if doing autoOverdrive (greatly improves multi-device cache speed)
		  if (autoOverdrive)
		  {
			 autoOverdrive = false;
			 DSPortAdapter adapter = owd[0].Adapter;
			 adapter.Speed = DSPortAdapter.SPEED_REGULAR;
			 adapter.reset();
			 adapter.putByte((sbyte) 0x3C);
			 adapter.Speed = DSPortAdapter.SPEED_OVERDRIVE;
		  }

		  // check if need to read the page bitmap for the first time
		  if (!pbmRead)
		  {
			 readPageBitMap();
		  }

		  // page NOT cached (maybe redirected)
		  if ((pageState[page] == NOT_READ) || (pageState[page] == READ_NO_CRC) || (redirect [page] != 0))
		  {

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("_Not in cache or redirected, length=" + len [page] + " redirect=" + redirect [page]);
			 }

			 // page not cached, so read it
			 int local_page = getLocalPage(page);
			 PagedMemoryBank pmb = getMemoryBankForPage(page);
			 int local_device_page = page - startPages[getDeviceIndex(page)];

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("_Look in MemoryBank " + pmb.BankDescription);
			 }

			 if (canRedirect)
			 {
				// don't use multi-bank page reference (would not work with redirect)

				// loop while page is redirected
				int loopcnt = 0;
				for (;;)
				{
				   // check for redirection
				   if (redirect [page] == 0)
				   {
					  // check if already in cache
					  if ((pageState[page] == READ_CRC) || (pageState[page] == VERIFY) || (pageState[page] == WRITE))
					  {
						 break;
					  }

					  // read the page with device generated CRC
					  if (pmb.hasExtraInfo())
					  {
						 pmb.readPageCRC(page, (lastPageRead == (page - 1)), cache [page], 0, tempExtra);

						 // set the last page read
						 lastPageRead = page;

						 // get the redirection byte
						 redirect [page] = ~tempExtra [0] & 0x00FF;
					  }
					  // OTP device that does not give redirect as extra info (DS1982/DS2502)
					  else
					  {
						 pmb.readPageCRC(page, (lastPageRead == (page - 1)), cache [page], 0);

						 // get the redirection
						 redirect[page] = (sbyte)(((OTPMemoryBank)pmb).getRedirectedPage(page));

						 // last page can't be used due to redirect read
						 lastPageRead = NONE;
					  }

					  // set the page state
					  pageState[page] = READ_NO_CRC;

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("_Page: " + page + "->" + redirect [page] + " local " + local_page + " with packet length byte " + (cache[page][0] & 0x00FF));
					  }

					  // not redirected so look at packet
					  if (redirect [page] == 0)
					  {
						 // check if length is realistic
						 if ((cache [page][0] & 0x00FF) > maxPacketDataLength)
						 {
							throw new OneWireIOException("Invalid length in packet");
						 }

						 // verify the CRC is correct
						 if (CRC16.compute(cache [page], 0, cache [page][0] + 3, page) == 0x0000B001)
						 {
							// get the length
							len [page] = cache [page][0];

							// set the page state
							pageState[page] = READ_CRC;

							break;
						 }
						 else
						 {
							throw new OneWireIOException("Invalid CRC16 in packet read " + page);
						 }
					  }
				   }
				   else
				   {
					  page = redirect [page];
				   }

				   // check for looping redirection
				   if (loopcnt++ > totalPages)
				   {
					  throw new OneWireIOException("Circular redirection of pages");
				   }
				}

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.Write("_Data found (" + len [page] + "):");
				   debugDump(cache [page], 1, len [page]);
				}

				// get copy of data for caller
				if (readBuf != null)
				{
				   Array.Copy(cache [page], 1, readBuf, offset, len [page]);
				}

				return len [page];
			 }
			 // not an EPROM
			 else
			 {
				// loop if get a crc error in packet data until get same data twice
				for (;;)
				{
				   pmb.readPage(local_page, (lastPageRead == (page - 1)), tempPage, 0);

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("_Page: " + page + " translates to " + local_page + " or device page " + local_device_page);
				   }

				   // set the last page read
				   lastPageRead = page;

				   // verify length is realistic
				   if ((tempPage [0] & 0x00FF) <= maxPacketDataLength)
				   {

					  // verify the CRC is correct
					  if (CRC16.compute(tempPage, 0, tempPage [0] + 3, local_device_page) == 0x0000B001)
					  {

						 // valid data so put into cache
						 Array.Copy(tempPage, 0, cache [page], 0, tempPage.Length);

						 // get the length
						 len [page] = tempPage [0];

						 // set the page state
						 pageState[page] = READ_CRC;

						 break;
					  }
				   }

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.Write("_Invalid CRC, raw: ");
					  debugDump(tempPage, 0, tempPage.Length);
				   }

				   // must have been invalid packet
				   // compare with data currently in the cache
				   bool same_data = true;

				   for (int i = 0; i < tempPage.Length; i++)
				   {
					  if ((tempPage [i] & 0x00FF) != (cache [page][i] & 0x00FF))
					  {

						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.WriteLine("_Differenet at position=" + i);
						 }

						 same_data = false;

						 break;
					  }
				   }

				   // if the same then throw the exception, else loop again
				   if (same_data)
				   {
					  // set the page state
					  pageState[page] = READ_NO_CRC;

					  throw new OneWireIOException("Invalid CRC16 in packet read");
				   }
				   else
				   {
					  Array.Copy(tempPage, 0, cache [page], 0, tempPage.Length);
				   }
				}
			 }

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("_Data found (" + len [page] + "):");
				debugDump(cache [page], 1, len [page]);
			 }

			 // get copy of data for caller
			 if (readBuf != null)
			 {
				Array.Copy(cache [page], 1, readBuf, offset, len [page]);
			 }

			 return len [page];
		  }
		  // page IS cached (READ_CRC, VERIFY, WRITE)
		  else
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("_In cache (" + len [page] + "):");
				debugDump(cache [page], 1, len [page]);
			 }

			 // get from cache
			 if (readBuf != null)
			 {
				Array.Copy(cache [page], 1, readBuf, offset, len [page]);
			 }

			 return len [page];
		  }
	   }

	   /// <summary>
	   /// Write a page packet into the cache.
	   /// </summary>
	   /// <param name="page">  page to write </param>
	   /// <param name="writeBuf"> buffer container the data to write </param>
	   /// <param name="offset"> offset into write buffer </param>
	   /// <param name="buflen"> length of data to write </param>
	   public virtual void writePagePacket(int page, sbyte[] writeBuf, int offset, int buflen)
	   {
		  int log;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("___writePagePacket on page " + page + " with data (" + buflen + "): ");
			 debugDump(writeBuf, offset, buflen);
		  }

		  // check if have a cache (any memory banks)
		  if (totalPages == 0)
		  {
			 throw new OneWireException("1-Wire Filesystem does not have memory");
		  }

		  // check if need to read the page bitmap for the first time
		  if (!pbmRead)
		  {
			 readPageBitMap();
		  }

		  // OTP device
		  if (canRedirect)
		  {
			 // get reference to memory bank
			 OTPMemoryBank otp = (OTPMemoryBank)getMemoryBankForPage(page);

			 // check redirectoin if writing to a page that has not been read
			 if ((redirect[page] == 0) && (pageState[page] == NOT_READ))
			 {
				redirect[page] = otp.getRedirectedPage(page);
			 }

			 // check if page to write to is already redirected
			 if (redirect[page] != 0)
			 {
				// loop to find the end of the redirect chain
				int last_page = page, cnt = 0;
				lastPageRead = NONE;
				do
				{
				   last_page = redirect[last_page];

				   redirect[last_page] = otp.getRedirectedPage(last_page);

				   if (cnt++ > totalPages)
				   {
					  throw new OneWireException("Error in Filesystem, circular redirection of pages");
				   }
				} while (redirect[last_page] != 0);

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.Write("___redirection chain ended on page " + last_page);
				}

				// Use the last_page since it was not redirected
				Array.Copy(writeBuf, offset, cache[last_page], 1, buflen);
				len [last_page] = buflen;
				cache [last_page][0] = (sbyte) buflen;
				int crc = CRC16.compute(cache[last_page], 0, buflen + 1, last_page);
				cache[last_page][buflen + 1] = unchecked((sbyte)(~crc & 0xFF));
				cache[last_page][buflen + 2] = unchecked((sbyte)(((int)((uint)(~crc & 0xFFFF) >> 8)) & 0xFF));

				// set pageState flag
				pageState [last_page] = VERIFY;

				// change page to last_page to be used in writeLog
				page = last_page;
			 }
			 else
			 {
				// Use the page since it is not redirected
				Array.Copy(writeBuf, offset, cache[page], 1, buflen);
				len [page] = buflen;
				cache [page][0] = (sbyte) buflen;
				int crc = CRC16.compute(cache[page], 0, buflen + 1, page);
				cache[page][buflen + 1] = unchecked((sbyte)(~crc & 0xFF));
				cache[page][buflen + 2] = unchecked((sbyte)(((int)((uint)(~crc & 0xFFFF) >> 8)) & 0xFF));

				// set pageState flag
				pageState [page] = VERIFY;
			 }
		  }
		  // NON-OTP device
		  else
		  {
			 // put in cache
			 Array.Copy(writeBuf, offset, cache [page], 1, buflen);

			 len [page] = buflen;
			 cache [page][0] = (sbyte) buflen;

			 // set pageState flag
			 pageState [page] = WRITE;
		  }

		  // record write in log
		  // search the write log until find 'page' or EMPTY
		  for (log = 0; log < totalPages; log++)
		  {
			 if ((writeLog [log] == page) || (writeLog [log] == EMPTY))
			 {
				break;
			 }
		  }

		  // shift write log down 1 to 'log'
		  for (; log > 0; log--)
		  {
			 writeLog [log] = writeLog [log - 1];
		  }

		  // add page at top
		  writeLog [0] = page;
	   }

	   /// <summary>
	   /// Flush the pages written back to the 1-Wire device.
	   /// </summary>
	   /// <exception cref="OneWireException"> when the adapter is not setup properly </exception>
	   /// <exception cref="OneWireIOException"> when an 1-Wire IO error occures </exception>
	   public virtual void sync()
	   {
		  int page, log;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___sync");
		  }

		  // check if have a cache (any memory banks)
		  if (totalPages == 0)
		  {
			 return;
		  }

		  // loop until all jobs complete
		  bool jobs;
		  do
		  {
			 jobs = false;

			 // loop through write log and write the oldest pages first
			 for (log = totalPages - 1; log >= 0; log--)
			 {
				// check if this is a valid log entry
				if (writeLog [log] != EMPTY)
				{

				   // this was not empty so there is a job
				   jobs = true;

				   // get page number to write
				   page = writeLog [log];

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("_page " + page + " in log " + log + " is not empty, pageState: " + pageState[page]);
				   }

				   // get the memory bank
				   PagedMemoryBank pmb = (PagedMemoryBank)getMemoryBankForPage(page);

				   // get the local page number
				   int local_page = getLocalPage(page);

				   // Verify operation (only in EPROM operations)
				   if (pageState[page] == VERIFY)
				   {
					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("_verify page " + page);
					  }

					  // read the page with device generated CRC
					  pmb.readPageCRC(page, (lastPageRead == (page - 1)), tempPage, 0);

					  // set the last page read
					  lastPageRead = page;

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.Write("_Desired data: ");
						 debugDump(cache [page], 0, cache[page].Length);
						 Debug.Write("_Current data: ");
						 debugDump(tempPage, 0, tempPage.Length);
						 Debug.WriteLine("_len " + len[page]);
					  }

					  // check to see if the desired data can be written here
					  bool do_redirect = false;
					  for (int i = 1; i < (len[page] + 2); i++)
					  {
						 if ((((tempPage[i] & 0x00FF) ^ (cache[page][i] & 0x00FF)) & (~tempPage[i] & 0x00FF)) > 0)
						 {
							// error, data already on device, must redirect
							do_redirect = true;
							break;
						 }
					  }

					  // need to redirect
					  if (do_redirect)
					  {
						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.WriteLine("_page is occupied with conflicting data, must redirect");
						 }

						 // find a new page, set VERIFY job there
						 // get the next available page
						 int new_page = FirstFreePage;
						 while (new_page == page)
						 {
							Debug.WriteLine("_can't use this page " + page);
							markPageUsed(new_page);
							new_page = NextFreePage;
						 }

						 // verify got a free page
						 if (new_page < 0)
						 {
							throw new OneWireException("Redireciton required but out of space on 1-Wire device");
						 }

						 // mark page used
						 markPageUsed(new_page);

						 // put the data in the new page and setup the job
						 Array.Copy(cache[page], 0, cache[new_page], 0, tempPage.Length);
						 pageState[new_page] = VERIFY;
						 len[new_page] = len[page];

						 // add to write log
						 for (int i = 0; i < totalPages; i++)
						 {
							if (writeLog[i] == EMPTY)
							{
							   writeLog[i] = new_page;
							   break;
							}
						 }

						 // set old page for redirect
						 pageState[page] = REDIRECT;
						 cache[page][0] = unchecked((sbyte)(new_page & 0xFF));
					  }
					  // verify passed
					  else
					  {
						 pageState[page] = WRITE;
					  }
				   }

				   // Redirect operation
				   if (pageState[page] == REDIRECT)
				   {
					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("_redirecting page " + page + " to " + (cache[page][0] & 0x00FF));
					  }

					  // redirect the page (new page located in first byte of cache)
					  ((OTPMemoryBank)pmb).redirectPage(page, cache[page][0] & 0x00FF);

					  // clear the redirect job
					  pageState [page] = NOT_READ;
					  lastPageRead = NONE;
					  writeLog [log] = EMPTY;
				   }

				   // Write operation
				   if (pageState [page] == WRITE)
				   {
					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.Write("_write page " + page + " with data (" + len [page] + "): ");
						 debugDump(cache [page], 1, len [page]);
					  }

					  // check for new device, make sure it is at the correct speed
					  int new_index = getDeviceIndex(page);
					  if (new_index != lastDevice)
					  {
						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.Write("(" + new_index + ")");
						 }

						 lastDevice = new_index;
						 owd[lastDevice].doSpeed();
					  }

					  // write the page
					  pmb.writePagePacket(local_page, cache[page], 1, len[page]);

					  // clear pageState flag
					  pageState [page] = READ_CRC;
					  lastPageRead = NONE;
					  writeLog [log] = EMPTY;
				   }
				}
			 }
		  } while (jobs);

		  // write the bitmap of used pages for OTP device
		  if (canRedirect)
		  {
			 // make a buffer that contains only then new '0' bits in the bitmap
			 // required to not overprogram any bits
			 int numBytes = totalPages / 8;
			 if (numBytes == 0)
			 {
				numBytes = 1;
			 }
			 bool changed = false;
			 sbyte[] temp_buf = new sbyte[numBytes];

			 for (int i = 0; i < numBytes; i++)
			 {
				temp_buf[i] = unchecked((sbyte)(~(pbmCache[i] ^ pbmCacheModified[i]) & 0x00FF));
				if ((sbyte)temp_buf[i] != unchecked((sbyte)0xFF))
				{
				   changed = true;
				}
			 }

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("_device bitmap: ");
				debugDump(pbmCache, 0, pbmCache.Length);
				Debug.Write("_modified bitmap: ");
				debugDump(pbmCacheModified, 0, pbmCacheModified.Length);
				Debug.Write("_page bitmap to write, changed: " + changed + "   ");
				debugDump(temp_buf, 0, temp_buf.Length);
			 }

			 // write if changed
			 if (changed)
			 {
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("_writing page bitmap");
				}

				// turn off read-back verification
				pbmBank.WriteVerification = false;

				// write buffer
				pbmBank.write(0, temp_buf, 0, numBytes);

				// readback to make sure that it matches pbmCacheModified
				pbmBank.read(0, false, temp_buf, 0, numBytes);
				for (int i = 0; i < numBytes; i++)
				{
				   if ((temp_buf[i] & 0x00FF) != (pbmCacheModified[i] & 0x00FF))
				   {
					  throw new OneWireException("Readback verfication of page bitmap was not correct");
				   }
				}

				// put new value of bitmap pbmCache
				Array.Copy(temp_buf, 0, pbmCache, 0, numBytes);
				Array.Copy(temp_buf, 0, pbmCacheModified, 0, numBytes);
			 }
		  }
	   }

	   //--------
	   //-------- Owner tracking methods
	   //--------

	   /// <summary>
	   /// Add an owner to this memory cache.  This will be tracked
	   /// for later cleanup.
	   /// </summary>
	   /// <param name="tobj"> owner of instance </param>
	   public virtual void addOwner(object tobj)
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___addOwner");
		  }

		  if (owners.IndexOf(tobj) == -1)
		  {
			 owners.Add(tobj);
		  }
	   }

	   /// <summary>
	   /// Remove the specified owner of this memory cache.
	   /// </summary>
	   /// <param name="tobj"> owner of instance </param>
	   public virtual void removeOwner(object tobj)
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___removeOwner");
		  }

		  owners.Remove(tobj);
	   }

	   /// <summary>
	   /// Check to see if there on no owners of this memory cache.
	   /// </summary>
	   /// <returns> true if not owners of this memory cache </returns>
	   public virtual bool noOwners()
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___noOwners = " + (owners.Count == 0));
		  }

		  return owners.Count == 0;
	   }

	   //--------
	   //-------- Write file tracking methods
	   //--------

	   /// <summary>
	   /// Remove the provided filePath from the list of files
	   /// currently opened to write.
	   /// </summary>
	   /// <param name="filePath"> file to remove from write list </param>
	   public virtual void removeWriteOpen(string filePath)
	   {
		  int index = openedToWrite.IndexOf(filePath);

		  if (index != -1)
		  {
			 openedToWrite.RemoveAt(index);
		  }
	   }

	   /// <summary>
	   /// Check to see if the provided filePath is currently opened
	   /// to write.  Optionally add it to the list if it not already
	   /// there.
	   /// </summary>
	   /// <param name="filePath">  file to check to see if opened to write </param>
	   /// <param name="addToList"> true to add file to list if not present
	   /// </param>
	   /// <returns> true if file was not in the opened to write list </returns>
	   public virtual bool isOpenedToWrite(string filePath, bool addToList)
	   {
		  int index = openedToWrite.IndexOf(filePath);

		  if (index != -1)
		  {
			 return true;
		  }
		  else
		  {
			 if (addToList)
			 {
				openedToWrite.Add(filePath);
			 }
			 return false;
		  }
	   }

	   //--------
	   //-------- Page-Bitmap methods
	   //--------

	   /// <summary>
	   /// Check to see if this memory cache should handle the page bitmap.
	   /// </summary>
	   /// <returns> true if this memory cache should handle the page bitmap </returns>
	   public virtual bool handlePageBitmap()
	   {
		  return !(pbmBank == null);
	   }

	   /// <summary>
	   /// Mark the specified page as used in the page bitmap.
	   /// </summary>
	   /// <param name="page"> number to mark as used </param>
	   public virtual void markPageUsed(int page)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("___markPageUsed " + page);
		  }

		  // mark page used in cached bitmap of used pages
		  Bit.arrayWriteBit(USED, pbmBitOffset + page, pbmByteOffset, pbmCacheModified);
	   }

	   /// <summary>
	   /// free the specified page as being un-used in the page bitmap
	   /// </summary>
	   /// <param name="page"> number to mark as un-used
	   /// </param>
	   /// <returns> true if the page as be been marked as un-used, false
	   ///      if the page is on an OTP device and cannot be freed </returns>
	   public virtual bool freePage(int page)
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("___freePage " + page);
		  }

		  // only free pages that have been written to cache
		  // but not flushed to device
		  if (Bit.arrayReadBit(pbmBitOffset + page, pbmByteOffset, pbmCache) == NOT_USED)
		  {
			 Bit.arrayWriteBit(NOT_USED, pbmBitOffset + page, pbmByteOffset, pbmCacheModified);

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("_ was cached so really free now ");
			 }

			 return true;
		  }
		  else
		  {

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("_ not cached so not free");
			 }

			 return false;
		  }
	   }

	   /// <summary>
	   /// Get the first free page from the page bitmap.
	   /// </summary>
	   /// <returns> first page number that is free to write </returns>
	   public virtual int FirstFreePage
	   {
		   get
		   {
    
			  //\\//\\//\\//\\//\\//\\//\\//
			  if (doDebugMessages)
			  {
				 Debug.Write("___getFirstFreePage ");
			  }
    
			  lastFreePage = 0;
    
			  return NextFreePage;
		   }
	   }

	   /// <summary>
	   /// Get the next free page from the page bitmap.
	   /// </summary>
	   /// <returns> next page number that is free to write </returns>
	   public virtual int NextFreePage
	   {
		   get
		   {
			  for (int pg = lastFreePage; pg < totalPages; pg++)
			  {
				 if (Bit.arrayReadBit(pbmBitOffset + pg, pbmByteOffset, pbmCacheModified) == NOT_USED)
				 {
    
					//\\//\\//\\//\\//\\//\\//\\//
					if (doDebugMessages)
					{
					   Debug.WriteLine("___getNextFreePage " + pg);
					}
    
					lastFreePage = pg + 1;
    
					return pg;
				 }
			  }
    
			  //\\//\\//\\//\\//\\//\\//\\//
			  if (doDebugMessages)
			  {
				 Debug.WriteLine("___getNextFreePage, no free pages ");
			  }
    
			  return -1;
		   }
	   }

	   /// <summary>
	   /// Get the total number of free pages in this Filesystem.
	   /// </summary>
	   /// <returns> number of pages free
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   public virtual int NumberFreePages
	   {
		   get
		   {
			  // check if need to read the page bitmap for the first time
			  if (!pbmRead)
			  {
				 // read the pbm
				 pbmBank.read(0, false, pbmCache, 0, pbmCache.Length);
    
				 // make a copy of it
				 Array.Copy(pbmCache, 0, pbmCacheModified, 0, pbmCache.Length);
    
				 // mark as read
				 pbmRead = true;
    
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.Write("_Page bitmap read in getNumberFreePages: ");
					debugDump(pbmCache, 0, pbmCache.Length);
				 }
			  }
    
			  int free_pages = 0;
			  for (int pg = 0; pg < totalPages; pg++)
			  {
				 if (Bit.arrayReadBit(pbmBitOffset + pg, pbmByteOffset, pbmCacheModified) == NOT_USED)
				 {
					free_pages++;
				 }
			  }
    
			  //\\//\\//\\//\\//\\//\\//\\//
			  if (doDebugMessages)
			  {
				 Debug.WriteLine("___getNumberFreePages = " + free_pages);
			  }
    
			  return free_pages;
		   }
	   }

	   /// <summary>
	   /// Gets the page number used in the remote page bitmap in an OTP device.
	   /// </summary>
	   /// <returns> page number used in the directory for the remote page bitmap </returns>
	   public virtual int BitMapPageNumber
	   {
		   get
		   {
			  return (pbmBank.StartPhysicalAddress / pbmBank.PageLength);
		   }
	   }

	   /// <summary>
	   /// Get the number of pages used for the remote page bitmap in an
	   /// OTP device.
	   /// </summary>
	   /// <returns> number of pages used in page bitmap </returns>
	   public virtual int BitMapNumberOfPages
	   {
		   get
		   {
			  return ((totalPages / 8) / pbmBank.PageLength);
		   }
	   }

	   /// <summary>
	   /// Get's the memory bank object for the specified page.
	   /// This is significant if the Filesystem spans memory banks
	   /// on the same or different devices.
	   /// </summary>
	   public virtual PagedMemoryBank getMemoryBankForPage(int page)
	   {
		  int cnt = 0;

		  for (int bank_num = 0; bank_num < banks.Count; bank_num++)
		  {
			 // check if 'page' is in this memory bank
			 if ((cnt + bankPages[bank_num]) > page)
			 {
				return (PagedMemoryBank) banks[bank_num];
			 }

			 cnt += bankPages[bank_num];
		  }

		  // page provided is not in this Filesystem
		  return null;
	   }

	   /// <summary>
	   /// Get's the index into the array of Devices where this page
	   /// resides.
	   /// This is significant if the Filesystem spans memory banks
	   /// on the same or different devices.
	   /// </summary>
	   private int getDeviceIndex(int page)
	   {
		  for (int dev_num = (startPages.Length - 1); dev_num >= 0; dev_num--)
		  {
			 // check if 'page' is in this memory bank
			 if (startPages[dev_num] < page)
			 {
				return dev_num;
			 }
		  }

		  // page provided is not in this Filesystem
		  return 0;
	   }

	   /// <summary>
	   /// Get's the local page number on the memory bank object for
	   /// the specified page.
	   /// This is significant if the Filesystem spans memory banks
	   /// on the same or different devices.
	   /// </summary>
	   public virtual int getLocalPage(int page)
	   {
		  int cnt = 0;

		  for (int bank_num = 0; bank_num < banks.Count; bank_num++)
		  {
			 // check if 'page' is in this memory bank
			 if ((cnt + bankPages[bank_num]) > page)
			 {
				return (page - cnt);
			 }

			 cnt += bankPages[bank_num];
		  }

		  // page provided is not in this Filesystem
		  return 0;
	   }

	   /// <summary>
	   /// Clears the lastPageRead global so that a readPage will
	   /// not try to continue where the last page left off.
	   /// This should be called anytime exclusive access to the
	   /// 1-Wire canot be guaranteed.
	   /// </summary>
	   public virtual void clearLastPageRead()
	   {
		  // last page can't be used due to redirect read
		  lastPageRead = NONE;
	   }

	   /// <summary>
	   /// Read the page bitmap.
	   /// </summary>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   private void readPageBitMap()
	   {
		  // read the pbm
		  pbmBank.read(0, false, pbmCache, 0, pbmCache.Length);

		  // make a copy of it
		  Array.Copy(pbmCache, 0, pbmCacheModified, 0, pbmCache.Length);

		  // mark as read
		  pbmRead = true;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("____Page bitmap read: ");
			 debugDump(pbmCache, 0, pbmCache.Length);
		  }
	   }


	   //--------
	   //-------- Misc Utility Methods
	   //--------

	   /// <summary>
	   /// Debug dump utility method
	   /// </summary>
	   /// <param name="buf"> buffer to dump </param>
	   /// <param name="offset"> offset to start in the buffer </param>
	   /// <param name="len"> length to dump </param>
	   private void debugDump(sbyte[] buf, int offset, int len)
	   {
		  for (int i = offset; i < (offset + len); i++)
		  {
			 Debug.Write(((int) buf [i] & 0x00FF).ToString("x") + " ");
		  }

		  Debug.WriteLine("");
	   }
	}

}