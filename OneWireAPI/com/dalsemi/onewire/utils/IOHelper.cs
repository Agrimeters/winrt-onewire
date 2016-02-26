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


namespace com.dalsemi.onewire.utils
{

	/// <summary>
	/// Generic IO routines.  Supports printing and reading arrays of bytes.
	/// Also, using the setReader and setWriter methods, the source of the
	/// bytes can come from any stream as well as the destination for
	/// written bytes.  All routines are static and final and handle all
	/// exceptional cases by returning a default value.
	/// 
	/// @version    0.02, 2 June 2001
	/// @author     SH
	/// </summary>
	public sealed class IOHelper
	{
	   /// <summary>
	   /// Do not instantiate this class </summary>
	   private IOHelper()
	   {
		   ;
	   }

	   /*----------------------------------------------------------------*/
	   /*   Reading Helper Methods                                       */
	   /*----------------------------------------------------------------*/

	   private static System.IO.StreamReader br = null;
	   // default the buffered reader to read from STDIN
	   static IOHelper()
	   {
	      try
		  {
			 br = new StreamReader(null);  //TODO STDIN
		  }
		  catch (System.Exception)
		  {
			Debug.WriteLine("IOHelper: Catastrophic Failure!");
			//TODO Environment.Exit(1);
		  }
		  try
		  {
			pw = new StreamWriter(null);  //TODO STDOUT
		  }
		  catch (System.Exception)
		  {
			Debug.WriteLine("IOHelper: Catastrophic Failure!");
            //TODO Environment.Exit(1);
          }
       }

	   public static Stream StreamReader
	   {
		  set
		  {
			 lock (typeof(IOHelper))
			 {
			    br = new System.IO.StreamReader(value);
			 }
		  }
	   }

	   public static string readLine()
	   {
		   lock (typeof(IOHelper))
		   {
			  try
			  {
				 return br.ReadLine();
			  }
			  catch (System.IO.IOException)
			  {
				 return "";
			  }
		   }
	   }

	   public static sbyte[] readBytes(int count, int pad, bool hex)
	   {
		   lock (typeof(IOHelper))
		   {
			  if (hex)
			  {
				 return readBytesHex(count,pad);
			  }
			  else
			  {
				 return readBytesAsc(count,pad);
			  }
		   }
	   }

	   public static sbyte[] readBytesHex(int count, int pad)
	   {
		   lock (typeof(IOHelper))
		   {
			  try
			  {
				 string s = br.ReadLine();
				 int len = s.Length > count ? count : s.Length;
				 sbyte[] ret;
        
				 if (count > 0)
				 {
					ret = new sbyte [count];
				 }
				 else
				 {
					ret = new sbyte [s.Length];
				 }
        
				 sbyte[] temp = parseHex(s, 0);
        
				 if (count == 0)
				 {
					return temp;
				 }
        
				 len = temp.Length;
        
				 Array.Copy(temp, 0, ret, 0, len);
        
				 for (; len < count; len++)
				 {
					ret [len] = (sbyte) pad;
				 }
        
				 return ret;
			  }
			  catch (System.Exception)
			  {
				 return new sbyte [count];
			  }
		   }
	   }

	   public static sbyte[] readBytesAsc(int count, int pad)
	   {
		   lock (typeof(IOHelper))
		   {
			  try
			  {
				 string s = br.ReadLine();
				 int len = s.Length > count ? count : s.Length;
				 sbyte[] ret;
        
				 if (count > 0)
				 {
					ret = new sbyte [count];
				 }
				 else
				 {
					ret = new sbyte [s.Length];
				 }
        
				 if (count == 0)
				 {
					Array.Copy(s.GetBytes(), 0, ret, 0, s.Length);
        
					return ret;
				 }
        
				 Array.Copy(s.GetBytes(), 0, ret, 0, len);
        
				 for (; len < count; len++)
				 {
					ret [len] = (sbyte) pad;
				 }
        
				 return ret;
			  }
			  catch (System.IO.IOException)
			  {
				 return new sbyte [count];
			  }
		   }
	   }

	   private static sbyte[] parseHex(string s, int size)
	   {
		  sbyte[] temp;
		  int index = 0;
		  char[] x = s.ToLower().ToCharArray();

		  if (size > 0)
		  {
			 temp = new sbyte [size];
		  }
		  else
		  {
			 temp = new sbyte [x.Length];
		  }

		  try
		  {
			 for (int i = 0; i < x.Length && index < temp.Length; index++)
			 {
				int digit = -1;

				while (i < x.Length && digit == -1)
				{
				   digit = Character.digit(x[i++], 16);
				}
				if (digit != -1)
				{
				   temp[index] = unchecked((sbyte)((digit << 4) & 0xF0));
				}

				digit = -1;

				while (i < x.Length && digit == -1)
				{
				   digit = Character.digit(x[i++], 16);
				}
				if (digit != -1)
				{
				  temp[index] |= (sbyte)(digit & 0x0F);
				}
			 }
		  }
		  catch (System.Exception)
		  {
			  ;
		  }

		  sbyte[] t;

		  if (size == 0 && temp.Length != index)
		  {
			 t = new sbyte [index];
			 Array.Copy(temp, 0, t, 0, t.Length);
		  }
		  else
		  {
			 t = temp;
		  }

		  return t;
	   }

	   public static int readInt()
	   {
		   lock (typeof(IOHelper))
		   {
			  return readInt(-1);
		   }
	   }
	   public static int readInt(int def)
	   {
		   lock (typeof(IOHelper))
		   {
			  try
			  {
				 return int.Parse(br.ReadLine());
			  }
			  catch (System.FormatException)
			  {
				 return def;
			  }
			  catch (System.IO.IOException)
			  {
				 return def;
			  }
		   }
	   }

	   /*----------------------------------------------------------------*/
	   /*   Writing Helper Methods                                       */
	   /*----------------------------------------------------------------*/

	   private static StreamWriter pw = null;
	   // default the print writer to write to STDOUT
	   public static Stream Writer
	   {
		   set
		   {
			   lock (typeof(IOHelper))
			   {
				  pw = new StreamWriter(value);
			   }
		   }
	   }

	   public static void writeBytesHex(string delim, sbyte[] b, int offset, int cnt)
	   {
		   lock (typeof(IOHelper))
		   {
			  int i = offset;
			  for (; i < (offset + cnt);)
			  {
				 if (i != offset && ((i - offset) & 15) == 0)
				 {
					pw.WriteLine();
				 }
				 pw.Write(byteStr(b[i++]));
				 pw.Write(delim);
			  }
			  pw.WriteLine();
			  pw.Flush();
		   }
	   }
	   public static void writeBytesHex(sbyte[] b, int offset, int cnt)
	   {
		   lock (typeof(IOHelper))
		   {
			  writeBytesHex(".", b, offset, cnt);
		   }
	   }
	   public static void writeBytesHex(sbyte[] b)
	   {
		   lock (typeof(IOHelper))
		   {
			  writeBytesHex(".", b, 0, b.Length);
		   }
	   }

	   /// <summary>
	   /// Writes a <code>byte[]</code> to the specified output stream.  This method
	   /// writes a combined hex and ascii representation where each line has
	   /// (at most) 16 bytes of data in hex followed by three spaces and the ascii
	   /// representation of those bytes.  To write out just the Hex representation,
	   /// use <code>writeBytesHex(byte[],int,int)</code>.
	   /// </summary>
	   /// <param name="b"> the byte array to print out. </param>
	   /// <param name="offset"> the starting location to begin printing </param>
	   /// <param name="cnt"> the number of bytes to print. </param>
	   public static void writeBytes(string delim, sbyte[] b, int offset, int cnt)
	   {
		   lock (typeof(IOHelper))
		   {
			  int last, i;
			  last = i = offset;
			  for (; i < (offset + cnt);)
			  {
				 if (i != offset && ((i - offset) & 15) == 0)
				 {
					pw.Write("  ");
					for (; last < i; last++)
					{
					   pw.Write((char)b[last]);
					}
					pw.WriteLine();
				 }
				 pw.Write(byteStr(b[i++]));
				 pw.Write(delim);
			  }
			  for (int k = i; ((k - offset) & 15) != 0; k++)
			  {
				 pw.Write("  ");
				 pw.Write(delim);
			  }
			  pw.Write("  ");
			  for (; last < i; last++)
			  {
				 pw.Write((char)b[last]);
			  }
			  pw.WriteLine();
			  pw.Flush();
		   }
	   }

	   /// <summary>
	   /// Writes a <code>byte[]</code> to the specified output stream.  This method
	   /// writes a combined hex and ascii representation where each line has
	   /// (at most) 16 bytes of data in hex followed by three spaces and the ascii
	   /// representation of those bytes.  To write out just the Hex representation,
	   /// use <code>writeBytesHex(byte[],int,int)</code>.
	   /// </summary>
	   /// <param name="b"> the byte array to print out. </param>
	   public static void writeBytes(sbyte[] b)
	   {
		   lock (typeof(IOHelper))
		   {
			  writeBytes(".", b, 0, b.Length);
		   }
	   }

	   public static void writeBytes(sbyte[] b, int offset, int cnt)
	   {
		   lock (typeof(IOHelper))
		   {
			  writeBytes(".", b, offset, cnt);
		   }
	   }

	   public static void write(string s)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(s);
			  pw.Flush();
		   }
	   }
	   public static void write(object o)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(o);
			  pw.Flush();
		   }
	   }
	   public static void write(bool b)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(b);
			  pw.Flush();
		   }
	   }
	   public static void write(int i)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(i);
			  pw.Flush();
		   }
	   }


	   public static void writeLine()
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine();
			  pw.Flush();
		   }
	   }
	   public static void writeLine(string s)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(s);
			  pw.Flush();
		   }
	   }
	   public static void writeLine(object o)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(o);
			  pw.Flush();
		   }
	   }
	   public static void writeLine(bool b)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(b);
			  pw.Flush();
		   }
	   }
	   public static void writeLine(int i)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(i);
			  pw.Flush();
		   }
	   }

	   public static void writeHex(sbyte b)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(byteStr(b));
			  pw.Flush();
		   }
	   }
	   public static void writeHex(long l)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.Write(l.ToString("x"));
			  pw.Flush();
		   }
	   }

	   public static void writeLineHex(sbyte b)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(byteStr(b));
			  pw.Flush();
		   }
	   }
	   public static void writeLineHex(long l)
	   {
		   lock (typeof(IOHelper))
		   {
			  pw.WriteLine(l.ToString("x"));
			  pw.Flush();
		   }
	   }

	   private static readonly char[] hex = "0123456789ABCDEF".ToCharArray();
	   private static string byteStr(sbyte b)
	   {
		  return "" + hex[((b >> 4) & 0x0F)] + hex[(b & 0x0F)];
	   }

	}

}