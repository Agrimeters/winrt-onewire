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

namespace com.dalsemi.onewire.utils
{

	/// <summary>
	/// Utilities for bit operations on an array.
	/// 
	/// @version    0.00, 27 August 2000
	/// @author     DS
	/// </summary>
	public class Bit
	{

	   /// <summary>
	   /// Write the bit state in a byte array.
	   /// </summary>
	   /// <param name="state"> new state of the bit 1, 0 </param>
	   /// <param name="index"> bit index into byte array </param>
	   /// <param name="offset"> byte offset into byte array to start </param>
	   /// <param name="buf"> byte array to manipulate </param>
	   public static void arrayWriteBit(int state, int index, int offset, byte[] buf)
	   {
		  int nbyt = ((int)((uint)index >> 3));
		  int nbit = index - (nbyt << 3);

		  if (state == 1)
		  {
			 buf [nbyt + offset] |= (byte)(0x01 << nbit);
		  }
		  else
		  {
			 buf [nbyt + offset] &= (byte)(~(0x01 << nbit));
		  }
	   }

	   /// <summary>
	   /// Read a bit state in a byte array.
	   /// </summary>
	   /// <param name="index"> bit index into byte array </param>
	   /// <param name="offset"> byte offset into byte array to start </param>
	   /// <param name="buf"> byte array to read from
	   /// </param>
	   /// <returns> bit state 1 or 0 </returns>
	   public static int arrayReadBit(int index, int offset, byte[] buf)
	   {
		  int nbyt = ((int)((uint)index >> 3));
		  int nbit = index - (nbyt << 3);

		  return (((int)((uint)buf [nbyt + offset] >> nbit)) & 0x01);
	   }
	}

}