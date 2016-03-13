using System.IO;
using System.Text;
using Windows.Storage.Streams;

//TODO add Dispose...

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

namespace com.dalsemi.onewire.adapter
{

	/// <summary>
	/// Raw Send Packet that contains a StingBuffer of bytes to send and
	///  an expected return length.
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal class RawSendPacket
	{

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// StringBuffer of bytes to send
	   /// </summary>
       public MemoryStream buffer;
       public BinaryWriter writer;

	   /// <summary>
	   /// Expected length of return packet
	   /// </summary>
	   public int returnLength;

	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Construct and initiailize the raw send packet
	   /// </summary>
	   public RawSendPacket()
	   {
          buffer = new MemoryStream();
          writer = new BinaryWriter(buffer);
		  returnLength = 0;
	   }
	}

}