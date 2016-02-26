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
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;


	/// <summary>
	///  Scratchpad interface for Memory banks that require it.
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal interface ScratchPad
	{

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
	   void readScratchpad(sbyte[] readBuf, int offset, int len, sbyte[] extraInfo);

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
	   void writeScratchpad(int startAddr, sbyte[] writeBuf, int offset, int len);

	   /// <summary>
	   /// Copy the scratchpad page to memory.
	   /// </summary>
	   /// <param name="startAddr">     starting address </param>
	   /// <param name="len">           length in bytes that was written already
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   void copyScratchpad(int startAddr, int len);

	   /// <summary>
	   /// Query to get the length in bytes of extra information that
	   /// is read when read a page in the current memory bank.  See
	   /// 'hasExtraInfo()'.
	   /// </summary>
	   /// <returns>  number of bytes in Extra Information read when reading
	   ///          pages in the current memory bank. </returns>
	   int ExtraInfoLength {get;}

	   /// <summary>
	   /// Check the device speed if has not been done before or if
	   /// an error was detected.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   void checkSpeed();

	   /// <summary>
	   /// Set the flag to indicate the next 'checkSpeed()' will force
	   /// a speed set and verify.
	   /// </summary>
	   void forceVerify();
	}

}