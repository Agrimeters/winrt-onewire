﻿using System;
using System.Threading;

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

namespace com.dalsemi.onewire.container
{

	// imports
	using com.dalsemi.onewire.adapter;
	using com.dalsemi.onewire.utils;



	/// <summary>
	/// Memory bank class for the Scratchpad section of EEPROM iButtons and
	/// 1-Wire devices.
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal class MemoryBankScratchEE : MemoryBankScratch
	{
	   /// <summary>
	   /// Copy Scratchpad Delay length
	   /// </summary>
	   protected internal sbyte COPY_DELAY_LEN;

	   /// <summary>
	   /// Mask for ES byte during copy scratchpad
	   /// </summary>
	   protected internal sbyte ES_MASK;

	   /// <summary>
	   /// Number of bytes to read for verification (only last one will be checked).
	   /// </summary>
	   protected internal int numVerificationBytes = 1;

	   //--------
	   //-------- Constructor
	   //--------

	   /// <summary>
	   /// Memory bank contstuctor.  Requires reference to the OneWireContainer
	   /// this memory bank resides on.
	   /// </summary>
	   public MemoryBankScratchEE(OneWireContainer ibutton) : base(ibutton)
	   {

		  // default copy scratchpad delay
		  COPY_DELAY_LEN = (sbyte) 5;

		  // default ES mask for copy scratchpad 
		  ES_MASK = 0;
	   }

	   //--------
	   //-------- ScratchPad methods
	   //--------

	   /// <summary>
	   /// Write to the scratchpad page of memory a NVRAM device.
	   /// </summary>
	   /// <param name="startAddr">     starting address </param>
	   /// <param name="writeBuf">      byte array containing data to write </param>
	   /// <param name="offset">        offset into readBuf to place data </param>
	   /// <param name="len">           length in bytes to write
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void writeScratchpad(int startAddr, sbyte[] writeBuf, int offset, int len)
	   {
		  bool calcCRC = false;

		  // select the device
		  if (!ib.adapter.select(ib.address))
		  {
			 forceVerify();

			 throw new OneWireIOException("Device select failed");
		  }

		  // build block to send
		  sbyte[] raw_buf = new sbyte [37];

		  raw_buf [0] = WRITE_SCRATCHPAD_COMMAND;
		  raw_buf [1] = unchecked((sbyte)(startAddr & 0xFF));
		  raw_buf [2] = unchecked((sbyte)(((int)((uint)(startAddr & 0xFFFF) >> 8)) & 0xFF));

		  Array.Copy(writeBuf, offset, raw_buf, 3, len);

		  // check if full page (can utilize CRC)
		  if (((startAddr + len) % pageLength) == 0)
		  {
			 Array.Copy(ffBlock, 0, raw_buf, len + 3, 2);

			 calcCRC = true;
		  }

		  // send block, return result 
		  ib.adapter.dataBlock(raw_buf, 0, len + 3 + ((calcCRC) ? 2 : 0));

		  // check crc
		  if (calcCRC)
		  {
			 if (CRC16.compute(raw_buf, 0, len + 5, 0) != 0x0000B001)
			 {
				forceVerify();

				throw new OneWireIOException("Invalid CRC16 read from device");
			 }
		  }
	   }

	   /// <summary>
	   /// Copy the scratchpad page to memory.
	   /// </summary>
	   /// <param name="startAddr">     starting address </param>
	   /// <param name="len">           length in bytes that was written already
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override void copyScratchpad(int startAddr, int len)
	   {

		  // select the device
		  if (!ib.adapter.select(ib.address))
		  {
			 forceVerify();

			 throw new OneWireIOException("Device select failed");
		  }

		  // build block to send
		  sbyte[] raw_buf = new sbyte [3];

		  raw_buf [0] = COPY_SCRATCHPAD_COMMAND;
		  raw_buf [1] = unchecked((sbyte)(startAddr & 0xFF));
		  raw_buf [2] = unchecked((sbyte)(((int)((uint)(startAddr & 0xFFFF) >> 8)) & 0xFF));

		  // send block (command, address)
		  ib.adapter.dataBlock(raw_buf, 0, 3);

		  try
		  {

			 // setup strong pullup
			 ib.adapter.PowerDuration = DSPortAdapter.DELIVERY_INFINITE;
			 ib.adapter.startPowerDelivery(DSPortAdapter.CONDITION_AFTER_BYTE);


			 // send the offset and start power delivery
			 ib.adapter.putByte((sbyte)(((startAddr + len - 1) & (pageLength - 1))) | ES_MASK);

			 // delay for ms
			 Thread.Sleep(COPY_DELAY_LEN);

			 // disable power
			 ib.adapter.setPowerNormal();

			 // check if complete
			 sbyte rslt = 0;
			 if (numVerificationBytes == 1)
			 {
				rslt = (sbyte) ib.adapter.Byte;
			 }
			 else
			 {
				raw_buf = new sbyte[numVerificationBytes];
				ib.adapter.getBlock(raw_buf, 0, numVerificationBytes);
				rslt = raw_buf[numVerificationBytes - 1];
			 }

			 if ((unchecked((sbyte)(rslt & 0x0F0)) != unchecked((sbyte) 0xA0)) && (unchecked((sbyte)(rslt & 0x0F0)) != (sbyte) 0x50))
			 {
				forceVerify();

				throw new OneWireIOException("Copy scratchpad complete not found");
			 }
		  }
		  catch (InterruptedException)
		  {
		  }
		  ;
	   }
	}

}