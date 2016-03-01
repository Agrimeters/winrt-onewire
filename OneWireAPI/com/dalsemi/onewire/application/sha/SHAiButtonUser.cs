using System;
using System.IO;
using System.Text;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.application.sha
{

	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;
	using OWFile = com.dalsemi.onewire.application.file.OWFile;
	using Address = com.dalsemi.onewire.utils.Address;

	/// <summary>
	/// <P>The abstract superclass for all users of a SHAiButton transaction system.
	/// The user in a SHA transaction system mainly consists of a page (or pages) of
	/// account data.  The abstract superclass guarantees an interface for retrieving
	/// information about account data, as well as reading and writing that data.<P>
	/// 
	/// <P><code>SHAiButtonUser</code> was defined for use with the DS1963S (family
	/// code 0x18) and the DS1961S (family code 0x33).  The benefit to using our SHA
	/// iButtons in a transaction is for device authentication.  Using a
	/// challenge-response protocol, the DS1961S and the DS1963S can authenticate
	/// themselves to the system before their actual account data is verified.  But,
	/// if device authentication isn't important to your transaction, the
	/// <code>SHAiButtonUser</code> can be extended to support any 1-Wire memory
	/// device.</P>
	/// 
	/// <P>The format of user's account data is not specified by this class, only
	/// the interface for accessing it.  Each different SHATransaction is free to
	/// implement whatever format is appropriate for the transaction type.</P>
	/// </summary>
	/// <seealso cref= SHATransaction </seealso>
	/// <seealso cref= SHAiButtonCopr </seealso>
	/// <seealso cref= SHAiButtonUser18 </seealso>
	/// <seealso cref= SHAiButtonUser33
	/// 
	/// @version 1.00
	/// @author  SKH </seealso>
	public abstract class SHAiButtonUser
	{
	   /// <summary>
	   /// Turns on extra debugging output
	   /// </summary>
	   internal const bool DEBUG = false;

	   /// <summary>
	   /// Cache of 1-Wire Address
	   /// </summary>
	   protected internal byte[] address = null;

	   /// <summary>
	   /// local cache of accountData
	   /// </summary>
	   protected internal readonly byte[] accountData = new byte[32];

	   /// <summary>
	   /// page number account data is stored on
	   /// </summary>
	   protected internal int accountPageNumber = -1;

	   /// <summary>
	   /// used to construct appropriate string for OWFile constructor
	   /// </summary>
	   protected internal readonly byte[] serviceFile = new byte[]{(byte)'D',(byte)'L', (byte)'S',(byte)'M'};
	   /// <summary>
	   /// stores string name of user's service file
	   /// </summary>
	   protected internal string strServiceFilename = null;

	   /// <summary>
	   /// maintains a cache of the fullBindCode, for later binding of
	   /// coprocessor.
	   /// </summary>
	   protected internal readonly byte[] fullBindCode = new byte[15];

	   /// <summary>
	   /// local cache of writeCycleCounter for data page
	   /// </summary>
	   protected internal int writeCycleCounter = -1;

	   /// <summary>
	   /// force 1-wire container into overdrive speed
	   /// </summary>
	   protected internal bool forceOverdrive = false;



	   // ***********************************************************************
	   // Begin Accessor Methods
	   // ***********************************************************************

	   /// <summary>
	   /// <P>Returns the page number of the first memory page where account
	   /// data is stored.</P>
	   /// </summary>
	   /// <returns> page number where account data is stored. </returns>
	   public virtual int AccountPageNumber
	   {
		   get
		   {
			  return this.accountPageNumber;
		   }
	   }

	   /// <summary>
	   /// <P>Returns the TMEX filename of the user's account data file.</P>
	   /// </summary>
	   /// <returns> filename of user's account data file </returns>
	   public virtual string AccountFilename
	   {
		   get
		   {
			  return this.strServiceFilename;
		   }
	   }

	   /// <summary>
	   /// <P>Returns the 8 byte address of the OneWireContainer this
	   /// SHAiButton refers to.</P>
	   /// </summary>
	   /// <returns> 8 byte array containing family code, address, and
	   ///         crc8 of the OneWire device. </returns>
	   public virtual byte[] Address
	   {
		   get
		   {
			  byte[] data = new byte[8];
			  Array.Copy(address,0,data,0,8);
			  return data;
		   }
	   }

	   /// <summary>
	   /// <P>Copies the 8 byte address of the OneWireContainer into
	   /// the provided array starting at the given offset.</P>
	   /// </summary>
	   /// <param name="data"> array with at least 8 bytes after offset </param>
	   /// <param name="offset"> the index at which copying starts </param>
	   public virtual void getAddress(byte[] data, int offset)
	   {
		  Array.Copy(address, 0, data, offset, 8);
	   }

	   /// <summary>
	   /// <P>Copies the specified number of bytes from the address
	   /// of the OneWireContainer into the provided array starting
	   /// at the given offset.</P>
	   /// </summary>
	   /// <param name="data"> array with at least cnt bytes after offset </param>
	   /// <param name="offset"> the index at which copying starts </param>
	   /// <param name="cnt"> the number of bytes to copy </param>
	   public virtual void getAddress(byte[] data, int offset, int cnt)
	   {
		  Array.Copy(address, 0, data, offset, cnt);
	   }

	   /// <summary>
	   /// Sets whether or not the container should be forced into
	   /// overdrive.
	   /// </summary>
	   /// <param name="value"> if true, the container will be forced to overdrive </param>
	   public virtual bool ForceOverdrive
	   {
		   set
		   {
			  this.forceOverdrive = value;
		   }
		   get
		   {
			  return this.forceOverdrive;
		   }
	   }


	   /// <summary>
	   /// <P>Create's empty service file for the user with the given filename.
	   /// Also, it populates <code>accountPageNumber</code> with the page
	   /// that the service file is stored on.</P>
	   /// </summary>
	   /// <param name="owc"> the 1-wire device where the service file will be created. </param>
	   /// <param name="filename"> the filename of the service file. </param>
	   /// <param name="formatDevice"> if <code>true</code>, the device is formatted
	   ///        before creating the service file. </param>
	   protected internal virtual bool createServiceFile(OneWireContainer owc, string filename, bool formatDevice)
	   {
		  bool bRetVal = false;
		  OWFile owf = new OWFile(owc, strServiceFilename);
		  try
		  {
			 if (formatDevice)
			 {
				owf.format();
			 }

			 if (!owf.exists())
			 {
				owf.createNewFile();
			 }

			 //save reference to page number for service file
			 this.accountPageNumber = owf.PageList[0];

			 bRetVal = true;
		  }
		  catch (IOException ioe)
		  {
			 bRetVal = false;
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 if (DEBUG)
			 {
				Debug.WriteLine(ioe.ToString());
				Debug.Write(ioe.StackTrace);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  }

		  try
		  {
			 owf.close();
			 return bRetVal;
		  }
		  catch (IOException ioe)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 if (DEBUG)
			 {
				Debug.WriteLine(ioe.ToString());
				Debug.Write(ioe.StackTrace);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			 /*well, at least I tried!*/
			 return false;
		  }
	   }

	   /// <summary>
	   /// <P>Asserts that proper account info exists for this SHAiButtonUser
	   /// and establishes a buffer for caching account data.</P>
	   /// </summary>
	   /// <param name="ibc"> the OneWireContainer whose account info is checked </param>
	   /// <returns> whether or not the device is initialized properly </returns>
	   protected internal virtual bool checkAccountPageInfo(OneWireContainer ibc)
	   {
		   lock (this)
		   {
			  //this flag should only be set if there is valid data
			  if (accountPageNumber <= 0)
			  {
				 try
				 {
					//create a file object representing service file
					OWFile owf = new OWFile(ibc, strServiceFilename);
        
					//check to see if file exists
					if (!owf.exists())
					{
					   return false;
					}
        
					//get the page number for the file
					//this.accountPageNumber = owf.getPageList()[0];
					this.accountPageNumber = owf.StartPage;
        
					//close the file
					owf.close();
        
					//mark the cache as dirty
					this.accountData[0] = 0;
        
					//clear the write cycle counter
					this.writeCycleCounter = -1;
        
				 }
				 catch (System.Exception e)
				 {
					this.accountPageNumber = -1;
					if (DEBUG)
					{
					   Debug.WriteLine(e.ToString());
					   Debug.Write(e.StackTrace);
					}
				 }
			  }
        
			  return (this.accountPageNumber > 0);
		   }
	   }

	   // *************************************************************** //
	   // Begin Abstract Methods for SHAiButtonUser                       //
	   // *************************************************************** //

	   /// <summary>
	   /// <P>Modifies this SHA iButton so that it refers to another device.
	   /// If this object already has an appropriate instance of OneWireContainer,
	   /// that instance is updated with the new address.</P>
	   /// </summary>
	   /// <param name="adapter"> The adapter that the device can be found on. </param>
	   /// <param name="address"> The address of the 1-Wire device
	   /// </param>
	   /// <returns> <code>true</code> if a valid account service file exists on
	   ///         this <code>OneWireContainer18</code>.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract bool setiButtonUser(DSPortAdapter adapter, byte[] address);

	   /// <summary>
	   /// <P>Modifies this SHA iButton so that it refers to another device.
	   /// If this object does not already has an appropriate instance of
	   /// OneWireContainer, it returns false immediately, because there is
	   /// no adapter info available.  Otherwise, it reuses the same adapter.</P>
	   /// </summary>
	   /// <param name="address"> The address of the 1-Wire device
	   /// </param>
	   /// <returns> <code>true</code> if a valid account service file exists on
	   ///         this <code>OneWireContainer18</code>.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract bool setiButtonUser(byte[] address);

	   /// <summary>
	   /// <P>Returns the value of the write cycle counter for the
	   /// page where the account data is stored.  If the write
	   /// cycle counter has ever been retrieved, this returns the
	   /// cached value.  Otherwise, this method reads the value
	   /// from the part.</P>
	   /// 
	   /// <P>For devices that do not support write cycle counters,
	   /// this method always returns -1.</P>
	   /// </summary>
	   /// <returns> the value of the write cycle counter for the
	   ///         account data page.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract int WriteCycleCounter {get;}

	   /// <summary>
	   /// <P>Returns <code>true</code> if this buttons account data is stored
	   /// on a page that has a write cycle counter.</P>
	   /// </summary>
	   /// <returns> <code>true</code> if account page has write cycle counter. </returns>
	   public abstract bool hasWriteCycleCounter();

	   /// <summary>
	   /// <P>This function creates the full 15-byte binding data for the
	   /// coprocessor to use to recreate this user's secret on the copr's
	   /// workspace page.  This function is located in the SHAiButtonUser
	   /// class to support binding codes for user buttons who use alternate
	   /// techniques (such as the DS1961S) for secret computation.</P>
	   /// </summary>
	   /// <param name="bindCode"> the 7-byte binding code from coprocessor's service file </param>
	   /// <param name="fullBindCode"> the 15-byte full binding code to to be copied into
	   ///                     the coprocessor's scratchpad.  There should be 15
	   ///                     bytes available starting from the offset. </param>
	   /// <param name="offset"> the offset into fullBindCode where copying should begin.
	   ///  </param>
	   public abstract void getFullBindCode(byte[] l_fullBindCode, int offset);


	   /// <summary>
	   /// <P>Returns a byte representing the appropriate authorization command
	   /// for the coprocessor to use to authenticate this user.  For a DS1961S,
	   /// the authentication command is AUTH_HOST, but for a DS1963S, the
	   /// authentication command is VALIDATE_PAGE.</P>
	   /// </summary>
	   /// <returns> byte indicating appropriate command for authenticating user
	   ///  </returns>
	   public abstract byte AuthorizationCommand {get;}

	   /// <summary>
	   /// <P>Writes the account data to the SHAiButton.  First, this function
	   /// asserts that the account page number is known.  The account data is
	   /// copied from dataBuffer starting at the offset.  If there are less
	   /// than 32 bytes available to copy, this function only copies the bytes
	   /// that are available.</P>
	   /// </summary>
	   /// <param name="dataBuffer"> the buffer to copy the account data from </param>
	   /// <param name="offset"> the index into the buffer where copying should begin </param>
	   /// <returns> whether or not the data write succeeded
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract bool writeAccountData(byte[] dataBuffer, int offset);

	   /// <summary>
	   /// <P>Reads the account data off the SHAiButton using a standard READ
	   /// command.  First, this function asserts that the account page number is
	   /// known as well as the length of the account file.  The 32 byte account
	   /// data page is copied into dataBuffer starting at the given offset.</P>
	   /// </summary>
	   /// <param name="dataBuffer"> the buffer to copy the account data into </param>
	   /// <param name="offset"> the index into the buffer where copying should begin </param>
	   /// <returns> whether or not the read was successful
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract bool readAccountData(byte[] dataBuffer, int offset);

	   /// <summary>
	   /// <P>Reads the account data off the SHAiButton using a READ_AUTHENTICATE
	   /// command.  First, this function asserts that the account page number is
	   /// known as well as the length of the account file.  Then it copies the
	   /// 3 byte challenge to the scratchpad before sending the command for
	   /// READ_AUTHENTICATE.  The 32 byte account data page is copied into
	   /// dataBuffer starting at dataStart.</P>
	   /// 
	   /// <P>In addition to the account data, this function also returns a
	   /// calculated MAC.  The MAC requires 20 bytes after the start index.
	   /// The return value is the write cycle counter value for the account
	   /// data page<para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="chlg"> the buffer containing a 3-byte random challenge. </param>
	   /// <param name="chlgStart"> the index into the buffer where the 3 byte
	   ///        challenge begins. </param>
	   /// <param name="dataBuffer"> the buffer to copy the account data into </param>
	   /// <param name="dataStart"> the index into the buffer where copying should begin </param>
	   /// <param name="mac"> the buffer to copy the resulting Message Authentication Code </param>
	   /// <param name="macStart"> the index into the mac buffer to start copying
	   /// </param>
	   /// <returns> the value of the write cycle counter for the page
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter </exception>
	   public abstract int readAccountData(byte[] chlg, int chlgStart, byte[] dataBuffer, int dataStart, byte[] mac, int macStart);

	   // *************************************************************** //
	   // End Abstract Methods for SHAiButtonUser                         //
	   // *************************************************************** //

	   /// <summary>
	   /// Refreshes eeprom SHA devices in case of weakly-programmed bits on
	   /// the account page.
	   /// </summary>
	   /// <returns> true if the refresh was successful </returns>
	   public virtual bool refreshDevice()
	   {
		  // no-op by default
		  return true;
	   }

	   /// <summary>
	   /// <P>Returns a string representing this SHAiButton.</P>
	   /// </summary>
	   /// <returns> a string containing the 8-byte address of this 1-Wire device. </returns>
	   public override string ToString()
	   {
		  StringBuilder sb = new StringBuilder(100);
		  sb.Append("USER: ");
		  sb.Append(Address.ToString()); //TODO ToString(this.address)
          sb.Append(", service: ");
		  sb.Append(this.strServiceFilename);
		  sb.Append(", acctPageNum: ");
		  sb.Append(this.accountPageNumber);
		  return sb.ToString();
	   }
	}

}