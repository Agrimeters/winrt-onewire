using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

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

	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OneWireIOException = com.dalsemi.onewire.adapter.OneWireIOException;
	using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;
	using Address = com.dalsemi.onewire.utils.Address;
	//using Convert = com.dalsemi.onewire.utils.Convert;
	using Bit = com.dalsemi.onewire.utils.Bit;
	using PagedMemoryBank = com.dalsemi.onewire.container.PagedMemoryBank;

	/// <summary>
	/// Instances of the 1-Wire file descriptor class serve as an opaque handle
	/// to the underlying machine-specific structure representing an open
	/// file, an open socket, or another source or sink of bytes. The
	/// main practical use for a file descriptor is to create a
	/// <code>OWFileInputStream</code> or <code>OWFileOutputStream</code> to
	/// contain it.
	/// <para>
	/// Applications should not create their own file descriptors.
	/// 
	/// @author  DS
	/// @version 0.01, 1 June 2001
	/// </para>
	/// </summary>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFile </seealso>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFileInputStream </seealso>
	/// <seealso cref=     com.dalsemi.onewire.application.file.OWFileOutputStream </seealso>
	public class OWFileDescriptor
	{
	   //--------
	   //-------- Static Variables
	   //--------

	   /// <summary>
	   /// Hashtable to contain MemoryCache instances (one per container) </summary>
	   private static Hashtable memoryCacheHash = new Hashtable(4);

	   /// <summary>
	   /// Field EXT_DIRECTORY entension value </summary>
	   private const byte EXT_DIRECTORY = 0x007F;

	   /// <summary>
	   /// Field EXT_UNKNOWN marker in path vector to indicate don't
	   /// know if file or directory
	   /// </summary>
	   private const byte EXT_UNKNOWN = 0x007E;

	   /// <summary>
	   /// Field BM_CACHE bitmap type MemoryCache </summary>
	   private const int BM_CACHE = 0;

	   /// <summary>
	   /// Field BM_LOCAL bitmap type Local (in directory page 0) </summary>
	   private const int BM_LOCAL = 1;

	   /// <summary>
	   /// Field BM_FILE bitmap type file, in an external file </summary>
	   private const int BM_FILE = 2;

	   /// <summary>
	   /// Field PAGE_USED marker for a used page in the bitmap </summary>
	   private const int PAGE_USED = 1;

	   /// <summary>
	   /// Field PAGE_NOT_USED marker for an unused page in the bitmap </summary>
	   private const int PAGE_NOT_USED = 0;

	   /// <summary>
	   /// Field LEN_FILENAME </summary>
	   private const int LEN_FILENAME = 5;

	   /// <summary>
	   /// Enable/disable debug messages </summary>
	   private const bool doDebugMessages = true;

	   //--------
	   //-------- Variables
	   //--------

	   /// <summary>
	   /// Field address - 1-Wire device address </summary>
	   private long? address;

	   /// <summary>
	   /// Field cache - used to read/write 1-Wire device </summary>
	   private MemoryCache cache;

	   /// <summary>
	   /// Field owd - is the 1-Wire container </summary>
	   private OneWireContainer[] owd;

	   /// <summary>
	   /// Field rawPath - what was provided in constructor except for toUpper </summary>
	   private string rawPath;

	   /// <summary>
	   /// Field path - converted path to vector of 5 byte arrays </summary>
	   private ArrayList path;

	   /// <summary>
	   /// Field verbosePath - same as 'path' but includes '.' and '..' </summary>
	   private ArrayList verbosePath;

	   //--------
	   // file entry (fe) info on device
	   //--------

	   /// <summary>
	   /// Field fePage - File Entry page number </summary>
	   private int fePage;

	   /// <summary>
	   /// Field feOffset - Offset into File Entry page </summary>
	   private int feOffset;

	   /// <summary>
	   /// Field feData - buffer containing the last File Entry Page </summary>
	   private byte[] feData;

	   /// <summary>
	   /// Field feLen - length of packet in the last File Entry Page </summary>
	   private int feLen;

	   /// <summary>
	   /// Field feNumPages - Number of Pages specified in File Entry </summary>
	   private int feNumPages;

	   /// <summary>
	   /// Field feStartPage - Start Page specified in the File Entry </summary>
	   private int feStartPage;

	   /// <summary>
	   /// Field feParentPage - Parent page of current File Entry Page </summary>
	   private int feParentPage;

	   /// <summary>
	   /// Field feParentOffset - Offset into Parent page </summary>
	   private int feParentOffset;

	   //--------
	   // file read/write info
	   //--------

	   /// <summary>
	   /// Field lastPage - last page read </summary>
	   private int lastPage;

	   /// <summary>
	   /// Field lastOffset - offset into last page read </summary>
	   private int lastOffset;

	   /// <summary>
	   /// Field lastLen - length of last page read </summary>
	   private int lastLen;

	   /// <summary>
	   /// Field lastPageData - buffer for the last page read </summary>
	   private byte[] lastPageData;

	   /// <summary>
	   /// Field filePosition - overall file position when reading </summary>
	   private int filePosition;

	   /// <summary>
	   /// Field markPosition - mark position in read file </summary>
	   private int markPosition;

	   /// <summary>
	   /// Field markLimit - mark position limit </summary>
	   private int markLimit;

	   //--------
	   // total device info
	   //--------

	   /// <summary>
	   /// Field totalPages - number of pages in filesystem </summary>
	   private int totalPages;

	   /// <summary>
	   /// Field rootTotalPages - number of pages on the ROOT device in the filesystem </summary>
	   private int rootTotalPages;

	   /// <summary>
	   /// Field maxDataLen - max data per page including page pointer </summary>
	   private int maxDataLen;

	   /// <summary>
	   /// Field LEN_PAGE_PTR - length in bytes for the page pointer </summary>
	   private int LEN_PAGE_PTR;

	   /// <summary>
	   /// Field LEN_FILE_ENTRY - length in bytes of the directory file entry </summary>
	   private int LEN_FILE_ENTRY;

	   /// <summary>
	   /// Field LEN_FILE_ENTRY - length in bytes of the directory control Data </summary>
	   private int LEN_CONTROL_DATA;

	   /// <summary>
	   /// Field openedToWrite - flag to indicate file is opened for writing </summary>
	   private bool openedToWrite;

	   //--------
	   // page used bitmap
	   //--------

	   /// <summary>
	   /// Field lastFreePage - last free page </summary>
	   private int lastFreePage;

	   /// <summary>
	   /// Field bitmapType - type of page bitmap </summary>
	   private int bitmapType;

	   /// <summary>
	   /// Field pbm - buffer containering the current image for the page bitmap </summary>
	   private byte[] pbm;

	   /// <summary>
	   /// Field pbmByteOffset - byte offset into page bitmap </summary>
	   private int pbmByteOffset;

	   /// <summary>
	   /// Field pbmBitOffset - bit offset into page bitmap </summary>
	   private int pbmBitOffset;

	   /// <summary>
	   /// Field pbmStartPage - start page of page bitmap </summary>
	   private int pbmStartPage;

	   /// <summary>
	   /// Field pbmNumPages - number of pages in the page bitmap </summary>
	   private int pbmNumPages;

	   //--------
	   // Misc
	   //--------

	   /// <summary>
	   /// Field tempPage - temporary page buffer </summary>
	   private byte[] tempPage;

	   /// <summary>
	   /// Field initName - image of blank directory entry, used in parsing </summary>
	   private byte[] initName = new byte[] {0x20, 0x20, 0x20, 0x20, EXT_UNKNOWN};

	   /// <summary>
	   /// Field smallBuf - small buffer </summary>
	   private byte[] smallBuf;

	   /// <summary>
	   /// Field dmBuf - device map page buffer </summary>
	   private byte[] dmBuf;

	   /// <summary>
	   /// Field addrBuf - address buffer </summary>
	   private byte[] addrBuf;

	   //--------
	   //-------- Constructors
	   //--------

	   /// <summary>
	   /// Construct an invalid 1-Wire FileDescriptor
	   /// 
	   /// </summary>
	   public OWFileDescriptor()
	   {
		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===Invalid Constructor OWFileDescriptor ");
		  }
	   }

	   /// <summary>
	   /// Construct a 1-Wire FileDescrioptor providing the Filesystem
	   /// 1-Wire device and file path.
	   /// </summary>
	   /// <param name="owd"> - 1-Wire container where the filesystem resides </param>
	   /// <param name="newPath"> - path containing the file/directory that represents
	   ///                  this file descriptor </param>
	   protected internal OWFileDescriptor(OneWireContainer owd, string newPath)
	   {
		  OneWireContainer[] devices = new OneWireContainer[1];
		  devices[0] = owd;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===Constructor OWFileDescriptor with device: " + devices[0].AddressAsString + " and path: " + newPath);
		  }
		  setupFD(devices,newPath);
	   }

	   /// <summary>
	   /// Construct a 1-Wire FileDescrioptor providing the Filesystem
	   /// 1-Wire device and file path.
	   /// </summary>
	   /// <param name="owd"> - 1-Wire container where the filesystem resides </param>
	   /// <param name="newPath"> - path containing the file/directory that represents
	   ///                  this file descriptor </param>
	   protected internal OWFileDescriptor(OneWireContainer[] owd, string newPath)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===Constructor OWFileDescriptor with device: " + owd[0].AddressAsString + " and path: " + newPath);
		  }
		  setupFD(owd,newPath);
	   }

	   /// <summary>
	   /// Setups the 1-Wire FileDescrioptor providing the Filesystem
	   /// 1-Wire device(s) and file path.
	   /// </summary>
	   /// <param name="owd"> - 1-Wire container where the filesystem resides </param>
	   /// <param name="newPath"> - path containing the file/directory that represents
	   ///                  this file descriptor </param>
	   protected internal virtual void setupFD(OneWireContainer[] owd, string newPath)
	   {
		  // synchronize with the static memoryCacheHash while initializing
		  lock (memoryCacheHash)
		  {
			 // keep reference to container, adapter, and name
			 this.owd = owd;

			 if (!string.ReferenceEquals(newPath, null))
			 {
				this.rawPath = newPath.ToUpper();
			 }
			 else
			 {
				this.rawPath = "";
			 }

			 // check the hash to see if already have a MemoryCache for this device
			 address = new long?(owd[0].AddressAsLong);
			 cache = (MemoryCache) memoryCacheHash[address];

			 if (cache == null)
			 {
				// create a new cache
				cache = new MemoryCache(owd);

				// add to hash
				memoryCacheHash[address] = cache;
			 }

			 // indicate this fd uses this cache, used later in cleanup
			 cache.addOwner(this);

			 // get info on device through cache
			 totalPages = cache.NumberPages;
			 rootTotalPages = cache.getNumberPagesInBank(0);
			 maxDataLen = cache.MaxPacketDataLength;
			 openedToWrite = false;

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("=cache has totalPages = " + totalPages + " with max data " + maxDataLen);
			 }

			 // construct the page bufs
			 lastPageData = new byte [maxDataLen];
			 tempPage = new byte [lastPageData.Length];
			 feData = new byte [lastPageData.Length];
			 dmBuf = new byte [lastPageData.Length];
			 smallBuf = new byte [10];
			 addrBuf = new byte [8];

			 // guese at the number of bytes to represent a page number
			 // since have not read the root directory yet this may change
			 LEN_PAGE_PTR = (totalPages > 256) ? 2 : 1;
			 LEN_FILE_ENTRY = LEN_FILENAME + LEN_PAGE_PTR * 2;
			 LEN_CONTROL_DATA = 6 + LEN_PAGE_PTR;

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("=Number of page bytes = " + LEN_PAGE_PTR + " with directory entry size of " + LEN_FILE_ENTRY);
			 }

			 // decide what type of bitmap we will have
			 if (cache.handlePageBitmap())
			 {
				bitmapType = BM_CACHE;
			 }
			 else if (totalPages <= 32)
			 {
				bitmapType = BM_LOCAL;

				// make PageBitMap max size of first page of directory
				pbm = new byte [maxDataLen];
				pbmByteOffset = 3;
				pbmBitOffset = 0;
			 }
			 else
			 {
				bitmapType = BM_FILE;

				// make PageBitMap correct size number of pages in fs
				pbm = new byte [totalPages / 8 + LEN_PAGE_PTR];
				pbmByteOffset = 0;
				pbmBitOffset = 0;
			 }
			 pbmStartPage = -1;

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("=Page BitMap type is " + bitmapType + " with bit offset of " + pbmBitOffset);
			 }

			 // parse the path into a Vector
			 verbosePath = new ArrayList(3);

			 // done could not parse the skip compressing
			 if (!parsePath(rawPath, verbosePath))
			 {
				return;
			 }

			 // create a compressed path (take out "." and "..")
			 path = new ArrayList(verbosePath.Count);

			 byte[] element;

			 for (int element_num = 0; element_num < verbosePath.Count; element_num++)
			 {
				element = (byte[]) verbosePath[element_num];

				// ".."
				if ((element [0] == '.') && (element [1] == '.'))
				{

				   // remove last entry in path
				   if (path.Count > 0)
				   {
					  path.RemoveAt(path.Count - 1);
				   }
				   else
				   {
					  path = null;
					  break;
				   }
				}

				// not "." (so ignore entries ".")
				else if (element [0] != '.')
				{
				   path.Add(element);
				}
			 }
		  }
	   }

	   //--------
	   //-------- Standard FileDescriptor methods
	   //--------

	   /// <summary>
	   /// Tests if this file descriptor object is valid.
	   /// </summary>
	   /// <returns>  <code>true</code> if the file descriptor object represents a
	   ///          valid, open file, socket, or other active I/O connection;
	   ///          <code>false</code> otherwise. </returns>
	   public virtual bool valid()
	   {
		  lock (cache)
		  {
			 return (cache != null);
		  }
	   }

	   /// <summary>
	   /// Force all system buffers to synchronize with the underlying
	   /// device.  This method returns after all modified data and
	   /// attributes of this FileDescriptor have been written to the
	   /// relevant device(s).  In particular, if this FileDescriptor
	   /// refers to a physical storage medium, such as a file in a file
	   /// system, sync will not return until all in-memory modified copies
	   /// of buffers associated with this FileDesecriptor have been
	   /// written to the physical medium.
	   /// 
	   /// sync is meant to be used by code that requires physical
	   /// storage (such as a file) to be in a known state  For
	   /// example, a class that provided a simple transaction facility
	   /// might use sync to ensure that all changes to a file caused
	   /// by a given transaction were recorded on a storage medium.
	   /// 
	   /// sync only affects buffers downstream of this FileDescriptor.  If
	   /// any in-memory buffering is being done by the application (for
	   /// example, by a BufferedOutputStream object), those buffers must
	   /// be flushed into the OWFileDescriptor (for example, by invoking
	   /// OutputStream.flush) before that data will be affected by sync.
	   /// 
	   /// <para>This method may be called multiple times if the source of
	   /// OWSyncFailedException has been rectified (1-Wire device was
	   /// reattached to the network).
	   /// 
	   /// </para>
	   /// </summary>
	   /// <exception cref="OWSyncFailedException">
	   ///        Thrown when the buffers cannot be flushed,
	   ///        or because the system cannot guarantee that all the
	   ///        buffers have been synchronized with physical media. </exception>
	   public virtual void sync()
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===sync");
		  }

		  try
		  {
			 lock (cache)
			 {
				// clear last page read flag
				cache.clearLastPageRead();

				// flush the writes to the device
				cache.sync();
			 }
		  }
		  catch (OneWireIOException e)
		  {
			 throw new OWSyncFailedException(e.ToString());
		  }
		  catch (OneWireException e)
		  {
			 throw new OWSyncFailedException(e.ToString());
		  }
	   }

	   //--------
	   //-------- General File methods
	   //--------

	   /// <summary>
	   /// Opens the file for reading.  If successfull (no exceptions) then
	   /// the following class member variables will be set:
	   /// <ul>
	   /// <li> fePage - File Entry page number
	   /// <li> feOffset - Offset into File Entry page
	   /// <li> feData - buffer containing the last File Entry Page
	   /// <li> feLen - length of packet in the last File Entry Page
	   /// <li> feNumPages - Number of Pages specified in File Entry
	   /// <li> feStartPage - Start Page specified in the File Entry
	   /// <li> feParentPage - Parent page of current File Entry Page
	   /// <li> feParentOffset - Offset into Parent page
	   /// <li> lastPage - (file only) last page read
	   /// <li> lastOffset - (file only) offset into last page read
	   /// <li> lastLen - (file only) length of last page read
	   /// <li> lastPageData - (file only) buffer for the last page read
	   /// <li> filePosition - (file only) overall file position when reading
	   /// </ul>
	   /// </summary>
	   /// <exception cref="FileNotFoundException"> when the file/directory path is invalid or
	   ///         there was an IOException thrown when trying to read the device. </exception>
	   protected internal virtual void open()
	   {
		  string last_error = null;
		  int cnt = 0;

		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===open");
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // reset the file position
			 lastPage = -1;

			 // check if had an invalid path
			 if (path == null)
			 {
				throw new OWFileNotFoundException("Invalid path");
			 }

			 // check if have an empty path
			 if (path.Count == 0)
			 {
				throw new OWFileNotFoundException("Invalid path, no elements");
			 }

			 // check to see if this file entry has been found
			 if (feStartPage <= 0)
			 {

				// loop up to 2 times if getting 1-Wire IO exceptions
				do
				{
				   try
				   {
					  if (verifyPath(path.Count))
					  {
						 return;
					  }
				   }
				   catch (OneWireException e)
				   {
					  last_error = e.ToString();
				   }
				} while (cnt++ < 2);

				// could not find file so pass along the last error
				throw new OWFileNotFoundException(last_error);
			 }
		  }
	   }

	   /// <summary>
	   /// Closes this file descriptor and releases any system resources
	   /// associated with this stream.  Any cached writes are flushed into
	   /// the filesystem.  This file descriptor may no longer
	   /// be used for writing bytes. If successfull (no exceptions) then
	   /// the following class member variables will be set:
	   /// <ul>
	   /// <li> fePage - File Entry page number
	   /// <li> feOffset - Offset into File Entry page
	   /// <li> feData - buffer containing the last File Entry Page
	   /// <li> feLen - length of packet in the last File Entry Page
	   /// <li> feNumPages - Number of Pages specified in File Entry
	   /// <li> feStartPage - Start Page specified in the File Entry
	   /// <li> feParentPage - Parent page of current File Entry Page
	   /// <li> feParentOffset - Offset into Parent page
	   /// <li> lastPage - (file only) last page read
	   /// <li> lastOffset - (file only) offset into last page read
	   /// <li> lastLen - (file only) length of last page read
	   /// <li> lastPageData - (file only) buffer for the last page read
	   /// <li> filePosition - (file only) overall file position when reading
	   /// </ul>
	   /// </summary>
	   /// <exception cref="IOException"> if an I/O error occurs </exception>
	   protected internal virtual void close()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===close");
			 }

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("thread " + System.Environment.CurrentManagedThreadId);
                Debug.WriteLine(System.Environment.StackTrace);
			 }

			 // sync the cache to the device
			 try
			 {
				sync();
			 }
			 catch (OWSyncFailedException e)
			 {
				throw new System.IO.IOException(e.ToString());
			 }

			 // free the resources for this fd
			 free();
		  }
	   }

	   /// <summary>
	   /// Creates a directory or file to write.
	   /// </summary>
	   /// <param name="append">  for files only, true to append data to end of file,
	   ///                 false to reset the file </param>
	   /// <param name="isDirectory">  true if creating a directory, false for a file </param>
	   /// <param name="makeParents">  true if creating all needed parent directories
	   ///                  in order to create the file/directory </param>
	   /// <param name="startPageNum"> starting page of file/directory, -1 if not
	   ///        renaming </param>
	   /// <param name="numberPages"> number of pages in file/directory, -1 if not
	   ///        renaming
	   /// </param>
	   /// <exception cref="FileNotFoundException"> if file already opened to write, if
	   ///          makeParents=false and parent directories not found, if
	   ///          file is read only, or if there is an IO error reading
	   ///          filesystem </exception>
	   protected internal virtual void create(bool append, bool isDirectory, bool makeParents, int startPage, int numberPages)
	   {
		  byte[] element;
		  bool element_found;

		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===create, appent=" + append + " isDirectory=" + isDirectory + " makeParents=" + makeParents);
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // reset the file position
			 lastPage = -1;

			 // check if had an invalid path
			 if (path == null)
			 {
				throw new OWFileNotFoundException("Invalid path");
			 }

			 // check if have an empty path
			 if (path.Count == 0)
			 {
				throw new OWFileNotFoundException("Invalid path, no elements");
			 }

			 // make sure last element in path is a directory (or unknown) if making directory
			 if (isDirectory || makeParents)
			 {
				element = (byte[]) path[path.Count - 1];

				if (((element [4] & 0x7F) != EXT_UNKNOWN) && ((element [4] & 0x7F) != EXT_DIRECTORY))
				{
				   throw new OWFileNotFoundException("Invalid path, directory has an extension");
				}
			 }

			 // check if file is already opened to write
			 if (!isDirectory)
			 {
				if (cache.isOpenedToWrite(owd[0].AddressAsString + Path, true))
				{
				   throw new OWFileNotFoundException("File already opened to write");
				}

				openedToWrite = true;
			 }

			 // loop through the path elements, creating directories/file as needed
			 feStartPage = 0;
			 bool file_exists = false;
			 byte[] prev_element = new byte[] {0x52, 0x4F, 0x4F, 0x54};
			 int prev_element_start = 0;

			 for (int element_num = 0; element_num < path.Count; element_num++)
			 {
				element = (byte[]) path[element_num];

				try
				{
				   element_found = findElement(feStartPage, element, 0);
				}
				catch (OneWireException e)
				{
				   throw new OWFileNotFoundException(e.ToString());
				}

				if (!element_found)
				{
				   if (isDirectory)
				   {

					  // convert unknown entry to directory
					  if (element [4] == EXT_UNKNOWN)
					  {
						 element [4] = EXT_DIRECTORY;
					  }

					  if ((element_num != (path.Count - 1)) && !makeParents)
					  {
						 throw new OWFileNotFoundException("Invalid path, parent not found");
					  }

					  createEntry(element, startPage, numberPages, prev_element, prev_element_start);
				   }
				   else
				   {

					  // convert unknown entry to file with 0 extension
					  if (element [4] == EXT_UNKNOWN)
					  {
						 element [4] = 0;
					  }

					  if (element_num == (path.Count - 1))
					  {

						 // this is the file (end of path)
						 createEntry(element, startPage, numberPages, prev_element, prev_element_start);
					  }
					  else
					  {
						 // remove the entry in the cache before throwing exception
						 cache.removeWriteOpen(owd[0].AddressAsString + Path);
						 throw new OWFileNotFoundException("Path not found");
					  }
				   }
				}
				else if (element_num == (path.Count - 1))
				{
				   // last element
				   if (isDirectory)
				   {
					  if (startPage != -1)
					  {
						 throw new OWFileNotFoundException("Destination File exists");
					  }
				   }
				   else
				   {
					  // check if last element is a directory and should be a file
					  if ((element[4] == EXT_DIRECTORY) && (!isDirectory))
					  {
						 // remove the entry in the cache before throwing exception
						 cache.removeWriteOpen(owd[0].AddressAsString + Path);
						 throw new OWFileNotFoundException("Filename provided is a directory!");
					  }
					  file_exists = true;
				   }
				}

				// get pointers to the next element
				feStartPage = com.dalsemi.onewire.utils.Convert.toInt(feData, feOffset + LEN_FILENAME, LEN_PAGE_PTR);
				feNumPages = com.dalsemi.onewire.utils.Convert.toInt(feData, feOffset + LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);
				prev_element = element;
				prev_element_start = feStartPage;

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=feStartPage " + feStartPage + " feNumPages " + feNumPages);
				}
			 }

			 // if is a file and it already exists, free all but the first data page
			 if (file_exists)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=file exists");
				}

				// check for readonly
				if (!canWrite())
				{
				   throw new OWFileNotFoundException("File is read only (access is denied)");
				}

				try
				{

				   // read the first file page
				   lastLen = cache.readPagePacket(feStartPage, lastPageData, 0);

				   // write over this with an 'empty' page
				   com.dalsemi.onewire.utils.Convert.toByteArray(0, smallBuf, 0, LEN_PAGE_PTR);
				   cache.writePagePacket(feStartPage, smallBuf, 0, LEN_PAGE_PTR);

				   // loop to read the rest of the pages and 'free' them
				   int next_page = com.dalsemi.onewire.utils.Convert.toInt(lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				   while (next_page != 0)
				   {

					  // free the page
					  readBitMap();
					  freePage(next_page);
					  writeBitMap();

					  // read the file page
					  lastLen = cache.readPagePacket(next_page, lastPageData, 0);

					  // get the next page pointer
					  next_page = com.dalsemi.onewire.utils.Convert.toInt(lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);
				   }

				   // update the directory entry to free the pages
				   feNumPages = 1;
				   lastLen = cache.readPagePacket(fePage, lastPageData, 0);

				   com.dalsemi.onewire.utils.Convert.toByteArray(feNumPages, lastPageData, feOffset + LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);
				   cache.writePagePacket(fePage, lastPageData, 0, lastLen);

				   // set the lastPage pointer to the current page
				   lastPage = feStartPage;
				}
				catch (OneWireException e)
				{
				   throw new OWFileNotFoundException(e.ToString());
				}
			 }
		  }
	   }

	   /// <summary>
	   /// Format the Filesystem on the 1-Wire device.
	   /// <para>WARNING: all files/directories will be deleted in the process.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <exception cref="OneWireException"> when adapter is not setup properly </exception>
	   /// <exception cref="OneWireIOException"> when an IO error occured reading
	   ///         the 1-Wire device </exception>
	   protected internal virtual void format()
	   {
		  int i , j , len , next_page , cnt , cdcnt = 0, device_map_pages , dm_bytes = 0;

		  lock (cache)
		  {
			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // check for device with no memory
			 if (totalPages == 0)
			 {
				throw new OneWireException("1-Wire Filesystem does not have memory");
			 }

			 for (i = 0; i < feData.Length; i++)
			 {
				feData [i] = 0;
			 }

			 // create the directory page
			 // Directory Marker 'DM'
			 feData[cdcnt] = (LEN_PAGE_PTR == 1) ? (byte) 0x0A : (byte) 0x0B;
			 feData[cdcnt++] |= (owd.Length == 1) ? (byte) 0xA0 : (byte) 0xB0;

			 // Map Address 'MA', skip for now
			 cdcnt += LEN_PAGE_PTR;

			 // decide what type of bitmap we will have
			 if (cache.handlePageBitmap())
			 {
				bitmapType = BM_CACHE;
				feData [cdcnt++] = 0;

				com.dalsemi.onewire.utils.Convert.toByteArray(cache.BitMapPageNumber, feData, cdcnt, LEN_PAGE_PTR);
				cdcnt += LEN_PAGE_PTR;
				com.dalsemi.onewire.utils.Convert.toByteArray(cache.BitMapNumberOfPages, feData, cdcnt, LEN_PAGE_PTR);
			 }
			 // regular bitmap
			 else
			 {
				// check for Device Map file
				if (owd.Length > 1)
				{
				   // calculate the number of pages need so leave space
				   dm_bytes = (owd.Length - 1) * 8;
				   device_map_pages = dm_bytes / (maxDataLen - LEN_PAGE_PTR);
				   if ((dm_bytes % (maxDataLen - LEN_PAGE_PTR)) > 0)
				   {
					  device_map_pages++;
				   }
				}
				else
				{
				   device_map_pages = 0;
				}

				// local
				if (totalPages <= 32)
				{
				   bitmapType = BM_LOCAL;

				   // make PageBitMap max size of first page of directory
				   pbm = new byte [maxDataLen];
				   pbmByteOffset = 3;
				   pbmBitOffset = 0;
				   // 'BC'
				   feData [cdcnt++] = (owd.Length > 1) ? (byte) 0x82 : (byte) 0x80;

				   // check if this will fit on the ROOT device
				   if (device_map_pages >= rootTotalPages)
				   {
					  throw new OneWireException("ROOT 1-Wire device does not have memory to support this many SATELLITE devices");
				   }

				   // set local page bitmap
				   for (i = 0; i <= device_map_pages; i++)
				   {
					  Bit.arrayWriteBit(PAGE_USED, i, cdcnt, feData);
				   }

				   // put dummy directory on each SATELLITE device
				   if (owd.Length > 1)
				   {
					  // create the dummy directory
					  tempPage[0] = feData[0];
					  tempPage[LEN_PAGE_PTR] = 0;
					  tempPage[1] = 0x01;
					  tempPage[LEN_PAGE_PTR + 1] = 0x80;
					  for (j = 2; j <= 5; j++)
					  {
						 tempPage[LEN_PAGE_PTR + j] = 0xFF;
					  }
					  for (j = 6; j <= 7; j++)
					  {
						 tempPage[LEN_PAGE_PTR + j] = 0x00;
					  }

					  // create link back to the MASTER
					  Array.Copy(owd[0].Address, 0, smallBuf, 0, 8);
					  smallBuf[8] = 0;
					  smallBuf[9] = 0;

					  // write dummy directory on each SATELLITE device and mark in bitmap
					  for (i = 1; i < owd.Length; i++)
					  {
						 // dummy directory
						 cache.writePagePacket(cache.getPageOffsetForDevice(i), tempPage, 0, LEN_PAGE_PTR * 2 + 6);
						 Bit.arrayWriteBit(PAGE_USED, cache.getPageOffsetForDevice(i), cdcnt, feData);

						 // MASTER device map link
						 cache.writePagePacket(cache.getPageOffsetForDevice(i) + 1, smallBuf, 0, LEN_PAGE_PTR + 8);
						 Bit.arrayWriteBit(PAGE_USED, cache.getPageOffsetForDevice(i) + 1, cdcnt, feData);
					  }
				   }
				}
				// file
				else
				{
				   bitmapType = BM_FILE;
				   pbmByteOffset = 0;
				   pbmBitOffset = 0;

				   // calculate the number of bitmap pages needed
				   int pbm_bytes = (totalPages / 8);
				   int pgs = pbm_bytes / (maxDataLen - LEN_PAGE_PTR);

				   if ((pbm_bytes % (maxDataLen - LEN_PAGE_PTR)) > 0)
				   {
					  pgs++;
				   }

				   // check if this will fit on the ROOT device
				   if ((device_map_pages + pgs) >= rootTotalPages)
				   {
					  throw new OneWireException("ROOT 1-Wire device does not have memory to support this many SATELLITE devices");
				   }

				   // 'BC' set the page number of the bitmap file
				   feData [cdcnt++] = (owd.Length > 1) ? (byte) 0x02 : (byte) 0x00;

				   // page address and number of pages for bitmap file
				   if (LEN_PAGE_PTR == 1)
				   {
					  feData[cdcnt++] = 0;
					  feData[cdcnt++] = 0;
				   }
				   com.dalsemi.onewire.utils.Convert.toByteArray(device_map_pages + 1, feData, cdcnt, LEN_PAGE_PTR);
				   cdcnt += LEN_PAGE_PTR;
				   com.dalsemi.onewire.utils.Convert.toByteArray(pgs, feData, cdcnt, LEN_PAGE_PTR);

				   // clear the bitmap
				   for (i = 0; i < pbm.Length; i++)
				   {
					  pbm [i] = 0;
				   }

				   // set the pages used by the directory and bitmap file and device map
				   for (i = 0; i <= (pgs + device_map_pages); i++)
				   {
					  Bit.arrayWriteBit(PAGE_USED, pbmBitOffset + i, pbmByteOffset, pbm);
				   }

				   // put dummy directory on each SATELLITE device
				   if (owd.Length > 1)
				   {
					  // create the dummy directory
					  tempPage[0] = feData[0];
					  tempPage[LEN_PAGE_PTR] = 0;
					  tempPage[1] = 0x01;
					  tempPage[LEN_PAGE_PTR + 1] = 0x80;
					  for (j = 2; j <= 5; j++)
					  {
						 tempPage[LEN_PAGE_PTR + j] = 0xFF;
					  }
					  for (j = 6; j <= 7; j++)
					  {
						 tempPage[LEN_PAGE_PTR + j] = 0x00;
					  }

					  // create link back to the MASTER
					  Array.Copy(owd[0].Address, 0, smallBuf, 0, 8);
					  smallBuf[8] = 0;
					  smallBuf[9] = 0;

					  // write dummy directory on each SATELLITE device and mark in bitmap
					  for (i = 1; i < owd.Length; i++)
					  {
						 // dummy directory
						 cache.writePagePacket(cache.getPageOffsetForDevice(i), tempPage, 0, LEN_PAGE_PTR * 2 + 6);
						 Bit.arrayWriteBit(PAGE_USED, pbmBitOffset + cache.getPageOffsetForDevice(i), pbmByteOffset, pbm);

						 // MASTER device map link
						 cache.writePagePacket(cache.getPageOffsetForDevice(i) + 1, smallBuf, 0, LEN_PAGE_PTR + 8);
						 Bit.arrayWriteBit(PAGE_USED, pbmBitOffset + cache.getPageOffsetForDevice(i) + 1, pbmByteOffset, pbm);
					  }
				   }

				   // write the bitmap file
				   cnt = 0;
				   for (i = device_map_pages + 1; i <= (pgs + device_map_pages); i++)
				   {
					  // calculate length to write for this page
					  if ((pbm_bytes - cnt) > (maxDataLen - LEN_PAGE_PTR))
					  {
						 len = maxDataLen - LEN_PAGE_PTR;
					  }
					  else
					  {
						 len = pbm_bytes - cnt;
					  }

					  // copy bitmap data to temp
					  Array.Copy(pbm, pbmByteOffset + cnt, tempPage, 0, len);

					  // set the next page marker
					  next_page = (i == (pgs + device_map_pages)) ? 0 : (i + 1);

					  com.dalsemi.onewire.utils.Convert.toByteArray(next_page, tempPage, len, LEN_PAGE_PTR);

					  // write the page
					  cache.writePagePacket(i, tempPage, 0, len + LEN_PAGE_PTR);

					  cnt += len;
				   }
				}

				// write Device Map file
				if (owd.Length > 1)
				{
				   // set the start page 'MA'
				   com.dalsemi.onewire.utils.Convert.toByteArray(1, feData, 1, LEN_PAGE_PTR);

				   // bitmap already taken care of, just put right after directory
				   // create the device map data to write
				   byte[] dmf = new byte[dm_bytes];
				   for (i = 1; i < owd.Length; i++)
				   {
					  Array.Copy(owd[i].Address, 0, dmf, (i - 1) * 8, 8);
				   }

				   // write the pages
				   cnt = 0;
				   for (i = 1; i <= device_map_pages; i++)
				   {
					  // calculate length to write for this page
					  if ((dm_bytes - cnt) > (maxDataLen - LEN_PAGE_PTR))
					  {
						 len = maxDataLen - LEN_PAGE_PTR;
					  }
					  else
					  {
						 len = dm_bytes - cnt;
					  }

					  // copy bitmap data to temp
					  Array.Copy(dmf, cnt, tempPage, 0, len);

					  // set the next page marker
					  next_page = (i == device_map_pages) ? 0 : (i + 1);

					  com.dalsemi.onewire.utils.Convert.toByteArray(next_page, tempPage, len, LEN_PAGE_PTR);

					  // write the page
					  cache.writePagePacket(i, tempPage, 0, len + LEN_PAGE_PTR);

					  cnt += len;
				   }
				}
			 }

			 // write the directory page
			 cache.writePagePacket(0, feData, 0, LEN_CONTROL_DATA + LEN_PAGE_PTR);

			 // update bitmap if implemented in cache
			 if (cache.handlePageBitmap())
			 {
				markPageUsed(0);
			 }

			 fePage = 0;
			 feLen = LEN_CONTROL_DATA + LEN_PAGE_PTR;
		  }
	   }

	   //--------
	   //-------- Read methods
	   //--------

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
	   protected internal virtual int read(byte[] b, int off, int len)
	   {
		  int read_count = 0, page_data_read , next_page = 0;

		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===read(byte[],int,int)  with len " + len);
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // check for no pages read
			 if (lastPage == -1)
			 {
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=first read");
				}

				lastPage = feStartPage;
				lastOffset = 0;
				filePosition = 0;

				// read the lastPage into the lastPageData buffer
				fetchPage();
			 }

			 // loop to read pages needed or end of file found
			 do
			 {

				// check if need to fetch another page
				if ((lastOffset + LEN_PAGE_PTR) >= lastLen)
				{

				   // any more pages?
				   next_page = com.dalsemi.onewire.utils.Convert.toInt(lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				   if (next_page == 0)
				   {
					  break;
				   }

				   // get the next page
				   lastPage = next_page;

				   fetchPage();
				}


				// calculate the data available/needed to read from this page
				if (len >= (lastLen - lastOffset - LEN_PAGE_PTR))
				{
				   page_data_read = (lastLen - lastOffset - LEN_PAGE_PTR);
				}
				else
				{
				   page_data_read = len;
				}

				// get the data from the page (if buffer not null)
				if (b != null)
				{
				   Array.Copy(lastPageData, lastOffset, b, off, page_data_read);
				}

				// adjust counters
				read_count += page_data_read;
				off += page_data_read;
				len -= page_data_read;
				lastOffset += page_data_read;
				filePosition += page_data_read;
				next_page = com.dalsemi.onewire.utils.Convert.toInt(lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);
			 } while ((len != 0) && (next_page != 0));

			 // check for end of file
			 if ((read_count == 0) && (len != 0))
			 {
				return -1;
			 }

			 // return number of bytes read
			 return read_count;
		  }
	   }

	   /// <summary>
	   /// Reads a byte of data from this input stream. This method blocks
	   /// if no input is yet available.
	   /// </summary>
	   /// <returns>     the next byte of data, or <code>-1</code> if the end of the
	   ///             file is reached. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   protected internal virtual int read()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===read()");
			 }

			 int len = read(smallBuf, 0, 1);

			 if (len == 1)
			 {
				return (int)(smallBuf [0] & 0x00FF);
			 }
			 else
			 {
				return -1;
			 }
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
	   protected internal virtual long skip(long n)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===skip " + n);
			 }

			 return read(null, 0, (int) n);
		  }
	   }

	   /// <summary>
	   /// Returns the number of bytes that can be read from this file input
	   /// stream without blocking.
	   /// </summary>
	   /// <returns>     the number of bytes that can be read from this file input
	   ///             stream without blocking. </returns>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   protected internal virtual int available()
	   {
		  lock (cache)
		  {
			 // check for no pages read
			 if (lastPage == -1)
			 {
				return 0;
			 }
			 else
			 {
				return (lastLen - lastOffset - 1);
			 }
		  }
	   }

	   //--------
	   //-------- Write methods
	   //--------

	   /// <summary>
	   /// Writes the specified byte to this file output stream. Implements
	   /// the <code>write</code> method of <code>OutputStream</code>.
	   /// </summary>
	   /// <param name="b">   the byte to be written. </param>
	   /// <exception cref="IOException">  if an I/O error occurs. </exception>
	   protected internal virtual void write(int b)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===write(int) " + b);
			 }

			 smallBuf [0] = (byte) b;

			 write(smallBuf, 0, 1);
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
	   protected internal virtual void write(byte[] b, int off, int len)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("===write(byte[],int,int) with data (" + len + ") :");
				debugDump(b, off, len);
			 }

			 // check for something to do
			 if (len == 0)
			 {
				return;
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // check for no pages read
			 if (lastPage == -1)
			 {
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=first write");
				}

				lastPage = feStartPage;
				lastOffset = 0;
				filePosition = 0;
			 }

			 try
			 {
				// read the last page
				lastLen = cache.readPagePacket(lastPage, lastPageData, 0);

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("===write, readpagePacket " + lastPage + " got len " + lastLen);
				}

				int write_len;

				do
				{

				   // check if room to write
				   if (lastLen >= maxDataLen)
				   {

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.Write("=Need new page");
					  }

					  // get another page to write
					  // get the pagebitmap
					  readBitMap();

					  // get the next available page
					  int new_page = getFirstFreePage(false);

					  // verify got a free page
					  if (new_page < 0)
					  {
						 try
						 {
							sync();
						 }
						 catch (OWSyncFailedException)
						 {
							// DRAIN
						 }

						 throw new System.IO.IOException("Out of space on 1-Wire device");
					  }

					  // mark page used
					  markPageUsed(new_page);

					  // put blank data page in new page
					  com.dalsemi.onewire.utils.Convert.toByteArray(0, tempPage, 0, LEN_PAGE_PTR);
					  cache.writePagePacket(new_page, tempPage, 0, LEN_PAGE_PTR);

					  // change next page pointer in last page
					  com.dalsemi.onewire.utils.Convert.toByteArray(new_page, lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

					  // put data page back in place with new next page pointer
					  cache.writePagePacket(lastPage, lastPageData, 0, lastLen);

					  // write the page bitmap
					  writeBitMap();

					  // update the directory entry to include this new page
					  lastLen = cache.readPagePacket(fePage, lastPageData, 0);

					  com.dalsemi.onewire.utils.Convert.toByteArray(++feNumPages, lastPageData, feOffset + LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);
					  cache.writePagePacket(fePage, lastPageData, 0, lastLen);

					  // make 'lastPage' the new empty page
					  lastPageData [0] = 0;
					  lastPage = new_page;
					  lastLen = LEN_PAGE_PTR;
				   }

				   // calculate how much of the data can write to lastPage
				   if (len > (maxDataLen - lastLen))
				   {
					  write_len = maxDataLen - lastLen;
				   }
				   else
				   {
					  write_len = len;
				   }

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("===write, len " + len + " maxDataLen " + maxDataLen + " lastLen " + lastLen + " write_len " + write_len + " off " + off);
				   }

				   // copy the data
				   Array.Copy(b, off, lastPageData, lastLen - LEN_PAGE_PTR, write_len);

				   // update the counters
				   len -= write_len;
				   off += write_len;
				   lastLen += write_len;

				   // set the next page pointer to end of file marker '0'
				   com.dalsemi.onewire.utils.Convert.toByteArray(0, lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				   // write the data
				   cache.writePagePacket(lastPage, lastPageData, 0, lastLen);
				} while (len > 0);
			 }
			 catch (OneWireException e)
			 {
				throw new System.IO.IOException(e.ToString());
			 }
		  }
	   }

	   //--------
	   //-------- Info methods
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
	   protected internal virtual string Name
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===getName()");
				 }
    
				 return pathToString(path, path.Count - 1, path.Count, true);
			  }
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
	   protected internal virtual string Parent
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===getParent(), path is null=" + (path == null));
				 }
    
				 if (path == null)
				 {
					throw new System.NullReferenceException("path is not valid");
				 }
    
				 if (path.Count >= 1)
				 {
					return pathToString(path, 0, path.Count - 1, false);
				 }
				 else
				 {
					return null;
				 }
			  }
		   }
	   }

	   /// <summary>
	   /// Converts this abstract pathname into a pathname string.  The resulting
	   /// string uses the <seealso cref="OWFile#separator default name-separator character"/> to
	   /// separate the names in the name sequence.
	   /// </summary>
	   /// <returns>  The string form of this abstract pathname </returns>
	   protected internal virtual string Path
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===getPath(), path is null=" + (path == null));
				 }
    
				 if (path == null)
				 {
					throw new System.NullReferenceException("path is not valid");
				 }
    
				 return pathToString(verbosePath, 0, verbosePath.Count, false);
			  }
		   }
	   }

	   /// <summary>
	   /// Checks to see if the file exists
	   /// </summary>
	   /// <returns> true if the file exists and false otherwise </returns>
	   protected internal virtual bool exists()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===exists()");
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // check if this is the root
			 if (path != null && path.Count == 0)
			 {
				// force a check of the Filesystem if never been read
				if (pbmStartPage == -1)
				{
				   try
				   {
					  readBitMap();
				   }
				   catch (OneWireException)
				   {
					  return false;
				   }
				}

				return true;
			 }

			 // attempt to open the file/directory
			 try
			 {
				open();

				return true;
			 }
			 catch (OWFileNotFoundException)
			 {
				return false;
			 }
		  }
	   }

	   /// <summary>
	   /// Checks to see if can read the file associated with
	   /// this descriptor.
	   /// </summary>
	   /// <returns> true if this file exists, false otherwise </returns>
	   protected internal virtual bool canRead()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===canRead()");
			 }

			 return exists();
		  }
	   }

	   /// <summary>
	   /// Checks to see if the file represented by this descriptor
	   /// is writable.
	   /// </summary>
	   /// <returns> true if this file exists and is not read only, false
	   ///          otherwise </returns>
	   protected internal virtual bool canWrite()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===canWrite()");
			 }

			 if (exists())
			 {
				if (File)
				{
				   return ((feData [feOffset + LEN_FILENAME - 1] & 0x80) == 0);
				}
				else
				{
				   return true;
				}
			 }
			 else
			 {
				return false;
			 }
		  }
	   }

	   /// <summary>
	   /// Checks to see if this is a directory.
	   /// </summary>
	   /// <returns> true if this file exists and it is a directory, false
	   ///         otherwise </returns>
	   protected internal virtual bool Directory
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===isDirectory()");
				 }
    
				 if (exists())
				 {
					return !File;
				 }
				 else
				 {
					return false;
				 }
			  }
		   }
	   }

	   /// <summary>
	   /// Checks to see if this is a file
	   /// </summary>
	   /// <returns> true if this file exists and is a file, false
	   ///         otherwise </returns>
	   protected internal virtual bool File
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===isFile()");
				 }
    
				 // check if this is the root
				 if (path.Count == 0)
				 {
					return false;
				 }
    
				 if (exists())
				 {
					return ((feData [feOffset + LEN_FILENAME - 1] & 0x7F) != 0x7F);
				 }
				 else
				 {
					return false;
				 }
			  }
		   }
	   }

	   /// <summary>
	   /// Checks to see if this directory is hidden.
	   /// </summary>
	   /// <returns> true if this is a directory and is marked as hidden, false
	   ///         otherwise </returns>
	   protected internal virtual bool Hidden
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===isHidden()");
				 }
    
				 if (exists())
				 {
    
					// look at hidden flag in parent (if it has one)
					if (path.Count > 0)
					{
					   if (Directory)
					   {
						  byte[] fl = (byte[]) path[path.Count - 1];
    
						  return ((fl [LEN_FILENAME - 1] & 0x80) != 0);
					   }
					}
				 }
    
				 return false;
			  }
		   }
	   }

	   /// <summary>
	   /// Get the estimated length of the file represented by this
	   /// descriptor.  This is calculated by looking at how may pages
	   /// the file is using so is not a very accurate measure.
	   /// </summary>
	   /// <returns> estimated length of file in bytes </returns>
	   protected internal virtual long length()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===length()");
			 }

			 if (exists())
			 {
				return (feNumPages * (maxDataLen - LEN_PAGE_PTR));
			 }
			 else
			 {
				return 0;
			 }
		  }
	   }

	   /// <summary>
	   /// Delete this file or directory represented by this descriptor.
	   /// Will fail if it is a read-only file or a non-empty directory.
	   /// </summary>
	   /// <returns> true if the file/directory was successfully deleted or
	   ///         false if not </returns>
	   protected internal virtual bool delete()
	   {
		  lock (cache)
		  {
			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 if (File)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("===delete() is a file");
				}

				try
				{

				   // remove the directory entry
				   Array.Copy(feData, feOffset + LEN_FILE_ENTRY, feData, feOffset, feLen - feOffset - LEN_FILE_ENTRY);

				   feLen -= LEN_FILE_ENTRY;

				   cache.writePagePacket(fePage, feData, 0, feLen);

				   // loop to remove all of the file pages 'free' only if not EPROM
				   if (bitmapType != BM_CACHE)
				   {

					  // loop to read the rest of the pages and 'free' them
					  int next_page = feStartPage;

					  while (next_page != 0)
					  {

						 // free the page
						 readBitMap();
						 freePage(next_page);
						 writeBitMap();

						 // read the file page
						 lastLen = cache.readPagePacket(next_page, lastPageData, 0);

						 // get the next page pointer
						 next_page = com.dalsemi.onewire.utils.Convert.toInt(lastPageData, lastLen - LEN_PAGE_PTR, LEN_PAGE_PTR);
					  }

					  // update
					  lastPage = -1;
					  feStartPage = -1;
				   }

				   return true;
				}
				catch (OneWireException)
				{
				   return false;
				}
			 }
			 else if (Directory)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("===delete() is a directory");
				}

				try
				{

				   // read the first page of the directory to see if empty
				   int len = cache.readPagePacket(feStartPage, tempPage, 0);

				   if (len != LEN_CONTROL_DATA + LEN_PAGE_PTR)
				   {
					  return false;
				   }

				   // remove the directory entry
				   Array.Copy(feData, feOffset + LEN_CONTROL_DATA, feData, feOffset, feLen - feOffset - LEN_CONTROL_DATA);

				   feLen -= LEN_CONTROL_DATA;

				   cache.writePagePacket(fePage, feData, 0, feLen);

				   // free the page
				   readBitMap();
				   freePage(feStartPage);
				   writeBitMap();

				   return true;
				}
				catch (OneWireException)
				{
				   return false;
				}
			 }
			 else
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("===delete() is neither file or directory so fail");
				}

				return false;
			 }
		  }
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
	   protected internal virtual string[] list()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===list() string");
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 if (Directory)
			 {
				ArrayList entries = new ArrayList(1);

				try
				{

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("=feStartPage " + feStartPage);
				   }

				   // loop though the entries and collect string reps
				   int next_page = feStartPage;
				   StringBuilder build_buffer = new StringBuilder();
				   int offset = LEN_CONTROL_DATA, len , i , page ;

				   do
				   {
					  page = next_page;

					  // read the page
					  len = cache.readPagePacket(page, tempPage, 0);

					  // loop through the entries
					  for (; offset < (len - LEN_PAGE_PTR); offset += LEN_FILE_ENTRY)
					  {
						 build_buffer.Length = 0;

						 for (i = 0; i < 4; i++)
						 {
							if ((byte) tempPage [offset + i] != (byte) 0x20)
							{
							   build_buffer.Append((char) tempPage [offset + i]);
							}
							else
							{
							   break;
							}
						 }

						 if ((byte)(tempPage [offset + 4] & 0x7F) != (byte) EXT_DIRECTORY)
						 {
							build_buffer.Append("." + System.Convert.ToString((int)(tempPage [offset + 4] & 0x7F)));
						 }

						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.WriteLine("=entry= " + build_buffer.ToString());
						 }

						 // only add if not hidden directory
						 if ((byte) tempPage [offset + 4] != 0xFF)
						 {
							// add to the vector of strings
							entries.Add(build_buffer.ToString());
						 }
					  }

					  // get next page pointer to read
					  next_page = com.dalsemi.onewire.utils.Convert.toInt(tempPage, len - LEN_PAGE_PTR, LEN_PAGE_PTR);
					  offset = 0;

					  // check for looping Filesystem
					  if (entries.Count > totalPages)
					  {
						 return null;
					  }

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("=next page = " + next_page);
					  }
				   } while (next_page != 0);
				}
				catch (OneWireException e)
				{

				   // DRAIN
				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("= " + e);
				   }
				}

				// return the entries as an array of strings
				string[] strs = new string [entries.Count];
				for (int i = 0; i < strs.Length; i++)
				{
				   strs[i] = (string)entries[i];
				}
				return strs;
			 }
			 else
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=not a directory so no list");
				}

				return null;
			 }
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
	   protected internal virtual bool renameTo(OWFile dest)
	   {
		  if (dest == null)
		  {
			 throw new System.NullReferenceException("Desitination file is null");
		  }

		  lock (cache)
		  {
			 // make sure exists (also getting the file entry info)
			 if (!exists())
			 {
				return false;
			 }

			 try
			 {
				// get the file descriptor of the destination
				OWFileDescriptor dest_fd = dest.FD;

				// create the new entry pointing to the old file
				dest_fd.create(false, Directory, false, feStartPage, feNumPages);

				// delete the old entry
				feLen = cache.readPagePacket(fePage, feData, 0);
				Array.Copy(feData, feOffset + LEN_FILE_ENTRY, feData, feOffset, feLen - feOffset - LEN_FILE_ENTRY);
				feLen -= LEN_FILE_ENTRY;
				cache.writePagePacket(fePage, feData, 0, feLen);

				// open this file to make sure all file entry pointers get reset
				feStartPage = -1;
				try
				{
				   open();
				}
				catch (OWFileNotFoundException)
				{
				   // DRAIN
				}

				return true;
			 }
			 catch (IOException)
			 {
				return false;
			 }
			 catch (OneWireException)
			 {
				return false;
			 }
		  }
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
	   protected internal virtual bool setReadOnly()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===setReadOnly()");
			 }

			 if (File)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=is a file");
				}

				// mark the readonly bit in the file entry page
				feData [feOffset + LEN_FILENAME - 1] |= 0x80;

				try
				{
				   // write new setting to cache
				   cache.writePagePacket(fePage, feData, 0, feLen);
				}
				catch (OneWireException)
				{
				   return false;
				}

				return true;
			 }

			 return false;
		  }
	   }

	   /// <summary>
	   /// Mark the current position in the file being read for later
	   /// reference.
	   /// </summary>
	   /// <param name="readlimit"> limit to keep track of the current position </param>
	   protected internal virtual void mark(int readlimit)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===mark() with readlimit=" + readlimit + " current pos=" + filePosition);
			 }

			 markPosition = filePosition;
			 markLimit = readlimit;
		  }
	   }

	   /// <summary>
	   /// Reset the the read of this file back to the marked position.
	   /// </summary>
	   /// <exception cref="IOException"> when a read error occurs </exception>
	   protected internal virtual void reset()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===reset() current pos=" + filePosition);
			 }

			 if ((filePosition - markPosition) > markLimit)
			 {
				throw new System.IO.IOException("File read beyond mark readlimit");
			 }

			 // reset the file
			 lastPage = -1;

			 // skip to the mark position
			 skip(markPosition);
		  }
	   }

	   //--------
	   //-------- Page Bitmap Methods
	   //--------

	   /// <summary>
	   /// Mark the specified page as used in the page bitmap.
	   /// </summary>
	   /// <param name="page"> number to mark as used
	   /// </param>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   protected internal virtual void markPageUsed(int page)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===markPageUsed " + page);
			 }

			 if (bitmapType == BM_CACHE)
			 {
				cache.markPageUsed(page);
			 }
			 else
			 {
				// mark page used in cached bitmap of used pages
				Bit.arrayWriteBit(PAGE_USED, pbmBitOffset + page, pbmByteOffset, pbm);
			 }
		  }
	   }

	   /// <summary>
	   /// free the specified page as being un-used in the page bitmap
	   /// </summary>
	   /// <param name="page"> number to mark as un-used
	   /// </param>
	   /// <returns> true if the page as be been marked as un-used, false
	   ///      if the page is on an OTP device and cannot be freed
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   protected internal virtual bool freePage(int page)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("===freePage " + page);
			 }

			 if (bitmapType == BM_CACHE)
			 {
				return cache.freePage(page);
			 }
			 else
			 {
				// mark page used in cached bitmap of used pages
				Bit.arrayWriteBit(PAGE_NOT_USED, pbmBitOffset + page, pbmByteOffset, pbm);

				return true;
			 }
		  }
	   }

	   /// <summary>
	   /// Get the first free page from the page bitmap.
	   /// </summary>
	   /// <param name="counterPage"> <code> true </code> if page needed is
	   ///        a 'counter' page (used in for monitary files)
	   /// </param>
	   /// <returns> first page number that is free to write
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   protected internal virtual int getFirstFreePage(bool counterPage)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.Write("===getFirstFreePage, counter " + counterPage);
			 }

			 if (bitmapType == BM_CACHE)
			 {
				return cache.FirstFreePage;
			 }
			 else
			 {
				lastFreePage = 0;

				return getNextFreePage(counterPage);
			 }
		  }
	   }

	   /// <summary>
	   /// Get the next free page from the page bitmap.
	   /// </summary>
	   /// <param name="counterPage"> <code> true </code> if page needed is
	   ///        a 'counter' page (used in for monitary files)
	   /// </param>
	   /// <returns> next page number that is free to write
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   protected internal virtual int getNextFreePage(bool counterPage)
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===getNextFreePage ");
			 }

			 if (bitmapType == BM_CACHE)
			 {
				return cache.NextFreePage;
			 }
			 else
			 {
				for (int pg = lastFreePage; pg < totalPages; pg++)
				{
				   if (Bit.arrayReadBit(pbmBitOffset + pg, pbmByteOffset, pbm) == PAGE_NOT_USED)
				   {
					  // check if need a counter page
					  if (counterPage)
					  {
						 // counter page has extra info with COUNTER or MAC
						 PagedMemoryBank pmb = getMemoryBankForPage(pg);
						 if (pmb.hasExtraInfo() && (pg != 8))
						 {
							string ex_info = pmb.ExtraInfoDescription;
							if ((ex_info.IndexOf("counter", StringComparison.Ordinal) > -1) || (ex_info.IndexOf("MAC", StringComparison.Ordinal) > -1))
							{
							   return pg;
							}
						 }
						 continue;
					  }

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.WriteLine("=free page is " + pg);
					  }

					  lastFreePage = pg + 1;

					  return pg;
				   }
				}

				return -1;
			 }
		  }
	   }

	   /// <summary>
	   /// Gets the number of bytes available on this device for
	   /// file and directory information.
	   /// </summary>
	   /// <returns> number of free bytes
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO exception occurs </exception>
	   protected internal virtual int FreeMemory
	   {
		   get
		   {
			  lock (cache)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//
				 if (doDebugMessages)
				 {
					Debug.WriteLine("===getFreeMemory()");
				 }
    
				 // clear last page read flag in the cache
				 cache.clearLastPageRead();
    
				 if (bitmapType == BM_CACHE)
				 {
					return (cache.NumberFreePages * (maxDataLen - LEN_PAGE_PTR));
				 }
				 else
				 {
					// read the bitmap
					readBitMap();
    
					int free_pages = 0;
					for (int pg = 0; pg < totalPages; pg++)
					{
					   if (Bit.arrayReadBit(pbmBitOffset + pg, pbmByteOffset, pbm) == PAGE_NOT_USED)
					   {
						  free_pages++;
					   }
					}
    
					//\\//\\//\\//\\//\\//\\//\\//
					if (doDebugMessages)
					{
					   Debug.WriteLine("=num free pages = " + free_pages);
					}
    
					return (free_pages * (maxDataLen - LEN_PAGE_PTR));
				 }
			  }
		   }
	   }

	   /// <summary>
	   /// Write the page bitmap back to the device.
	   /// </summary>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   protected internal virtual void writeBitMap()
	   {
		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===writeBitMap() ");
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 if (bitmapType == BM_LOCAL)
			 {
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=local ");
				}

				int pg_len = cache.readPagePacket(0, tempPage, 0);

				// check to see if the bitmap has changed
				for (int i = 3; i < 7; i++)
				{
				   if ((tempPage[i] & 0x00FF) != (pbm[i] & 0x00FF))
				   {
					  Array.Copy(pbm, 3, tempPage, 3, 4);
					  cache.writePagePacket(0, tempPage, 0, pg_len);
					  break;
				   }
				}
			 }
			 else if (bitmapType == BM_FILE)
			 {
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=file ");
				}

				// FILE type page bitmap (got start page from validateFileSystem)
				int offset = 0, pg = pbmStartPage, len ;

				// loop through all of the pages in the bitmap
				for (int pg_cnt = 0; pg_cnt < pbmNumPages; pg_cnt++)
				{
				   // read the current bitmap just to get the next page pointer and length
				   len = cache.readPagePacket(pg, tempPage, 0);

				   // check to see if this bitmap segment has changed
				   for (int i = 0; i < (len - LEN_PAGE_PTR); i++)
				   {
					  if ((tempPage[i] & 0x00FF) != (pbm[i + offset] & 0x00FF))
					  {
						 // copy new bitmap value to it
						 Array.Copy(pbm, offset, tempPage, 0, len - LEN_PAGE_PTR);

						 // write back to device
						 cache.writePagePacket(pg, tempPage, 0, len);

						 break;
					  }
				   }

				   // get next page number to read
				   pg = com.dalsemi.onewire.utils.Convert.toInt(tempPage, len - LEN_PAGE_PTR, LEN_PAGE_PTR);
				   offset += (len - LEN_PAGE_PTR);
				}
			 }
		  }
	   }

	   /// <summary>
	   /// Read the page bitmap.
	   /// </summary>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   protected internal virtual void readBitMap()
	   {
		  int len;

		  lock (cache)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===readBitMap() ");
			 }

			 // clear last page read flag in the cache
			 cache.clearLastPageRead();

			 // check to see if the directory has been read to know where the page bitmap is
			 if (pbmStartPage == -1)
			 {
				fePage = 0;
				feLen = cache.readPagePacket(fePage, feData, 0);
				validateFileSystem();
			 }

			 // depending on the type of the page bitmap, read it
			 if (bitmapType == BM_LOCAL)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=local ");
				}

				cache.readPagePacket(0, pbm, 0);
			 }
			 else if (bitmapType == BM_FILE)
			 {

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=file ");
				}

				// FILE type page bitmap (got start page from validateFileSystem)
				int offset = 0, pg = pbmStartPage;

				for (int pg_cnt = 0; pg_cnt < pbmNumPages; pg_cnt++)
				{
				   len = cache.readPagePacket(pg, pbm, offset);
				   pg = com.dalsemi.onewire.utils.Convert.toInt(pbm, offset + len - LEN_PAGE_PTR, LEN_PAGE_PTR);
				   offset += (len - LEN_PAGE_PTR);

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("=pg " + pg + " len " + len + " offset " + offset);
					  //debugDump(pbm, offset + len - LEN_PAGE_PTR, LEN_PAGE_PTR);
				   }
				}

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=pbm is ");
				   debugDump(pbm, 0, pbm.Length);
				}
			 }
		  }
	   }

	   /// <summary>
	   /// Get's an array of integers that represents the page
	   /// list of the file or directory represented by this
	   /// OWFile.
	   /// </summary>
	   /// <exception cref="OneWireException">  if an I/O error occurs. </exception>
	   protected internal virtual int[] PageList
	   {
		   get
		   {
			  int[] page_list = new int[feNumPages + 10];
			  int cnt = 0, len ;
			  int next_page = feStartPage;
    
			  // clear last page read flag in the cache
			  cache.clearLastPageRead();
    
			  // loop to read all of the pages
			  do
			  {
				 // check list for size limit
				 if (cnt >= page_list.Length)
				 {
					// grow this list by 10
					int[] temp = new int[page_list.Length + 10];
					Array.Copy(page_list, 0, temp, 0, page_list.Length);
					page_list = temp;
				 }
    
				 // add to the list
				 page_list[cnt++] = next_page;
    
				 // read the file page
				 len = cache.readPagePacket(next_page, tempPage, 0);
    
				 // get the next page pointer
				 next_page = com.dalsemi.onewire.utils.Convert.toInt(tempPage, len - LEN_PAGE_PTR, LEN_PAGE_PTR);
    
				 // check for looping pages
				 if (cnt > totalPages)
				 {
					throw new OneWireException("Error in Filesystem, looping pointers");
				 }
			  } while (next_page != 0);
    
			  // create the return array
			  int[] rt_array = new int[cnt];
			  Array.Copy(page_list, 0, rt_array, 0, cnt);
    
			  return rt_array;
		   }
	   }

	   /// <summary>
	   /// Returns an integer which represents the starting memory page
	   /// of the file or directory represented by this OWFile.
	   /// </summary>
	   /// <returns> The starting page of the file or directory.
	   /// </returns>
	   /// <exception cref="IOException"> if the file doesn't exist </exception>
	   protected internal virtual int StartPage
	   {
		   get
		   {
			  return feStartPage;
		   }
	   }

	   /// <summary>
	   /// Get's the memory bank object for the specified page.
	   /// This is significant if the Filesystem spans memory banks
	   /// on the same or different devices.
	   /// </summary>
	   protected internal virtual PagedMemoryBank getMemoryBankForPage(int page)
	   {
		  return cache.getMemoryBankForPage(page);
	   }

	   /// <summary>
	   /// Get's the local page number on the memory bank object for
	   /// the specified page.
	   /// This is significant if the Filesystem spans memory banks
	   /// on the same or different devices.
	   /// </summary>
	   protected internal virtual int getLocalPage(int page)
	   {
		  return cache.getLocalPage(page);
	   }

	   //--------
	   //-------- Private methods
	   //--------

	   /// <summary>
	   /// Convert the specified vector path into a string.
	   /// </summary>
	   /// <param name="tempPath">  vector of byte arrays that represents the path </param>
	   /// <param name="beginIndex"> start index to convert </param>
	   /// <param name="endIndex"> end iindex to convert </param>
	   /// <param name="single"> true if only need a single field not a path
	   /// </param>
	   /// <returns> string representation of the specified path </returns>
	   private string pathToString(ArrayList tempPath, int beginIndex, int endIndex, bool single)
	   {
		  if (beginIndex < 0)
		  {
			 return null;
		  }

		  byte[] name;
		  StringBuilder build_buffer = new StringBuilder((single ? "" : OWFile.separator));

		  for (int element = beginIndex; element < endIndex; element++)
		  {
			 name = (byte[]) tempPath[element];

			 if (!single && (element != beginIndex))
			 {
				build_buffer.Append(OWFile.separatorChar);
			 }

			 for (int i = 0; i < 4; i++)
			 {
				if ((byte) name [i] != (byte) 0x20)
				{
				   build_buffer.Append((char) name [i]);
				}
				else
				{
				   break;
				}
			 }

			 if (((byte)(name[4] & 0x7F) != (byte) EXT_DIRECTORY) && ((byte) name [4] != (byte) EXT_UNKNOWN))
			 {
				build_buffer.Append("." + Convert.ToString((int) name [4] & 0x7F));
			 }
		  }

		  if (build_buffer.Length == 0)
		  {
			 return null;
		  }
		  else
		  {
			 return build_buffer.ToString();
		  }
	   }

	   /// <summary>
	   /// Verifies the path up to the specified depth.  Sets feStartPage
	   /// and feNumPages.
	   /// </summary>
	   /// <param name="depth"> of path to verify
	   /// </param>
	   /// <returns> true if the path is valid, false if elements not found
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   private bool verifyPath(int depth)
	   {
		  byte[] element;

		  feStartPage = 0;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===verifyPath() depth=" + depth);
		  }

		  for (int element_num = 0; element_num < depth; element_num++)
		  {
			 element = (byte[]) path[element_num];

			 // remember where the parent entry is
			 feParentPage = feStartPage;
			 feParentOffset = feOffset;

			 // attempt to find the element starting at the last entry
			 if (!findElement(feStartPage, element, 0))
			 {
				return false;
			 }

			 // get the next entry start
			 feStartPage = com.dalsemi.onewire.utils.Convert.toInt(feData, feOffset + LEN_FILENAME, LEN_PAGE_PTR);
			 feNumPages = com.dalsemi.onewire.utils.Convert.toInt(feData, feOffset + LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);
		  }

		  return true;
	   }

	   /// <summary>
	   /// Search for the specified element staring at the current file entry
	   /// page startPage.  Set variables fePage,feOffset, and feData.
	   /// </summary>
	   /// <param name="startPage"> directory page to start looking for the element on </param>
	   /// <param name="element"> element to search for </param>
	   /// <param name="offset"> offset into element byte array where element is
	   /// </param>
	   /// <returns> true if the element was found and the instance variables
	   ///         fePage,feOffset, and feData have been set
	   /// </returns>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   private bool findElement(int startPage, byte[] element, int offset)
	   {
		  int next_page = startPage;

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("===findElement() start page=" + startPage + " element:");
			 debugDump(element, offset, 5);
		  }

		  // clear last page read flag in the cache
		  cache.clearLastPageRead();

		  // read the 1-Wire device to find a file/directory reference
		  feOffset = LEN_CONTROL_DATA;

		  do
		  {
			 fePage = next_page;

			 // read the page
			 feLen = cache.readPagePacket(fePage, feData, 0);

			 // if just read root directory, check filesystem
			 if (fePage == 0)
			 {
				readBitMap();
			 }

			 // loop through the entries
			 for (; feOffset < (feLen - LEN_PAGE_PTR); feOffset += LEN_FILE_ENTRY)
			 {
				// compare with current element
				if (elementEquals(element, offset, feData, feOffset))
				{

				   // copy over any read-only or hidden flag in element name
				   if ((feData [feOffset + LEN_FILENAME - 1] & 0x80) != 0)
				   {
					  element [offset + LEN_FILENAME - 1] |= 0x80;
				   }

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.WriteLine("=found on page " + fePage + " at offset " + feOffset);
				   }

				   return true;
				}
			 }

			 // get next page pointer to read
			 next_page = com.dalsemi.onewire.utils.Convert.toInt(feData, feLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

			 // reset loop start
			 if (next_page != 0)
			 {
				feOffset = 0;
			 }
		  } while (next_page != 0);

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("=NOT found");
		  }

		  // end of directory and no entry found
		  return false;
	   }

	   /// <summary>
	   /// Compare if two path elements are equal.
	   /// </summary>
	   /// <param name="file1"> first file </param>
	   /// <param name="offset1"> first file offset </param>
	   /// <param name="file2"> second file </param>
	   /// <param name="offset2"> second file offset
	   /// 
	   /// @return </param>
	   private bool elementEquals(byte[] file1, int offset1, byte[] file2, int offset2)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("===elementEquals()  ");
			 debugDump(file1, offset1, 5);
			 Debug.Write("=to                 ");
			 debugDump(file2, offset2, 5);
		  }

		  for (int i = 0; i < 4; i++)
		  {
			 if ((byte) file1 [offset1 + i] != (byte) file2 [offset2 + i])
			 {
				return false;
			 }
		  }

		  // check if type is inknown (either a file with 000 extension or directory)
		  if (file1 [offset1 + 4] == EXT_UNKNOWN)
		  {
			 if ((file2 [offset2 + 4] & 0x7F) == 0)
			 {
				file1 [offset1 + 4] = 0;
			 }
			 else if ((file2 [offset2 + 4] & 0x7F) == EXT_DIRECTORY)
			 {
				file1 [offset1 + 4] = EXT_DIRECTORY;
			 }
		  }

		  return ((byte)(file1 [offset1 + 4] & 0x7F) == (byte)(file2 [offset2 + 4] & 0x7F));
	   }

	   /// <summary>
	   /// Read the current page <code>lastPage</code> and place it in
	   /// the <code>lastPageData</code> buffer and set the <code>lastLen</code>.
	   /// </summary>
	   /// <exception cref="IOException"> when an IO error occurs </exception>
	   private void fetchPage()
	   {
		  try
		  {

			 //\\//\\//\\//\\//\\//\\//\\//
			 if (doDebugMessages)
			 {
				Debug.WriteLine("===fetchPage() " + lastPage);
			 }

			 lastLen = cache.readPagePacket(lastPage, lastPageData, 0);
			 lastOffset = 0;
		  }
		  catch (OneWireException e)
		  {
			 throw new System.IO.IOException(e.ToString());
		  }
	   }

	   /// <summary>
	   /// Create a file or directory entry in the current directory page
	   /// specified with fePage, feOffset, and feData.
	   /// </summary>
	   /// <param name="newEntry"> file or directory entry to create </param>
	   /// <param name="startPage"> page that the elements data (-1 if has no data yet) </param>
	   /// <param name="numberPages"> number of page of the elements data </param>
	   /// <param name="prevEntry"> previous entry used for back reference in creating new
	   ///        directories </param>
	   /// <param name="prevEntryStart"> previous entry start page for directory back
	   ///        referenece
	   /// </param>
	   /// <exception cref="FileNotFoundException"> if the filesystem runs out of space or
	   ///         if an IO error occurs </exception>
	   private void createEntry(byte[] newEntry, int startPage, int numberPages, byte[] prevEntry, int prevEntryStart)
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.Write("===createEntry() ");
			 Debug.Write("=prevEntryStart " + prevEntryStart + " ");
			 debugDump(newEntry, 0, 5);
			 debugDump(feData, 0, feLen);
		  }

		  // clear last page read flag in the cache
		  cache.clearLastPageRead();

		  int new_page;

		  try
		  {

			 // check if room in current page
			 if ((feLen + LEN_FILE_ENTRY) <= maxDataLen)
			 {

				// add to current page
				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=add to current dir page " + fePage);
				}

				// get the pagebitmap
				readBitMap();

				// check if this is just a new entry pointing to an old file
				if (startPage != -1)
				{
				   // instead of getting a new file, use the old file start
				   new_page = startPage;
				}
				else
				{
				   // get a new file to represent the new file/directory location

				   // get the next available page
				   new_page = getFirstFreePage((newEntry[4] == (byte)102) || (newEntry[4] == (byte)101));

				   // verify got a free page
				   if (new_page < 0)
				   {
					  try
					  {
						 sync();
					  }
					  catch (OWSyncFailedException)
					  {
						 // DRAIN
					  }

					  // if extension is 101 or 102, it could be there is not COUNTER pages
					  if ((newEntry[4] == (byte)102) || (newEntry[4] == (byte)101))
					  {
						 throw new OWFileNotFoundException("Out of space on 1-Wire device, or no secure pages available");
					  }
					  else
					  {
						 throw new OWFileNotFoundException("Out of space on 1-Wire device");
					  }
				   }
				}

				// get next page pointer
				int npp = com.dalsemi.onewire.utils.Convert.toInt(feData, feLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				// copy the file name into
				Array.Copy(newEntry, 0, feData, feLen - LEN_PAGE_PTR, LEN_FILENAME);
                    com.dalsemi.onewire.utils.Convert.toByteArray(new_page, feData, feLen - LEN_PAGE_PTR + LEN_FILENAME, LEN_PAGE_PTR);
                    com.dalsemi.onewire.utils.Convert.toByteArray(((numberPages == -1) ? 1 : numberPages), feData, feLen - LEN_PAGE_PTR + LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);

				feOffset = feLen - LEN_PAGE_PTR;
				feLen = feLen + LEN_FILE_ENTRY;

				// restore next page pointer
				com.dalsemi.onewire.utils.Convert.toByteArray(npp, feData, feLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				// check if this is not a rename operation
				if (startPage == -1)
				{
				   // no rename, so write the new file/directory starting data

				   // mark page used
				   markPageUsed(new_page);

				   // put blank data page or directory entry in new page
				   if ((newEntry [4] & 0x7F) == EXT_DIRECTORY)
				   {
					  // Directory Marker 'DM'
					  tempPage[0] = (byte)((LEN_PAGE_PTR == 1) ? 0x0A : 0x0B);
					  tempPage[0] |= (byte)((owd.Length == 1) ? unchecked(0xA0) : 0xB0);

					  // dummy byte
					  tempPage[1] = 0;

					  Array.Copy(prevEntry, 0, tempPage, 2, 4);
					  com.dalsemi.onewire.utils.Convert.toByteArray(prevEntryStart, tempPage, 6, LEN_PAGE_PTR);
					  // set next page pointer to end
					  com.dalsemi.onewire.utils.Convert.toByteArray(0, tempPage, 6 + LEN_PAGE_PTR, LEN_PAGE_PTR);
					  cache.writePagePacket(new_page, tempPage, 0, 6 + LEN_PAGE_PTR * 2);
				   }
				   else
				   {
					  com.dalsemi.onewire.utils.Convert.toByteArray(0, smallBuf, 0, LEN_PAGE_PTR);
					  cache.writePagePacket(new_page, smallBuf, 0, LEN_PAGE_PTR);
				   }
				}

				// put new directory page in place
				cache.writePagePacket(fePage, feData, 0, feLen);

				// set the page bitmap
				writeBitMap();

				// if the new entry is not a directory
				if ((newEntry [4] & 0x7F) != EXT_DIRECTORY)
				{

				   // setup the pointers for writing
				   filePosition = 0;
				   lastPage = new_page;
				   lastOffset = 0;
				   lastLen = 1;
				}
			 }
			 else
			 {
				// need a new directory page

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=need a new dir page ");
				}

				// get the pagebitmap
				readBitMap();

				// get new page for directory
				int new_dir_page = getFirstFreePage(false);

				// verify got a free page
				if (new_dir_page < 0)
				{
				   try
				   {
					  sync();
				   }
				   catch (OWSyncFailedException)
				   {
					  // DRAIN
				   }

				   throw new OWFileNotFoundException("Out of space on 1-Wire device");
				}

				// mark page used
				markPageUsed(new_dir_page);

				// check if this is just a new entry pointing to an old file
				if (startPage != -1)
				{
				   // instead of getting a new file, use the old file start
				   new_page = startPage;
				}
				else
				{
				   // get a new file to represent the new file/directory location

				   // get new page for the file
				   new_page = getNextFreePage((newEntry[4] == (byte)102) || (newEntry[4] == (byte)101));

				   // verify got a free page
				   if (new_page < 0)
				   {
					  try
					  {
						 sync();
					  }
					  catch (OWSyncFailedException)
					  {
						 // DRAIN
					  }

					  // if extension is 101 or 102, it could be there is not COUNTER pages
					  if ((newEntry[4] == (byte)102) || (newEntry[4] == (byte)101))
					  {
						 throw new OWFileNotFoundException("Out of space on 1-Wire device, or no secure pages available");
					  }
					  else
					  {
						 throw new OWFileNotFoundException("Out of space on 1-Wire device");
					  }
				   }

				   // mark page used
				   markPageUsed(new_page);
				}

				// create the new directory entry page
				Array.Copy(newEntry, 0, lastPageData, 0, LEN_FILENAME);
				com.dalsemi.onewire.utils.Convert.toByteArray(new_page, lastPageData, LEN_FILENAME, LEN_PAGE_PTR);
				com.dalsemi.onewire.utils.Convert.toByteArray(((numberPages == -1) ? 1 : numberPages), lastPageData, LEN_FILENAME + LEN_PAGE_PTR, LEN_PAGE_PTR);
				feOffset = 0;

				// set next page pointer to end
				com.dalsemi.onewire.utils.Convert.toByteArray(0, lastPageData, LEN_FILE_ENTRY, LEN_PAGE_PTR);

				// put new directory page in place
				cache.writePagePacket(new_dir_page, lastPageData, 0, LEN_FILE_ENTRY + LEN_PAGE_PTR);

				// check if this is not a rename operation
				if (startPage == -1)
				{
				   // is not a rename operation so write new file/directory start data (stub)

				   // write the new file/directory page
				   if ((newEntry [4] & 0x7F) == EXT_DIRECTORY)
				   {
					  // Directory Marker 'DM'
					  tempPage[0] = (byte)((LEN_PAGE_PTR == 1) ? 0x0A : 0x0B);
					  tempPage[0] |= (byte)((owd.Length == 1) ? 0xA0 : 0xB0);

					  // dummy byte
					  tempPage[1] = 0;

					  Array.Copy(prevEntry, 0, tempPage, 2, 4);
					  com.dalsemi.onewire.utils.Convert.toByteArray(prevEntryStart, tempPage, 6, LEN_PAGE_PTR);
					  // set next page pointer to end
					  com.dalsemi.onewire.utils.Convert.toByteArray(0, tempPage, 6 + LEN_PAGE_PTR, LEN_PAGE_PTR);
					  cache.writePagePacket(new_page, tempPage, 0, 6 + LEN_PAGE_PTR * 2);
				   }
				   else
				   {
					  com.dalsemi.onewire.utils.Convert.toByteArray(0, smallBuf, 0, LEN_PAGE_PTR);
					  cache.writePagePacket(new_page, smallBuf, 0, LEN_PAGE_PTR);
				   }
				}

				// update the page pointer in the old directory page
				com.dalsemi.onewire.utils.Convert.toByteArray(new_dir_page, feData, feLen - LEN_PAGE_PTR, LEN_PAGE_PTR);

				// put new directory page in place
				cache.writePagePacket(fePage, feData, 0, feLen);

				// set file entry info to the new directory page
				fePage = new_dir_page;
				feOffset = 0;
				feLen = LEN_FILE_ENTRY + LEN_PAGE_PTR;
				Array.Copy(lastPageData, 0, feData, 0, feLen);

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=NEW fePage=" + fePage + " feOffset " + feOffset + " feLen " + feLen);
				   debugDump(feData, 0, feLen);
				}

				// set the page bitmap
				writeBitMap();

				// if the new entry is a directory
				if ((newEntry [4] & 0x7F) != EXT_DIRECTORY)
				{
				   // set file position info
				   filePosition = 0;
				   lastPage = new_page;
				   lastOffset = 0;
				   lastLen = 1;
				}
			 }
		  }
		  catch (OneWireException e)
		  {
			 throw new OWFileNotFoundException(e.ToString());
		  }
	   }

	   /// <summary>
	   /// Check to see if the Filesystem is valid based on the
	   /// root directory provided in feData
	   /// </summary>
	   /// <exception cref="OneWireException"> if the filesystem is invalid </exception>
	   private void validateFileSystem()
	   {

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("===validateFileSystem()");
		  }

		  // clear last page read flag in the cache
		  cache.clearLastPageRead();

		  // check for SATELLITE
		  LEN_PAGE_PTR = ((feData [0] & 0x000F) == 0x0B) ? 2 : 1;
		  if (((feData [0] & 0x00F0) == 0xB0) && ((feData[1 + LEN_PAGE_PTR] & 0x0002) == 0))
		  {
			 // read the DM page
			 int len = cache.readPagePacket(com.dalsemi.onewire.utils.Convert.toInt(feData, 1, LEN_PAGE_PTR), dmBuf, 0);

			 if (len >= (8 + LEN_PAGE_PTR))
			 {
				// copy address to temp buff (or piece of address number)
				Array.Copy(dmBuf, 0, addrBuf, 0, 8);

				// get ref to adapter to create the new MASTER container
				DSPortAdapter adapter = owd[0].Adapter;
				owd = new OneWireContainer[1];
				owd[0] = adapter.getDeviceContainer(addrBuf);

				// check for overdrive
				if ((adapter.Speed == DSPortAdapter.SPEED_OVERDRIVE) && (owd[0].MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
				{
				   owd[0].setSpeed(DSPortAdapter.SPEED_OVERDRIVE,false);
				}

				// free the current cache
				cache.removeOwner(this);
				if (cache.noOwners())
				{
				   // remove the cache from the hash
				   memoryCacheHash.Remove(address);
				   cache = null;
				}

				// recreate the cache and setup
				setupFD(owd,rawPath);

				// get the new root dir
				fePage = 0;
				feLen = cache.readPagePacket(0, feData, 0);

				// make sure this is not a SATELLITE also (could be recursively bad!)
				LEN_PAGE_PTR = ((feData [0] & 0x000F) == 0x0B) ? 2 : 1;
				if (((feData [0] & 0x000F) == 0x0B) && ((feData[1 + LEN_PAGE_PTR] & 0x0002) == 0))
				{
				   throw new OneWireIOException("Invalid filesystem, this is a satellite device, pointing to another satellite?");
				}

				// call this recursively
				validateFileSystem();
				return;
			 }
			 else
			 {
				throw new OneWireIOException("Invalid filesystem, this is a satellite device with invalid MASTER reference");
			 }
		  }

		  // check for MASTER
		  if (((feData [0] & 0x00F0) == 0xB0) && ((feData[1 + LEN_PAGE_PTR] & 0x0002) == 0x0002))
		  {
			 // read the DM file
			 int page = com.dalsemi.onewire.utils.Convert.toInt(feData, 1, LEN_PAGE_PTR);

			 // make sure is valid page
			 if ((page == 0) || (page >= totalPages))
			 {
				throw new OneWireIOException("Invalid Filesystem, Device Map page number not valid.");
			 }

			 // check for incorrect or incomplete device list (member owd)
			 int num_devices = verifyDeviceMap(page, 0, false);
			 if (num_devices > 0)
			 {
				// create a new list
				verifyDeviceMap(page, num_devices, (owd[0].Adapter.Speed == DSPortAdapter.SPEED_OVERDRIVE));

				// free the current cache
				cache.removeOwner(this);
				if (cache.noOwners())
				{
				   // remove the cache from the hash
				   memoryCacheHash.Remove(address);
				   cache = null;
				}

				// recreate the cache and setup
				setupFD(owd,rawPath);

				// continue on with the root dir
				fePage = 0;
				feLen = cache.readPagePacket(0, feData, 0);
			 }
		  }

		  // check and verify that this is a valid directory
		  if (((totalPages <= 256) && ((feData [0] & 0x000F) != 0x0A)) || ((totalPages > 256) && ((feData [0] & 0x000F) != 0x0B)))
		  {
			 throw new OneWireIOException("Invalid Filesystem marker found, number of pages incorrect");
		  }

		  if (((owd.Length == 1) && ((feData [0] & 0x00F0) != 0x00A0)) || ((owd.Length > 1) && ((feData [0] & 0x00F0) != 0x00B0)))
		  {
			 throw new OneWireIOException("Invalid Filesystem marker found, multi-device marker incorrect");
		  }

		  // check where page bitmap is
		  if ((feData [1 + LEN_PAGE_PTR] & 0x0080) != 0)
		  {
			 bitmapType = BM_LOCAL;
			 pbmByteOffset = 2 + LEN_PAGE_PTR;
			 pbmBitOffset = 0;
			 pbmStartPage = 0; // used as flag to see if filesystem has been validated
		  }
		  else if (bitmapType != BM_CACHE)
		  {
			 bitmapType = BM_FILE;
			 pbmStartPage = com.dalsemi.onewire.utils.Convert.toInt(feData, LEN_CONTROL_DATA - LEN_PAGE_PTR * 2, LEN_PAGE_PTR);
			 pbmNumPages = com.dalsemi.onewire.utils.Convert.toInt(feData, LEN_CONTROL_DATA - LEN_PAGE_PTR, LEN_PAGE_PTR);
			 pbmByteOffset = 0;
			 pbmBitOffset = 0;

			 // make sure the number of pages in a FILE BM is correct
			 int pbm_bytes = (totalPages / 8);
			 int pgs = pbm_bytes / (maxDataLen - LEN_PAGE_PTR);
			 if ((pbm_bytes % (maxDataLen - LEN_PAGE_PTR)) > 0)
			 {
				pgs++;
			 }

			 if (pbmNumPages != pgs)
			 {
				throw new OneWireIOException("Invalid Filesystem, incorrect number of pages in remote bitmap file!");
			 }
		  }
		  else
		  {
			 pbmStartPage = 0;
		  }

		  //\\//\\//\\//\\//\\//\\//\\//
		  if (doDebugMessages)
		  {
			 Debug.WriteLine("= is valid, pbmStartPage=" + pbmStartPage);
		  }
	   }

	   /// <summary>
	   /// Verify the Device Map of a MASTER device is correct.
	   /// </summary>
	   /// <param name="page"> starting page number of the device map file </param>
	   /// <param name="numberOfContainers"> to re-create the
	   ///        OneWireContainer array in the instance variable
	   ///        from the devices listed in the device map 'owd[]'.
	   ///        Zero indicates leave the list alone.  >0 means
	   ///        recreate the array keeping the same MASTER device. </param>
	   /// <param name="setOverdrive"> <code> true </code> if set new
	   ///        containers to do a max speed of overdrive if
	   ///        possible
	   /// 
	   /// @returns the number of devices in the device map if
	   ///          the current device list is INVALID and returns
	   ///          zero if the current device list is VALID.
	   /// </param>
	   /// <exception cref="OneWireException"> when an IO error occurs </exception>
	   protected internal virtual int verifyDeviceMap(int startPage, int numberOfContainers, bool setOverdrive)
	   {
		  int len, data_len;
		  int ow_cnt = 1;
		  int addr_offset = 0;
		  int pg_offset = 0;
		  int copy_len;
		  DSPortAdapter adapter = null;

		  // flag to indicate the device list 'owd' list is valid
		  bool list_valid = true;

		  // first page to read
		  int page = startPage;

		  // clear last page read flag in the cache
		  cache.clearLastPageRead();

		  // check to see if need to create a new array for the new list of containers
		  if (numberOfContainers > 0)
		  {
			 // get reference to the adapter for use in creating containers
			 adapter = owd[0].Adapter;

			 OneWireContainer master_owc = owd[0];
			 owd = new OneWireContainer[numberOfContainers + 1];
			 owd[0] = master_owc;
		  }

		  // loop to read the Device Map file
		  do
		  {
			 // read the first file page
			 len = cache.readPagePacket(page, dmBuf, 0);
			 data_len = len - LEN_PAGE_PTR;

			 // loop through the device addresses in the device map file
			 while (pg_offset < data_len)
			 {
				if ((data_len - pg_offset) >= (8 - addr_offset))
				{
				   copy_len = 8 - addr_offset;
				}
				else
				{
				   copy_len = data_len - pg_offset;
				}

				// copy address to temp buff (or piece of address number)
				Array.Copy(dmBuf, pg_offset, addrBuf, addr_offset, copy_len);

				// increment offsets
				addr_offset += copy_len;
				pg_offset += copy_len;

				// convert completed address to long and compare
				if (addr_offset >= 8)
				{
				   // check if creating OneWireContainers
				   if (numberOfContainers > 0)
				   {
					  owd[ow_cnt] = adapter.getDeviceContainer(addrBuf);

					  // set new container to correct speed
					  if (setOverdrive && (owd[ow_cnt].MaxSpeed == DSPortAdapter.SPEED_OVERDRIVE))
					  {
						 owd[ow_cnt].setSpeed(DSPortAdapter.SPEED_OVERDRIVE,false);
					  }
				   }
				   else
				   {
					  // not creating containers so just check if correct
					  if (owd.Length <= ow_cnt)
					  {
						 list_valid = false;
					  }
					  else if (Address.toLong(addrBuf) != owd[ow_cnt].AddressAsLong)
					  {
						 list_valid = false;
					  }
				   }

				   ow_cnt++;
				   addr_offset = 0;
				}
			 }

			 // get the next page
			 page = com.dalsemi.onewire.utils.Convert.toInt(dmBuf, data_len, LEN_PAGE_PTR);

			 pg_offset = 0;
		  } while (page != 0);

		  // verify correct number of devices found
		  return (list_valid) ? 0 : ow_cnt - 1;
	   }


	   /// <summary>
	   /// Parse the provided raw path and set the provided vector.
	   /// </summary>
	   /// <param name="rawPath"> path to parse </param>
	   private bool parsePath(string rawPath, ArrayList parsedPath)
	   {
		  // parse name into a vector of byte arrays using the file structure
		  int index , last_index = 0, period_index , i , name_len ;
		  string field;
		  byte[] name;

		  do
		  {
			 index = rawPath.IndexOf(OWFile.separator, last_index, StringComparison.Ordinal);
			 name_len = 0;

			 // check if this is the last field
			 if ((index == -1) && (last_index < rawPath.Length))
			 {
				index = rawPath.Length;
			 }

			 // not done
			 if (index > 0)
			 {

				// get the field
				field = rawPath.Substring(last_index, index - last_index);

				// check for bogus field
				if (field.Length == 0)
				{
				   return false;
				}

				// create byte array for field
				name = new byte [LEN_FILENAME];

				Array.Copy(initName, 0, name, 0, LEN_FILENAME);

				// check if this is: ".", "..", or "name.number"
				period_index = field.IndexOf(".", 0, StringComparison.Ordinal);

				// period not in field
				if (period_index == -1)
				{

				   // check for valid length
				   if (field.Length > 4)
				   {
					  return false;
				   }

				   // is name only
				   Array.Copy(System.Text.Encoding.ASCII.GetBytes(field), 0, name, 0, field.Length);
				   name_len = field.Length;

				   // check if last field
				   if (index != rawPath.Length)
				   {
					  name [4] = EXT_DIRECTORY;
				   }

				   //\\//\\//\\//\\//\\//\\//\\//
				   if (doDebugMessages)
				   {
					  Debug.Write("=Parse, directory: ");
					  debugDump(name, 0, 5);
				   }
				}
				else
				{

				   // assume that if first char is '.' then must be '.' or '..' directory refs
				   if (period_index == 0)
				   {

					  // check for valid length
					  if (field.Length > 2)
					  {

						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.WriteLine("=Parse ERROR, '.' field length > 2 ");
						 }

						 return false;
					  }

					  Array.Copy(System.Text.Encoding.ASCII.GetBytes(field), 0, name, 0, field.Length);
					  name [4] = EXT_DIRECTORY;
					  name_len = field.Length;

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.Write("=Parse, directory: ");
						 debugDump(name, 0, 5);
					  }
				   }
				   else
				   {

					  // is name.number file
					  name_len = period_index;

					  // get name part
					  Array.Copy(System.Text.Encoding.ASCII.GetBytes(field), 0, name, 0, period_index);

					  // get the name part
					  try
					  {
						 name [4] = (byte) int.Parse(field.Substring(period_index + 1, field.Length - (period_index + 1)));
					  }
					  catch (System.FormatException)
					  {
						 return false;
					  }

					  //\\//\\//\\//\\//\\//\\//\\//
					  if (doDebugMessages)
					  {
						 Debug.Write("=Parse, file: ");
						 debugDump(name, 0, 5);
					  }

					  // check if invalid field or extension
					  if ((index != rawPath.Length) || ((name [4] & 0x00FF) > 102))
					  {

						 //\\//\\//\\//\\//\\//\\//\\//
						 if (doDebugMessages)
						 {
							Debug.WriteLine("=Parse ERROR, entension > 102 or spaces detected");
						 }

						 return false;
					  }
				   }
				}

				// make sure file name is exceptable
				for (i = 0; i < name_len; i++)
				{
				   if (((name[i] & 0x00FF) < 0x21) || ((name[i] & 0x00FF) > 0x7E))
				   {
					  return false;
				   }
				}

				// add to path vector
				parsedPath.Add(name);
			 }

			 last_index = index + 1;
		  } while (index > -1);
		  return true;
	   }

	   //--------
	   //-------- Misc Utility Methods
	   //--------

	   /// <summary>
	   ///  Atomically creates a new, empty file named by this abstract pathname if
	   ///  and only if a file with this name does not yet exist.  The check for the
	   ///  existence of the file and the creation of the file if it does not exist
	   ///  are a single operation that is atomic with respect to all other
	   ///  filesystem activities that might affect the file.
	   /// </summary>
	   ///  <returns>  <code>true</code> if the named file does not exist and was
	   ///           successfully created; <code>false</code> if the named file
	   ///           already exists
	   /// </returns>
	   ///  <exception cref="IOException">
	   ///           If an I/O error occurred
	   ///  </exception>
	   protected internal virtual bool createNewFile()
	   {
		  if (exists())
		  {
			 return false;
		  }
		  else
		  {
			 try
			 {
				create(false, false, false, -1, -1);
			 }
			 catch (OWFileNotFoundException e)
			 {
				throw new System.IO.IOException(e.ToString());
			 }

			 return true;
		  }
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
	   protected internal virtual int HashCode
	   {
		   get
		   {
			  lock (cache)
			  {
				 int i , j , hash = 0;
				 string this_path = owd[0].AddressAsString + Path;
				 byte[] path_bytes = System.Text.Encoding.ASCII.GetBytes(this_path);
    
				 for (i = 0; i < (path_bytes.Length / 4); i++)
				 {
					hash ^= com.dalsemi.onewire.utils.Convert.toInt(path_bytes, i * 4, 4);
				 }
    
				 for (j = 0; j < (path_bytes.Length % 4); j++)
				 {
					hash ^= (int)(path_bytes [i * 4 + j] & 0x00FF);
				 }
    
				 return hash;
			  }
		   }
	   }

	   /// <summary>
	   /// Gets the OneWireContainers that represent this Filesystem.
	   /// 
	   /// </summary>
	   ///  <returns>  array of OneWireContainer's that represent this
	   ///           Filesystem.
	   ///  </returns>
	   protected internal virtual OneWireContainer[] OneWireContainers
	   {
		   get
		   {
			  return owd;
		   }
	   }

	   /// <summary>
	   /// Free's this file descriptors system resources.
	   /// 
	   /// </summary>
	   protected internal virtual void free()
	   {
		  lock (cache)
		  {
			 // if opened to write then remove the block
			 if (openedToWrite)
			 {
				cache.removeWriteOpen(owd[0].AddressAsString + Path);
			 }

			 // remove this owner from the list
			 cache.removeOwner(this);

			 // if not more owners then free the cache
			 if (cache.noOwners())
			 {
				// remove the cache from the hash
				memoryCacheHash.Remove(address);

				cache = null;

				//\\//\\//\\//\\//\\//\\//\\//
				if (doDebugMessages)
				{
				   Debug.WriteLine("=released cache for " + Address.ToString(address.Value));
				}
			 }
		  }
	   }

	   /// <summary>
	   /// Debug dump utility method
	   /// </summary>
	   /// <param name="buf"> buffer to dump </param>
	   /// <param name="offset"> offset to start in the buffer </param>
	   /// <param name="len"> length to dump </param>
	   private void debugDump(byte[] buf, int offset, int len)
	   {
		  for (int i = offset; i < (offset + len); i++)
		  {
			 Debug.Write(((int) buf [i] & 0x00FF).ToString("x") + " ");
		  }

		  Debug.WriteLine("");
	   }
	}

}