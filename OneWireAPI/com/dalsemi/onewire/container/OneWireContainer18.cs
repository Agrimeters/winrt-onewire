using System;
using System.Collections;
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

	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using CRC16 = com.dalsemi.onewire.utils.CRC16;
	using IOHelper = com.dalsemi.onewire.utils.IOHelper;
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;


	/// <summary>
	/// <P> 1-Wire&#174 container for a SHA Transaction iButton, DS1963S.
	/// This container encapsulates the functionality of the 1-Wire family type <B>18</B> (hex).
	/// </P>
	/// 
	/// <H3> Features </H3>
	/// <UL>
	///   <LI> 4096 bits of read/write nonvolatile memory organized into 16 pages
	///   <li> 8 memory pages have 64-bit secrets and 32-bit read-only, non-rolling over write cycle counters
	///   <LI> Secrets are write only and have their own write cycle counters
	///   <li> On-chip 512-bit SHA-1 engine to compute 160-bit Message Authentication Codes
	///   <li> 256-bit scratchpad ensures integrity of data transfer
	///   <li> On-chip 16-bit CRC generator for safeguarding data transfers
	///   <li> Overdrive mode supports communication up to 142 kbits per second
	/// </UL>
	/// 
	/// <para>This container provides the functionality to use the raw power of the
	/// DS1963S.  It does not immediately implement transactions and authentication.
	/// The class <seealso cref="com.dalsemi.onewire.application.sha.SHAiButtonUser18 SHAiButton"/> does these.
	/// The <code>SHAiButton</code> class exists on top of this class, making higher
	/// level calls to implement transactions.</para>
	/// 
	/// <para>This container makes use of several optimizations to help it run fast
	/// on TINI.  These optimizations do little for the PC, but they do not
	/// slow down the PC.  Most methods are <code>synchronized</code> because they access
	/// an instance byte array.  This is less expensive than creating new byte
	/// arrays for every method, because there should
	/// not be contention for the resources in this container between threads.
	/// Threads should use the <code>com.dalsemi.onewire.adapter.DSPortAdapter</code> methods
	/// <code>beginExclusive(bool)</code> and <code>endExclusive()</code> to synchronize on the 1-Wire port.</para>
	/// 
	/// <para>Notice that for maximum performance, programs should call the method <code>setSpeedCheck(bool)</code>
	/// with an argument <code>false</code> before any other methods that access the
	/// 1-Wire.  A program that calls this function is assumed to understand and
	/// control the speed of communication on the 1-Wire bus.  If the speed check is not disabled,
	/// a call to the method <code>OneWireContainer.doSpeed()</code> will occur in almost every
	/// function.  While this should guarantee that the 1-Wire bus is never at an unknown speed,
	/// it will slow down throughput considerably.</para>
	/// 
	/// <H3> Memory </H3>
	/// 
	/// <para>In the interest of speed, this container has several methods to quickly access
	/// the memory of a DS9163S.  These methods include:
	/// <ul>
	///     <li> <seealso cref="#eraseScratchPad(int)                    <code>eraseScratchPad</code>"/>
	///     <li> <seealso cref="#readScratchPad(byte[],int)              <code>readScratchPad</code>"/>
	///     <li> <seealso cref="#writeScratchPad(int,int,byte[],int,int) <code>writeScratchPad</code>"/>
	///     <li> <seealso cref="#readMemoryPage(int,byte[],int)          <code>readMemoryPage</code>"/>
	///     <li> <seealso cref="#readAuthenticatedPage(int,byte[],int)   <code>readAuthenticatedPage</code>"/>
	///     <li> <seealso cref="#writeDataPage(int,byte[])               <code>writeDataPage</code>"/>
	/// </ul></para>
	/// 
	/// <P> The memory can also be accessed through the objects that are returned
	/// from the <seealso cref="#getMemoryBanks() getMemoryBanks"/> method. </P>
	/// 
	/// The following is a list of the MemoryBank instances that are returned:
	/// 
	/// <UL>
	///   <LI> <B> Scratchpad with CRC and auto-hide</B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 32 starting at physical address 0
	///         <LI> <I> Features</I> Read/Write not-general-purpose volatile
	///         <LI> <I> Pages</I> 1 page of length 32 bytes
	///         <LI> <I> Page Features </I> page-device-CRC
	///         <li> <i> Extra information for each page</i>  Target address, offset, length 3
	///      </UL>
	///   <LI> <B> Main Memory </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 256 starting at physical address 0
	///         <LI> <I> Features</I> Read/Write general-purpose non-volatile
	///         <LI> <I> Pages</I> 8 pages of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC
	///      </UL>
	///   <LI> <B> Memory with write cycle counter </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 256 starting at physical address 256
	///         <LI> <I> Features</I> Read/Write general-purpose non-volatile
	///         <LI> <I> Pages</I> 8 pages of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC
	///         <li> <i> Extra information for each page</i>  Write cycle counter, length 3
	///      </UL>
	///   <LI> <B> Write cycle counters and PRNG counter </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	///         <LI> <I> Size </I> 96 starting at physical address 608
	///         <LI> <I> Features</I> Read-only not-general-purpose non-volatile
	///         <LI> <I> Pages</I> 3 pages of length 32 bytes
	///      </UL>
	/// </UL>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// <para>For examples on using the SHA iButton for transactions and authentications, see
	/// the examples in the <seealso cref="com.dalsemi.onewire.application.sha.SHAiButtonUser18 SHAiButton"/>
	/// usage section.</para>
	/// 
	/// For examples regarding memory operations,
	/// <uL>
	/// <li> See the usage example in
	/// <seealso cref="com.dalsemi.onewire.container.OneWireContainer OneWireContainer"/>
	/// to enumerate the MemoryBanks.
	/// <li> See the usage examples in
	/// <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/> and
	/// <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
	/// for bank specific operations.
	/// </uL>
	/// 
	/// <H3> DataSheet </H3>
	/// <DL>
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1963S.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1963S.pdf</A>
	/// </DL>
	/// 
	/// <para>Note that the protocol for transactions using SHA iButtons calls for
	/// 2 iButtons.  One belongs to the system that grants authentication and
	/// is authorized to validate money.  This iButton is usually called the
	/// Coprocessor.  The other iButton is the device that contains digital
	/// money that must be authenticated and verified to perform transactions.
	/// This iButton is usually called a User iButton, Roaming iButton,
	/// or Roving iButton.</para>
	/// 
	/// @author KLA
	/// @version 0.00, 28 Aug 2000 </summary>
	/// <seealso cref= com.dalsemi.onewire.application.sha.SHAiButtonUser18 </seealso>
	/// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#beginExclusive(bool) </seealso>
	/// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#endExclusive() </seealso>
	/// <seealso cref= OneWireContainer#doSpeed() </seealso>
	public class OneWireContainer18 : OneWireContainer
	{

	   //turns on extra debugging output in all 1-wire containers
	   private const bool DEBUG = true;

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Scratchpad access memory bank
	   /// </summary>
	   private MemoryBankScratch scratch;

	   /// <summary>
	   /// Main memory access memory bank
	   /// </summary>
	   private MemoryBankNVCRC memory;

	   /// <summary>
	   /// Main memory with counter access memory bank
	   /// </summary>
	   private MemoryBankNVCRC memoryPlus;

	   /* number of times we wait for the 10101010... to appear
	    * on commands such as read_auth_page, copy_scratch...
	    */
	   private int block_wait_count = 20;

	   /* are we currently using resume?  some internal code
	    * makes use of the resume function, but often we can't
	    * tell from inside this class
	    */
	   private bool resume = false;

	   /* should we call doSpeed every time we do something?
	    * this is the safe way to make sure we never loose
	    * communication, but this stuff has really gotta
	    * fly, so to make it fly, disable this before doing anything
	    * else!
	    */
	   private bool doSpeedEnable = true;

	   private sbyte[] byte_buffer = new sbyte [60]; //use this everywhere to communicate
	   private sbyte[] private_address = null;


	   internal static sbyte[] FF = new sbyte [60]; //use this to fill an array with 0x0ff's
	   static OneWireContainer18()
	   {
		  for (int i = 0; i < FF.Length; i++)
		  {
			 FF [i] = unchecked((sbyte) 0x0ff);
		  }
	   }
	   //there's really no good reason for these to be public like they were in 0.00 OneWire release
	   private sbyte TA1;
	   private sbyte TA2;
	   private sbyte ES;

	   //--------
	   //-------- PUBLIC STATIC FINAL's
	   //--------

	   /* 1-Wire Protocol command to read the DS1963S memory.
	    * See the datasheet for more information.
	    *
	    * @see #readMemoryPage(int,byte[],int)
	    */
	   public static readonly sbyte READ_MEMORY = unchecked((sbyte) 0xF0);

	   /* 1-Wire Protocol command to write the DS1963S scratchpad.
	    * See the datasheet for more information.
	    *
	    * @see #writeScratchPad(int,int,byte[],int,int)
	    */
	   public static readonly sbyte WRITE_SCRATCHPAD = (sbyte) 0x0F;

	   /* 1-Wire Protocol command to match a signature to
	    * the one in the DS1963S scratchpad.  To verify a signature,
	    * the DS1963S goes into hidden mode.  It can only be verified
	    * with this command.  See the datasheet for more information.
	    *
	    * @see #matchScratchPad(byte[])
	    */
	   public static readonly sbyte MATCH_SCRATCHPAD = (sbyte) 0x3C;

	   /* 1-Wire Protocol command to erase the DS1963S scratchpad.
	    * Also brings the device out of hidden mode and back to
	    * normal operation.  See the datasheet for more information.
	    *
	    * @see #eraseScratchPad(int)
	    */
	   public static readonly sbyte ERASE_SCRATCHPAD = unchecked((sbyte) 0xC3);

	   /* 1-Wire Protocol command to read the DS1963S scratchpad.
	    * See the datasheet for more information.
	    *
	    * @see #readScratchPad(byte[],int)
	    */
	   public static readonly sbyte READ_SCRATCHPAD = unchecked((sbyte) 0xAA);

	   /* 1-Wire Protocol command to read an authenticated
	    * DS1963S memory page.  The device generates a signature for
	    * the page that is placed in the scratchpad.
	    * See the datasheet for more information.
	    *
	    * @see #readAuthenticatedPage(int,byte[],int)
	    */
	   public static readonly sbyte READ_AUTHENTICATED_PAGE = unchecked((sbyte) 0xA5);

	   /* 1-Wire Protocol command to copy the DS1963S scratchpad
	    * to a memory location.  See the datasheet for more information.
	    *
	    * @see #copyScratchPad()
	    */
	   public static readonly sbyte COPY_SCRATCHPAD = (sbyte) 0x55;

	   /* 1-Wire Protocol command to perform a SHA cryptographic
	    * function on the DS1963S.  See the datasheet for more information.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    */
	   public static readonly sbyte COMPUTE_SHA = (sbyte) 0x33;

	   //SHA commands

	   /* 1-Wire Protocol command to compute a master secret
	    * on the DS1963S.  See the datasheet for more information.
	    * This command should be used with the first partial secret.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    * @see #installMasterSecret(int,byte[],int)
	    */
	   public static readonly sbyte COMPUTE_FIRST_SECRET = (sbyte) 0x0F;

	   /* 1-Wire Protocol command to compute a master secret
	    * on the DS1963S.  See the datasheet for more information.
	    * This command should be used with any partial secret
	    * except the first.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    * @see #installMasterSecret(int,byte[],int)
	    */
	   public static readonly sbyte COMPUTE_NEXT_SECRET = unchecked((sbyte) 0xF0);

	   /* 1-Wire Protocol command to verify signatures
	    * on the DS1963S.  See the datasheet for more information.
	    * This command should be used to verify authentication
	    * and to verify user data.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    */
	   public static readonly sbyte VALIDATE_DATA_PAGE = (sbyte) 0x3C;

	   /* 1-Wire Protocol command to create a signature of a
	    * selected data page on the DS1963S.  See the datasheet for more information.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    */
	   public static readonly sbyte SIGN_DATA_PAGE = unchecked((sbyte) 0xC3);

	   /* 1-Wire Protocol command to create a random challenge
	    * using the DS1963S's pseudo random number generator.
	    * See the datasheet for more information.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    * @see #SHAiButton#generateChallenge(int,int,byte[])
	    */
	   public static readonly sbyte COMPUTE_CHALLENGE = unchecked((sbyte) 0xCC);

	   /* 1-Wire Protocol command to authenticate a host
	    * on the DS1963S.  See the datasheet for more information.
	    *
	    * @see #SHAFunction(byte,int)
	    * @see #SHAFunction(byte)
	    */
	   public static readonly sbyte AUTH_HOST = unchecked((sbyte) 0xAA);

	   /* 1-Wire Protocol command that allows quick reselection
	    * of the DS1963S.  Normally, selection involved a nine byte sequence:
	    * one byte for the Select Command, and the eight byte address of the
	    * 1-Wire device.  The 1963S remembers if it was the last device to
	    * communicate with the master on the 1-Wire bus.  If it was, and it
	    * receives the <code>RESUME</code> command, then it is selected.
	    *
	    * @see #useResume(bool)
	    */
	   public static readonly sbyte RESUME = unchecked((sbyte) 0xA5);

	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS1963S SHA iButton.
	   /// Note that the method <code>setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[])</code>
	   /// must be called to set the correct <code>DSPortAdapter</code> device address.
	   /// </summary>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer#setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) setupContainer(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer18(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer18(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer18(DSPortAdapter,String) </seealso>
	   public OneWireContainer18() : base(null,0)
	   {

		  if (private_address == null)
		  {
			 private_address = new sbyte [8];
		  }

		  // initialize the memory banks
		  initMem();
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS1963S SHA iButton.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton </param>
	   /// <param name="newAddress">        address of this DS1963S SHA iButton
	   /// </param>
	   /// <seealso cref= #OneWireContainer18() </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer18(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer18(DSPortAdapter,String) </seealso>
	   public OneWireContainer18(DSPortAdapter sourceAdapter, sbyte[] newAddress) : base(sourceAdapter, newAddress)
	   {

		  if (private_address == null)
		  {
			 private_address = new sbyte [8];
		  }

		  // initialize the memory banks
		  initMem();
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS1963S SHA iButton.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton </param>
	   /// <param name="newAddress">        address of this DS1963S SHA iButton
	   /// </param>
	   /// <seealso cref= #OneWireContainer18() </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer18(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer18(DSPortAdapter,String) </seealso>
	   public OneWireContainer18(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {

		  if (private_address == null)
		  {
			 private_address = new sbyte [8];
		  }

		  // initialize the memory banks
		  initMem();
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS1963S SHA iButton.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton </param>
	   /// <param name="newAddress">        address of this DS1963S SHA iButton
	   /// </param>
	   /// <seealso cref= #OneWireContainer18() </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer18(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer18(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer18(DSPortAdapter,long) </seealso>
	   public OneWireContainer18(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {

		  if (private_address == null)
		  {
			 private_address = new sbyte [8];
		  }

		  // initialize the memory banks
		  initMem();
	   }

	   //--------
	   //-------- Methods
	   //--------

	   /// <summary>
	   /// <para>Provides this container with the adapter object used to access this device and
	   /// the address of the iButton or 1-Wire device.  Overrides the <code>OneWireContainer</code>
	   /// method for speed, as the <code>OneWireContainer</code> version has
	   /// a byte array allocation.  Since there is a call to <code>setupContainer()</code>
	   /// in the critical path of a transaction (when a roving SHA iButton is
	   /// introduced to the 1-Wire Bus), this must occur quickly.  This improves performance
	   /// on TINI.</para>
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   ///                           this iButton </param>
	   /// <param name="newAddress">        address of this 1-Wire device
	   /// </param>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   public override void setupContainer(DSPortAdapter sourceAdapter, sbyte[] newAddress)
	   {
		  // get a reference to the source adapter (will need this to communicate)
		  this.adapter = sourceAdapter;

		  // set the Address
		  lock (this)
		  {
			 if (private_address == null)
			 {
				private_address = new sbyte[8];
			 }

			 Array.Copy(newAddress, 0, private_address, 0, 8);

			 this.address = private_address;
		  }

		  // set desired speed to be SPEED_REGULAR by default with no fallback
		  speed = DSPortAdapter.SPEED_REGULAR;
		  speedFallBackOK = false;
	   }

	   /// <summary>
	   /// Get the Dallas Semiconductor part number of the iButton
	   /// or 1-Wire Device as a <code>java.lang.String</code>.
	   /// For example "DS1992".
	   /// </summary>
	   /// <returns> iButton or 1-Wire device name </returns>
	   public override string Name
	   {
		   get
		   {
			  return "DS1963S";
		   }
	   }

	   /// <summary>
	   /// Retrieve the alternate Dallas Semiconductor part numbers or names.
	   /// A 'family' of MicroLAN devices may have more than one part number
	   /// depending on packaging.  There can also be nicknames such as
	   /// "Crypto iButton".
	   /// </summary>
	   /// <returns>  the alternate names for this iButton or 1-Wire device </returns>
	   public override string AlternateNames
	   {
		   get
		   {
			  return "SHA-1 iButton";
		   }
	   }

	   /// <summary>
	   /// Get a short description of the function of this iButton
	   /// or 1-Wire Device type.
	   /// </summary>
	   /// <returns> device description </returns>
	   public override string Description
	   {
		   get
		   {
			  return "4096 bits of read/write nonvolatile memory. Memory " + "is partitioned into sixteen pages of 256 bits each. " + "Has overdrive mode.  One-chip 512-bit SHA-1 engine " + "and secret storage.";
		   }
	   }

	   /// <summary>
	   /// Returns the maximum speed this iButton device can
	   /// communicate at.
	   /// </summary>
	   /// <returns> maximum speed </returns>
	   /// <seealso cref= DSPortAdapter#setSpeed </seealso>
	   public override int MaxSpeed
	   {
		   get
		   {
			  return DSPortAdapter.SPEED_OVERDRIVE;
		   }
	   }

	   /// <summary>
	   /// Gets an enumeration of memory bank instances that implement one or more
	   /// of the following interfaces:
	   /// <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	   /// <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>,
	   /// and <seealso cref="com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank"/>. </summary>
	   /// <returns> <CODE>Enumeration</CODE> of memory banks </returns>
	   public override System.Collections.IEnumerator MemoryBanks
	   {
		   get
		   {
			  ArrayList bank = new ArrayList(4);
    
			  // scratchpad
			  bank.Add(scratch);
    
			  // NVRAM (no write cycle)
			  bank.Add(memory);
    
			  // NVRAM (with write cycle counters)
			  bank.Add(memoryPlus);
    
			  // Page Write cycle counters
			  MemoryBankNV cnt = new MemoryBankNV(this, (MemoryBankScratch) scratch);
    
			  cnt.numberPages = 3;
			  cnt.size = 96;
			  cnt.bankDescription = "Write cycle counters and PRNG counter";
			  cnt.startPhysicalAddress = 608;
			  cnt.readOnly = true;
			  cnt.pageAutoCRC = false;
			  cnt.generalPurposeMemory = false;
			  cnt.readWrite = false;
    
			  bank.Add(cnt);
    
			  return bank.GetEnumerator();
		   }
	   }

	   /// <summary>
	   /// Construct the memory banks used for I/O.
	   /// </summary>
	   private void initMem()
	   {

		  // scratchpad
		  scratch = new MemoryBankScratchSHA(this);

		  // NVRAM (no write cycle)
		  memory = new MemoryBankNVCRC(this, (MemoryBankScratch) scratch);
		  memory.numberPages = 8;
		  memory.size = 256;
		  memory.extraInfoLength = 8;
		  memory.readContinuePossible = false;
		  memory.numVerifyBytes = 8;

		  // NVRAM (with write cycle counters)
		  memoryPlus = new MemoryBankNVCRC(this, (MemoryBankScratch) scratch);
		  memoryPlus.numberPages = 8;
		  memoryPlus.size = 256;
		  memoryPlus.bankDescription = "Memory with write cycle counter";
		  memoryPlus.startPhysicalAddress = 256;
		  memoryPlus.extraInfo = true;
		  memoryPlus.extraInfoDescription = "Write cycle counter";
		  memoryPlus.extraInfoLength = 8;
		  memoryPlus.readContinuePossible = false;
		  memoryPlus.numVerifyBytes = 8;
	   }

	   ////////////////////////////////////////////////////////////////////
	   //SHAiButton real methods!!!!!!
	   ////////////////////////////////////////////////////////////////////

	   /// <summary>
	   /// Directs the container to avoid the calls to doSpeed() in methods that communicate
	   /// with the SHA iButton. To ensure that all parts can talk to the 1-Wire bus
	   /// at their desired speed, each method contains a call
	   /// to <code>doSpeed()</code>.  However, this is an expensive operation.
	   /// If a user manages the bus speed in an
	   /// application,  call this method with <code>doSpeedCheck</code>
	   /// as <code>false</code>.  The default behavior is
	   /// to call <code>doSpeed()</code>.
	   /// </summary>
	   /// <param name="doSpeedCheck"> <code>true</code> for <code>doSpeed()</code> to be called before every
	   /// 1-Wire bus access, <code>false</code> to skip this expensive call
	   /// </param>
	   /// <seealso cref= OneWireContainer#doSpeed() </seealso>
	   public virtual bool SpeedCheck
	   {
		   set
		   {
			   lock (this)
			   {
				  doSpeedEnable = value;
			   }
		   }
	   }

	   /// <summary>
	   /// <para>Tells this <code>OneWireContainer18</code> whether it can use the <code>RESUME</code> command.
	   /// The <code>RESUME</code> command allows the DS1963S to be re-selected for communication
	   /// quickly by using a one <code>byte</code> <code>RESUME</code> command rather than a
	   /// nine <code>byte</code> selection sequence.  If another 1-Wire device is accessed,
	   /// <code>useResume(false)</code> must be called, a normal selection of the part
	   /// must be performed, and then <code>useResume(true)</code> can be called.</para>
	   /// </summary>
	   /// <param name="set"> <code>true</code> to use the one <code>byte</code> <code>RESUME</code> command
	   /// instead of the nine <code>byte</code> select command
	   /// </param>
	   /// <seealso cref= #RESUME </seealso>
	   public virtual void useResume(bool set)
	   {
		   lock (this)
		   {
			  resume = set;
		   }
	   }

	   /// <summary>
	   /// <para>Erases the scratchpad of the DS1963S.  The cryptographic features
	   /// of the DS1963S leave the device in 'hidden mode', which makes most
	   /// memory functions inaccessible.  A call to <code>eraseScratchPad(int)</code>
	   /// brings the device out of 'hidden mode', filling the scratchpad
	   /// with 0x0FF <code>bytes</code>.</para>
	   /// 
	   /// <para>The argument <code>page</code> is usually unimportant, except in cases
	   /// where a memory page needs to be erased.  Erase a memory page by calling
	   /// <code>eraseScratchPad(page_number)</code>, followed by <code>readScratchPad()</code>
	   /// and then <code>copyScratchPad()</code>.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="page"> the target page number
	   /// </param>
	   /// <returns> true if successful, false if the operation failed while waiting for the
	   /// DS1963S's output to alternate (see the datasheet for a description)
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #waitForSuccessfulFinish() </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   public virtual bool eraseScratchPad(int page)
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  // select the device
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  // build block to send
			  sbyte[] buffer = byte_buffer;
        
			  buffer [0] = ERASE_SCRATCHPAD;
			  buffer [1] = (sbyte)(page << 5);
			  buffer [2] = (sbyte)(page >> 3);
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Erase Scratchpad");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)buffer [2]);
				 IOHelper.writeLineHex((sbyte)buffer [1]);
				 IOHelper.writeLine("adapter.getSpeed()=" + adapter.Speed);
				 IOHelper.writeLine("adapter.getPortTypeDescription()=" + adapter.PortTypeDescription);
				 IOHelper.writeLine("this.speed=" + this.speed);
				 IOHelper.writeLine("device isPresent: " + adapter.isPresent(this.address));
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  Array.Copy(FF, 0, buffer, 3, 3);
        
			  if (DEBUG)
			  {
				 IOHelper.writeLine("buffer:");
				 IOHelper.writeBytesHex(buffer,0,9);
			  }
        
			  // send block (check copy indication complete)
			  adapter.dataBlock(buffer, 0, 6);
        
			  if (DEBUG)
			  {
				 IOHelper.writeLine("buffer:");
				 IOHelper.writeBytesHex(buffer,0,9);
				 IOHelper.writeLine("------------------------------------");
			  }
        
			  if (buffer [5] == unchecked((sbyte) 0x0ff))
			  {
				 return waitForSuccessfulFinish();
			  }
			  else
			  {
				 return true;
			  }
		   }
	   }

	   /// <summary>
	   /// <para>Waits for the DS1963S's output to alternate.  Several operations must
	   /// wait for the DS1963S to stop sending 0x0ff's and begin alternating its
	   /// output bits.  This method reads until it finds a non-0x0ff <code>byte</code> or until it
	   /// reaches a specified number of tries, indicating failure.</para>
	   /// 
	   /// <para>This method can often be optimized away.  A normal 1-Wire transaction
	   /// involves writing and reading a known number of bytes.  If a few more bytes
	   /// are read, a program can check to see if the DS1963S has started alternating
	   /// its output much quicker than calling this method will.  For instance,
	   /// to copy the scratchpad, the source code might look like this:
	   /// <code><pre>
	   ///    buffer [0] = COPY_SCRATCHPAD;
	   ///    buffer [1] = TA1;
	   ///    buffer [2] = TA2;
	   ///    buffer [3] = ES;
	   /// 
	   ///    adapter.dataBlock(buffer,0,4);
	   ///    return waitForSuccessfulFinish();
	   /// </pre></code>
	   /// To optimize the code, read more bytes than required:
	   /// <code><pre>
	   ///    buffer [0] = COPY_SCRATCHPAD;
	   ///    buffer [1] = TA1;
	   ///    buffer [2] = TA2;
	   ///    buffer [3] = ES;
	   /// 
	   ///    //copy 0x0FF into the buffer, this effectively reads
	   ///    System.arraycopy(FF, 0, buffer, 4, 5);
	   /// 
	   ///    //read 5 extra bytes
	   ///    adapter.dataBlock(buffer, 0, 9);
	   /// 
	   ///    //if the last byte has not shown alternating output,
	   ///    //still call waitForSuccessfulFinish(), else
	   ///    //we are already done
	   ///    if (buffer [8] == ( byte ) 0x0ff)
	   ///         return waitForSuccessfulFinish();
	   ///     else
	   ///         return true;
	   /// </pre></code></para>
	   /// 
	   /// <para>The second method is faster because it is more expensive to
	   /// invoke another method that goes down to the native access layer
	   /// than it is to just read a few more bytes while the program is
	   /// already at the native access layer.</para>
	   /// 
	   /// <para>See the datasheet for which operations function in this manner.
	   /// Only call this method after another method which has successfully
	   /// communicated with the DS1963S.</para>
	   /// </summary>
	   /// <returns> <code>true</code> if the DS1963S completed its operation successfully
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   /// <seealso cref= #eraseScratchPad(int) </seealso>
	   /// <seealso cref= #readAuthenticatedPage(int,byte[],int) </seealso>
	   public virtual bool waitForSuccessfulFinish()
	   {
		   lock (this)
		   {
        
			  //this method should be called after another method, so let's not worry
			  //about making sure the speed is right, it should be already
			  int count = 0;
        
			  while (adapter.Byte == 0xff)
			  {
				 count++;
        
				 if (count == block_wait_count)
				 {
					return false;
				 }
			  }
        
			  return true;
		   }
	   }

	   /// <summary>
	   /// <para>Reads a memory page from the DS1963S.  Pages 0-15 are normal memory pages.
	   /// Pages 16 and 17 contain the secrets--the DS1963S will return all 1's (0x0ff
	   /// bytes).  Page 18 is the physical location of the scratchpad, and will only
	   /// return valid data if the part is not in hidden mode (0x0ff bytes are returned
	   /// otherwise).  Pages 19 and 20 contain the write cycle counters.
	   /// Page 21 contains the counter for the pseudo random number generator (PRNG).
	   /// Consult the datasheet for the memory maps of these special pages.</para>
	   /// 
	   /// <para>Note that the same data can be returned through the <code>MemoryBank</code>s
	   /// returned by <code>getMemoryBanks()</code>.  However, this method contains
	   /// several enhancements for faster reading.</para>
	   /// </summary>
	   /// <param name="pageNum"> page number to read </param>
	   /// <param name="data"> array for the return of the data (must be at least 32 bytes long) </param>
	   /// <param name="start"> offset into the byte array to start copying page data
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #readAuthenticatedPage(int,byte[],int) </seealso>
	   /// <seealso cref= #getMemoryBanks() </seealso>
	   public virtual void readMemoryPage(int pageNum, sbyte[] data, int start)
	   {

		  //don't need to be synchronized, since readMemoryPage(int, byte, int, byte[], int) is
		  readMemoryPage(pageNum, READ_MEMORY, 32, data, start);
	   }

	   /*
	    * read the contents of a data page
	    */
	   private void readMemoryPage(int pageNum, sbyte COMMAND, int bytes_to_read, sbyte[] data, int start)
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
			  int addr = pageNum << 5; //pageNumber * 32
        
			  buffer [0] = COMMAND;
			  buffer [1] = (sbyte) addr;
			  buffer [2] = (sbyte)(addr >> 8);
        
			  Array.Copy(FF, 0, buffer, 3, bytes_to_read);
			  adapter.dataBlock(buffer, 0, 3 + bytes_to_read);
        
			  // copy data for return
			  Array.Copy(buffer, 3, data, start, bytes_to_read);
		   }
	   }

	   /// <summary>
	   /// <para>Reads and authenticates a page.  See <code>readMemoryPage()</code> for a description
	   /// of page numbers and their contents.  This method will also generate a signature for the
	   /// selected page, used in the authentication of roving (User) iButtons.
	   /// Extra data is returned with the page as well--such as the write cycle
	   /// counter for the page and the write cycle counter of the selected
	   /// secret page.</para>
	   /// <ul>
	   ///     <li>32 bytes of page data                </li>
	   ///     <li>4 bytes secret counter for page      </li>
	   ///     <li>4 bytes write cycle counter for page </li>
	   ///     <li>2 byte CRC                           </li>
	   /// </ul>
	   /// <para>Note that the CRC will be verified by this method, though it is returned with the data.</para>
	   /// </summary>
	   /// <param name="pageNum"> page number to read and authenticate </param>
	   /// <param name="data"> array for the page data plus extra information (2 write cycle
	   /// counters of 4 bytes each, one 2 byte CRC, appended after 32 bytes of
	   /// the data page).  This byte array must be at least 42 bytes long. </param>
	   /// <param name="start"> offset to copy into the array
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed while waiting for the DS1963S's output to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #readMemoryPage(int,byte[],int) </seealso>
	   public virtual bool readAuthenticatedPage(int pageNum, sbyte[] data, int start)
	   {

		  //don't need to be synchronized, since readMemoryPage(int, byte, int, byte[], int) is
		  //read 42 bytes = 32 page bytes + 4 bytes secret counter + 4 bytes page counter + 2 bytes CRC
		  readMemoryPage(pageNum, READ_AUTHENTICATED_PAGE, 42, data, start);

		  int crc = CRC16.compute(READ_AUTHENTICATED_PAGE);

		  crc = CRC16.compute((sbyte)(pageNum << 5), crc); //(pagenumber*32 = address) lower 8 bits
		  crc = CRC16.compute((sbyte)((int)((uint)pageNum >> 3)), crc); //pagenumber*32 is pagenumber<<5, but
		  //we want the upper 8 bits, so we would just do
		  // (pagenum<<5)>>>8, so just make it one op

		  // check CRC16
		  if (CRC16.compute(data, start, 42, crc) != 0xB001)
		  {
			 return false;
		  }

		  return (waitForSuccessfulFinish());
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
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   /// <seealso cref= #eraseScratchPad(int) </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   /// <seealso cref= #writeDataPage(int,byte[]) </seealso>
	   public virtual bool writeScratchPad(int targetPage, int targetPageOffset, sbyte[] inputbuffer, int start, int length)
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
			  int addr = (targetPage << 5) + targetPageOffset;
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Writing Scratchpad of iButton");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("targetPage: " + targetPage);
				 IOHelper.writeLine("targetPageOffset: " + targetPageOffset);
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)(addr >> 8));
				 IOHelper.writeLineHex((sbyte)addr);
				 IOHelper.writeLine("inputbuffer");
				 IOHelper.writeBytesHex(inputbuffer);
				 IOHelper.writeLine("start: " + start);
				 IOHelper.writeLine("length: " + length);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
        
			  buffer [0] = WRITE_SCRATCHPAD;
			  buffer [1] = (sbyte) addr;
			  buffer [2] = (sbyte)(addr >> 8);
        
			  int maxbytes = 32 - (addr & 31);
        
			  if (length > maxbytes)
			  {
				 length = maxbytes; //if we are going to go over the 32byte boundary
			  }
        
			  //let's cut it
			  Array.Copy(inputbuffer, start, buffer, 3, length);
        
			  buffer [3 + length] = unchecked((sbyte) 0xff);
			  buffer [4 + length] = unchecked((sbyte) 0xff); //leave space for the CRC
        
			  //this nasty statement sends the length depending on if we are
			  //going to get a CRC back.  you only get a CRC if you get to the end of
			  //the scratchpad writing.  so we look at the address and add the length
			  //that we are writing.  if this is not a multiple of 32 then we do not
			  //finish reading at the scratchpad boundary (and we checked earlier to
			  //make sure don't go over THEREFORE we have gone under).  if we are at
			  //the boundary we need two extra bytes to read the crc
			  adapter.dataBlock(buffer, 0, ((((addr + length) & 31) == 0) ? length + 5 : length + 3));
        
			  //if we dont check the CRC we are done
			  if (((addr + length) & 31) != 0)
			  {
				 return true;
			  }
        
			  //else we need to do a CRC calculation
			  if (CRC16.compute(buffer, 0, length + 5, 0) != 0xB001)
			  {
				 if (DEBUG)
				 {
					Debug.WriteLine("CRC Failed in Write Scratchpad");
				 }
				 return false;
			  }
        
			  return true;
		   }
	   }

	   /// <summary>
	   /// <para>Verifies the hidden signature in the scratchpad of the DS1963S.
	   /// After a <code>VALIDATE_DATA_PAGE</code> command, the scratchpad
	   /// contains a signature that cannot be read, but must be verified.
	   /// This method gives the DS1963S a signature.  The iButton then
	   /// checks to see if it equals the hidden signature in its
	   /// scratchpad.</para>
	   /// 
	   /// <para>The signature must be 20 bytes long, and it must start at offset 0
	   /// of the input array.</para>
	   /// </summary>
	   /// <param name="mac"> the signature starting at offset 0
	   /// </param>
	   /// <returns> <code>true</code> if the signature matches
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #VALIDATE_DATA_PAGE </seealso>
	   public virtual bool matchScratchPad(sbyte[] mac)
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
        
			  buffer [0] = MATCH_SCRATCHPAD;
        
			  Array.Copy(mac, 0, buffer, 1, 20);
        
			  buffer [21] = unchecked((sbyte) 0x0ff); //CRC1
			  buffer [22] = unchecked((sbyte) 0x0ff); //CRC2
			  buffer [23] = unchecked((sbyte) 0x0ff); //status
        
			  adapter.dataBlock(buffer, 0, 24);
        
			  if (CRC16.compute(buffer, 0, 23, 0) != 0xB001)
			  {
				 return false;
			  }
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Matching the scratchpad");
				 IOHelper.writeLine("Address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("mac");
				 IOHelper.writeBytesHex(mac);
				 IOHelper.writeLine("matchScratchpad buffer");
				 IOHelper.writeBytesHex(buffer,0,23);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  if (buffer [23] != unchecked((sbyte) 0x0ff))
			  {
				 return true;
			  }
        
			  return false;
		   }
	   }

	   /// <summary>
	   /// <para>Reads the contents of the DS1963S scratchpad.  If the device is in hidden
	   /// mode, all 1's (0x0ff bytes) will be returned.</para>
	   /// 
	   /// <para>This method will return up to 32 bytes.  It may return less
	   /// if the target address stored internally on the DS1963S is
	   /// not a multiple of 32.</para>
	   /// </summary>
	   /// <param name="data"> array to hold the contents of the scratchpad </param>
	   /// <param name="start"> offset to copy the scratchpad data into the <code>byte</code> array
	   /// </param>
	   /// <returns> the number of bytes read, or -1 if there was a CRC failure
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #writeScratchPad(int,int,byte[],int,int) </seealso>
	   public virtual int readScratchPad(sbyte[] data, int start)
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
        
			  buffer [0] = READ_SCRATCHPAD;
        
			  Array.Copy(FF, 0, buffer, 1, 37);
			  adapter.dataBlock(buffer, 0, 38);
        
			  TA1 = buffer [1];
			  TA2 = buffer [2];
			  ES = buffer [3];
        
			  int length = 32 - (TA1 & 31);
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Read Scratchpad");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)TA2);
				 IOHelper.writeLineHex((sbyte)TA1);
				 IOHelper.write("ES: 0x");
				 IOHelper.writeLineHex((sbyte)ES);
				 IOHelper.writeLine("data");
				 IOHelper.writeBytesHex(buffer,4,length);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  if (CRC16.compute(buffer, 0, 6 + length, 0) != 0xB001)
			  {
				 return -1;
			  }
        
			  if (data != null)
			  {
				 Array.Copy(buffer, 4, data, start, length);
			  }
        
			  return length;
		   }
	   }

	   /// <summary>
	   /// Copies the contents of the scratchpad to the target destination
	   /// that was specified in a call to <code>writeScratchPad()</code> or
	   /// <code>eraseScratchPad()</code>.  This method will not success unless
	   /// a call to <code>readScratchPad()</code> is made first to verify
	   /// the target address registers in the DS1963S.
	   /// </summary>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed while waiting for the DS1963S's output to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #eraseScratchPad(int) </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   /// <seealso cref= #writeScratchPad(int,int,byte[],int,int) </seealso>
	   public virtual bool copyScratchPad()
	   {
		   lock (this)
		   {
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
        
			  buffer [0] = COPY_SCRATCHPAD;
			  buffer [1] = TA1;
			  buffer [2] = TA2;
			  buffer [3] = ES;
        
			  Array.Copy(FF, 0, buffer, 4, 5);
        
			  //adapter.dataBlock(buffer,0,4);
			  adapter.dataBlock(buffer, 0, 9);
        
			  if (buffer [8] == unchecked((sbyte) 0x0ff))
			  {
				 return waitForSuccessfulFinish();
			  }
			  else
			  {
				 return true;
			  }
		   }
	   }

	   /// <summary>
	   /// <para>Installs a secret on a DS1963S.  The secret is written in partial phrases
	   /// of 47 bytes (32 bytes to a memory page, 15 bytes to the scratchpad) and
	   /// is cumulative until the entire secret is processed.  Secrets are associated
	   /// with a page number.  See the datasheet for more information on this
	   /// association.</para>
	   /// 
	   /// <para>In most cases, <code>page</code> should be equal to <code>secret_number</code>
	   /// or <code>secret_number+8</code>, based on the association of secrets and page numbers.
	   /// Secrets are stored across pages 16 and 17 of the DS1963S memory.  A secret is 8 bytes, so
	   /// there are 8 secrets.  These 8 secrets are associated with the first 16 pages of memory.</para>
	   /// 
	   /// <para>On TINI, this method will be slightly faster if the secret's length is divisible by 47.
	   /// However, since secret key generation is a part of initialization, it is probably
	   /// not necessary.</para>
	   /// </summary>
	   /// <param name="page"> the page number used to write the partial secrets to </param>
	   /// <param name="secret"> the entire secret, in partial phrases, to be installed </param>
	   /// <param name="secret_number"> the secret 'page' to use (0 - 7)
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
	   /// <seealso cref= #bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual bool installMasterSecret(int page, sbyte[] secret, int secret_number)
	   {
		   lock (this)
		   {
        
			  //47 is a magic number here because every time a partial secret
			  //is to be computed, 32 bytes goes in the page and 15 goes in
			  //the scratchpad, so it's going to be easier in the computations
			  //if i know the input buffer length is divisible by 47
			  if (secret.Length == 0)
			  {
				 return false;
			  }
        
			  sbyte[] input_secret = null;
			  sbyte[] buffer = byte_buffer;
			  int secret_mod_length = secret.Length % 47;
        
			  if (secret_mod_length == 0) //if the length of the secret is divisible by 47
			  {
				 input_secret = secret;
			  }
			  else
			  {
        
				 /* i figure in the case where secret is not divisible by 47
				    it will be quicker to just create a new array once and
				    copy the data in, rather than on every partial secret
				    calculation do bounds checking */
				 input_secret = new sbyte [secret.Length + (47 - secret_mod_length)];
        
				 Array.Copy(secret, 0, input_secret, 0, secret.Length);
			  }
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Installing Secret on iButton");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("page: " + page);
				 IOHelper.writeLine("secret");
				 IOHelper.writeBytesHex(secret);
				 IOHelper.writeLine("input_secret");
				 IOHelper.writeBytesHex(input_secret);
				 IOHelper.writeLine("secret_number: " + (secret_number & 7));
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  //make sure the secret number is between 0 and 7
			  secret_number = secret_number & 7;
        
			  //the secret page is 16 for secrets 0-3, page 17 for secrets 4-7
			  int secret_page = (secret_number > 3) ? 17 : 16;
        
			  //each page has 4 secrets, so look at 2 LS bits
			  int secret_offset = (secret_number & 3) << 3;
			  int offset = 0; //the current offset into the input_secret buffer
			  sbyte[] sp_buffer = new sbyte [32];
        
			  while (offset < input_secret.Length)
			  {
				 if (!eraseScratchPad(page))
				 {
					return false;
				 }
        
				 if (!writeScratchPad(page, 0, input_secret, offset, 32))
				 {
					return false; //first write the whole page
				 }
        
				 if (readScratchPad(buffer, 0) < 0)
				 {
					return false; //get the address registers
				 }
        
				 if (!copyScratchPad())
				 {
					return false; //copy the page into memory
				 }
        
				 Array.Copy(input_secret, offset + 32, sp_buffer, 8, 15);
        
				 if (!writeScratchPad(page, 0, sp_buffer, 0, 32))
				 {
					return false; //now write the scratchpad data
				 }
        
				 if (!SHAFunction((offset == 0) ? COMPUTE_FIRST_SECRET : COMPUTE_NEXT_SECRET))
				 {
					return false; //means a failure
				 }
        
				 //here we have to write the scratchpad with 32 bytes of dummy data
				 if (!write_read_copy_quick(secret_page, secret_offset))
				 {
					return false;
				 }
        
				 offset += 47;
			  }
        
			  //now lets clean up - erase the scratchpad and data page
			  writeDataPage(page, FF);
        
			  /*
			  //This fails for some parts, due to "well-known" problem with
			  //erase scratchpad not setting up TA1,TA2 properly
			  eraseScratchPad(page);
			  readScratchPad(buffer, 0);
			  copyScratchPad();*/
        
			  return true;
		   }
	   }

	   //local cahce to make TINI fast
	   private sbyte[] bind_code_temp = new sbyte [32];
	   private sbyte[] bind_code_alt_temp = new sbyte [32];
	   private sbyte[] bind_data_temp = new sbyte [32];

	   /// <summary>
	   /// <para>Binds an installed secret to a DS1963S by using
	   /// well-known binding data and the DS1963S's unique
	   /// address.  This makes the secret unique
	   /// for this iButton.  Coprocessor iButtons use this method
	   /// to recreate the iButton's secret to verify authentication.
	   /// Roving iButtons use this method to finalize their secret keys.</para>
	   /// 
	   /// <para>Note that unlike in the <code>installMasterSecret()</code> method,
	   /// the page number does not need to be equivalent to the <code>secret_number</code>
	   /// modulo 8.  The new secret (installed secret + binding code) is generated
	   /// from this page but can be copied into another secret.  User iButtons should
	   /// bind to the same page the secret was installed on.  Coprocessor iButtons
	   /// must copy to a new secret to preserve the general system authentication
	   /// secret.</para>
	   /// 
	   /// <para>The binding should be either 7 bytes long or 15 bytes long.
	   /// A 15-length <code>byte</code> array is unaltered and placed in the scratchpad
	   /// for the binding.  A 7-length <code>byte</code> array is combined with the page
	   /// number and DS1963S unique address and then placed in the scratchpad.
	   /// Coprocessors should use a pre-formatted 15-length <code>byte</code> array.
	   /// User iButtons should let the method format for them (i.e.
	   /// use the 7-length <code>byte</code> array option).</para>
	   /// </summary>
	   /// <param name="page"> the page number that has the master secret already installed </param>
	   /// <param name="bind_data"> 32 bytes of binding data used to bind the iButton to the system </param>
	   /// <param name="bind_code"> the 7-byte or 15-byte binding code </param>
	   /// <param name="secret_number"> secret number to copy the resulting secret to
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
	   /// <seealso cref= #installMasterSecret(int,byte[],int) </seealso>
	   public virtual bool bindSecretToiButton(int page, sbyte[] bind_data, sbyte[] bind_code, int secret_number)
	   {
		   lock (this)
		   {
			  if (bind_data.Length != 32)
			  {
				 Array.Copy(bind_data, 0, bind_data_temp, 0, (bind_data.Length > 32 ? 32 : bind_data.Length));
        
				 bind_data = bind_data_temp;
			  }
        
			  if (bind_code.Length != 15)
			  {
				 if (bind_code.Length == 7)
				 {
					Array.Copy(bind_code, 0, bind_code_alt_temp, 0, 4);
        
					bind_code_alt_temp [4] = (sbyte) page;
        
					Array.Copy(address, 0, bind_code_alt_temp, 5, 7);
					Array.Copy(bind_code, 4, bind_code_alt_temp, 12, 3);
				 }
				 else
				 {
					Array.Copy(bind_code, 0, bind_code_alt_temp, 0, (bind_code.Length > 15 ? 15 : bind_code.Length));
				 }
        
				 bind_code = bind_code_alt_temp;
			  }
        
			  Array.Copy(bind_code, 0, bind_code_temp, 8, 15);
        
			  bind_code = bind_code_temp;
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Binding Secret to iButton");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("page: " + page);
				 IOHelper.writeLine("secret_number: " + (secret_number & 7));
				 IOHelper.writeLine("bind_data");
				 IOHelper.writeBytesHex(bind_data);
				 IOHelper.writeLine("bind_code");
				 IOHelper.writeBytesHex(bind_code);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  if (!writeDataPage(page, bind_data))
			  {
				 return false;
			  }
        
			  resume = true;
        
			  if (!writeScratchPad(page, 0, bind_code, 0, 32))
			  {
				 resume = false;
        
				 return false;
			  }
        
			  if (!SHAFunction(COMPUTE_NEXT_SECRET))
			  {
				 resume = false;
        
				 return false; //means a failure
			  }
        
			  //go ahead and set resume = false, but write_read_copy_quick doesn't
			  //check resume, it automatically assumes it can resume
			  resume = false;
        
			  //make sure the secret number is between 0 and 7
			  secret_number = secret_number & 7;
        
			  //the secret page is 16 for secrets 0-3, page 17 for secrets 4-7
			  int secret_page = (secret_number > 3) ? 17 : 16;
        
			  //each page has 4 secrets, so look at 2 LS bits
			  int secret_offset = (secret_number & 3) << 3;
        
			  return (write_read_copy_quick(secret_page, secret_offset));
		   }
	   }

	   //used when copying secrets from the scratchpad to a secret page
	   private bool write_read_copy_quick(int secret_page, int secret_offset)
	   {
		   lock (this)
		   {
        
			  //don't worry about doSpeed here, this should never be called before something else
			  //that would call doSpeed
			  int addr = (secret_page << 5) + secret_offset;
			  sbyte[] buffer = byte_buffer;
        
			  //assume we can resume
			  buffer [0] = (sbyte) RESUME;
			  buffer [1] = (sbyte) WRITE_SCRATCHPAD;
			  buffer [2] = (sbyte) addr; //(secret_page << 5);
			  buffer [3] = (sbyte)(addr >> 8); // secret_offset;
        
			  int length = 32 - secret_offset;
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("write_read_copy_quick");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("write scratchpad");
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)(addr >> 8));
				 IOHelper.writeLineHex((sbyte)addr);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  //write the scratchpad
			  adapter.reset();
			  Array.Copy(FF, 0, buffer, 4, length + 2);
			  adapter.dataBlock(buffer, 0, length + 6);
        
			  if (CRC16.compute(buffer, 1, length + 5, 0) != 0xB001)
			  {
				 return false;
			  }
        
			  //here we want to read the scratchpad WITHOUT reading the rest of the data
			  buffer [1] = (sbyte) READ_SCRATCHPAD;
        
			  Array.Copy(FF, 0, buffer, 2, 8);
			  adapter.reset();
			  adapter.dataBlock(buffer, 0, 5);
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("read scratchpad");
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)buffer[3]);
				 IOHelper.writeLineHex((sbyte)buffer[2]);
				 IOHelper.write("ES: 0x");
				 IOHelper.writeLineHex((sbyte)buffer[4]);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  //here we just shoot the buffer back out to call copyScratchpad
			  buffer [1] = COPY_SCRATCHPAD;
        
			  adapter.reset();
        
			  //adapter.dataBlock(buffer,0,5);
			  adapter.dataBlock(buffer, 0, 8);
        
			  if (buffer [7] == unchecked((sbyte) 0x0ff))
			  {
				 return waitForSuccessfulFinish();
			  }
			  else
			  {
				 return true;
			  }
		   }
	   }

	   /// <summary>
	   /// <para>Writes a data page to the DS1963S.  This method is the equivalent of calling:
	   /// <ul>
	   ///     <li><code>eraseScratchPad(0);                                     </code></li>
	   ///     <li><code>writeScratchPad(page_number,0,page_data_array,0,32);    </code></li>
	   ///     <li><code>readScratchPad(buffer,0);                               </code></li>
	   ///     <li><code>copyScratchPad();                                       </code></li>
	   /// </ul>
	   /// However, this method makes several optimizations to help it
	   /// run faster.  Because of the optimizations, this is the preferred
	   /// way of writing data to a normal memory page on the DS1963S.</para>
	   /// </summary>
	   /// <param name="page_number"> page number to write </param>
	   /// <param name="page_data"> page data to write (must be at least 32 bytes long)
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if the operation
	   ///         failed on a CRC error or while waiting for the DS1963S's output
	   ///         to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #eraseScratchPad(int) </seealso>
	   /// <seealso cref= #writeScratchPad(int,int,byte[],int,int) </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   public virtual bool writeDataPage(int page_number, sbyte[] page_data)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Writing Data Page to iButton");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("page_number: " + page_number);
				 IOHelper.writeLine("page_data");
				 IOHelper.writeBytesHex(page_data);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  //first we need to erase the scratchpad
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
        
			  buffer [1] = ERASE_SCRATCHPAD;
			  buffer [2] = (sbyte) 0;
			  buffer [3] = (sbyte) 0;
        
			  // send block (check copy indication complete)
			  Array.Copy(FF, 0, buffer, 4, 3);
			  adapter.dataBlock(buffer, 1, 6);
        
			  if (buffer [6] == unchecked((sbyte) 0x0ff))
			  {
				 if (!waitForSuccessfulFinish())
				 {
					return false;
				 }
			  }
        
			  //then we need to write the scratchpad
			  int addr = page_number << 5;
        
			  buffer [0] = (sbyte) RESUME;
			  buffer [1] = WRITE_SCRATCHPAD;
			  buffer [2] = (sbyte) addr;
			  buffer [3] = (sbyte)(addr >> 8);
        
			  Array.Copy(page_data, 0, buffer, 4, 32);
        
			  buffer [36] = unchecked((sbyte) 0xff);
			  buffer [37] = unchecked((sbyte) 0xff); //leave space for the CRC
        
			  adapter.reset();
        
			  //adapter.putByte(RESUME);
			  adapter.dataBlock(buffer, 0, 38);
        
			  if (CRC16.compute(buffer, 1, 37, 0) != 0xB001)
			  {
				 return false;
			  }
        
			  //then we need to do a 'read' to get out TA1,TA2,E/S
			  adapter.reset();
        
			  //buffer[0] = RESUME;
			  buffer [1] = READ_SCRATCHPAD;
        
			  Array.Copy(FF, 0, buffer, 2, 37);
			  adapter.dataBlock(buffer, 0, 39);
        
			  //match the scratchpad contents before copying
			  for (int i = 0; i < 32; i++)
			  {
				 if (page_data[i] != buffer[5 + i])
				 {
					return false;
				 }
			  }
        
			  //Don't skip this!
			  TA1 = buffer[2];
			  TA2 = buffer[3];
			  ES = buffer[4];
			  int length = 32 - (TA1 & 31);
        
			  if (CRC16.compute(buffer, 1, 6 + length, 0) != 0xB001)
			  {
				 return false;
			  }
        
			  //now we can copy the scratchpad
			  adapter.reset();
        
			  //buffer[0] still has the resume command in it
			  buffer [1] = COPY_SCRATCHPAD;
        
			  //TA1,TA2,ES area already in buffer
			  Array.Copy(FF, 0, buffer, 5, 3);
			  adapter.dataBlock(buffer, 0, 8);
        
			  if (buffer [7] == unchecked((sbyte) 0x0ff))
			  {
				 return waitForSuccessfulFinish();
			  }
        
			  return true;
		   }
	   }

	   /// <summary>
	   /// <para>Performs one of the DS1963S's cryptographic functions.  See the datasheet for
	   /// more information on these functions.</para>
	   /// 
	   /// Valid parameters for the <code>function</code> argument are:
	   /// <ul>
	   ///    <li> COMPUTE_FIRST_SECRET  </li>
	   ///    <li> COMPUTE_NEXT_SECRET   </li>
	   ///    <li> VALIDATE_DATA_PAGE    </li>
	   ///    <li> SIGN_DATA_PAGE        </li>
	   ///    <li> COMPUTE_CHALLENGE     </li>
	   ///    <li> AUTH_HOST             </li>
	   /// </ul>
	   /// <para>This method uses the last target address used by this
	   /// <code>OneWireContainer</code>.  These are read in the
	   /// <code>copyScratchPad()</code> and <code>readScratchPad()</code>
	   /// methods.</para>
	   /// </summary>
	   /// <param name="function"> the SHA function code
	   /// </param>
	   /// <returns> <code>true</code> if the function successfully completed,
	   ///         <code>false</code> if the operation failed on a CRC error
	   ///         or while waiting for the DS1963S's output to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAFunction(byte,int) </seealso>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   public virtual bool SHAFunction(sbyte function)
	   {
		  return SHAFunction(function, (TA1 & 0x0ff) | (TA2 << 8));
	   }

	   /// <summary>
	   /// <para>Performs one of the DS1963S's cryptographic functions.  See the datasheet for
	   /// more information on these functions.</para>
	   /// 
	   /// Valid parameters for the <code>function</code> argument are:
	   /// <ul>
	   ///    <li> COMPUTE_FIRST_SECRET  </li>
	   ///    <li> COMPUTE_NEXT_SECRET   </li>
	   ///    <li> VALIDATE_DATA_PAGE    </li>
	   ///    <li> SIGN_DATA_PAGE        </li>
	   ///    <li> COMPUTE_CHALLENGE     </li>
	   ///    <li> AUTH_HOST             </li>
	   /// </ul>
	   /// <para>This method uses the last target address used by this
	   /// <code>OneWireContainer</code>.  These are read in the
	   /// <code>copyScratchPad()</code> and <code>readScratchPad()</code>
	   /// methods.</para>
	   /// </summary>
	   /// <param name="function"> the SHA function code </param>
	   /// <param name="T"> the raw target address for the operation
	   /// </param>
	   /// <returns> <code>true</code> if the function successfully completed,
	   ///         <code>false</code> if the operation failed on a CRC error
	   ///         or while waiting for the DS1963S's output to alternate
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAFunction(byte) </seealso>
	   /// <seealso cref= #copyScratchPad() </seealso>
	   /// <seealso cref= #readScratchPad(byte[],int) </seealso>
	   public virtual bool SHAFunction(sbyte function, int T)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("SHA Function");
				 IOHelper.writeLine("address");
				 IOHelper.writeBytesHex(address);
				 IOHelper.writeLine("adapter.getSpeed()=" + adapter.Speed);
				 IOHelper.writeLine("adapter.getPortTypeDescription()=" + adapter.PortTypeDescription);
				 IOHelper.writeLine("this.speed=" + this.speed);
				 IOHelper.writeLine("device isPresent: " + adapter.isPresent(this.address));
				 IOHelper.write("function: 0x");
				 IOHelper.writeLineHex(function);
				 IOHelper.write("target address: 0x");
				 IOHelper.writeHex((sbyte)(T >> 8));
				 IOHelper.writeLineHex((sbyte)T);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  if (doSpeedEnable && (!resume))
			  {
				 doSpeed();
			  }
        
			  if (!resume)
			  {
				 adapter.select(address);
			  }
			  else
			  {
				 adapter.reset();
				 adapter.putByte(RESUME);
			  }
        
			  sbyte[] buffer = byte_buffer;
        
			  buffer [0] = COMPUTE_SHA;
			  buffer [1] = (sbyte) T;
			  buffer [2] = (sbyte)(T >> 8);
			  buffer [3] = function;
        
			  Array.Copy(FF, 0, buffer, 4, 5);
        
			  if (DEBUG)
			  {
				 IOHelper.writeLine("buffer:");
				 IOHelper.writeBytesHex(buffer,0,9);
			  }
        
			  adapter.dataBlock(buffer, 0, 9);
        
			  if (DEBUG)
			  {
				 IOHelper.writeLine("buffer:");
				 IOHelper.writeBytesHex(buffer,0,9);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (CRC16.compute(buffer, 0, 6, 0) != 0xB001)
			  {
				 return false;
			  }
        
			  if (buffer [8] == unchecked((sbyte) 0x0ff))
			  {
				 return waitForSuccessfulFinish();
			  }
        
			  return true;
		   }
	   }
	}




}