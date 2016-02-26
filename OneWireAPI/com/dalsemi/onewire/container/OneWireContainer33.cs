﻿using System;
using System.Collections;

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

namespace com.dalsemi.onewire.container
{

	// imports
	using com.dalsemi.onewire.adapter;


	/// <summary>
	/// <P>1-Wire&#174 container for the '1K-Bit protected 1-Wire EEPROM with SHA-1
	///  Engine' family type <B>33</B> (hex), Dallas Semiconductor part number:
	/// <B>DS1961S,DS2432</B>.
	/// 
	/// <H3> Features </H3>
	/// <UL>
	///   <LI> 1128 bits of 5V EEPROM memory partitioned into four pages of 256 bits,
	///        a 64-bit write-only secret and up to 5 general purpose read/write
	///        registers.
	///   <LI> On-chip 512-bit SHA-1 engine to compute 160-bit Message Authentication
	///        Codes (MAC) and to generate secrets.
	///   <LI> Write access requires knowledge of the secret and the capability of
	///        computing and transmitting a 160-bit MAC as authorization.
	///   <LI> Secret and data memory can be write-protected (all or page 0 only) or
	///        put in EPROM-emulation mode ("write to 0", page0)
	///   <LI> unique, fatory-lasered and tested 64-bit registration number (8-bit
	///        family code + 48-bit serial number + 8-bit CRC tester) assures
	///        absolute traceablity because no two parts are alike.
	///   <LI> Built-in multidrop controller ensures compatibility with other 1-Wire
	///        net products.
	///   <LI> Reduces control, address, data and power to a single data pin.
	///   <LI> Directly connects to a single port pin of a microprocessor and
	///        communicates at up to 16.3k bits per second.
	///   <LI> Overdrive mode boosts communication speed to 142k bits per second.
	///   <LI> 8-bit family code specifies DS2432 communication requirements to reader.
	///   <LI> Presence detector acknowledges when reader first applies voltage.
	///   <LI> Low cost 6-lead TSOC surface mount package, or solder-bumped chip scale
	///        package.
	///   <LI> Reads and writes over a wide voltage range of 2.8V to 5.25V from -40C
	///        to +85C.
	/// </UL>
	/// 
	/// <P> The memory can also be accessed through the objects that are returned
	/// from the <seealso cref="#getMemoryBanks() getMemoryBanks"/> method. </P>
	/// 
	/// The following is a list of the MemoryBank instances that are returned:
	/// 
	/// <UL>
	///   <LI> <B> Page Zero with write protection</B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 32 starting at physical address 0
	///         <LI> <I> Features</I> Read/Write general-purpose non-volatile
	///         <LI> <I> Page</I> 1 page of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC and write protection.
	///      </UL>
	///   <LI> <B> Page One with EPROM mode and write protection </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 32 starting at physical address 32
	///         <LI> <I> Features</I> Read/Write general-purpose non-volatile
	///         <LI> <I> Page</I> 1 page of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC, EPROM mode and write protection.
	///      </UL>
	///   <LI> <B> Page Two and Three with write protection </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 64 starting at physical address 64
	///         <LI> <I> Features</I> Read/Write general-purpose non-volatile
	///         <LI> <I> Pages</I> 2 pages of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC and write protection.
	///      </UL>
	///   <LI> <B> Status Page that contains the secret and the status. </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 24 starting at physical address 128
	///         <LI> <I> Page Features </I> Contains secret and status for the iButton.
	///      </UL>
	/// </UL>
	/// 
	/// <DD> <H4> Example 1</H4>
	/// Display some features of isMACValid where owd is an instanceof OneWireContainer33 and
	/// bank is an instanceof PagedMemoryBank:
	/// <PRE> <CODE>
	///  byte[] read_buf  = new byte [bank.getPageLength()];
	///  byte[] extra_buf = new byte [bank.getExtraInfoLength()];
	///  byte[] challenge = new byte [8];
	/// 
	///  // read a page (use the most verbose and secure method)
	///  if (bank.hasPageAutoCRC())
	///  {
	///     System.out.println("Using device generated CRC");
	/// 
	///     if (bank.hasExtraInfo())
	///     {
	///        bank.readPageCRC(pg, false, read_buf, 0, extra_buf);
	/// 
	///        owd.getChallenge(challenge,0);
	///        owd.getContainerSecret(secret, 0);
	///        sernum = owd.getAddress();
	///        macvalid = owd.isMACValid(bank.getStartPhysicalAddress()+pg*bank.getPageLength(),
	///                                  sernum,read_buf,extra_buf,challenge,secret);
	///     }
	///     else
	///        bank.readPageCRC(pg, false, read_buf, 0);
	///  }
	///  else
	///  {
	///     if (bank.hasExtraInfo())
	///        bank.readPage(pg, false, read_buf, 0, extra_buf);
	///     else
	///        bank.readPage(pg, false, read_buf, 0);
	///  }
	/// </CODE> </PRE>
	/// 
	/// <H3> DataSheet </H3>
	/// <DL>
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2432.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2432.pdf</A>
	/// </DL>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.application.sha.SHAiButtonUser33
	/// @version 	0.00, 19 Dec 2000
	/// @author JPE </seealso>
	public class OneWireContainer33 : OneWireContainer
	{
	   //turns on extra debugging output in all 1-wire containers
	   private const bool DEBUG = true;

	   //--------
	   //-------- Static Final Variables
	   //--------

	   /// <summary>
	   /// Private Secret </summary>
	   private sbyte[] secret = new sbyte [8];

	   /// <summary>
	   /// Challenge to use for the Read Authenticate Methods </summary>
	   private sbyte[] challenge = new sbyte [3];

	   /// <summary>
	   /// The different memory banks for the container. </summary>
	   private MemoryBankScratchSHAEE mbScratchpad;
	   private MemoryBankSHAEE memstatus;
	   private readonly MemoryBankSHAEE[] memoryPages = new MemoryBankSHAEE[4];

	   /// <summary>
	   /// Buffer used to hold MAC for certain calls </summary>
	   private sbyte[] MAC_buffer = new sbyte[20];

	   /// <summary>
	   /// Flag to indicate if the secret has been set. </summary>
	   protected internal bool secretSet;

	   /// <summary>
	   /// Flag to indicate if the secret is write protected. </summary>
	   protected internal bool secretProtected;

	   /// <summary>
	   /// Flag to indicate if the adapter has been specified. </summary>
	   protected internal bool setAdapter;

	   /// <summary>
	   /// Flag to indicate if the status has been checked. </summary>
	   protected internal bool container_check;

	   /// <summary>
	   /// block of 0xFF's used for faster read pre-fill of 1-Wire blocks </summary>
	   protected internal static readonly sbyte[] ffBlock = new sbyte[] {unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF), unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF),unchecked((sbyte)0xFF)};

	   /// <summary>
	   /// block of 0xFF's used for faster erase of blocks </summary>
	   protected internal static readonly sbyte[] zeroBlock = new sbyte[] {0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00};

	   /// <summary>
	   /// This byte is used to set flags in the status register </summary>
	   private static readonly sbyte[] ACTIVATION_BYTE = new sbyte[] {unchecked((sbyte)0xAA)};

	   /// <summary>
	   /// Default Constructor OneWireContainer33.
	   /// Must call setupContainer before using.
	   /// </summary>
	   public OneWireContainer33() : base()
	   {

		  Array.Copy(ffBlock,0,secret,0,8);
		  Array.Copy(ffBlock,0,challenge,0,3);

		  setAdapter = false;
		  container_check = false;
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public OneWireContainer33(DSPortAdapter sourceAdapter, sbyte[] newAddress) : base(sourceAdapter, newAddress)
	   {

		  Array.Copy(ffBlock,0,secret,0,8);
		  Array.Copy(ffBlock,0,challenge,0,3);

		  setAdapter = true;
		  container_check = false;
		  initmem();
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public OneWireContainer33(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {

		  Array.Copy(ffBlock,0,secret,0,8);
		  Array.Copy(ffBlock,0,challenge,0,3);

		  setAdapter = true;
		  container_check = false;
		  initmem();
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public OneWireContainer33(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {

		  Array.Copy(ffBlock,0,secret,0,8);
		  Array.Copy(ffBlock,0,challenge,0,3);

		  setAdapter = true;
		  container_check = false;
		  initmem();
	   }

	   //--------
	   //-------- Methods
	   //--------

	   /// <summary>
	   /// Tells whether an adapter has been set.
	   /// </summary>
	   /// <returns> bool telling weather an adapter has been set. </returns>
	   protected internal virtual bool adapterSet()
	   {
		  return setAdapter;
	   }

	   /// <summary>
	   /// Provide this container the adapter object used to access this device
	   /// and provide the address of this iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public override void setupContainer(DSPortAdapter sourceAdapter, sbyte[] newAddress)
	   {
		  base.setupContainer(sourceAdapter,newAddress);

		  if (!setAdapter)
		  {
			 initmem();
		  }

		  setAdapter = true;
	   }

	   /// <summary>
	   /// Provide this container the adapter object used to access this device
	   /// and provide the address of this iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public override void setupContainer(DSPortAdapter sourceAdapter, long newAddress)
	   {
		  base.setupContainer(sourceAdapter,newAddress);

		  if (!setAdapter)
		  {
			 initmem();
		  }

		  setAdapter = true;
	   }

	   /// <summary>
	   /// Provide this container the adapter object used to access this device
	   /// and provide the address of this iButton or 1-Wire device.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   public override void setupContainer(DSPortAdapter sourceAdapter, string newAddress)
	   {
		  base.setupContainer(sourceAdapter,newAddress);

		  if (!setAdapter)
		  {
			 initmem();
		  }

		  setAdapter = true;
	   }

	   /// <summary>
	   /// Retrieve the Dallas Semiconductor part number of the iButton
	   /// as a string.  For example 'DS1992'.
	   /// </summary>
	   /// <returns> string represetation of the iButton name. </returns>
	   public override string Name
	   {
		   get
		   {
			  return "DS1961S";
		   }
	   }

	   /// <summary>
	   /// Retrieve the alternate Dallas Semiconductor part numbers or names.
	   /// A 'family' of MicroLAN devices may have more than one part number
	   /// depending on packaging.
	   /// </summary>
	   /// <returns>  the alternate names for this iButton or 1-Wire device </returns>
	   public override string AlternateNames
	   {
		   get
		   {
			  return "DS2432";
		   }
	   }

	   /// <summary>
	   /// Retrieve a short description of the function of the iButton type.
	   /// </summary>
	   /// <returns> string represetation of the function description. </returns>
	   public override string Description
	   {
		   get
		   {
			  return "1K-Bit protected 1-Wire EEPROM with SHA-1 Engine.";
		   }
	   }

	   /// <summary>
	   /// Returns the maximum speed this iButton can communicate at.
	   /// </summary>
	   /// <returns>  max. communication speed. </returns>
	   public override int MaxSpeed
	   {
		   get
		   {
			  return DSPortAdapter.SPEED_OVERDRIVE;
		   }
	   }

	   /// <summary>
	   /// Get an enumeration of memory bank instances that implement one or more
	   /// of the following interfaces:
	   /// <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	   /// <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>,
	   /// and <seealso cref="com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank"/>. </summary>
	   /// <returns> <CODE>Enumeration</CODE> of memory banks </returns>
	   public override System.Collections.IEnumerator MemoryBanks
	   {
		   get
		   {
			  ArrayList bank_vector = new ArrayList(4);
    
			  bank_vector.Add(mbScratchpad);
			  bank_vector.Add(memoryPages[0]);
			  bank_vector.Add(memoryPages[1]);
			  bank_vector.Add(memoryPages[2]);
			  bank_vector.Add(memstatus);
    
			  return bank_vector.GetEnumerator();
		   }
	   }

	   /// <summary>
	   /// Returns the instance of the Scratchpad memory bank.  Contains
	   /// methods for reading/writing the Scratchpad contents.  Also,
	   /// methods for Load First Secret, Compute Next Secret, and
	   /// Refresh Scratchpad
	   /// </summary>
	   /// <returns> the instance of the Scratchpad memory bank </returns>
	   internal virtual MemoryBankScratchSHAEE ScratchpadMemoryBank
	   {
		   get
		   {
			  return mbScratchpad;
		   }
	   }

	   /// <summary>
	   /// Returns the instance of the Status page memory bank.
	   /// </summary>
	   /// <returns> the instance of the Status page memory bank </returns>
	   public virtual MemoryBankSHAEE StatusPageMemoryBank
	   {
		   get
		   {
			  return memstatus;
		   }
	   }

	   /// <summary>
	   /// Returns the instance of the memory bank for a particular page
	   /// </summary>
	   /// <param name="page"> the page for the requested memory bank;
	   /// </param>
	   /// <returns> the instance of the memory bank for the specified page </returns>
	   public virtual MemoryBankSHAEE getMemoryBankForPage(int page)
	   {
		  if (page == 3)
		  {
			 page = 2;
		  }
		  return memoryPages[page];
	   }

	   /// <summary>
	   /// Sets the bus master secret for this DS2432.
	   /// </summary>
	   /// <param name="newSecret"> Secret for this DS2432. </param>
	   /// <param name="offset"> index into array to copy the secret from </param>
	   public virtual void setContainerSecret(sbyte[] newSecret, int offset)
	   {
		  Array.Copy(newSecret,offset,secret,0,8);
		  secretSet = true;
	   }
	   /// <summary>
	   /// Get the secret of this device as an array of bytes.
	   /// </summary>
	   /// <param name="secretBuf"> array of bytes for holding the container secret </param>
	   /// <param name="offset"> index into array to copy the secret to </param>
	   public virtual void getContainerSecret(sbyte[] secretBuf, int offset)
	   {
		  Array.Copy(secret,0,secretBuf,offset,8);
	   }
	   /// <summary>
	   /// Get the current status of the secret.
	   /// </summary>
	   /// <returns>  bool telling if the secret is set
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool ContainerSecretSet
	   {
		   get
		   {
			  //if(!container_check)
			  //   container_check = this.checkStatus();
    
			  return (secretSet);
		   }
	   }
	   /// <summary>
	   /// Get the status of the secret, if it is write protected.
	   /// </summary>
	   /// <returns>  bool telling if the secret is write protected.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool SecretWriteProtected
	   {
		   get
		   {
			  if (!container_check)
			  {
				 container_check = this.checkStatus();
			  }
    
			  return secretProtected;
		   }
	   }

	   /// <summary>
	   /// Sets the challenge for the Read Authenticate Page
	   /// </summary>
	   /// <param name="challengeset">  Challenge for all the memory banks. </param>
	   public virtual void setChallenge(sbyte[] challengeset, int offset)
	   {
		  Array.Copy(challengeset,offset,challenge,0,3);
	   }
	   /// <summary>
	   /// Get the challenge of this device as an array of bytes.
	   /// </summary>
	   /// <param name="get">  array of bytes containing the iButton challenge </param>
	   public virtual void getChallenge(sbyte[] get, int offset)
	   {
		  Array.Copy(challenge,0,get,offset,3);
	   }

	   /// <summary>
	   /// Get the status of all the pages, if they are write protected.
	   /// </summary>
	   /// <returns>  bool telling if all the pages are write protected.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool WriteProtectAllSet
	   {
		   get
		   {
			  if (!container_check)
			  {
				 container_check = this.checkStatus();
			  }
    
			  return memoryPages[2].readOnly;
		   }
	   }

	   /// <summary>
	   ///  Write protects the secret for the DS2432.
	   /// </summary>
	   public virtual void writeProtectSecret()
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  // write protect secret
		  //  - write status byte to address
		  //    ((8h) + physical) = 88h
		  memstatus.write(8,ACTIVATION_BYTE,0,1);

		  secretProtected = true;
	   }

	   /// <summary>
	   /// Write protect pages 0 to 3
	   /// </summary>
	   public virtual void writeProtectAll()
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  // write protect all pages
		  //  - write status byte to address
		  //    ((9h) + physical) = 89h
		  memstatus.write(9,ACTIVATION_BYTE,0,1);

		  memoryPages[0].writeprotect();
		  memoryPages[1].writeprotect();
		  memoryPages[2].writeprotect();
	   }

	   /// <summary>
	   /// Sets the EPROM mode for page 1.  After setting, Page One can only be written to once.
	   /// </summary>
	   public virtual void setEPROMModePageOne()
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  // Enable EPROM mode for page 1.
		  //  - write status byte to address
		  //    ((12h) + physical) = 8Ch
		  memstatus.write(12,ACTIVATION_BYTE,0,1);

		  memoryPages[1].setEPROM();
	   }

	   /// <summary>
	   /// Tells if page one is in EPROM mode.
	   /// </summary>
	   /// <returns>  bool telling if page one is in EPROM mode.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool PageOneEPROMmode
	   {
		   get
		   {
			  if (!container_check)
			  {
				 container_check = this.checkStatus();
			  }
    
			  return memoryPages[1].WriteOnce;
		   }
	   }

	   /// <summary>
	   /// Write protect page zero only.
	   /// </summary>
	   public virtual void writeProtectPageZero()
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  // Enable Write protection for page zero.
		  //  - write status byte to address
		  //    ((13h) + physical) = 8Dh
		  memstatus.write(13,ACTIVATION_BYTE,0,1);

		  memoryPages[0].writeprotect();
	   }
	   /// <summary>
	   /// Get the status of page zero, if it is write protected.
	   /// </summary>
	   /// <returns>  bool telling if page zero is write protected.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool WriteProtectPageZeroSet
	   {
		   get
		   {
			  if (!container_check)
			  {
				 container_check = this.checkStatus();
			  }
    
			  return memoryPages[0].ReadOnly;
		   }
	   }

	   /// <summary>
	   /// Compute Next Secret
	   /// </summary>
	   /// <param name="addr"> address of page to use for the next secret computation. </param>
	   /// <param name="parialsecret"> the data to put into the scrathpad in computing next secret. </param>
	   public virtual void computeNextSecret(int pageNum, sbyte[] partialsecret, int offset)
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  mbScratchpad.computeNextSecret(pageNum * 32, partialsecret, offset);
	   }

	   /// <summary>
	   /// Compute Next Secret using the current contents of data page and scratchpad.
	   /// </summary>
	   /// <param name="addr"> address of page to use for the next secret computation. </param>
	   public virtual void computeNextSecret(int pageNum)
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  mbScratchpad.computeNextSecret(pageNum * 32);
	   }

	   /// <summary>
	   /// Load First Secret
	   /// </summary>
	   /// <returns>              bool saying if first secret was loaded
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool loadFirstSecret(sbyte[] data, int offset)
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  mbScratchpad.loadFirstSecret(0x080, data, offset);
		  return true;
	   }

	   /// <summary>
	   /// Refreshes a particular 8-byte set of data on a given page.
	   /// This will correct any weakly-programmed EEPROM bits.  This
	   /// feature is only available on the DS1961S, but is safely
	   /// ignored on the DS2432.  The refresh consists of a Refresh Scratchpad
	   /// command followed by a Load First Secret to the same offset.
	   /// </summary>
	   /// <param name="page"> the page number that contains the 8-bytes to refresh. </param>
	   /// <param name="offset"> the offset into the page for the 8-bytes to refresh.
	   /// </param>
	   /// <returns> <code>true</code> if refresh is successful. </returns>
	   public virtual bool refreshPage(int page, int offset)
	   {
		  if (!container_check)
		  {
			 container_check = this.checkStatus();
		  }

		  int addr = page * 32 + offset;
		  try
		  {
			 mbScratchpad.refreshScratchpad(addr);
		  }
		  catch (OneWireException)
		  {
			 // only return false for the DS2432 devices
			 // which do not support this command
			 return false;
		  }

		  mbScratchpad.loadFirstSecret(addr);
		  return true;
	   }

	   /// <summary>
	   /// Refreshes all 32 bytes of data on a given page.
	   /// This will correct any weakly-programmed EEPROM bits.  This
	   /// feature is only available on the DS1961S, but is safely
	   /// ignored on the DS2432.  The refresh consists of a Refresh Scratchpad
	   /// command followed by a Load First Secret to the same offset, for
	   /// all 8-byte offsets on the page.
	   /// </summary>
	   /// <param name="page"> the page number that will be refreshed.
	   /// </param>
	   /// <returns> <code>true</code> if refresh is successful. </returns>
	   public virtual bool refreshPage(int page)
	   {
		  return refreshPage(page, 0) && refreshPage(page, 8) && refreshPage(page, 16) && refreshPage(page, 24);
	   }

	   /// <summary>
	   /// To check the status of the part.
	   /// </summary>
	   /// <returns> bool saying the part has been checked or was checked. </returns>
	   protected internal virtual bool checkStatus()
	   {
		  if (!container_check)
		  {
			 sbyte[] mem = new sbyte [8];

			 memstatus.read(8,false,mem,0,8);

			 if ((mem [0] == unchecked((sbyte) 0xAA)) || (mem [0] == (sbyte) 0x55))
			 {
				secretProtected = true;
			 }
			 else
			 {
				secretProtected = false;
			 }

			 if (((mem [1] == unchecked((sbyte) 0xAA)) || (mem [1] == (sbyte) 0x55)) || ((mem [5] == unchecked((sbyte) 0xAA)) || (mem [5] == (sbyte) 0x55)))
			 {
				memoryPages[0].readWrite = false;
				memoryPages[0].readOnly = true;
			 }
			 else
			 {
				memoryPages[0].readWrite = true;
				memoryPages[0].readOnly = false;
			 }


			 if ((mem [4] == unchecked((sbyte) 0xAA)) || (mem [4] == (sbyte) 0x55))
			 {
				memoryPages[1].writeOnce = true;
			 }
			 else
			 {
				memoryPages[1].writeOnce = false;
			 }

			 if ((mem [1] == unchecked((sbyte) 0xAA)) || (mem [1] == (sbyte)0x55))
			 {
				memoryPages[1].readWrite = false;
				memoryPages[1].readOnly = true;
			 }
			 else
			 {
				memoryPages[1].readWrite = true;
				memoryPages[1].readOnly = false;
			 }

			 if ((mem [1] == unchecked((sbyte) 0xAA)) || (mem[1] == (sbyte) 0x55))
			 {
				memoryPages[2].readWrite = false;
				memoryPages[2].readOnly = true;
			 }
			 else
			 {
				memoryPages[2].readWrite = true;
				memoryPages[2].readOnly = false;
			 }

			 memstatus.@checked = true;
			 memoryPages[0].@checked = true;
			 memoryPages[1].@checked = true;
			 memoryPages[2].@checked = true;
			 container_check = true;
		  }

		  return container_check;
	   }

	   /// <summary>
	   /// Initialize the memory banks and data associated with each.
	   /// </summary>
	   private void initmem()
	   {
		  secretSet = false;

		  mbScratchpad = new MemoryBankScratchSHAEE(this);

		  // Set memory bank variables for the status and secret page
		  memstatus = new MemoryBankSHAEE(this, mbScratchpad);
		  memstatus.bankDescription = "Status Page that contains the secret and the status.";
		  memstatus.generalPurposeMemory = true;
		  memstatus.startPhysicalAddress = 128;
		  memstatus.size = 32;
		  memstatus.numberPages = 1;
		  memstatus.pageLength = 32;
		  memstatus.maxPacketDataLength = 32 - 3;
		  memstatus.extraInfo = false;
		  memstatus.extraInfoLength = 20;
		  memstatus.readWrite = false;
		  memstatus.writeOnce = false;
		  memstatus.pageCRC = false;
		  memstatus.readOnly = false;
		  memstatus.@checked = false;

		  // Set memory bank variables
		  memoryPages[0] = new MemoryBankSHAEE(this, mbScratchpad);
		  memoryPages[0].bankDescription = "Page Zero with write protection.";
		  memoryPages[0].generalPurposeMemory = true;
		  memoryPages[0].startPhysicalAddress = 0;
		  memoryPages[0].size = 32;
		  memoryPages[0].numberPages = 1;
		  memoryPages[0].pageLength = 32;
		  memoryPages[0].maxPacketDataLength = 32 - 3;
		  memoryPages[0].extraInfo = true;
		  memoryPages[0].extraInfoLength = 20;
		  memoryPages[0].writeOnce = false;
		  memoryPages[0].pageCRC = true;
		  memoryPages[0].@checked = false;

		  // Set memory bank varialbes
		  memoryPages[1] = new MemoryBankSHAEE(this, mbScratchpad);
		  memoryPages[1].bankDescription = "Page One with EPROM mode and write protection.";
		  memoryPages[1].generalPurposeMemory = true;
		  memoryPages[1].startPhysicalAddress = 32;
		  memoryPages[1].size = 32;
		  memoryPages[1].numberPages = 1;
		  memoryPages[1].pageLength = 32;
		  memoryPages[1].maxPacketDataLength = 32 - 3;
		  memoryPages[1].extraInfo = true;
		  memoryPages[1].extraInfoLength = 20;
		  memoryPages[1].pageCRC = true;
		  memoryPages[1].@checked = false;

		  // Set memory bank varialbes
		  memoryPages[2] = new MemoryBankSHAEE(this, mbScratchpad);
		  memoryPages[2].bankDescription = "Page Two and Three with write protection.";
		  memoryPages[2].generalPurposeMemory = true;
		  memoryPages[2].startPhysicalAddress = 64;
		  memoryPages[2].size = 64;
		  memoryPages[2].numberPages = 2;
		  memoryPages[2].pageLength = 32;
		  memoryPages[2].maxPacketDataLength = 32 - 3;
		  memoryPages[2].extraInfo = true;
		  memoryPages[2].extraInfoLength = 20;
		  memoryPages[2].writeOnce = false;
		  memoryPages[2].pageCRC = true;
		  memoryPages[2].@checked = false;

		  memoryPages[3] = memoryPages[2];
	   }

	   /// <summary>
	   ///  Authenticates page data given a MAC.
	   /// </summary>
	   /// <param name="addr">          address of the data to be read </param>
	   /// <param name="memory">        the memory read from the page </param>
	   /// <param name="mac">           the MAC calculated for this function given back as the extra info </param>
	   /// <param name="challenge">     the 3 bytes written to the scratch pad used in calculating the mac
	   ///  </param>
	   public static bool isMACValid(int addr, sbyte[] SerNum, sbyte[] memory, sbyte[] mac, sbyte[] challenge, sbyte[] secret)
	   {
		  sbyte[] MT = new sbyte [64];

		  Array.Copy(secret,0,MT,0,4);
		  Array.Copy(memory,0,MT,4,32);
		  Array.Copy(ffBlock,0,MT,36,4);

		  MT[40] = (sbyte)(0x40 | (((addr) << 3) & 0x08) | (((int)((uint)(addr) >> 5)) & 0x07));

		  Array.Copy(SerNum,0,MT,41,7);
		  Array.Copy(secret,4,MT,48,4);
		  Array.Copy(challenge,0,MT,52,3);

		  // finish up with proper padding
		  MT[55] = unchecked((sbyte) 0x80);
		  for (int i = 56;i < 62;i++)
		  {
			 MT[i] = (sbyte) 0x00;
		  }
		  MT[62] = (sbyte) 0x01;
		  MT[63] = unchecked((sbyte) 0xB8);

		  int[] AtoE = new int [5];
		  com.dalsemi.onewire.utils.SHA.ComputeSHA(MT,AtoE);

		  int cnt = 0;
		  for (int i = 0; i < 5; i++)
		  {
			 int temp = AtoE[4 - i];
			 for (int j = 0;j < 4;j++)
			 {
				if (mac[cnt++] != unchecked((sbyte)(temp & 0x0FF)))
				{
				   return false;
				}
				temp = (int)((uint)temp >> 8);
			 }
		  }

		  return true;
	   }

	   /// <summary>
	   /// <para>Installs a secret on a DS1961S/DS2432.  The secret is written in partial phrases
	   /// of 47 bytes (32 bytes to a memory page, 8 bytes to the scratchpad, 7 bytes are
	   /// discarded (but included for compatibility with DS193S)) and
	   /// is cumulative until the entire secret is processed.</para>
	   /// 
	   /// <para>On TINI, this method will be slightly faster if the secret's length is divisible by 47.
	   /// However, since secret key generation is a part of initialization, it is probably
	   /// not necessary.</para>
	   /// </summary>
	   /// <param name="page"> the page number used to write the partial secrets to </param>
	   /// <param name="secret"> the entire secret, in partial phrases, to be installed
	   /// </param>
	   /// <returns> <code>true</code> if successful
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #bindSecretToiButton(int,byte[]) </seealso>
	   public virtual bool installMasterSecret(int page, sbyte[] newSecret)
	   {
		  for (int i = 0; i < secret.Length; i++)
		  {
			 secret[i] = 0x00;
		  }
		  if (loadFirstSecret(secret, 0))
		  {
			 if (newSecret.Length == 0)
			 {
				return false;
			 }

			 sbyte[] input_secret = null;
			 sbyte[] sp_buffer = new sbyte[8];
			 int secret_mod_length = newSecret.Length % 47;

			 if (secret_mod_length == 0) //if the length of the secret is divisible by 40
			 {
				input_secret = newSecret;
			 }
			 else
			 {
				input_secret = new sbyte [newSecret.Length + (47 - secret_mod_length)];

				Array.Copy(newSecret, 0, input_secret, 0, newSecret.Length);
			 }

			 for (int i = 0; i < input_secret.Length; i += 47)
			 {
				writeDataPage(page,input_secret,i);
				Array.Copy(input_secret,i + 36,sp_buffer,0,8);
				mbScratchpad.computeNextSecret(page * 32, sp_buffer, 0);
			 }
			 return true;
		  }
		  else
		  {
			 throw new OneWireException("Load first secret failed");
		  }
	   }

	   /// <summary>
	   /// <para>Binds an installed secret to a DS1961S/DS2432 by using
	   /// well-known binding data and the DS1961S/DS2432's unique
	   /// address.  This makes the secret unique
	   /// for this iButton.</para>
	   /// </summary>
	   /// <param name="page"> the page number that has the master secret already installed </param>
	   /// <param name="bind_data"> 32 bytes of binding data used to bind the iButton to the system
	   /// </param>
	   /// <returns> <code>true</code> if successful
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #installMasterSecret(int,byte[]) </seealso>
	   public virtual bool bindSecretToiButton(int pageNum, sbyte[] bindData)
	   {
		  if (!writeDataPage(pageNum,bindData))
		  {
			 return false;
		  }

		  sbyte[] bind_code = new sbyte[8];
		  bind_code[0] = (sbyte)pageNum;
		  Array.Copy(address,0,bind_code,1,7);
		  mbScratchpad.computeNextSecret(pageNum * 32, bind_code, 0);

		  return true;
	   }

	   /// <summary>
	   /// <para>Writes a data page to the DS1961S/DS2432.</para>
	   /// </summary>
	   /// <param name="page_number"> page number to write </param>
	   /// <param name="page_data"> page data to write (must be at least 32 bytes long)
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual bool writeDataPage(int targetPage, sbyte[] pageData)
	   {

		  return writeDataPage(targetPage, pageData, 0);
	   }

	   /// <summary>
	   /// <para>Writes a data page to the DS1961S/DS2432.</para>
	   /// </summary>
	   /// <param name="page_number"> page number to write </param>
	   /// <param name="page_data"> page data to write (must be at least 32 bytes long) </param>
	   /// <param name="offset"> the offset to start copying the 32-bytes of page data.
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual bool writeDataPage(int targetPage, sbyte[] pageData, int offset)
	   {
		  int addr = 0;
		  if (targetPage == 3)
		  {
			 addr = 32;
		  }
		  memoryPages[targetPage].write(addr,pageData,offset,32);
		  return true;
	   }

	   /// <summary>
	   /// <para>Writes data to the scratchpad.  In order to write to a data page using this method,
	   /// next call <code>readScratchPad()</code>, and then <code>copyScratchPad()</code>.
	   /// Note that the addresses passed to this method will be the addresses the data is
	   /// copied to if the <code>copyScratchPad()</code> method is called afterward.</para>
	   /// 
	   /// <para>Also note that if too many bytes are written, this method will truncate the
	   /// data so that only a valid number of bytes will be sent.</para>
	   /// </summary>
	   /// <param name="targetPage"> the page number this data will eventually be copied to </param>
	   /// <param name="targetPageOffset"> the offset on the page to copy this data to </param>
	   /// <param name="inputbuffer"> the data that will be copied into the scratchpad </param>
	   /// <param name="start"> offset into the input buffer for the data to write </param>
	   /// <param name="length"> number of bytes to write
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> on a CRC error
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual bool writeScratchpad(int targetPage, int targetPageOffset, sbyte[] inputbuffer, int start, int length)
	   {
		  int addr = (targetPage << 5) + targetPageOffset;
		  mbScratchpad.writeScratchpad(addr,inputbuffer,start,length);
		  return true;
	   }

	   /// <summary>
	   /// Read from the Scratch Pad, which is a max of 8 bytes.
	   /// </summary>
	   /// <param name="scratchpad">    byte array to place read data into
	   ///                       length of array is always pageLength. </param>
	   /// <param name="offset">        offset into readBuf to pug data </param>
	   /// <param name="extraInfo">     byte array to put extra info read into
	   ///                       (TA1, TA2, e/s byte)
	   ///                       Can be 'null' if extra info is not needed.
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual void readScratchpad(sbyte[] scratchpad, int offset, sbyte[] extraInfo)
	   {
		  mbScratchpad.readScratchpad(scratchpad, offset, 8, extraInfo);
	   }

	   /// <summary>
	   /// Copy all 8 bytes of the Sratch Pad to a certain page and offset in memory.
	   /// </summary>
	   /// <param name="targetPage"> the page to copy the data to </param>
	   /// <param name="targetPageOffset"> the offset into the page to copy to </param>
	   /// <param name="copy_auth"> byte[] containing write authorization </param>
	   /// <param name="authStart"> the offset into the copy_auth array where the authorization begins.
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool copyScratchpad(int targetPage, int targetPageOffset, sbyte[] copy_auth, int authStart)
	   {
		  int addr = (targetPage << 5) + targetPageOffset;
		  mbScratchpad.copyScratchpadWithMAC(addr, copy_auth, authStart);
		  return true;
	   }

	   /// <summary>
	   /// Copy all 8 bytes of the Sratch Pad to a certain page and offset in memory.
	   /// 
	   /// The container secret must be set so that the container can produce the
	   /// correct MAC.
	   /// </summary>
	   /// <param name="targetPage"> the page to copy the data to </param>
	   /// <param name="targetPageOffset"> the offset into the page to copy to
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool copyScratchpad(int targetPage, int targetPageOffset)
	   {
		  int addr = (targetPage << 5) + targetPageOffset;
		  mbScratchpad.copyScratchpad(addr, 8);

		  return true;
	   }

	   /// <summary>
	   /// Reads a page of memory..
	   /// </summary>
	   /// <param name="page">          page number to read packet from </param>
	   /// <param name="pageData">       byte array to place read data into </param>
	   /// <param name="offset">        offset into readBuf to place data
	   /// </param>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public virtual bool readMemoryPage(int page, sbyte[] pageData, int offset)
	   {
		  int addr = 0;
		  if (page == 3)
		  {
			 addr = 32;
		  }
		  memoryPages[page].read(addr, false, pageData, offset, 32);
		  return true;
	   }

	   /// <summary>
	   /// <para>Reads and authenticates a page.  See <code>readMemoryPage()</code> for a description
	   /// of page numbers and their contents.  This method will also generate a signature for the
	   /// selected page, used in the authentication of roving (User) iButtons.</para>
	   /// </summary>
	   /// <param name="pageNum"> page number to read and authenticate </param>
	   /// <param name="pagedata"> array for the page data. </param>
	   /// <param name="offset"> offset to copy into the array </param>
	   /// <param name="computed_mac"> array for the MAC returned by the device. </param>
	   /// <param name="macStart"> offset to copy into the mac array
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed while waiting for the DS1963S's output to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual bool readAuthenticatedPage(int page, sbyte[] pagedata, int offset, sbyte[] computed_mac, int macStart)
	   {
		  int mbPage = 0;
		  if (page == 3)
		  {
			 mbPage = 1;
			 page = 2;
		  }
		  return memoryPages[page].readAuthenticatedPage(mbPage, pagedata, offset, computed_mac, macStart);
	   }
	}

}