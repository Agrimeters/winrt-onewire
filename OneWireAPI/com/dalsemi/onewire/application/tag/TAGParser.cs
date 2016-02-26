using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000,2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.application.tag
{

	using SAXException = org.xml.sax.SAXException;
	using InputSource = org.xml.sax.InputSource;
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;


	/// <summary>
	/// The tag parser parses tagging information.
	/// </summary>
	public class TAGParser
	{

	   /// <summary>
	   /// Construct the tag parser.
	   /// </summary>
	   /// <param name="adapter"> What port adapter will serve the devices created. </param>
	   public TAGParser(DSPortAdapter adapter)
	   {
          parser = XML.createSAXParser();
		  handler = new TAGHandler();

		  try
		  {
			 handler.Adapter = adapter;
		  }
		  catch (System.Exception e)
		  {
			 Debug.WriteLine(e);
		  }

		  parser.DocumentHandler = handler;
		  parser.ErrorHandler = handler;
	   }

	   /// <summary>
	   /// Returns the vector of TaggedDevice objects described in the TAG file.
	   /// </summary>
	   /// <param name="in"> The XML document to parse.
	   /// </param>
	   /// <returns> Vector of TaggedDevice objects. </returns>
	   /// <exception cref="SAXException"> If a parse error occurs parsing <var>in</var>. </exception>
	   /// <exception cref="IOException"> If an I/O error occurs while reading <var>in</var>. </exception>
	   public virtual ArrayList parse(System.IO.Stream @in)
	   {
		  InputSource insource = new InputSource(@in);

		  parser.parse(insource);

		  ArrayList v = handler.TaggedDeviceList;

		  return v;
	   }

	   /// <summary>
	   /// Returns the vector of Branch TaggedDevice objects described in the TAG file.
	   /// The XML should already be parsed before calling this method.
	   /// </summary>
	   /// <param name="in"> The XML document to parse.
	   /// </param>
	   /// <returns> Vector of Branch TaggedDevice objects. </returns>
	   public virtual ArrayList Branches
	   {
		   get
		   {
    
			  ArrayList v = handler.AllBranches;
    
			  return v;
		   }
	   }

	   /// <summary>
	   /// Returns the vector of OWPath objects discovered through parsing 
	   /// the XML file.  The XML file should already be parsed before calling 
	   /// this method.
	   /// </summary>
	   /// <param name="no"> parameters.
	   /// </param>
	   /// <returns> Vector of OWPath objects. </returns>
	   public virtual ArrayList OWPaths
	   {
		   get
		   {
    
			  ArrayList v = handler.AllBranchPaths;
    
			  return v;
		   }
	   }


       /// <summary>
       /// Field parser </summary>
       private XmlReader parser;
	   //TODO private SAXParser parser;

	   /// <summary>
	   /// Field handler </summary>
	   private TAGHandler handler;
	}

}