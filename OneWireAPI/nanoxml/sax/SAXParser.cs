using System.Collections;
using System.IO;
using System;

/* This file is part of NanoXML.
 *
 * $Revision: 2 $
 * $Date: 3/25/04 6:24p $
 * $Name: RELEASE_1_6_8 $
 *
 * Copyright (C) 2000 Marc De Scheemaecker, All Rights Reserved.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from the
 * use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software in
 *     a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *
 *  3. This notice may not be removed or altered from any source distribution.
 */


namespace nanoxml.sax
{


    using AttributeListImpl = org.xml.sax.helpers.AttributeListImpl;
    using Parser = org.xml.sax.Parser;
    using DocumentHandler = org.xml.sax.DocumentHandler;
    using DTDHandler = org.xml.sax.DTDHandler;
    using EntityResolver = org.xml.sax.EntityResolver;
    using ErrorHandler = org.xml.sax.ErrorHandler;
    using HandlerBase = org.xml.sax.HandlerBase;
    using InputSource = org.xml.sax.InputSource;
    using SAXException = org.xml.sax.SAXException;
    using SAXParseException = org.xml.sax.SAXParseException;
    using System.Collections.Generic;

    /// <summary>
    /// This is the SAX adapter for NanoXML. Note that this adapter is provided
    /// to make NanoXML "buzzword compliant". If you're not stuck with SAX
    /// compatibility, you should use the basic API (nanoxml.NanoXML) which is
    /// much more programmer-friendly as it doesn't require the cumbersome use
    /// of event handlers and has more powerful attribute-handling methods, but
    /// that is just IMHO. If you really want to use the SAX API, I would like you
    /// to point to the currently available native SAX parsers.
    /// <P>
    /// Here are some important notes:
    /// <UL><LI>The parser is non-validating.
    ///     <LI>The DTD is fully ignored, including <CODE>&lt;!ENTITY...&gt;</CODE>.
    ///     <LI>SAXParser is reentrant.
    ///     <LI>There is support for a document locator.
    ///     <LI>There is no support for mixed content (elements containing both
    ///         subelements and CDATA elements)
    ///     <LI>There are no ignorableWhitespace() events
    ///     <LI>Attribute types are always reported as CDATA
    /// </UL>
    /// <P>
    /// $Revision: 2 $<BR>
    /// $Date: 3/25/04 6:24p $<P>
    /// </summary>
    /// <seealso cref= nanoxml.sax.SAXLocator </seealso>
    /// <seealso cref= nanoxml.XMLElement
    /// 
    /// @author Marc De Scheemaecker
    ///         &lt;<A HREF="mailto:Marc.DeScheemaecker@advalvas.be"
    ///         >Marc.DeScheemaecker@advalvas.be</A>&gt;
    /// @version 1.6 </seealso>
    public class SAXParser : Parser
	{

	   /// <summary>
	   /// The associated document handler.
	   /// </summary>
	   private DocumentHandler documentHandler;


	   /// <summary>
	   /// The associated error handler.
	   /// </summary>
	   private ErrorHandler errorHandler;


	   /// <summary>
	   /// Initializes the SAX parser adapter.
	   /// </summary>
	   public SAXParser()
	   {
		  this.documentHandler = new HandlerBase();
		  this.errorHandler = new HandlerBase();
	   }


	   /// <summary>
	   /// Sets the locale. Only locales using the language english are accepted.
	   /// </summary>
	   /// <exception cref="org.xml.sax.SAXException">
	   ///    if <CODE>locale</CODE> is <CODE>null</CODE> or the associated
	   ///    language is not english. </exception>
//TODO
	   //public virtual Locale Locale
	   //{
		  // set
		  // {
			 // if ((value == null) || (!value.Language.Equals("en")))
			 // {
				//	throw new SAXException("NanoXML/SAX doesn't support locale: " + value);
			 // }
		  // }
	   //}


	   /// <summary>
	   /// Sets the entity resolver. As the DTD is ignored, this resolver is never
	   /// called.
	   /// </summary>
	   public virtual EntityResolver EntityResolver
	   {
		   set
		   {
			  // nothing to do
		   }
	   }


	   /// <summary>
	   /// Sets the DTD handler. As the DTD is ignored, this handler is never
	   /// called.
	   /// </summary>
	   public virtual DTDHandler DTDHandler
	   {
		   set
		   {
			  // nothing to do
		   }
	   }


	   /// <summary>
	   /// Allows an application to register a document event handler.
	   /// </summary>
	   public virtual DocumentHandler DocumentHandler
	   {
		   set
		   {
			  this.documentHandler = value;
		   }
	   }


	   /// <summary>
	   /// Allows an applicaiton to register an error event handler.
	   /// </summary>
	   public virtual ErrorHandler ErrorHandler
	   {
		   set
		   {
			  this.errorHandler = value;
		   }
	   }


	   /// <summary>
	   /// Handles a subtree of the parsed XML data structure.
	   /// </summary>
	   /// <exception cref="org.xml.sax.SAXException">
	   ///     if one of the handlers throw such exception </exception>
	   private void handleSubTree(XMLElement element, SAXLocator locator)
	   {
		  AttributeListImpl attrList = new AttributeListImpl();
		  locator.LineNr = element.LineNr;
		  IEnumerator l_enum = element.enumeratePropertyNames();

		  while (l_enum.MoveNext())
		  {
				string key = ((KeyValuePair<string, string>)l_enum.Current).Key;
				string value = element.getProperty(key);
				attrList.addAttribute(key, "CDATA", value);
		  }

		  this.documentHandler.startElement(element.TagName, attrList);

		  if (string.ReferenceEquals(element.Contents, null))
		  {
				l_enum = element.enumerateChildren();

				while (l_enum.MoveNext())
				{
					  this.handleSubTree((XMLElement)(l_enum.Current), locator);
				}
		  }
		  else
		  {
				char[] chars = element.Contents.ToCharArray();
				this.documentHandler.characters(chars, 0, chars.Length);
		  }

		  locator.LineNr = -1;
		  this.documentHandler.endElement(element.TagName);
	   }


	   /// <summary>
	   /// Creates the top XML element.
	   /// Override this method if you need a different parsing behaviour.<P>
	   /// The default behaviour is:
	   /// <UL><LI>Case insensitive tag and attribute names, names converted to
	   ///         uppercase
	   ///     <LI>The only initial entities are amp, lt, gt, apos and quot.
	   ///     <LI>Skip formatting whitespace in PCDATA elements.
	   /// </UL>
	   /// </summary>
	   protected internal virtual XMLElement createTopElement()
	   {
		  return new XMLElement();
	   }


	   /// <summary>
	   /// Parses an XML document.
	   /// </summary>
	   /// <exception cref="org.xml.sax.SAXException">
	   ///    if one of the handlers throws such exception </exception>
	   /// <exception cref="System.IO.IOException">
	   ///    if an I/O exception occured while trying to read the document </exception>
	   public virtual void parse(InputSource source)
	   {
		  XMLElement topElement = this.createTopElement();
		  TextReader reader = source.CharacterStream;
		  SAXLocator locator = new SAXLocator(source.SystemId);
		  this.documentHandler.DocumentLocator = locator;


		  if (reader == null)
		  {
				Stream stream = source.ByteStream;

                System.Text.Encoding encoding = source.Encoding;

				if (stream == null)
				{
					  string systemId = source.SystemId;

					  if (string.ReferenceEquals(systemId, null))
					  {
							SAXParseException saxException = new SAXParseException("Invalid input source", locator);
							this.errorHandler.fatalError(saxException);
							return;
					  }

					  try
					  {
                            Uri url = new Uri(systemId);
//TODO							stream = url.openStream();
					  }
					  catch (Exception)
//					  catch (MalformedURLException)
					  {
							try
							{
								  stream = new FileStream(systemId, FileMode.Open, FileAccess.Read);
							}
							catch (FileNotFoundException exception2)
							{
								  SAXParseException saxException = new SAXParseException(null, locator, exception2);
								  this.errorHandler.fatalError(saxException);
								  return;
							}
							//catch (SecurityException exception2)
							//{
							//	  SAXParseException saxException = new SAXParseException(null, locator, exception2);
							//	  this.errorHandler.fatalError(saxException);
							//	  return;
							//}
					  }
				}

				if (string.ReferenceEquals(encoding, null))
				{
					  reader = new StreamReader(stream);
				}
				else
				{
					  try
					  {
							reader = new StreamReader(stream, encoding);
					  }
                      catch(ArgumentException exception)
					  //TODO catch (UnsupportedEncodingException exception)
					  {
							SAXParseException saxException = new SAXParseException(null, locator, exception);
							this.errorHandler.fatalError(saxException);
							return;
					  }
				}
		  }

		  try
		  {
				topElement.parseFromReader(reader);
		  }
		  catch (XMLParseException exception)
		  {
				locator.LineNr = exception.LineNr;
				SAXParseException saxException = new SAXParseException(null, locator, exception);
				this.errorHandler.fatalError(saxException);
				this.documentHandler.endDocument();
				return;
		  }

		  locator.LineNr = topElement.LineNr;
		  this.documentHandler.startDocument();
		  this.handleSubTree(topElement, locator);
		  this.documentHandler.endDocument();
	   }


	   /// <summary>
	   /// Parses an XML document from a system identifier (URI).
	   /// </summary>
	   /// <exception cref="org.xml.sax.SAXException">
	   ///    if one of the handlers throws such exception </exception>
	   /// <exception cref="System.IO.IOException">
	   ///    if an I/O exception occured while trying to read the document </exception>
	   public virtual void parse(string systemId)
	   {
		  this.parse(new InputSource(systemId));
	   }


	}

}