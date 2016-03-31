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

    /// <summary>
    /// <P> One-Time-Programmable (OTP) Memory bank interface for iButtons (or 1-Wire devices)
    /// with OTP features.  This interface extents the base functionality of
    /// the super-interfaces <seealso cref="com.dalsemi.onewire.container.MemoryBank MemoryBank"/>
    /// and <seealso cref="com.dalsemi.onewire.container.PagedMemoryBank PagedMemoryBank"/>
    /// by providing One-Time-Programmable services. </P>
    ///
    /// <P>The OTPMemoryBank methods can be organized into the following categories: </P>
    /// <UL>
    ///   <LI> <B> Information </B>
    ///     <UL>
    ///       <LI> <seealso cref="#canRedirectPage() canRedirectPage"/>
    ///       <LI> <seealso cref="#canLockPage() canLockPage"/>
    ///       <LI> <seealso cref="#canLockRedirectPage() canLockRedirectPage"/>
    ///     </UL>
    ///   <LI> <B> Read Status </B>
    ///     <UL>
    ///       <LI> <seealso cref="#getRedirectedPage(int) getRedirectedPage"/>
    ///       <LI> <seealso cref="#isPageLocked(int) isPageLocked"/>
    ///       <LI> <seealso cref="#isRedirectPageLocked(int) isRedirectPageLocked"/>
    ///     </UL>
    ///   <LI> <B> Write Status </B>
    ///     <UL>
    ///       <LI> <seealso cref="#redirectPage(int,int) redirectPage"/>
    ///       <LI> <seealso cref="#lockPage(int) lockPage"/>
    ///       <LI> <seealso cref="#lockRedirectPage(int) lockRedirectPage"/>
    ///     </UL>
    ///  </UL>
    ///
    /// <H3> Usage </H3>
    ///
    /// <DL>
    /// <DD> <H4> Example 1</H4>
    /// Read the OTP status of page 0 in the OTPMemoryBank instance 'otp':
    /// <PRE> <CODE>
    ///  if (otp.canRedirectPage())
    ///  {
    ///     int new_page = getRedirectedPage(0);
    ///     if (new_page != 0)
    ///        System.out.println("Page 0 is redirected to " + new_page);
    ///  }
    ///
    ///  if (otp.canLockPage())
    ///  {
    ///     if (otp.isPageLocked(0))
    ///        System.out.println("Page 0 is locked");
    ///  }
    ///
    ///  if (otp.canLockRedirectPage())
    ///  {
    ///     if (otp.isRedirectPageLocked(0))
    ///        System.out.println("Page 0 redirection is locked");
    ///  }
    /// </CODE> </PRE>
    ///
    /// <DD> <H4> Example 1</H4>
    /// Lock all of the pages in the OTPMemoryBank instance 'otp':
    /// <PRE> <CODE>
    ///  if (otp.canLockPage())
    ///  {
    ///     // loop to lock each page
    ///     for (int pg = 0; pg < otp.getNumberPages(); pg++)
    ///     {
    ///        otp.lockPage(pg);
    ///     }
    ///  }
    ///  else
    ///     System.out.println("OTPMemoryBank does not support page locking");
    /// </CODE> </PRE>
    /// </DL>
    /// </summary>
    /// <seealso cref= com.dalsemi.onewire.container.MemoryBank </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.PagedMemoryBank </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer09 </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer0B </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer0F </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer12 </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer13
    ///
    /// @version    0.01, 11 Dec 2000
    /// @author     DS </seealso>
    public interface OTPMemoryBank : PagedMemoryBank
    {
        //--------
        //-------- OTP Memory Bank feature methods
        //--------

        /// <summary>
        /// Checks to see if this memory bank has pages that can be redirected
        /// to a new page.  This is used in Write-Once memory
        /// to provide a means to update.
        /// </summary>
        /// <returns>  <CODE> true </CODE> if this memory bank pages can be redirected
        ///          to a new page
        /// </returns>
        /// <seealso cref= #redirectPage(int,int) redirectPage </seealso>
        /// <seealso cref= #getRedirectedPage(int) getRedirectedPage </seealso>
        bool canRedirectPage();

        /// <summary>
        /// Checks to see if this memory bank has pages that can be locked.  A
        /// locked page would prevent any changes to it's contents.
        /// </summary>
        /// <returns>  <CODE> true </CODE> if this memory bank has pages that can be
        ///          locked
        /// </returns>
        /// <seealso cref= #lockPage(int) lockPage </seealso>
        /// <seealso cref= #isPageLocked(int) isPageLocked </seealso>
        bool canLockPage();

        /// <summary>
        /// Checks to see if this memory bank has pages that can be locked from
        /// being redirected.  This would prevent a Write-Once memory from
        /// being updated.
        /// </summary>
        /// <returns>  <CODE> true </CODE> if this memory bank has pages that can
        ///          be locked from being redirected to a new page
        /// </returns>
        /// <seealso cref= #lockRedirectPage(int) lockRedirectPage </seealso>
        /// <seealso cref= #isRedirectPageLocked(int) isRedirectPageLocked </seealso>
        bool canLockRedirectPage();

        //--------
        //-------- I/O methods
        //--------

        /// <summary>
        /// Locks the specifed page in this memory bank.  Not supported
        /// by all devices.
        /// </summary>
        /// <param name="page">   number of page to lock
        /// </param>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a read back verification fails.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.  It will also be thrown if the device needs 'program' voltage
        ///         and the adapter used by this device does not support it.
        /// </exception>
        /// <seealso cref= #isPageLocked(int) isPageLocked </seealso>
        /// <seealso cref= #canLockPage() canLockPage </seealso>
        /// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#canProgram() DSPortAdapter.canProgram() </seealso>
        void lockPage(int page);

        /// <summary>
        /// Checks to see if the specified page is locked.
        /// </summary>
        /// <param name="page">  page to check
        /// </param>
        /// <returns>  <CODE> true </CODE> if page is locked
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.
        /// </exception>
        /// <seealso cref= #lockPage(int) lockPage </seealso>
        /// <seealso cref= #canLockPage() canLockPage </seealso>
        bool isPageLocked(int page);

        /// <summary>
        /// Redirects the specifed page to a new page.
        /// Not supported by all devices.
        /// </summary>
        /// <param name="page">      number of page to redirect </param>
        /// <param name="newPage">   new page number to redirect to
        /// </param>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.  It will also be thrown if the device needs 'program' voltage
        ///         and the adapter used by this device does not support it.
        /// </exception>
        /// <seealso cref= #canRedirectPage() canRedirectPage </seealso>
        /// <seealso cref= #getRedirectedPage(int) getRedirectedPage </seealso>
        /// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#canProgram() DSPortAdapter.canProgram() </seealso>
        void redirectPage(int page, int newPage);

        /// <summary>
        /// Checks to see if the specified page is redirected.
        /// Not supported by all devices.
        /// </summary>
        /// <param name="page">  page to check for redirection
        /// </param>
        /// <returns>  the new page number or 0 if not redirected
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.
        /// </exception>
        /// <seealso cref= #canRedirectPage() canRedirectPage </seealso>
        /// <seealso cref= #redirectPage(int,int) redirectPage
        /// </seealso>
        /// @deprecated  As of 1-Wire API 0.01, replaced by <seealso cref="#getRedirectedPage(int)"/>
        int isPageRedirected(int page);

        /// <summary>
        /// Gets the page redirection of the specified page.
        /// Not supported by all devices.
        /// </summary>
        /// <param name="page">  page to check for redirection
        /// </param>
        /// <returns>  the new page number or 0 if not redirected
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.
        /// </exception>
        /// <seealso cref= #canRedirectPage() canRedirectPage </seealso>
        /// <seealso cref= #redirectPage(int,int) redirectPage
        /// @since 1-Wire API 0.01 </seealso>
        int getRedirectedPage(int page);

        /// <summary>
        /// Locks the redirection of the specifed page.
        /// Not supported by all devices.
        /// </summary>
        /// <param name="page">  page to redirect
        /// </param>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.  It will also be thrown if the device needs 'program' voltage
        ///         and the adapter used by this device does not support it.
        /// </exception>
        /// <seealso cref= #canLockRedirectPage() canLockRedirectPage </seealso>
        /// <seealso cref= #isRedirectPageLocked(int) isRedirectPageLocked </seealso>
        /// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter#canProgram() DSPortAdapter.canProgram() </seealso>
        void lockRedirectPage(int page);

        /// <summary>
        /// Checks to see if the specified page has redirection locked.
        /// Not supported by all devices.
        /// </summary>
        /// <param name="page">  page to check for locked redirection
        /// </param>
        /// <returns>  <CODE> true </CODE> if redirection is locked for this page
        /// </returns>
        /// <exception cref="OneWireIOException"> on a 1-Wire communication error such as
        ///         no device present or a CRC read from the device is incorrect.  This could be
        ///         caused by a physical interruption in the 1-Wire Network due to
        ///         shorts or a newly arriving 1-Wire device issuing a 'presence pulse'. </exception>
        /// <exception cref="OneWireException"> on a communication or setup error with the 1-Wire
        ///         adapter.
        /// </exception>
        /// <seealso cref= #canLockRedirectPage() canLockRedirectPage </seealso>
        /// <seealso cref= #lockRedirectPage(int) lockRedirectPage </seealso>
        bool isRedirectPageLocked(int page);
    }
}