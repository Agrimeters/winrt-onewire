﻿using System;
using System.Collections;

/*---------------------------------------------------------------------------
 * Copyright (C) 2004 Dallas Semiconductor Corporation, All Rights Reserved.
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


	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;

	/// <summary>
	/// <P> 1-Wire&#174 container for a Addressable 1-Wire 4K-bit EEPROM, DS28E04.
	/// This container encapsulates the functionality of the 1-Wire family
	/// type <B>1C</B> (hex)</P>
	/// 
	/// <H3> Features </H3>
	/// <UL>
	///   <LI> 4096 bits of EEPROM memory partitioned into 16 pages of 256 bits.
	///   <LI> Seven address inputs for physical location configuration.
	///   <LI> 2 general-purpose I/O pins with pulse generation capability.
	///   <LI> Individual Memory pages can be permanently write-protected or put in
	///        OTP EPROM-emulation mode ("write to 0").
	///   <LI> Supports 1-Wire Conditional Search command with response controlled by
	///        programmable PIO conditions
	///   <LI> Supports Overdrive mode which boosts communication speed up to 125k bits
	///        per second.
	/// </UL>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireSensor </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.SwitchContainer </seealso>
	/// <seealso cref= com.dalsemi.onewire.container.OneWireContainer
	/// 
	///  @version    1.00, 06 Jul 2004
	///  @author     SKH </seealso>

	public class OneWireContainer1C : OneWireContainer, SwitchContainer
	{
	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Used for 0xFF array
	   /// </summary>
	   private sbyte[] FF = new sbyte [8];

	   //--------
	   //-------- Memory Banks
	   //--------

	   /// <summary>
	   /// Memory bank scratchpad for the DS28E04
	   /// </summary>
	   private MemoryBankScratch scratch;

	   /// <summary>
	   /// Memory bank of the DS28E04 for main memory, protection memory,
	   /// pio readout memory, conditional search and status memory.
	   /// </summary>
	   private MemoryBankNV mainMemory, protectionMemory, pioMemory, searchMemory;

	   //--------
	   //-------- Device Specific Commands
	   //--------

	   /// <summary>
	   /// Reads the PIO logical status in an endless loops
	   /// </summary>
	   public static readonly sbyte PIO_ACCESS_READ = unchecked((sbyte) 0xF5);

	   /// <summary>
	   /// Writes the PIO output latch
	   /// </summary>
	   public static readonly sbyte PIO_ACCESS_WRITE = (sbyte) 0x5A;

	   /// <summary>
	   /// Generates a pulse on the selected PIOs
	   /// </summary>
	   public static readonly sbyte PIO_ACCESS_PULSE = unchecked((sbyte) 0xA5);

	   /// <summary>
	   /// Generates a pulse on the selected PIOs
	   /// </summary>
	   public static readonly sbyte RESET_ACTIVITY_LATCHES = unchecked((sbyte) 0xC3);

	   /// <summary>
	   /// Writes the value to the Conditional Search registers or Control/Status register
	   /// </summary>
	   public static readonly sbyte WRITE_REGISTER = unchecked((sbyte) 0xCC);


	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS28E04.
	   /// Note that the method <code>setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[])</code>
	   /// must be called to set the correct <code>DSPortAdapter</code> device address.
	   /// </summary>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer#setupContainer(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) setupContainer(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer1C(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,long) OneWireContainer1C(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer1C(DSPortAdapter,String) </seealso>
	   public OneWireContainer1C() : base()
	   {

		  initmem();

		  for (int i = 0; i < FF.Length; i++)
		  {
			 FF[i] = unchecked((sbyte) 0x0FF);
		  }
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS28E04.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS28E04
	   /// </param>
	   /// <seealso cref= #OneWireContainer1C() </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,long) OneWireContainer1C(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer1C(DSPortAdapter,String) </seealso>
	   public OneWireContainer1C(DSPortAdapter sourceAdapter, sbyte[] newAddress) : base(sourceAdapter, newAddress)
	   {

		  initmem();

		  for (int i = 0; i < FF.Length; i++)
		  {
			 FF[i] = unchecked((sbyte) 0x0FF);
		  }
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS28E04.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS28E04
	   /// </param>
	   /// <seealso cref= #OneWireContainer1C() </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer1C(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,java.lang.String) OneWireContainer1C(DSPortAdapter,String) </seealso>
	   public OneWireContainer1C(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {

		  initmem();

		  for (int i = 0; i < FF.Length; i++)
		  {
			 FF[i] = unchecked((sbyte) 0x0FF);
		  }
	   }

	   /// <summary>
	   /// Creates a new <code>OneWireContainer</code> for communication with a DS28E04.
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this 1-Wire device </param>
	   /// <param name="newAddress">        address of this DS28E04
	   /// </param>
	   /// <seealso cref= #OneWireContainer1C() </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,byte[]) OneWireContainer1C(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer1C(com.dalsemi.onewire.adapter.DSPortAdapter,long) OneWireContainer1C(DSPortAdapter,long) </seealso>
	   public OneWireContainer1C(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {

		  initmem();

		  for (int i = 0; i < FF.Length; i++)
		  {
			 FF[i] = unchecked((sbyte) 0x0FF);
		  }
	   }

	   //--------
	   //-------- Methods
	   //--------

	   /// <summary>
	   /// Gets the Dallas Semiconductor part number of the iButton
	   /// or 1-Wire Device as a <code>java.lang.String</code>.
	   /// </summary>
	   /// <returns> iButton or 1-Wire device name </returns>
	   public override string Name
	   {
		   get
		   {
			  return "DS28E04";
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
			  ArrayList bank_vector = new ArrayList(5);
    
			  bank_vector.Add(scratch);
			  bank_vector.Add(mainMemory);
			  bank_vector.Add(protectionMemory);
			  bank_vector.Add(pioMemory);
			  bank_vector.Add(searchMemory);
    
			  return bank_vector.GetEnumerator();
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
			  return "DS28E04";
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
			  return "Addressable 1-Wire 4K-bit EEPROM, with 2 channels" + " of general-purpose PIO pins with pulse generation capability.";
		   }
	   }

	   //--------
	   //-------- Switch Feature methods
	   //--------

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
		  return 2;
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
		  sbyte level = (sbyte)(0x01 << channel);
		  return ((state[0] & level) == level);
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
		  sbyte latch = (sbyte)(0x01 << channel);
		  return ((state [1] & latch) == latch);
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
	   /// <exception cref="OneWireException"> if this device does not have activity sensing
	   /// </exception>
	   /// <seealso cref= #hasActivitySensing() </seealso>
	   /// <seealso cref= #clearActivity() </seealso>
	   public virtual bool getSensedActivity(int channel, sbyte[] state)
	   {
		  sbyte activity = (sbyte)(0x01 << channel);
		  return ((state[2] & activity) == activity);
	   }

	   /// <summary>
	   /// Clears the activity latches the next time possible.  For
	   /// example, on a DS2406/07, this happens the next time the
	   /// status is read with <code>readDevice()</code>.
	   /// </summary>
	   /// <exception cref="OneWireException"> if this device does not support activity sensing
	   /// </exception>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#readDevice() </seealso>
	   /// <seealso cref= #getSensedActivity(int,byte[]) </seealso>
	   public virtual void clearActivity()
	   {
		  adapter.assertSelect(address);

		  sbyte[] buffer = new sbyte[9];

		  buffer[0] = RESET_ACTIVITY_LATCHES;
		  Array.Copy(FF,0,buffer,1,8);

		  adapter.dataBlock(buffer, 0, 9);

		  if ((buffer[1] != unchecked((sbyte) 0xAA)) && (buffer[1] != (sbyte) 0x55))
		  {
			 throw new OneWireException("Sense Activity was not cleared.");
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
		  sbyte latch = (sbyte)(0x01 << channel);

		  if (latchState)
		  {
			 state[1] = (sbyte)(state[1] | latch);
		  }
		  else
		  {
			 state[1] = (sbyte)(state[1] & ~latch);
		  }
	   }

	   /// <summary>
	   /// Sets the latch state for all of the channels.
	   /// The method <code>writeDevice()</code> must be called to finalize
	   /// changes to the device.  Note that multiple 'set' methods can
	   /// be called before one call to <code>writeDevice()</code>.
	   /// </summary>
	   /// <param name="set"> the state to set all of the channels, in the range [0 to (<code>getNumberChannels(byte[])</code> - 1)] </param>
	   /// <param name="state"> current state of the device returned from <code>readDevice()</code>
	   /// </param>
	   /// <seealso cref= #getLatchState(int,byte[]) </seealso>
	   /// <seealso cref= com.dalsemi.onewire.container.OneWireSensor#writeDevice(byte[]) </seealso>
	   public virtual void setLatchState(sbyte set, sbyte[] state)
	   {
		  state[1] = (sbyte) set;
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
		  sbyte[] state = new sbyte [3];

		  Array.Copy(FF,0,state,0,3);
		  pioMemory.read(0,false,state,0,3);

		  return state;
	   }

	   /// <summary>
	   /// Retrieves the 1-Wire device register mask.  This register is
	   /// returned as a byte array.  Pass this byte array to the 'get'
	   /// and 'set' methods.  If the device register mask needs to be changed then call
	   /// the 'writeRegister' to finalize the changes.
	   /// </summary>
	   /// <returns> 1-Wire device register mask
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual sbyte[] readRegister()
	   {
		  sbyte[] register = new sbyte[3];

		  searchMemory.read(0,false,register,0,3);

		  return register;
	   }

	   /// <summary>
	   /// Updates the latch state for the 2 general purpose PIO pins.  This
	   /// has the side-effect of also sampling the PIO pins level after updating
	   /// the latch, so the state buffer is updated with this new level information.
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
		  //channel Access Write
		  adapter.assertSelect(address);

		  sbyte[] buffer = new sbyte[5];

		  buffer[0] = PIO_ACCESS_WRITE;
		  buffer[1] = state[1];
		  buffer[2] = (sbyte)~state[1];
		  buffer[3] = unchecked((sbyte)0xFF);
		  buffer[4] = unchecked((sbyte)0xFF);

		  adapter.dataBlock(buffer, 0, 5);

		  if (buffer[3] != unchecked((sbyte) 0x00AA))
		  {
			 throw new OneWireIOException("Failure to change latch state.");
		  }

		  // update sensed logic level in state.
		  state[0] = buffer[4];
	   }

	   /// <summary>
	   /// Writes the 1-Wire device register mask that
	   /// have been changed by 'set' methods.
	   /// </summary>
	   /// <param name="register"> 1-Wire device sensor state
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual void writeRegister(sbyte[] register)
	   {
		  //channel Access Write
		  adapter.assertSelect(address);

		  sbyte[] buffer = new sbyte[6];

		  buffer[0] = WRITE_REGISTER;
		  buffer[1] = 0x23;
		  buffer[2] = 0x02;
		  buffer[3] = register[0];
		  buffer[4] = register[1];
		  buffer[5] = register[2];

		  adapter.dataBlock(buffer, 0, 6);
	   }

	   /// <summary>
	   /// Retrieves the state of the VCC pin.  If the pin is powered 'TRUE' is
	   /// returned else 'FALSE' is returned if the pin is grounded.
	   /// </summary>
	   /// <returns> <code>true</code> if VCC is powered and <code>false</code> if it is
	   ///         grounded.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public virtual bool isVccPowered(sbyte[] register)
	   {
		  if ((register[2] & unchecked((sbyte) 0x80)) == unchecked((sbyte) 0x80))
		  {
			 return true;
		  }

		  return false;
	   }

	   public virtual bool getDefaultPolarity(sbyte[] register)
	   {
		  if ((register[2] & (sbyte) 0x40) == (sbyte) 0x40)
		  {
			 return true;
		  }

		  return false;
	   }

	   public virtual bool getPowerOnResetLatch(sbyte[] register)
	   {
		  if ((register[2] & (sbyte) 0x08) == (sbyte) 0x08)
		  {
			 return true;
		  }

		  return false;
	   }

	   /// <summary>
	   /// Checks if the Power On Reset if on and if so clears it.
	   /// </summary>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual void clearPowerOnReset(sbyte[] register)
	   {
		  if ((register[2] & (sbyte) 0x08) == (sbyte) 0x08)
		  {
			 register[2] = (sbyte)((sbyte) register[2] & unchecked((sbyte) 0xF7));
		  }
	   }

	   /// <summary>
	   /// Checks if the 'or' Condition Search is set and if not sets it.
	   /// </summary>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual void orConditionalSearch(sbyte[] register)
	   {
		  if ((register[2] & (sbyte) 0x02) == (sbyte) 0x02)
		  {
			 register[2] = (sbyte)((sbyte) register[2] & unchecked((sbyte) 0xFD));
		  }
	   }

	   /// <summary>
	   /// Checks if the 'and' Conditional Search is set and if not sets it.
	   /// </summary>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual void andConditionalSearch(sbyte[] register)
	   {
		  if ((register[2] & (sbyte) 0x02) != (sbyte) 0x02)
		  {
			 register[2] = (sbyte)((sbyte) register[2] | (sbyte) 0x02);
		  }
	   }

	   /// <summary>
	   /// Checks if the 'PIO Level' Conditional Search is set for input and if not sets it.
	   /// </summary>
	   /// <param name="pinActivity"> if true, the activity latch for the pin is used for the
	   ///                    conditional search.  Otherwise, the sensed level of the
	   ///                    pin is used for the conditional search. </param>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual sbyte[] ConditionalSearchLogicLevel
	   {
		   set
		   {
			  if ((value[2] & (sbyte) 0x01) == (sbyte) 0x01)
			  {
				 value[2] = (sbyte)((sbyte) value[2] & unchecked((sbyte) 0xFE));
			  }
		   }
	   }

	   /// <summary>
	   /// Checks if the 'PIO Activity latches' are set for Conditional Search and if not sets it.
	   /// </summary>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual sbyte[] ConditionalSearchActivity
	   {
		   set
		   {
			  if ((value[2] & (sbyte) 0x01) != (sbyte) 0x01)
			  {
				 value[2] = (sbyte)((sbyte) value[2] | (sbyte) 0x01);
			  }
		   }
	   }

	   /// <summary>
	   /// Sets the channel passed to the proper state depending on the set parameter for
	   /// responding to the Conditional Search.
	   /// </summary>
	   /// <param name="channel">  current channel to set </param>
	   /// <param name="set">      whether to turn the channel on/off for Conditional Search </param>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual void setChannelMask(int channel, bool set, sbyte[] register)
	   {
		  sbyte mask = (sbyte)(0x01 << channel);

		  if (set)
		  {
			 register[0] = (sbyte)((sbyte) register[0] | (sbyte) mask);
		  }
		  else
		  {
			 register[0] = (sbyte)((sbyte) register[0] & (sbyte)~mask);
		  }
	   }

	   /// <summary>
	   /// Sets the channel passed to the proper state depending on the set parameter for
	   /// the correct polarity in the Conditional Search.
	   /// </summary>
	   /// <param name="channel">  current channel to set </param>
	   /// <param name="set">      whether to turn the channel on/off for polarity
	   ///                 Conditional Search </param>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code> </param>
	   public virtual void setChannelPolarity(int channel, bool set, sbyte[] register)
	   {
		  sbyte polarity = (sbyte)(0x01 << channel);

		  if (set)
		  {
			 register[1] = (sbyte)((sbyte) register[1] | (sbyte) polarity);
		  }
		  else
		  {
			 register[1] = (sbyte)((sbyte) register[1] & (sbyte)~polarity);
		  }
	   }

	   /// <summary>
	   /// Retrieves the information if the channel is masked for the Conditional Search.
	   /// </summary>
	   /// <param name="channel">  current channel to set </param>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code>
	   /// </param>
	   /// <returns> <code>true</code> if the channel is masked and <code>false</code> other wise. </returns>
	   public virtual bool getChannelMask(int channel, sbyte[] register)
	   {
		  sbyte mask = (sbyte)(0x01 << channel);

		  return ((register[0] & mask) == mask);
	   }

	   /// <summary>
	   /// Retrieves the polarity of the channel for the Conditional Search.
	   /// </summary>
	   /// <param name="channel">  current channel to set </param>
	   /// <param name="register"> current register for conditional search, which
	   ///                 if returned from <code>readRegister()</code>
	   /// </param>
	   /// <returns> <code>true</code> if the channel is masked and <code>false</code> other wise. </returns>
	   public virtual bool getChannelPolarity(int channel, sbyte[] register)
	   {
		  sbyte polarity = (sbyte)(0x01 << channel);

		  return ((register[1] & polarity) == polarity);
	   }

	   /// <summary>
	   /// Initialize the memory banks and data associated with each.
	   /// </summary>
	   private void initmem()
	   {
		  scratch = new MemoryBankScratchEE(this);
		  scratch.bankDescription = "Scratchpad";
		  ((MemoryBankScratchEE)scratch).COPY_DELAY_LEN = 18; //TODO: Should be 10
		  ((MemoryBankScratchEE)scratch).numVerificationBytes = 2;

		  mainMemory = new MemoryBankNV(this, scratch);
		  mainMemory.bankDescription = "User Data Memory";
		  mainMemory.startPhysicalAddress = 0x000;
		  mainMemory.size = 0x200;
		  mainMemory.readOnly = false;
		  mainMemory.generalPurposeMemory = true;
		  mainMemory.readWrite = true;
		  mainMemory.powerDelivery = true;

		  protectionMemory = new MemoryBankNV(this, scratch);
		  protectionMemory.bankDescription = "Protection Control and Factory Bytes";
		  protectionMemory.startPhysicalAddress = 0x200;
		  protectionMemory.size = 0x020;
		  protectionMemory.readOnly = false;
		  protectionMemory.generalPurposeMemory = false;
		  protectionMemory.readWrite = true;
		  protectionMemory.powerDelivery = true;

		  pioMemory = new MemoryBankNV(this, scratch);
		  pioMemory.bankDescription = "PIO Readouts";
		  pioMemory.startPhysicalAddress = 0x220;
		  pioMemory.size = 0x003;
		  pioMemory.readOnly = true;
		  pioMemory.generalPurposeMemory = false;
		  pioMemory.nonVolatile = false;
		  pioMemory.readWrite = false;
		  pioMemory.powerDelivery = false;

		  searchMemory = new MemoryBankNV(this, scratch);
		  searchMemory.bankDescription = "Conditional Search and Status Register";
		  searchMemory.startPhysicalAddress = 0x223;
		  searchMemory.size = 0x003;
		  searchMemory.readOnly = true;
		  searchMemory.generalPurposeMemory = false;
		  searchMemory.nonVolatile = false;
		  searchMemory.readWrite = false;
		  searchMemory.powerDelivery = false;
	   }
	}

}