﻿using System;
using System.Threading;

/*---------------------------------------------------------------------------
 * Copyright (C) 2003 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.container
{

	// imports
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using CRC16 = com.dalsemi.onewire.utils.CRC16;
	using SHA = com.dalsemi.onewire.utils.SHA;
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;

	using Debug = com.dalsemi.onewire.debug.Debug;
	using Convert = com.dalsemi.onewire.utils.Convert;

	/// <summary>
	/// Memory bank class for the Scratchpad section of SHA EEPROM iButtons and
	/// 1-Wire devices with SHA write-protected memory pages.
	/// 
	///  @version    1.00, 11 Aug 2002
	///  @author     SH
	/// </summary>
	internal class MemoryBankScratchSHAEE : MemoryBankScratchEx
	{
	   /// <summary>
	   /// turn on extra debugging output </summary>
	   private const bool DEBUG = true;


	   /// <summary>
	   /// Load First Secret </summary>
	   public static readonly sbyte LOAD_FIRST_SECRET = (sbyte) 0x5A;

	   /// <summary>
	   /// Compute next Secret command </summary>
	   public static readonly sbyte COMPUTE_NEXT_SECRET = (sbyte) 0x33;

	   /// <summary>
	   /// Refresh Scratchpad command </summary>
	   public static readonly sbyte REFRESH_SCRATCHPAD = unchecked((sbyte) 0xA3);

	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] MT_buffer = new sbyte [64];
	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] MAC_buffer = new sbyte[20];
	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] page_data_buffer = new sbyte [32];
	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] scratchpad_buffer = new sbyte [8];
	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] copy_scratchpad_buffer = new sbyte[4];
	   /// <summary>
	   /// cached byte[] for re-use in SHA debit applications, speeds up operation on TINI </summary>
	   private readonly sbyte[] read_scratchpad_buffer = new sbyte[8 + 3 + 3];

	   /// <summary>
	   /// block of 0xFF's used for faster read pre-fill of 1-Wire blocks
	   /// Comes from OneWireContainer33 that this MemoryBank references.
	   /// </summary>
	   protected internal new static readonly sbyte[] ffBlock = OneWireContainer33.ffBlock;

	   /// <summary>
	   /// block of 0x00's used for faster read pre-fill of 1-Wire blocks
	   /// Comes from OneWireContainer33 that this MemoryBank references.
	   /// </summary>
	   protected internal static readonly sbyte[] zeroBlock = OneWireContainer33.zeroBlock;

	   /// <summary>
	   /// The Password container to acces the 8 byte passwords
	   /// </summary>
	   protected internal OneWireContainer33 owc33 = null;


	   //--------
	   //-------- Constructor
	   //--------

	   /// <summary>
	   /// Memory bank contstuctor.  Requires reference to the OneWireContainer
	   /// this memory bank resides on.
	   /// </summary>
	   public MemoryBankScratchSHAEE(OneWireContainer33 ibutton) : base((OneWireContainer)ibutton)
	   {

		  owc33 = ibutton;

		  // initialize attributes of this memory bank - DEFAULT: DS1963L scratchapd
		  bankDescription = "Scratchpad with CRC and 'Copy Scratchpad w/ SHA MAC'";
		  pageAutoCRC = true;
		  startPhysicalAddress = 0;
		  size = 8;
		  numberPages = 1;
		  pageLength = 8;
		  maxPacketDataLength = 8 - 3;
		  extraInfo = true;
		  extraInfoLength = 3;

		  // COPY_SCRATCHPAD_WITH_MAC
		  COPY_SCRATCHPAD_COMMAND = (sbyte) 0x55;
	   }

	   //--------
	   //-------- PagedMemoryBank I/O methods
	   //--------

	   /// <summary>
	   /// Read a complete memory page with CRC verification provided by the
	   /// device.  Not supported by all devices.  See the method
	   /// 'hasPageAutoCRC()'.
	   /// </summary>
	   /// <param name="page">          page number to read </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       readPagePacket() continious where the last one
	   ///                       stopped and it is inside a
	   ///                       'beginExclusive/endExclusive' block. </param>
	   /// <param name="readBuf">       byte array to put data read. Must have at least
	   ///                       'getMaxPacketDataLength()' elements. </param>
	   /// <param name="offset">        offset into readBuf to place data
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void readPageCRC(int page, bool readContinue, sbyte[] readBuf, int offset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.readPageCRC(int, bool, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  page=" + page);
			 Debug.debug("  readContinue=" + readContinue);
			 Debug.debug("  offset=" + offset);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  //byte[] extraInfo = new byte [extraInfoLength];

		  readPageCRC(page, readContinue, readBuf, offset, null);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Read a complete memory page with CRC verification provided by the
	   /// device with extra information.  Not supported by all devices.
	   /// See the method 'hasPageAutoCRC()'.
	   /// See the method 'hasExtraInfo()' for a description of the optional
	   /// extra information.
	   /// </summary>
	   /// <param name="page">          page number to read </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       readPagePacket() continious where the last one
	   ///                       stopped and it is inside a
	   ///                       'beginExclusive/endExclusive' block. </param>
	   /// <param name="readBuf">       byte array to put data read. Must have at least
	   ///                       'getMaxPacketDataLength()' elements. </param>
	   /// <param name="offset">        offset into readBuf to place data </param>
	   /// <param name="extraInfo">     byte array to put extra info read into
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void readPageCRC(int page, bool readContinue, sbyte[] readBuf, int offset, sbyte[] extraInfo)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.readPageCRC(int, bool, byte[], int, byte[]) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  page=" + page);
			 Debug.debug("  readContinue=" + readContinue);
			 Debug.debug("  offset=" + offset);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  // only needs to be implemented if supported by hardware
		  if (!pageAutoCRC)
		  {
			 throw new OneWireException("Read page with CRC not supported in this memory bank");
		  }

		  // attempt to put device at max desired speed
		  if (!readContinue)
		  {
			 checkSpeed();
		  }

		  // check if read exceeds memory
		  if (page > numberPages)
		  {
			 throw new OneWireException("Read exceeds memory bank end");
		  }

		  // read the scratchpad
		  readScratchpad(readBuf, offset, pageLength, extraInfo);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   //--------
	   //-------- ScratchPad methods
	   //--------

	   /// <summary>
	   /// Read the scratchpad page of memory from a NVRAM device
	   /// This method reads and returns the entire scratchpad after the byte
	   /// offset regardless of the actual ending offset
	   /// </summary>
	   /// <param name="readBuf">       byte array to place read data into
	   ///                       length of array is always pageLength. </param>
	   /// <param name="offset">        offset into readBuf to pug data </param>
	   /// <param name="len">           length in bytes to read </param>
	   /// <param name="extraInfo">     byte array to put extra info read into
	   ///                       (TA1, TA2, e/s byte)
	   ///                       length of array is always extraInfoLength.
	   ///                       Can be 'null' if extra info is not needed.
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void readScratchpad(sbyte[] readBuf, int offset, int len, sbyte[] extraInfo)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.readScratchpad(byte[], int, int, byte[]) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  offset=" + offset);
			 Debug.debug("  len=" + len);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  lock (read_scratchpad_buffer)
		  {
			 int num_crc = 0;

			 checkSpeed();

			 // select the device
			 if (!ib.adapter.select(ib.address))
			 {
				forceVerify();

				throw new OneWireIOException("Device select failed");
			 }

			 // build block
			 read_scratchpad_buffer[0] = READ_SCRATCHPAD_COMMAND;

			 Array.Copy(ffBlock, 0, read_scratchpad_buffer, 1, read_scratchpad_buffer.Length - 1);

			 // send block, command + (extra) + page data + CRC
			 ib.adapter.dataBlock(read_scratchpad_buffer, 0, read_scratchpad_buffer.Length);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("read_scratchpad_buffer", read_scratchpad_buffer);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 // get the starting offset to see when the crc will show up
			 int addr = read_scratchpad_buffer[1];

			 addr = (addr | ((read_scratchpad_buffer[2] << 8) & 0xFF00)) & 0xFFFF;

			 num_crc = pageLength + 3 + extraInfoLength;

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("num_crc=" + num_crc);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 // check crc of entire block
			 if (len == pageLength)
			 {
				if (CRC16.compute(read_scratchpad_buffer, 0, num_crc, 0) != 0x0000B001)
				{
				   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				   if (DEBUG)
				   {
					  Debug.debug("CRC16 Failed");
				   }
				   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				   forceVerify();

				   throw new OneWireIOException("Invalid CRC16 read from device");
				}
			 }

			 // optionally extract the extra info
			 if (extraInfo != null)
			 {
				Array.Copy(read_scratchpad_buffer, 1, extraInfo, 0, extraInfoLength);
			 }

			 // extract the page data
			 Array.Copy(read_scratchpad_buffer, extraInfoLength + 1, readBuf, offset, len);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Copy the scratchpad page to memory.
	   /// </summary>
	   /// <param name="addr"> the address to copy the data to </param>
	   /// <param name="len"> length byte is ignored, must always be 8.
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void copyScratchpad(int addr, int len)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.copyScratchpad(int, int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  len=" + len);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  lock (scratchpad_buffer)
		  {
			 readScratchpad(scratchpad_buffer, 0, 8, null);
			 copyScratchpad(addr, scratchpad_buffer, 0);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Copy the scratchpad page to memory.
	   /// </summary>
	   /// <param name="addr"> the address to copy to </param>
	   /// <param name="scratchpad"> the scratchpad contents that will be copied </param>
	   /// <param name="offset"> the offset into scratchpad byte[] where scratchpad data begins
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void copyScratchpad(int addr, sbyte[] scratchpad, int offset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.copyScratchpad(int, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  scratchpad", scratchpad, offset, 8);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  lock (page_data_buffer)
		  {
			 readMemory(addr & 0xE0, false, page_data_buffer, 0, 32);

			 // readMemory clears the TA address set by write scratchpad, let's re-write it
			 writeScratchpad(addr, scratchpad, offset, 8);

			 copyScratchpad(addr, scratchpad, offset, page_data_buffer, 0);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Copy the scratchpad page to memory.
	   /// </summary>
	   /// <param name="addr"> the address to copy to </param>
	   /// <param name="scratchpad"> the scratchpad contents that will be copied </param>
	   /// <param name="scratchpadOffset"> the offset into scratchpad byte[] where scratchpad data begins </param>
	   /// <param name="pageData"> the data on the page of memory to be written to </param>
	   /// <param name="pageDataOffset"> the offset into pageData byte[] where pageData begins
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void copyScratchpad(int addr, sbyte[] scratchpad, int scratchpadOffset, sbyte[] pageData, int pageDataOffset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.copyScratchpad(int, byte[], int, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  scratchpad", scratchpad, scratchpadOffset, 8);
			 Debug.debug("  pageData", pageData, pageDataOffset, 32);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  lock (MT_buffer)
		  {
			 // copy the secret into message block
			 owc33.getContainerSecret(MT_buffer, 0);
			 Array.Copy(MT_buffer,4,MT_buffer,48,4);

			 // copy the current page contents into the buffer
			 Array.Copy(pageData,pageDataOffset,MT_buffer,4,28);

			 Array.Copy(scratchpad,scratchpadOffset,MT_buffer,32,8);

			 MT_buffer[40] = (sbyte)((int)((uint)(addr & 0x0E0) >> 5));
			 Array.Copy(owc33.Address,0,MT_buffer,41,7);
			 Array.Copy(ffBlock,0,MT_buffer,52,3);

			 // put in the padding
			 MT_buffer[55] = unchecked((sbyte) 0x80);
			 Array.Copy(zeroBlock,0,MT_buffer,56,6);
			 MT_buffer[62] = (sbyte) 0x01;
			 MT_buffer[63] = unchecked((sbyte) 0xB8);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("MT_buffer", MT_buffer);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 lock (MAC_buffer)
			 {
				// do the SHA calculation to get MAC
				SHA.ComputeSHA(MT_buffer, MAC_buffer, 0);
				copyScratchpadWithMAC(addr, MAC_buffer, 0);
			 }
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }


	   /// <summary>
	   /// Copy all 8 bytes of the Sratch Pad to a certain address in memory
	   /// using the provided authorization MAC
	   /// </summary>
	   /// <param name="addr"> the address to copy the data to </param>
	   /// <param name="authMAC"> byte[] containing write authorization MAC </param>
	   /// <param name="authOffset"> offset into authMAC where authorization MAC begins
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void copyScratchpadWithMAC(int addr, sbyte[] authMAC, int authOffset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.copyScratchpadWithMAC(int, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  authMAC", authMAC, authOffset, 20);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  lock (copy_scratchpad_buffer)
		  {
			 sbyte[] send_block = copy_scratchpad_buffer;

			 checkSpeed();

			 // access the device
			 if (ib.adapter.select(ib.Address))
			 {
				// ending address with data status
				send_block[3] = 0x5F; //ES - always 0x5F

				// address 2
				send_block[2] = unchecked((sbyte)((addr >> 8) & 0x0FF)); //TA2

				// address 1
				send_block[1] = unchecked((sbyte)((addr) & 0x0FF)); //TA1;

				// Copy command
				send_block[0] = COPY_SCRATCHPAD_COMMAND;

				// send copy scratchpad command
				ib.adapter.dataBlock(send_block, 0, 4);

				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				if (DEBUG)
				{
				   Debug.debug("  send_block", send_block, 0, 4);
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

				// pause before sending appropriate MAC
				try
				{
				   Thread.Sleep(2);
				}
				catch (InterruptedException)
				{
				}

				// sending MAC
				ib.adapter.dataBlock(authMAC, authOffset, 19);

				// provide strong pull-up for copy
				ib.adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
				ib.adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);
				ib.adapter.putByte(authMAC[authOffset + 19]);

				// pause before checking result
				try
				{
				   Thread.Sleep(12);
				}
				catch (InterruptedException)
				{
				}

				ib.adapter.setPowerNormal();

				// get result
				sbyte test = (sbyte) ib.adapter.Byte;

				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				if (DEBUG)
				{
				   Debug.debug("  result=0x" + Convert.toHexString((sbyte)test));
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

				if ((test != unchecked((sbyte) 0xAA)) && (test != (sbyte) 0x55))
				{
				   if (test == unchecked((sbyte) 0xFF))
				   {
					  throw new OneWireException("That area of memory is write-protected.");
				   }
				   else if (test == (sbyte)0x00)
				   {
					  throw new OneWireIOException("Error due to not matching MAC.");
				   }
				}
			 }
			 else
			 {
				throw new OneWireIOException("Device select failed.");
			 }
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Write to the scratchpad page of memory a NVRAM device.
	   /// </summary>
	   /// <param name="addr"> physical address to copy data to </param>
	   /// <param name="writeBuf"> byte array containing data to write </param>
	   /// <param name="offset"> offset into readBuf to place data </param>
	   /// <param name="len"> length in bytes to write
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void writeScratchpad(int addr, sbyte[] writeBuf, int offset, int len)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.writeScratchpad(int, byte[], int, int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  writeBuf", writeBuf, offset, len);
			 Debug.stackTrace();
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  checkSpeed();

		  base.writeScratchpad(addr, writeBuf, offset, len);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("MemoryBankScratchSHAEE.writeScratchpad(int, byte[], int, int) finished");
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }


	   /// <summary>
	   /// Write  memory in the current bank.  It is recommended that
	   /// when writing  data that some structure in the data is created
	   /// to provide error free reading back with read().  Or the
	   /// method 'writePagePacket()' could be used which automatically
	   /// wraps the data in a length and CRC.
	   /// 
	   /// When using on Write-Once devices care must be taken to write into
	   /// into empty space.  If write() is used to write over an unlocked
	   /// page on a Write-Once device it will fail.  If write verification
	   /// is turned off with the method 'setWriteVerification(false)' then
	   /// the result will be an 'AND' of the existing data and the new data.
	   /// </summary>
	   /// <param name="addr">          the address to write to </param>
	   /// <param name="writeBuf">      byte array containing data to write </param>
	   /// <param name="offset">        offset into writeBuf to get data </param>
	   /// <param name="len">           length in bytes to write
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void write(int addr, sbyte[] writeBuf, int offset, int len)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.write(int, byte[], int, int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  writeBuf", writeBuf, offset, len);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		 writeScratchpad(addr, writeBuf, offset, len);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   ///  Load First Secret for the DS2432.  Loads the specified data
	   ///  to the specified location.  If the address is data memory
	   ///  (instead of secret memory), this command must have been preceded by
	   ///  a Refresh Scratchpad command for it to be successful.
	   /// </summary>
	   /// <param name="addr"> the address to write the data to </param>
	   /// <param name="data"> the data to 'load' with the Load First Secret command </param>
	   /// <param name="offset"> the offset to use for reading the data byte[]
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void loadFirstSecret(int addr, sbyte[] data, int offset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.loadFirstSecret(int, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString(addr));
			 Debug.debug("  data", data, offset, 8);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  writeScratchpad(addr, data, offset, 8);
		  loadFirstSecret(addr);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }



	   /// <summary>
	   ///  Load First Secret for the DS2432.  Loads current contents of the
	   ///  scratchpad to the specified location.  If the address is data memory
	   ///  (instead of secret memory), this command must have been preceded by
	   ///  a Refresh Scratchpad command for it to be successful.
	   /// </summary>
	   /// <param name="addr"> the address to write the data to
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void loadFirstSecret(int addr)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.loadFirstSecret(int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  sbyte[] send_block = new sbyte[4];

		  checkSpeed();

		  // access the device
		  if (ib.adapter.select(ib.Address))
		  {
			 send_block [0] = LOAD_FIRST_SECRET;
			 send_block [1] = unchecked((sbyte)(addr & 0x00FF));
			 send_block [2] = unchecked((sbyte)(((int)((uint)addr >> 8)) & 0x00FF));
			 send_block [3] = (sbyte) 0x5F; // Should be 0x5F,not ( byte ) ((addr + 7) & 0x01F);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 if (DEBUG)
			 {
				Debug.debug("send_block", send_block, 0, 4);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

			  // now send the block
			 ib.adapter.dataBlock(send_block,0,3);

			 // provide strong pull-up for load
			 ib.adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 ib.adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);
			 ib.adapter.putByte(send_block[3]);

			 try
			 {
				Thread.Sleep(20);
			 }
			 catch (InterruptedException)
			 {
			 }

			 ib.adapter.setPowerNormal();

			 sbyte test = (sbyte) ib.adapter.Byte;

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 if (DEBUG)
			 {
				Debug.debug("result=" + Convert.toHexString(test));
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

			 if ((test != unchecked((sbyte) 0xAA)) && (test != (sbyte) 0x55))
			 {
				throw new OneWireException("Error due to invalid load.");
			 }

			 // if data is loaded to secrets memory, lets read it so we can
			 // set the container secret
			 if (addr == 0x080)
			 {
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
				if (DEBUG)
				{
				   Debug.debug("reading scratchpad and setting container secret");
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

				sbyte[] secret = new sbyte[8];
				readScratchpad(secret, 0, 8, null);
				owc33.setContainerSecret(secret, 0);
			 }
		  }
		  else
		  {
			 throw new OneWireIOException("Device select failed.");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Computes the next secret.
	   /// </summary>
	   /// <param name="addr"> the physical address of the page to use for secret computation </param>
	   /// <param name="partialsecret"> byte array containing next partial secret for writing to the scratchpad
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void computeNextSecret(int addr)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.computeNextSecret(int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  sbyte[] send_block = new sbyte [3];
		  sbyte[] scratch = new sbyte [8];
		  sbyte[] next_secret = null;

		  // check to see if secret is set
		  if (owc33.ContainerSecretSet)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("Calculating next secret for container");
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 sbyte[] memory = new sbyte [32];
			 sbyte[] secret = new sbyte [8];
			 sbyte[] MT = new sbyte [64];

			 readMemory(addr & 0xE0, false, memory, 0, 32);

			 owc33.getContainerSecret(secret, 0);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("currentSecret", secret, 0, 8);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 Array.Copy(secret,0,MT,0,4);
			 Array.Copy(memory,0,MT,4,32);
			 Array.Copy(ffBlock,0,MT,36,4);
			 readScratchpad(MT, 40, 8, null);
			 MT[40] = (sbyte)(MT[40] & (sbyte) 0x3F);
			 Array.Copy(secret,4,MT,48,4);
			 Array.Copy(ffBlock,0,MT,52,3);

			 // message padding
			 MT[55] = unchecked((sbyte) 0x80);
			 Array.Copy(zeroBlock,0,MT,56,6);
			 MT[62] = (sbyte) 0x01;
			 MT[63] = unchecked((sbyte) 0xB8);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("MT", MT, 0, 64);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 int[] AtoE = new int [5];
			 SHA.ComputeSHA(MT,AtoE);

			 //copy E into secret
			 for (int temp = AtoE[4],i = 0;i < 4;i++)
			 {
				secret[i] = unchecked((sbyte)(temp & 0x0FF));
				temp >>= 8;
			 }
			 //copy D into secret
			 for (int temp = AtoE[3],i = 4;i < 8;i++)
			 {
				secret[i] = unchecked((sbyte)(temp & 0x0FF));
				temp >>= 8;
			 }
			 next_secret = secret;

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("nextSecret", secret, 0, 8);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  }

		  checkSpeed();

		  // access the device
		  if (ib.adapter.select(ib.Address))
		  {
			 // Next Secret command
			 send_block[0] = COMPUTE_NEXT_SECRET;
			 // address 1
			 send_block[1] = unchecked((sbyte)(addr & 0xFF));
			 // address 2
			 send_block[2] = unchecked((sbyte)(((int)((uint)(addr & 0xFFFF) >> 8)) & 0xFF));

			 // now send the block
			 ib.adapter.dataBlock(send_block,0,2);

			 // provide strong pull-up for compute next secret
			 ib.adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 ib.adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);
			 ib.adapter.putByte(send_block[2]);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("sendblock ", send_block, 0, 3);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 try
			 {
				Thread.Sleep(14);
			 }
			 catch (InterruptedException)
			 {
			 }

			 ib.adapter.setPowerNormal();

			 readScratchpad(scratch,0,8,null);
			 for (int i = 0;i < 8;i++)
			 {
				if (scratch[i] != unchecked((sbyte) 0xAA))
				{
				   throw new OneWireIOException("Next secret not calculated.");
				}
			 }
			 if (next_secret != null)
			 {
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				if (DEBUG)
				{
				   Debug.debug("setting container secret", next_secret, 0, 8);
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				owc33.setContainerSecret(next_secret, 0);
			 }
		  }
		  else
		  {
			 throw new OneWireIOException("Device select failed.");
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Computes the next secret.
	   /// </summary>
	   /// <param name="addr"> the physical address of the page to use for secret computation </param>
	   /// <param name="partialsecret"> byte array containing next partial secret for writing to the scratchpad </param>
	   /// <param name="offset"> into partialsecret byte array to start reading
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void computeNextSecret(int addr, sbyte[] partialsecret, int offset)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.computeNextSecret(int, byte[], int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.debug("  partialsecret", partialsecret, offset, 8);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  sbyte[] send_block = new sbyte [3];
		  sbyte[] scratch = new sbyte [8];
		  sbyte[] next_secret = null;

		  writeScratchpad(addr, partialsecret, 0, 8);

		  // check to see if secret is set
		  if (owc33.ContainerSecretSet)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("Calculating next secret for container");
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 sbyte[] memory = new sbyte [32];
			 sbyte[] secret = new sbyte [8];
			 sbyte[] MT = new sbyte [64];

			 readMemory(addr & 0xE0, false, memory, 0, 32);

			 owc33.getContainerSecret(secret, 0);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("currentSecret", secret, 0, 8);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 Array.Copy(secret,0,MT,0,4);
			 Array.Copy(memory,0,MT,4,32);
			 Array.Copy(ffBlock,0,MT,36,4);
			 MT[40] = (sbyte)(partialsecret[0] & (sbyte) 0x3F);
			 Array.Copy(partialsecret,1,MT,41,7);
			 Array.Copy(secret,4,MT,48,4);
			 Array.Copy(ffBlock,0,MT,52,3);

			 // message padding
			 MT[55] = unchecked((sbyte) 0x80);
			 Array.Copy(zeroBlock,0,MT,56,6);
			 MT[62] = (sbyte) 0x01;
			 MT[63] = unchecked((sbyte) 0xB8);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("MT", MT, 0, 64);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 int[] AtoE = new int [5];
			 SHA.ComputeSHA(MT,AtoE);

			 //copy E into secret
			 for (int temp = AtoE[4],i = 0;i < 4;i++)
			 {
				secret[i] = unchecked((sbyte)(temp & 0x0FF));
				temp >>= 8;
			 }
			 //copy D into secret
			 for (int temp = AtoE[3],i = 4;i < 8;i++)
			 {
				secret[i] = unchecked((sbyte)(temp & 0x0FF));
				temp >>= 8;
			 }
			 next_secret = secret;

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("nextSecret=", secret, 0, 8);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  }

		  // access the device
		  if (ib.adapter.select(ib.Address))
		  {
			 // Next Secret command
			 send_block[0] = COMPUTE_NEXT_SECRET;
			 // address 1
			 send_block[1] = unchecked((sbyte)(addr & 0xFF));
			 // address 2
			 send_block[2] = unchecked((sbyte)(((int)((uint)(addr & 0xFFFF) >> 8)) & 0xFF));

			 // now send the block
			 ib.adapter.dataBlock(send_block,0,2);

			 // provide strong pull-up for compute next secret
			 ib.adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 ib.adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);
			 ib.adapter.putByte(send_block[2]);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("sendblock ", send_block, 0, 3);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 try
			 {
				Thread.Sleep(14);
			 }
			 catch (InterruptedException)
			 {
			 }

			 ib.adapter.setPowerNormal();

			 readScratchpad(scratch,0,8,null);
			 for (int i = 0;i < 8;i++)
			 {
				if (scratch[i] != unchecked((sbyte) 0xAA))
				{
				   throw new OneWireIOException("Next secret not calculated.");
				}
			 }

			 if (next_secret != null)
			 {
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				if (DEBUG)
				{
				   Debug.debug("setting container secret", next_secret, 0, 8);
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				owc33.setContainerSecret(next_secret, 0);
			 }
		  }
		  else
		  {
			 throw new OneWireIOException("Device select failed.");
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }


	   /// <summary>
	   /// Refreshes the scratchpad for DS1961S.  Command has no effect on DS2432
	   /// devices.  After this command is executed, the data at the address
	   /// specified will be loaded into the scratchpad.  The Load First Secret
	   /// command can then be used to re-write the data back to the page, correcting
	   /// any weakly-programmed EEPROM bits.
	   /// </summary>
	   /// <param name="addr"> the address to load the data from into the scratchpad
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void refreshScratchpad(int addr)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.refreshScratchpad(int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  addr=0x" + Convert.toHexString((sbyte)addr));
			 Debug.stackTrace();
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  checkSpeed();

		  // access the device
		  if (ib.adapter.select(ib.Address))
		  {
			 sbyte[] send_block = new sbyte[13];

			 send_block [0] = REFRESH_SCRATCHPAD;
			 send_block [1] = unchecked((sbyte)(addr & 0x00FF));
			 send_block [2] = unchecked((sbyte)(((int)((uint)addr >> 8)) & 0x00FF));
			 for (int i = 3; i < 11; i++)
			 {
				send_block[i] = (sbyte)0x00;
			 }
			 send_block[11] = unchecked((sbyte)0xFF);
			 send_block[12] = unchecked((sbyte)0xFF);

			  // now send the block
			 ib.adapter.dataBlock(send_block, 0, 13);

			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 if (DEBUG)
			 {
				Debug.debug("send_block", send_block, 0, 13);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

			 if (CRC16.compute(send_block, 0, 13, 0) != 0x0B001)
			 {
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				if (DEBUG)
				{
				   Debug.debug("   Refresh Scratchpad failed because of bad CRC16");
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				throw new OneWireException("Bad CRC16 on Refresh Scratchpad");
			 }
		  }
		  else
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("   Refresh Scratchpad failed because there is no device");
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 throw new OneWireIOException("Device select failed.");
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// Reads actual memory (not scratchpad memory) with no CRC checking (device or
	   /// data). The resulting data from this API may or may not be what is on
	   /// the 1-Wire device.  It is recommends that the data contain some kind
	   /// of checking (CRC) like in the readPagePacket() method or have
	   /// the 1-Wire device provide the CRC as in readPageCRC().  readPageCRC()
	   /// however is not supported on all memory types, see 'hasPageAutoCRC()'.
	   /// If neither is an option then this method could be called more
	   /// then once to at least verify that the same thing is read consistantly.
	   /// </summary>
	   /// <param name="startAddr">     starting physical address </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       read() continious where the last one led off
	   ///                       and it is inside a 'beginExclusive/endExclusive'
	   ///                       block. </param>
	   /// <param name="readBuf">       byte array to place read data into </param>
	   /// <param name="offset">        offset into readBuf to place data </param>
	   /// <param name="len">           length in bytes to read
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   private void readMemory(int startAddr, bool readContinue, sbyte[] readBuf, int offset, int len)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("-----------------------------------------------------------");
			 Debug.debug("MemoryBankScratchSHAEE.readMemory(int, bool, byte[], int, int) called");
			 Debug.debug("  romID=" + owc33.AddressAsString);
			 Debug.debug("  startAddr=0x" + Convert.toHexString((sbyte)startAddr));
			 Debug.debug("  readContinue=" + readContinue);
			 Debug.debug("  offset=" + offset);
			 Debug.debug("  len=" + len);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  // attempt to put device at max desired speed
		  if (!readContinue)
		  {
			 checkSpeed();
		  }

		  // see if need to access the device
		  if (!readContinue)
		  {
			 // select the device
			 if (!ib.adapter.select(ib.Address))
			 {
				throw new OneWireIOException("Device select failed.");
			 }

			 // build start reading memory block
			 readBuf [offset] = unchecked((sbyte) 0xF0); // READ MEMORY, no CRC, no MAC
			 readBuf [offset + 1] = unchecked((sbyte)(startAddr & 0xFF));
			 readBuf [offset + 2] = unchecked((sbyte)(((int)((uint)(startAddr & 0xFFFF) >> 8)) & 0xFF));

			 // do the first block for command, address
			 ib.adapter.dataBlock(readBuf, offset, 3);
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.debug("  readBuf", readBuf, offset, 3);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  }

		  // pre-fill readBuf with 0xFF
		  int pgs = len / 32;
		  int extra = len % 32;

		  for (int i = 0; i < pgs; i++)
		  {
			 Array.Copy(ffBlock, 0, readBuf, offset + i * 32, 32);
		  }
		  if (extra > 0)
		  {
			 Array.Copy(ffBlock, 0, readBuf, offset + pgs * 32, extra);
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  // send second block to read data, return result
		  ib.adapter.dataBlock(readBuf, offset, len);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.debug("  readBuf", readBuf, offset, len);
			 Debug.debug("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
	   }

	   /// <summary>
	   /// helper method to pause for specified milliseconds
	   /// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private static final void msWait(final long ms)
	   private static void msWait(long ms)
	   {
		  try
		  {
			 Thread.Sleep(ms);
		  }
		  catch (InterruptedException)
		  {
			  ;
		  }
	   }
	}

}