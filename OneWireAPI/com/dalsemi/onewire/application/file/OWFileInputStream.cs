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
    using System;
    using System.IO;
    using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;

    /// <summary>
    /// A <code>OWFileInputStream</code> obtains input bytes
    /// from a file in a 1-Wire Filesystem. What files
    /// are available depends on the 1-Wire device.
    /// 
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
    /// Read from a 1-Wire file on device 'owd':
    /// <PRE> <CODE>
    ///   // get an input stream to the 1-Wire file
    ///   OWFileInputStream instream = new OWFileInputStream(owd, "DEMO.0");
    /// 
    ///   // read some data
    ///   byte[] data = new byte[2000];
    ///   int len = instream.read(data);
    /// 
    ///   // close the stream to release system resources
    ///   instream.close();
    /// 
    /// </CODE> </PRE>
    /// 
    /// @author  DS
    /// @version 0.01, 1 June 2001
    /// </para>
    /// </summary>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFile </seealso>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
    /// <seealso cref=     com.dalsemi.onewire.application.file.OWFileOutputStream </seealso>
    public class OWFileInputStream : System.IO.Stream
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
	   /// Creates a <code>FileInputStream</code> by
	   /// opening a connection to an actual file,
	   /// the file named by the path name <code>name</code>
	   /// in the Filesystem.  A new <code>OWFileDescriptor</code>
	   /// object is created to represent this file
	   /// connection.
	   /// <para>
	   /// First, if there is a security
	   /// manager, its <code>checkRead</code> method
	   /// is called with the <code>name</code> argument
	   /// as its argument.
	   /// </para>
	   /// <para>
	   /// If the named file does not exist, is a directory rather than a regular
	   /// file, or for some other reason cannot be opened for reading then a
	   /// <code>FileNotFoundException</code> is thrown.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
	   /// <param name="name">   the system-dependent file name. </param>
	   /// <exception cref="FileNotFoundException">  if the file does not exist,
	   ///                   is a directory rather than a regular file,
	   ///                   or for some other reason cannot be opened for
	   ///                   reading. </exception>
	   public OWFileInputStream(OneWireContainer owd, string name)
	   {
		  fd = new OWFileDescriptor(owd, name);

		  // open the file
		  try
		  {
			 fd.open();
		  }
		  catch (OWFileNotFoundException e)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException(e.ToString());
		  }

		  // make sure this is not directory
		  if (!fd.File)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException("Not a file");
		  }
	   }

	   /// <summary>
	   /// Creates a <code>FileInputStream</code> by
	   /// opening a connection to an actual file,
	   /// the file named by the path name <code>name</code>
	   /// in the Filesystem.  A new <code>OWFileDescriptor</code>
	   /// object is created to represent this file
	   /// connection.
	   /// <para>
	   /// First, if there is a security
	   /// manager, its <code>checkRead</code> method
	   /// is called with the <code>name</code> argument
	   /// as its argument.
	   /// </para>
	   /// <para>
	   /// If the named file does not exist, is a directory rather than a regular
	   /// file, or for some other reason cannot be opened for reading then a
	   /// <code>FileNotFoundException</code> is thrown.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="owd">    array of OneWireContainers that this Filesystem resides on </param>
	   /// <param name="name">   the system-dependent file name. </param>
	   /// <exception cref="FileNotFoundException">  if the file does not exist,
	   ///                   is a directory rather than a regular file,
	   ///                   or for some other reason cannot be opened for
	   ///                   reading. </exception>
	   public OWFileInputStream(OneWireContainer[] owd, string name)
	   {
		  fd = new OWFileDescriptor(owd, name);

		  // open the file
		  try
		  {
			 fd.open();
		  }
		  catch (OWFileNotFoundException e)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException(e.ToString());
		  }

		  // make sure this is not directory
		  if (!fd.File)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException("Not a file");
		  }
	   }

	   /// <summary>
	   /// Creates a <code>OWFileInputStream</code> by
	   /// opening a connection to an actual file,
	   /// the file named by the <code>File</code>
	   /// object <code>file</code> in the Filesystem.
	   /// A new <code>OWFileDescriptor</code> object
	   /// is created to represent this file connection.
	   /// <para>
	   /// If the named file does not exist, is a directory rather than a regular
	   /// file, or for some other reason cannot be opened for reading then a
	   /// <code>FileNotFoundException</code> is thrown.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="file">   the file to be opened for reading. </param>
	   /// <exception cref="FileNotFoundException">  if the file does not exist,
	   ///                   is a directory rather than a regular file,
	   ///                   or for some other reason cannot be opened for
	   ///                   reading. </exception>
	   /// <seealso cref=        com.dalsemi.onewire.application.file.OWFile#getPath() </seealso>
	   public OWFileInputStream(OWFile file)
	   {
		  // get the file descriptor
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

		  // open the file
		  try
		  {
			 fd.open();
		  }
		  catch (OWFileNotFoundException e)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException(e.ToString());
		  }

		  // make sure it is not a directory
		  if (!fd.File)
		  {
			 fd.free();
			 fd = null;
			 throw new OWFileNotFoundException("Not a file");
		  }
	   }

	   /// <summary>
	   /// Creates a <code>OWFileInputStream</code> by using the file descriptor
	   /// <code>fdObj</code>, which represents an existing connection to an
	   /// actual file in the Filesystem.
	   /// <para>
	   /// If <code>fdObj</code> is null then a <code>NullPointerException</code>
	   /// is thrown.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="fdObj">   the file descriptor to be opened for reading. </param>
	   public OWFileInputStream(OWFileDescriptor fdObj)
	   {
		  if (fdObj == null)
		  {
			 throw new System.NullReferenceException("OWFile provided is null");
		  }

		  fd = fdObj;
	   }

	   //--------
	   //-------- Read Methods
	   //--------

	   /// <summary>
	   /// Reads a byte of data from this input stream. This method blocks
	   /// if no input is yet available.
	   /// </summary>
	   /// <returns>     the next byte of data, or <code>-1</code> if the end of the
	   ///             file is reached. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual int read()
	   {
		  if (fd != null)
		  {
			 return fd.read();
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }
	   }

	   /// <summary>
	   /// Reads up to <code>b.length</code> bytes of data from this input
	   /// stream into an array of bytes. This method blocks until some input
	   /// is available.
	   /// </summary>
	   /// <param name="b">   the buffer into which the data is read. </param>
	   /// <returns>     the total number of bytes read into the buffer, or
	   ///             <code>-1</code> if there is no more data because the end of
	   ///             the file has been reached. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual int read(byte[] b)
	   {
		  if (fd != null)
		  {
			 return fd.read(b, 0, b.Length);
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }
	   }

	   /// <summary>
	   /// Reads up to <code>len</code> bytes of data from this input stream
	   /// into an array of bytes. This method blocks until some input is
	   /// available.
	   /// </summary>
	   /// <param name="b">     the buffer into which the data is read. </param>
	   /// <param name="off">   the start offset of the data. </param>
	   /// <param name="len">   the maximum number of bytes read. </param>
	   /// <returns>     the total number of bytes read into the buffer, or
	   ///             <code>-1</code> if there is no more data because the end of
	   ///             the file has been reached. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual int read(byte[] b, int off, int len)
	   {
		  if (fd != null)
		  {
			 return fd.read(b, off, len);
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }
	   }

	   /// <summary>
	   /// Skips over and discards <code>n</code> bytes of data from the
	   /// input stream. The <code>skip</code> method may, for a variety of
	   /// reasons, end up skipping over some smaller number of bytes,
	   /// possibly <code>0</code>. The actual number of bytes skipped is returned.
	   /// </summary>
	   /// <param name="n">   the number of bytes to be skipped. </param>
	   /// <returns>     the actual number of bytes skipped. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual long skip(long n)
	   {
		  if (fd != null)
		  {
			 return fd.skip(n);
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }
	   }

	   /// <summary>
	   /// Returns the number of bytes that can be read from this file input
	   /// stream without blocking.
	   /// </summary>
	   /// <returns>     the number of bytes that can be read from this file input
	   ///             stream without blocking. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual int available()
	   {
		  if (fd != null)
		  {
			 return fd.available();
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }
	   }

	   /// <summary>
	   /// Closes this file input stream and releases any system resources
	   /// associated with the stream.
	   /// </summary>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   public virtual void close()
	   {
		  if (fd != null)
		  {
			 fd.close();
		  }
		  else
		  {
			 throw new System.IO.IOException("1-Wire FileDescriptor is null");
		  }

		  fd = null;
	   }

	   /// <summary>
	   /// Returns the <code>OWFileDescriptor</code>
	   /// object  that represents the connection to
	   /// the actual file in the Filesystem being
	   /// used by this <code>FileInputStream</code>.
	   /// </summary>
	   /// <returns>     the file descriptor object associated with this stream. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   /// <seealso cref=        com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
	   public OWFileDescriptor FD
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
        /// Ensures that the <code>close</code> method of this file input stream is
        /// called when there are no more references to it.
        /// </summary>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        /// <seealso cref=        com.dalsemi.onewire.application.file.OWFileInputStream#close() </seealso>
        ~OWFileInputStream()
	   {
		  if (fd != null)
		  {
			 fd.close();
		  }
	   }

	   //--------
	   //-------- Mark Methods
	   //--------

	   /// <summary>
	   /// Marks the current position in this input stream. A subsequent call to
	   /// the <code>reset</code> method repositions this stream at the last marked
	   /// position so that subsequent reads re-read the same bytes.
	   /// 
	   /// <para> The <code>readlimit</code> arguments tells this input stream to
	   /// allow that many bytes to be read before the mark position gets
	   /// invalidated.
	   /// 
	   /// </para>
	   /// <para> The general contract of <code>mark</code> is that, if the method
	   /// <code>markSupported</code> returns <code>true</code>, the stream somehow
	   /// remembers all the bytes read after the call to <code>mark</code> and
	   /// stands ready to supply those same bytes again if and whenever the method
	   /// <code>reset</code> is called.  However, the stream is not required to
	   /// remember any data at all if more than <code>readlimit</code> bytes are
	   /// read from the stream before <code>reset</code> is called.
	   /// 
	   /// </para>
	   /// <para> The <code>mark</code> method of <code>InputStream</code> does
	   /// nothing.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="readlimit">   the maximum limit of bytes that can be read before
	   ///                      the mark position becomes invalid. </param>
	   /// <seealso cref=     java.io.InputStream#reset() </seealso>
	   public virtual void mark(int readlimit)
	   {
		  if (fd != null)
		  {
			 fd.mark(readlimit);
		  }
	   }

	   /// <summary>
	   /// Repositions this stream to the position at the time the
	   /// <code>mark</code> method was last called on this input stream.
	   /// 
	   /// <para> The general contract of <code>reset</code> is:
	   /// 
	   /// </para>
	   /// <para><ul>
	   /// 
	   /// <li> If the method <code>markSupported</code> returns
	   /// <code>true</code>, then:
	   /// 
	   ///     <ul><li> If the method <code>mark</code> has not been called since
	   ///     the stream was created, or the number of bytes read from the stream
	   ///     since <code>mark</code> was last called is larger than the argument
	   ///     to <code>mark</code> at that last call, then an
	   ///     <code>IOException</code> might be thrown.
	   /// 
	   ///     <li> If such an <code>IOException</code> is not thrown, then the
	   ///     stream is reset to a state such that all the bytes read since the
	   ///     most recent call to <code>mark</code> (or since the start of the
	   ///     file, if <code>mark</code> has not been called) will be resupplied
	   ///     to subsequent callers of the <code>read</code> method, followed by
	   ///     any bytes that otherwise would have been the next input data as of
	   ///     the time of the call to <code>reset</code>. </ul>
	   /// 
	   /// <li> If the method <code>markSupported</code> returns
	   /// <code>false</code>, then:
	   /// 
	   ///     <ul><li> The call to <code>reset</code> may throw an
	   ///     <code>IOException</code>.
	   /// 
	   ///     <li> If an <code>IOException</code> is not thrown, then the stream
	   ///     is reset to a fixed state that depends on the particular type of the
	   ///     input stream and how it was created. The bytes that will be supplied
	   ///     to subsequent callers of the <code>read</code> method depend on the
	   ///     particular type of the input stream. </ul></ul>
	   /// 
	   /// </para>
	   /// <para> The method <code>reset</code> for class <code>InputStream</code>
	   /// does nothing and always throws an <code>IOException</code>.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <exception cref="IOException">  if this stream has not been marked or if the
	   ///               mark has been invalidated. </exception>
	   /// <seealso cref=     java.io.InputStream#mark(int) </seealso>
	   /// <seealso cref=     System.IO.IOException </seealso>
	   public virtual void reset()
	   {
		  if (fd != null)
		  {
			 fd.reset();
		  }
	   }

	   /// <summary>
	   /// Tests if this input stream supports the <code>mark</code> and
	   /// <code>reset</code> methods. The <code>markSupported</code> method of
	   /// <code>InputStream</code> returns <code>false</code>.
	   /// </summary>
	   /// <returns>  <code>true</code> if this true type supports the mark and reset
	   ///          method; <code>false</code> otherwise. </returns>
	   /// <seealso cref=     java.io.InputStream#mark(int) </seealso>
	   /// <seealso cref=     java.io.InputStream#reset() </seealso>
	   public virtual bool markSupported()
	   {
		  return true;
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
    }

}