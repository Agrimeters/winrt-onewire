using System;
using System.IO;
using System.Diagnostics;

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
namespace com.dalsemi.onewire.debug
{
    using Windows.Storage;
    using Convert = utils.Convert;

    /// <summary>
    /// <para>This class is intended to help both developers of the 1-Wire API for
    /// Java and developers using the 1-Wire API for Java to have a standard
    /// method for printing debug messages.  Applications that want to see debug messages
    /// should call  the <code>setDebugMode(bool)</code> method.
    /// Classes that want to print information under debugging
    /// circumstances should call the <code>debug(String)</code>
    /// method.</para>
    /// 
    /// <para>Debug printing is turned off by default.</para>
    /// 
    /// @version    1.00, 1 Sep 2003
    /// @author     KA, SH
    /// </summary>
    public class Debug
	{
		private static bool DEBUG = false;
		private static StreamWriter @out = null;

	    /// <summary>
	    /// Static constructor.  Checks system properties to see if debugging
	    /// is enabled by default.  Also, will redirect debug output to a log
	    /// file if specified.
	    /// </summary>
		static Debug()
		{
		   string enable = OneWireAccessProvider.getProperty("onewire.debug");
		   if (!string.ReferenceEquals(enable, null) && enable.ToLower().Equals("true"))
		   {
			  DEBUG = true;
		   }
		   else
		   {
			  DEBUG = false;
		   }

		   if (DEBUG)
		   {
			  string logFile = OneWireAccessProvider.getProperty("onewire.debug.logfile");
			  if (!string.ReferenceEquals(logFile, null))
			  {
                  // ignore any absolute path provided, only use filename
                  string[] strtok = logFile.Split(new char[] { '\\' });
                  StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                  string logFilePath = localFolder.Path + "\\" + strtok[strtok.Length - 1].Trim();

                  try
                  {
                      @out = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write));
                      @out.AutoFlush = true;
				  }
				  catch (System.Exception e)
				  {
				      @out = null;
                      DEBUG = false;
					  debug("Error opening log file in Debug Static Constructor", e);
                  }
               }
		    }
		}

	    /// <summary>
	    /// Sets the debug printing mode for this application.
	    /// </summary>
	    /// @param <code>true</code> to see debug messages, <code>false</code>
	    ///        to suppress them </param>
		public static bool DebugMode
		{
			set
			{
				DEBUG = value;
			}
			get
			{
				return DEBUG;
			}
		}


		/// <summary>
		/// Sets the output stream for printing the debug info.
		/// </summary>
		/// <param name="out"> the output stream for printing the debug info. </param>
		public static StreamWriter StreamWriter
		{
			set
			{
			  @out = value;
			}
		}

	   /// <summary>
	   /// Prints the specified <code>java.lang.String</code> object
	   /// if debug mode is enabled.  This method calls <code>PrintStream.println(String)</code>,
	   /// and pre-pends the <code>String</code> ">> " to the message, so
	   /// taht if a program were to call (when debug mode was enabled):
	   /// <code><pre>
	   ///     com.dalsemi.onewire.debug.Debug.debug("Some notification...");
	   /// </pre></code>
	   /// the resulting output would look like:
	   /// <code><pre>
	   ///     >> Some notification...
	   /// </pre></code>
	   /// </summary>
	   /// <param name="x"> the message to print out if in debug mode </param>
	   public static void debug(string x)
	   {
		  if (DEBUG)
		  {
             @out.WriteLine(">> " + x);
          }
	   }

	   /// <summary>
	   /// Prints the specified array of bytes with a given label
	   /// if debug mode is enabled.  This method calls
	   /// <code>PrintStream.println(String)</code>,
	   /// and pre-pends the <code>String</code> ">> " to the message, so
	   /// taht if a program were to call (when debug mode was enabled):
	   /// <code><pre>
	   ///     com.dalsemi.onewire.debug.Debug.debug("Some notification...", myBytes);
	   /// </pre></code>
	   /// the resulting output would look like:
	   /// <code><pre>
	   ///     >> my label
	   ///     >>   FF F1 F2 F3 F4 F5 F6 FF
	   /// </pre></code>
	   /// </summary>
	   /// <param name="lbl"> the message to print out above the array </param>
	   /// <param name="bytes"> the byte array to print out </param>
	   public static void debug(string lbl, byte[] bytes)
	   {
	     if (DEBUG)
	     {
	   	    debug(lbl, bytes, 0, bytes.Length);
		 }
	   }

	   /// <summary>
	   /// Prints the specified array of bytes with a given label
	   /// if debug mode is enabled.  This method calls
	   /// <code>PrintStream.println(String)</code>,
	   /// and pre-pends the <code>String</code> ">> " to the message, so
	   /// taht if a program were to call (when debug mode was enabled):
	   /// <code><pre>
	   ///     com.dalsemi.onewire.debug.Debug.debug("Some notification...", myBytes, 0, 8);
	   /// </pre></code>
	   /// the resulting output would look like:
	   /// <code><pre>
	   ///     >> my label
	   ///     >>   FF F1 F2 F3 F4 F5 F6 FF
	   /// </pre></code>
	   /// </summary>
	   /// <param name="lbl"> the message to print out above the array </param>
	   /// <param name="bytes"> the byte array to print out </param>
	   /// <param name="offset"> the offset to start printing from the array </param>
	   /// <param name="length"> the number of bytes to print from the array </param>
		public static void debug(string lbl, byte[] bytes, int offset, int length)
		{
		  if (DEBUG)
		  {
			 @out.Write(">> " + lbl + ", offset=" + offset + ", length=" + length);
			 length += offset;
			 int inc = 8;
			 bool printHead = true;
			 for (int i = offset; i < length; i += inc)
			 {
				if (printHead)
				{
				   @out.WriteLine();
				   @out.Write(">>    ");
				}
				else
				{
				   @out.Write(" : ");
				}
				int len = Math.Min(inc, length - i);
				@out.Write(Convert.toHexString(bytes, i, len, " "));
				printHead = !printHead;
			 }
			 @out.WriteLine();
		  }
		}

	   /// <summary>
	   /// Prints the specified exception with a given label
	   /// if debug mode is enabled.  This method calls
	   /// <code>PrintStream.println(String)</code>,
	   /// and pre-pends the <code>String</code> ">> " to the message, so
	   /// taht if a program were to call (when debug mode was enabled):
	   /// <code><pre>
	   ///     com.dalsemi.onewire.debug.Debug.debug("Some notification...", exception);
	   /// </pre></code>
	   /// the resulting output would look like:
	   /// <code><pre>
	   ///     >> my label
	   ///     >>   OneWireIOException: Device Not Present
	   /// </pre></code>
	   /// </summary>
	   /// <param name="lbl"> the message to print out above the array </param>
	   /// <param name="bytes"> the byte array to print out </param>
	   /// <param name="offset"> the offset to start printing from the array </param>
	   /// <param name="length"> the number of bytes to print from the array </param>
	   public static void debug(string lbl, Exception t)
	   {
		  if (DEBUG)
		  {
			 @out.WriteLine(">> " + lbl);
			 @out.WriteLine(">>    " + t.Message);
			 @out.WriteLine(t.StackTrace);
             @out.WriteLine(Environment.StackTrace);
           }
        }

	   /// <summary>
	   /// Prints out an exception stack trace for debugging purposes.
	   /// This is useful to figure out which functions are calling
	   /// a particular function at runtime.
	   /// 
	   /// </summary>
	   public static void stackTrace()
	   {
		  if (DEBUG)
		  {
             @out.WriteLine("DEBUG STACK TRACE");
             @out.WriteLine(Environment.StackTrace);
		  }
	   }

	}
}