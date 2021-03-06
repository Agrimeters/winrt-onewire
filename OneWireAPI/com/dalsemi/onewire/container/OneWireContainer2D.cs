﻿using System.Collections;
using System.Collections.Generic;

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
    // imports
    using com.dalsemi.onewire.adapter;

    /// <summary>
    /// <P>1-Wire&#174 container for the '1K-Bit protected 1-Wire EEPROM
    /// family type <B>2D</B> (hex), Dallas Semiconductor part number:
    /// <B>DS2431</B>.
    ///
    /// <H3> Features </H3>
    /// <UL>
    ///   <LI> 1024 bits of 5V EEPROM memory partitioned into four pages of 256 bits
    ///   <LI> unique, fatory-lasered and tested 64-bit registration number (8-bit
    ///        family code + 48-bit serial number + 8-bit CRC tester) assures
    ///        absolute traceablity because no two parts are alike.
    ///   <LI> Built-in multidrop controller ensures compatibility with other 1-Wire
    ///        net products.
    ///   <LI> Reduces control, address, data and power to a single data pin.
    ///   <LI> Directly connects to a single port pin of a microprocessor and
    ///        communicates at up to 16.3k bits per second.
    ///   <LI> Overdrive mode boosts communication speed to 142k bits per second.
    ///   <LI> 8-bit family code specifies DS2431 communication requirements to reader.
    ///   <LI> Presence detector acknowledges when reader first applies voltage.
    ///   <LI> Low cost 6-lead TSOC surface mount package
    ///   <LI> Reads and writes over a wide voltage range of 2.8V to 5.25V from -40C
    ///        to +85C.
    /// </UL>
    ///
    /// <P> The memory can also be accessed through the objects that are returned
    /// from the <seealso cref="#getMemoryBanks() getMemoryBanks"/> method. </P>
    ///
    /// <DL>
    /// <DD> </A>
    /// </DL>
    ///
    /// @version 	0.00, 10 March 2004
    /// @author DS
    /// </summary>
    public class OneWireContainer2D : OneWireContainer
    {
        /*
         * registery memory bank to control write-once (EPROM) mode
         */
        private MemoryBankEEPROM register;

        /*
         * main memory bank
         */
        private MemoryBankEEPROM main_mem;

        /// <summary>
        /// Page Lock Flag
        /// </summary>
        public static readonly byte WRITEONCE_FLAG = unchecked((byte)0xAA);

        //--------
        //-------- Static Final Variables
        //--------

        /// <summary>
        /// Default Constructor OneWireContainer2D.
        /// Must call setupContainer before using.
        /// </summary>
        public OneWireContainer2D() : base()
        {
        }

        /// <summary>
        /// Create a container with a provided adapter object
        /// and the address of the iButton or 1-Wire device.
        /// </summary>
        /// <param name="sourceAdapter">     adapter object required to communicate with
        ///                           this iButton. </param>
        /// <param name="newAddress">        address of this 1-Wire device </param>
        public OneWireContainer2D(DSPortAdapter sourceAdapter, byte[] newAddress) : base(sourceAdapter, newAddress)
        {
            // initialize the memory banks
            initMem();
        }

        /// <summary>
        /// Create a container with a provided adapter object
        /// and the address of the iButton or 1-Wire device.
        /// </summary>
        /// <param name="sourceAdapter">     adapter object required to communicate with
        ///                           this iButton. </param>
        /// <param name="newAddress">        address of this 1-Wire device </param>
        public OneWireContainer2D(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
        {
            // initialize the memory banks
            initMem();
        }

        /// <summary>
        /// Create a container with a provided adapter object
        /// and the address of the iButton or 1-Wire device.
        /// </summary>
        /// <param name="sourceAdapter">     adapter object required to communicate with
        ///                           this iButton. </param>
        /// <param name="newAddress">        address of this 1-Wire device </param>
        public OneWireContainer2D(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
        {
            // initialize the memory banks
            initMem();
        }

        //--------
        //-------- Methods
        //--------

        /// <summary>
        /// Provide this container the adapter object used to access this device
        /// and provide the address of this iButton or 1-Wire device.
        /// </summary>
        /// <param name="sourceAdapter">     adapter object required to communicate with
        ///                           this iButton. </param>
        /// <param name="newAddress">        address of this 1-Wire device </param>
        public override void setupContainer(DSPortAdapter sourceAdapter, byte[] newAddress)
        {
            base.setupContainer(sourceAdapter, newAddress);

            // initialize the memory banks
            initMem();
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
            base.setupContainer(sourceAdapter, newAddress);

            // initialize the memory banks
            initMem();
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
            base.setupContainer(sourceAdapter, newAddress);

            // initialize the memory banks
            initMem();
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
                return "DS1972";
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
                return "DS2431";
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
                return "1K-Bit protected 1-Wire EEPROM.";
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
        public override IEnumerator MemoryBanks
        {
            get
            {
                List<MemoryBank> bank_vector = new List<MemoryBank>(2);

                // main memory
                bank_vector.Add((MemoryBank)main_mem);

                // register memory
                bank_vector.Add((MemoryBank)register);

                return bank_vector.GetEnumerator();
            }
        }

        /// <summary>
        /// Construct the memory banks used for I/O.
        /// </summary>
        private void initMem()
        {
            // scratch pad
            MemoryBankScratchEE sp = new MemoryBankScratchEE(this);
            sp.size = 8;
            sp.pageLength = 8;
            sp.maxPacketDataLength = 5;
            sp.pageAutoCRC = true;
            sp.COPY_DELAY_LEN = 30;
            sp.ES_MASK = 0;

            // main memory
            main_mem = new MemoryBankEEPROM(this, sp);

            // register memory
            register = new MemoryBankEEPROM(this, sp);

            // initialize attributes of this memory bank
            register.generalPurposeMemory = false;
            register.bankDescription = "Write-protect/EPROM-Mode control register";
            register.numberPages = 1;
            register.size = 8;
            register.pageLength = 8;
            register.maxPacketDataLength = 0;
            register.readWrite = true;
            register.writeOnce = false;
            register.readOnly = false;
            register.nonVolatile = true;
            register.pageAutoCRC = false;
            register._lockPage = false;
            register.programPulse = false;
            register.powerDelivery = true;
            register.extraInfo = false;
            register.extraInfoLength = 0;
            register.extraInfoDescription = null;
            register.writeVerification = false;
            register.startPhysicalAddress = 128;
            register.doSetSpeed = true;

            // set the lock mb
            main_mem.mbLock = register;
        }

        //--------
        //-------- Custom Methods for this 1-Wire Device Type
        //--------

        /// <summary>
        /// Query to see if current memory bank is write write once such
        /// as with EPROM technology.
        /// </summary>
        /// <returns>  'true' if current memory bank can only be written once </returns>
        public virtual bool isPageWriteOnce(int page)
        {
            byte[] rd_byte = new byte[1];

            register.read(page, false, rd_byte, 0, 1);

            return (rd_byte[0] == WRITEONCE_FLAG);
        }

        /// <summary>
        /// Lock the specifed page in the current memory bank.  Not supported
        /// by all devices.  See the method 'canLockPage()'.
        /// </summary>
        /// <param name="page">   number of page to lock
        /// </param>
        /// <exception cref="OneWireIOException"> </exception>
        /// <exception cref="OneWireException"> </exception>
        public virtual int PageWriteOnce
        {
            set
            {
                byte[] wr_byte = new byte[1];

                wr_byte[0] = WRITEONCE_FLAG;

                register.write(value, wr_byte, 0, 1);

                // read back to verify
                if (!isPageWriteOnce(value))
                {
                    throw new OneWireIOException("Failed to set page to write-once mode.");
                }
            }
        }
    }
}