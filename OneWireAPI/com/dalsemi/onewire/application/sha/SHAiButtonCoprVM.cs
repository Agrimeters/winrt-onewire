using System;
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

using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.file;
using com.dalsemi.onewire.container;
using com.dalsemi.onewire.utils;

namespace com.dalsemi.onewire.application.sha
{

    /// <summary>
    /// <P>Class for simulating an instance of a SHA iButton Coprocessor involved
    /// in SHA Transactions.  The Coprocessor is used for digitally signing transaction
    /// data as well as generating random challenges for users and verifying
    /// their response.</P>
    /// 
    /// <para>With this class, no DS1963S SHA iButton is necessary for the coprocessor in
    /// SHA Transactions.  The simulated Coprocessor iButton verifies signatures
    /// and signs data for User iButtons.</P>
    /// 
    /// </para>
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
    /// <seealso cref= SHAiButtonUser </seealso>
    /// <seealso cref= SHAiButtonCopr
    /// 
    /// @version 1.00
    /// @author  SKH </seealso>
    public class SHAiButtonCoprVM : SHAiButtonCopr
    {
       /// <summary>
       /// 8 8-byte Secrets for this simulated SHAiButton
       /// </summary>
       //ORIGINAL LINE: protected internal byte[][] secretPage = new byte[8][8];
       protected internal byte[][] secretPage;
         //= RectangularArrays.ReturnRectangularByteArray(8, 8);

	   /// <summary>
	   /// 1-Wire Address for this simulated device
	   /// </summary>
	   protected internal new byte[] address = new byte[8];

	   // ***********************************************************************
	   // Transient Data Members
	   // ***********************************************************************

	   //Temporary 512-bit buffer used for digest computation
	   private static readonly byte[] digestBuff = new byte[64];

	   //used for compute first secret
	   private static readonly byte[] NullSecret = new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00};

	   //used for generate challenge and random RomID
	   private static Random rand = new Random();

	   // ***********************************************************************
	   // Class Constructors
	   // ***********************************************************************

	   /// <summary>
	   /// <P>Sets up this simulated coprocessor based on the provided parameters.
	   /// Then, the system secret and authentication secret are installed on the
	   /// simulated coprocessor iButton.</P>
	   /// 
	   /// <P>For the proper format of the coprocessor data file, see the
	   /// document entitled "Implementing Secured D-Identification and E-Payment
	   /// Applications using SHA iButtons".  For the format of TMEX file
	   /// structures, see Application Note 114.</P>
	   /// </summary>
	   /// <param name="RomID"> The address for the simulated coprocessor. </param>
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
	   /// <seealso cref= #SHAiButtonCoprVM(String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer18,String,byte[],byte[]) </seealso>
	   public SHAiButtonCoprVM(byte[] RomID, int l_signPageNumber, int l_authPageNumber, int l_wspcPageNumber, int l_version, int l_encCode, byte l_serviceFileExt, byte[] l_serviceFilename, byte[] l_providerName, byte[] l_bindData, byte[] l_bindCode, byte[] l_auxData, byte[] l_initialSignature, byte[] l_signingChlg, byte[] l_signingSecret, byte[] l_authSecret)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  //set up all the appropriate members
		  Array.Copy(RomID,0,this.address,0,8);
		  this.signPageNumber = l_signPageNumber;
		  this.authPageNumber = l_authPageNumber;
		  this.wspcPageNumber = l_wspcPageNumber;
		  this.version = l_version;
		  this.encCode = l_encCode;
		  Array.Copy(l_serviceFilename,0,this.filename,0,4);
		  this.filename[4] = l_serviceFileExt;
		  this.providerName = Encoding.UTF8.GetString(l_providerName);
		  Array.Copy(l_bindData,0,this.bindData,0,32);
		  Array.Copy(l_bindCode,0,this.bindCode,0,7);
		  this.auxData = Encoding.UTF8.GetString(l_auxData);
		  Array.Copy(l_initialSignature,0,this.initialSignature,0,20);
		  Array.Copy(l_signingChlg,0,this.signingChallenge,0,3);

          secretPage = new byte[8][]
          {
              new byte[8], new byte[8], new byte[8], new byte[8],
              new byte[8], new byte[8], new byte[8], new byte[8]
          };


          //Check to see if this coprocessor's authentication secret
          //is appropriately padded to be used with a DS1961S
            this._DS1961Scompatible = ((l_authSecret.Length % 47) == 0);
		  int secretDiv = l_authSecret.Length / 47;
		  for (int j = 0; j < secretDiv && _DS1961Scompatible; j++)
		  {
			 int offset = 47 * j;
			 for (int i = 32; i < 36 && this._DS1961Scompatible; i++)
			 {
				this._DS1961Scompatible = (l_authSecret[i + offset] == 0x0FF);
			 }
			 for (int i = 44; i < 47 && this._DS1961Scompatible; i++)
			 {
				this._DS1961Scompatible = (l_authSecret[i + offset] == 0x0FF);
			 }
		  }

		  //Install the system signing secret, used to sign and validate all user data
		  if (!installMasterSecret(signPageNumber, l_signingSecret, signPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install system signing secret");
		  }

		  //Install the system authentication secret, used to authenticate users
		  if (!installMasterSecret(authPageNumber, l_authSecret, authPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install authentication secret");
		  }
	   }

	   /// <summary>
	   /// <para>Loads a simulated DS1963S coprocessor device from disk.  The given
	   /// file name is loaded to get all the parameters of the coprocessor.
	   /// It is assumed that the secrets were stored in the file when
	   /// the simulated coprocessor's data was saved to disk.</para>
	   /// </summary>
	   /// <param name="filename"> The filename of the simulated coprocessor's data file ("shaCopr.dat")
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCoprVM(String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer18,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(byte[],int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[])
	   ///  </seealso>
	   public SHAiButtonCoprVM(string filename)
	   {
		  if (!load(filename))
		  {
			 throw new OneWireIOException("failed to load config info");
		  }
	   }

	   /// <summary>
	   /// <para>Loads a simulated DS1963S coprocessor device from disk.  The given
	   /// file name is loaded to get all the parameters of the coprocessor.
	   /// After it is loaded, the given secrets are installed.</para>
	   /// </summary>
	   /// <param name="filename"> The filename of the simulated coprocessor's data file ("shaCopr.dat") </param>
	   /// <param name="sign_secret"> The system data signing secret. </param>
	   /// <param name="auth_secret"> The system device authentication secret.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCoprVM(String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer18,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(byte[],int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   public SHAiButtonCoprVM(string filename, byte[] sign_secret, byte[] auth_secret)
	   {
		  if (!load(filename))
		  {
			 throw new OneWireIOException("failed to load config info");
		  }
		  if (!installMasterSecret(signPageNumber, sign_secret, signPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install system signing secret");
		  }
		  if (!installMasterSecret(authPageNumber, auth_secret, authPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install authentication secret");
		  }
	   }

	   /// <summary>
	   /// <para>Loads a simulated DS1963S coprocessor device from any 1-Wire memory device
	   /// supported by the 1-Wire File I/O API.  The given file name is loaded to get
	   /// all the parameters of the coprocessor.  It is assumed that the secrets were
	   /// stored in the file when the simulated coprocessor's data was saved to disk.</para>
	   /// </summary>
	   /// <param name="owc"> 1-Wire memory device with valid TMEX file system </param>
	   /// <param name="filename"> The filename of the simulated coprocessor's data file ("shaCopr.dat")
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCoprVM(String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer18,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(byte[],int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   public SHAiButtonCoprVM(OneWireContainer owc, string filename)
	   {
		  if (!load(owc,filename))
		  {
			 throw new OneWireIOException("failed to load config info");
		  }
	   }

	   /// <summary>
	   /// <para>Loads a simulated DS1963S coprocessor device from any 1-Wire
	   /// memory device supported by the 1-Wire File I/O API.  The given
	   /// file name is loaded to get all the parameters of the coprocessor.
	   /// After it is loaded, the given secrets are installed.</para>
	   /// </summary>
	   /// <param name="owc"> 1-Wire memory device with valid TMEX file system </param>
	   /// <param name="filename"> The filename of the simulated coprocessor's data file ("shaCopr.dat") </param>
	   /// <param name="sign_secret"> The system data signing secret. </param>
	   /// <param name="auth_secret"> The system device authentication secret.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCoprVM(String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer18,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(byte[],int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   public SHAiButtonCoprVM(OneWireContainer owc, string filename, byte[] sign_secret, byte[] auth_secret)
	   {
		  if (!load(owc,filename))
		  {
			 throw new OneWireIOException("failed to load config info");
		  }
		  if (!installMasterSecret(signPageNumber, sign_secret, signPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install system signing secret");
		  }
		  if (!installMasterSecret(authPageNumber, auth_secret, authPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install authentication secret");
		  }
	   }

	   /// <summary>
	   /// <para>Simulates a specific DS1963S coprocessor device.  First, the given
	   /// TMEX file name is loaded of the container to get all the parameters of
	   /// the coprocessor.  Then (since secrets are not readable off the iButton,
	   /// they must be provided) the secrets are installed on the virtual
	   /// coprocessor.</para>
	   /// </summary>
	   /// <param name="owc"> The coprocessor button this VM will simulate. </param>
	   /// <param name="filename"> The TMEX filename of the coprocessor service file ("COPR.0") </param>
	   /// <param name="sign_secret"> The system data signing secret. </param>
	   /// <param name="auth_secret"> The system device authentication secret.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <seealso cref= #SHAiButtonCoprVM(String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(OneWireContainer,String,byte[],byte[]) </seealso>
	   /// <seealso cref= #SHAiButtonCoprVM(byte[],int,int,int,int,int,byte,byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[],byte[]) </seealso>
	   public SHAiButtonCoprVM(OneWireContainer18 owc, string filename, byte[] sign_secret, byte[] auth_secret)
	   {
		  if (!load(owc,filename))
		  {
			 throw new OneWireIOException("failed to load config info");
		  }
		  if (!installMasterSecret(signPageNumber, sign_secret, signPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install system signing secret");
		  }
		  if (!installMasterSecret(authPageNumber, auth_secret, authPageNumber & 7))
		  {
			 throw new OneWireIOException("failed to install authentication secret");
		  }
	   }

	   // ***********************************************************************
	   // End Constructors
	   // ***********************************************************************

	   // ***********************************************************************
	   // Save and Load methods for serializing all data
	   // ***********************************************************************

	   /// <summary>
	   /// <para>Saves simulated coprocessor configuration info to an (almost)
	   /// standard-format to a hard drive file.</para>
	   /// </summary>
	   /// <param name="filename"> The filename of the simulated coprocessor's data
	   ///        file ("shaCopr.dat") </param>
	   /// <param name="saveSecretData"> If <code>true</true>, the raw secret information
	   ///        is also written to the file
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <returns> <code>true</code> if the info was successfully saved </returns>
	   public virtual bool save(string filename, bool saveSecretData)
	   {
		  try
		  {
			 //Create the configuration file
			 System.IO.FileStream fos = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write);

			 //write the data out to the config file
			 toStream(fos);

			 //non-standard additions
			 fos.Write(address,0,8);
			 for (int i = 0; i < 8; i++)
			 {
				if (saveSecretData)
				{
				   fos.Write(secretPage[i], 0, 8);  //TODO
				}
				else
				{
				   fos.Write(NullSecret, 0, NullSecret.Length);
				}
			 }
			 fos.Flush();
             fos.Dispose(); //TODO
			 //TODO fos.close();

			 return true;
		  }
		  catch (System.Exception)
		  {
			 return false;
		  }
	   }

	   /// <summary>
	   /// <para>Saves simulated coprocessor configuration info to an (almost)
	   /// standard-format to a 1-Wire Memory Device's TMEX file.</para>
	   /// </summary>
	   /// <param name="owc"> 1-Wire Memory Device with valid TMEX file structure. </param>
	   /// <param name="filename"> The TMEX filename of the simulated coprocessor's data
	   ///        file ("COPR.2") </param>
	   /// <param name="saveSecretData"> If <code>true</true>, the raw secret information
	   ///        is also written to the file.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
	   ///         reading an incorrect CRC from a 1-Wire device.  This could be
	   ///         caused by a physical interruption in the 1-Wire Network due to
	   ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
	   /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
	   ///         adapter
	   /// </exception>
	   /// <returns> <code>true</code> if the info was successfully saved </returns>
	   public virtual bool save(OneWireContainer owc, string filename, bool saveSecretData)
	   {
		  try
		  {
			 //Create the configuration file
			 OWFileOutputStream fos = new OWFileOutputStream(owc, filename);

			 //write the data out
			 toStream(fos);

			 //non-standard additions
			 fos.write(address,0,8);
			 for (int i = 0; i < 8; i++)
			 {
				if (saveSecretData)
				{
				   fos.write(secretPage[i]);
				}
				else
				{
				   fos.write(NullSecret);
				}
			 }
			 fos.Flush();
			 fos.close();

			 return true;
		  }
		  catch (System.Exception)
		  {
			 return false;
		  }
	   }

	   /// <summary>
	   /// <para>Loads coprocessor configuration information from an (almost) standard
	   /// service file on hard drive. If secret information was saved, this routine
	   /// automatically loads it.</P>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="filename"> The filename of the simulated coprocessor's data
	   ///        file ("shaCopr.dat")
	   /// </param>
	   /// <returns> <code>true</code> if the info was successfully loaded </returns>
	   public virtual bool load(string filename)
	   {
		  try
		  {
			 //open the file containing config info
			 System.IO.FileStream fis = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);

			 //load info from the file stream
			 fromStream(fis);

			 //non-standard file components
			 if (fis.Length > 0)
			 {
				fis.Read(this.address,0,8);
				for (int i = 0; i < 8 && fis.Length>0; i++)
				{
				   fis.Read(secretPage[i], 0, secretPage[i].Length);
				}
			 }
			 fis.Dispose(); //TODO .Close()
			 return true;
		  }
		  catch (System.Exception)
		  {
			 return false;
		  }
	   }

	   /// <summary>
	   /// <para>Loads coprocessor configuration information from an (almost) standard
	   /// service TMEX file on 1-Wire memory device. If secret information was saved,
	   /// this routine automatically loads it.</P>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="owc"> 1-Wire memory device with valid TMEX file structure </param>
	   /// <param name="filename"> The TMEX filename of the simulated coprocessor's data
	   ///        file ("COPR.2")
	   /// </param>
	   /// <returns> <code>true</code> if the info was successfully loaded </returns>
	   public virtual bool load(OneWireContainer owc, string filename)
	   {
		  try
		  {
			 //open the file containing config info
			 OWFileInputStream fis = new OWFileInputStream(owc,filename);

			 //load info from the file stream
			 fromStream(fis);

			 //non-standard file components
			 if (fis.available() > 0)
			 {
				fis.read(this.address,0,8);
				for (int i = 0; i < 8 && fis.available()>0; i++)
				{
				   fis.read(secretPage[i]);
				}
			 }
			 fis.close();
			 return true;
		  }
		  catch (System.Exception)
		  {
			 return false;
		  }
	   }

	   /// <summary>
	   /// <para>Loads coprocessor configuration information from a standard TMEX
	   /// service file on a DS1963S.</P>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="owc"> DS1963S set up as a valid coprocessor </param>
	   /// <param name="filename"> The TMEX filename of the coprocessor's data
	   ///        file ("COPR.0")
	   /// </param>
	   /// <returns> <code>true</code> if the info was successfully loaded </returns>
	   public virtual bool load(OneWireContainer18 owc, string filename)
	   {
		  try
		  {
			 //open the file containing config info
			 OWFileInputStream fis = new OWFileInputStream(owc,filename);

			 //load info from the file stream
			 fromStream(fis);

			 //non-standard components
			 Array.Copy(owc.Address,0,this.address,0,8);

			 fis.close();
			 return true;
		  }
		  catch (System.Exception e)
		  {
			 Debug.WriteLine(e.ToString());
			 Debug.Write(e.StackTrace);
			 return false;
		  }
	   }
	   // ***********************************************************************
	   // End Save and Load methods
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
	   public override bool createDataSignature(byte[] accountData, byte[] signScratchpad, byte[] mac_buffer, int macStart)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  if (SHAFunction(OneWireContainer18.SIGN_DATA_PAGE, secretPage[signPageNumber & 7], accountData, signScratchpad, null, signPageNumber, -1))
		  {
			 Array.Copy(signScratchpad, 8, mac_buffer, macStart, 20);
			 return true;
		  }

		  this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  return false;
	   }

	   //prevent malloc'ing in the critical path
	   private byte[] generateChallenge_chlg = new byte[20];

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
	   public override bool generateChallenge(int offset, byte[] ch, int start)
	   {
		   lock (this)
		   {
			  //clear any errors
			  this.lastError = SHAiButtonCopr.NO_ERROR;
        
			  SHAiButtonCoprVM.rand.NextBytes(this.generateChallenge_chlg);
        
			  Array.Copy(this.generateChallenge_chlg,offset, ch,start, 3);
        
			  return true;
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
	   /// <seealso cref= #bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   /// <seealso cref= OneWireContainer18#SHAFunction(byte,int) </seealso>
	   /// <seealso cref= OneWireContainer18#matchScratchPad(byte[]) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public override bool verifyAuthentication(byte[] fullBindCode, byte[] pageData, byte[] scratchpad, byte[] verify_mac, byte authCmd)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;
		  int secretNum = this.wspcPageNumber & 7;

		  //set Workspace Secret
		  bindSecretToiButton(authPageNumber, this.bindData, fullBindCode, secretNum);

		  if (SHAFunction(authCmd, secretPage[secretNum], pageData, scratchpad, null, wspcPageNumber, -1))
		  {
			 for (int i = 0; i < 20; i++)
			 {
				if (scratchpad[i + 8] != verify_mac[i])
				{
				   this.lastError = SHAiButtonCopr.MATCH_SCRATCHPAD_FAILED;
				   return false;
				}
			 }
			 return true;
		  }
		  this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  return false;

	   }

	   /// <summary>
	   /// <para>Creates a data signature, but instead of using the signing secret,
	   /// it uses the authentication secret, bound for a particular button.</para>
	   /// 
	   /// <P><code>fullBindCode</code> is ignored by the Coprocessor VM.  Instead
	   /// of binding the secret to the signing page, the coprocessor VM "cheats"
	   /// and lets you sign the workspace page, where (presumably) the secret is
	   /// already bound.</p>
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
	   /// <param name="fullBindCode"> ignored by simulated coprocessor
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
	   public override bool createDataSignatureAuth(byte[] accountData, byte[] signScratchpad, byte[] mac_buffer, int macStart, byte[] fullBindCode)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  if (SHAFunction(OneWireContainer18.SIGN_DATA_PAGE, secretPage[wspcPageNumber & 7], accountData, signScratchpad, null, signPageNumber, -1))
		  {
			 Array.Copy(signScratchpad, 8, mac_buffer, macStart, 20);
			 return true;
		  }

		  this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
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
	   public override bool verifySignature(byte[] pageData, byte[] scratchpad, byte[] verify_mac)
	   {
		  //clear any errors
		  this.lastError = SHAiButtonCopr.NO_ERROR;

		  if (SHAFunction(OneWireContainer18.VALIDATE_DATA_PAGE, this.secretPage[signPageNumber & 7], pageData, scratchpad, this.address, signPageNumber, -1))
		  {
			 for (int i = 0; i < 20; i++)
			 {
				if (scratchpad[i + 8] != verify_mac[i])
				{
				   this.lastError = SHAiButtonCopr.MATCH_SCRATCHPAD_FAILED;
				   return false;
				}
			 }
			 return true;
		  }
		  this.lastError = SHAiButtonCopr.SHA_FUNCTION_FAILED;
		  return false;
	   }

	   private byte[] bindSecretToiButton_scratchpad = new byte[32];
	   /// <summary>
	   /// <para>Binds an installed secret to this virtual DS1963S by using
	   /// well-known binding data and this DS1963S's (unique?)
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
	   /// <seealso cref= #installMasterSecret(int,byte[],int) </seealso>
	   public virtual bool bindSecretToiButton(int pageNum, byte[] bindData, byte[] bindCode, int secretNum)
	   {
		   lock (this)
		   {
			  //local vars
			  byte[] scratchpad = this.bindSecretToiButton_scratchpad;
        
			  //write the bind_code to the scratchpad
			  if (bindCode.Length == 7)
			  {
				 Array.Copy(bindCode,0,scratchpad,8,4);
				 scratchpad[12] = (byte)pageNum;
				 Array.Copy(this.address,0,scratchpad,13,7);
				 Array.Copy(bindCode,4,scratchpad,20,3);
			  }
			  else
			  {
				 Array.Copy(bindCode, 0, scratchpad, 8, (bindCode.Length > 15 ? 15 : bindCode.Length));
			  }
        
			  //compute the MAC
			  if (!SHAFunction(OneWireContainer18.COMPUTE_NEXT_SECRET, secretPage[pageNum & 7], bindData, scratchpad, null, pageNum, 0))
			  {
				 return false;
			  }
        
			  //install the secret
			  Array.Copy(scratchpad,0,secretPage[secretNum & 7],0,8);
        
			  return true;
		   }
	   }



	   /// <summary>
	   /// <para>Installs a secret on this virtual DS1963S.  The secret is written in partial phrases
	   /// of 47 bytes (32 bytes to a memory page, 15 bytes to the scratchpad) and
	   /// is cumulative until the entire secret is processed.  Secrets are associated
	   /// with a page number.  See the datasheet for more information on this
	   /// association.</para>
	   /// 
	   /// <para>In most cases, <code>page</code> should be equal to <code>secret_number</code>
	   /// or <code>secret_number+8</code>, based on the association of secrets and page numbers.
	   /// A secret is 8 bytes and there are 8 secrets.  These 8 secrets are associated with the
	   /// first 16 pages of memory.</para>
	   /// 
	   /// <para>On TINI, this method will be slightly faster if the secret's length is divisible by 47.
	   /// However, since secret key generation is a part of initialization, it is probably
	   /// not necessary.</para>
	   /// </summary>
	   /// <param name="page"> the page number used to write the partial secrets to </param>
	   /// <param name="secret"> the entire secret to be installed </param>
	   /// <param name="secret_number"> the secret 'page' to use (0 - 7)
	   /// </param>
	   /// <returns> <code>true</code> if successful
	   /// </returns>
	   /// <seealso cref= #bindSecretToiButton(int,byte[],byte[],int) </seealso>
	   public virtual bool installMasterSecret(int pageNum, byte[] secret, int secretNum)
	   {
		  //47 is a magic number here because every time a partial secret
		  //is to be computed, 32 bytes goes in the page and 15 goes in
		  //the scratchpad, so it's going to be easier in the computations
		  //if i know the input buffer length is divisible by 47
		  if (secret.Length == 0)
		  {
			 return false;
		  }

		  byte[] input_secret = null;
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
			 input_secret = new byte [secret.Length + (47 - secret_mod_length)];

			 Array.Copy(secret, 0, input_secret, 0, secret.Length);
		  }

		  //the current offset into the input_secret buffer
		  secretNum = secretNum & 7;
		  int offset = 0;
		  byte cmd = OneWireContainer18.COMPUTE_FIRST_SECRET;
		  byte[] scratchpad = new byte[32];
		  byte[] dataPage = new byte[32];
		  while (offset < input_secret.Length)
		  {
			 for (int i = 0; i < 32; i++)
			 {
				scratchpad[i] = 0xFF;
			 }

			 Array.Copy(input_secret,offset,dataPage,0,32);
			 Array.Copy(input_secret,offset + 32,scratchpad,8,15);
			 if (!SHAFunction(cmd, secretPage[pageNum & 7], dataPage, scratchpad, null, signPageNumber, 0))
			 {
				return false;
			 }

			 //install the secret
			 Array.Copy(scratchpad,0,secretPage[secretNum],0,8);

			 offset += 47;
			 cmd = OneWireContainer18.COMPUTE_NEXT_SECRET;
		  }

		  return true;
	   }
	   /// <summary>
	   /// <para>Performs one of the DS1963S's cryptographic functions on this
	   /// virtual SHA iButton.  See the datasheet for more information on
	   /// these functions.</para>
	   /// 
	   /// <para>Valid parameters for the <code>function</code> argument are:
	   /// <ul>
	   ///    <li> COMPUTE_FIRST_SECRET    </li>
	   ///    <li> COMPUTE_NEXT_SECRET     </li>
	   ///    <li> VALIDATE_DATA_PAGE      </li>
	   ///    <li> SIGN_DATA_PAGE          </li>
	   ///    <li> COMPUTE_CHALLENGE       </li>
	   ///    <li> AUTH_HOST               </li>
	   /// </ul></para>
	   /// </summary>
	   /// <param name="function"> the SHA function code </param>
	   /// <param name="shaSecret"> the secret used in SHA caclulation </param>
	   /// <param name="shaPage"> the 32-byte page used in SHA caculation </param>
	   /// <param name="scratchpad"> the 32-byte scratchpad data used in SHA caculation.
	   ///        MAC is returned in this buffer starting at offset 8, unless
	   ///        the function is COMPUTE_FIRST_SECRET or COMPUTE_NEXT_SECRET,
	   ///        when the 4-byte parts E and D are repeated throughout the
	   ///        scratchpad, starting at offset zero. </param>
	   /// <param name="romID"> 1-Wire address.  Only necessary for a
	   ///        READ_AUTHENTICATED_PAGE command and COMPUTE_CHALLENGE command. </param>
	   /// <param name="pageNum"> the page number on which the shaPage resides.  only
	   ///        necessary for a READ_AUTHENTICATED_PAGE command and
	   ///        COMPUTE_CHALLENGE command. </param>
	   /// <param name="writeCycleCounter"> the counter is only necessary for a
	   ///        READ_AUTHENTICATED_PAGE command and COMPUTE_CHALLENGE command.
	   /// </param>
	   /// <returns> <code>true</code> if the function successfully completed,
	   ///         <code>false</code> if the operation failed or if invalid
	   ///          command.
	   ///  </returns>
	   private bool SHAFunction(byte function, byte[] shaSecret, byte[] shaPage, byte[] scratchpad, byte[] romID, int pageNum, int writeCycleCounter)
	   {
		   lock (this)
		   {
			  //offset for location in scratchpad to copy the MAC
			  int offset = 8;
        
			  //byte used for the M-X control bits
			  //Since never matching, I assume M bit is never set...
			  //but I'm not confident that won't change if more functionality
			  //is added to this class.
			  byte shaMX = 0x00;
        
			  switch (function)
			  {
        
			  //Compute first secret, compute next secret, validate and sign data page
			  case OneWireContainer18.COMPUTE_FIRST_SECRET:
				 shaSecret = NullSecret;
				  goto case com.dalsemi.onewire.container.OneWireContainer18.COMPUTE_NEXT_SECRET;
			  case OneWireContainer18.COMPUTE_NEXT_SECRET:
				 //starts copying at location zero, for secret placement.
				 //secret is repeated 4 times in scratchpad.
				 offset = 0;
				  goto case com.dalsemi.onewire.container.OneWireContainer18.VALIDATE_DATA_PAGE;
			  case OneWireContainer18.VALIDATE_DATA_PAGE:
			  case OneWireContainer18.SIGN_DATA_PAGE:
				 //M-X-P byte
				 scratchpad[12] = (byte)((scratchpad[12] & 0x3F) | (shaMX & 0xC0));
				 break;
        
			  //Authenticate host
			  case OneWireContainer18.AUTH_HOST:
				 //for authenticate host, X bit is set.
				 shaMX |= 0x40;
				 //M-X-P byte
				 scratchpad[12] = (byte)((scratchpad[12] & 0x3F) | (shaMX & 0xC0));
				 break;
        
			  //compute challenge and read authenticated page
			  case OneWireContainer18.COMPUTE_CHALLENGE:
				 //for Compute_Challenge, X bit is set.
				 shaMX |= 0x40;
				  goto case OneWireContainer18.READ_AUTHENTICATED_PAGE;
			  case OneWireContainer18.READ_AUTHENTICATED_PAGE:
				 //place the write cycle counter into the scratchpad
				 scratchpad[8] = (byte)(writeCycleCounter & 0x0FF);
				 scratchpad[9] = (byte)(((int)((uint)writeCycleCounter >> 8)) & 0x0FF);
				 scratchpad[10] = (byte)(((int)((uint)writeCycleCounter >> 16)) & 0x0FF);
				 scratchpad[11] = (byte)(((int)((uint)writeCycleCounter >> 24)) & 0x0FF);
        
				 //M-X-P byte
				 scratchpad[12] = (byte)((pageNum & 0x0F) | (shaMX & 0xC0));
        
				 //place the RomID into the scratchpad
				 Array.Copy(romID,0,scratchpad,13,7);
				 break;
        
        
			  //Bad function input, can't perform SHA.
			  default:
				 return false;
			  }
        
			  //Set up the 64 byte buffer for computing the digest.
			  Array.Copy(shaSecret,0,digestBuff,0,4);
			  Array.Copy(shaPage,0,digestBuff,4,32);
			  Array.Copy(scratchpad,8,digestBuff,36,12);
			  Array.Copy(shaSecret,4,digestBuff,48,4);
			  Array.Copy(scratchpad,20,digestBuff,52,3);
        
			  //init. digest buffer padding
			  digestBuff[55] = 0x80;
			  for (int i = 56; i < 62; i++)
			  {
				 digestBuff[i] = 0x00;
			  }
			  digestBuff[62] = 0x01;
			  digestBuff[63] = 0xB8;
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------------------------------");
        
				 if (function == OneWireContainer18.VALIDATE_DATA_PAGE)
				 {
					IOHelper.writeLine("Validating data page");
				 }
				 else if (function == OneWireContainer18.AUTH_HOST)
				 {
					IOHelper.writeLine("Authenticating Host");
				 }
				 else if (function == OneWireContainer18.SIGN_DATA_PAGE)
				 {
					IOHelper.writeLine("Signing Data Page");
				 }
				 else if (function == OneWireContainer18.COMPUTE_NEXT_SECRET)
				 {
					IOHelper.writeLine("Computing Next Secret");
				 }
				 else if (function == OneWireContainer18.COMPUTE_FIRST_SECRET)
				 {
					IOHelper.writeLine("Computing FIRST Secret");
				 }
				 else
				 {
					IOHelper.writeLine("SHA Function" + function);
				 }
        
				 IOHelper.writeLine("pageNum: " + pageNum);
				 IOHelper.writeLine("DigestBuffer: ");
				 IOHelper.writeBytesHex(digestBuff);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  //compute the MAC
			  SHA.ComputeSHA(digestBuff,scratchpad,offset);
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
			  if (DEBUG)
			  {
				 IOHelper.writeLine("SHA Result: ");
				 IOHelper.writeBytesHex(scratchpad,offset,20);
				 IOHelper.writeLine("------------------------------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        
			  //is this a secret computation?
			  if (offset == 0)
			  {
				 //Repeat E and D throughout scratchpad, just like hardware
				 //not sure if this is necessary, maybe for NEXT_SECRET?
				 Array.Copy(scratchpad,0,scratchpad,8,8);
				 Array.Copy(scratchpad,0,scratchpad,16,8);
				 Array.Copy(scratchpad,0,scratchpad,24,8);
			  }
        
			  return true;
		   }
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
		  return "COPRVM: " + Encoding.UTF8.GetString(this.address) + ", provider: " + this.providerName + ", version: " + this.version;
	   }
	}

}