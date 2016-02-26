/*---------------------------------------------------------------------------
 * Copyright (C) 2003 Dallas Semiconductor Corporation, All Rights Reserved.
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

	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;

	/// <summary>
	/// Public interface for all devices which implement some form of
	/// password protection.  The operation protected could be reading from
	/// the device, writing to the device, or both.  These interface methods
	/// will allow you to set the passwords on the device, enable/disable the
	/// passwords on the device, and set the passwords for the API to use
	/// when interacting with the device.
	/// 
	/// @version    1.00, 8 Aug 2003
	/// @author     shughes, JPE
	/// </summary>
	public interface PasswordContainer
	{
	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns the length in bytes of the Read-Only password.
	   /// </summary>
	   /// <returns> the length in bytes of the Read-Only password. </returns>
	   int ReadOnlyPasswordLength {get;}

	   /// <summary>
	   /// Returns the length in bytes of the Read/Write password.
	   /// </summary>
	   /// <returns> the length in bytes of the Read/Write password. </returns>
	   int ReadWritePasswordLength {get;}

	   /// <summary>
	   /// Returns the length in bytes of the Write-Only password.
	   /// </summary>
	   /// <returns> the length in bytes of the Write-Only password. </returns>
	   int WriteOnlyPasswordLength {get;}

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns the absolute address of the memory location where
	   /// the Read-Only password is written.
	   /// </summary>
	   /// <returns> the absolute address of the memory location where
	   ///         the Read-Only password is written. </returns>
	   int ReadOnlyPasswordAddress {get;}

	   /// <summary>
	   /// Returns the absolute address of the memory location where
	   /// the Read/Write password is written.
	   /// </summary>
	   /// <returns> the absolute address of the memory location where
	   ///         the Read/Write password is written. </returns>
	   int ReadWritePasswordAddress {get;}

	   /// <summary>
	   /// Returns the absolute address of the memory location where
	   /// the Write-Only password is written.
	   /// </summary>
	   /// <returns> the absolute address of the memory location where
	   ///         the Write-Only password is written. </returns>
	   int WriteOnlyPasswordAddress {get;}

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns true if this device has a Read-Only password.
	   /// If false, all other functions dealing with the Read-Only
	   /// password will throw an exception if called.
	   /// </summary>
	   /// <returns> <code>true</code> if this device has a Read-Only password. </returns>
	   bool hasReadOnlyPassword();

	   /// <summary>
	   /// Returns true if this device has a Read/Write password.
	   /// If false, all other functions dealing with the Read/Write
	   /// password will throw an exception if called.
	   /// </summary>
	   /// <returns> <code>true</code> if this device has a Read/Write password. </returns>
	   bool hasReadWritePassword();

	   /// <summary>
	   /// Returns true if this device has a Write-Only password.
	   /// If false, all other functions dealing with the Write-Only
	   /// password will throw an exception if called.
	   /// </summary>
	   /// <returns> <code>true</code> if this device has a Write-Only password. </returns>
	   bool hasWriteOnlyPassword();

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns true if the device's Read-Only password has been enabled.
	   /// </summary>
	   /// <returns> <code>true</code> if the device's Read-Only password has been enabled. </returns>
	   bool DeviceReadOnlyPasswordEnable {get;}

	   /// <summary>
	   /// Returns true if the device's Read/Write password has been enabled.
	   /// </summary>
	   /// <returns> <code>true</code> if the device's Read/Write password has been enabled. </returns>
	   bool DeviceReadWritePasswordEnable {get;}

	   /// <summary>
	   /// Returns true if the device's Write-Only password has been enabled.
	   /// </summary>
	   /// <returns> <code>true</code> if the device's Write-Only password has been enabled. </returns>
	   bool DeviceWriteOnlyPasswordEnable {get;}

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns true if this device has the capability to enable one type of password
	   /// while leaving another type disabled.  i.e. if the device has Read-Only password
	   /// protection and Write-Only password protection, this method indicates whether or
	   /// not you can enable Read-Only protection while leaving the Write-Only protection
	   /// disabled.
	   /// </summary>
	   /// <returns> <code>true</code> if the device has the capability to enable one type 
	   ///         of password while leaving another type disabled. </returns>
	   bool hasSinglePasswordEnable();

	   /// <summary>
	   /// <para>Enables/Disables passwords for this Device.  This method allows you to 
	   /// individually enable the different types of passwords for a particular
	   /// device.  If <code>hasSinglePasswordEnable()</code> returns true,
	   /// you can selectively enable particular types of passwords.  Otherwise,
	   /// this method will throw an exception if all supported types are not
	   /// enabled.</para>
	   /// 
	   /// <para>For this to be successful, either write-protect passwords must be disabled,
	   /// or the write-protect password(s) for this container must be set and must match
	   /// the value of the write-protect password(s) in the device's register.</para>
	   /// </summary>
	   /// <param name="enableReadOnly"> if <code>true</code> Read-Only passwords will be enabled. </param>
	   /// <param name="enableReadWrite"> if <code>true</code> Read/Write passwords will be enabled. </param>
	   /// <param name="enableWriteOnly"> if <code>true</code> Write-Only passwords will be enabled. </param>
	   void setDevicePasswordEnable(bool enableReadOnly, bool enableReadWrite, bool enableWriteOnly);

	   /// <summary>
	   /// <para>Enables/Disables passwords for this device.  If the part has more than one
	   /// type of password (Read-Only, Write-Only, or Read/Write), all passwords
	   /// will be enabled.  This function is equivalent to the following:
	   ///    <code> owc41.setDevicePasswordEnable(
	   ///                    owc41.hasReadOnlyPassword(), 
	   ///                    owc41.hasReadWritePassword(),
	   ///                    owc41.hasWriteOnlyPassword() ); </code></para>
	   /// 
	   /// <para>For this to be successful, either write-protect passwords must be disabled,
	   /// or the write-protect password(s) for this container must be set and must match
	   /// the value of the write-protect password(s) in the device's register.</para>
	   /// </summary>
	   /// <param name="enableAll"> if <code>true</code>, all passwords are enabled.  Otherwise,
	   ///        all passwords are disabled. </param>
	   bool DevicePasswordEnableAll {set;}

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// <para>Writes the given password to the device's Read-Only password register.  Note
	   /// that this function does not enable the password, just writes the value to
	   /// the appropriate memory location.</para>
	   /// 
	   /// <para>For this to be successful, either write-protect passwords must be disabled,
	   /// or the write-protect password(s) for this container must be set and must match
	   /// the value of the write-protect password(s) in the device's register.</para>
	   /// </summary>
	   /// <param name="password"> the new password to be written to the device's Read-Only
	   ///        password register.  Length must be 
	   ///        <code>(offset + getReadOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setDeviceReadOnlyPassword(sbyte[] password, int offset);

	   /// <summary>
	   /// <para>Writes the given password to the device's Read/Write password register.  Note
	   /// that this function does not enable the password, just writes the value to
	   /// the appropriate memory location.</para>
	   /// 
	   /// <para>For this to be successful, either write-protect passwords must be disabled,
	   /// or the write-protect password(s) for this container must be set and must match
	   /// the value of the write-protect password(s) in the device's register.</para>
	   /// </summary>
	   /// <param name="password"> the new password to be written to the device's Read-Write
	   ///        password register.  Length must be 
	   ///        <code>(offset + getReadWritePasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setDeviceReadWritePassword(sbyte[] password, int offset);

	   /// <summary>
	   /// <para>Writes the given password to the device's Write-Only password register.  Note
	   /// that this function does not enable the password, just writes the value to
	   /// the appropriate memory location.</para>
	   /// 
	   /// <para>For this to be successful, either write-protect passwords must be disabled,
	   /// or the write-protect password(s) for this container must be set and must match
	   /// the value of the write-protect password(s) in the device's register.</para>
	   /// </summary>
	   /// <param name="password"> the new password to be written to the device's Write-Only
	   ///        password register.  Length must be 
	   ///        <code>(offset + getWriteOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setDeviceWriteOnlyPassword(sbyte[] password, int offset);

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Sets the Read-Only password used by the API when reading from the
	   /// device's memory.  This password is not written to the device's
	   /// Read-Only password register.  It is the password used by the
	   /// software for interacting with the device only.
	   /// </summary>
	   /// <param name="password"> the new password to be used by the API when 
	   ///        reading from the device's memory.  Length must be 
	   ///        <code>(offset + getReadOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setContainerReadOnlyPassword(sbyte[] password, int offset);

	   /// <summary>
	   /// Sets the Read/Write password used by the API when reading from  or
	   /// writing to the device's memory.  This password is not written to 
	   /// the device's Read/Write password register.  It is the password used 
	   /// by the software for interacting with the device only.
	   /// </summary>
	   /// <param name="password"> the new password to be used by the API when 
	   ///        reading from or writing to the device's memory.  Length must be 
	   ///        <code>(offset + getReadWritePasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setContainerReadWritePassword(sbyte[] password, int offset);

	   /// <summary>
	   /// Sets the Write-Only password used by the API when writing to the
	   /// device's memory.  This password is not written to the device's
	   /// Write-Only password register.  It is the password used by the
	   /// software for interacting with the device only.
	   /// </summary>
	   /// <param name="password"> the new password to be used by the API when 
	   ///        writing to the device's memory.  Length must be 
	   ///        <code>(offset + getWriteOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying from the given password array </param>
	   void setContainerWriteOnlyPassword(sbyte[] password, int offset);

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Returns true if the password used by the API for reading from the
	   /// device's memory has been set.  The return value is not affected by 
	   /// whether or not the read password of the container actually matches 
	   /// the value in the device's password register.
	   /// </summary>
	   /// <returns> <code>true</code> if the password used by the API for 
	   /// reading from the device's memory has been set. </returns>
	   bool ContainerReadOnlyPasswordSet {get;}

	   /// <summary>
	   /// Returns true if the password used by the API for reading from or
	   /// writing to the device's memory has been set.  The return value is 
	   /// not affected by whether or not the read/write password of the 
	   /// container actually matches the value in the device's password 
	   /// register.
	   /// </summary>
	   /// <returns> <code>true</code> if the password used by the API for 
	   /// reading from or writing to the device's memory has been set. </returns>
	   bool ContainerReadWritePasswordSet {get;}

	   /// <summary>
	   /// Returns true if the password used by the API for writing to the
	   /// device's memory has been set.  The return value is not affected by 
	   /// whether or not the write password of the container actually matches 
	   /// the value in the device's password register.
	   /// </summary>
	   /// <returns> <code>true</code> if the password used by the API for 
	   /// writing to the device's memory has been set. </returns>
	   bool ContainerWriteOnlyPasswordSet {get;}

	   // -----------------------------------------------------------------

	   /// <summary>
	   /// Gets the Read-Only password used by the API when reading from the
	   /// device's memory.  This password is not read from the device's
	   /// Read-Only password register.  It is the password used by the
	   /// software for interacting with the device only and must have been
	   /// set using the <code>setContainerReadOnlyPassword</code> method.
	   /// </summary>
	   /// <param name="password"> array for holding the password that is used by the 
	   ///        API when reading from the device's memory.  Length must be 
	   ///        <code>(offset + getWriteOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying into the given password array </param>
	   void getContainerReadOnlyPassword(sbyte[] password, int offset);

	   /// <summary>
	   /// Gets the Read/Write password used by the API when reading from or 
	   /// writing to the device's memory.  This password is not read from 
	   /// the device's Read/Write password register.  It is the password used 
	   /// by the software for interacting with the device only and must have 
	   /// been set using the <code>setContainerReadWritePassword</code> method.
	   /// </summary>
	   /// <param name="password"> array for holding the password that is used by the 
	   ///        API when reading from or writing to the device's memory.  Length must be 
	   ///        <code>(offset + getReadWritePasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying into the given password array </param>
	   void getContainerReadWritePassword(sbyte[] password, int offset);

	   /// <summary>
	   /// Gets the Write-Only password used by the API when writing to the
	   /// device's memory.  This password is not read from the device's
	   /// Write-Only password register.  It is the password used by the
	   /// software for interacting with the device only and must have been
	   /// set using the <code>setContainerWriteOnlyPassword</code> method.
	   /// </summary>
	   /// <param name="password"> array for holding the password that is used by the 
	   ///        API when writing to the device's memory.  Length must be 
	   ///        <code>(offset + getWriteOnlyPasswordLength)</code> </param>
	   /// <param name="offset"> the starting point for copying into the given password array </param>
	   void getContainerWriteOnlyPassword(sbyte[] password, int offset);

	   // -----------------------------------------------------------------
	}
}