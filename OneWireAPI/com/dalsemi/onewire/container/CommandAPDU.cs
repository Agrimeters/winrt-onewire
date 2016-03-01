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

// CommandAPDU.java
namespace com.dalsemi.onewire.container
{

	/// <summary>
	/// A <code>CommandAPDU</code> represents an ISO 7816-4 specified
	/// Application Protocol Data Unit (APDU) sent to a
	/// smart card. A response from the smart card is in turn represented
	/// by a <code>ResponseAPDU</code>.<BR><BR>
	/// 
	/// According to ISO 7816-4, a <code>CommandAPDU</code> has the following
	/// format: <pre>
	///                  HEADER         |           BODY
	///         CLA    INS    P1    P2  |  [LC]    [DATA]    [LE]</pre>
	/// where
	/// <ul>
	/// <li><code>CLA</code>  is the class byte
	/// <li><code>INS</code>  is the instruction byte
	/// <li><code>P1</code>   is the first parameter byte
	/// <li><code>P2</code>   is the second parameter byte
	/// <li><code>LC</code>   is the number of bytes present in the data block
	/// <li><code>DATA</code> is an byte array of data to be sent
	/// <li><code>LE</code>   is the maximum number of bytes expected in the <code>ResponseAPDU</code>
	/// <li><code>[ ]</code>  denotes optional fields
	/// </ul>
	/// 
	/// <H3> Usage </H3> 
	/// <OL>
	/// <LI> 
	///    <code><pre>
	///   byte[] buffer = {(byte)0x90, (byte)0x00, (byte)0x00, (byte)0x00, 
	///                    (byte)0x01, (byte)0x02, (byte)0x03};
	///   CommandAPDU capdu = new CommandAPDU(buffer); </pre></code>
	/// <LI>
	///   <code><pre>
	///   CommandAPDU capdu = new CommandAPDU((byte)0x90, (byte)0x00, (byte)0x00, (byte)0x00, 
	///                                       (byte)0x01, (byte)0x02, (byte)0x03);</pre></code>
	/// </OL>
	/// 
	/// <H3> Additonal information </H3> 
	/// <DL>
	/// <DD><A HREF="http://www.opencard.org"> http://www.opencard.org</A>
	/// </DL>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.ResponseAPDU </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer16
	/// 
	/// @version    0.00, 28 Aug 2000
	/// @author     YL
	///  </seealso>
	public class CommandAPDU
	{

	   /// <summary>
	   /// Index for addressing <code>CLA</code> in this <code>CommandAPDU</code>
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public const int CLA_Renamed = 0;

	   /// <summary>
	   /// Index for addressing <code>INS</code> in this <code>CommandAPDU</code>
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public const int INS_Renamed = 1;

	   /// <summary>
	   /// Index for addressing <code>P1</code>  in this <code>CommandAPDU</code>
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public const int P1_Renamed = 2;

	   /// <summary>
	   /// Index for addressing <code>P2</code>  in this <code>CommandAPDU</code>
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public const int P2_Renamed = 3;

	   /// <summary>
	   /// Index for addressing <code>LC</code> in this <code>CommandAPDU</code>   
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public const int LC_Renamed = 4;

	   /// <summary>
	   /// Byte array containing the entire <code>CommandAPDU</code>. </summary>
	   protected internal byte[] apduBuffer = null;

	   /// <summary>
	   /// Length of this <code>CommandAPDU</code> currently in the 
	   ///    <code>apduBuffer</code>. 
	   /// </summary>
	   protected internal int apduLength;

	   /// <summary>
	   /// Constructs a new ISO 7816-4 <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="buffer">  the entire <code>CommandAPDU</code> as a byte array </param>
	   public CommandAPDU(byte[] buffer)
	   {
		  apduLength = buffer.Length;
		  apduBuffer = new byte [apduLength];

		  Array.Copy(buffer, 0, apduBuffer, 0, apduLength);
	   } // CommandAPDU

	   /// <summary>
	   /// Constructs a new ISO 7816-4 CASE 1 <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="cla">  <code>CLA</code> byte </param>
	   /// <param name="ins">  <code>INS</code> byte </param>
	   /// <param name="p1">   parameter byte <code>P1</code> </param>
	   /// <param name="p2">   parameter byte <code>P2</code> </param>
	   public CommandAPDU(byte cla, byte ins, byte p1, byte p2) : this(cla, ins, p1, p2, null, -1)
	   {
	   } // CommandAPDU

	   /// <summary>
	   /// Constructs a new ISO 7816-4 CASE 2 <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="cla">  <code>CLA</code> byte </param>
	   /// <param name="ins">  <code>INS</code> byte </param>
	   /// <param name="p1">   parameter byte <code>P1</code> </param>
	   /// <param name="p2">   parameter byte <code>P2</code> </param>
	   /// <param name="le">   length of expected <code>ResponseAPDU</code>,
	   ///                 ranges from <code>-1</code> to 
	   ///                 <code>255</code>, where <code>-1</code> is no length
	   ///                 and <code>0</code> is the maximum length 
	   ///                 supported
	   /// </param>
	   /// <seealso cref=       ResponseAPDU </seealso>
	   public CommandAPDU(byte cla, byte ins, byte p1, byte p2, int le) : this(cla, ins, p1, p2, null, le)
	   {
	   } // CommandAPDU

	   /// <summary>
	   /// Constructs a new ISO 7816-4 CASE 3 <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="cla">  <code>CLA</code> byte </param>
	   /// <param name="ins">  <code>INS</code> byte </param>
	   /// <param name="p1">   parameter byte <code>P1</code> </param>
	   /// <param name="p2">   parameter byte <code>P2</code> </param>
	   /// <param name="data"> this <code>CommandAPDU</code> data as a byte array,
	   ///                 <code>LC</code> is derived from this data 
	   ///                 array length </param>
	   public CommandAPDU(byte cla, byte ins, byte p1, byte p2, byte[] data) : this(cla, ins, p1, p2, data, -1)
	   {
	   } // CommandAPDU

	   /// <summary>
	   /// Constructs a new ISO 7816-4 CASE 4 <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="cla">  <code>CLA</code> byte </param>
	   /// <param name="ins">  <code>INS</code> byte </param>
	   /// <param name="p1">   parameter byte <code>P1</code> </param>
	   /// <param name="p2">   parameter byte <code>P2</code> </param>
	   /// <param name="data"> <code>CommandAPDU</code> data as a byte array,
	   ///                 <code>LC</code> is derived from this data 
	   ///                 array length </param>
	   /// <param name="le">   length of expected <code>ResponseAPDU</code>,
	   ///                 ranges from <code>-1</code> to 
	   ///                 <code>255</code>, where <code>-1</code> is no length
	   ///                 and <code>0</code> is the maximum length 
	   ///                 supported
	   /// </param>
	   /// <seealso cref=       ResponseAPDU </seealso>
	   public CommandAPDU(byte cla, byte ins, byte p1, byte p2, byte[] data, int le)
	   {

		  // KLA ... changed 7-18-02.  We always need that
		  // length byte (LC) specified.  Otherwise if the IPR isn't
		  // cleared out, then we might try to read a garbage
		  // length and think we've gotten a screwy APDU on the
		  // button.

		  // all CommandAPDU has at least 5 bytes of header...
		  // that's CLA, INS, P1, P2, and LC 

		  apduLength = 5;

		  if (data != null)
		  {
			 apduLength += data.Length; // add data length
		  }

		  if (le >= 0)
		  {
			 apduLength++; // add one byte for LE
		  }

		  apduBuffer = new byte [apduLength];

		  // fill CommandAPDU buffer body
		  apduBuffer [CLA_Renamed] = cla;
		  apduBuffer [INS_Renamed] = ins;
		  apduBuffer [P1_Renamed] = p1;
		  apduBuffer [P2_Renamed] = p2;

		  if (data != null)
		  {
			 apduBuffer [LC_Renamed] = (byte) data.Length;

			 Array.Copy(data, 0, apduBuffer, LC_Renamed + 1, data.Length);
		  }
		  else
		  {
			 // fill in the LC byte anyhoo
			 apduBuffer[LC_Renamed] = (byte)0;
		  }

		  if (le >= 0)
		  {
			 apduBuffer [apduLength - 1] = (byte) le;
		  }
	   } // CommandAPDU

	   /// <summary>
	   /// Gets the <code>CLA</code> byte value.
	   /// </summary>
	   /// <returns> <code>CLA</code> byte of this <code>CommandAPDU</code> </returns>
	   public virtual byte CLA
	   {
		   get
		   {
			  return apduBuffer [CLA_Renamed];
		   }
	   } // getCLA

	   /// <summary>
	   /// Gets the <code>INS</code> byte value.
	   /// </summary>
	   /// <returns> <code>INS</code> byte of this <code>CommandAPDU</code> </returns>
	   public virtual byte INS
	   {
		   get
		   {
			  return apduBuffer [INS_Renamed];
		   }
	   } // getINS

	   /// <summary>
	   /// Gets the first parameter (<code>P1</code>) byte value.
	   /// </summary>
	   /// <returns> <code>P1</code> byte of this <code>CommandAPDU</code> </returns>
	   public virtual byte P1
	   {
		   get
		   {
			  return apduBuffer [P1_Renamed];
		   }
	   } //getP1

	   /// <summary>
	   /// Gets the second parameter (<code>P2</code>) byte value.
	   /// </summary>
	   /// <returns> <code>P2</code> byte of this <code>CommandAPDU</code> </returns>
	   public virtual byte P2
	   {
		   get
		   {
			  return apduBuffer [P2_Renamed];
		   }
	   } // getP2

	   /// <summary>
	   /// Gets the length of data field (<code>LC</code>).
	   /// </summary>
	   /// <returns> the number of bytes present in the data field of
	   /// this <code>CommandAPDU</code>, <code>0</code> 
	   /// indicates that there is no body </returns>
	   public virtual int LC
	   {
		   get
		   {
			  if (apduLength >= 6)
			  {
				 return apduBuffer [LC_Renamed];
			  }
			  else
			  {
				 return 0;
			  }
		   }
	   } // getLC

	   /// <summary>
	   /// Gets the expected length of <code>ResponseAPDU</code> (<code>LE</code>).
	   /// </summary>
	   /// <returns> the maximum number of bytes expected in the data field
	   /// of the <code>ResponseAPDU</code> to this <code>CommandAPDU</code>, 
	   /// <code>-1</code> indicates that no value is specified
	   /// </returns>
	   /// <seealso cref=       ResponseAPDU </seealso>
	   public virtual int LE
	   {
		   get
		   {
			  if ((apduLength == 5) || (apduLength == (6 + LC)))
			  {
				 return apduBuffer [apduLength - 1];
			  }
			  else
			  {
				 return -1;
			  }
		   }
	   } // getLE

	   /// <summary>
	   /// Gets this <code>CommandAPDU</code> <code>apduBuffer</code>.
	   /// This method allows user to manipulate the buffered <code>CommandAPDU</code>.
	   /// </summary>
	   /// <returns>  <code>apduBuffer</code> that holds the current <code>CommandAPDU</code>
	   /// </returns>
	   /// <seealso cref= #getBytes
	   ///  </seealso>
	   public byte[] Buffer
	   {
		   get
		   {
			  return apduBuffer;
		   }
	   } // getBuffer

	   /// <summary>
	   /// Gets the byte at the specified offset in the <code>apduBuffer</code>.
	   /// This method can only be used to access the <code>CommandAPDU</code>
	   /// currently stored.  It is not possible to read beyond the
	   /// end of the <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="index">   the offset in the <code>apduBuffer</code>
	   /// </param>
	   /// <returns>        the value at the given offset,
	   ///                or <code>-1</code> if the offset is invalid
	   /// </returns>
	   /// <seealso cref= #setByte </seealso>
	   /// <seealso cref= #getLength </seealso>
	   public byte getByte(int index)
	   {
		  if (index >= apduLength)
		  {
			 return 0xFF; // read beyond end of CommandAPDU
		  }
		  else
		  {
			 return (apduBuffer [index]);
		  }
	   } // getByte

	   /// <summary>
	   /// Gets a byte array of the buffered <code>CommandAPDU</code>.
	   /// The byte array returned gets allocated with the exact size of the
	   /// buffered <code>CommandAPDU</code>. To get direct access to the 
	   /// internal <code>apduBuffer</code>, use <code>getBuffer()</code>.
	   /// </summary>
	   /// <returns>  the buffered <code>CommandAPDU</code> copied into a new array
	   /// </returns>
	   /// <seealso cref= #getBuffer </seealso>
	   public byte[] Bytes
	   {
		   get
		   {
			  byte[] apdu = new byte [apduLength];
    
			  Array.Copy(apduBuffer, 0, apdu, 0, apduLength);
    
			  return apdu;
		   }
	   } // getBytes

	   /// <summary>
	   /// Gets the length of the buffered <code>CommandAPDU</code>.
	   /// </summary>
	   /// <returns>  the length of the <code>CommandAPDU</code> currently stored </returns>
	   public int Length
	   {
		   get
		   {
			  return apduLength;
		   }
	   } // getLength

	   /// <summary>
	   /// Sets the byte value at the specified offset in the 
	   /// <code>apduBuffer</code>.
	   /// This method can only be used to modify a <code>CommandAPDU</code>
	   /// already stored. It is not possible to set bytes beyond
	   /// the end of the current <code>CommandAPDU</code>.
	   /// </summary>
	   /// <param name="index">   the offset in the <code>apduBuffer</code> </param>
	   /// <param name="value">   the new byte value to store
	   /// </param>
	   /// <seealso cref= #getByte </seealso>
	   /// <seealso cref= #getLength
	   ///  </seealso>
	   public void setByte(int index, byte value)
	   {
		  if (index < apduLength)
		  {
			 apduBuffer [index] = value;
		  }
	   } // setByte

	   /// <summary>
	   /// Gets a string representation of this <code>CommandAPDU</code>.
	   /// </summary>
	   /// <returns> a string describing this <code>CommandAPDU</code> </returns>
	   public override string ToString()
	   {
		  string apduString = "";

		  apduString += "CLA = " + (apduBuffer [CLA_Renamed] & 0xFF).ToString("x");
		  apduString += " INS = " + (apduBuffer [INS_Renamed] & 0xFF).ToString("x");
		  apduString += " P1 = " + (apduBuffer [P1_Renamed] & 0xFF).ToString("x");
		  apduString += " P2 = " + (apduBuffer [P2_Renamed] & 0xFF).ToString("x");
		  apduString += " LC = " + (LC & 0xFF).ToString("x");

		  if (LE == -1)
		  {
			 apduString += " LE = " + LE;
		  }
		  else
		  {
			 apduString += " LE = " + (LE & 0xFF).ToString("x");
		  }

		  if (apduLength > 5)
		  {
			 apduString += "\nDATA = ";

			 for (int i = 5; i < LC + 5; i++)
			 {
				if ((apduBuffer [i] & 0xFF) < 0x10)
				{
				   apduString += '0';
				}

				apduString += ((int)(apduBuffer [i] & 0xFF)).ToString("x") + " ";
			 }
		  }

		  // make hex String representation of byte array
		  return (apduString.ToUpper());
	   } // toString
	}

}