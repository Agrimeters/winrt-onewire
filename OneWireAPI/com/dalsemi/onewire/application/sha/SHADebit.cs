using System;

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

	using com.dalsemi.onewire;
	using com.dalsemi.onewire.adapter;
	using com.dalsemi.onewire.utils;

	/// <summary>
	/// <P>This class implements an account debit application for SHA Transactions.
	/// Account data is stored on user iButtons after being digitally signed by
	/// a coprocessor iButton. Account data consists of the following:
	///  <UL>
	///    <LI> 1 byte: Length of the account file</LI>
	///    <LI> 1 byte: Data type code (dynamic or static)</LI>
	///    <LI>20 bytes: Account Data Signature</LI>
	///    <LI> 2 bytes: Money Conversion Factor</LI>
	///    <LI> 3 bytes: Account Balance</LI>
	///    <LI> 2 bytes: Transaction ID</LI>
	///    <LI> 1 byte: File continuation pointer</LI>
	///    <LI> 2 bytes: CRC16 of entire 30 bytes seeded with the page number</LI>
	///    <LI> <B>32 bytes Total</B></LI>
	///  </UL></P>
	/// 
	/// <P>A typical use case for this class might be as follows:
	/// <pre>
	///   OneWireContainer18 coprOWC18 = new OneWireContainer18(adapter,address);
	/// 
	///   //COPR.0 is the filename for coprocessor service data
	///   SHAiButtonCopr copr = new SHAiButtonCopr(coprOWC18,"COPR.0");
	/// 
	///   //Initial amount for new users is $100, and debit amount is 50 cents
	///   SHATransaction trans = new SHADebit(copr, 10000, 50);
	/// 
	///   OneWireContainer18 owc18 = new OneWireContainer18(adapter, userAddress);
	/// 
	///   //The following constructor erases all transaction data from the user and
	///   //installs the system authentication secret on the user iButton.
	///   SHAiButtonUser user = new SHAiButtonUser18(copr, owc18, true, authSecret);
	/// 
	///   //creates account data on iButton
	///   if(trans.setupTransactionData(user))
	///      System.out.println("Account data installed successfully");
	///   else
	///      System.out.println("Account data installation failed");
	/// 
	///   //... ... ... ... ... ... ... ... ... ... ... ... ... ... ... ...
	/// 
	///   //"challenges" user iButton
	///   if(trans.verifyUser(user))
	///   {
	///      System.out.println("User Verified Successfully");
	/// 
	///      //checks data signature and account balance>0
	///      if(trans.verifyTransactionData(user))
	///      {
	///         System.out.println("Account Data Verified Successfully");
	/// 
	///         //debits and writes new data to user iButton
	///         if(trans.executeTransaction(user))
	///         {
	///            System.out.println("Account debited.");
	///            System.out.println("New Balance: " +
	///               trans.getParameter(SHADebit.USER_BALANCE));
	///         }
	///      }
	///   }
	/// 
	///   //... ... ... ... ... ... ... ... ... ... ... ... ... ... ... ...
	/// 
	///   if(trans.getLastError()!=0)
	///   {
	///      System.err.println("Error code: " + trans.getLastError());
	///   }
	/// </pre></P>
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
	/// <seealso cref= SHAiButtonCopr </seealso>
	/// <seealso cref= SHAiButtonUser
	/// 
	/// @version 1.00
	/// @author  SKH </seealso>
	public class SHADebit : SHATransaction
	{
	   /// <summary>
	   /// Used for fast FF copy </summary>
	   private static readonly byte[] ffBlock = new byte[] {0x0FF, 0x0FF, 0x0FF, 0x0FF, 0x0FF, 0x0FF, 0x0FF, 0x0FF};

	   // ************************************************************** //
	   // Type constants
	   // used to update/retrieve parameters of transaction
	   // ************************************************************** //
	   /// <summary>
	   /// Update the amount this transaction will debit </summary>
	   public const int DEBIT_AMOUNT = 0;

	   /// <summary>
	   /// Update the amount for initial account balance </summary>
	   public const int INITIAL_AMOUNT = 1;

	   /// <summary>
	   /// Retrieve the amount for user's current balance </summary>
	   public const int USER_BALANCE = 2;

	   /// <summary>
	   /// indices for fields in user account file </summary>
	   public const int I_FILE_LENGTH = 0;
	   public const int I_DATA_TYPE_CODE = 1;
	   public const int I_SIGNATURE = 2;
	   public const int I_CONVERSION_FACTOR = 22;
	   public const int I_BALANCE = 24;
	   public const int I_TRANSACTION_ID = 27;
	   public const int I_CONTINUATION_PTR = 29;
	   public const int I_FILE_CRC16 = 30;

	   // ************************************************************** //
	   // Member variables
	   // ************************************************************** //
	   /// <summary>
	   /// Amount to debit from user </summary>
	   private int debitAmount;

	   /// <summary>
	   /// Amount to initialize new user with </summary>
	   private int initialAmount;

	   /// <summary>
	   /// Most recent user's account balance </summary>
	   private int userBalance;

	   /// <summary>
	   /// User apps should never call this </summary>
	   protected internal SHADebit()
	   {
		   ;
	   }

	   /// <summary>
	   /// SHADebit constructor.  <code>copr</code> is the SHAiButtonCopr
	   /// that is used to perform this transaction.  After saving a
	   /// reference to the SHA coprocessor, this constructor resets all
	   /// parameters for this type of transaction to their default values.
	   /// </summary>
	   /// <param name="copr"> The coprocessor used for authentication and data
	   ///             signing in this transaction. </param>
	   public SHADebit(SHAiButtonCopr copr) : base(copr)
	   {

		  resetParameters();
	   }

	   /// <summary>
	   /// SHADebit constructor.  <code>copr</code> is the SHAiButtonCopr
	   /// that is used to perform this transaction.  After saving a
	   /// reference to the SHA coprocessor, this constructor resets all
	   /// parameters for this type of transaction to their default values.
	   /// </summary>
	   /// <param name="copr"> The coprocessor used for authentication and data
	   ///             signing in this transaction. </param>
	   public SHADebit(SHAiButtonCopr copr, int initialAmount, int debitAmount) : base(copr)
	   {

		  this.initialAmount = initialAmount;
		  this.debitAmount = debitAmount;
	   }

	   /// <summary>
	   /// <P>Setup account data on a fresh user iButton.  Prior to calling
	   /// setup transaction data, the authentication secret for the iButton
	   /// should already be setup and a directory entry (as well as at least
	   /// an empty placeholder file) should exist for the account data.  If
	   /// you constructed the SHAiButtonUser using
	   /// <code>SHAiButtonUser(SHAiButtonCopr,OneWireContainer18,bool,byte[])</code>
	   /// the secret has been setup for you and you should know call this
	   /// function.  If you try to install the authentication secret after
	   /// creating the account data, you will destroy all account data on the
	   /// iButton.</P>
	   /// 
	   /// <P>You can set the value of the intial account balance by calling
	   /// <code>transaction.setParameter(SHADebit.INITIAL_AMOUNT,10000)</code>
	   /// where the value of the units is in cents (i.e. 10000 = $100).</P>
	   /// 
	   /// <P><B>Flow of action: </B>
	   ///   <ul>
	   ///     <li> Generate generic account page </li>
	   ///     <li> Insert the initial balance into account data </li>
	   ///     <li> Create a signature using coprocessor </li>
	   ///     <li> Insert the digital signature </li>
	   ///     <li> Write account data page to iButton </li>
	   ///   </ul></P>
	   /// </summary>
	   /// <param name="user"> SHAiButtonUser upon which the transaction occurs.
	   /// </param>
	   /// <returns> <code>true</code>if and only if the signature is
	   /// successfully created by the coprocessor AND the data is
	   /// successfully written to the user iButton.
	   /// </returns>
	   /// <seealso cref= SHAiButtonCopr#createDataSignature(byte[],byte[],byte[],int) </seealso>
	   /// <seealso cref= SHAiButtonUser#writeAccountData(byte[],int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public override bool setupTransactionData(SHAiButtonUser user)
	   {
		  //clear any error
		  lastError = NO_ERROR;

		  // not in critical path, so malloc'ing is okay
		  byte[] accountData = new byte[32];

		  return writeTransactionData(user, 0, this.initialAmount, accountData);
	   }

	   //prevent malloc'ing in the critical path
	   private byte[] verifyUser_fullBindCode = new byte[15];
	   private byte[] verifyUser_scratchpad = new byte[32];
	   private byte[] verifyUser_accountData = new byte[32];
	   private byte[] verifyUser_mac = new byte[20];
	   private byte[] verifyUser_chlg = new byte[3];
	   /// <summary>
	   /// <P>Verifies user's authentication response.  User is "authenticated" if
	   /// and only if the digital signature generated the user iButton matches
	   /// the digital signature generated by the coprocessor after the user's
	   /// unique secret has been recreated on the coprocessor.</P>
	   /// 
	   /// <P><B>Flow of action: </B>
	   ///   <ul>
	   ///     <li> Generate 3-byte "challenge" on coprocessor </li>
	   ///     <li> Write challenge to scratchpad of user </li>
	   ///     <li> Read account data page with signature </li>
	   ///     <li> Attempt to match user's signature with the coprocessor </li>
	   ///   </ul></P>
	   /// </summary>
	   /// <param name="user"> SHAiButtonUser upon which the transaction occurs.
	   /// </param>
	   /// <seealso cref= SHAiButtonCopr#generateChallenge(int,byte[],int) </seealso>
	   /// <seealso cref= SHAiButtonCopr#verifyAuthentication(byte[],byte[],byte[],byte[],byte) </seealso>
	   /// <seealso cref= SHAiButtonUser#readAccountData(byte[],int,byte[],int,byte[],int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public override bool verifyUser(SHAiButtonUser user)
	   {
		   lock (this)
		   {
			  //clear any error
			  this.lastError = SHATransaction.NO_ERROR;
        
			  //local vars
			  byte[] fullBindCode = this.verifyUser_fullBindCode;
			  byte[] scratchpad = this.verifyUser_scratchpad;
			  byte[] accountData = this.verifyUser_accountData;
			  byte[] mac = this.verifyUser_mac;
			  byte[] chlg = this.verifyUser_chlg;
        
        
        
			  //Generate random challenge. This must be done on the
			  //coprocessor, otherwise flags aren't setup for VALIDATE_PAGE.
			  if (!copr.generateChallenge(0, chlg, 0))
			  {
				 lastError = COPR_COMPUTE_CHALLENGE_FAILED;
				 return false;
			  }
        
			  //have user answer the challenge
			  int wcc = user.readAccountData(chlg, 0, accountData, 0, mac, 0);
        
			  if (wcc < 0)
			  {
				 if (user.hasWriteCycleCounter())
				 {
					// failed to read account data
					this.lastError = SHATransaction.USER_READ_AUTH_FAILED;
					return false;
				 }
				 Array.Copy(ffBlock, 0, scratchpad, 8, 4);
			  }
			  else
			  {
				 //copy the write cycle counter into scratchpad
				 Convert.toByteArray(wcc, scratchpad, 8, 4);
			  }
        
			  //get the user's fullBindCode, formatted for user device
			  user.getFullBindCode(fullBindCode, 0);
        
			  //get the user address and page num from fullBindCode
			  Array.Copy(fullBindCode, 4, scratchpad, 12, 8);
        
			  //set the same challenge bytes
			  Array.Copy(chlg, 0, scratchpad, 20, 3);
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 IOHelper.writeLine("------------------------------------");
				 IOHelper.writeLine("Verifying user");
				 IOHelper.writeLine("chlg");
				 IOHelper.writeBytesHex(chlg);
				 IOHelper.writeLine("accountData");
				 IOHelper.writeBytesHex(accountData);
				 IOHelper.writeLine("mac");
				 IOHelper.writeBytesHex(mac);
				 IOHelper.writeLine("wcc: " + user.WriteCycleCounter);
				 IOHelper.writeLine("fullBindCode");
				 IOHelper.writeBytesHex(fullBindCode);
				 IOHelper.writeLine("scratchpad");
				 IOHelper.writeBytesHex(scratchpad);
				 IOHelper.writeLine("------------------------------------");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  if (!copr.verifyAuthentication(fullBindCode, accountData, scratchpad, mac, user.AuthorizationCommand))
			  {
				 this.lastError = SHATransaction.COPROCESSOR_FAILURE;
				 return false;
			  }
        
			  return true;
		   }
	   }

	   //prevent malloc'ing in the critical path
	   private byte[] verifyData_fullBindCode = new byte[32];
	   private byte[] verifyData_scratchpad = new byte[32];
	   private byte[] verifyData_accountData = new byte[32];
	   private byte[] verifyData_mac = new byte[20];
	   /// <summary>
	   /// <P>Verifies user's account data.  Account data is "verified" if and
	   /// only if the account balance is greater than zero and the digital
	   /// signature matches the signature recreated by the coprocessor.</P>
	   /// 
	   /// <P><B>Flow of action: </B>
	   ///   <ul>
	   ///     <li> Read the account data from user </li>
	   ///     <li> Extract account balance </li>
	   ///     <li> Debit money from balance </li>
	   ///     <li> Insert the new balance </li>
	   ///     <li> Reset the digital signature </li>
	   ///     <li> Use coprocessor to sign account data </li>
	   ///     <li> Insert the new digital signature </li>
	   ///     <li> Write the account data to the user </li>
	   ///   </ul></P>
	   /// 
	   /// <P>If previous steps have been executed, all "Read" commands on
	   /// the user are reading from cached data.</P>
	   /// </summary>
	   /// <param name="user"> SHAiButtonUser upon which the transaction occurs.
	   /// </param>
	   /// <returns> <code>true</code> if and only if the account balance is
	   /// greater than zero and digital signature matches the signature
	   /// recreated by the coprocessor.
	   /// </returns>
	   /// <seealso cref= SHAiButtonUser#readAccountData(byte[],int) </seealso>
	   /// <seealso cref= SHAiButtonCopr#verifySignature(byte[],byte[],byte[]) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public override bool verifyTransactionData(SHAiButtonUser user)
	   {
		   lock (this)
		   {
			  //clear any error
			  this.lastError = NO_ERROR;
        
			  //init local vars
			  byte[] scratchpad = this.verifyData_scratchpad;
			  byte[] accountData = this.verifyData_accountData;
			  byte[] verify_mac = this.verifyData_mac;
        
			  //if verifyUser was called, this is a read of cached data
			  int wcc = user.WriteCycleCounter;
        
			  //if verifyUser was called, this is a read of cached data
			  user.readAccountData(accountData,0);
        
			  //get the user's balance from accountData
			  int balance = Convert.toInt(accountData, I_BALANCE, 3);
			  if (balance < 0)
			  {
				 this.lastError = USER_BAD_ACCOUNT_DATA;
				 return false;
			  }
			  this.userBalance = balance;
        
			  //get the mac from the account data page
			  Array.Copy(accountData, I_SIGNATURE, verify_mac, 0, 20);
        
			  //now lets reset the mac
			  copr.getInitialSignature(accountData, I_SIGNATURE);
			  //and reset the CRC
			  accountData[I_FILE_CRC16 + 0] = 0;
			  accountData[I_FILE_CRC16 + 1] = 0;
        
			  //now we also need to get things like wcc, user_page_number, user ID
			  if (wcc < 0)
			  {
				 if (user.hasWriteCycleCounter())
				 {
					// failed to read account data
					this.lastError = USER_READ_AUTH_FAILED;
					return false;
				 }
				 //has no write cycle counter
				 Array.Copy(ffBlock, 0, scratchpad, 8, 4);
			  }
			  else
			  {
				 //copy the write cycle counter into scratchpad
				 Convert.toByteArray(wcc, scratchpad, 8, 4);
			  }
			  scratchpad[12] = (byte)user.AccountPageNumber;
			  user.getAddress(scratchpad, 13, 7);
        
			  copr.getSigningChallenge(scratchpad, 20);
        
			  if (!copr.verifySignature(accountData, scratchpad, verify_mac))
			  {
				 this.lastError = COPROCESSOR_FAILURE;
				 return false;
			  }
        
			  return true;
		   }
	   }

	   //prevent malloc'ing in critical path
	   private byte[] executeTransaction_accountData = new byte[32];
	   private byte[] executeTransaction_oldAcctData = new byte[32];
	   private byte[] executeTransaction_newAcctData = new byte[32];
	   //private byte[] executeTransaction_scratchpad = new byte[32];
	   /// <summary>
	   /// <P>Performs the signed debit, subtracting the debit amount from
	   /// the user's balance and storing the new, signed account data on the
	   /// user's iButton.  The debit amount can be set using
	   /// <code>transaction.setParameter(SHADebit.DEBIT_AMOUNT, 50)</code>,
	   /// where the value is in units of cents (i.e. for 1 dollar, use 100).</P>
	   /// 
	   /// <P><B>Flow of action: </B>
	   ///   <ul>
	   ///     <li> Read the account data from user </li>
	   ///     <li> Extract account balance </li>
	   ///     <li> Assert balance greater than debit amount </li>
	   ///     <li> Debit money from balance </li>
	   ///     <li> Insert the new balance </li>
	   ///     <li> Reset the digital signature </li>
	   ///     <li> Use coprocessor to sign account data </li>
	   ///     <li> Insert the new digital signature </li>
	   ///     <li> Write the account data to the user </li>
	   ///   </ul></P>
	   /// 
	   /// <P>If previous steps have been executed, all "Read" commands on
	   /// the user are reading from cached data.</P>
	   /// </summary>
	   /// <param name="user"> SHAiButtonUser upon which the transaction occurs.
	   /// </param>
	   /// <returns> <code>true</code> if and only if the user has enough in the
	   /// account balance to perform the requested debit AND a new digital
	   /// signature is successfully created AND the account data has been written
	   /// to the button.
	   /// </returns>
	   /// <seealso cref= SHAiButtonUser#readAccountData(byte[],int) </seealso>
	   /// <seealso cref= SHAiButtonUser#writeAccountData(byte[],int) </seealso>
	   /// <seealso cref= SHAiButtonCopr#createDataSignature(byte[],byte[],byte[],int) </seealso>
	   /// <seealso cref= #getLastError() </seealso>
	   public override bool executeTransaction(SHAiButtonUser user, bool verifySuccess)
	   {
		   lock (this)
		   {
			  //clear any error
			  this.lastError = NO_ERROR;
        
			  //init local vars
			  //holds the working copy of account data
			  byte[] accountData = this.executeTransaction_accountData;
			  //holds the backup copy of account data before writing
			  byte[] oldAcctData = this.executeTransaction_oldAcctData;
			  //holds the account data read back for checking
			  byte[] newAcctData = this.executeTransaction_newAcctData;
			  //just make the transaction ID a random number, so it changes
			  int transID = rand.Next();
        
			  //if verifyUser was called, this is a read of cached data
			  user.readAccountData(accountData,0);
        
			  //before we update the account data array at all, let's make a backup copy
			  Array.Copy(accountData, 0, oldAcctData, 0, 32);
        
			  //get the user's balance from accountData
			  int balance = Convert.toInt(accountData, I_BALANCE, 3);
        
			  //if there are insufficient funds
			  if (this.debitAmount > balance)
			  {
				 this.lastError = USER_BAD_ACCOUNT_DATA;
				 return false;
			  }
        
			  //update the user's balance
			  this.userBalance = (balance - this.debitAmount);
        
			  bool success = writeTransactionData(user, transID, this.userBalance, accountData);
        
			  //if write didn't succeeded or if we need to perform
			  //a verification step anyways, let's double-check what
			  //the user has on the button.
			  if (verifySuccess || !success)
			  {
				 bool dataOK = false;
				 int cnt = MAX_RETRY_CNT;
				 do
				 {
					//calling verify user re-issues a challenge-response
					//and reloads the cached account data in the user object.
					if (verifyUser(user))
					{
					   //compare the user's account data against the working
					   //copy and the backup copy.
					   if (user.readAccountData(newAcctData,0))
					   {
						  bool isOld = true;
						  bool isCur = true;
						  for (int i = 0; i < 32 && (isOld || isCur); i++)
						  {
							 //match the backup
							 isOld = isOld && (newAcctData[i] == oldAcctData[i]);
							 //match the working copy
							 isCur = isCur && (newAcctData[i] == accountData[i]);
						  }
						  if (isOld)
						  {
							 //if it matches the backup copy, we didn't write anything
							 //and the data is still okay, but we didn't do a debit
							 dataOK = true;
							 success = false;
						  }
						  else if (isCur)
						  {
							 dataOK = true;
						  }
						  else
						  {
							 //iBUTTON DATA IS TOTALLY HOSED
							 //keep trying to get account data on the button
							 success = writeTransactionData(user, transID, this.userBalance, accountData);
						  }
					   }
					}
				 } while (!dataOK && ((cnt -= 1) >0)); //TODO
        
				 if (!dataOK)
				 {
					//couldn't fix the data after 255 retries
					IOHelper.writeLine("Catastrophic Failure!");
					success = false;
				 }
			  }
        
			  return success;
		   }
	   }

	   private byte[] writeTransactionData_scratchpad = new byte[32];
	   /// <summary>
	   /// Does the writing of transaction data to the user button as well
	   /// as actually signing the data with the coprocessor.
	   /// </summary>
	   private bool writeTransactionData(SHAiButtonUser user, int transID, int balance, byte[] accountData)
	   {
		  //init local vars
		  SHAiButtonCopr copr = this.copr;
		  int acctPageNum = user.AccountPageNumber;
		  byte[] scratchpad = this.writeTransactionData_scratchpad;

		  // length of the TMEX file - 28 data, 1 cont. ptr
		  accountData[I_FILE_LENGTH] = (byte)29;

		  // transaction ID - 2 data bytes
		  accountData[I_TRANSACTION_ID + 0] = (byte)transID;
		  accountData[I_TRANSACTION_ID + 1] = (byte)((int)((uint)transID >> 8));

		  // conversion factor - 2 data bytes
		  accountData[I_CONVERSION_FACTOR + 0] = 0x8B;
		  accountData[I_CONVERSION_FACTOR + 1] = 0x48;

		  // account balance - 3 data bytes
		  Convert.toByteArray(balance, accountData, I_BALANCE, 3);

		  // initial signature - 20 data bytes
		  copr.getInitialSignature(accountData, I_SIGNATURE);

		  // data type code - dynamic: 0x00, static: 0x01
		  accountData[I_DATA_TYPE_CODE] = 0x01;

		  // continuation pointer for TMEX file
		  accountData[I_CONTINUATION_PTR] = 0x00;

		  // clear out the crc16 - 2 data bytes
		  accountData[I_FILE_CRC16 + 0] = 0x00;
		  accountData[I_FILE_CRC16 + 1] = 0x00;

		  //we need to increment the writeCycleCounter since we will be writing to the part
		  int wcc = user.WriteCycleCounter;
		  if (wcc > 0)
		  {
			 //copy the write cycle counter into scratchpad
			 Convert.toByteArray(wcc + 1, scratchpad, 8, 4);
		  }
		  else
		  {
			 if (user.hasWriteCycleCounter())
			 {
				// failed to read account data
				this.lastError = SHATransaction.USER_READ_AUTH_FAILED;
				return false;
			 }
			 Array.Copy(ffBlock, 0, scratchpad, 8, 4);
		  }

		  // svcPageNumber, followed by address of device
		  scratchpad [12] = (byte)acctPageNum;
		  user.getAddress(scratchpad, 13, 7);

		  // copy in the signing challenge
		  copr.getSigningChallenge(scratchpad, 20);

		  // sign the data, return the mac right into accountData
		  copr.createDataSignature(accountData, scratchpad, accountData, I_SIGNATURE);

		  //after signature make sure to dump in the inverted CRC
		  int crc = ~CRC16.compute(accountData, 0, accountData[I_FILE_LENGTH] + 1, acctPageNum);

		  //set the the crc16 bytes
		  accountData[I_FILE_CRC16 + 0] = (byte)crc;
		  accountData[I_FILE_CRC16 + 1] = (byte)(crc >> 8);

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 IOHelper.writeLine("------------------------------------");
			 IOHelper.writeLine("writing transaction data");
			 IOHelper.writeLine("acctPageNum: " + acctPageNum);
			 IOHelper.writeLine("accountData");
			 IOHelper.writeBytesHex(accountData);
			 IOHelper.writeLine("------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

		  // write it to the button
		  try
		  {
			 if (user.writeAccountData(accountData, 0))
			 {
				return true;
			 }

		  }
		  catch (OneWireException owe)
		  {
			 if (DEBUG)
			 {
				IOHelper.writeLine(owe);
			 }
		  }

		  this.lastError = SHATransaction.USER_WRITE_DATA_FAILED;
		  return false;
	   }

	   /// <summary>
	   /// <P>Retrieves the value of a particular parameter for this
	   /// debit transaction.</P>
	   /// 
	   /// <P><B>Valid Parameters</B>
	   ///   <UL>
	   ///     <LI><code>SHADebit.DEBIT_AMOUNT</code></LI>
	   ///     <LI><code>SHADebit.INITIAL_AMOUNT</code></LI>
	   ///     <LI><code>SHADebit.USER_BALANCE</code></LI>
	   ///   </UL>
	   /// </P>
	   /// 
	   /// <P>Note that the value of <code>SHADebit.USER_BALANCE</code> will
	   /// be set after calling <code>verifyTransactionData(SHAiButtonUser)</code>
	   /// and  after calling <code>executeTransaction(SHAiButtonUser)</code>.</P>
	   /// </summary>
	   /// <returns> The value of the requested parameter.
	   /// </returns>
	   /// <exception cref="IllegalArgumentException"> if an invalid parameter type
	   ///         is requested. </exception>
	   public override int getParameter(int type)
	   {
		   lock (this)
		   {
			  switch (type)
			  {
				 case DEBIT_AMOUNT:
					return debitAmount;
				 case INITIAL_AMOUNT:
					return initialAmount;
				 case USER_BALANCE:
					return userBalance;
				 default:
					throw new System.ArgumentException("Invalid parameter type");
			  }
		   }
	   }

	   /// <summary>
	   /// <P>Sets the value of a particular parameter for this
	   /// debit transaction.</P>
	   /// 
	   /// <P><B>Valid Parameters</B>
	   ///   <UL>
	   ///     <LI><code>SHADebit.DEBIT_AMOUNT</code></LI>
	   ///     <LI><code>SHADebit.INITIAL_AMOUNT</code></LI>
	   ///   </UL>
	   /// </P>
	   /// </summary>
	   /// <param name="type"> Specifies the parameter type (<code>SHADebit.DEBIT_AMOUNT</code> or
	   ///        <code>SHADebit.INITIAL_AMOUNT</code>) </param>
	   /// <returns> </code>true</code> if a valid parameter type was specified
	   ///         and the value of the parameter is positive.
	   /// </returns>
	   /// <exception cref="IllegalArgumentException"> if an invalid parameter type
	   ///         is requested. </exception>
	   public override bool setParameter(int type, int param)
	   {
		   lock (this)
		   {
			  if (param <= 0)
			  {
				 return false;
			  }
			  switch (type)
			  {
				 case DEBIT_AMOUNT:
					debitAmount = param;
					break;
				 case INITIAL_AMOUNT:
					initialAmount = param;
					break;
				 default:
					return false;
			  }
			  return true;
		   }
	   }

	   /// <summary>
	   /// <para>Resets all transaction parameters to default values</para>
	   /// </summary>
	   public override void resetParameters()
	   {
		   lock (this)
		   {
			  debitAmount = 50; //50 cents
			  initialAmount = 10000; //100 dollars
			  userBalance = 0; //0 dollars
		   }
	   }

	}

}