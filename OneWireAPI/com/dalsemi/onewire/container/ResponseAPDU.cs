using System;

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

// ResponseAPDU.java
namespace com.dalsemi.onewire.container
{

	/// <summary>
	/// A <code>ResponseAPDU</code> represents an Application Protocol Data Unit (APDU)
	/// received from the smart card in response to a previous <code>CommandAPDU</code>.
	/// A <code>ResponseAPDU</code> consists of an optional body and a mandatory
	/// Status Word (SW). <BR><BR>
	/// 
	/// According to ISO 7816-4, a <code>ResponseAPDU</code>  has the following
	/// format:
	/// 
	/// <pre>
	///          DATA   |  STATUS WORD
	///         [data]  |  SW1     SW2 </pre>
	/// 
	/// where
	/// <ul>
	/// <li><code>data</code> is an optional byte array of data received from the smart card.
	/// <li><code>SW1</code>  is the status byte 1 containing command processing status.
	/// <li><code>SW2</code>  is the status byte 2 containing command processing qualifier.
	/// </ul>
	/// 
	/// 
	/// <H3> Usage </H3> 
	/// <OL>
	/// <LI>
	///   <code><pre>
	///   byte[] buffer = {(byte)0x01, (byte)0x02, (byte)0x90, (byte)0x00};
	///   ResponseAPDU rapdu = new ResponseAPDU(buffer); </pre></code>
	/// <LI>
	///   <code><pre>
	///   OneWireContainer16 owc16 = new OneWireContainer16(adapter, address);
	///   byte[] buffer = {(byte)0x90, (byte)0x00, (byte)0x00, (byte)0x00, 
	///                    (byte)0x01, (byte)0x02, (byte)0x03};
	///   CommandAPDU capdu = new CommandAPDU(buffer);
	///   ResponseAPDU rapdu = owc16.sendAPDU(capdu, runTime); </pre></code>
	/// </OL>
	/// 
	/// <H3> Additonal information </H3> 
	/// <DL>
	/// <DD><A HREF="http://www.opencard.org"> http://www.opencard.org</A>
	/// </DL>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.CommandAPDU </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer16
	/// 
	/// @version    0.00, 28 Aug 2000
	/// @author     YL
	///  </seealso>
	public class ResponseAPDU
	{

	   /// <summary>
	   /// byte array containing the entire <code>ResponseAPDU</code> </summary>
	   protected internal sbyte[] apduBuffer = null;

	   /// <summary>
	   /// length of this <code>ResponseAPDU</code> currently in the 
	   ///    <code>apduBuffer</code> 
	   /// </summary>
	   protected internal int apduLength;

	   /// <summary>
	   /// Constructs a new <code>ResponseAPDU</code> with the given buffer 
	   /// byte array. The internal <code>apduLength</code> is set to the 
	   /// length of the buffer passed.
	   /// </summary>
	   /// <param name="buffer">  the byte array with data for the internal
	   ///                 <code>apduBuffer</code>
	   /// </param>
	   /// <exception cref="RuntimeException"> thrown when <code>buffer</code> length 
	   ///                          < <code>2</code>.
	   /// </exception>
	   /// <seealso cref=    CommandAPDU </seealso>
	   public ResponseAPDU(sbyte[] buffer)
	   {
		  if (buffer.Length < 2)
		  {
			 throw new Exception("invalid ResponseAPDU, " + "length must be at least 2 bytes");
		  }

		  apduLength = buffer.Length;
		  apduBuffer = new sbyte [apduLength];

		  Array.Copy(buffer, 0, apduBuffer, 0, apduLength);
	   } // ResponseAPDU

	   /// <summary>
	   /// Gets the data field of this <code>ResponseAPDU</code>.
	   /// </summary>
	   /// <returns> a byte array containing this <code>ResponseAPDU</code> data field </returns>
	   public virtual sbyte[] Data
	   {
		   get
		   {
			  if (apduLength > 2)
			  {
				 sbyte[] data = new sbyte [apduLength - 2];
    
				 Array.Copy(apduBuffer, 0, data, 0, apduLength - 2);
    
				 return data;
			  }
			  else
			  {
				 return null;
			  }
		   }
	   } // data

	   /// <summary>
	   /// Gets the value of SW1 and SW2 as an integer.
	   /// It is computed as:<BR><BR>
	   /// <code>(((SW1 << 8) & 0xFF00) | (SW2 & 0xFF))</code><BR>
	   /// </summary>
	   /// <returns>    <code>(((SW1 << 8) & 0xFF00) | (SW2 & 0xFF))</code> as an integer </returns>
	   public int SW
	   {
		   get
		   {
			  return (((SW1 << 8) & 0xFF00) | (SW2 & 0xFF));
		   }
	   } // getSW

	   /// <summary>
	   /// Gets the value of SW1.
	   /// </summary>
	   /// <returns>    value of SW1 as a byte </returns>
	   public sbyte SW1
	   {
		   get
		   {
			  return apduBuffer [apduLength - 2];
		   }
	   } // getSW1

	   /// <summary>
	   /// Gets the value of SW2.
	   /// </summary>
	   /// <returns>    value of SW2 as a byte </returns>
	   public sbyte SW2
	   {
		   get
		   {
			  return apduBuffer [apduLength - 1];
		   }
	   } // getSW2

	   /// <summary>
	   /// Gets the byte value at the specified offset in <code>apduBuffer</code>.
	   /// </summary>
	   /// <param name="index">   the offset in the <code>apduBuffer</code> </param>
	   /// <returns>        the value at the given offset,
	   ///                or <code>-1</code> if the offset is invalid
	   /// </returns>
	   /// <seealso cref= #getBytes </seealso>
	   /// <seealso cref= #getLength </seealso>
	   public sbyte getByte(int index)
	   {
		  if (index >= apduLength)
		  {
			 return (sbyte)-1; // read beyond end of ResponseAPDU
		  }

		  return (apduBuffer [index]);
	   } // getByte

	   /// <summary>
	   /// Gets a byte array holding this <code>ResponseAPDU</code> 
	   /// <code>apduBuffer</code>.
	   /// </summary>
	   /// <returns>  <code>apduBuffer</code> copied into a new array
	   /// </returns>
	   /// <seealso cref= #getByte </seealso>
	   /// <seealso cref= #getLength </seealso>
	   public sbyte[] Bytes
	   {
		   get
		   {
			  sbyte[] apdu = new sbyte [apduLength];
    
			  Array.Copy(apduBuffer, 0, apdu, 0, apduLength);
    
			  return apdu;
		   }
	   } // getBytes

	   /// <summary>
	   /// Gets the length of <code>apduBuffer</code>.
	   /// </summary>
	   /// <returns>  <code>apduLength</code> the length of the 
	   ///          <code>apduBuffer</code> currently stored </returns>
	   public int Length
	   {
		   get
		   {
			  return apduLength;
		   }
	   } // getLength

	   /// <summary>
	   /// Gets a string representation of this <code>ResponseAPDU</code>.
	   /// </summary>
	   /// <returns> a string describing this <code>ResponseAPDU</code> </returns>
	   public override string ToString()
	   {
		  string apduString = "";

		  if (apduLength > 2)
		  {
			 sbyte[] dataBuffer = new sbyte [apduLength - 2];

			 dataBuffer = Data;
			 apduString += "DATA = ";

			 for (int i = 0; i < dataBuffer.Length; i++)
			 {

				// make hex String representation of byte array
				if ((dataBuffer [i] & 0xFF) < 0x10)
				{
				   apduString += '0';
				}

				apduString += ((int)(dataBuffer [i] & 0xFF)).ToString("x") + " ";
			 }

			 apduString += " | ";
		  }

		  apduString += "SW1 = ";

		  if ((SW1 & 0xFF) < 0x10)
		  {
			 apduString += '0';
		  }

		  apduString += (SW1 & 0xFF).ToString("x");
		  apduString += ", SW2 = ";

		  if ((SW2 & 0xFF) < 0x10)
		  {
			 apduString += '0';
		  }

		  apduString += (SW2 & 0xFF).ToString("x");

		  return (apduString.ToUpper());
	   } // toString
	}

}