using System;
using System.IO;
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


	using OneWireContainer18 = com.dalsemi.onewire.container.OneWireContainer18;
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using IOHelper = com.dalsemi.onewire.utils.IOHelper;
	using OWFile = com.dalsemi.onewire.application.file.OWFile;
	using OWFileOutputStream = com.dalsemi.onewire.application.file.OWFileOutputStream;
	using OWFileInputStream = com.dalsemi.onewire.application.file.OWFileInputStream;
	using OWFileNotFoundException = com.dalsemi.onewire.application.file.OWFileNotFoundException;

	/// <summary>
	/// <P>Class for holding instances of SHA iButton Coprocessors involved in SHA
	/// Transactions.  The Coprocessor is used for digitally signing transaction
	/// data as well as generating random challenges for users and verifying
	/// their response.</P>
	/// 
	/// <para>A DS1963S SHA iButton can be a <code>SHAiButtonCopr</code> or a
	/// <code>SHAiButtonUser</code>.  A Coprocessor iButton verifiessignatures
	/// and signs data for User iButtons.  A Coprocessor might be located
	/// inside a vending machine, where a person would bring their User iButton.  When
	/// the User iButton is pressed to the Blue Dot to perform a transaction, the Coprocessor
	/// would first verify that this button belongs to the system, i.e. that it knows the same
	/// authentication secret (example: a Visa terminal making sure the iButton had a Visa
	/// account installed). Then the Coprocessor would verify the signed data, probably money,
	/// to make sure it was valid.  If someone tried to overwrite the money file, even with a
	/// previously valid money file (an attempt to 'restore' a previous amount of money), the
	/// signed file would be invalid because the signature includes the write cycle counter,
	/// which is incremented every time a page is written to.  The write cycle counter is
	/// read-only and does not roll over, so the previous amount of money could not be restored
	/// by rolling the write counter. The Coprocessor verifies the money, then signs a new data
	/// file that contains the new amount of money. </para>
	/// 
	/// <para>There are two secrets involved with the transaction process.  The first secret is
	/// the authentication secret.  It is used to validate a User iButton to a system.  The Coprocessor
	/// iButton has the system authentication secret installed.  On User iButtons, the
	/// system authentication secret is merged with binding data and the unique address
	/// of the User iButton to create a unique device authentication secret.  The second secret
	/// is a signing secret.  This secret only exists on the Coprocessor iButton, and is used
	/// to sign and verify transaction data (i.e. money).  These secrets are inaccessible outside the
	/// iButton.  Once they are installed, they cannot be retrieved.</para>
	/// 
	/// <para>This class makes use of several performance enhancements for TINI.
	/// For instance, most methods are <code>synchronized</code> to access instance variable
	/// byte arrays rather than creating new byte arrays every time a transaction
	/// is performed.  This could hurt performance in multi-threaded
	/// applications, but the usefulness of having several threads contending
	/// to talk to a single iButton is questionable since the methods in
	/// <code>com.dalsemi.onewire.adapter.DSPortAdapter</code>
	/// <code>beginExclusive(bool)</code> and <code>endExclusive()</code> should be used.</para>
	/// </summary>
	/// <seealso cref= SHATransaction </seealso>
	/// <seealso cref= SHAiButtonUser
	/// 
	/// @version 1.00
	/// @author  SKH </seealso>
	public class SHAiButtonCopr
	{
	   /// <summary>
	   /// Turns on extra debugging output
	   /// </summary>
	   internal const bool DEBUG = true;

	   // ***********************************************************************
	   // Constants for Error codes
	   // ***********************************************************************
	   public const int NO_ERROR = 0;
	   public const int WRITE_DATA_PAGE_FAILED = -1;
	   public const int WRITE_SCRATCHPAD_FAILED = -2;
	   public const int MATCH_SCRATCHPAD_FAILED = -3;
	   public const int ERASE_SCRATCHPAD_FAILED = -4;
	   public const int COPY_SCRATCHPAD_FAILED = -5;
	   public const int SHA_FUNCTION_FAILED = -6;
	   public const int BIND_SECRET_FAILED = -7;
	   // ***********************************************************************

	   /// <summary>
	   /// Last error code raised
	   /// </summary>
	   protected internal int lastError;

	   /// <summary>
	   /// Reference to the OneWireContainer
	   /// </summary>
	   protected internal OneWireContainer18 ibc = null;

	   /// <summary>
	   /// Cache of 1-Wire Address
	   /// </summary>
	   protected internal byte[] address = null;

	   /// <summary>
	   /// Page used for generating user authentication secret.
	   /// </summary>
	   protected internal int authPageNumber = -1;

	   /// <summary>
	   /// Any auxilliary data stored on this coprocessor
	   /// </summary>
	   protected internal string auxData;

	   /// <summary>
	   /// 7 bytes of binding data for scratchpad to bind secret installation
	   /// </summary>
	   protected internal byte[] bindCode = new byte [7];

	   /// <summary>
	   /// 32 bytes of binding data to bind secret installation
	   /// </summary>
	   protected internal byte[] bindData = new byte [32];

	   /// <summary>
	   /// Specifies whether or not this coprocessor is compatible with
	   /// the DS1961S.  This entails the use of a specifically padded
	   /// authentication secret.
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   protected internal bool DS1961Scompatible_Renamed = false;

	   /// <summary>
	   /// Code used to specify encryption type.
	   /// </summary>
	   protected internal int encCode = -1;

	   /// <summary>
	   /// Filename, including extension, for user's service file
	   /// </summary>
	   protected internal byte[] filename = new byte[5];

	   /// <summary>
	   /// 20 byte initial signature, used for signing user account data
	   /// </summary>
	   protected internal byte[] initialSignature = new byte [20];

	   /// <summary>
	   /// The Provider name of the coprocessor's service
	   /// </summary>
	   protected internal string providerName;

	   /// <summary>
	   /// 3 byte challenge, used for signing user account data
	   /// </summary>
	   protected internal byte[] signingChallenge = new byte [3];

	   /// <summary>
	   /// Page used for signing user account data.
	   /// </summary>
	   protected internal int signPageNumber = 8;

	   /// <summary>
	   /// Code used to specify encryption type.
	   /// </summary>
	   protected internal int version = -1;

	   /// <summary>
	   /// Page used for generating user's validation MAC.
	   /// </summary>
	   protected internal int wspcPageNumber = -1;



	   // ***********************************************************************
	   // Constructors
	   // ***********************************************************************

	   /// <summary>
	   /// <para>No default construct for user apps.  Coprocessors, unlike users, are
	   /// immutable classes, so there is no <code>setiButton</code> for User
	   /// applications.</para>
	   /// </summary>
	   /// <seealso cref= #SHAiButtonCopr(OneWireContainer18,String,bool,int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCopr(OneWireContainer18,String) </seealso>
	   protected internal SHAiButtonCopr()
	   {
		   ;
	   }

	   /// <summary>
	   /// <P>Sets up this coprocessor object based on the provided parameters
	   /// and saves all of these parameters as the contents of the file
	   /// <code>coprFilename</code> stored on <code>owc</code>.  Then, the
	   /// system secret and authentication secret are installed on the
	   /// coprocessor button.</P>
	   /// 
	   /// <P>For the proper format of the coprocessor data file, see the
	   /// document entitled "Implementing Secured D-Identification and E-Payment
	   /// Applications using SHA iButtons".  For the format of TMEX file
	   /// structures, see Application Note 114.</P>
	   /// </summary>
	   /// <param name="l_owc"> The DS1963S used as a coprocessor. </param>
	   /// <param name="coprFilename"> The TMEX filename where coprocessor configuration
	   ///        data is stored.  Usually, "COPR.0". </param>
	   /// <param name="l_formatDevice"> bool indicating whether or not the TMEX
	   ///        filesystem of this device should be formatted before the
	   ///        coprocessor data file is stored. </param>
	   /// <param name="l_signPageNumber"> page number used for signing user account data.
	   ///        (Should be page 8, but page 0 is acceptable if you don't need
	   ///        the TMEX directory structure) </param>
	   /// <param name="l_authPageNumber"> page number used for recreating user secret. </param>
	   /// <param name="l_wspcPageNumber"> page number used for storing user secret and
	   ///        recreating authentication MAC. </param>
	   /// <param name="l_version"> version of the service provided by this coprocessor. </param>
	   /// <param name="l_encCode"> refers to a type of encryption used for user account
	   ///        data stored on user buttons. </param>
	   /// <param name="l_serviceFileExt"> the file extension used for the service file.
	   ///        (An extension of decimal 102 is reserved for Money files). </param>
	   /// <param name="l_serviceFilename"> the 4-byte name of the user's account data
	   ///        file. </param>
	   /// <param name="l_providerName"> the name of the provider of this service </param>
	   /// <param name="l_bindData"> the binding data used to finalize secret installation
	   ///        on user buttons. </param>
	   /// <param name="l_bindCode"> the binding code used to finalize secret installation
	   ///        on user buttons. </param>
	   /// <param name="l_auxData"> any auxilliary or miscellaneous data to be stored on
	   ///        the coprocessor. </param>
	   /// <param name="l_initialSignature"> the 20-byte initial MAC placed in user account
	   ///        data before generating actual MAC. </param>
	   /// <param name="l_signingChlg"> the 3-byte challenge used for signing user
	   ///        account data. </param>
	   /// <param name="l_signingSecret"> the system signing secret used by the
	   ///        service being installed on this coprocessor. </param>
	   /// <param name="l_authSecret"> the system authentication secret used by the
	   ///        service being installed on this coprocessor.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCopr(OneWireContainer18,String) </seealso>
	   public SHAiButtonCopr(OneWireContainer18 l_owc, string coprFilename, bool l_formatDevice, int l_signPageNumber, int l_authPageNumber, int l_wspcPageNumber, int l_version, int l_encCode, byte l_serviceFileExt, byte[] l_serviceFilename, byte[] l_providerName, byte[] l_bindData, byte[] l_bindCode, byte[] l_auxData, byte[] l_initialSignature, byte[] l_signingChlg, byte[] l_signingSecret, byte[] l_authSecret)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  // Do some bounds checking on our array data.
		  if (l_bindData.Length != 32)
		  {
			 throw new OneWireException("Invalid Binding Data");
		  }
		  if (l_bindCode.Length != 7)
		  {
			 throw new OneWireException("Invalid Binding Code");
		  }
		  if (l_signingChlg.Length != 3)
		  {
			 throw new OneWireException("Invalid Signing Challenge");
		  }
		  if (l_serviceFilename.Length < 4)
		  {
			 throw new OneWireException("Invalid Service Filename");
		  }
		  if (l_signPageNumber != 0 && l_signPageNumber != 8)
		  {
			 throw new OneWireException("Invalid Signing Page Number (must be 0 or 8)");
		  }

		  //Check to see if this coprocessor's authentication secret
		  //is appropriately padded to be used with a DS1961S
		  this.DS1961Scompatible_Renamed = ((l_authSecret.Length % 47) == 0);
		  int secretDiv = l_authSecret.Length / 47;
		  for (int j = 0; j < secretDiv && DS1961Scompatible_Renamed; j++)
		  {
			 int offset = 47 * j;
			 for (int i = 32; i < 36 && this.DS1961Scompatible_Renamed; i++)
			 {
				this.DS1961Scompatible_Renamed = (l_authSecret[i + offset] == unchecked((byte)0x0FF));
			 }
			 for (int i = 44; i < 47 && this.DS1961Scompatible_Renamed; i++)
			 {
				this.DS1961Scompatible_Renamed = (l_authSecret[i + offset] == unchecked((byte)0x0FF));
			 }
		  }

		  //get the current month, date, and year
		  DateTime c = new DateTime();
		  int month = c.Month + 1;
		  int date = c.Day;
		  int year = c.Year - 1900;
		  byte yearMSB = (byte)((year >> 8) & 0x0FF);
		  byte yearLSB = (byte)(year & 0x0FF);

		  try
		  {
			 if (l_formatDevice)
			 {
				//format if necessary
				OWFile f = new OWFile(l_owc,coprFilename);
				f.format();
				f.close();
			 }
			 //Create the service file
			 OWFileOutputStream fos = new OWFileOutputStream(l_owc, coprFilename);
			 fos.write(l_serviceFilename,0,4);
			 fos.write(l_serviceFileExt);
			 fos.write(l_signPageNumber);
			 fos.write(l_authPageNumber);
			 fos.write(l_wspcPageNumber);
			 fos.write(l_version);
			 fos.write(month);
			 fos.write(date);
			 fos.write(yearMSB);
			 fos.write(yearLSB);
			 fos.write(l_bindData);
			 fos.write(l_bindCode);
			 fos.write(l_signingChlg);
			 fos.write((byte)l_providerName.Length);
			 fos.write((byte)l_initialSignature.Length);
			 fos.write((byte)l_auxData.Length);
			 fos.write(l_providerName,0,(byte)l_providerName.Length);
			 fos.write(l_initialSignature,0,(byte)l_initialSignature.Length);
			 fos.write(l_auxData,0,(byte)l_auxData.Length);
			 fos.write(l_encCode);
			 fos.write(DS1961Scompatible_Renamed?0x55:0x00);
			 fos.Flush();
			 fos.close();
		  }
		  catch (System.Exception ioe)
		  {
			 Debug.WriteLine(ioe.ToString());
			 Debug.Write(ioe.StackTrace);
			 throw new OneWireException("Creating Service File failed!");
		  }

		  //Install the system signing secret, used to sign and validate all user data
		  if (!l_owc.installMasterSecret(l_signPageNumber, l_signingSecret, l_signPageNumber & 7))
		  {
			 throw new OneWireException("Could not install signing secret");
		  }

		  //Install the system authentication secret, used to authenticate users
		  if (!l_owc.installMasterSecret(l_authPageNumber, l_authSecret, l_authPageNumber & 7))
		  {
			 throw new OneWireException("Could not install authentication secret");
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (DEBUG)
		  {
			 IOHelper.writeLine("------------------------------------");
			 IOHelper.writeLine("Initialized Coprocessor");
			 IOHelper.writeLine("address");
			 IOHelper.writeBytesHex(l_owc.Address);
			 IOHelper.writeLine("signPageNumber: " + l_signPageNumber);
			 IOHelper.writeLine("authPageNumber: " + l_authPageNumber);
			 IOHelper.writeLine("wspcPageNumber: " + l_wspcPageNumber);
			 IOHelper.writeLine("serviceFilename");
			 IOHelper.writeBytesHex(l_serviceFilename);
			 IOHelper.writeLine("bindData");
			 IOHelper.writeBytesHex(l_bindData);
			 IOHelper.writeLine("bindCode");
			 IOHelper.writeBytesHex(l_bindCode);
			 IOHelper.writeLine("initialSignature");
			 IOHelper.writeBytesHex(l_initialSignature);
			 IOHelper.writeLine("signingChlg");
			 IOHelper.writeBytesHex(l_signingChlg);
			 IOHelper.writeLine("signingSecret");
			 IOHelper.writeBytesHex(l_signingSecret);
			 IOHelper.writeLine("authSecret");
			 IOHelper.writeBytesHex(l_authSecret);
			 IOHelper.writeLine("------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  //Call this method because it will read back the file.  Ensuring
		  //there were no errors in writing the file in the first place.
		  this.setiButton(l_owc, coprFilename);

	   }

	   /// <summary>
	   /// <P>Sets up this coprocessor object based on the contents of the file
	   /// <code>coprFilename</code> stored on <code>owc</code>. This sets
	   /// all the properties of the object as a consequence of what's in
	   /// the coprocessor file.</P>
	   /// 
	   /// <P>For the proper format of the coprocessor data file, see the
	   /// document entitled "Implementing Secured D-Identification and E-Payment
	   /// Applications using SHA iButtons".  For the format of TMEX file
	   /// structures, see Application Note 114.
	   /// </summary>
	   /// <param name="owc"> The DS1963S used as a coprocessor </param>
	   /// <param name="coprFilename"> The TMEX filename where coprocessor configuration
	   ///        data is stored.  Usually, "COPR.0".
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCopr(OneWireContainer18,String,bool,int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   public SHAiButtonCopr(OneWireContainer18 owc, string coprFilename)
	   {
		  setiButton(owc, coprFilename);
	   }



	   // ***********************************************************************
	   // Modifier Methods
	   //  - setiButton is the only essential modifier.  It updates all
	   //    data members based on contents of service file alone.
	   // ***********************************************************************

	   /// <summary>
	   /// <P>Sets up this coprocessor object based on the contents of the file
	   /// <code>coprFilename</code> stored on <code>owc</code>. This sets
	   /// all the properties of the object as a consequence of what's in
	   /// the coprocessor file.</P>
	   /// 
	   /// <P>In essence, this is the classes only proper modifier.  All data
	   /// members should be set by this method alone.</P>
	   /// 
	   /// <P>For the proper format of the coprocessor data file, see the
	   /// document entitled "Implementing Secured D-Identification and E-Payment
	   /// Applications using SHA iButtons".  For the format of TMEX file
	   /// structures, see Application Note 114.
	   /// </summary>
	   /// <param name="owc"> The DS1963S used as a coprocessor </param>
	   /// <param name="coprFilename"> The TMEX filename where coprocessor configuration
	   ///        data is stored.  Usually, "COPR.0".
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   ///  </exception>
	   private void setiButton(OneWireContainer18 owc, string coprFilename)
	   {
		  //hold container reference
		  this.ibc = owc;
		  //and address
		  this.address = owc.Address;

		  OWFileInputStream fis = null;
		  try
		  {
			 if (DEBUG)
			 {
				IOHelper.writeLine("opening file: " + coprFilename + " on token: " + owc);
			 }
			 fis = new OWFileInputStream(owc, coprFilename);
		  }
		  catch (OWFileNotFoundException e)
		  {
			 if (DEBUG)
			 {
				Debug.WriteLine(e.ToString());
				Debug.Write(e.StackTrace);
			 }
			 throw new OneWireIOException("Coprocessor service file Not found: " + e);
		  }
		  try
		  {
			 //configures this object from the info in the given stream
			 fromStream(fis);
		  }
		  catch (IOException ioe)
		  {
			 if (DEBUG)
			 {
				Debug.WriteLine(ioe.ToString());
				Debug.Write(ioe.StackTrace);
			 }
			 throw new OneWireException("Bad Data in Coproccessor Service File: " + ioe);
		  }
		  finally
		  {
			 try
			 {
				   fis.close();
			 }
			 catch (IOException)
			 { //well, at least I tried!
	;
			 }
		  }

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (DEBUG)
		  {
			 IOHelper.writeLine("------------------------------------");
			 IOHelper.writeLine("Loaded Coprocessor");
			 IOHelper.writeLine("address");
			 IOHelper.writeBytesHex(owc.Address);
			 IOHelper.writeLine("signPageNumber: " + signPageNumber);
			 IOHelper.writeLine("authPageNumber: " + authPageNumber);
			 IOHelper.writeLine("wspcPageNumber: " + wspcPageNumber);
			 IOHelper.writeLine("serviceFilename");
			 IOHelper.writeBytesHex(filename);
			 IOHelper.writeLine("bindData");
			 IOHelper.writeBytesHex(bindData);
			 IOHelper.writeLine("bindCode");
			 IOHelper.writeBytesHex(bindCode);
			 IOHelper.writeLine("initialSignature");
			 IOHelper.writeBytesHex(initialSignature);
			 IOHelper.writeLine("signingChallenge");
			 IOHelper.writeBytesHex(signingChallenge);
			 IOHelper.writeLine("------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
	   }


	   /*
	   Do I even need modifiers.  The class should only be modified with
	   "setibutton"
	   // ***********************************************************************
	   // Protected Modifiers
	   //  - All modifiers of this class's data are protected.  The reason
	   //    is that all data members are set based on information in the
	   //    Coprocessor's service file.  These member's should never be
	   //    directly updated by User applications, but rather as a result
	   //    to changes in the service file.
	   // ***********************************************************************
	   protected void setSigningPageNumber(int pg)
	   {
	      this.signPageNumber = pg;
	   }
	   protected void setWorkspacePageNumber(int pg)
	   {
	      this.wspcPageNumber = pg;
	   }
	   protected void setAuthenticationPageNumber(int pg)
	   {
	      this.authPageNumber = pg;
	   }
	   protected void setFilename(byte[] l_filename, int offset)
	   {
	      int cnt = Math.min(l_filename.length - offset, 4);
	      System.arraycopy(l_filename, offset, filename, 0, cnt);
	   }
	   protected void setFilenameExt(byte ext)
	   {
	      filename[4] = ext;
	   }
	   protected void setBindData(byte[] l_bindData, int offset)
	   {
	      int cnt = Math.min(l_bindData.length - offset, 32);
	      System.arraycopy(l_bindData, offset, bindData, 0, cnt);
	   }
	   protected void setBindCode(byte[] l_bindCode, int offset)
	   {
	      int cnt = Math.min(l_bindCode.length - offset, 7);
	      System.arraycopy(l_bindCode, offset, bindCode, 0, cnt);
	   }
	   protected synchronized void setSigningChallenge (byte[] chlg, int offset)
	   {
	      int cnt = (chlg.length > (3+offset)) ? 3 : (chlg.length - offset);
	      System.arraycopy(chlg, offset, signingChallenge, 0, cnt);
	   }
	   protected synchronized void setInitialSignature (byte[] sig_ini, int offset)
	   {
	      int cnt = (sig_ini.length > (20+offset)) ? 20 : (sig_ini.length - offset);
	      System.arraycopy(sig_ini, offset, initialSignature, 0, cnt);
	   }*/

	   // ***********************************************************************
	   // Begin Accessor Methods
	   // ***********************************************************************

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
	   /// <P>Returns the page number used by this coprocessor for storing
	   /// system authentication secret and recreating user's authentication
	   /// secret.  The authentication secret stays constant, while the new
	   /// secret is copied on to the secret corresponding to the workspace
	   /// page.</P>
	   /// </summary>
	   /// <returns> page number used for system authentication secret </returns>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual int AuthenticationPageNumber
	   {
		   get
		   {
			  return this.authPageNumber;
		   }
	   }

	   /// <summary>
	   /// <P>Returns a string representing the auxilliary data associated
	   /// with this coprocessor's service.</P>
	   /// </summary>
	   /// <returns> the auxilliary data of this coprocessor's service </returns>
	   public virtual string AuxilliaryData
	   {
		   get
		   {
			  return auxData;
		   }
	   }

	   /// <summary>
	   /// <P>Returns 7 byte binding code for finalizing secret installation
	   /// on user buttons.  This is copied into the user's scratchpad,
	   /// along with 8 other bytes of binding data (see
	   /// <code>bindSecretToiButton</code>) for finalizing the secret
	   /// and making it unique to the button.</P>
	   /// </summary>
	   /// <returns> the binding code for user's scratchpad </returns>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual byte[] BindCode
	   {
		   get
		   {
			  byte[] data = new byte[7];
			  Array.Copy(bindCode, 0, data, 0, 7);
			  return data;
		   }
	   }

	   /// <summary>
	   /// <P>Copies 7 byte binding code for finalizing secret installation
	   /// on user buttons.  This is copied into the user's scratchpad,
	   /// along with 8 other bytes of binding data (see
	   /// <code>bindSecretToiButton</code>) for finalizing the secret
	   /// and making it unique to the button.</P>
	   /// </summary>
	   /// <param name="data"> array for copying the binding code for user's
	   ///        scratchpad </param>
	   /// <param name="offset"> the index at which to start copying. </param>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual void getBindCode(byte[] data, int offset)
	   {
		  Array.Copy(bindCode, 0, data, offset, 7);
	   }

	   /// <summary>
	   /// <P>Returns 32 byte binding data for finalizing secret installation
	   /// on user buttons.  This is copied into the user's account data
	   /// page (see <code>bindSecretToiButton</code>) for finalizing the
	   /// secret and making it unique to the button.</P>
	   /// </summary>
	   /// <returns> the binding data for user's data page </returns>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual byte[] BindData
	   {
		   get
		   {
			  byte[] data = new byte[32];
			  Array.Copy(bindData, 0, data, 0, 32);
			  return data;
		   }
	   }

	   /// <summary>
	   /// <P>Copies 32 byte binding data for finalizing secret installation
	   /// on user buttons.  This is then copied into the user's account data
	   /// page (see <code>bindSecretToiButton</code>) for finalizing the
	   /// secret and making it unique to the button.</P>
	   /// </summary>
	   /// <param name="data"> array for copying the binding data for user's
	   ///        data page </param>
	   /// <param name="offset"> the index at which to start copying. </param>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual void getBindData(byte[] data, int offset)
	   {
		  Array.Copy(bindData, 0, data, offset, 32);
	   }

	   /// <summary>
	   /// <P>Returns an integer representing the encryption code for
	   /// this coprocessor.  No handling of specific encryption codes
	   /// are in place with this API, but could be added easily at
	   /// the <code>SHATransaction</codE> layer.</P>
	   /// </summary>
	   /// <returns> an integer representing type of encryption for user data. </returns>
	   public virtual int EncryptionCode
	   {
		   get
		   {
			  return encCode;
		   }
	   }

	   /// <summary>
	   /// <P>Copies the filename (used for storing account data on user
	   /// buttons) into the specified array starting at the specified
	   /// offset.</P>
	   /// </summary>
	   /// <param name="l_filename"> the array into which the filename will be
	   ///        copied. </param>
	   /// <param name="offset"> the starting index for copying the filename. </param>
	   public virtual void getFilename(byte[] l_filename, int offset)
	   {
		  int cnt = Math.Min(l_filename.Length - offset, 4);
		  Array.Copy(filename, offset, l_filename, offset, cnt);
	   }

	   /// <summary>
	   /// <P>Returns the extension of the filename used for storing account
	   /// data on user buttons.  If the type of this service is an
	   /// e-cash application, the file extension will be decimal 102.</P>
	   /// </summary>
	   /// <returns> proper file extension for user's account data file. </returns>
	   public virtual byte FilenameExt
	   {
		   get
		   {
			  return filename[4];
		   }
	   }

	   /// <summary>
	   /// <P>Returns the 20-byte initial signature used by this coprocessor
	   /// for signing account data.</P>
	   /// </summary>
	   /// <returns> 20-byte initial signature. </returns>
	   public virtual byte[] InitialSignature
	   {
		   get
		   {
			  byte[] data = new byte[20];
			  Array.Copy(initialSignature, 0, data, 0, 20);
			  return data;
		   }
	   }

	   /// <summary>
	   /// <P>Copies the 20-byte initial signature used by this coprocessor
	   /// for signing account data into the specified array starting at the
	   /// specified offset.</P>
	   /// </summary>
	   /// <param name="data"> arry for copying the 20-byte initial signature. </param>
	   /// <param name="offset"> the index at which to start copying. </param>
	   public virtual void getInitialSignature(byte[] data, int offset)
	   {
		  Array.Copy(initialSignature, 0, data, offset, 20);
	   }

	   /// <summary>
	   /// <P>Returns error code matching last error encountered by the
	   /// coprocessor.  An error code of zero implies NO_ERROR.</P>
	   /// </summary>
	   /// <returns> the error code, zero for none. </returns>
	   public virtual int LastError
	   {
		   get
		   {
			  return lastError;
		   }
	   }

	   /// <summary>
	   /// <P>Returns a string representing the Provider name associated
	   /// with this coprocessor's service.</P>
	   /// </summary>
	   /// <returns> the name of the provider's service. </returns>
	   public virtual string ProviderName
	   {
		   get
		   {
			  return providerName;
		   }
	   }

	   /// <summary>
	   /// <P>Returns the 3-byte signing challenge used by this coprocessor
	   /// for data validation.</P>
	   /// </summary>
	   /// <returns> 3-byte challenge </returns>
	   public virtual byte[] SigningChallenge
	   {
		   get
		   {
			  byte[] data = new byte[3];
			  Array.Copy(signingChallenge, 0, data, 0, 3);
			  return data;
		   }
	   }

	   /// <summary>
	   /// <P>Copies the 3-byte signing challenge used by this coprocessor
	   /// for data validation into the specified array starting at
	   /// the specified offset.</P>
	   /// </summary>
	   /// <param name="data"> the array for copying the 3-byte challenge. </param>
	   /// <param name="offset"> the index at which to start copying. </param>
	   public virtual void getSigningChallenge(byte[] data, int offset)
	   {
		  Array.Copy(signingChallenge, 0, data, offset, 3);
	   }

	   /// <summary>
	   /// <P>Returns the page number used by this coprocessor for signing
	   /// account data.</P>
	   /// </summary>
	   /// <returns> page number used for signing </returns>
	   public virtual int SigningPageNumber
	   {
		   get
		   {
			  return this.signPageNumber;
		   }
	   }

	   /// <summary>
	   /// <P>Returns the version number of this service.</P>
	   /// </summary>
	   /// <returns> version number for service. </returns>
	   public virtual int Version
	   {
		   get
		   {
			  return this.version;
		   }
	   }

	   /// <summary>
	   /// <P>Returns the page number used by this coprocessor for regenerating
	   /// the user authentication.  This page is the target page for
	   /// <code>bindSecretToiButton</code> when trying to reproduce a user's
	   /// authentication secret.</P>
	   /// </summary>
	   /// <returns> page number used for regenerating user authentication. </returns>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual int WorkspacePageNumber
	   {
		   get
		   {
			  return this.wspcPageNumber;
		   }
	   }

	   /// <summary>
	   /// <P>Returns a bool indicating whether or not this coprocessor's
	   /// secret's were formatted for compatibility with the DS1961S.</P>
	   /// </summary>
	   /// <returns> <code>true</code> if this coprocessor can authenticate a
	   ///         DS1961S using it's system authentication secret. </returns>
	   /// <seealso cref= #reformatFor1961S(byte[]) </seealso>
	   public virtual bool DS1961Scompatible
	   {
		   get
		   {
			  return DS1961Scompatible_Renamed;
		   }
	   }
	   // ***********************************************************************
	   // End Accessor Methods
	   // ***********************************************************************



	   // ***********************************************************************
	   // Begin SHA iButton Methods
	   // ***********************************************************************


	   /// <summary>
	   /// <P>Given a 32-byte array for page data and a 32-byte array for
	   /// scratchpad content, this function will create a 20-byte signature
	   /// for the data based on SHA-1.  The format of the calculation of the
	   /// data signature is as follows: First 4-bytes of signing secret,
	   /// 32-bytes of accountData, 12 bytes of scratchpad data starting at
	   /// index 8, last 4-bytes of signing secret, 3 bytes of scratchpad data
	   /// starting at index 20, and the rest is padding as specified for
	   /// standard SHA-1.  This is all laid out, in detail, in the DS1963S
	   /// data sheet.</P>
	   /// 
	   /// <P>The resulting 20-byte signature is copied into
	   /// <code>mac_buffer</code> starting at <code>macStart</code>.  If you're
	   /// updating a signature that already exists in the accountData array,
	   /// it is acceptable to call the method like so:
	   /// <code><pre>
	   ///   copr.createDataSignature(accountData, spad, accountData, 8);
	   /// </pre></code>
	   /// assuming that the signature starts at index 8 of the accountData
	   /// array.</p>
	   /// </summary>
	   /// <param name="accountData"> the 32-byte data page for which the signature is
	   ///        generated. </param>
	   /// <param name="signScratchpad"> the 32-byte scratchpad contents for which the
	   ///        signature is generated.  This will contain parameters such
	   ///        as the user's write cycle counter for the page, the user's
	   ///        1-wire address, and the page number where account data is
	   ///        stored. </param>
	   /// <param name="mac_buffer"> used to return the 20-byte signature generated
	   ///        by signing the page using the coprocessor's system signing
	   ///        secret. </param>
	   /// <param name="macStart"> the offset into mac_buffer where copying should start.
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if an error
	   ///         occurred  (use <code>getLastError()</code> for more
	   ///         information on the type of error)
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= OneWireContainer18#SHAFunction(byte,int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public virtual bool createDataSignature(byte[] accountData, byte[] signScratchpad, byte[] mac_buffer, int macStart)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  //maintain local reference to container
		  OneWireContainer18 ibcL = this.ibc;
		  int addr = this.signPageNumber << 5;

		  //now we are ready to make a signature
		  if (!ibcL.writeDataPage(this.signPageNumber, accountData))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_DATA_PAGE_FAILED;
			 return false;
		  }

		  //allow resume access to coprocessor
		  ibcL.useResume(true);

		  //write the signing information to the scratchpad
		  if (!ibcL.writeScratchPad(0, 0, signScratchpad, 0, 32))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_SCRATCHPAD_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//
		  if (DEBUG)
		  {
			 IOHelper.writeLine("-----------------------------------------------------------");
			 IOHelper.writeLine("COPR DEBUG - createDataSignature");
			 IOHelper.write("address: ");
			 IOHelper.writeBytesHex(address, 0, 8);
			 IOHelper.writeLine("speed: " + this.ibc.Adapter.Speed);
			 IOHelper.writeLine("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//

		  //sign that baby!
		  if (ibcL.SHAFunction(OneWireContainer18.SIGN_DATA_PAGE, addr))
		  {
			 //get the MAC from the scratchpad
			 ibcL.readScratchPad(signScratchpad, 0);

			 //copy the MAC into the accountData page
			 Array.Copy(signScratchpad, 8, mac_buffer, macStart, 20);

			 ibcL.useResume(false);
			 return true;
		  }
		  else
		  {
			 this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  }

		  ibcL.useResume(false);
		  return false;
	   }

	   /// <summary>
	   /// <para>Creates a data signature, but instead of using the signing secret,
	   /// it uses the authentication secret, bound for a particular button.</para>
	   /// 
	   /// <P>fullBindCode can be null if the secret is already bound and in
	   /// the signing page.</p>
	   /// </summary>
	   /// <param name="accountData"> the 32-byte data page for which the signature is
	   ///        generated. </param>
	   /// <param name="signScratchpad"> the 32-byte scratchpad contents for which the
	   ///        signature is generated.  This will contain parameters such
	   ///        as the user's write cycle counter for the page, the user's
	   ///        1-wire address, and the page number where account data is
	   ///        stored. </param>
	   /// <param name="mac_buffer"> used to return the 20-byte signature generated
	   ///        by signing the page using the coprocessor's system signing
	   ///        secret. </param>
	   /// <param name="macStart"> the offset into mac_buffer where copying should start. </param>
	   /// <param name="fullBindCode"> used to recreate the user iButton's unique secret
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if an error
	   ///         occurred  (use <code>getLastError()</code> for more
	   ///         information on the type of error)
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= OneWireContainer18#SHAFunction(byte,int) </seealso>
	   /// <seealso cref= #createDataSignature(byte[],byte[],byte[],int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public virtual bool createDataSignatureAuth(byte[] accountData, byte[] signScratchpad, byte[] mac_buffer, int macStart, byte[] fullBindCode)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  //maintain local reference to container
		  OneWireContainer18 ibcL = this.ibc;
		  int page = this.signPageNumber;
		  int addr = page << 5;

		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//
		  if (DEBUG)
		  {
			 IOHelper.writeLine("-----------------------------------------------------------");
			 IOHelper.writeLine("COPR DEBUG - createDataSignatureAuth");
			 IOHelper.writeLine("address:");
			 IOHelper.writeBytesHex(address, 0, 8);
			 IOHelper.writeLine("accountData");
			 IOHelper.writeBytesHex(accountData);
			 IOHelper.writeLine("signScratchpad");
			 IOHelper.writeBytesHex(signScratchpad);
			 IOHelper.writeLine("mac_buffer: ");
			 IOHelper.writeBytesHex(mac_buffer,macStart,20);
			 IOHelper.writeLine("fullBindCode: ");
			 if (fullBindCode != null)
			 {
				IOHelper.writeBytesHex(fullBindCode);
			 }
			 else
			 {
				IOHelper.writeLine("null");
			 }
			 IOHelper.writeLine("-----------------------------------------------------------");
		  }

		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//
		  if (fullBindCode != null)
		  {
			 //recreate the user's secret on the coprocessor.
			 if (!ibcL.bindSecretToiButton(this.authPageNumber, this.bindData, fullBindCode, page & 7))
			 {
				this.lastError = SHAiButtonCopr.BIND_SECRET_FAILED; //bind secret failed
				return false;
			 }
		  }

		  //now we are ready to make a signature
		  if (!ibcL.writeDataPage(this.signPageNumber, accountData))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_DATA_PAGE_FAILED;
			 return false;
		  }

		  //allow resume access to coprocessor
		  ibcL.useResume(true);

		  //write the signing information to the scratchpad
		  if (!ibcL.writeScratchPad(0, 0, signScratchpad, 0, 32))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_SCRATCHPAD_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  //sign that baby!
		  if (ibcL.SHAFunction(OneWireContainer18.SIGN_DATA_PAGE, addr))
		  {
			 //get the MAC from the scratchpad
			 ibcL.readScratchPad(signScratchpad, 0);

			 //copy the MAC into the accountData page
			 Array.Copy(signScratchpad, 8, mac_buffer, macStart, 20);

			 ibcL.useResume(false);
			 return true;
		  }
		  else
		  {
			 this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  }

		  ibcL.useResume(false);
		  return false;
	   }

	   //prevent malloc'ing in the critical path
	   private byte[] generateChallenge_scratchpad = new byte[32];

	   /// <summary>
	   /// <para>Generates a 3 byte random challenge in the iButton, sufficient to be used
	   /// as a challenge to be answered by a User iButton.  The user answers the challenge
	   /// with an authenticated read of it's account data.</para>
	   /// 
	   /// <para>The DS1963S will generate 20 bytes of pseudo random data, though only
	   /// 3 bytes are needed for the challenge.  Programs can add more 'randomness'
	   /// by selecting different bytes from the 20 bytes of random data using the
	   /// <code>offset</code> parameter.</para>
	   /// 
	   /// <para>The random number generator is actually the DS1963S's SHA engine, which requires
	   /// page data to compute a hash.  Select a page number with the <code>page_number</code>
	   /// parameter.</para>
	   /// </summary>
	   /// <param name="offset"> offset into the 20 random bytes to draw random data from
	   ///        (must be in range 0-16) </param>
	   /// <param name="ch"> buffer for the challenge to be returned (must be of length 3 or more) </param>
	   /// <param name="start"> the starting index into array <code>ch</code> to begin copying
	   ///        the challenge bytes.
	   /// </param>
	   /// <returns> <code>true</code> if successful, <code>false</code> if an error
	   ///         occurred  (use <code>getLastError()</code> for more
	   ///         information on the type of error)
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= SHAiButtonUser#readAccountData(byte[],int,byte[],int,byte[],int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public virtual bool generateChallenge(int offset, byte[] ch, int start)
	   {
		   lock (this)
		   {
			  //clear any errors
			  this.lastError = SHAiButtonCopr.NO_ERROR;
        
			  //maintain local reference to container
			  OneWireContainer18 ibcL = this.ibc;
			  byte[] scratchpad = this.generateChallenge_scratchpad;
			  int addr = authPageNumber << 5;
        
			  if (ibcL.eraseScratchPad(authPageNumber))
			  {
				 //ibcL.useResume(true);
        
				 if (ibcL.SHAFunction(OneWireContainer18.COMPUTE_CHALLENGE, addr))
				 {
					//get the mac from the scratchpad
					ibcL.readScratchPad(scratchpad, 0);
        
					//copy the requested 3 return bytes
					Array.Copy(scratchpad, 8 + (offset % 17), ch, start, 3);
        
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
					if (DEBUG)
					{
					   IOHelper.writeLine("-----------------------------------------------------------");
					   IOHelper.writeLine("COPR DEBUG");
					   IOHelper.writeLine("address:");
					   IOHelper.writeLine("speed: " + this.ibc.Adapter.Speed);
					   IOHelper.writeBytesHex(address, 0, 8);
					   IOHelper.writeLine("Challenge:");
					   IOHelper.writeBytesHex(ch, start, 3);
					   ch[start] = 0x01;
					   ch[start + 1] = 0x02;
					   ch[start + 2] = 0x03;
					   IOHelper.writeBytesHex(ch, start, 3);
					   IOHelper.writeLine("-----------------------------------------------------------");
					}
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
					ibcL.useResume(false);
					return true;
				 }
				 else
				 {
					this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
				 }
			  }
			  else
			  {
				 this.lastError = SHAiButtonCopr.ERASE_SCRATCHPAD_FAILED;
			  }
        
			  ibcL.useResume(false);
			  return false;
		   }
	   }


	   /// <summary>
	   /// <para>Determines if a <code>SHAiButtonUser</code> belongs to the system
	   /// defined by this Coprocessor iButton.See the usage example in this
	   /// class for initializing a Coprocessor iButton.</para>
	   /// 
	   /// <para>The first step in user authentication is to recreate the user's
	   /// unique secret on the coprocessor button using
	   /// <code>bindSecretToiButton(int,byte[],byte[],int)</code>.  Then the
	   /// coprocessor signs the pageData to produce a MAC.  If the MAC matches
	   /// that produced by the user, the user belongs to the system.</para>
	   /// 
	   /// <para>The TMEX formatted page with the user's account data is in the
	   /// 32-byte parameter <code>pageData</code>.  If the verification
	   /// is successful, the data data signature must still be verified with
	   /// the <code>verifySignature()</code> method.</para>
	   /// 
	   /// <para>Failure of this method does not necessarily mean that
	   /// the User iButton does not belong to the system.  It is possible that
	   /// a communication disruption here could cause a CRC error that
	   /// would be indistinguishable from a failed authentication.  However,
	   /// repeated attempts should reveal whether it was truly a communication
	   /// problem or a User iButton that does not belong to the system.</para>
	   /// </summary>
	   /// <param name="fullBindCode"> 15-byte binding code used to recreate user iButtons
	   ///        unique secret in the coprocessor. </param>
	   /// <param name="pageData"> 32-byte buffer containing the data page holding the user's
	   ///        account data. </param>
	   /// <param name="scratchpad"> the 32-byte scratchpad contents for which the
	   ///        signature is generated.  This will contain parameters such
	   ///        as the user's write cycle counter for the page, the user's
	   ///        1-wire address, and the page number where account data is
	   ///        stored. </param>
	   /// <param name="verify_mac"> the 20-byte buffer containing the user's authentication
	   ///        response to the coprocessor's challenge.
	   /// </param>
	   /// <returns> <code>true</code> if the operation was successful and the user's
	   ///         MAC matches that generated by the coprocessor.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #generateChallenge(int,byte[],int) </seealso>
	   /// <seealso cref= #verifySignature(byte[],byte[],byte[]) </seealso>
	   /// <seealso cref= OneWireContainer18#bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   /// <seealso cref= OneWireContainer18#SHAFunction(byte,int) </seealso>
	   /// <seealso cref= OneWireContainer18#matchScratchPad(byte[]) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public virtual bool verifyAuthentication(byte[] fullBindCode, byte[] pageData, byte[] scratchpad, byte[] verify_mac, byte authCmd)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  //maintain local reference to container
		  OneWireContainer18 ibcL = this.ibc;
		  int addr = this.wspcPageNumber << 5;
		  int wspc = this.wspcPageNumber;

		  //recreate the user's secret on the coprocessor.
		  if (!ibcL.bindSecretToiButton(this.authPageNumber, this.bindData, fullBindCode, wspc))
		  {
			 this.lastError = SHAiButtonCopr.BIND_SECRET_FAILED; //bind secret failed
			 return false;
		  }

		  ibcL.useResume(true);

		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//
		  if (DEBUG)
		  {
			 IOHelper.writeLine("-----------------------------------------------------------");
			 IOHelper.writeLine("COPR DEBUG - verifyAuthentication");
			 IOHelper.write("address: ");
			 IOHelper.writeBytesHex(address, 0, 8);
			 IOHelper.writeLine("speed: " + this.ibc.Adapter.Speed);
			 IOHelper.writeLine("pageData");
			 IOHelper.writeBytesHex(pageData);
			 IOHelper.writeLine("scratchpad");
			 IOHelper.writeBytesHex(scratchpad);
			 IOHelper.writeLine("authCmd: " + authCmd);
			 IOHelper.writeLine("bindData: ");
			 IOHelper.writeBytesHex(bindData);
			 IOHelper.writeLine("fullBindCode: ");
			 IOHelper.writeBytesHex(fullBindCode);
			 IOHelper.writeLine("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//

		  //write the account data
		  if (!ibcL.writeDataPage(wspc, pageData))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_DATA_PAGE_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  //write the scratchapd data
		  if (!ibcL.writeScratchPad(wspc, 0, scratchpad, 0, 32))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_SCRATCHPAD_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  //generate the MAC
		  if (ibcL.SHAFunction(authCmd, addr))
		  {
			 if (ibcL.matchScratchPad(verify_mac))
			 {
				ibcL.useResume(false);
				return true;
			 }
			 else
			 {
				this.lastError = SHAiButtonCopr.MATCH_SCRATCHPAD_FAILED;
			 }
		  }
		  else
		  {
			 this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  }

		  ibcL.useResume(false);

		  return false;
	   }

	   /// <summary>
	   /// <P>Verifies a User iButton's signed data on this Coprocessor iButton.
	   /// The Coprocessor must recreate the signature based on the data in the
	   /// file and the contents of the given scratchpad, and then match that
	   /// with the signature passed in verify_mac.</P>
	   /// </summary>
	   /// <param name="pageData"> the full 32 byte TMEX file from the User iButton
	   ///        (from <code>verifyAuthentication</code>) with the </param>
	   /// <param name="scratchpad"> the 32-byte scratchpad contents for which the
	   ///        signature is generated.  This will contain parameters such
	   ///        as the user's write cycle counter for the page, the user's
	   ///        1-wire address, and the page number where account data is
	   ///        stored. </param>
	   /// <param name="verify_mac"> the 20-byte buffer containing the signature the user
	   ///        had stored with the account data file.
	   /// </param>
	   /// <returns> <code>true<code> if the data file is valid, <code>false</code>
	   ///         if an error occurred (use <code>getLastError()</code> for more
	   ///         information on the type of error)
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #verifyAuthentication(byte[],byte[],byte[],byte[],byte) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public virtual bool verifySignature(byte[] pageData, byte[] scratchpad, byte[] verify_mac)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  //maintain local reference to container
		  OneWireContainer18 ibcL = this.ibc;
		  int addr = this.signPageNumber << 5;

		  //now we are ready to make a signature
		  if (!ibcL.writeDataPage(this.signPageNumber, pageData))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_DATA_PAGE_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  ibcL.useResume(true);
		  if (!ibcL.writeScratchPad(0, 0, scratchpad, 0, 32))
		  {
			 this.lastError = SHAiButtonCopr.WRITE_SCRATCHPAD_FAILED;
			 ibcL.useResume(false);
			 return false;
		  }

		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//
		  if (DEBUG)
		  {
			 IOHelper.writeLine("-----------------------------------------------------------");
			 IOHelper.writeLine("COPR DEBUG - verifySignature");
			 IOHelper.write("address: ");
			 IOHelper.writeBytesHex(address, 0, 8);
			 IOHelper.writeLine("speed: " + this.ibc.Adapter.Speed);
			 IOHelper.writeLine("-----------------------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\////\\////\\////\\////\\////\\////\\//

		  //sign that baby!
		  if (ibcL.SHAFunction(OneWireContainer18.VALIDATE_DATA_PAGE, addr))
		  {
			 if (ibcL.matchScratchPad(verify_mac))
			 {
				ibcL.useResume(false);
				return true;
			 }
			 else
			 {
				this.lastError = SHAiButtonCopr.MATCH_SCRATCHPAD_FAILED;
			 }
		  }
		  else
		  {
			 this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  }

		  ibcL.useResume(false);
		  return false;
	   }

	   // ***********************************************************************
	   // End SHA iButton Methods
	   // ***********************************************************************


	   /// <summary>
	   /// Returns a string representing the 1-Wire address of this SHAiButton.
	   /// </summary>
	   /// <returns> a string containing the 8-byte address of this 1-Wire device. </returns>
	   public override string ToString()
	   {
		  return "COPR: " + this.ibc.AddressAsString + ", provider: " + this.providerName + ", version: " + this.version;
	   }


	   /// <summary>
	   /// Configuration helper.  Used also by Coprocessor VM
	   /// </summary>
	   protected internal virtual void fromStream(System.IO.Stream @is)
	   {
#if false //TODO
		  @is.Read(this.filename, 0,5);

		  this.signPageNumber = @is.Read();
		  this.authPageNumber = @is.Read();
		  this.wspcPageNumber = @is.Read();

		  this.version = @is.Read();

		  @is.Seek(4, SeekOrigin.Current); //skip date info

		  @is.Read(this.bindData,0,32);
		  @is.Read(this.bindCode,0,7);
		  @is.Read(this.signingChallenge,0,3);

		  int namelen = @is.Read();
		  int siglen = @is.Read();
		  int auxlen = @is.Read();

		  byte[] l_providerName = new byte[namelen];
		  @is.Read(l_providerName, 0, l_providerName.Length);
		  this.providerName = System.Text.Encoding.Unicode.GetString(l_providerName);

		  int cnt = Math.Min(this.initialSignature.Length, siglen);
		  @is.Read(this.initialSignature,0,cnt);

		  byte[] l_auxData = new byte[auxlen];
		  @is.Read(l_auxData, 0, l_auxData.Length);
		  this.auxData = System.Text.Encoding.Unicode.GetString(l_auxData);

		  this.encCode = @is.Read();
		  this.DS1961Scompatible_Renamed = (@is.Read() != 0);
#endif
        }

        /// <summary>
        /// Configuration saving method. Used also by Coprocessor VM
        /// </summary>
        protected internal virtual void toStream(System.IO.Stream os)
	   {
#if false
          //first part written is completely standard format
          os.Write(this.filename,0,5);
		  os.WriteByte(this.signPageNumber);
		  os.WriteByte(this.authPageNumber);
		  os.WriteByte(this.wspcPageNumber);
		  os.WriteByte(this.version);

		  //month, date, and year ignored
		  os.WriteByte(1);
		  os.WriteByte(1);
		  os.WriteByte(0);
		  os.WriteByte(100);

		  os.Write(this.bindData, 0, this.bindData.Length);
		  os.Write(this.bindCode, 0, this.bindCode.Length);
		  os.Write(this.signingChallenge, 0, this.signingChallenge.Length);

		  byte[] l_providerName = this.providerName.GetBytes();
		  byte[] l_auxData = this.auxData.GetBytes();
		  os.WriteByte((byte)l_providerName.Length);
		  os.WriteByte((byte)this.initialSignature.Length);
		  os.WriteByte((byte)l_auxData.Length);
		  os.Write(l_providerName,0,(byte)l_providerName.Length);
		  os.Write(this.initialSignature,0,(byte)this.initialSignature.Length);
		  os.Write(l_auxData,0,(byte)l_auxData.Length);
		  os.WriteByte(this.encCode);
		  os.WriteByte(this.DS1961Scompatible_Renamed?0x55:0x00);

		  os.Flush();
#endif
	   }


	   // ***********************************************************************
	   // Begin Static Utility Methods
	   // ***********************************************************************

	   /// <summary>
	   /// <P>Static method that reformats the inputted authentication secret
	   /// so it is compatible with the DS1961S.  This means that for every
	   /// group of 47 bytes in the secret, bytes at indices 32-35 and indices
	   /// 44-46 are all set to 0xFF.  Check the format for secret generation
	   /// in the DS1961S data sheet to verify format of digest buffer.</P>
	   /// 
	   /// <P>Note that if a coprocessor button uses this formatted secret,
	   /// this function should be called for all user buttons including the
	   /// DS1963S and DS1961S to ensure compatibility</P>
	   /// </summary>
	   /// <param name="auth_secret"> the authentication secret to be reformatted.
	   /// </param>
	   /// <returns> a reformatted authentication secret, with the appropriate
	   ///         padding for DS1961S interaction. </returns>
	   public static byte[] reformatFor1961S(byte[] auth_secret)
	   {
		  int numPartials = (auth_secret.Length / 47) + 1;
		  byte[] new_secret = new byte[47 * numPartials];

		  for (int i = 0; i < numPartials; i++)
		  {
			 int cnt = Math.Min(auth_secret.Length - (i * 47), 47);
			 Array.Copy(auth_secret, i * 47, new_secret, i * 47, cnt);
			 new_secret[i * 47 + 32] = 0xFF;
			 new_secret[i * 47 + 33] = 0xFF;
			 new_secret[i * 47 + 34] = 0xFF;
			 new_secret[i * 47 + 35] = 0xFF;
			 new_secret[i * 47 + 44] = 0xFF;
			 new_secret[i * 47 + 45] = 0xFF;
			 new_secret[i * 47 + 46] = 0xFF;
		  }
		  return new_secret;
	   }
	}


}