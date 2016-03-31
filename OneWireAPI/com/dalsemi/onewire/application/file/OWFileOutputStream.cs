using System;
using System.IO;

/*---------------------------------------------------------------------------
 * Copyright (C) 2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.application.file
{
    using com.dalsemi.onewire.container;

    /// <summary>
    /// A 1-Wire file output stream is an output stream for writing data to a
    /// <code>OWFile</code> or to a <code>OWFileDescriptor</code>. Whether or not
    /// a file is available or may be created depends upon the underlying
    /// platform.  This platform allows a file to be opened
    /// for writing by only one <tt>OWFileOutputStream</tt> (or other
    /// file-writing object) at a time.  In such situations the constructors in
    /// this class will fail if the file involved is already open.  The 1-Wire
    /// File system must be formatted before use.  Use OWFile:format to prepare
    /// a device or group of devices.
    ///
    /// <para> The 1-Wire device will only be written in the following situations
    /// <ul>
    /// <li> use <code>getFD()</code> and call the <code>sync()</code> method of the
    ///      <code>OWFileDescriptor</code> until a <code>SyncFailedException</code> is
    ///      NOT thrown
    /// <li> if the device runs out of memory during a write, before
    ///      <code>IOException</code> is thrown
    /// <li> by calling <code>close()</code>
    /// <li> in <code>finalize()</code> <B>WARNING</B> could deadlock if device not
    ///      synced and inside beginExclusive/endExclusive block.
    /// </ul>
    ///
    /// </para>
    /// <para> Note that the 1-Wire File system can reside across multiple 1-Wire
    ///  devices.  In this case only one of the devices need be supplied to the
    ///  constructor.  Each device in a multi-device file system contains
    ///  information to reacquire the entire list.
    ///
    /// </para>
    /// <para>  File and directory <b> name </b> limitations
    /// <ul>
    /// <li> File/directory names limited to 4 characters not including extension
    /// <li> File/directory names are not case sensitive and will be automatically
    ///      changed to all-CAPS
    /// <li> Only files can have extensions
    /// <li> Extensions are numberical in the range 0 to 125
    /// <li> Extensions 100 to 125 are special purpose and not yet implemented or allowed
    /// <li> Files can have the read-only attribute
    /// <li> Directories can have the hidden attribute
    /// <li> It is recommended to limit directory depth to 10 levels to accomodate
    ///      legacy implementations
    /// </ul>
    ///
    /// <H3> Usage </H3>
    /// <DL>
    /// <DD> <H4> Example </H4>
    /// Write to a 1-Wire file on device 'owd':
    /// <PRE> <CODE>
    ///   // create a 1-Wire file at root
    ///   OWFileOutputStream outstream = new OWFileOutputStream(owd, "DEMO.0");
    ///
    ///   // write the data (in a byte array data[])
    ///   outstream.write(data);
    ///
    ///   // get 1-Wire File descriptor to flush to device
    ///   OWFileDescriptor owfd = owfile.getFD();
    ///
    ///   // loop until sync is successful
    ///   do
    ///   {
    ///      try
    ///      {
    ///         owfd.sync();
    ///         done = true;
    ///      }
    ///      catch (SyncFailedException e)
    ///      {
    ///         // do something
    ///         ...
    ///         done = false;
    ///      }
    ///   }
    ///   while (!done)
    ///
    ///   // close the stream to release system resources
    ///   outstream.close();
    /// </CODE> </PRE>
    ///
    /// @author  DS
    /// @version 0.01, 1 June 2001
    /// </para>
    /// </summary>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFile </seealso>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFileInputStream </seealso>
    public class OWFileOutputStream : System.IO.Stream, IDisposable
    {
        //--------
        //-------- Variables
        //--------

        /// <summary>
        /// File descriptor.
        /// </summary>
        private OWFileDescriptor fd;

        //--------
        //-------- Constructors
        //--------

        /// <summary>
        /// Creates an output file stream to write to the file with the
        /// specified name. A new <code>OWFileDescriptor</code> object is
        /// created to represent this file connection.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with <code>name</code> as its argument.
        /// </para>
        /// <para>
        /// If the file exists but is a directory rather than a regular file, does
        /// not exist but cannot be created, or cannot be opened for any other
        /// reason then a <code>FileNotFoundException</code> is thrown.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
        /// <param name="name">   the system-dependent filename </param>
        /// <exception cref="FileNotFoundException">  if the file exists but is a directory
        ///                   rather than a regular file, does not exist but cannot
        ///                   be created, or cannot be opened for any other reason </exception>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies write access
        ///               to the file. </exception>
        public OWFileOutputStream(OneWireContainer owd, string name)
        {
            OneWireContainer[] devices = new OneWireContainer[1];
            devices[0] = owd;
            fd = new OWFileDescriptor(devices, name);

            try
            {
                fd.create(false, false, false, -1, -1);
            }
            catch (OWFileNotFoundException e)
            {
                fd.free();
                fd = null;
                throw new OWFileNotFoundException(e.ToString());
            }
        }

        /// <summary>
        /// Creates an output file stream to write to the file with the
        /// specified name. A new <code>OWFileDescriptor</code> object is
        /// created to represent this file connection.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with <code>name</code> as its argument.
        /// </para>
        /// <para>
        /// If the file exists but is a directory rather than a regular file, does
        /// not exist but cannot be created, or cannot be opened for any other
        /// reason then a <code>FileNotFoundException</code> is thrown.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    array of OneWireContainers that this Filesystem resides on </param>
        /// <param name="name">   the system-dependent filename </param>
        /// <exception cref="FileNotFoundException">  if the file exists but is a directory
        ///                   rather than a regular file, does not exist but cannot
        ///                   be created, or cannot be opened for any other reason </exception>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies write access
        ///               to the file. </exception>
        public OWFileOutputStream(OneWireContainer[] owd, string name)
        {
            fd = new OWFileDescriptor(owd, name);

            try
            {
                fd.create(false, false, false, -1, -1);
            }
            catch (OWFileNotFoundException e)
            {
                fd.free();
                fd = null;
                throw new OWFileNotFoundException(e.ToString());
            }
        }

        /// <summary>
        /// Creates an output file stream to write to the file with the specified
        /// <code>name</code>.  If the second argument is <code>true</code>, then
        /// bytes will be written to the end of the file rather than the beginning.
        /// A new <code>OWFileDescriptor</code> object is created to represent this
        /// file connection.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with <code>name</code> as its argument.
        /// </para>
        /// <para>
        /// If the file exists but is a directory rather than a regular file, does
        /// not exist but cannot be created, or cannot be opened for any other
        /// reason then a <code>FileNotFoundException</code> is thrown.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
        /// <param name="name">   the system-dependent file name </param>
        /// <param name="append"> if <code>true</code>, then bytes will be written
        ///                   to the end of the file rather than the beginning </param>
        /// <exception cref="FileNotFoundException">  if the file exists but is a directory
        ///                   rather than a regular file, does not exist but cannot
        ///                   be created, or cannot be opened for any other reason. </exception>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies write access
        ///               to the file. </exception>
        public OWFileOutputStream(OneWireContainer owd, string name, bool append)
        {
            fd = new OWFileDescriptor(owd, name);

            try
            {
                fd.create(append, false, false, -1, -1);
            }
            catch (OWFileNotFoundException e)
            {
                fd.free();
                fd = null;
                throw new OWFileNotFoundException(e.ToString());
            }
        }

        /// <summary>
        /// Creates an output file stream to write to the file with the specified
        /// <code>name</code>.  If the second argument is <code>true</code>, then
        /// bytes will be written to the end of the file rather than the beginning.
        /// A new <code>OWFileDescriptor</code> object is created to represent this
        /// file connection.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with <code>name</code> as its argument.
        /// </para>
        /// <para>
        /// If the file exists but is a directory rather than a regular file, does
        /// not exist but cannot be created, or cannot be opened for any other
        /// reason then a <code>FileNotFoundException</code> is thrown.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    array of OneWireContainers that this Filesystem resides on </param>
        /// <param name="name">    the system-dependent file name </param>
        /// <param name="append">  if <code>true</code>, then bytes will be written
        ///                   to the end of the file rather than the beginning </param>
        /// <exception cref="FileNotFoundException">  if the file exists but is a directory
        ///                   rather than a regular file, does not exist but cannot
        ///                   be created, or cannot be opened for any other reason. </exception>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies write access
        ///               to the file. </exception>
        public OWFileOutputStream(OneWireContainer[] owd, string name, bool append)
        {
            fd = new OWFileDescriptor(owd, name);

            try
            {
                fd.create(append, false, false, -1, -1);
            }
            catch (OWFileNotFoundException e)
            {
                fd.free();
                fd = null;
                throw new OWFileNotFoundException(e.ToString());
            }
        }

        /// <summary>
        /// Creates a file output stream to write to the file represented by
        /// the specified <code>File</code> object. A new
        /// <code>OWFileDescriptor</code> object is created to represent this
        /// file connection.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with the path represented by the <code>file</code>
        /// argument as its argument.
        /// </para>
        /// <para>
        /// If the file exists but is a directory rather than a regular file, does
        /// not exist but cannot be created, or cannot be opened for any other
        /// reason then a <code>FileNotFoundException</code> is thrown.
        ///
        /// </para>
        /// </summary>
        /// <param name="file">               the file to be opened for writing. </param>
        /// <exception cref="FileNotFoundException">  if the file exists but is a directory
        ///                   rather than a regular file, does not exist but cannot
        ///                   be created, or cannot be opened for any other reason </exception>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies write access
        ///               to the file. </exception>
        /// <seealso cref=        java.io.File#getPath() </seealso>
        public OWFileOutputStream(OWFile file)
        {
            try
            {
                fd = file.FD;
            }
            catch (System.IO.IOException e)
            {
                fd.free();
                fd = null;
                throw new OWFileNotFoundException(e.ToString());
            }

            fd.open();
        }

        /// <summary>
        /// Creates an output file stream to write to the specified file
        /// descriptor, which represents an existing connection to an actual
        /// file in the Filesystem.
        /// <para>
        /// First, if there is a security manager, its <code>checkWrite</code>
        /// method is called with the file descriptor <code>fdObj</code>
        /// argument as its argument.
        ///
        /// </para>
        /// </summary>
        /// <param name="fdObj">   the file descriptor to be opened for writing. </param>
        /// <exception cref="SecurityException">  if a security manager exists and its
        ///               <code>checkWrite</code> method denies
        ///               write access to the file descriptor. </exception>
        public OWFileOutputStream(OWFileDescriptor fdObj)
        {
            if (fdObj == null)
            {
                throw new System.NullReferenceException("1-Wire FileDescriptor provided is null");
            }

            fd = fdObj;
        }

        //--------
        //-------- Write Methods
        //--------

        /// <summary>
        /// Writes the specified byte to this file output stream. Implements
        /// the <code>write</code> method of <code>OutputStream</code>.
        /// </summary>
        /// <param name="b">   the byte to be written. </param>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual void write(int b)
        {
            if (fd != null)
            {
                fd.write(b);
            }
            else
            {
                throw new System.IO.IOException("1-Wire FileDescriptor is null");
            }
        }

        /// <summary>
        /// Writes <code>b.length</code> bytes from the specified byte array
        /// to this file output stream.
        /// </summary>
        /// <param name="b">   the data. </param>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual void write(byte[] b)
        {
            if (fd != null)
            {
                fd.write(b, 0, b.Length);
            }
            else
            {
                throw new System.IO.IOException("1-Wire FileDescriptor is null");
            }
        }

        /// <summary>
        /// Writes <code>len</code> bytes from the specified byte array
        /// starting at offset <code>off</code> to this file output stream.
        /// </summary>
        /// <param name="b">     the data. </param>
        /// <param name="off">   the start offset in the data. </param>
        /// <param name="len">   the number of bytes to write. </param>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual void write(byte[] b, int off, int len)
        {
            if (fd != null)
            {
                fd.write(b, off, len);
            }
            else
            {
                throw new System.IO.IOException("1-Wire FileDescriptor is null");
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the file descriptor associated with this stream.
        /// </summary>
        /// <returns>  the <code>OWFileDescriptor</code> object that represents
        ///          the connection to the file in the Filesystem being used
        ///          by this <code>FileOutputStream</code> object.
        /// </returns>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        /// <seealso cref=        com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
        public virtual OWFileDescriptor FD
        {
            get
            {
                if (fd != null)
                {
                    return fd;
                }
                else
                {
                    throw new System.IO.IOException("1-Wire FileDescriptor is null");
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanSeek
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Cleans up the connection to the file, and ensures that the
        /// <code>close</code> method of this file output stream is
        /// called when there are no more references to this stream.
        /// </summary>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        /// <seealso cref=        com.dalsemi.onewire.application.file.OWFileInputStream#close() </seealso>
        ~OWFileOutputStream()
        {
            Dispose(false);
        }

        /// <summary>
        /// Closes this file output stream and releases any system resources
        /// associated with this stream. This file output stream may no longer
        /// be used for writing bytes.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (fd != null)
                {
                    fd.close();
                }
            }
        }
    }
}