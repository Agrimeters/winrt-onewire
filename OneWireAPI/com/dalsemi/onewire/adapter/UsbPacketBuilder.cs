using System;
using System.Collections.Generic;
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

namespace com.dalsemi.onewire.adapter
{
    // imports
    using Address = com.dalsemi.onewire.utils.Address;

    /// <summary>
    /// UsbPacketBuilder contains the methods to build a communication packet
    ///  to the DS2490 based USB adapter.
    ///
    ///  @version    0.00, 1 Mar 2016
    ///  @author     JW
    /// </summary>
    internal class UsbPacketBuilder : IDisposable
    {
        //--------
        //-------- Finals
        //--------
        //-------- Misc

        /// <summary>
        /// Byte operation </summary>
        public const int OPERATION_BYTE = 0;

        /// <summary>
        /// Byte operation </summary>
        public const int OPERATION_SEARCH = 1;

        /// <summary>
        /// Max bytes to stream at once </summary>
        public const byte MAX_BYTES_STREAMED = 64;

        //-------- DS9097U function commands

        /// <summary>
        /// DS9097U funciton command, single bit </summary>
        public const byte FUNCTION_BIT = 0x81;

        /// <summary>
        /// DS9097U funciton command, turn search mode on </summary>
        public const byte FUNCTION_SEARCHON = 0xB1;

        /// <summary>
        /// DS9097U funciton command, turn search mode off </summary>
        public const byte FUNCTION_SEARCHOFF = 0xA1;

        /// <summary>
        /// DS9097U funciton command, OneWire reset </summary>
        public const byte FUNCTION_RESET = 0xC1;

        /// <summary>
        /// DS9097U funciton command, 5V pulse imediate </summary>
        public const byte FUNCTION_5VPULSE_NOW = 0xED;

        /// <summary>
        /// DS9097U funciton command, 12V pulse imediate </summary>
        public const byte FUNCTION_12VPULSE_NOW = 0xFD;

        /// <summary>
        /// DS9097U funciton command, 5V pulse after next byte </summary>
        public const byte FUNCTION_5VPULSE_ARM = 0xEF;

        /// <summary>
        /// DS9097U funciton command to stop an ongoing pulse </summary>
        public const byte FUNCTION_STOP_PULSE = 0xF1;

        //-------- DS9097U bit polarity settings for doing bit operations

        /// <summary>
        /// DS9097U bit polarity one for function FUNCTION_BIT </summary>
        public const byte BIT_ONE = 0x10;

        /// <summary>
        /// DS9097U bit polarity zero  for function FUNCTION_BIT </summary>
        public const byte BIT_ZERO = 0x00;

        //-------- DS9097U 5V priming values

        /// <summary>
        /// DS9097U 5V prime on for function FUNCTION_BIT </summary>
        public const byte PRIME5V_TRUE = 0x02;

        /// <summary>
        /// DS9097U 5V prime off for function FUNCTION_BIT </summary>
        public const byte PRIME5V_FALSE = 0x00;

        //-------- DS9097U command masks

        /// <summary>
        /// DS9097U mask to read or write a configuration parameter </summary>
        public const byte CONFIG_MASK = 0x01;

        /// <summary>
        /// DS9097U mask to read the OneWire reset response byte </summary>
        public const byte RESPONSE_RESET_MASK = 0x03;

        //-------- DS9097U reset results

        /// <summary>
        /// DS9097U  OneWire reset result = shorted </summary>
        public const byte RESPONSE_RESET_SHORT = 0x00;

        /// <summary>
        /// DS9097U  OneWire reset result = presence </summary>
        public const byte RESPONSE_RESET_PRESENCE = 0x01;

        /// <summary>
        /// DS9097U  OneWire reset result = alarm </summary>
        public const byte RESPONSE_RESET_ALARM = 0x02;

        /// <summary>
        /// DS9097U  OneWire reset result = no presence </summary>
        public const byte RESPONSE_RESET_NOPRESENCE = 0x03;

        //-------- DS9097U bit interpretation

        /// <summary>
        /// DS9097U mask to read bit operation result </summary>
        public const byte RESPONSE_BIT_MASK = 0x03;

        /// <summary>
        /// DS9097U read bit operation 1 </summary>
        public const byte RESPONSE_BIT_ONE = 0x03;

        /// <summary>
        /// DS9097U read bit operation 0 </summary>
        public const byte RESPONSE_BIT_ZERO = 0x00;

        /// <summary>
        /// Enable/disable debug messages </summary>
        public static bool doDebugMessages = true;

        //--------
        //-------- Variables
        //--------

        /// <summary>
        /// The current state of the DS2490, passed into constructor.
        /// </summary>
        private UsbAdapterState UsbState;

        /// <summary>
        /// The current current count for the number of return bytes from
        /// the packet being created.
        /// </summary>
        protected internal int totalReturnLength;

        /// <summary>
        /// Current raw send packet before it is added to the packetsVector
        /// </summary>
        protected internal RawSendPacket packet;

        /// <summary>
        /// Vector of raw send packets
        /// </summary>
        protected internal List<RawSendPacket> packetsVector;

        //--------
        //-------- Constructors
        //--------

        /// <summary>
        /// Constructs a new USB packet builder.
        /// </summary>
        /// <param name="startUState">   the object that contains the DS2490 state
        ///                        which is reference when creating packets </param>
        public UsbPacketBuilder(UsbAdapterState startUState)
        {
            // get a reference to the USB adapter state
            UsbState = startUState;

            // create the buffer for the data
            packet = new RawSendPacket();

            // create the vector
            packetsVector = new List<RawSendPacket>();

            // restart the packet to initialize
            restart();
        }

        //--------
        //-------- Packet handling Methods
        //--------

        /// <summary>
        /// Reset the packet builder to start a new one.
        /// </summary>
        public virtual void restart()
        {
            // clear the vector list of packets
            packetsVector.Clear();

            // truncate the packet to 0 length
            packet.buffer.Flush(); //TODO .buffer.Length = 0;

            packet.returnLength = 0;

            // reset the return cound
            totalReturnLength = 0;
        }

        /// <summary>
        /// Take the current packet and place it into the vector.  This
        /// indicates a place where we need to wait for the results from
        /// DS9097U adapter.
        /// </summary>
        public virtual void newPacket()
        {
            // add the packet
            packetsVector.Add(packet);

            // get a new packet
            packet = new RawSendPacket();
        }

        /// <summary>
        /// Retrieve enumeration of raw send packets
        /// </summary>
        /// <returns>  the enumeration of packets </returns>
        public virtual System.Collections.IEnumerator Packets
        {
            get
            {
                // put the last packet into the vector if it is non zero
                if (packet.buffer.Length > 0)
                {
                    newPacket();
                }

                return packetsVector.GetEnumerator();
            }
        }

        //--------
        //-------- 1-Wire Network operation append methods
        //--------

        /// <summary>
        /// Append data bytes (read/write) to the packet.
        /// </summary>
        /// <param name="dataBytesValue">  character array of data bytes
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int dataBytes(byte[] dataBytesValue)
        {
            //TODO		  byte byte_value;
            int i; //TODO, j;

            // provide debug output
            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: UsbPacketbuilder-dataBytes[] length " + dataBytesValue.Length);
            }

            // record the current count location
            int ret_value = totalReturnLength;

            // check each byte to see if some need duplication
            for (i = 0; i < dataBytesValue.Length; i++)
            {
                // convert the rest to OneWireIOExceptions
                // append the data
                packet.writer.Write(dataBytesValue[i]);

                // provide debug output
                if (doDebugMessages)
                {
                    Debug.WriteLine("DEBUG: UsbPacketbuilder-dataBytes[] byte[" + ((int)dataBytesValue[i] & 0x00FF).ToString("x") + "]");
                }

                // Escape special characters
                //TODO
                //if (((byte)(dataBytesValue [i] & 0x00FF) == UsbAdapterState.MODE_COMMAND) || (((byte)(dataBytesValue [i] & 0x00FF) == UsbAdapterState.MODE_SPECIAL) && (uState.revision == UsbAdapterState.CHIP_VERSION1)))
                //{
                //	// duplicate this data byte
                //             packet.writer.Write(dataBytesValue[i]);
                //	//TODO packet.buffer.Append(dataBytesValue [i]);
                //}

                // add to the return number of bytes
                totalReturnLength++;
                packet.returnLength++;

                // provide debug output
                if (doDebugMessages)
                {
                    Debug.WriteLine("DEBUG: UsbPacketbuilder-dataBytes[] returnlength " + packet.returnLength + " bufferLength " + packet.buffer.Length);
                }

                // check for packet too large or not streaming bytes
                //TODO
                //if ((packet.buffer.Length > MAX_BYTES_STREAMED) || !UsbState.streamBytes)
                //{
                //	newPacket();
                //}
            }

            return ret_value;
        }

        /// <summary>
        /// Append data bytes (read/write) to the packet.
        /// </summary>
        /// <param name="dataBytesValue">  byte array of data bytes </param>
        /// <param name="off">   offset into the array of data to start </param>
        /// <param name="len">   length of data to send / receive starting at 'off'
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int dataBytes(byte[] dataBytesValue, int off, int len)
        {
            byte[] temp_ch = new byte[len];

            for (int i = 0; i < len; i++)
            {
                temp_ch[i] = (byte)dataBytesValue[off + i];
            }

            return dataBytes(temp_ch);
        }

        /// <summary>
        /// Append a data byte (read/write) to the packet.
        /// </summary>
        /// <param name="dataByteValue">  data byte to append
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int dataByte(byte dataByteValue)
        {
            // contruct a temporary array of characters of lenght 1
            // to use the dataBytes method
            byte[] temp_byte_array = new byte[1];

            temp_byte_array[0] = dataByteValue;

            // provide debug output
            if (doDebugMessages)
            {
                Debug.WriteLine("DEBUG: UsbPacketbuilder-dataBytes [" + ((int)dataByteValue & 0x00FF).ToString("x") + "]");
            }

            return dataBytes(temp_byte_array);
        }

        /// <summary>
        /// Append a data byte (read/write) to the packet.  Do a strong pullup
        /// when the byte is complete
        /// </summary>
        /// <param name="dataByteValue">  data byte to append
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int primedDataByte(byte dataByteValue)
        {
            int offset, start_offset = 0;

            // create a primed data byte by using bits with last one primed
            for (int i = 0; i < 8; i++)
            {
                offset = dataBit(((dataByteValue & 0x01) == 0x01), (i == 7));
                dataByteValue = (byte)((uint)dataByteValue >> 1);

                // record the starting offset
                if (i == 0)
                {
                    start_offset = offset;
                }
            }

            return start_offset;
        }

        /// <summary>
        /// Append a data bit (read/write) to the packet.
        /// </summary>
        /// <param name="dataBit">   bit to append </param>
        /// <param name="strong5V">  true if want strong5V after bit
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int dataBit(bool dataBit, bool strong5V)
        {
            // append the bit with polarity and strong5V options
            packet.writer.Write((byte)(FUNCTION_BIT | UsbState.BusCommSpeed | ((dataBit) ? BIT_ONE : BIT_ZERO) | ((strong5V) ? PRIME5V_TRUE : PRIME5V_FALSE)));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large or not streaming bits
            //TODO
            //if ((packet.buffer.Length > MAX_BYTES_STREAMED) || !UsbState.streamBits)
            //{
            //newPacket();
            //}

            return (totalReturnLength - 1);
        }

        /// <summary>
        /// Append a search to the packet.  Assume that any reset and search
        /// command have already been appended.  This will add only the search
        /// itself.
        /// </summary>
        /// <param name="mState"> OneWire state
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int search(OneWireState mState)
        {
            // create the search sequence character array
            byte[] search_sequence = new byte[8];

            // get a copy of the current ID
            for (int i = 0; i < 8; i++)
                search_sequence[i] = mState.ID[i];

            // only modify bits if not the first search
            if (mState.searchLastDiscrepancy != 0xFF)
            {
                if (mState.searchLastDiscrepancy > 0)
                    bitWrite(search_sequence, mState.searchLastDiscrepancy - 1, true);

                for (int i = mState.searchLastDiscrepancy; i < 64; i++)
                    bitWrite(search_sequence, i, false);
            }

            // add this sequence
            packet.writer.Write(search_sequence);

            return totalReturnLength;
        }

        /// <summary>
        /// Append a search off to set the current speed.
        /// </summary>
        public virtual void setSpeed()
        {
            // search mode off and change speed
            packet.writer.Write((byte)(FUNCTION_SEARCHOFF | UsbState.BusCommSpeed));

            // no return byte
        }

        //--------
        //-------- U mode commands
        //--------

        /// <summary>
        /// Append a get parameter to the packet.
        /// </summary>
        /// <param name="parameter">  parameter to get
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int getParameter(int parameter)
        {
            // append paramter get
            packet.writer.Write((byte)(CONFIG_MASK | parameter >> 3));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large
            if (packet.buffer.Length > MAX_BYTES_STREAMED)
            {
                newPacket();
            }

            return (totalReturnLength - 1);
        }

        /// <summary>
        /// Append a set parameter to the packet.
        /// </summary>
        /// <param name="parameter">       parameter to set </param>
        /// <param name="parameterValue">  parameter value
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation </returns>
        public virtual int setParameter(byte parameter, byte parameterValue)
        {
            // append the paramter set with value
            packet.writer.Write((byte)((CONFIG_MASK | parameter) | parameterValue));

            // add to the return number of bytes
            totalReturnLength++;
            packet.returnLength++;

            // check for packet too large
            if (packet.buffer.Length > MAX_BYTES_STREAMED)
            {
                newPacket();
            }

            return (totalReturnLength - 1);
        }

        /// <summary>
        /// Append a send command to the packet.  This command does not
        /// elicit a response byte.
        /// </summary>
        /// <param name="command">       command to send </param>
        /// <param name="expectResponse">
        /// </param>
        /// <returns> the number offset in the return packet to get the
        ///          result of this operation (if there is one) </returns>
        public virtual int sendCommand(byte command, bool expectResponse)
        {
            // append the paramter set with value
            packet.writer.Write(command);

            // check for response
            if (expectResponse)
            {
                // add to the return number of bytes
                totalReturnLength++;
                packet.returnLength++;
            }

            // check for packet too large
            if (packet.buffer.Length > MAX_BYTES_STREAMED)
            {
                newPacket();
            }

            return (totalReturnLength - 1);
        }

        //--------
        //-------- 1-Wire Network result interpretation methods
        //--------

        /// <summary>
        /// Interpret the block of bytes
        /// </summary>
        /// <param name="dataByteResponse"> </param>
        /// <param name="responseOffset"> </param>
        /// <param name="result"> </param>
        /// <param name="offset"> </param>
        /// <param name="len"> </param>
        public virtual void interpretDataBytes(byte[] dataByteResponse, int responseOffset, byte[] result, int offset, int len)
        {
            for (var i = 0; i < len; i++)
            {
                // convert the rest to OneWireIOExceptions
                result[offset + i] = (byte)dataByteResponse[responseOffset + i];
            }
        }

        /// <summary>
        /// Interpret the bit response byte from a U adapter
        /// </summary>
        /// <param name="bitResponse">  bit response byte from U
        /// </param>
        /// <returns> bool representing the result of a 1-Wire bit operation </returns>
        public virtual bool interpretOneWireBit(byte bitResponse)
        {
            // interpret the bit
            if ((bitResponse & RESPONSE_BIT_MASK) == RESPONSE_BIT_ONE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Interpret the search response and set the 1-Wire state accordingly.
        /// </summary>
        /// <param name="mState"> </param>
        /// <param name="searchResponse"> </param>
        /// <param name="responseOffset">
        /// </param>
        /// <returns> bool return is true if a valid ID was found when
        ///                 interpreting the search results </returns>
        public virtual bool interpretSearch(OneWireState mState, byte[] searchResponse, int responseOffset)
        {
            bool rt = false;

            // check
            byte[] id = new byte[8];

            for (int i = 0; i < 8; i++)
                id[i] = searchResponse[i];

            // set the temp Last Descrep to none
            int temp_last_descrepancy = 0xFF;

            if (Address.isValid(id) && id[0] != 0)
            {
                if (searchResponse.Length == 8)
                {
                    mState.searchLastDevice = true;
                }
                else
                {
                    for (int i = 0; i < 64; i++)
                    {
                        // if descrepancy
                        if (bitRead(searchResponse, 64 + i) &&
                            (bitRead(searchResponse, i) == false))
                        {
                            temp_last_descrepancy = i + 1;
                        }
                    }
                }

                // copy the ID number to the buffer
                for (int i = 0; i < 8; i++)
                    mState.ID[i] = id[i];

                rt = true;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    mState.ID[i] = 0;
            }

            mState.searchLastDiscrepancy = temp_last_descrepancy;

            return rt;
        }

        /// <summary>
        /// Interpret the data response byte from a primed byte operation
        /// </summary>
        /// <param name="primedDataResponse"> </param>
        /// <param name="responseOffset">
        /// </param>
        /// <returns> the byte representing the result of a 1-Wire data byte </returns>
        public virtual byte interpretPrimedByte(byte[] primedDataResponse, int responseOffset)
        {
            byte result_byte = 0;

            // loop through and interpret each bit
            for (int i = 0; i < 8; i++)
            {
                result_byte = (byte)((int)((uint)result_byte >> 1));

                if (interpretOneWireBit(primedDataResponse[responseOffset + i]))
                {
                    result_byte |= 0x80;
                }
            }

            return (byte)(result_byte & 0xFF);
        }

        //--------
        //-------- Misc Utility methods
        //--------

        /// <summary>
        /// Request the maximum rate to do an operation
        /// </summary>
        public static int getDesiredBaud(int operation, int owSpeed, int maxBaud)
        {
            int baud = 9600;

            switch (operation)
            {
                case OPERATION_BYTE:
                    if (owSpeed == DSPortAdapter.SPEED_OVERDRIVE)
                    {
                        baud = 115200;
                    }
                    else
                    {
                        baud = 9600;
                    }
                    break;

                case OPERATION_SEARCH:
                    if (owSpeed == DSPortAdapter.SPEED_OVERDRIVE)
                    {
                        baud = 57600;
                    }
                    else
                    {
                        baud = 9600;
                    }
                    break;
            }

            if (baud > maxBaud)
            {
                baud = maxBaud;
            }

            return baud;
        }

        /// <summary>
        /// Bit utility to read a bit in the provided array of chars.
        /// </summary>
        /// <param name="bitBuffer"> array of chars where the bit to read is located </param>
        /// <param name="address">   bit location to read (LSBit of first Byte in bitBuffer
        ///                    is postion 0)
        /// </param>
        /// <returns> the bool value of the bit position </returns>
        public virtual bool bitRead(byte[] bitBuffer, int address)
        {
            int byte_number, bit_number;

            byte_number = (address / 8);
            bit_number = address - (byte_number * 8);

            return (((byte)((bitBuffer[byte_number] >> bit_number) & 0x01)) == 0x01);
        }

        /// <summary>
        /// Bit utility to write a bit in the provided array of chars.
        /// </summary>
        /// <param name="bitBuffer"> array of chars where the bit to write is located </param>
        /// <param name="address">   bit location to write (LSBit of first Byte in bitBuffer
        ///                    is postion 0) </param>
        /// <param name="newBitState"> new bit state </param>
        public virtual void bitWrite(byte[] bitBuffer, int address, bool newBitState)
        {
            int byte_number, bit_number;

            byte_number = (address / 8);
            bit_number = address - (byte_number * 8);

            if (newBitState)
            {
                bitBuffer[byte_number] |= (byte)(0x01 << bit_number);
            }
            else
            {
                bitBuffer[byte_number] &= (byte)(~(0x01 << bit_number));
            }
        }

        ~UsbPacketBuilder()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (packet != null)
                {
                    packet.Dispose();
                    packet = null;
                }
            }
        }
    }
}