using System;
using System.Diagnostics;

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
	/// Memory bank class for the EEPROM section of iButtons and 1-Wire devices on the DS2408.
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	internal class MemoryBankEEPROMblock : OTPMemoryBank
	{
	   /// <summary>
	   /// Memory functions.
	   /// </summary>
	   private static readonly byte WRITE_DATA_COMMAND = (byte) 0x6C;
	   private static readonly byte READ_DATA_COMMAND = (byte) 0x69;
	   private static readonly byte COPY_DATA_COMMAND = (byte) 0x48;
	   private static readonly byte RECALL_DATA_COMMAND = unchecked((byte) 0xB8);

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Reference to the OneWireContainer this bank resides on.
	   /// </summary>
	   protected internal OneWireContainer30 ib;

	   /// <summary>
	   /// block of 0xFF's used for faster read pre-fill of 1-Wire blocks
	   /// </summary>
	   protected internal byte[] ffBlock = new byte [32];

	   /// <summary>
	   /// Flag to indicate that speed needs to be set
	   /// </summary>
	   protected internal bool doSetSpeed;

	   //--------
	   //-------- Protected Variables for MemoryBank implementation
	   //--------

	   /// <summary>
	   /// Size of memory bank in bytes
	   /// </summary>
	   protected internal int size;


	   /// <summary>
	   /// Starting physical address in memory bank.  Needed for different
	   /// types of memory in the same logical memory bank.  This can be
	   /// used to seperate them into two virtual memory banks.  Example:
	   /// DS2406 status page has mixed EPROM and Volatile RAM.
	   /// </summary>
	   protected internal int startPhysicalAddress;

	   /// <summary>
	   /// Flag if read back verification is enabled in 'write()'.
	   /// </summary>
	   protected internal bool writeVerification;

	   //--------
	   //-------- Protected Variables for PagedMemoryBank implementation
	   //--------

	   /// <summary>
	   /// Number of pages in memory bank
	   /// </summary>
	   protected internal int numberPages;

	   /// <summary>
	   ///  page length in memory bank
	   /// </summary>
	   protected internal int pageLength;

	   /// <summary>
	   /// Max data length in page packet in memory bank
	   /// </summary>
	   protected internal int maxPacketDataLength;

	   //--------
	   //-------- Protected Variables for OTPMemoryBank implementation
	   //--------

	   /// <summary>
	   /// Flag if memory bank page 1 is locked.
	   /// </summary>
	   protected internal bool lockPage0;

	   /// <summary>
	   /// Flag if memory bank page 2 is locked.
	   /// </summary>
	   protected internal bool lockPage1;

	   //--------
	   //-------- Constructor
	   //--------

	   /// <summary>
	   /// Memory bank contstuctor.  Requires reference to the OneWireContainer
	   /// this memory bank resides on.  Requires reference to memory banks used
	   /// in OTP operations.
	   /// </summary>
	   public MemoryBankEEPROMblock(OneWireContainer30 ibutton)
	   {

		  // keep reference to ibutton where memory bank is
		  ib = ibutton;

		  // initialize attributes of this memory bank - DEFAULT: Main memory DS1985 w/o lock stuff
		  numberPages = 2;
		  size = 32;
		  pageLength = 16;
		  maxPacketDataLength = 13;

		  try
		  {
			 lockPage0 = ib.getFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_0_LOCK_FLAG);
			 lockPage1 = ib.getFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_1_LOCK_FLAG);
		  }
		  catch (System.Exception ioe)
		  {
			 Debug.WriteLine(ioe.ToString());
			 Debug.Write(ioe.StackTrace);
		  }

		  writeVerification = false;
		  startPhysicalAddress = 32;
		  doSetSpeed = true;

		  // create the ffblock (used for faster 0xFF fills)
		  for (int i = 0; i < 32; i++)
		  {
			 ffBlock [i] = unchecked((byte) 0xFF);
		  }
	   }

	   //--------
	   //-------- MemoryBank query methods
	   //--------

	   /// <summary>
	   /// Query to see get a string description of the current memory bank.
	   /// </summary>
	   /// <returns>  String containing the memory bank description </returns>
	   public virtual string BankDescription
	   {
		   get
		   {
			  return "EEPROM memory for DS2760";
		   }
	   }

	   /// <summary>
	   /// Query to see if the current memory bank is general purpose
	   /// user memory.  If it is NOT then it is Memory-Mapped and writing
	   /// values to this memory will affect the behavior of the 1-Wire
	   /// device.
	   /// </summary>
	   /// <returns>  'true' if current memory bank is general purpose </returns>
	   public virtual bool GeneralPurposeMemory
	   {
		   get
		   {
			  return true;
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank is read/write.
	   /// </summary>
	   /// <returns>  'true' if current memory bank is read/write </returns>
	   public virtual bool ReadWrite
	   {
		   get
		   {
			  if (lockPage0 && lockPage1)
			  {
				 return false;
			  }
			  else
			  {
				 return true;
			  }
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank is write write once such
	   /// as with EPROM technology.
	   /// </summary>
	   /// <returns>  'true' if current memory bank can only be written once </returns>
	   public virtual bool WriteOnce
	   {
		   get
		   {
			  return false;
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank is read only.
	   /// </summary>
	   /// <returns>  'true' if current memory bank can only be read </returns>
	   public virtual bool ReadOnly
	   {
		   get
		   {
			  if (lockPage0 && lockPage1)
			  {
				 return true;
			  }
			  else
			  {
				 return false;
			  }
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank non-volatile.  Memory is
	   /// non-volatile if it retains its contents even when removed from
	   /// the 1-Wire network.
	   /// </summary>
	   /// <returns>  'true' if current memory bank non volatile. </returns>
	   public virtual bool NonVolatile
	   {
		   get
		   {
			  return true;
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages need the adapter to
	   /// have a 'ProgramPulse' in order to write to the memory.
	   /// </summary>
	   /// <returns>  'true' if writing to the current memory bank pages
	   ///                 requires a 'ProgramPulse'. </returns>
	   public virtual bool needsProgramPulse()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages need the adapter to
	   /// have a 'PowerDelivery' feature in order to write to the memory.
	   /// </summary>
	   /// <returns>  'true' if writing to the current memory bank pages
	   ///                 requires 'PowerDelivery'. </returns>
	   public virtual bool needsPowerDelivery()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Query to get the starting physical address of this bank.  Physical
	   /// banks are sometimes sub-divided into logical banks due to changes
	   /// in attributes.
	   /// </summary>
	   /// <returns>  physical starting address of this logical bank. </returns>
	   public virtual int StartPhysicalAddress
	   {
		   get
		   {
			  return startPhysicalAddress;
		   }
	   }

	   /// <summary>
	   /// Query to get the memory bank size in bytes.
	   /// </summary>
	   /// <returns>  memory bank size in bytes. </returns>
	   public virtual int Size
	   {
		   get
		   {
			  return size;
		   }
	   }

	   //--------
	   //-------- PagedMemoryBank query methods
	   //--------

	   /// <summary>
	   /// Query to get the number of pages in current memory bank.
	   /// </summary>
	   /// <returns>  number of pages in current memory bank </returns>
	   public virtual int NumberPages
	   {
		   get
		   {
			  return numberPages;
		   }
	   }

	   /// <summary>
	   /// Query to get  page length in bytes in current memory bank.
	   /// </summary>
	   /// <returns>   page length in bytes in current memory bank </returns>
	   public virtual int PageLength
	   {
		   get
		   {
			  return pageLength;
		   }
	   }

	   /// <summary>
	   /// Query to get Maximum data page length in bytes for a packet
	   /// read or written in the current memory bank.  See the 'ReadPagePacket()'
	   /// and 'WritePagePacket()' methods.  This method is only usefull
	   /// if the current memory bank is general purpose memory.
	   /// </summary>
	   /// <returns>  max packet page length in bytes in current memory bank </returns>
	   public virtual int MaxPacketDataLength
	   {
		   get
		   {
			  return maxPacketDataLength;
		   }
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages can be read with
	   /// the contents being verified by a device generated CRC.
	   /// This is used to see if the 'ReadPageCRC()' can be used.
	   /// </summary>
	   /// <returns>  'true' if current memory bank can be read with self
	   ///          generated CRC. </returns>
	   public virtual bool hasPageAutoCRC()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages when read deliver
	   /// extra information outside of the normal data space.  Examples
	   /// of this may be a redirection byte, counter, tamper protection
	   /// bytes, or SHA-1 result.  If this method returns true then the
	   /// methods 'ReadPagePacket()' and 'readPageCRC()' with 'extraInfo'
	   /// parameter can be used.
	   /// </summary>
	   /// <returns>  'true' if reading the current memory bank pages
	   ///                 provides extra information.
	   /// </returns>
	   /// @deprecated  As of 1-Wire API 0.01, replaced by <seealso cref="#hasExtraInfo()"/> 
	   public virtual bool haveExtraInfo()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Checks to see if this memory bank's pages deliver extra
	   /// information outside of the normal data space,  when read.  Examples
	   /// of this may be a redirection byte, counter, tamper protection
	   /// bytes, or SHA-1 result.  If this method returns true then the
	   /// methods with an 'extraInfo' parameter can be used:
	   /// <seealso cref="#readPage(int,bool,byte[],int,byte[]) readPage"/>,
	   /// <seealso cref="#readPageCRC(int,bool,byte[],int,byte[]) readPageCRC"/>, and
	   /// <seealso cref="#readPagePacket(int,bool,byte[],int,byte[]) readPagePacket"/>.
	   /// </summary>
	   /// <returns>  <CODE> true </CODE> if reading the this memory bank's
	   ///                 pages provides extra information
	   /// </returns>
	   /// <seealso cref= #readPage(int,bool,byte[],int,byte[]) readPage(extra) </seealso>
	   /// <seealso cref= #readPageCRC(int,bool,byte[],int,byte[]) readPageCRC(extra) </seealso>
	   /// <seealso cref= #readPagePacket(int,bool,byte[],int,byte[]) readPagePacket(extra)
	   /// @since 1-Wire API 0.01 </seealso>
	   public virtual bool hasExtraInfo()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Query to get the length in bytes of extra information that
	   /// is read when read a page in the current memory bank.  See
	   /// 'hasExtraInfo()'.
	   /// </summary>
	   /// <returns>  number of bytes in Extra Information read when reading
	   ///          pages in the current memory bank. </returns>
	   public virtual int ExtraInfoLength
	   {
		   get
		   {
			  return 0;
		   }
	   }

	   /// <summary>
	   /// Query to get a string description of what is contained in
	   /// the Extra Informationed return when reading pages in the current
	   /// memory bank.  See 'hasExtraInfo()'.
	   /// </summary>
	   /// <returns> string describing extra information. </returns>
	   public virtual string ExtraInfoDescription
	   {
		   get
		   {
			  return null;
		   }
	   }

	   /// <summary>
	   /// Set the write verification for the 'write()' method.
	   /// </summary>
	   /// <param name="doReadVerf">   true (default) verify write in 'write'
	   ///                      false, don't verify write (used on
	   ///                      Write-Once bit manipulation) </param>
	   public virtual bool WriteVerification
	   {
		   set
		   {
			  writeVerification = value;
		   }
	   }

	   //--------
	   //-------- OTPMemoryBank query methods
	   //--------

	   /// <summary>
	   /// Query to see if current memory bank pages can be redirected
	   /// to another pages.  This is mostly used in Write-Once memory
	   /// to provide a means to update.
	   /// </summary>
	   /// <returns>  'true' if current memory bank pages can be redirected
	   ///          to a new page. </returns>
	   public virtual bool canRedirectPage()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages can be locked.  A
	   /// locked page would prevent any changes to the memory.
	   /// </summary>
	   /// <returns>  'true' if current memory bank pages can be redirected
	   ///          to a new page. </returns>
	   public virtual bool canLockPage()
	   {
		  return true;
	   }

	   /// <summary>
	   /// Query to see if current memory bank pages can be locked from
	   /// being redirected.  This would prevent a Write-Once memory from
	   /// being updated.
	   /// </summary>
	   /// <returns>  'true' if current memory bank pages can be locked from
	   ///          being redirected to a new page. </returns>
	   public virtual bool canLockRedirectPage()
	   {
		  return false;
	   }

	   //--------
	   //-------- MemoryBank I/O methods
	   //--------

	   /// <summary>
	   /// Read  memory in the current bank with no CRC checking (device or
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
	   public virtual void read(int startAddr, bool readContinue, byte[] readBuf, int offset, int len)
	   {
		  int i;
		  byte[] buffer = new byte[18];

		  if ((startAddr + len) > 32)
		  {
			 throw new OneWireException("Read exceeds memory bank end");
		  }

		  // calculate the address (32 and 48 are valid addresses)
		  byte memAddr;
		  if (startAddr < 16)
		  {
			 memAddr = (byte) startPhysicalAddress;
		  }
		  else
		  {
			 memAddr = (byte)(startPhysicalAddress + 16);
		  }

		  /* perform the recall/read and verification */
		  ib.doSpeed();
		  ib.adapter.reset();

		  if (ib.adapter.select(ib.address))
		  {

			 /* first recall the memory to shadow ram */
			 buffer [0] = RECALL_DATA_COMMAND;
			 buffer [1] = memAddr;

			 ib.adapter.dataBlock(buffer, 0, 2);

			 /* now read the shadow ram */
			 ib.adapter.reset();
			 ib.adapter.select(ib.address);

			 buffer [0] = READ_DATA_COMMAND;

			 // buffer[1] should still hold memAddr
			 Array.Copy(ffBlock,0,buffer,2,16);

			 ib.adapter.dataBlock(buffer, 0, 18);

			 //user can re-read for verification
			 if ((startAddr < 16) && (startAddr + len < 16))
			 {
				for (i = startAddr;i < (startAddr + len);i++)
				{
				   readBuf[offset + i - startAddr] = buffer[i + 2];
				}
			 }
			 else if (startAddr >= 16)
			 {
				for (i = startAddr;i < (startAddr + len);i++)
				{
				   readBuf[offset + i - startAddr] = buffer[i - startAddr + 2 + (startAddr - 16)];
				}
			 }
			 else
			 {
				for (i = startAddr;i < 16;i++)
				{
				   readBuf[offset + i - startAddr] = buffer[i + 2];
				}

				ib.adapter.reset();
				ib.adapter.select(ib.address);

				buffer[0] = RECALL_DATA_COMMAND;
				buffer[1] = (byte)(memAddr + 16);

				ib.adapter.dataBlock(buffer,0,2);

				/* now read the shadow ram */
				ib.adapter.reset();
				ib.adapter.select(ib.address);

				buffer[0] = READ_DATA_COMMAND;

				// buffer[1] should still hold memAddr
				Array.Copy(ffBlock,0,buffer,2,16);

				ib.adapter.dataBlock(buffer, 0, 18);

				//user can re-read for verification
				for (i = 16;i < (startAddr + len);i++)
				{
				   readBuf[i + offset - startAddr] = buffer[i - 14];
				}
			 }
		  }
		  else
		  {
			 throw new OneWireException("OneWireContainer30-Device not found.");
		  }
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
	   /// <param name="startAddr">     starting address </param>
	   /// <param name="writeBuf">      byte array containing data to write </param>
	   /// <param name="offset">        offset into writeBuf to get data </param>
	   /// <param name="len">           length in bytes to write
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void write(int startAddr, byte[] writeBuf, int offset, int len)
	   {
		  byte[] buffer = new byte [18];
		  byte[] memory = new byte [32];
		  int modify;
		  int i;

		  if (startAddr + len > 32)
		  {
			 throw new OneWireException("Write exceeds memory bank end");
		  }

		  // the first block is at address 32 and the second is at address 48
		  byte memAddr;
		  if (startAddr < 16)
		  {
			 memAddr = (byte) startPhysicalAddress;
		  }
		  else
		  {
			 memAddr = (byte)(startPhysicalAddress + 16);
		  }

		  // if the EEPROM block is locked throw a OneWireIOException
		  if ((lockPage0 && (memAddr == 32)) || (lockPage1 && (memAddr == 48)))
		  {
			 throw new OneWireIOException("OneWireContainer30-Cant write data to locked EEPROM block.");
		  }

		  // read memory that is already there
		  read(0, false, memory, 0, 32);

		  Array.Copy(writeBuf,offset,memory,startAddr,len);

		  /* perform the write/verification and copy */
		  ib.doSpeed();
		  ib.adapter.reset();

		  if (ib.adapter.select(ib.address))
		  {
			 /* first write to shadow rom */
			 buffer[0] = WRITE_DATA_COMMAND;
			 buffer[1] = memAddr;

			 if (memAddr == 32)
			 {
				Array.Copy(memory,0,buffer,2,16);
				modify = 0;
			 }
			 else
			 {
				Array.Copy(memory,16,buffer,2,16);
				modify = 16;
			 }

			 ib.adapter.dataBlock(buffer, 0, 18);

			 /* read the shadow ram back for verification */
			 ib.adapter.reset();
			 ib.adapter.select(ib.address);

			 buffer[0] = READ_DATA_COMMAND;

			 // buffer[1] should still hold memAddr
			 Array.Copy(ffBlock,0,buffer,2,16);

			 ib.adapter.dataBlock(buffer, 0, 18);

			 // verify data
			 for (i = 0; i < 16; i++)
			 {
				if (buffer [i + 2] != memory[i + modify])
				{
				   throw new OneWireIOException("Error writing EEPROM memory bank");
				}
			 }

			 /* now perform the copy to EEPROM */
			 ib.adapter.reset();
			 ib.adapter.select(ib.address);

			 buffer[0] = COPY_DATA_COMMAND;

			 // buffer[1] should still hold memAddr
			 ib.adapter.dataBlock(buffer, 0, 2);

			 if ((startAddr < 16) && ((startAddr + len) >= 16))
			 {
				memAddr = 48;
				ib.adapter.reset();

				if (ib.adapter.select(ib.address))
				{
				   /* first write to shadow rom */
				   buffer[0] = WRITE_DATA_COMMAND;
				   buffer[1] = memAddr;

				   Array.Copy(memory,16,buffer,2,16);

				   ib.adapter.dataBlock(buffer,0,18);

				   /* read the shadow ram back for verification */
				   ib.adapter.reset();
				   ib.adapter.select(ib.address);

				   buffer[0] = READ_DATA_COMMAND;

				   // buffer[1] should still hold memAddr
				   Array.Copy(ffBlock,0,buffer,2,16);

				   ib.adapter.dataBlock(buffer,0,18);

				   // verify data
				   for (i = 0; i < 16; i++)
				   {
					  if (buffer[i + 2] != memory[i + 16])
					  {
						 throw new OneWireIOException("Error writing EEPROM memory bank");
					  }
				   }

				   /* now perfomr the copy to EEPROM */
				   ib.adapter.reset();
				   ib.adapter.select(ib.address);

				   buffer[0] = COPY_DATA_COMMAND;

				   ib.adapter.dataBlock(buffer, 0, 2);
				}
			 }
		  }
		  else
		  {
			 throw new OneWireException("OneWireContainer30-Device not found.");
		  }
	   }

	   //--------
	   //-------- PagedMemoryBank I/O methods
	   //--------

	   /// <summary>
	   /// Read  page in the current bank with no
	   /// CRC checking (device or data). The resulting data from this API
	   /// may or may not be what is on the 1-Wire device.  It is recommends
	   /// that the data contain some kind of checking (CRC) like in the
	   /// readPagePacket() method or have the 1-Wire device provide the
	   /// CRC as in readPageCRC().  readPageCRC() however is not
	   /// supported on all memory types, see 'hasPageAutoCRC()'.
	   /// If neither is an option then this method could be called more
	   /// then once to at least verify that the same thing is read consistantly.
	   /// </summary>
	   /// <param name="page">          page number to read packet from </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       readPage() continious where the last one
	   ///                       led off and it is inside a
	   ///                       'beginExclusive/endExclusive' block. </param>
	   /// <param name="readBuf">       byte array to place read data into </param>
	   /// <param name="offset">        offset into readBuf to place data
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void readPage(int page, bool readContinue, byte[] readBuf, int offset)
	   {
		  read(page * pageLength, readContinue, readBuf, offset, pageLength);
	   }

	   /// <summary>
	   /// Read  page with extra information in the current bank with no
	   /// CRC checking (device or data). The resulting data from this API
	   /// may or may not be what is on the 1-Wire device.  It is recommends
	   /// that the data contain some kind of checking (CRC) like in the
	   /// readPagePacket() method or have the 1-Wire device provide the
	   /// CRC as in readPageCRC().  readPageCRC() however is not
	   /// supported on all memory types, see 'hasPageAutoCRC()'.
	   /// If neither is an option then this method could be called more
	   /// then once to at least verify that the same thing is read consistantly.
	   /// See the method 'hasExtraInfo()' for a description of the optional
	   /// extra information some devices have.
	   /// </summary>
	   /// <param name="page">          page number to read packet from </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       readPage() continious where the last one
	   ///                       led off and it is inside a
	   ///                       'beginExclusive/endExclusive' block. </param>
	   /// <param name="readBuf">       byte array to place read data into </param>
	   /// <param name="offset">        offset into readBuf to place data </param>
	   /// <param name="extraInfo">     byte array to put extra info read into
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void readPage(int page, bool readContinue, byte[] readBuf, int offset, byte[] extraInfo)
	   {
		  throw new OneWireException("Read extra information not supported on this memory bank");
	   }

	   /// <summary>
	   /// Read a Universal Data Packet and extra information.  See the
	   /// method 'readPagePacket()' for a description of the packet structure.
	   /// See the method 'hasExtraInfo()' for a description of the optional
	   /// extra information some devices have.
	   /// </summary>
	   /// <param name="page">          page number to read packet from </param>
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
	   /// <returns>  number of data bytes written to readBuf at the offset.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual int readPagePacket(int page, bool readContinue, byte[] readBuf, int offset, byte[] extraInfo)
	   {
		  throw new OneWireException("Read extra information not supported on this memory bank");
	   }

	   /// <summary>
	   /// Read a Universal Data Packet and extra information.  See the
	   /// method 'readPagePacket()' for a description of the packet structure.
	   /// See the method 'hasExtraInfo()' for a description of the optional
	   /// extra information some devices have.
	   /// </summary>
	   /// <param name="page">          page number to read packet from </param>
	   /// <param name="readContinue">  if 'true' then device read is continued without
	   ///                       re-selecting.  This can only be used if the new
	   ///                       readPagePacket() continious where the last one
	   ///                       stopped and it is inside a
	   ///                       'beginExclusive/endExclusive' block. </param>
	   /// <param name="readBuf">       byte array to put data read. Must have at least
	   ///                       'getMaxPacketDataLength()' elements. </param>
	   /// <param name="offset">        offset into readBuf to place data
	   /// </param>
	   /// <returns>  number of data bytes written to readBuf at the offset.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual int readPagePacket(int page, bool readContinue, byte[] readBuf, int offset)
	   {
		  byte[] raw_buf = new byte [pageLength];

		  // read entire page with read page CRC
		  read((page * pageLength), readContinue, raw_buf, 0, pageLength);

		  // check if length is realistic
		  if ((raw_buf [0] & 0x00FF) > maxPacketDataLength)
		  {
			 forceVerify();

			 throw new OneWireIOException("Invalid length in packet");
		  }

		  // verify the CRC is correct
		  if (CRC16.compute(raw_buf, 0, raw_buf [0] + 3, page) == 0x0000B001)
		  {

			 // extract the data out of the packet
			 Array.Copy(raw_buf, 1, readBuf, offset, raw_buf [0]);

			 // return the length
			 return raw_buf [0];
		  }
		  else
		  {
			 forceVerify();

			 throw new OneWireIOException("Invalid CRC16 in packet read");
		  }
	   }

	   /// <summary>
	   /// Write a Universal Data Packet.  See the method 'readPagePacket()'
	   /// for a description of the packet structure.
	   /// </summary>
	   /// <param name="page">          page number to write packet to </param>
	   /// <param name="writeBuf">      data byte array to write </param>
	   /// <param name="offset">        offset into writeBuf where data to write is </param>
	   /// <param name="len">           number of bytes to write
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void writePagePacket(int page, byte[] writeBuf, int offset, int len)
	   {

		  // make sure length does not exceed max
		  if (len > maxPacketDataLength)
		  {
			 throw new OneWireIOException("Length of packet requested exceeds page size");
		  }

		  // construct the packet to write
		  byte[] raw_buf = new byte [len + 3];

		  raw_buf [0] = (byte) len;

		  Array.Copy(writeBuf, offset, raw_buf, 1, len);

		  int crc = CRC16.compute(raw_buf, 0, len + 1, page);

		  raw_buf [len + 1] = unchecked((byte)(~crc & 0xFF));
		  raw_buf [len + 2] = unchecked((byte)(((int)((uint)(~crc & 0xFFFF) >> 8)) & 0xFF));

		  // write the packet, return result
		  write(page * pageLength, raw_buf, 0, len + 3);
	   }

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
	   public virtual void readPageCRC(int page, bool readContinue, byte[] readBuf, int offset)
	   {
		  throw new OneWireException("Read page with CRC not supported in this memory bank");
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
	   public virtual void readPageCRC(int page, bool readContinue, byte[] readBuf, int offset, byte[] extraInfo)
	   {
		  throw new OneWireException("Read page with CRC not supported in this memory bank");
	   }

	   //--------
	   //-------- OTPMemoryBank I/O methods
	   //--------

	   /// <summary>
	   /// Lock the specifed page in the current memory bank.  Not supported
	   /// by all devices.  See the method 'canLockPage()'.
	   /// </summary>
	   /// <param name="page">   number of page to lock
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void lockPage(int page)
	   {
		  if (page > 1)
		  {
			 throw new OneWireException("Page does not exist to lock");
		  }

		  ib.setFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_LOCK_ENABLE_FLAG,true);

		  if (page == 0)
		  {
			 ib.setFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_0_LOCK_FLAG,true);
		  }
		  else if (page == 1)
		  {
			 ib.setFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_1_LOCK_FLAG,true);
		  }

		  // read back to verify
		  if (!isPageLocked(page))
		  {
			 forceVerify();

			 throw new OneWireIOException("Read back from write incorrect, could not lock page");
		  }
		  else
		  {
			 if (page == 0)
			 {
				lockPage0 = true;
			 }
			 else if (page == 1)
			 {
				lockPage1 = true;
			 }
		  }
	   }

	   /// <summary>
	   /// Query to see if the specified page is locked.
	   /// See the method 'canLockPage()'.
	   /// </summary>
	   /// <param name="page">  number of page to see if locked
	   /// </param>
	   /// <returns>  'true' if page locked.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool isPageLocked(int page)
	   {
		  bool flag = false;

		  if (page > 1)
		  {
			 throw new OneWireException("Page does not exist to be locked");
		  }

		  if (page == 0)
		  {
			 flag = ib.getFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_0_LOCK_FLAG);
		  }
		  else if (page == 1)
		  {
			 flag = ib.getFlag(OneWireContainer30.EEPROM_REGISTER, OneWireContainer30.EEPROM_BLOCK_1_LOCK_FLAG);
		  }

		  return flag;

	   }

	   /// <summary>
	   /// Redirect the specifed page in the current memory bank to a new page.
	   /// Not supported by all devices.  See the method 'canRedirectPage()'.
	   /// </summary>
	   /// <param name="page">      number of page to redirect </param>
	   /// <param name="newPage">   new page number to redirect to
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void redirectPage(int page, int newPage)
	   {
		  throw new OneWireException("This memory bank does not support redirection.");
	   }

	   /// <summary>
	   /// Query to see if the specified page is redirected.
	   /// Not supported by all devices.  See the method 'canRedirectPage()'.
	   /// </summary>
	   /// <param name="page">      number of page check for redirection
	   /// </param>
	   /// <returns>  return the new page number or 0 if not redirected
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException">
	   /// </exception>
	   /// @deprecated  As of 1-Wire API 0.01, replaced by <seealso cref="#getRedirectedPage(int)"/> 
	   public virtual int isPageRedirected(int page)
	   {
		  throw new OneWireException("This memory bank does not support redirection.");
	   }

	   /// <summary>
	   /// Gets the page redirection of the specified page.
	   /// Not supported by all devices.
	   /// </summary>
	   /// <param name="page">  page to check for redirection
	   /// </param>
	   /// <returns>  the new page number or 0 if not redirected
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         no device present or a CRC read from the device is incorrect.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter.
	   /// </exception>
	   /// <seealso cref= #canRedirectPage() canRedirectPage </seealso>
	   /// <seealso cref= #redirectPage(int,int) redirectPage
	   /// @since 1-Wire API 0.01 </seealso>
	   public virtual int getRedirectedPage(int page)
	   {
		  throw new OneWireException("This memory bank does not support redirection.");
	   }

	   /// <summary>
	   /// Lock the redirection option for the specifed page in the current
	   /// memory bank. Not supported by all devices.  See the method
	   /// 'canLockRedirectPage()'.
	   /// </summary>
	   /// <param name="page">      number of page to redirect
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void lockRedirectPage(int page)
	   {
		  throw new OneWireException("This memory bank does not support redirection.");
	   }

	   /// <summary>
	   /// Query to see if the specified page has redirection locked.
	   /// Not supported by all devices.  See the method 'canRedirectPage()'.
	   /// </summary>
	   /// <param name="page">      number of page check for locked redirection
	   /// </param>
	   /// <returns>  return 'true' if redirection is locked for this page
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool isRedirectPageLocked(int page)
	   {
		  throw new OneWireException("This memory bank does not support redirection.");
	   }


	   //--------
	   //-------- checkSpeed methods
	   //--------

	   /// <summary>
	   /// Check the device speed if has not been done before or if
	   /// an error was detected.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void checkSpeed()
	   {
		  lock (this)
		  {

			 // only check the speed
			 if (doSetSpeed)
			 {

				// attempt to set the correct speed and verify device present
				ib.doSpeed();

				// no execptions so clear flag
				doSetSpeed = false;
			 }
		  }
	   }

	   /// <summary>
	   /// Set the flag to indicate the next 'checkSpeed()' will force
	   /// a speed set and verify 'doSpeed()'.
	   /// </summary>
	   public virtual void forceVerify()
	   {
		  lock (this)
		  {
			 doSetSpeed = true;
		  }
	   }

	}


}