﻿using System.Collections;

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
	using CRC16 = com.dalsemi.onewire.utils.CRC16;
	using com.dalsemi.onewire.adapter;


	/// <summary>
	/// <P> 1-Wire&#174 container for a Dual Addressable Switch, DS2406 or DS2407.
	/// This container encapsulates the functionality of the 1-Wire family type <B>12</B> (hex).
	/// The DS2406 replaces the DS2407, but does not have hidden mode or user programmable
	/// power-on settings.</P>
	/// 
	/// <H3> Features </H3>
	/// <UL>
	///   <LI> Open drain PIO pin controlled through 1-Wire communication
	///   <li> 1024-bits of user programmable OTP EPROM
	///   <LI> Operating temperature range from -40&#176C to +85&#176C
	///   <li> On-chip CRC16 generator allows detection of data transfer errors
	///   <li> One or two channels with level sensing abilities
	///   <li> Supports activity sensing
	///   <li> Does not support 'Smart On' capabilities
	///   <li> TO-92 (1 channel) or TSOC (2 channel) packaging
	///   <li> Supports Conditional Search with user-selectable search options
	/// </UL>
	/// 
	/// <H3> Memory </H3>
	/// 
	/// <P> The memory can be accessed through the objects that are returned
	/// from the <seealso cref="#getMemoryBanks() getMemoryBanks"/> method. </P>
	/// 
	/// The following is a list of the MemoryBank instances that are returned:
	/// 
	/// <UL>
	///   <LI> <B> Main Memory </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank"/>
	///         <LI> <I> Size </I> 128 starting at physical address 0
	///         <LI> <I> Features</I> Write-once general-purpose non-volatile needs-program-pulse
	///         <LI> <I> Pages</I> 4 pages of length 32 bytes giving 29 bytes Packet data payload
	///         <LI> <I> Page Features </I> page-device-CRC pages-redirectable pages-lockable
	///         <li> <i> Extra information for each page</i>  Inverted redirection page, length 1
	///      </UL>
	///   <LI> <B> Write protect pages and Page redirection </B>
	///      <UL>
	///         <LI> <I> Implements </I> <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>,
	///                  <seealso cref="com.dalsemi.onewire.container.OTPMemoryBank OTPMemoryBank"/>
	///         <LI> <I> Size </I> 8 starting at physical address 0 (in STATUS memory area)
	///         <LI> <I> Features</I> Write-once not-general-purpose non-volatile needs-program-pulse
	///         <LI> <I> Pages</I> 1 pages of length 8 bytes
	///         <LI> <I> Page Features </I> page-device-CRC
	///      </UL>
	/// </UL>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// <para>The DS2406 supports level sensing and activity sensing.  The code below
	/// reports the flip-flop state, PIO level, and sensed activity while toggling
	/// every switch each time through the loop.  It toggles every switch it finds,
	/// regardless if the device has one or two switches.</para>
	/// 
	/// <code><pre>
	///      // "ID" is a byte array of size 8 with an address of a part we
	///      // have already found with family code 12 hex
	///      // "access" is a DSPortAdapter
	/// 
	///      OneWireContainer12 ds2406 = (OneWireContainer12) access.getDeviceContainer(ID);
	///      ds2406.setupContainer(access,ID);
	/// 
	///      byte[] state = ds2406.readDevice();
	///      int numchannels = ds2406.getNumberChannels(state);
	///      System.out.println("Number of Channels: "+numchannels);
	///      bool[] switches = new bool[numchannels];
	/// 
	///      ds2406.clearActivity();
	/// 
	///      for (int j=0;j<10;j++)
	///      {
	///          //clear the activity latches halfway through the test
	///          if (j==5)
	///              ds2406.clearActivity();
	///          state = ds2406.readDevice();
	/// 
	///          //first let's print out the status of all the latches
	///          for (int i=0;i &lt; numchannels;i++)
	///          {
	///              System.out.println("---------------------------------------------------------\r\n");
	///              System.out.println("                       CHANNEL "+i);
	///              System.out.println("---------------------------------------------------------\r\n");
	/// 
	///              System.out.println("           Latch state: "+ds2406.getLatchState(i,state));
	///              System.out.println("           Level      : "+ds2406.getLevel(i,state));
	///              System.out.println("           Activity   : "+ds2406.getSensedActivity(i, state));
	///              switches[i] = ds2406.getLatchState(i,state);
	///          }
	/// 
	///          //now lets toggle the switch flip-flop
	///          for (int i=0;i &lt; numchannels;i++)
	///          {
	///              ds2406.setLatchState(i,!switches[i],false,state);
	///          }
	///          ds2406.writeDevice(state);
	/// 
	///          Thread.sleep(500);
	///      }
	/// 
	/// </pre></code>
	/// 
	/// <para>Also see the usage example in the <seealso cref="com.dalsemi.onewire.container.SwitchContainer SwithContainer"/>
	/// interface.</para>
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
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2406.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2406.pdf for the DS2406</A>
	/// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2407.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2407.pdf for the DS2407</A>
	/// </DL>
	/// 
	/// Also see the <seealso cref="com.dalsemi.onewire.container.OneWireContainer05 DS2405"/>, a single addressable switch (OneWireContainer05).
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireSensor </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.SwitchContainer </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer05
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     KLA,DSS </seealso>
	public class OneWireContainer12 : OneWireContainer, SwitchContainer
	{

	   //--------
	   //-------- Static Final Variables
	   //--------

	   /// <summary>
	   /// Used to set the <code>source</code> to the activity latch for
	   /// conditional searches in the <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public static readonly sbyte SOURCE_ACTIVITY_LATCH = (sbyte) 0x02;

	   /// <summary>
	   /// Used to set the <code>source</code> to the flip-flop state for
	   /// conditional searches in the <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public static readonly sbyte SOURCE_FLIP_FLOP = (sbyte) 0x04;

	   /// <summary>
	   /// Used to set the <code>source</code> to the PIO status for
	   /// conditional searches in the <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public static readonly sbyte SOURCE_PIO = (sbyte) 0x06;


	   /// <summary>
	   /// Used to set the <code>polarity</code> to logical '0' for conditional search
	   /// checking in the <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte POLARITY_ZERO = 0x00;

	   /// <summary>
	   /// Used to set the <code>polarity</code> to logical '1' for conditional search
	   /// checking in the <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte POLARITY_ONE = 0x01;

	   /// <summary>
	   /// Used to select neither channel as the source for alarm conditions in the
	   /// <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte CHANNEL_NONE = 0x00;

	   /// <summary>
	   /// Used for options in the <code>setSearchConditions()</code> to
	   /// make sure the specified option is not changes from its current
	   /// value.
	   /// </summary>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public static readonly sbyte DONT_CHANGE = unchecked((sbyte)0x0ff);

	   /// <summary>
	   /// <code>channelMode</code> for the <code>channelAccess</code> method.
	   /// Selects Channel A (channel 0) for communication.  Also used to
	   /// select Channel A as the source for alarm conditions in the
	   /// <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte CHANNEL_A_ONLY = 0x04;

	   /// <summary>
	   /// <code>channelMode</code> for the <code>channelAccess</code> method.
	   /// Selects Channel B (channel 1) for communication.  Also used to
	   /// select Channel B as the source for alarm conditions in the
	   /// <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte CHANNEL_B_ONLY = 0x08;

	   /// <summary>
	   /// <code>channelMode</code> for the <code>channelAccess</code> method.
	   /// Selects both Channel A and B (channel 0 and 1) for communication.  Also used to
	   /// select both channels as the source for alarm conditions in the
	   /// <code>setSearchConditions()</code> method.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   /// <seealso cref= #setSearchConditions(byte,byte,byte,byte[]) </seealso>
	   public const sbyte CHANNEL_BOTH = 0x0c;

	   /// <summary>
	   /// <code>CRCMode</code> for the <code>channelAccess</code> method.
	   /// Requests no CRC generation by the DS2406/2407.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   public const sbyte CRC_DISABLE = 0x00;

	   /// <summary>
	   /// <code>CRCMode</code> for the <code>channelAccess</code> method.
	   /// Requests CRC generation after every byte transmitted.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   public const sbyte CRC_EVERY_BYTE = 0x01;

	   /// <summary>
	   /// <code>CRCMode</code> for the <code>channelAccess</code> method.
	   /// Requests CRC generation after every 8 bytes transmitted.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   public const sbyte CRC_EVERY_8_BYTES = 0x02;

	   /// <summary>
	   /// <code>CRCMode</code> for the <code>channelAccess</code> method.
	   /// Requests CRC generation after every 32 bytes transmitted.
	   /// </summary>
	   /// <seealso cref= #channelAccess(byte[],bool,bool,int,int,bool,bool) </seealso>
	   public const sbyte CRC_EVERY_32_BYTES = 0x03;



	   // privates !

	   /* DS2406 Write status command                                */
	   private const sbyte WRITE_STATUS_COMMAND = 0x55;
	   /* DS2406 channel access command                              */
	   private static readonly sbyte CHANNEL_ACCESS_COMMAND = unchecked((sbyte) 0xF5);
	   /* internal buffer  */
	   private sbyte[] buffer = new sbyte [7];
	   private bool clearactivity = false;
	   private bool doSpeedEnable = true;

	   //--------
	   //-------- Variables
	   //--------
	   //--------
	   //-------- Constructor
	   //--------

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS2406/2407.
	   /// Note that the method <code>setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[])</code>
	   /// must be called to set the correct <code>DSPortAdapter</code> device address.
	   /// </summary>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer#setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) setupContainer(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer12(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer12(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer12(DSPortAdapter,String) </seealso>
	   public OneWireContainer12() : base()
	   {
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS2406/2407.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS2406/2407
	   /// </param>
	   /// <seealso cref= #OneWireContainer12() </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer12(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer12(DSPortAdapter,String) </seealso>
	   public OneWireContainer12(DSPortAdapter sourceAdapter, sbyte[] newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS2406/2407.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS2406/2407
	   /// </param>
	   /// <seealso cref= #OneWireContainer12() </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer12(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer12(DSPortAdapter,String) </seealso>
	   public OneWireContainer12(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS2406/2407.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS2406/2407
	   /// </param>
	   /// <seealso cref= #OneWireContainer12() </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer12(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer12(com.dalsemi.onewire.adapter.DSPortAdapter,long)   OneWireContainer12(DSPortAdapter,long) </seealso>
	   public OneWireContainer12(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   //--------
	   //-------- Information Methods
	   //--------

	   /// <summary>
	   /// Gets the Dallas Semiconductor part number of the iButton
	   /// or 1-Wire Device as a <code>java.lang.String</code>.
	   /// For example "DS1992".
	   /// </summary>
	   /// <returns> iButton or 1-Wire device name </returns>
	   public override string Name
	   {
		   get
		   {
			  return "DS2406";
		   }
	   }

	   /// <summary>
	   /// Retrieves the alternate Dallas Semiconductor part numbers or names.
	   /// A 'family' of MicroLAN devices may have more than one part number
	   /// depending on packaging.  There can also be nicknames such as
	   /// "Crypto iButton".
	   /// </summary>
	   /// <returns>  the alternate names for this iButton or 1-Wire device </returns>
	   public override string AlternateNames
	   {
		   get
		   {
			  return "Dual Addressable Switch, DS2407";
		   }
	   }

	   /// <summary>
	   /// Gets a short description of the function of this iButton
	   /// or 1-Wire Device type.
	   /// </summary>
	   /// <returns> device description </returns>
	   public override string Description
	   {
		   get
		   {
			  return "1-Wire Dual Addressable Switch.  PIO pin channel " + "A sink capability of typical 50mA at 0.4V with " + "soft turn-on; optional channel B typical 10 mA at " + "0.4V.  1024 bits of Electrically Programmable " + "Read Only Memory (EPROM) partitioned into four 256 " + "bit pages.  7 bytes of user-programmable status " + "memory to control the device.";
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
			  ArrayList bank_vector = new ArrayList(2);
    
			  // EPROM main bank
			  MemoryBankEPROM mn = new MemoryBankEPROM(this);
    
			  mn.numberPages = 4;
			  mn.size = 128;
    
			  bank_vector.Add(mn);
    
			  // EPROM status write protect pages bank
			  MemoryBankEPROM st = new MemoryBankEPROM(this);
    
			  st.bankDescription = "Write protect pages, Page redirection, Switch control";
			  st.numberPages = 1;
			  st.size = 8;
			  st.pageLength = 8;
			  st.generalPurposeMemory = false;
			  st.extraInfo = false;
			  st.extraInfoLength = 0;
			  st.extraInfoDescription = null;
			  st.crcAfterAddress = false;
			  st.READ_PAGE_WITH_CRC = MemoryBankEPROM.STATUS_READ_PAGE_COMMAND;
			  st.WRITE_MEMORY_COMMAND = MemoryBankEPROM.STATUS_WRITE_COMMAND;
    
			  bank_vector.Add(st);
    
			  // setup OTP features in main memory
			  mn.mbLock = st;
			  mn.lockPage_Renamed = true;
			  mn.mbRedirect = st;
			  mn.redirectOffset = 1;
			  mn.redirectPage_Renamed = true;
    
			  return bank_vector.GetEnumerator();
		   }
	   }

	   //--------
	   //-------- Custom Methods for this 1-Wire Device Type
	   //--------

	   /// <summary>
	   /// Checks to see how the DS2406 is being supplied with power.
	   /// The 6-pin (2 channel) package of the DS2406 can be powered
	   /// by an outside source, but will still function on
	   /// parasite power only.
	   /// </summary>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <returns>  <code>true</code> if the device is getting supplied with
	   /// power and <code>false</code> if the device is parasite powered
	   /// </returns>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   public virtual bool isPowerSupplied(sbyte[] state)
	   {
		  return ((state [0] & 0x80) == 0x80);
	   }

	   /// <summary>
	   /// Gets the number of channels supported by this switch.
	   /// Channel specific methods will use a channel number specified
	   /// by an integer from [0 to (<code>getNumberChannels(byte[])</code> - 1)].  Note that
	   /// all devices of the same family will not necessarily have the
	   /// same number of channels.
	   /// </summary>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <returns> the number of channels for this device
	   /// </returns>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   public virtual int getNumberChannels(sbyte[] state)
	   {
		  return ((state [0] & 0x40) == 0x40) ? 2 : 1;
	   }

	   /// <summary>
	   /// Checks if the channels of this switch are 'high side'
	   /// switches.  This indicates that when 'on' or <code>true</code>, the switch output is
	   /// connect to the 1-Wire data.  If this method returns  <code>false</code>
	   /// then when the switch is 'on' or <code>true</code>, the switch is connected
	   /// to ground.
	   /// </summary>
	   /// <returns> <code>true</code> if the switch is a 'high side' switch,
	   ///         <code>false</code> if the switch is a 'low side' switch
	   /// </returns>
	   /// <seealso cref= #getLatchState(int,byte[]) </seealso>
	   public virtual bool HighSideSwitch
	   {
		   get
		   {
			  return false;
		   }
	   }

	   /// <summary>
	   /// Checks if the channels of this switch support
	   /// activity sensing.  If this method returns <code>true</code> then the
	   /// method <code>getSensedActivity(int,byte[])</code> can be used.
	   /// </summary>
	   /// <returns> <code>true</code> if channels support activity sensing
	   /// </returns>
	   /// <seealso cref= #getSensedActivity(int,byte[]) </seealso>
	   /// <seealso cref= #clearActivity() </seealso>
	   public virtual bool hasActivitySensing()
	   {
		  return true;
	   }

	   /// <summary>
	   /// Checks if the channels of this switch support
	   /// level sensing.  If this method returns <code>true</code> then the
	   /// method <code>getLevel(int,byte[])</code> can be used.
	   /// </summary>
	   /// <returns> <code>true</code> if channels support level sensing
	   /// </returns>
	   /// <seealso cref= #getLevel(int,byte[]) </seealso>
	   public virtual bool hasLevelSensing()
	   {
		  return true;
	   }

	   /// <summary>
	   /// Checks if the channels of this switch support
	   /// 'smart on'. Smart on is the ability to turn on a channel
	   /// such that only 1-Wire device on this channel are awake
	   /// and ready to do an operation.  This greatly reduces
	   /// the time to discover the device down a branch.
	   /// If this method returns <code>true</code> then the
	   /// method <code>setLatchState(int,bool,bool,byte[])</code>
	   /// can be used with the <code>doSmart</code> parameter <code>true</code>.
	   /// </summary>
	   /// <returns> <code>true</code> if channels support 'smart on'
	   /// </returns>
	   /// <seealso cref= #setLatchState(int,bool,bool,byte[]) </seealso>
	   public virtual bool hasSmartOn()
	   {
		  return false;
	   }

	   /// <summary>
	   /// Checks if the channels of this switch require that only one
	   /// channel is on at any one time.  If this method returns <code>true</code> then the
	   /// method <code>setLatchState(int,bool,bool,byte[])</code>
	   /// will not only affect the state of the given
	   /// channel but may affect the state of the other channels as well
	   /// to insure that only one channel is on at a time.
	   /// </summary>
	   /// <returns> <code>true</code> if only one channel can be on at a time.
	   /// </returns>
	   /// <seealso cref= #setLatchState(int,bool,bool,byte[]) </seealso>
	   public virtual bool onlySingleChannelOn()
	   {
		  return false;
	   }

	   //--------
	   //-------- Switch 'get' Methods
	   //--------

	   /// <summary>
	   /// Checks the sensed level on the indicated channel.
	   /// To avoid an exception, verify that this switch
	   /// has level sensing with the  <code>hasLevelSensing()</code>.
	   /// Level sensing means that the device can sense the logic
	   /// level on its PIO pin.
	   /// </summary>
	   /// <param name="channel"> channel to execute this operation, in the range [0 to (<code>getNumberChannels(byte[])</code> - 1)] </param>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <returns> <code>true</code> if level sensed is 'high' and <code>false</code> if level sensed is 'low'
	   /// </returns>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   /// <seealso cref= #hasLevelSensing() </seealso>
	   public virtual bool getLevel(int channel, sbyte[] state)
	   {
		  if (channel == 0)
		  {
			 return ((state [0] & 0x04) == 0x04);
		  }
		  else
		  {
			 return ((state [0] & 0x08) == 0x08);
		  }
	   }

	   /// <summary>
	   /// Checks the latch state of the indicated channel.
	   /// </summary>
	   /// <param name="channel"> channel to execute this operation, in the range [0 to (<code>getNumberChannels(byte[])</code> - 1)] </param>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <returns> <code>true</code> if channel latch is 'on'
	   /// or conducting and <code>false</code> if channel latch is 'off' and not
	   /// conducting.  Note that the actual output when the latch is 'on'
	   /// is returned from the <code>isHighSideSwitch()</code> method.
	   /// </returns>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   /// <seealso cref= #isHighSideSwitch() </seealso>
	   /// <seealso cref= #setLatchState(int,bool,bool,byte[]) </seealso>
	   public virtual bool getLatchState(int channel, sbyte[] state)
	   {
		  if (channel == 0)
		  {
			 return ((state [1] & 0x20) != 0x20);
		  }
		  else
		  {
			 return ((state [1] & 0x40) != 0x40);
		  }
	   }

	   /// <summary>
	   /// Checks if the indicated channel has experienced activity.
	   /// This occurs when the level on the PIO pins changes.  To clear
	   /// the activity that is reported, call <code>clearActivity()</code>.
	   /// To avoid an exception, verify that this device supports activity
	   /// sensing by calling the method <code>hasActivitySensing()</code>.
	   /// </summary>
	   /// <param name="channel"> channel to execute this operation, in the range [0 to (<code>getNumberChannels(byte[])</code> - 1)] </param>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <returns> <code>true</code> if activity was detected and <code>false</code> if no activity was detected
	   /// </returns>
	   /// <seealso cref= #hasActivitySensing() </seealso>
	   /// <seealso cref= #clearActivity() </seealso>
	   public virtual bool getSensedActivity(int channel, sbyte[] state)
	   {
		  if (channel == 0)
		  {
			 return ((state [0] & 0x10) == 0x10);
		  }
		  else
		  {
			 return ((state [0] & 0x20) == 0x20);
		  }
	   }

	   /// <summary>
	   /// <para>Clears the activity latches the next time possible.  For
	   /// example, on a DS2406/07, this happens the next time the
	   /// status is read with <code>readDevice()</code>.</para>
	   /// 
	   /// <para>The activity latches will only be cleared once.  With the
	   /// DS2406/07, this means that only the first call to 
	   /// <code>readDevice()</code> will clear the activity latches.  
	   /// Subsequent calls to <code>readDevice()</code> will leave the
	   /// activity latch states intact, unless this method has been 
	   /// invoked since the last call to <code>readDevice()</code>.</para>
	   /// </summary>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   /// <seealso cref= #getSensedActivity(int,byte[]) </seealso>
	   public virtual void clearActivity()
	   {
		  lock (this)
		  {
			 clearactivity = true;
		  }
	   }

	   //--------
	   //-------- Switch 'set' Methods
	   //--------

	   /// <summary>
	   /// Sets the latch state of the indicated channel.
	   /// The method <code>writeDevice()</code> must be called to finalize
	   /// changes to the device.  Note that multiple 'set' methods can
	   /// be called before one call to <code>writeDevice()</code>.
	   /// </summary>
	   /// <param name="channel"> channel to execute this operation, in the range [0 to (<code>getNumberChannels(byte[])</code> - 1)] </param>
	   /// <param name="latchState"> <code>true</code> to set the channel latch 'on'
	   ///     (conducting) and <code>false</code> to set the channel latch 'off' (not
	   ///     conducting).  Note that the actual output when the latch is 'on'
	   ///     is returned from the <code>isHighSideSwitch()</code> method. </param>
	   /// <param name="doSmart"> If latchState is 'on'/<code>true</code> then doSmart indicates
	   ///                  if a 'smart on' is to be done.  To avoid an exception
	   ///                  check the capabilities of this device using the
	   ///                  <code>hasSmartOn()</code> method. </param>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <seealso cref= #hasSmartOn() </seealso>
	   /// <seealso cref= #getLatchState(int,byte[]) </seealso>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[]) </seealso>
	   public virtual void setLatchState(int channel, bool latchState, bool doSmart, sbyte[] state)
	   {
		  if (channel == 0)
		  {
			 state[1] &= unchecked((sbyte)0xdf);
			 if (!latchState)
			 {
				state [1] = (sbyte)(state [1] | 0x20);
			 }
		  }
		  else
		  {
			 state[1] &= unchecked((sbyte)0xbf);
			 if (!latchState)
			 {
				state [1] = (sbyte)(state [1] | 0x40);
			 }
		  }
	   }

	   /// <summary>
	   /// Retrieves the 1-Wire device sensor state.  This state is
	   /// returned as a byte array.  Pass this byte array to the 'get'
	   /// and 'set' methods.  If the device state needs to be changed then call
	   /// the 'writeDevice' to finalize the changes.
	   /// </summary>
	   /// <returns> 1-Wire device sensor state
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual sbyte[] readDevice()
	   {
		  sbyte[] state = new sbyte [2];

		  //the first byte is the raw status
		  //the second byte is for writing
		  //this is a strange solution because
		  //the status we are interested in reading does not
		  //look the same as the status we are interested in writing
		  lock (this)
		  {
			 if (doSpeedEnable)
			 {
				doSpeed();
			 }

			 // select the device
			 if (adapter.select(address))
			 {

				// channel access command
				buffer [0] = CHANNEL_ACCESS_COMMAND;

				// send the control bytes
				if (clearactivity)
				{
				   buffer [1] = unchecked((sbyte) 0xD5);
				   clearactivity = false;
				}
				else
				{
				   buffer [1] = (sbyte) 0x55;
				}

				buffer [2] = unchecked((sbyte) 0xFF);

				// read the info, dummy and CRC16
				for (int i = 3; i < 7; i++)
				{
				   buffer [i] = unchecked((sbyte) 0xFF);
				}

				// send the block
				adapter.dataBlock(buffer, 0, 7);

				// calculate the CRC16 on the result and check if correct
				if (CRC16.compute(buffer, 0, 7, 0) == 0xB001)
				{
				   state [0] = buffer [3];

				   //let's read the status byte 7 and get the data there
				   buffer[0] = unchecked((sbyte)0x0aa); //READ_STATUS
				   buffer[1] = 7; //address to read
				   buffer[2] = 0;
				   for (int i = 3;i < 6;i++) //plus room for the CRC
				   {
					   buffer[i] = unchecked((sbyte)0x0ff);
				   }
				   adapter.reset();
				   adapter.select(address);
				   adapter.dataBlock(buffer,0,6);
				   if (CRC16.compute(buffer, 0, 6, 0) == 0xB001)
				   {
					   state[1] = buffer[3];
					   return state;
				   }
				}
			 }
		  } //end synch block

		  // device must not have been present
		  throw new OneWireIOException("OneWireContainer12-device not present");
	   }

	   /// <summary>
	   /// Writes the 1-Wire device sensor state that
	   /// have been changed by 'set' methods.  Only the state registers that
	   /// changed are updated.  This is done by referencing a field information
	   /// appended to the state data.
	   /// </summary>
	   /// <param name="state"> 1-Wire device sensor state
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual void writeDevice(sbyte[] state)
	   {
		  if (doSpeedEnable)
		  {
			 doSpeed();
		  }

		  if (adapter.select(address))
		  {
			 lock (this)
			 {

				// create a block to set the switch state
				// read memory and counter command
				// write status command
				buffer [0] = WRITE_STATUS_COMMAND;

				// address of switch state in status
				buffer [1] = 0x07;
				buffer [2] = 0x00;

				// write state
				buffer [3] = (sbyte) state [1];

				// read CRC16
				buffer [4] = unchecked((sbyte) 0xFF);
				buffer [5] = unchecked((sbyte) 0xFF);

				// send the block
				adapter.dataBlock(buffer, 0, 6);

				// calculate the CRC16 on the result and check if correct
				if (CRC16.compute(buffer, 0, 6, 0) == 0xB001)
				{
				   return;
				}
			 }
		  }

		  // device must not have been present
		  throw new OneWireException("OneWireContainer12-device not present");
	   }


	   /// <summary>
	   /// Directs the container to avoid the calls to doSpeed() in methods that communicate
	   /// with the Thermocron. To ensure that all parts can talk to the 1-Wire bus
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
		/// <para>Programs the Conditional Search options for the DS2406/2407.</para>
		/// 
		/// <para>The DS2406/2407 supports Conditional Searches with
		/// user programmable search conditions.  This means that
		/// the part can alarm on several kinds of conditions,
		/// programmable by the user.</para>
		/// 
		/// <para>The user can select a channel and a source to compare to
		/// a polarity.  If the source's logical value is equal to the
		/// polarity, the device alarms (responds to a Conditional Search).
		/// For instance, if <code>channel</code> is <code>CHANNEL_A</code>,
		/// <code>source</code> is <code>SOURCE_ACTIVITY_LATCH</code>,
		/// and <code>polarity</code> is <code>POLARITY_ONE</code>, then the
		/// device will respond to a Conditional Search when the activity
		/// latch on channel A is 1 (when activity has been detected on
		/// channel A).  When <code>channel</code> is <code>CHANNEL_BOTH</code>,
		/// the selected source signals are ORed for comparison with the polarity.
		/// When <code>channel</code> is <code>CHANNEL_NONE</code>,  the selected
		/// source signal is considered a logical '0'.  In other words, if
		/// <code>channel</code> is <code>CHANNEL_NONE</code>, if <code>polarity</code>
		/// is <code>POLARITY_ZERO</code>, the device always responds to a
		/// Conditional Search.  If <code>polarity</code> is <code>POLARITY_ONE</code>,
		/// the device never responds to a Conditional Search.</para>
		/// 
		/// <para>Note that for any of these options, the value <code>DONT_CHANGE</code>
		/// will insure that the value previously used by the DS2406/2407 will
		/// not be altered.</para>
		/// 
		/// <para>The method <code>writeDevice()</code> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <code>writeDevice()</code>.</para>
		/// 
		/// <para>Also note that the Hidden Mode of the DS2407 is not supported in this
		/// implementation as an option for source selection.  Hidden Mode was
		/// phased out for the newer DS2406.  See the datasheet for the DS2407
		/// for more information on Hidden Mode.</para>
		/// </summary>
		/// <param name="channel"> the channel of interest for the source of the conditional check
		/// (valid values are <code>CHANNEL_NONE</code>, <code>CHANNEL_A_ONLY</code>, <code>CHANNEL_B_ONLY</code>, <code>CHANNEL_BOTH</code>, and <code>DONT_CHANGE</code>) </param>
		/// <param name="source"> the source selection for the conditional check
		/// (valid values are <code>SOURCE_ACTIVITY_LATCH</code>, <code>SOURCE_FLIP_FLOP</code>, <code>SOURCE_PIO</code>, and <code>DONT_CHANGE</code>) </param>
		/// <param name="polarity"> the polarity selection for the conditional check
		/// (valid values are <code>POLARITY_ZERO</code>, <code>POLARITY_ONE</code>, and <code>DONT_CHANGE</code>) </param>
		/// <param name="state"> current state of the device returned from <code>readDevice()</code>
		/// </param>
		/// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
		/// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[]) </seealso>
		/// <seealso cref= #CHANNEL_NONE </seealso>
		/// <seealso cref= #CHANNEL_A_ONLY </seealso>
		/// <seealso cref= #CHANNEL_B_ONLY </seealso>
		/// <seealso cref= #CHANNEL_BOTH </seealso>
		/// <seealso cref= #SOURCE_ACTIVITY_LATCH </seealso>
		/// <seealso cref= #SOURCE_PIO </seealso>
		/// <seealso cref= #SOURCE_FLIP_FLOP </seealso>
		/// <seealso cref= #POLARITY_ONE </seealso>
		/// <seealso cref= #POLARITY_ZERO </seealso>
		/// <seealso cref= #DONT_CHANGE </seealso>
		public virtual void setSearchConditions(sbyte channel, sbyte source, sbyte polarity, sbyte[] state)
		{
			//state[1] bitmap
			// SUP PIOB PIOA CSS4 CSS3 CSS2 CSS1  CSS0
			//               [channel] [source ][polarity]
			//so channel needs to be shifted left once, other's can be or-ed in
			sbyte newstate = 0;
			if (channel != DONT_CHANGE)
			{
				newstate = (sbyte)(channel << 1);
			}
			if (source != DONT_CHANGE)
			{
				newstate |= source;
			}
			if (polarity != DONT_CHANGE)
			{
				newstate |= polarity;
			}
			state[1] = unchecked((sbyte)(state[1] & 0xe0));
			state[1] |= newstate;

		}


	   /// <summary>
	   /// <para>Accesses the PIO channels to sense the logical status of
	   /// the output node.  This method supports all the modes of
	   /// communication with the part as described in the datasheet for
	   /// the DS2406/2407.</para>
	   /// </summary>
	   /// <param name="inbuffer"> The input buffer.  Depending on the other options chosen
	   /// to this method, this will contain data to be written to
	   /// the channels or it will hold space for data that will be read.
	   /// </param>
	   /// <param name="toggleRW"> By selecting <code>toggleRW</code> to be
	   /// <code>true</code>, the part will alternately
	   /// read and write bytes from and to this channel.  Setting
	   /// <code>toggleRW</code> to <code>false</code> means
	   /// that only one operation will occur, whichever operation
	   /// is selected by <code>readInitially</code>. <br> <i> <b> NOTE: </b> 
	   /// When toggleRW is <code>true</code> the 'read' bytes are
	   /// automatically provided and only the results of the read
	   /// bytes are returned. </i>
	   /// </param>
	   /// <param name="readInitially"> If <code>readInitially</code> is
	   /// <code>true</code>, the first operation to occur will
	   /// be a read, else it will be a write.  If <code>toggleRW</code>
	   /// is <code>false</code>, the operation chosen by this flag
	   /// is the only operation that will occur.  If <code>toggleRW</code>
	   /// is <code>true</code>, this operation is the one
	   /// that will occur first, then the other will occur.  For example,
	   /// if <code>toggleRW</code> is <code>true</code> and
	   /// <code>readInitially</code> is <code>false</code> (and you only
	   /// have one channel communication), 8 bits will be written to channel
	   /// A and then 8 bits will be read from channel A.
	   /// </param>
	   /// <param name="CRCMode"> The 2406/7 supports 4 CRC generation modes for error detection
	   /// when performing channel access.  This argument should have one
	   /// of the following values:
	   /// <pre><code>
	   ///     CRC_DISABLE        Never generate a CRC
	   ///     CRC_EVERY_BYTE     Generate a CRC after every byte transmission.
	   ///     CRC_EVERY_8_BYTES  Generate a CRC after every 8 bytes.
	   ///     CRC_EVERY_32_BYTES Generate a CRC after every 32 bytes.
	   /// </code></pre>
	   /// Invalid values will be masked to valid values.  The CRC is 16 bits,
	   /// and does not get passed back with the output.  This method returns
	   /// <code>null</code> on a CRC failure.
	   /// </param>
	   /// <param name="channelMode"> The 2406/7 supports 3 modes of channel communication.  This
	   /// argument should take one of the following values:
	   /// <pre><code>
	   ///     CHANNEL_A_ONLY  Only communicate with PIO A
	   ///     CHANNEL_B_ONLY  Only communicate with PIO B
	   ///     CHANNEL_BOTH    Communicate with both PIO's
	   /// </code></pre>
	   /// If <code>CHANNEL_BOTH</code> is selected, data is written and read
	   /// from the input buffer to the two channels.
	   /// See the datasheet for a description of operation in this
	   /// mode.  If communicating with both channels, it is up to the
	   /// caller to format the data correctly in the input buffer
	   /// so the correct channel gets the correct data.  Similarly,
	   /// any return data must be parsed by the user.
	   /// </param>
	   /// <param name="clearActivity"> <code>true</code> to reset the activity latch
	   /// </param>
	   /// <param name="interleave"> The value for the Interleave Control bit.
	   /// If <code>true</code>, operates in synchronous mode.
	   /// If <code>false</code>, operates in asynchronous mode.
	   /// See the datasheet for a discussion of asynchronous and
	   /// synchronous mode.  This argument only matters if communicating
	   /// with both channels.
	   /// </param>
	   /// <returns> If any bytes were read, this returns a byte array of data
	   /// read from the channel access.  If no bytes were read, it
	   /// will return the input buffer that was to be written.  
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #CHANNEL_A_ONLY </seealso>
	   /// <seealso cref= #CHANNEL_B_ONLY </seealso>
	   /// <seealso cref= #CHANNEL_BOTH </seealso>
	   /// <seealso cref= #CRC_DISABLE </seealso>
	   /// <seealso cref= #CRC_EVERY_BYTE </seealso>
	   /// <seealso cref= #CRC_EVERY_8_BYTES </seealso>
	   /// <seealso cref= #CRC_EVERY_32_BYTES </seealso>
	   public virtual sbyte[] channelAccess(sbyte[] inbuffer, bool toggleRW, bool readInitially, int CRCMode, int channelMode, bool clearActivity, bool interleave)
	   {
		  CRCMode = CRCMode & 0x03; //MASK THIS TO ACCEPTABLE VALUE
		  channelMode = channelMode & 0x0c; //MASK THIS TO ACCEPTABLE VALUE

		  if (channelMode == 0)
		  {
			 channelMode = 0x04; //CHANNELMODE CANNOT BE 0
		  }

		  if (interleave && (channelMode != CHANNEL_BOTH)) //CANNOT INTERLEAVE WITH ONLY 1 CHANNEL
		  {
			 interleave = false;
		  }

		  if (doSpeedEnable)
		  {
			 doSpeed();
		  }

		  if (adapter.select(address))
		  {
			 int crc16;
			 int i;

			 //now figure out how many bytes my output buffer needs to be
			 int inlength = inbuffer.Length;

			 if (toggleRW)
			 {
				inlength = (inlength << 1); //= inlength * 2
			 }

			 switch (CRCMode)
			 {
				default:
					goto case CRC_EVERY_BYTE;
				case CRC_EVERY_BYTE: //we need to allow for 2 CRC bytes for every byte of the length
				   inlength = inlength * 3; //length + 2*length
				   break;
				case CRC_EVERY_8_BYTES: //we need to allow for 2 CRC bytes for every 8 bytes of length
				   inlength = inlength + ((inlength >> 3) << 1); //(length DIV 8) * 2
				   break;
				case CRC_EVERY_32_BYTES: //we need to allow for 2 CRC bytes for every 32 bytes of length
				   inlength = inlength + ((inlength >> 5) << 1); //(length DIV 32) * 2
				   break;
			 }

			 sbyte[] outputbuffer = new sbyte [inlength + 3 + 1]; //3 control bytes + 1 information byte

			 outputbuffer [0] = CHANNEL_ACCESS_COMMAND;
			 crc16 = CRC16.compute(CHANNEL_ACCESS_COMMAND & 0x0FF);

			 // send the control bytes
			 outputbuffer [1] = (sbyte)(CRCMode | channelMode | (clearActivity ? 0x80 : 0x00) | (interleave ? 0x10 : 0x00) | (toggleRW ? 0x20 : 0x00) | (readInitially ? 0x40 : 0x00));
			 outputbuffer [2] = unchecked((sbyte) 0xFF);
			 crc16 = CRC16.compute(outputbuffer, 1, 2, crc16);

			 for (i = 3; i < outputbuffer.Length; i++)
			 {
				outputbuffer [i] = unchecked((sbyte) 0xff);
			 }

			 //now for the hard part: putting the right outputbuffer into the array
			 //first lets see if we can skip this stage, ie on just a read

			 /*
			        At this point we have 16 options:
			        Initial  Toggle  CRC   Description
			     0   write    off     0    Only write these bytes, CRC disabled
			     1   write    off     1    Write these bytes, CRC for every byte
			     2   write    off     8    Write these bytes, CRC for every 8 bytes
			     3   write    off     32   Write these bytes, CRC for every 32 bytes
			     4   write    on      0    Write a byte, read a byte, no CRC
			     5   write    on      1    Write a byte, CRC, read a byte, CRC
			     6   write    on      8    Write a byte, read a byte X 4 then a CRC
			     7   write    on      32   Write a byte, read a byte X 16 then a CRC
			     8   read     off     0    Read this many bytes, CRC disabled
			     9   read     off     1    Read this many bytes, CRC for every byte
			     a   read     off     8    Read this many bytes, CRC for every 8 bytes
			     b   read     off     32   Read this many bytes, CRC for every 32 bytes
			     c   read     on      0    Read a byte, write a byte, no CRC
			     d   read     on      1    Read a byte, CRC, write a byte, CRC
			     e   read     on      8    Read a byte, write a byte X 4 then a CRC
			     f   read     on      32   Read a byte, write a byte X 16 then a CRC
	
			     Options 0-3 require that we space the input buffer for the CRCs.
			     Options 8-b require no extra work, since we have already loaded the buffer with FF's for reads.
			     Options 4 and c require that we interleave the write bytes and the read FF's
			     Options 5 and d require that we interleace write byte, CRC space, read byte, CRC space
			     Other options are really messy
	
			     ...Brain
			  */
			 int j = 4; //outputbuffer 0-2 is command bytes, outputbuffer[3] is return info
			 int option = outputbuffer [1] & 0x63; //get the bits out we want for toggle, initial, and CRC

			 option = ((option >> 3) | option) & 0x0f; //now lets make it a number 0-15

			 /*switch (option)
			 {
			     case 0    :
			     case 1    :
			     case 2    :
			     case 3    : for (i=0;i<inbuffer.length;i++)
			                 {
			                    outputbuffer[j] = inbuffer[i];
			                    j = j + fixJ(i+1,option);
			                 }
			                 break;
			     case 4    :
			     case 5    :
			     case 6    :
			     case 7    : for (i=0;i<inbuffer.length;i++)
			                 {
			                     outputbuffer[j] = inbuffer[i];
			                     j = j + fixJ((i*2)+1,option);
			                     //then we plug in a read space
			                     j = j + fixJ((i*2)+2,option);
			                 }
			                 break;
			     case 8    :
			     case 9    :
			     case 0x0a :
			     case 0x0b :
			                 break;  //nothing needs to be done
			     case 0x0c :
			     case 0x0d :
			     case 0x0e :
			     case 0x0f : for (i=0;i<inbuffer.length;i++)
			                 {
			                     //first we plug in a read space
			                     j = j + fixJ((i*2)+1,option);
			                     outputbuffer[j] = inbuffer[i];
			                     j = j + fixJ((i*2)+2,option);
			                 }
			                 break;
			 }*/

			 /* this next section of code replaces the previous section to reduce redundant code.
			    here we are formatting the output buffer so it has FF's in the right places
			    for reading the CRC's and reading the data from the channels.  the previous code
			    is left because it makes a little more sense in that form. at least i think so.
	
			    ...Pinky
			 */
			 if ((option < 8) || (option > 0x0b)) //if this is not a read-only (which we need do nothing for)
			 {
				for (i = 0; i < inbuffer.Length; i++)
				{
				   if (option > 0x0b) //then we are reading first
				   {
					  j = j + fixJ((i * 2) + 1, option); //  leave a space for a read, and the CRC if need be
				   }

				   outputbuffer [j] = inbuffer [i]; //write this data

				   if (option < 0x04) //if this is only a write
				   {
					  j = j + fixJ(i + 1, option); //  leave a space for CRC if needed, else just increment
				   }
				   else //else we are toggling
				   {
					  if (option < 0x08) //this is a write-first toggle
					  {
						 j = j + fixJ((i * 2) + 1, option); //   so leave a space for a read
					  }

					  j = j + fixJ((i * 2) + 2, option); //now leave a space for the CRC
				   }
				}
			 }

			 // now our output buffer should be set correctly
			 // send the block Pinky!
			 adapter.dataBlock(outputbuffer, 0, outputbuffer.Length);

			 // calculate the CRC16 within the resulting buffer for integrity
			 //start at offset 3 for the information byte
			 crc16 = CRC16.compute(outputbuffer [3], crc16);
			 j = 0; //j will be how many bytes we are into the buffer - CRC bytes read

			 int k = 0; //index into the return buffer
			 bool fresh = false; //whether or not we need to reinitialize the CRC calculation
			 sbyte[] returnbuffer = new sbyte [inbuffer.Length];

			 for (i = 4; i < outputbuffer.Length; i++)
			 {
				if (CRCMode != CRC_DISABLE)
				{
				   if (fresh)
				   {
					  crc16 = CRC16.compute(outputbuffer [i]);
					  fresh = false;
				   }
				   else
				   {
					  crc16 = CRC16.compute(outputbuffer [i], crc16);
				   }
				}

				if ((!toggleRW && readInitially) || (toggleRW && readInitially && ((j & 0x01) == 0x00)) || (toggleRW && !readInitially && ((j & 0x01) == 0x01)))
				{
				   returnbuffer [k] = outputbuffer [i];

				   k++;
				}

				j++;

				if ((fixJ(j, option) > 1) && (CRCMode != CRC_DISABLE)) //means that we should look for a CRC
				{
				   crc16 = CRC16.compute(outputbuffer, i + 1, 2, crc16);
				   i += 2;

				   if (crc16 != 0xb001)
				   {
					  throw new OneWireIOException("Invalid CRC");
				   }

				   fresh = true;
				}
			 }

			 //now that we got the right bytes out of the array
			 return returnbuffer;
		  }

		  // device must not have been present
		  throw new OneWireIOException("OneWireContainer12-device not present");
	   }

	   //--------
	   //-------- Private
	   //--------

	   /*
	    * This method returns how much we should increment the index variable into
	    * our output buffer.  should be called after every setting of a value.
	    *
	    * @param current_index  current index into the channel access array
	    * @param option_mask    contains data on CRC generation
	    *
	    * @return amount to increment the index variable
	    */
	   private int fixJ(int current_index, int option_mask)
	   {
		  //assume that current_index started at 0, but this function is never called at 0
		  switch (option_mask & 0x03)
		  {
			 case 0x00 :
				return 1; //no crc
			 case 0x01 :
				return 3; //2-byte CRC after every byte
			 default : //must be 0x02 (after 8 bytes) or 0x03 (after 32 bytes)
				if ((current_index & (8 + (24 * (option_mask & 0x01)) - 1)) == 0)
				{
				   return 3;
				}
                break;

			 /* OK let me explain that last piece of code:
			    The only return values are going to be 1 and 3, 1 for a normal increment
			    and 3 if we want to leave space to recieve a CRC.
	
			    So the mask gets the bits out that are concerned with the CRC.  When its 0 it
			    means that the CRC is disabled, so the next location into our destination
			    array we need to copy into is just the next available location.
	
			    When it is 1, it means we will recieve a CRC after each transmission, so
			    we should leave a 2 byte space for it (thus increament by 3).
	
			    When it is 2, it means that after every 8 bytes we want to recieve a CRC
			    byte pair.  When it is a 3, it means that every 32 bytes we want the CRC
			    pair.  So what we want to check is if the current_index is divisible by 8
			    or 32 (we do not call this method with current_index==0).  Since 8 and 32
			    are powers of 2 we do it with the '&' operator and 7 or 31 as the other
			    value (2^n - 1).  The (8+(24 * option_mask&0x01)) bit just returns me 8
			    or 32.
			 */
		  }

		  return 1;
	   }
	}

}