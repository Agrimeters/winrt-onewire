using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    using com.dalsemi.onewire.adapter;
    using utils;

    /// <summary>
    /// The tag parser parses tagging information.
    /// </summary>
    public class TAGParserEx
	{

	   /// <summary>
	   /// Construct the tag parser.
	   /// </summary>
	   /// <param name="adapter"> What port adapter will serve the devices created. </param>
	   public TAGParserEx(DSPortAdapter adapter)
	   {
		  handler = new TAGHandlerEx();

		  try
		  {
			 handler.Adapter = adapter;
		  }
		  catch (System.Exception e)
		  {
			 Debug.WriteLine(e);
		  }
	   }

	   /// <summary>
	   /// Returns the vector of TaggedDevice objects described in the TAG file.
	   /// </summary>
	   /// <param name="in"> The XML document to parse.
	   /// </param>
	   /// <returns> Vector of TaggedDevice objects. </returns>
	   public virtual List<TaggedDevice> parse(Stream inp)
	   {
          XmlReaderSettings settings = new XmlReaderSettings() { IgnoreWhitespace = true };

          parser = XmlReader.Create(inp, settings);

          handler.startDocument();

          parser.MoveToContent();

          do
          {
              switch (parser.NodeType)
              {
                  case XmlNodeType.Element:
                      handler.startElement(parser.Name, parser);
                      break;
                  case XmlNodeType.EndElement:
                      handler.endElement(parser.Name);
                      break;
              }

          } while (parser.Read());

          handler.endDocument();

          return handler.TaggedDeviceList;
	   }


	   /// <summary>
	   /// Returns the vector of Branch TaggedDevice objects described in the TAG file.
	   /// The XML should already be parsed before calling this method.
	   /// </summary>
	   /// <param name="in"> The XML document to parse.
	   /// </param>
	   /// <returns> Vector of Branch TaggedDevice objects. </returns>
	   public virtual List<TaggedDevice> Branches
	   {
		   get
		   {
			  return handler.AllBranches;
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
	   public virtual List<OWPath> OWPaths
	   {
		   get
		   {
			  return handler.AllBranchPaths;
		   }
	   }


       /// <summary>
       /// Field parser </summary>
	   private XmlReader parser;

	   /// <summary>
	   /// Field handler </summary>
	   private TAGHandlerEx handler;
	}

}