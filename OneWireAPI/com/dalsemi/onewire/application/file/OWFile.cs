using System;

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
    /// An abstract representation of file and directory pathnames on 1-Wire devices.
    ///
    /// <para> User interfaces and operating systems use system-dependent <em>pathname
    /// strings</em> to name files and directories.  This class presents an
    /// abstract, system-independent view of hierarchical pathnames.  An
    /// <em>abstract pathname</em> has two components:
    ///
    /// <ol>
    /// <li> An optional system-dependent <em>prefix</em> string,<br>
    ///      such as a disk-drive specifier, <code>"/"</code> for the UNIX root
    ///      directory, or <code>"\\"</code> for a Win32 UNC pathname, and
    /// <li> A sequence of zero or more string <em>names</em>.
    /// </ol>
    ///
    /// Each name in an abstract pathname except for the last denotes a directory;
    /// the last name may denote either a directory or a file.  The <em>empty</em>
    /// abstract pathname has no prefix and an empty name sequence.
    ///
    /// </para>
    /// <para> The conversion of a pathname string to or from an abstract pathname is
    /// inherently system-dependent.  When an abstract pathname is converted into a
    /// pathname string, each name is separated from the next by a single copy of
    /// the default <em>separator character</em>.  The default name-separator
    /// character is defined by the system property <code>OWFile.separator</code>, and
    /// is made available in the public static fields <code>{@link
    /// #separator}</code> and <code><seealso cref="#separatorChar"/></code> of this class.
    /// When a pathname string is converted into an abstract pathname, the names
    /// within it may be separated by the default name-separator character or by any
    /// other name-separator character that is supported by the underlying system.
    ///
    /// </para>
    /// <para> A pathname, whether abstract or in string form, may be either
    /// <em>absolute</em> or <em>relative</em>.  An absolute pathname is complete in
    /// that no other information is required in order to locate the file that it
    /// denotes.  A relative pathname, in contrast, must be interpreted in terms of
    /// information taken from some other pathname.  By default the classes in the
    /// <code>java.io</code> package always resolve relative pathnames against the
    /// current user directory.  This directory is named by the system property
    /// <code>user.dir</code>, and is typically the directory in which the Java
    /// virtual machine was invoked.  The pathname provided to this OWFile
    /// however is always <em>absolute</em>.
    ///
    /// </para>
    /// <para> The prefix concept is used to handle root directories on UNIX platforms,
    /// and drive specifiers, root directories and UNC pathnames on Win32 platforms,
    /// as follows:
    ///
    /// <ul>
    /// <li> For 1-Wire the Filesystem , the prefix of an absolute pathname is always
    /// <code>"/"</code>.  The abstract pathname denoting the root directory
    /// has the prefix <code>"/"</code> and an empty name sequence.
    ///
    /// </ul>
    ///
    /// </para>
    /// <para> Instances of the <code>OWFile</code> class are immutable; that is, once
    /// created, the abstract pathname represented by a <code>OWFile</code> object
    /// will never change.
    ///
    /// <H3> What is Different on the 1-Wire Filesystem </H3>
    /// </para>
    /// <para> The methods in the class are the same as in the java.io.File version 1.2
    ///     with the following exceptions
    /// </para>
    /// <para>
    /// </para>
    /// <para> Methods provided but of <b> limited </b> functionallity
    /// <ul>
    /// <li>   public long lastModified() - always returns 0
    /// <li>   public bool isAbsolute() - always true
    /// <li>   public bool setLastModified(long time) - does nothing
    /// <li>   public bool setReadOnly() - only for files
    /// <li>   public bool isHidden() - only could be true for directories
    /// </ul>
    ///
    /// </para>
    /// <para> Methods <b> not </b> provided or supported:
    /// <ul>
    /// <li>   public void deleteOnExit()
    /// <li>   public String[] list(FilenameFilter filter)
    /// <li>   public File[] listFiles(FilenameFilter filter)
    /// <li>   public File[] listFiles(FileFilter filter)
    /// <li>   public static File createTempFile(String prefix, String suffix, File directory)
    /// <li>   public static File createTempFile(String prefix, String suffix)
    /// <li>   public URL toURL()
    /// </ul>
    ///
    /// </para>
    /// <para> <b> Extra </b> Methods (not usually in 1.2 java.io.File)
    /// <ul>
    /// <li>   public OWFileDescriptor getFD()
    /// <li>   public void close()
    /// <li>   public OneWireContainer getOneWireContainer()
    /// <li>   public void format()
    /// <li>   public int getFreeMemory()
    /// <li>   public int[] getPageList()
    /// <li>   public PagedMemoryBank getMemoryBankForPage(int)
    /// <li>   public int getLocalPage(int)
    /// </ul>
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
    /// <H3> Tips </H3>
    /// <ul>
    /// <li> <i> Writes </i> will not be flushed to the 1-Wire device Filesystem
    ///      until the <code> OWFile </code> instance is closed with the
    ///      <code> close() </code> method or the <code> sync() </code> method
    ///      from the OWFileDescriptor
    /// <li> The <code> sync() </code> method for flushing the changes to the
    ///      Filesystem is preferred since it can be called multiple times if
    ///      there is a connection problem
    /// <li> New 1-Wire devices Filesystem must first be formatted with the
    ///      <code> format() </code> method in order for files or directories to
    ///      be added or changed.
    /// <li> Multiple 1-Wire devices can be linked into a common Filesystem by
    ///      using the constructor that accepts an array of 1-Wire devices.  The
    ///      first device in the list is the 'root' device and the rest will be
    ///      designated 'satelite's.  Once the <code> format() </code> method
    ///      is used to link these devices then only the 'root' need be used
    ///      in future constuctors of this class or the 1-Wire file stream classes.
    /// <li> Only rewrittable 1-Wire memory devices can be used in multi-device
    ///      file systems.  EPROM and write-once devices can only be used in
    ///      single device file systems.
    /// <li> 1-Wire devices have a limited amount of space.  Use the
    ///      <code> getFreeMemory() </code> method to get an estimate of free memory
    ///      available.
    /// <li> Call the <code> close() </code> method to release system resources
    ///      allocated when done with the <code> OWFile </code> instance
    /// </ul>
    ///
    /// <H3> Usage </H3>
    /// <DL>
    /// <DD> <H4> Example 1</H4>
    /// Format the Filesystem of the 1-Wire device 'owd':
    /// <PRE> <CODE>
    ///   // create a 1-Wire file at root
    ///   OWFile owfile = new OWFile(owd, "");
    ///
    ///   // format Filesystem
    ///   owfile.format();
    ///
    ///   // get 1-Wire File descriptor to flush to device
    ///   OWFileDescriptor owfd = owfile.getFD();
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
    ///   // close the 1-Wire file to release system resources
    ///   owfile.close();
    /// </CODE> </PRE>
    ///
    /// <DD> <H4> Example 2</H4>
    /// Make a multi-level directory structure on the 1-Wire device 'owd':
    /// <PRE> <CODE>
    ///   OWFile owfile = new OWFile(owd, "/doc/text/temp");
    ///
    ///   // make the directories
    ///   if (owfile.mkdirs())
    ///      System.out.println("Success!");
    ///   else
    ///      System.out.println("Out of memory or invalid file/directory");
    ///
    ///   // get 1-Wire File descriptor to flush to device
    ///   ...
    /// </CODE> </PRE>
    /// </DL>
    ///
    /// <H3> 1-Wire File Structure Format </H3>
    /// <DL>
    /// <DD><A HREF="http://pdfserv.maxim-ic.com/arpdf/AppNotes/app114.pdf"> http://pdfserv.maxim-ic.com/arpdf/AppNotes/app114.pdf</A>
    /// </DL>
    ///
    /// </para>
    /// </summary>
    /// <seealso cref= com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
    /// <seealso cref= com.dalsemi.onewire.application.file.OWFileInputStream </seealso>
    /// <seealso cref= com.dalsemi.onewire.application.file.OWFileOutputStream
    ///
    /// @author  DS
    /// @version 0.01, 1 June 2001 </seealso>
    public class OWFile
    {
        //--------
        //-------- Static Variables
        //--------

        /// <summary>
        /// Field separator </summary>
        public const string separator = "/";

        /// <summary>
        /// Field separatorChar </summary>
        public const char separatorChar = '/';

        /// <summary>
        /// Field pathSeparator </summary>
        public const string pathSeparator = ":";

        /// <summary>
        /// Field pathSeparatorChar </summary>
        public const char pathSeparatorChar = ':';

        //--------
        //-------- Variables
        //--------

        /// <summary>
        /// Abstract file descriptor containing this file </summary>
        private OWFileDescriptor fd;

        //--------
        //-------- Constructor
        //--------

        /// <summary>
        /// Creates a new <code>OWFile</code> instance by converting the given
        /// pathname string into an abstract pathname.  If the given string is
        /// the empty string, then the result is the empty abstract pathname.
        /// </summary>
        /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
        /// <param name="pathname">  A pathname string </param>
        /// <exception cref="NullPointerException">
        ///          If the <code>pathname</code> argument is <code>null</code> </exception>
        public OWFile(OneWireContainer owd, string pathname)
        {
            fd = new OWFileDescriptor(owd, pathname);
        }

        /// <summary>
        /// Creates a new <code>OWFile</code> instance by converting the given
        /// pathname string into an abstract pathname.  If the given string is
        /// the empty string, then the result is the empty abstract pathname.
        /// </summary>
        /// <param name="owd">  ordered array of OneWireContainers that this Filesystem
        ///               resides on </param>
        /// <param name="pathname">  A pathname string </param>
        /// <exception cref="NullPointerException">
        ///          If the <code>pathname</code> argument is <code>null</code>
        ///
        ///     Change the OWFileDescriptor to accept only an array of containers
        ///     Change the local ref to be an array
        ///     Create a single array in constructors with single passed owc
        ///  </exception>
        public OWFile(OneWireContainer[] owd, string pathname)
        {
            fd = new OWFileDescriptor(owd, pathname);
        }

        /* Note: The two-argument File constructors do not interpret an empty
           parent abstract pathname as the current user directory.  An empty parent
           instead causes the child to be resolved against the system-dependent
           directory defined by the FileSystem.getDefaultParent method.  On Unix
           this default is "/", while on Win32 it is "\\".  This is required for
           compatibility with the original behavior of this class. */

        /// <summary>
        /// Creates a new <code>OWFile</code> instance from a parent pathname string
        /// and a child pathname string.
        ///
        /// <para> If <code>parent</code> is <code>null</code> then the new
        /// <code>OWFile</code> instance is created as if by invoking the
        /// single-argument <code>OWFile</code> constructor on the given
        /// <code>child</code> pathname string.
        ///
        /// </para>
        /// <para> Otherwise the <code>parent</code> pathname string is taken to denote
        /// a directory, and the <code>child</code> pathname string is taken to
        /// denote either a directory or a file.  If the <code>child</code> pathname
        /// string is absolute then it is converted into a relative pathname in a
        /// system-dependent way.  If <code>parent</code> is the empty string then
        /// the new <code>OWFile</code> instance is created by converting
        /// <code>child</code> into an abstract pathname and resolving the result
        /// against a system-dependent default directory.  Otherwise each pathname
        /// string is converted into an abstract pathname and the child abstract
        /// pathname is resolved against the parent.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
        /// <param name="parent">  The parent pathname string </param>
        /// <param name="child">   The child pathname string </param>
        /// <exception cref="NullPointerException">
        ///          If <code>child</code> is <code>null</code> </exception>
        public OWFile(OneWireContainer owd, string parent, string child)
        {
            if (string.ReferenceEquals(child, null))
            {
                throw new System.NullReferenceException("child is null");
            }

            fd = new OWFileDescriptor(owd, parent + child);
        }

        /// <summary>
        /// Creates a new <code>OWFile</code> instance from a parent abstract
        /// pathname and a child pathname string.
        ///
        /// <para> If <code>parent</code> is <code>null</code> then the new
        /// <code>OWFile</code> instance is created as if by invoking the
        /// single-argument <code>OWFile</code> constructor on the given
        /// <code>child</code> pathname string.
        ///
        /// </para>
        /// <para> Otherwise the <code>parent</code> abstract pathname is taken to
        /// denote a directory, and the <code>child</code> pathname string is taken
        /// to denote either a directory or a file.  If the <code>child</code>
        /// pathname string is absolute then it is converted into a relative
        /// pathname in a system-dependent way.  If <code>parent</code> is the empty
        /// abstract pathname then the new <code>OWFile</code> instance is created by
        /// converting <code>child</code> into an abstract pathname and resolving
        /// the result against a system-dependent default directory.  Otherwise each
        /// pathname string is converted into an abstract pathname and the child
        /// abstract pathname is resolved against the parent.
        ///
        /// </para>
        /// </summary>
        /// <param name="owd">    OneWireContainer that this Filesystem resides on </param>
        /// <param name="parent">  The parent abstract pathname </param>
        /// <param name="child">   The child pathname string </param>
        /// <exception cref="NullPointerException">
        ///          If <code>child</code> is <code>null</code> </exception>
        public OWFile(OWFile parent, string child)
        {
            if (string.ReferenceEquals(child, null))
            {
                throw new System.NullReferenceException("child is null");
            }

            string new_path;

            if (parent.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                new_path = parent.AbsolutePath + child;
            }
            else
            {
                new_path = parent.AbsolutePath + separator + child;
            }

            fd = new OWFileDescriptor(parent.OneWireContainers, new_path);
        }

        //--------
        //-------- Path 'get' Methods
        //--------

        /// <summary>
        /// Returns the name of the file or directory denoted by this abstract
        /// pathname.  This is just the last name in the pathname's name
        /// sequence.  If the pathname's name sequence is empty, then the empty
        /// string is returned.
        /// </summary>
        /// <returns>  The name of the file or directory denoted by this abstract
        ///          pathname, or the empty string if this pathname's name sequence
        ///          is empty </returns>
        public virtual string Name
        {
            get
            {
                return fd.Name;
            }
        }

        /// <summary>
        /// Returns the pathname string of this abstract pathname's parent, or
        /// <code>null</code> if this pathname does not name a parent directory.
        ///
        /// <para> The <em>parent</em> of an abstract pathname consists of the
        /// pathname's prefix, if any, and each name in the pathname's name
        /// sequence except for the last.  If the name sequence is empty then
        /// the pathname does not name a parent directory.
        ///
        /// </para>
        /// </summary>
        /// <returns>  The pathname string of the parent directory named by this
        ///          abstract pathname, or <code>null</code> if this pathname
        ///          does not name a parent </returns>
        public virtual string Parent
        {
            get
            {
                return fd.Parent;
            }
        }

        /// <summary>
        /// Returns the abstract pathname of this abstract pathname's parent,
        /// or <code>null</code> if this pathname does not name a parent
        /// directory.
        ///
        /// <para> The <em>parent</em> of an abstract pathname consists of the
        /// pathname's prefix, if any, and each name in the pathname's name
        /// sequence except for the last.  If the name sequence is empty then
        /// the pathname does not name a parent directory.
        ///
        /// </para>
        /// </summary>
        /// <returns>  The abstract pathname of the parent directory named by this
        ///          abstract pathname, or <code>null</code> if this pathname
        ///          does not name a parent </returns>
        public virtual OWFile ParentFile
        {
            get
            {
                return new OWFile(fd.OneWireContainers, fd.Parent);
            }
        }

        /// <summary>
        /// Converts this abstract pathname into a pathname string.  The resulting
        /// string uses the <seealso cref="#separator default name-separator character"/> to
        /// separate the names in the name sequence.
        /// </summary>
        /// <returns>  The string form of this abstract pathname </returns>
        public virtual string Path
        {
            get
            {
                return fd.Path;
            }
        }

        //--------
        //-------- Path Operations
        //--------

        /// <summary>
        /// Tests whether this abstract pathname is absolute.  The definition of
        /// absolute pathname is system dependent.  On UNIX systems, a pathname is
        /// absolute if its prefix is <code>"/"</code>.  On Win32 systems, a
        /// pathname is absolute if its prefix is a drive specifier followed by
        /// <code>"\\"</code>, or if its prefix is <code>"\\"</code>.
        /// </summary>
        /// <returns>  <code>true</code> if this abstract pathname is absolute,
        ///          <code>false</code> otherwise </returns>
        public virtual bool Absolute
        {
            get
            {
                // always absolute
                return true;
            }
        }

        /// <summary>
        /// Returns the absolute pathname string of this abstract pathname.
        ///
        /// <para> If this abstract pathname is already absolute, then the pathname
        /// string is simply returned as if by the <code><seealso cref="#getPath"/></code>
        /// method.  If this abstract pathname is the empty abstract pathname then
        /// the pathname string of the current user directory, which is named by the
        /// system property <code>user.dir</code>, is returned.  Otherwise this
        /// pathname is resolved in a system-dependent way.  On UNIX systems, a
        /// relative pathname is made absolute by resolving it against the current
        /// user directory.  On Win32 systems, a relative pathname is made absolute
        /// by resolving it against the current directory of the drive named by the
        /// pathname, if any; if not, it is resolved against the current user
        /// directory.
        ///
        /// </para>
        /// </summary>
        /// <returns>  The absolute pathname string denoting the same file or
        ///          directory as this abstract pathname
        /// </returns>
        /// <seealso cref=     java.io.File#isAbsolute() </seealso>
        public virtual string AbsolutePath
        {
            get
            {
                return fd.Path;
            }
        }

        /// <summary>
        /// Returns the absolute form of this abstract pathname.  Equivalent to
        /// <code>new&nbsp;File(this.<seealso cref="#getAbsolutePath"/>())</code>.
        /// </summary>
        /// <returns>  The absolute abstract pathname denoting the same file or
        ///          directory as this abstract pathname </returns>
        public virtual OWFile AbsoluteFile
        {
            get
            {
                return new OWFile(fd.OneWireContainers, fd.Path);
            }
        }

        /// <summary>
        /// Returns the canonical pathname string of this abstract pathname.
        ///
        /// <para> The precise definition of canonical form is system-dependent, but
        /// canonical forms are always absolute.  Thus if this abstract pathname is
        /// relative it will be converted to absolute form as if by the <code>{@link
        /// #getAbsoluteFile}</code> method.
        ///
        /// </para>
        /// <para> Every pathname that denotes an existing file or directory has a
        /// unique canonical form.  Every pathname that denotes a nonexistent file
        /// or directory also has a unique canonical form.  The canonical form of
        /// the pathname of a nonexistent file or directory may be different from
        /// the canonical form of the same pathname after the file or directory is
        /// created.  Similarly, the canonical form of the pathname of an existing
        /// file or directory may be different from the canonical form of the same
        /// pathname after the file or directory is deleted.
        ///
        /// </para>
        /// </summary>
        /// <returns>  The canonical pathname string denoting the same file or
        ///          directory as this abstract pathname
        /// </returns>
        /// <exception cref="IOException">
        ///          If an I/O error occurs, which is possible because the
        ///          construction of the canonical pathname may require
        ///          filesystem queries
        ///
        /// @since   JDK1.1 </exception>
        public virtual string CanonicalPath
        {
            get
            {
                return fd.Path;
            }
        }

        /// <summary>
        /// Returns the canonical form of this abstract pathname.  Equivalent to
        /// <code>new&nbsp;File(this.<seealso cref="#getCanonicalPath"/>())</code>.
        /// </summary>
        /// <returns>  The canonical pathname string denoting the same file or
        ///          directory as this abstract pathname
        /// </returns>
        /// <exception cref="IOException">
        ///          If an I/O error occurs, which is possible because the
        ///          construction of the canonical pathname may require
        ///          filesystem queries </exception>
        public virtual OWFile CanonicalFile
        {
            get
            {
                return new OWFile(fd.OneWireContainers, fd.Path);
            }
        }

        //--------
        //-------- Attribute 'get' Methods
        //--------

        /// <summary>
        /// Tests whether the application can read the file denoted by this
        /// abstract pathname.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the file specified by this
        ///          abstract pathname exists <em>and</em> can be read by the
        ///          application; <code>false</code> otherwise </returns>
        public virtual bool canRead()
        {
            return fd.canRead();
        }

        /// <summary>
        /// Tests whether the application can modify to the file denoted by this
        /// abstract pathname.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the Filesystem actually
        ///          contains a file denoted by this abstract pathname <em>and</em>
        ///          the application is allowed to write to the file;
        ///          <code>false</code> otherwise.
        ///  </returns>
        public virtual bool canWrite()
        {
            return fd.canWrite();
        }

        /// <summary>
        /// Tests whether the file denoted by this abstract pathname exists.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the file denoted by this
        ///          abstract pathname exists; <code>false</code> otherwise
        ///  </returns>
        public virtual bool exists()
        {
            return fd.exists();
        }

        /// <summary>
        /// Tests whether the file denoted by this abstract pathname is a
        /// directory.
        /// </summary>
        /// <returns> <code>true</code> if and only if the file denoted by this
        ///          abstract pathname exists <em>and</em> is a directory;
        ///          <code>false</code> otherwise </returns>
        public virtual bool Directory
        {
            get
            {
                return fd.Directory;
            }
        }

        /// <summary>
        /// Tests whether the file denoted by this abstract pathname is a normal
        /// file.  A file is <em>normal</em> if it is not a directory and, in
        /// addition, satisfies other system-dependent criteria.  Any non-directory
        /// file created by a Java application is guaranteed to be a normal file.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the file denoted by this
        ///          abstract pathname exists <em>and</em> is a normal file;
        ///          <code>false</code> otherwise </returns>
        public virtual bool File
        {
            get
            {
                return fd.File;
            }
        }

        /// <summary>
        /// Tests whether the file named by this abstract pathname is a hidden
        /// file.  The exact definition of <em>hidden</em> is system-dependent.  On
        /// UNIX systems, a file is considered to be hidden if its name begins with
        /// a period character (<code>'.'</code>).  On Win32 systems, a file is
        /// considered to be hidden if it has been marked as such in the filesystem.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the file denoted by this
        ///          abstract pathname is hidden according to the conventions of the
        ///          underlying platform </returns>
        public virtual bool Hidden
        {
            get
            {
                return fd.Hidden;
            }
        }

        /// <summary>
        /// Returns the time that the file denoted by this abstract pathname was
        /// last modified.
        /// </summary>
        /// <returns>  A <code>long</code> value representing the time the file was
        ///          last modified, measured in milliseconds since the epoch
        ///          (00:00:00 GMT, January 1, 1970), or <code>0L</code> if the
        ///          file does not exist or if an I/O error occurs </returns>
        public virtual long lastModified()
        {
            // not supported
            return 0;
        }

        /// <summary>
        /// Returns the length of the file denoted by this abstract pathname.
        /// </summary>
        /// <returns>  The length, in bytes, of the file denoted by this abstract
        ///          pathname, or <code>0L</code> if the file does not exist </returns>
        public virtual long length()
        {
            return fd.length();
        }

        //--------
        //-------- File Operation Methods
        //--------

        /// <summary>
        /// Atomically creates a new, empty file named by this abstract pathname if
        /// and only if a file with this name does not yet exist.  The check for the
        /// existence of the file and the creation of the file if it does not exist
        /// are a single operation that is atomic with respect to all other
        /// filesystem activities that might affect the file.
        /// </summary>
        /// <returns>  <code>true</code> if the named file does not exist and was
        ///          successfully created; <code>false</code> if the named file
        ///          already exists
        /// </returns>
        /// <exception cref="IOException">
        ///          If an I/O error occurred </exception>
        public virtual bool createNewFile()
        {
            return fd.createNewFile();
        }

        /// <summary>
        /// Deletes the file or directory denoted by this abstract pathname.  If
        /// this pathname denotes a directory, then the directory must be empty in
        /// order to be deleted.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the file or directory is
        ///          successfully deleted; <code>false</code> otherwise </returns>
        public virtual bool delete()
        {
            return fd.delete();
        }

        /// <summary>
        /// Returns an array of strings naming the files and directories in the
        /// directory denoted by this abstract pathname.
        ///
        /// <para> If this abstract pathname does not denote a directory, then this
        /// method returns <code>null</code>.  Otherwise an array of strings is
        /// returned, one for each file or directory in the directory.  Names
        /// denoting the directory itself and the directory's parent directory are
        /// not included in the result.  Each string is a file name rather than a
        /// complete path.
        ///
        /// </para>
        /// <para> There is no guarantee that the name strings in the resulting array
        /// will appear in any specific order; they are not, in particular,
        /// guaranteed to appear in alphabetical order.
        ///
        /// </para>
        /// </summary>
        /// <returns>  An array of strings naming the files and directories in the
        ///          directory denoted by this abstract pathname.  The array will be
        ///          empty if the directory is empty.  Returns <code>null</code> if
        ///          this abstract pathname does not denote a directory, or if an
        ///          I/O error occurs. </returns>
        public virtual string[] list()
        {
            if (File || !Directory)
            {
                return null;
            }
            else
            {
                return fd.list();
            }
        }

        /// <summary>
        /// Returns an array of abstract pathnames denoting the files in the
        /// directory denoted by this abstract pathname.
        ///
        /// <para> If this abstract pathname does not denote a directory, then this
        /// method returns <code>null</code>.  Otherwise an array of
        /// <code>OWFile</code> objects is returned, one for each file or directory in
        /// the directory.  Pathnames denoting the directory itself and the
        /// directory's parent directory are not included in the result.  Each
        /// resulting abstract pathname is constructed from this abstract pathname
        /// using the <code>{@link #OWFile(com.dalsemi.onewire.application.file.OWFile, java.lang.String)
        /// OWFile(OWFile,&nbsp;String)}</code> constructor.  Therefore if this pathname
        /// is absolute then each resulting pathname is absolute; if this pathname
        /// is relative then each resulting pathname will be relative to the same
        /// directory.
        ///
        /// </para>
        /// <para> There is no guarantee that the name strings in the resulting array
        /// will appear in any specific order; they are not, in particular,
        /// guaranteed to appear in alphabetical order.
        ///
        /// </para>
        /// </summary>
        /// <returns>  An array of abstract pathnames denoting the files and
        ///          directories in the directory denoted by this abstract
        ///          pathname.  The array will be empty if the directory is
        ///          empty.  Returns <code>null</code> if this abstract pathname
        ///          does not denote a directory, or if an I/O error occurs. </returns>
        public virtual OWFile[] listFiles()
        {
            if (File || !Directory)
            {
                return null;
            }
            else
            {
                string[] str_list;
                OWFile[] file_list;
                string new_path;

                str_list = fd.list();
                file_list = new OWFile[str_list.Length];

                for (int i = 0; i < str_list.Length; i++)
                {
                    if ((string.ReferenceEquals(fd.Path, null)) || fd.Path.EndsWith("/", StringComparison.Ordinal))
                    {
                        new_path = "/" + str_list[i];
                    }
                    else
                    {
                        new_path = fd.Path + separator + str_list[i];
                    }

                    file_list[i] = new OWFile(fd.OneWireContainers, new_path);
                }

                return file_list;
            }
        }

        /// <summary>
        /// Creates the directory named by this abstract pathname.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the directory was
        ///          created; <code>false</code> otherwise </returns>
        public virtual bool mkdir()
        {
            try
            {
                fd.create(false, true, false, -1, -1);

                return true;
            }
            catch (OWFileNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates the directory named by this abstract pathname, including any
        /// necessary but nonexistent parent directories.  Note that if this
        /// operation fails it may have succeeded in creating some of the necessary
        /// parent directories.
        /// </summary>
        /// <returns>  <code>true</code> if and only if the directory was created,
        ///          along with all necessary parent directories; <code>false</code>
        ///          otherwise </returns>
        public virtual bool mkdirs()
        {
            try
            {
                fd.create(false, true, true, -1, -1);

                return true;
            }
            catch (OWFileNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Renames the file denoted by this abstract pathname.
        /// </summary>
        /// <param name="dest">  The new abstract pathname for the named file
        /// </param>
        /// <returns>  <code>true</code> if and only if the renaming succeeded;
        ///          <code>false</code> otherwise
        /// </returns>
        /// <exception cref="NullPointerException">
        ///          If parameter <code>dest</code> is <code>null</code> </exception>
        public virtual bool renameTo(OWFile dest)
        {
            return fd.renameTo(dest);
        }

        /// <summary>
        /// Sets the last-modified time of the file or directory named by this
        /// abstract pathname.
        ///
        /// <para> All platforms support file-modification times to the nearest second,
        /// but some provide more precision.  The argument will be truncated to fit
        /// the supported precision.  If the operation succeeds and no intervening
        /// operations on the file take place, then the next invocation of the
        /// <code><seealso cref="#lastModified"/></code> method will return the (possibly
        /// truncated) <code>time</code> argument that was passed to this method.
        ///
        /// </para>
        /// </summary>
        /// <param name="time">  The new last-modified time, measured in milliseconds since
        ///               the epoch (00:00:00 GMT, January 1, 1970)
        /// </param>
        /// <returns> <code>true</code> if and only if the operation succeeded;
        ///          <code>false</code> otherwise
        /// </returns>
        /// <exception cref="IllegalArgumentException">  If the argument is negative </exception>
        public virtual bool setLastModified(long time)
        {
            return false; // not supported
        }

        /// <summary>
        /// Marks the file or directory named by this abstract pathname so that
        /// only read operations are allowed.  After invoking this method the file
        /// or directory is guaranteed not to change until it is either deleted or
        /// marked to allow write access.  Whether or not a read-only file or
        /// directory may be deleted depends upon the underlying system.
        /// </summary>
        /// <returns> <code>true</code> if and only if the operation succeeded;
        ///          <code>false</code> otherwise </returns>
        public virtual bool setReadOnly()
        {
            bool result = fd.setReadOnly();

            return result;
        }

        //--------
        //-------- Filesystem Interface Methods
        //--------

        /// <summary>
        /// List the available filesystem roots.
        ///
        /// <para> A particular Java platform may support zero or more
        /// hierarchically-organized Filesystems.  Each Filesystem has a
        /// <code>root</code> directory from which all other files in that file
        /// system can be reached.  Windows platforms, for example, have a root
        /// directory for each active drive; UNIX platforms have a single root
        /// directory, namely <code>"/"</code>.  The set of available filesystem
        /// roots is affected by various system-level operations such the insertion
        /// or ejection of removable media and the disconnecting or unmounting of
        /// physical or virtual disk drives.
        ///
        /// </para>
        /// <para> This method returns an array of <code>OWFile</code> objects that
        /// denote the root directories of the available filesystem roots.  It is
        /// guaranteed that the canonical pathname of any file physically present on
        /// the local machine will begin with one of the roots returned by this
        /// method.
        ///
        /// </para>
        /// <para> The canonical pathname of a file that resides on some other machine
        /// and is accessed via a remote-filesystem protocol such as SMB or NFS may
        /// or may not begin with one of the roots returned by this method.  If the
        /// pathname of a remote file is syntactically indistinguishable from the
        /// pathname of a local file then it will begin with one of the roots
        /// returned by this method.  Thus, for example, <code>OWFile</code> objects
        /// denoting the root directories of the mapped network drives of a Windows
        /// platform will be returned by this method, while <code>OWFile</code>
        /// objects containing UNC pathnames will not be returned by this method.
        ///
        /// </para>
        /// </summary>
        /// <param name="owc">  OneWireContainer that this Filesystem resides on
        /// </param>
        /// <returns>  An array of <code>OWFile</code> objects denoting the available
        ///          filesystem roots, or <code>null</code> if the set of roots
        ///          could not be determined.  The array will be empty if there are
        ///          no filesystem roots. </returns>
        public static OWFile[] listRoots(OneWireContainer owc)
        {
            OWFile[] roots = new OWFile[1];

            roots[0] = new OWFile(owc, "/");

            return roots;
        }

        //--------
        //-------- Misc Methods
        //--------

        /// <summary>
        /// Compares two abstract pathnames lexicographically.  The ordering
        /// defined by this method depends upon the underlying system.  On UNIX
        /// systems, alphabetic case is significant in comparing pathnames; on Win32
        /// systems it is not.
        /// </summary>
        /// <param name="pathname">  The abstract pathname to be compared to this abstract
        ///                    pathname
        /// </param>
        /// <returns>  Zero if the argument is equal to this abstract pathname, a
        ///          value less than zero if this abstract pathname is
        ///          lexicographically less than the argument, or a value greater
        ///          than zero if this abstract pathname is lexicographically
        ///    -      greater than the argument </returns>
        public virtual int compareTo(OWFile pathname)
        {
            OneWireContainer[] owd = fd.OneWireContainers;
            string this_path = owd[0].AddressAsString + Path;
            string compare_path = pathname.OneWireContainer.AddressAsString + pathname.Path;

            return this_path.CompareTo(compare_path);
        }

        /// <summary>
        /// Compares this abstract pathname to another object.  If the other object
        /// is an abstract pathname, then this function behaves like <code>{@link
        /// #compareTo(OWFile)}</code>.  Otherwise, it throws a
        /// <code>ClassCastException</code>, since abstract pathnames can only be
        /// compared to abstract pathnames.
        /// </summary>
        /// <param name="o">  The <code>Object</code> to be compared to this abstract
        ///             pathname
        /// </param>
        /// <returns>  If the argument is an abstract pathname, returns zero
        ///          if the argument is equal to this abstract pathname, a value
        ///          less than zero if this abstract pathname is lexicographically
        ///          less than the argument, or a value greater than zero if this
        ///          abstract pathname is lexicographically greater than the
        ///          argument
        /// </returns>
        /// @throws  <code>ClassCastException</code> if the argument is not an
        ///          abstract pathname
        /// </exception>
        /// <seealso cref=     java.lang.Comparable </seealso>
        public virtual int compareTo(object o)
        {
            return compareTo((OWFile)o);
        }

        /// <summary>
        /// Tests this abstract pathname for equality with the given object.
        /// Returns <code>true</code> if and only if the argument is not
        /// <code>null</code> and is an abstract pathname that denotes the same file
        /// or directory as this abstract pathname.  Whether or not two abstract
        /// pathnames are equal depends upon the underlying system.  On UNIX
        /// systems, alphabetic case is significant in comparing pathnames; on Win32
        /// systems it is not.
        /// </summary>
        /// <param name="obj">   The object to be compared with this abstract pathname
        /// </param>
        /// <returns>  <code>true</code> if and only if the objects are the same;
        ///          <code>false</code> otherwise </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is OWFile))
            {
                return false;
            }

            return (compareTo((OWFile)obj) == 0);
        }

        /// <summary>
        /// Computes a hash code for this abstract pathname.  Because equality of
        /// abstract pathnames is inherently system-dependent, so is the computation
        /// of their hash codes.  On UNIX systems, the hash code of an abstract
        /// pathname is equal to the exclusive <em>or</em> of its pathname string
        /// and the decimal value <code>1234321</code>.  On Win32 systems, the hash
        /// code is equal to the exclusive <em>or</em> of its pathname string,
        /// convered to lower case, and the decimal value <code>1234321</code>.
        /// </summary>
        /// <returns>  A hash code for this abstract pathname </returns>
        public override int GetHashCode()
        {
            return fd.HashCode;
        }

        /// <summary>
        /// Returns the pathname string of this abstract pathname.  This is just the
        /// string returned by the <code><seealso cref="#getPath"/></code> method.
        /// </summary>
        /// <returns>  The string form of this abstract pathname </returns>
        public override string ToString()
        {
            return fd.Path;
        }

        //--------
        //-------- Custom additions, not normally in File class
        //--------

        /// <summary>
        /// Returns the <code>OWFileDescriptor</code>
        /// object  that represents the connection to
        /// the actual file in the Filesystem being
        /// used by this <code>OWFileInputStream</code>.
        /// </summary>
        /// <returns>     the file descriptor object associated with this File. </returns>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        /// <seealso cref=        com.dalsemi.onewire.application.file.OWFileDescriptor </seealso>
        public virtual OWFileDescriptor FD
        {
            get
            {
                return fd;
            }
        }

        /// <summary>
        /// Gets the OneWireContainer that this File resides on.  This
        /// is where the 'filesystem' resides.  If this Filesystem
        /// spans multiple devices then this method returns the
        /// 'MASTER' device.
        /// </summary>
        /// <returns>     the OneWireContainer for this Filesystem </returns>
        public virtual OneWireContainer OneWireContainer
        {
            get
            {
                OneWireContainer[] owd = fd.OneWireContainers;
                return owd[0];
            }
        }

        /// <summary>
        /// Gets the OneWireContainer(s) that this File resides on.  This
        /// is where the 'filesystem' resides.  The first device
        /// is the 'MASTER' device and the other devices are 'SATELLITE'
        /// devices.
        /// </summary>
        /// <returns>     the OneWireContainer(s) for this Filesystem </returns>
        public virtual OneWireContainer[] OneWireContainers
        {
            get
            {
                return fd.OneWireContainers;
            }
        }

        /// <summary>
        /// Format the Filesystem on the 1-Wire device provided in
        /// the constructor.  This operation is required before any
        /// file IO is possible. <P>
        /// <b>WARNING</b> this will remove any files/directories.
        /// <P> </summary>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual void format()
        {
            try
            {
                fd.format();
            }
            catch (OneWireException e)
            {
                throw new System.IO.IOException(e.ToString());
            }
        }

        /// <summary>
        /// Gets the number of bytes available on this device for
        /// file and directory information.
        /// </summary>
        /// <returns>     number of free bytes in the Filesystem
        /// </returns>
        /// <exception cref="IOException">  if an I/O error occurs </exception>
        public virtual int FreeMemory
        {
            get
            {
                try
                {
                    return fd.FreeMemory;
                }
                catch (OneWireException e)
                {
                    throw new System.IO.IOException(e.ToString());
                }
            }
        }

        /// <summary>
        /// Closes this file and releases any system resources
        /// associated with this stream. This file may no longer
        /// be used after this operation.
        /// </summary>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual void close()
        {
            fd.close();

            fd = null;
        }

        /// <summary>
        /// Get's an array of integers that represents the page
        /// list of the file or directory represented by this
        /// OWFile.
        /// </summary>
        /// <returns>     node page list file or directory
        /// </returns>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        public virtual int[] PageList
        {
            get
            {
                if (fd != null)
                {
                    if (!fd.exists())
                    {
                        return new int[0];
                    }
                }
                else
                {
                    return new int[0];
                }

                try
                {
                    return fd.PageList;
                }
                catch (OneWireException e)
                {
                    throw new System.IO.IOException(e.ToString());
                }
            }
        }

        /// <summary>
        /// Returns an integer which represents the starting memory page
        /// of the file or directory represented by this OWFile.
        /// </summary>
        /// <returns> The starting page of the file or directory.
        /// </returns>
        /// <exception cref="IOException"> if the file doesn't exist </exception>
        public virtual int StartPage
        {
            get
            {
                if (fd != null && fd.exists())
                {
                    return fd.StartPage;
                }
                else
                {
                    throw new System.IO.FileNotFoundException();
                }
            }
        }

        /// <summary>
        /// Get's the memory bank object for the specified page.
        /// This is significant if the Filesystem spans memory banks
        /// on the same or different devices.
        /// </summary>
        /// <returns>   PagedMemoryBank for the specified page </returns>
        public virtual PagedMemoryBank getMemoryBankForPage(int page)
        {
            if (fd != null)
            {
                if (!fd.exists())
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            return fd.getMemoryBankForPage(page);
        }

        /// <summary>
        /// Get's the local page number on the memory bank object for
        /// the specified page.
        /// This is significant if the Filesystem spans memory banks
        /// on the same or different devices.
        /// </summary>
        /// <returns>  local page for the specified Filesystem page
        ///          (memory bank specific) </returns>
        public virtual int getLocalPage(int page)
        {
            if (fd != null)
            {
                if (!fd.exists())
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }

            return fd.getLocalPage(page);
        }

        /// <summary>
        /// Cleans up the connection to the file, and ensures that the
        /// <code>close</code> method of this file output stream is
        /// called when there are no more references to this stream.
        /// </summary>
        /// <exception cref="IOException">  if an I/O error occurs. </exception>
        /// <seealso cref=        java.io.FileInputStream#close() </seealso>
        ~OWFile()
        {
            if (fd != null)
            {
                fd.close();
            }
        }
    }
}