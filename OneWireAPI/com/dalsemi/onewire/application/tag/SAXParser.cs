using System;

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
// THIS SOFTWARE IS PROVIDED BY THE AUTHORS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHORS OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// $Id: SAXParser.java,v 1.1.1.1 2001/06/20 18:21:11 seankelly Exp $
namespace com.dalsemi.onewire.application.tag
{

	using org.xml.sax;


	/// <summary>
	/// <P>A SAX parser.</P>
	/// 
	/// <P>This class encapsulates the underlying parser implementation.  We support
	/// only the SAX1 interface for now.</P>
	/// <P>Specify the SAXParser implementation by adding the property
	/// <code>SAXParser.ClassName</code> to the onewire.properties file with the
	/// fully qualified classname of an implementation of
	/// <code>org.xml.sax.Parser</code>.  The default classname used is
	/// for nanoxml's parser: <code>nanoxml.sax.SAXParser</code>.  
	/// 
	/// @author Kelly
	/// </summary>
	public class SAXParser : org.xml.sax.Parser
	{
	   /// <summary>
	   /// The parser implementation to which we delegate. </summary>
	   private org.xml.sax.Parser parser;

	   /// <summary>
	   /// Construct the SAX parser.
	   /// </summary>
	   public SAXParser()
	   {
		  // load the fully-qualified classname of the SAX Parser
		  string className = OneWireAccessProvider.getProperty("SAXParser.ClassName");
		  if (string.ReferenceEquals(className, null))
		  {
			 // default to NanoXML
			 className = "nanoxml.sax.SAXParser";
		  }

		  try
		  {
			 Type c = Type.GetType(className);
             parser = (org.xml.sax.Parser)Activator.CreateInstance(c);
		  }
		  catch (System.Exception e)
		  {
			 throw new Exception("Can't load SAX Parser (" + className + "): " + e.Message);
		  }
	   }

	   /// <summary>
	   /// Set the locale for errors and warnings.
	   /// </summary>
	   /// <param name="locale"> The locale to use. </param>
	   /// <exception cref="SAXException"> If the <var>locale</var> is not supported. </exception>
	   //public virtual Locale Locale
	   //{
		  // set
		  // {
			 // parser.Locale = value;
		  // }
	   //}

	   /// <summary>
	   /// Register a custom entity resolver.
	   /// 
	   /// If one is not registered, the parser will resolve system identifiers in an
	   /// implementation dependent way.
	   /// </summary>
	   /// <param name="resolver"> The entity resolver to use. </param>
	   public virtual EntityResolver EntityResolver
	   {
		   set
		   {
			  parser.EntityResolver = value;
		   }
	   }

	   /// <summary>
	   /// Register a DTD event handler.
	   /// 
	   /// If one is not registered, all DTD events reported by the parser will be ignored.
	   /// </summary>
	   /// <param name="handler"> The DTD handler to use. </param>
	   public virtual DTDHandler DTDHandler
	   {
		   set
		   {
			  parser.DTDHandler = value;
		   }
	   }

	   /// <summary>
	   /// Register a document event handler.
	   /// 
	   /// If one is not registered, all document events reported by the parser will be ignored.
	   /// </summary>
	   /// <param name="handler"> The document handler to use. </param>
	   public virtual DocumentHandler DocumentHandler
	   {
		   set
		   {
			  parser.DocumentHandler = value;
		   }
	   }

	   /// <summary>
	   /// Register an error event handler.
	   /// 
	   /// If one is not registered, all error events except for <code>fatalError</code>
	   /// are ignored.  A <code>fatalError</code> thorws a <seealso cref="org.xml.sax.SAXException"/>.
	   /// </summary>
	   /// <param name="handler"> The error handler to use. </param>
	   public virtual ErrorHandler ErrorHandler
	   {
		   set
		   {
			  parser.ErrorHandler = value;
		   }
	   }

	   /// <summary>
	   /// Parse an XML document.
	   /// </summary>
	   /// <param name="source"> Source of the document to parse.
	   /// </param>
	   /// <param name="inputSource"> </param>
	   /// <exception cref="SAXException"> Any SAX exception, possibly wrapping another exception. </exception>
	   /// <exception cref="IOException"> If an I/O error occurred while reading the document. </exception>
	   public virtual void parse(InputSource inputSource)
	   {
		  parser.parse(inputSource);
	   }

	   /// <summary>
	   /// Parse an XML document specified by system identifier or URL.
	   /// </summary>
	   /// <param name="systemID"> The system ID or URL of the document to parse. </param>
	   /// <exception cref="SAXException"> Any SAX exception, possibly wrapping another exception. </exception>
	   /// <exception cref="IOException"> If an I/O error occurred while reading the document. </exception>
	   public virtual void parse(string systemID)
	   {
		  parser.parse(systemID);
	   }
	}

}