using System.Text;

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

// Copyright (c) 2000-2001 by the XML 1-Wire Project
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright notice, this list of
//    conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright notice, this list
//    of conditions and the following disclaimer in the documentation and/or other materials
//    provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE AUTHORS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
// THE AUTHORS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
// $Id: XML.java,v 1.1.1.1 2001/06/20 18:21:11 seankelly Exp $
namespace com.dalsemi.onewire.application.tag
{

	/// <summary>
	/// XML services.
	/// 
	/// This class provides several XML utility functions.
	/// 
	/// @author Kelly
	/// </summary>
	public class XML
	{

	   /// <summary>
	   /// Create a SAX parser.
	   /// </summary>
	   /// <returns> A new SAX parser. </returns>
	   public static SAXParser createSAXParser()
	   {
		  return new SAXParser();
	   }

	   /// <summary>
	   /// Escape special characters in the given string.
	   /// 
	   /// This method takes a string and escapes special characters so it can be used as
	   /// the text content of an element or as an attribute value.  For example, the
	   /// ampersand &amp; becomes &amp;amp;.
	   /// </summary>
	   /// <param name="source"> The string to escape </param>
	   /// <returns> The escaped string. </returns>
	   public static string escape(string source)
	   {

		  // Optimistically start with at least as many characters in the source.
		  StringBuilder rc = new StringBuilder(source.Length);

		  for (int i = 0; i < source.Length; ++i)
		  {
			 char c = source[i];

			 // Nonprintable characters print as their corresponding char reference.
			 // Thanks to Apache project for specs for these characters.
			 if ((c < ' ' && c != '\t' && c != '\n' && c != '\r') || c > 0x7E || c == 0xF7)
			 {
				rc.Append("&#").Append(c).Append(';');
			 }
			 else
			 {

				// Use an entity reference where appropriate
				switch (c)
				{

				   case '"' :
					  rc.Append("&quot;");
					  break;
				   case '\'' :
					  rc.Append("&apos;");
					  break;
				   case '<' :
					  rc.Append("&lt;");
					  break;
				   case '>' :
					  rc.Append("&gt;");
					  break;
				   case '&' :
					  rc.Append("&amp;");
					  break;
				   default :
					  rc.Append(c);
				  break;
				}
			 }
		  }

		  return rc.ToString();
	   }
	}

}