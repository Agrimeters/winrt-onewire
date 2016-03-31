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
    /// <summary>
    /// <P>Abstract super class for all SHA Transactions.  Typical SHA transactions
    /// might be something like an account debit application, a door access control
    /// system, or a web-based authentication servlet.  The <code>SHATransaction</code>
    /// super class contains the bare minimum functionality necessary for the steps
    /// involved in most SHA transaction applications.</P>
    ///
    /// <P>The first characteristic of a SHA Transaction is that it is tied to an
    /// immutable <code>SHAiButtonCopr</code>, used for data signing and recreating
    /// user authentication responses.  The SHA Transaction guarantees an interface
    /// for initializing account transaction data (<code>setupTransactionData</code>),
    /// verifying that the transaction data has not been tampered with
    /// (<code>verifyTransactionData</code>), performing the transaction and updating
    /// the data (<code>executeTransaction</code>), and validating a user with a
    /// challenge-response authentication protocol (<code>verifyUser</code>).</P>
    ///
    /// <P>In addition, all transactions are characterized by certain parameters (i.e.
    /// how much to debit from the user or what level of access is being requested
    /// from the system).  The interface for retrieving and setting these parameters is
    /// provided through the generic <code>getParameter</code> and
    /// <code>setParameter</code>.</P>
    /// </summary>
    /// <seealso cref= SHADebit </seealso>
    /// <seealso cref= SHADebitUnsigned
    ///
    /// @version 1.00
    /// @author  SKH </seealso>
    public abstract class SHATransaction
    {
        internal const int MAX_RETRY_CNT = 65536;

        internal static readonly Random rand = new Random();

        // **************************************************************** //
        // Error Constants
        // **************************************************************** //
        public const int NO_ERROR = 0;

        public const int SHA_FUNCTION_FAILED = -1;
        public const int MATCH_SCRATCHPAD_FAILED = -2;
        public const int COPR_WRITE_DATAPAGE_FAILED = -3;
        public const int COPR_WRITE_SCRATCHPAD_FAILED = -4;
        public const int COPR_BIND_SECRET_FAILED = -5;
        public const int COPR_COMPUTE_CHALLENGE_FAILED = -6;
        public const int COPROCESSOR_FAILURE = -6;
        public const int USER_READ_AUTH_FAILED = -7;
        public const int USER_WRITE_DATA_FAILED = -8;
        public const int USER_BAD_ACCOUNT_DATA = -9;
        public const int USER_DATA_NOT_UPDATED = -10;
        // **************************************************************** //

        /// <summary>
        /// The last error that occurred during this transaction </summary>
        protected internal int lastError;

        /// <summary>
        /// The coprocessor used to complete this transaction </summary>
        protected internal SHAiButtonCopr copr;

        /// <summary>
        /// <para>User applications should not instantiate this class without
        /// an instance of a coprocessor.</para>
        /// </summary>
        protected internal SHATransaction()
        {
            ;
        }

        /// <summary>
        /// <P>Creates a new SHATransaction, ensuring that reference to
        /// the coprocessor is saved and the errors are cleared.</P>
        /// </summary>
        protected internal SHATransaction(SHAiButtonCopr copr)
        {
            this.copr = copr;
            this.lastError = 0;
        }

        /// <summary>
        /// <P>Returns the error code for the last error in the transaction
        /// process.</P>
        /// </summary>
        public virtual int LastError
        {
            get
            {
                return this.lastError;
            }
        }

        /// <summary>
        /// <P>Returns the error code for the last error in the transaction
        /// process.</P>
        /// </summary>
        public virtual int LastCoprError
        {
            get
            {
                return this.copr.LastError;
            }
        }

        /// <summary>
        /// <P>Setups initial transaction data on SHAiButtonUser.  This step
        /// creates the account data file, signs it with the coprocessor,
        /// and writes it to the iButton.</P>
        /// </summary>
        public abstract bool setupTransactionData(SHAiButtonUser user);

        /// <summary>
        /// <P>Verifies that SHAiButtonUser is a valid user of this service.
        /// This step writes a three byte challenge to the SHAiButtonUser
        /// before doing an authenticated read of the account data.  The
        /// returned MAC is verified using the system authentication secret
        /// on the coprocessor.  If the MAC matches that generated by the
        /// coprocessor, this function returns true.</P>
        /// </summary>
        public abstract bool verifyUser(SHAiButtonUser user);

        /// <summary>
        /// <P>Verifies account data is valid for this service.  The user's
        /// account data is recreated on the coprocessor and signed using
        /// the system signing secret.  If the recreated signature matches
        /// the signature in the account data, the account data is valid.</P>
        /// </summary>
        public abstract bool verifyTransactionData(SHAiButtonUser user);

        /// <summary>
        /// <P>Performs the transaction.  For any given transaction type,
        /// this step would involve updating any necessary account data,
        /// signing the account data using the coprocessor's system signing
        /// secret, and writing the updated account data to the user
        /// iButton</P>
        /// </summary>
        public abstract bool executeTransaction(SHAiButtonUser user, bool verifySuccess);

        /// <summary>
        /// <P>Sets a particular parameter for this transaction.  Parameters
        /// are specified in the class documentation for the specific type of
        /// transaction that is being peformed.</P>
        /// </summary>
        public abstract bool setParameter(int type, int param);

        /// <summary>
        /// <P>Retrieves the value of a particular parameter for this
        /// transaction.  Parameters are specified in the class documentation
        /// for the specific type of transaction that is being peformed.</P>
        /// </summary>
        public abstract int getParameter(int type);

        /// <summary>
        /// <P>Resets the value of all parameters for this transaction.
        /// Parameters are specified in the class documentation for the
        /// specific type of transaction that is being peformed.</P>
        /// </summary>
        public abstract void resetParameters();
    }
}