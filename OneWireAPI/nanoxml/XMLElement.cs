using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using com.dalsemi.onewire;

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


namespace nanoxml
{
    /// <summary>
    /// XMLElement is a representation of an XML object. The object is able to parse
    /// XML code.
    /// <P>
    /// Note that NanoXML is not 100% XML 1.0 compliant:
    /// <UL><LI>The parser is non-validating.
    ///     <LI>The DTD is fully ignored, including <CODE>&lt;!ENTITY...&gt;</CODE>.
    ///     <LI>There is no support for mixed content (elements containing both
    ///         subelements and CDATA elements)
    /// </UL>
    /// <P>
    /// You can opt to use a SAX compatible API, by including both
    /// <CODE>nanoxml.jar</CODE> and <CODE>nanoxml-sax.jar</CODE> in your classpath
    /// and setting the property <CODE>org.xml.sax.parser</CODE> to
    /// <CODE>nanoxml.sax.SAXParser</CODE>
    /// <P>
    /// $Revision: 2 $<BR>
    /// $Date: 3/25/04 6:24p $<P>
    /// </summary>
    /// <seealso cref= nanoxml.XMLParseException
    /// 
    /// @author Marc De Scheemaecker
    ///         &lt;<A HREF="mailto:Marc.DeScheemaecker@advalvas.be"
    ///         >Marc.DeScheemaecker@advalvas.be</A>&gt;
    /// @version 1.6 </seealso>
//TODO    [Serializable]
    public class XMLElement
	{

	   /// <summary>
	   /// Serialization serial version ID.
	   /// </summary>
	   internal const long serialVersionUID = 6685035139346394777L;


	   /// <summary>
	   /// Major version of NanoXML.
	   /// </summary>
	   public const int NANOXML_MAJOR_VERSION = 1;


	   /// <summary>
	   /// Minor version of NanoXML.
	   /// </summary>
	   public const int NANOXML_MINOR_VERSION = 6;


	   /// <summary>
	   /// The attributes given to the object.
	   /// </summary>
	   private Properties attributes;


	   /// <summary>
	   /// Subobjects of the object. The subobjects are of class XMLElement
	   /// themselves.
	   /// </summary>
	   private List<XMLElement> children;


	   /// <summary>
	   /// The class of the object (the name indicated in the tag).
	   /// </summary>
	   private string tagName;


	   /// <summary>
	   /// The #PCDATA content of the object. If there is no such content, this
	   /// field is null.
	   /// </summary>
	   private string contents;


	   /// <summary>
	   /// Conversion table for &amp;...; tags.
	   /// </summary>
	   private Properties conversionTable;


	   /// <summary>
	   /// Whether to skip leading whitespace in CDATA.
	   /// </summary>
	   private bool skipLeadingWhitespace;


	   /// <summary>
	   /// The line number where the element starts.
	   /// </summary>
	   private int lineNr;


	   /// <summary>
	   /// Whether the parsing is case sensitive.
	   /// </summary>
	   private bool ignoreCase;


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD>Minimal XML conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE></DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><CODE>false</CODE></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><CODE>true</CODE></DD>
	   /// </DL>
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(bool) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties,bool) </seealso>
	   public XMLElement() : this(new Properties(), false, true, true)
	   {
	   }


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD><I>conversionTable</I> combined with the minimal XML
	   ///         conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE></DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><CODE>false</CODE></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><CODE>true</CODE></DD>
	   /// </DL>
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement() </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(bool) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties,bool) </seealso>
	   public XMLElement(Properties conversionTable) : this(conversionTable, false, true, true)
	   {
	   }


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD>Minimal XML conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE></DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><I>skipLeadingWhitespace</I></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><CODE>true</CODE></DD>
	   /// </DL>
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement() </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties,bool) </seealso>
	   public XMLElement(bool skipLeadingWhitespace) : this(new Properties(), skipLeadingWhitespace, true, true)
	   {
	   }


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD><I>conversionTable</I> combined with the minimal XML
	   ///         conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE></DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><I>skipLeadingWhitespace</I></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><CODE>true</CODE></DD>
	   /// </DL>
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement() </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(bool) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties) </seealso>
	   public XMLElement(Properties conversionTable, bool skipLeadingWhitespace) : this(conversionTable, skipLeadingWhitespace, true, true)
	   {
	   }


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD><I>conversionTable</I>, eventually combined with the minimal XML
	   ///         conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE>
	   ///         (depending on <I>fillBasicConversionTable</I>)</DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><I>skipLeadingWhitespace</I></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><I>ignoreCase</I></DD>
	   /// </DL>
	   /// <P>
	   /// This constructor should <I>only</I> be called from XMLElement itself
	   /// to create child elements.
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement() </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(bool) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties,bool) </seealso>
	   public XMLElement(Properties conversionTable, bool skipLeadingWhitespace, bool ignoreCase) : this(conversionTable, skipLeadingWhitespace, true, ignoreCase)
	   {
	   }


	   /// <summary>
	   /// Creates a new XML element. The following settings are used:
	   /// <DL><DT>Conversion table</DT>
	   ///     <DD><I>conversionTable</I>, eventually combined with the minimal XML
	   ///         conversions: <CODE>&amp;amp; &amp;lt; &amp;gt;
	   ///         &amp;apos; &amp;quot;</CODE>
	   ///         (depending on <I>fillBasicConversionTable</I>)</DD>
	   ///     <DT>Skip whitespace in contents</DT>
	   ///     <DD><I>skipLeadingWhitespace</I></DD>
	   ///     <DT>Ignore Case</DT>
	   ///     <DD><I>ignoreCase</I></DD>
	   /// </DL>
	   /// <P>
	   /// This constructor should <I>only</I> be called from XMLElement itself
	   /// to create child elements.
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement() </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(bool) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties) </seealso>
	   /// <seealso cref= nanoxml.XMLElement#XMLElement(java.util.Properties,bool) </seealso>
	   protected internal XMLElement(Properties conversionTable, bool skipLeadingWhitespace, bool fillBasicConversionTable, bool ignoreCase)
	   {
		  this.ignoreCase = ignoreCase;
		  this.skipLeadingWhitespace = skipLeadingWhitespace;
		  this.tagName = null;
		  this.contents = "";
		  this.attributes = new Properties();
		  this.children = new List<XMLElement>();
		  this.conversionTable = conversionTable;
		  this.lineNr = 0;

		  if (fillBasicConversionTable)
		  {
				this.conversionTable.put("lt", "<");
				this.conversionTable.put("gt", ">");
				this.conversionTable.put("quot", "\"");
				this.conversionTable.put("apos", "'");
				this.conversionTable.put("amp", "&");
		  }
	   }


	   /// <summary>
	   /// Adds a subobject.
	   /// </summary>
	   public virtual void addChild(XMLElement child)
	   {
		  this.children.Add(child);
	   }


	   /// <summary>
	   /// Adds a property.
	   /// If the element is case insensitive, the property name is capitalized.
	   /// </summary>
	   public virtual void addProperty(string key, object value)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  this.attributes.put(key, value.ToString());
	   }


	   /// <summary>
	   /// Adds a property.
	   /// If the element is case insensitive, the property name is capitalized.
	   /// </summary>
	   public virtual void addProperty(string key, int value)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  this.attributes.put(key, Convert.ToString(value));
	   }


	   /// <summary>
	   /// Adds a property.
	   /// If the element is case insensitive, the property name is capitalized.
	   /// </summary>
	   public virtual void addProperty(string key, double value)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  this.attributes.put(key, Convert.ToString(value));
	   }


	   /// <summary>
	   /// Returns the number of subobjects of the object.
	   /// </summary>
	   public virtual int countChildren()
	   {
		  return this.children.Count;
	   }


	   /// <summary>
	   /// Enumerates the attribute names.
	   /// </summary>
	   public virtual System.Collections.IEnumerator enumeratePropertyNames()
	   {
		  return this.attributes.keys();
	   }


	   /// <summary>
	   /// Enumerates the subobjects of the object.
	   /// </summary>
	   public virtual System.Collections.IEnumerator enumerateChildren()
	   {
		  return this.children.GetEnumerator();
	   }


	   /// <summary>
	   /// Returns the subobjects of the object.
	   /// </summary>
	   public virtual List<XMLElement> Children
	   {
		   get
		   {
			  return this.children;
		   }
	   }


	   /// <summary>
	   /// Returns the #PCDATA content of the object. If there is no such content,
	   /// <CODE>null</CODE> is returned.
	   /// </summary>
	   public virtual string Contents
	   {
		   get
		   {
			  return this.contents;
		   }
	   }


	   /// <summary>
	   /// Returns the line nr on which the element is found.
	   /// </summary>
	   public virtual int LineNr
	   {
		   get
		   {
			  return this.lineNr;
		   }
	   }


	   /// <summary>
	   /// Returns a property by looking up a key in a hashtable.
	   /// If the property doesn't exist, the value corresponding to defaultValue
	   /// is returned.
	   /// </summary>
	   public virtual int getIntProperty(string key, Hashtable valueSet, string defaultValue)
	   {
		  string val = this.attributes.getProperty(key);
		  int? result;

		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  if (string.ReferenceEquals(val, null))
		  {
				val = defaultValue;
		  }

		  try
		  {
				result = (int?)(valueSet[val]);
		  }
		  catch (System.InvalidCastException)
		  {
				throw this.invalidValueSet(key);
		  }

		  if (result == null)
		  {
				throw this.invalidValue(key, val, this.lineNr);
		  }

		  return result.Value;
	   }


	   /// <summary>
	   /// Returns a property of the object. If there is no such property, this
	   /// method returns <CODE>null</CODE>.
	   /// </summary>
	   public virtual string getProperty(string key)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  return this.attributes.getProperty(key);
	   }


	   /// <summary>
	   /// Returns a property of the object.
	   /// If the property doesn't exist, <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual string getProperty(string key, string defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  return this.attributes.getProperty(key, defaultValue);
	   }


	   /// <summary>
	   /// Returns an integer property of the object.
	   /// If the property doesn't exist, <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual int getProperty(string key, int defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);

		  if (string.ReferenceEquals(val, null))
		  {
				return defaultValue;
		  }
		  else
		  {
				try
				{
					  return int.Parse(val);
				}
				catch (System.FormatException)
				{
					  throw this.invalidValue(key, val, this.lineNr);
				}
		  }
	   }


	   /// <summary>
	   /// Returns a floating point property of the object.
	   /// If the property doesn't exist, <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual double getProperty(string key, double defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);

		  if (string.ReferenceEquals(val, null))
		  {
				return defaultValue;
		  }
		  else
		  {
				try
				{
					  return Convert.ToDouble(val);
				}
				catch (System.FormatException)
				{
					  throw this.invalidValue(key, val, this.lineNr);
				}
		  }
	   }


	   /// <summary>
	   /// Returns a bool property of the object. If the property is missing,
	   /// <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual bool getProperty(string key, string trueValue, string falseValue, bool defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);

		  if (string.ReferenceEquals(val, null))
		  {
				return defaultValue;
		  }
		  else if (val.Equals(trueValue))
		  {
				return true;
		  }
		  else if (val.Equals(falseValue))
		  {
				return false;
		  }
		  else
		  {
				throw this.invalidValue(key, val, this.lineNr);
		  }
	   }


	   /// <summary>
	   /// Returns a property by looking up a key in the hashtable <I>valueSet</I>
	   /// If the property doesn't exist, the value corresponding to
	   /// <I>defaultValue</I>  is returned.
	   /// </summary>
	   public virtual object getProperty(string key, Hashtable valueSet, string defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);

		  if (string.ReferenceEquals(val, null))
		  {
				val = defaultValue;
		  }

		  object result = valueSet[val];

		  if (result == null)
		  {
				throw this.invalidValue(key, val, this.lineNr);
		  }

		  return result;
	   }


	   /// <summary>
	   /// Returns a property by looking up a key in the hashtable <I>valueSet</I>.
	   /// If the property doesn't exist, the value corresponding to
	   /// <I>defaultValue</I>  is returned.
	   /// </summary>
	   public virtual string getStringProperty(string key, Hashtable valueSet, string defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);
		  string result;

		  if (string.ReferenceEquals(val, null))
		  {
				val = defaultValue;
		  }

		  try
		  {
				result = (string)(valueSet[val]);
		  }
		  catch (System.InvalidCastException)
		  {
				throw this.invalidValueSet(key);
		  }

		  if (string.ReferenceEquals(result, null))
		  {
				throw this.invalidValue(key, val, this.lineNr);
		  }

		  return result;
	   }


	   /// <summary>
	   /// Returns a property by looking up a key in the hashtable <I>valueSet</I>.
	   /// If the value is not defined in the hashtable, the value is considered to
	   /// be an integer.
	   /// If the property doesn't exist, the value corresponding to
	   /// <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual int getSpecialIntProperty(string key, Hashtable valueSet, string defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);
		  int? result;

		  if (string.ReferenceEquals(val, null))
		  {
				val = defaultValue;
		  }

		  try
		  {
				result = (int?)(valueSet[val]);
		  }
		  catch (System.InvalidCastException)
		  {
				throw this.invalidValueSet(key);
		  }

		  if (result == null)
		  {
				try
				{
					  return int.Parse(val);
				}
				catch (System.FormatException)
				{
					  throw this.invalidValue(key, val, this.lineNr);
				}
		  }

		  return result.Value;
	   }


	   /// <summary>
	   /// Returns a property by looking up a key in the hashtable <I>valueSet</I>.
	   /// If the value is not defined in the hashtable, the value is considered to
	   /// be a floating point number.
	   /// If the property doesn't exist, the value corresponding to
	   /// <I>defaultValue</I> is returned.
	   /// </summary>
	   public virtual double getSpecialDoubleProperty(string key, Hashtable valueSet, string defaultValue)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  string val = this.attributes.getProperty(key);
		  double? result;

		  if (string.ReferenceEquals(val, null))
		  {
				val = defaultValue;
		  }

		  try
		  {
				result = (double?)(valueSet[val]);
		  }
		  catch (System.InvalidCastException)
		  {
				throw this.invalidValueSet(key);
		  }

		  if (result == null)
		  {
				try
				{
					  result = Convert.ToDouble(val);
				}
				catch (System.FormatException)
				{
					  throw this.invalidValue(key, val, this.lineNr);
				}
		  }

		  return result.Value;
	   }


	   /// <summary>
	   /// Returns the class (i.e. the name indicated in the tag) of the object.
	   /// </summary>
	   public virtual string TagName
	   {
		   get
		   {
			  return this.tagName;
		   }
		   set
		   {
			  this.tagName = value;
		   }
	   }


	   /// <summary>
	   /// Checks whether a character may be part of an identifier.
	   /// </summary>
	   private bool isIdentifierChar(char ch)
	   {
		  return (((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')) || ((ch >= '0') && (ch <= '9')) || (".-_:".IndexOf(ch) >= 0));
	   }


	   /// <summary>
	   /// Reads an XML definition from a java.io.Reader and parses it.
	   /// </summary>
	   /// <exception cref="System.IO.IOException">
	   ///    if an error occured while reading the input </exception>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the read data </exception>
	   public virtual void parseFromReader(TextReader reader)
	   {
		  this.parseFromReader(reader, 1);
	   }


	   /// <summary>
	   /// Reads an XML definition from a java.io.Reader and parses it.
	   /// </summary>
	   /// <exception cref="System.IO.IOException">
	   ///    if an error occured while reading the input </exception>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the read data </exception>
	   public virtual void parseFromReader(TextReader reader, int startingLineNr)
	   {
		  int blockSize = 4096;
		  char[] input = null;
		  int size = 0;

		  for (;;)
		  {
				if (input == null)
				{
					  input = new char[blockSize];
				}
				else
				{
					  char[] oldInput = input;
					  input = new char[input.Length + blockSize];
					  Array.Copy(oldInput, 0, input, 0, oldInput.Length);
				}

				int charsRead = reader.Read(input, size, blockSize);

				if (charsRead < 0)
				{
					  break;
				}

				size += charsRead;
		  }

		  this.parseCharArray(input, 0, size, startingLineNr);
	   }


	   /// <summary>
	   /// Parses an XML definition.
	   /// </summary>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the string </exception>
	   public virtual void parseString(string @string)
	   {
		  this.parseCharArray(@string.ToCharArray(), 0, @string.Length, 1);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the string following the XML data
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the string </exception>
	   public virtual int parseString(string @string, int offset)
	   {
		  return this.parseCharArray(@string.ToCharArray(), offset, @string.Length, 1);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the string following the XML data (<= end)
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the string </exception>
	   public virtual int parseString(string @string, int offset, int end)
	   {
		  return this.parseCharArray(@string.ToCharArray(), offset, end, 1);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the string following the XML data (<= end)
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the string </exception>
	   public virtual int parseString(string @string, int offset, int end, int startingLineNr)
	   {
		  return this.parseCharArray(@string.ToCharArray(), offset, end, startingLineNr);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the array following the XML data (<= end)
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   public virtual int parseCharArray(char[] input, int offset, int end)
	   {
		  return this.parseCharArray(input, offset, end, 1);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the array following the XML data (<= end)
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   public virtual int parseCharArray(char[] input, int offset, int end, int startingLineNr)
	   {
		  int[] lineNr = new int[1];
		  lineNr[0] = startingLineNr;
		  return this.parseCharArray(input, offset, end, lineNr);
	   }


	   /// <summary>
	   /// Parses an XML definition starting at <I>offset</I>.
	   /// </summary>
	   /// <returns> the offset of the array following the XML data (<= end)
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int parseCharArray(char[] input, int offset, int end, int[] currentLineNr)
	   {
		  this.lineNr = currentLineNr[0];
		  this.tagName = null;
		  this.contents = null;
		  this.attributes = new Properties();
		  this.children = new List<XMLElement>();

		  try
		  {
				offset = this.skipWhitespace(input, offset, end, currentLineNr);
		  }
		  catch (XMLParseException)
		  {
				return offset;
		  }

		  offset = this.skipPreamble(input, offset, end, currentLineNr);
		  offset = this.scanTagName(input, offset, end, currentLineNr);
		  this.lineNr = currentLineNr[0];
		  offset = this.scanAttributes(input, offset, end, currentLineNr);
		  int[] contentOffset = new int[1];
		  int[] contentSize = new int[1];
		  int contentLineNr = currentLineNr[0];
		  offset = this.scanContent(input, offset, end, contentOffset, contentSize, currentLineNr);

		  if (contentSize[0] > 0)
		  {
				this.scanChildren(input, contentOffset[0], contentSize[0], contentLineNr);

				if (this.children.Count > 0)
				{
					  this.contents = null;
				}
				else
				{
					  this.processContents(input, contentOffset[0], contentSize[0], contentLineNr);

					  for (int i = 0; i < this.contents.Length; i++)
					  {
						 if (this.contents[i] > ' ')
						 {
							return offset;
						 }
					  }

					  this.contents = null;
				}
		  }

		  return offset;
	   }


	   /// <summary>
	   /// Decodes the entities in the contents and, if skipLeadingWhitespace is
	   /// <CODE>true</CODE>, removes extraneous whitespaces after newlines and
	   /// convert those newlines into spaces.
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#decodeString
	   /// </seealso>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private void processContents(char[] input, int contentOffset, int contentSize, int contentLineNr)
	   {
		  int[] lineNr = new int[1];
		  lineNr[0] = contentLineNr;

		  if (!this.skipLeadingWhitespace)
		  {
				string str = new string(input, contentOffset, contentSize);
				this.contents = this.decodeString(str, lineNr[0]);
				return;
		  }

		  StringBuilder result = new StringBuilder(contentSize);
		  int end = contentSize + contentOffset;

		  for (int i = contentOffset; i < end; i++)
		  {
				char ch = input[i];

				// The end of the contents is always a < character, so there's
				// no danger for bounds violation
				while ((ch == '\r') || (ch == '\n'))
				{
					  lineNr[0]++;
					  result.Append(ch);

					  i++;
					  ch = input[i];

					  if (ch != '\n')
					  {
							result.Append(ch);
					  }

					  do
					  {
							i++;
							ch = input[i];
					  } while ((ch == ' ') || (ch == '\t'));
				}

				if (i < end)
				{
					  result.Append(input[i]);
				}
		  }

		  this.contents = this.decodeString(result.ToString(), lineNr[0]);
	   }


	   /// <summary>
	   /// Removes a child object. If the object is not a child, nothing happens.
	   /// </summary>
	   public virtual void removeChild(XMLElement child)
	   {
		  this.children.Remove(child);
	   }


	   /// <summary>
	   /// Removes an attribute.
	   /// </summary>
	   public virtual void removeChild(string key)
	   {
		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  this.attributes.remove(key);
	   }


	   /// <summary>
	   /// Scans the attributes of the object.
	   /// </summary>
	   /// <returns> the offset in the string following the attributes, so that
	   ///         input[offset] in { '/', '>' }
	   /// </returns>
	   /// <seealso cref= nanoxml.XMLElement#scanOneAttribute
	   /// </seealso>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int scanAttributes(char[] input, int offset, int end, int[] lineNr)
	   {
		  for (;;)
		  {
				offset = this.skipWhitespace(input, offset, end, lineNr);

				char ch = input[offset];

				if ((ch == '/') || (ch == '>'))
				{
					  break;
				}

				offset = this.scanOneAttribute(input, offset, end, lineNr);
		  }

		  return offset;
	   }


	   /// <summary>
	   ///!!!
	   /// Searches the content for child objects. If such objects exist, the
	   /// content is reduced to <CODE>null</CODE>.
	   /// </summary>
	   /// <seealso cref= nanoxml.XMLElement#parseCharArray
	   /// </seealso>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   protected internal virtual void scanChildren(char[] input, int contentOffset, int contentSize, int contentLineNr)
	   {
		  int end = contentOffset + contentSize;
		  int offset = contentOffset;
		  int[] lineNr = new int[1];
		  lineNr[0] = contentLineNr;

		  while (offset < end)
		  {
				try
				{
					  offset = this.skipWhitespace(input, offset, end, lineNr);
				}
				catch (XMLParseException)
				{
					  return;
				}

				if ((input[offset] != '<') || ((input[offset + 1] == '!') && (input[offset + 2] == '[')))
				{
					  return;
				}

				XMLElement child = this.createAnotherElement();
				offset = child.parseCharArray(input, offset, end, lineNr);
				this.children.Add(child);
		  }
	   }


	   /// <summary>
	   /// Creates a new XML element.
	   /// </summary>
	   protected internal virtual XMLElement createAnotherElement()
	   {
		  return new XMLElement(this.conversionTable, this.skipLeadingWhitespace, false, this.ignoreCase);
	   }


	   /// <summary>
	   /// Scans the content of the object.
	   /// </summary>
	   /// <returns> the offset after the XML element; contentOffset points to the
	   ///         start of the content section; contentSize is the size of the
	   ///         content section
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int scanContent(char[] input, int offset, int end, int[] contentOffset, int[] contentSize, int[] lineNr)
	   {
		  if (input[offset] == '/')
		  {
				contentSize[0] = 0;

				if (input[offset + 1] != '>')
				{
					  throw this.expectedInput("'>'", lineNr[0]);
				}

				return offset + 2;
		  }

		  if (input[offset] != '>')
		  {
				throw this.expectedInput("'>'", lineNr[0]);
		  }

		  if (this.skipLeadingWhitespace)
		  {
				offset = this.skipWhitespace(input, offset + 1, end, lineNr);
		  }
		  else
		  {
				offset++;
		  }

		  contentOffset[0] = offset;
		  int level = 0;
		  char[] tag = this.tagName.ToCharArray();
		  end -= (tag.Length + 2);

		  while ((offset < end) && (level >= 0))
		  {
				if (input[offset] == '<')
				{
					  bool ok = true;

					  if ((offset < (end - 3)) && (input[offset + 1] == '!') && (input[offset + 2] == '-') && (input[offset + 3] == '-'))
					  {
							 offset += 3;

							 while ((offset < end) && ((input[offset - 2] != '-') || (input[offset - 1] != '-') || (input[offset - 0] != '>')))
							 {
									offset++;
							 }

							 offset++;
							 continue;
					  }

					  if ((offset < (end - 1)) && (input[offset + 1] == '!') && (input[offset + 2] == '['))
					  {
							offset++;
							continue;
					  }

					  for (int i = 0; ok && (i < tag.Length); i++)
					  {
							ok &= (input[offset + (i + 1)] == tag[i]);
					  }

					  ok &= !this.isIdentifierChar(input[offset + tag.Length + 1]);

					  if (ok)
					  {
							while ((offset < end) && (input[offset] != '>'))
							{
								  offset++;
							}

							if (input[offset - 1] != '/')
							{
								  level++;
							}

							continue;
					  }
					  else if (input[offset + 1] == '/')
					  {
							ok = true;

							for (int i = 0; ok && (i < tag.Length); i++)
							{
								  ok &= (input[offset + (i + 2)] == tag[i]);
							}

							if (ok)
							{
								  contentSize[0] = offset - contentOffset[0];
								  offset += tag.Length + 2;

								  try
								  {
									 offset = this.skipWhitespace(input, offset, end + tag.Length + 2, lineNr);
								  }
								  catch (XMLParseException)
								  {
									 // ignore
								  }

								  if (input[offset] == '>')
								  {
										level--;
										offset++;
								  }

								  continue;
							}
					  }
				}

				if (input[offset] == '\r')
				{
					  lineNr[0]++;

					  if ((offset != end) && (input[offset + 1] == '\n'))
					  {
							offset++;
					  }
				}
				else if (input[offset] == '\n')
				{
					  lineNr[0]++;
				}

				offset++;
		  }

		  if (level >= 0)
		  {
				throw this.unexpectedEndOfData(lineNr[0]);
		  }

		  if (this.skipLeadingWhitespace)
		  {
				int i = contentOffset[0] + contentSize[0] - 1;

				while ((contentSize[0] >= 0) && (input[i] <= ' '))
				{
					  i--;
					  contentSize[0]--;
				}
		  }

		  return offset;
	   }


	   /// <summary>
	   /// Scans an identifier.
	   /// </summary>
	   /// <returns> the identifier, or <CODE>null</CODE> if offset doesn't point
	   ///         to an identifier </returns>
	   private string scanIdentifier(char[] input, int offset, int end)
	   {
		  int begin = offset;

		  while ((offset < end) && (this.isIdentifierChar(input[offset])))
		  {
				offset++;
		  }

		  if ((offset == end) || (offset == begin))
		  {
				return null;
		  }
		  else
		  {
				return new string(input, begin, offset - begin);
		  }
	   }


	   /// <summary>
	   /// Scans one attribute of an object.
	   /// </summary>
	   /// <returns> the offset after the attribute
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int scanOneAttribute(char[] input, int offset, int end, int[] lineNr)
	   {
		  string key, value;

		  key = this.scanIdentifier(input, offset, end);

		  if (string.ReferenceEquals(key, null))
		  {
				throw this.syntaxError("an attribute key", lineNr[0]);
		  }

		  offset = this.skipWhitespace(input, offset + key.Length, end, lineNr);

		  if (this.ignoreCase)
		  {
				key = key.ToUpper();
		  }

		  if (input[offset] != '=')
		  {
				throw this.valueMissingForAttribute(key, lineNr[0]);
		  }

		  offset = this.skipWhitespace(input, offset + 1, end, lineNr);

		  value = this.scanString(input, offset, end, lineNr);

		  if (string.ReferenceEquals(value, null))
		  {
				throw this.syntaxError("an attribute value", lineNr[0]);
		  }

		  if ((value[0] == '"') || (value[0] == '\''))
		  {
				value = value.Substring(1, (value.Length - 1) - 1);
				offset += 2;
		  }

		  this.attributes.put(key, this.decodeString(value, lineNr[0]));
		  return offset + value.Length;
	   }


	   /// <summary>
	   /// Scans a string. Strings are either identifiers, or text delimited by
	   /// double quotes.
	   /// </summary>
	   /// <returns> the string found, without delimiting double quotes; or null
	   ///         if offset didn't point to a valid string
	   /// </returns>
	   /// <seealso cref= nanoxml.XMLElement#scanIdentifier
	   /// </seealso>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private string scanString(char[] input, int offset, int end, int[] lineNr)
	   {
		  char delim = input[offset];

		  if ((delim == '"') || (delim == '\''))
		  {
				int begin = offset;
				offset++;

				while ((offset < end) && (input[offset] != delim))
				{
					  if (input[offset] == '\r')
					  {
							lineNr[0]++;

							if ((offset != end) && (input[offset + 1] == '\n'))
							{
								  offset++;
							}
					  }
					  else if (input[offset] == '\n')
					  {
							lineNr[0]++;
					  }

					  offset++;
				}

				if (offset == end)
				{
					  return null;
				}
				else
				{
					  return new string(input, begin, offset - begin + 1);
				}
		  }
		  else
		  {
				return this.scanIdentifier(input, offset, end);
		  }
	   }


	   /// <summary>
	   /// Scans the class (tag) name of the object.
	   /// </summary>
	   /// <returns> the position after the tag name
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int scanTagName(char[] input, int offset, int end, int[] lineNr)
	   {
		  this.tagName = this.scanIdentifier(input, offset, end);

		  if (string.ReferenceEquals(this.tagName, null))
		  {
				throw this.syntaxError("a tag name", lineNr[0]);
		  }

		  return offset + this.tagName.Length;
	   }


	   /// <summary>
	   /// Changes the content string.
	   /// </summary>
	   /// <param name="content"> The new content string. </param>
	   public virtual string Content
	   {
		   set
		   {
			  this.contents = value;
		   }
	   }




	   /// <summary>
	   /// Skips a tag that don't contain any useful data: &lt;?...?&gt;,
	   /// &lt;!...&gt; and comments.
	   /// </summary>
	   /// <returns> the position after the tag
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   protected internal virtual int skipBogusTag(char[] input, int offset, int end, int[] lineNr)
	   {
		  int level = 1;

		  while (offset < end)
		  {
				char ch = input[offset++];

				switch (ch)
				{
					  case '\r':
						 if ((offset < end) && (input[offset] == '\n'))
						 {
							   offset++;
						 }

						 lineNr[0]++;
						 break;

					  case '\n':
						 lineNr[0]++;
						 break;

					  case '<':
						 level++;
						 break;

					  case '>':
						 level--;

						 if (level == 0)
						 {
							return offset;
						 }

						 break;

					  default:
				  break;
				}
		  }

		  throw this.unexpectedEndOfData(lineNr[0]);
	   }


	   /// <summary>
	   /// Skips a tag that don't contain any useful data: &lt;?...?&gt;,
	   /// &lt;!...&gt; and comments.
	   /// </summary>
	   /// <returns> the position after the tag
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int skipPreamble(char[] input, int offset, int end, int[] lineNr)
	   {
		  char ch;

		  do
		  {
				offset = this.skipWhitespace(input, offset, end, lineNr);

				if (input[offset] != '<')
				{
					  this.expectedInput("'<'", lineNr[0]);
				}

				offset++;

				if (offset >= end)
				{
					  throw this.unexpectedEndOfData(lineNr[0]);
				}

				ch = input[offset];

				if ((ch == '!') || (ch == '?'))
				{
					  offset = this.skipBogusTag(input, offset, end, lineNr);
				}
		  } while (!isIdentifierChar(ch));

		  return offset;
	   }


	   /// <summary>
	   /// Skips whitespace characters.
	   /// </summary>
	   /// <returns> the position after the whitespace
	   /// </returns>
	   /// <exception cref="nanoxml.XMLParseException">
	   ///    if an error occured while parsing the array </exception>
	   private int skipWhitespace(char[] input, int offset, int end, int[] lineNr)
	   {
		  int startLine = lineNr[0];

		  while (offset < end)
		  {
				if (((offset + 6) < end) && (input[offset + 3] == '-') && (input[offset + 2] == '-') && (input[offset + 1] == '!') && (input[offset] == '<'))
				{
					  offset += 4;

					  while ((input[offset] != '-') || (input[offset + 1] != '-') || (input[offset + 2] != '>'))
					  {
							if ((offset + 2) >= end)
							{
								  throw this.unexpectedEndOfData(startLine);
							}

							offset++;
					  }

					  offset += 2;
				}
				else if (input[offset] == '\r')
				{
					  lineNr[0]++;

					  if ((offset != end) && (input[offset + 1] == '\n'))
					  {
							offset++;
					  }
				}
				else if (input[offset] == '\n')
				{
					  lineNr[0]++;
				}
				else if (input[offset] > ' ')
				{
					  break;
				}

				offset++;
		  }

		  if (offset == end)
		  {
				throw this.unexpectedEndOfData(startLine);
		  }

		  return offset;
	   }


	   /// <summary>
	   /// Converts &amp;...; sequences to "normal" chars.
	   /// </summary>
	   protected internal virtual string decodeString(string s, int lineNr)
	   {
		  StringBuilder result = new StringBuilder(s.Length);
		  int index = 0;

		  while (index < s.Length)
		  {
				int index2 = (s + '&').IndexOf('&', index);
				int index3 = (s + "<![CDATA[").IndexOf("<![CDATA[", index, StringComparison.Ordinal);
				int index4 = (s + "<!--").IndexOf("<!--", index, StringComparison.Ordinal);

				if ((index2 <= index3) && (index2 <= index4))
				{
					  result.Append(s.Substring(index, index2 - index));

					  if (index2 == s.Length)
					  {
							break;
					  }

					  index = s.IndexOf(';', index2);

					  if (index < 0)
					  {
							result.Append(s.Substring(index2));
							break;
					  }

					  string key = s.Substring(index2 + 1, index - (index2 + 1));

					  if (key[0] == '#')
					  {
							if (key[1] == 'x')
							{
								  result.Append((char)(int.Parse(key.Substring(2), System.Globalization.NumberStyles.HexNumber)));
							}
							else
							{
								  result.Append((char)(int.Parse(key.Substring(1), System.Globalization.NumberStyles.Integer)));
							}
					  }
					  else
					  {
							result.Append(this.conversionTable.getProperty(key, "&" + key + ';'));
					  }
				}
				else if (index3 <= index4)
				{
					  int end = (s + "]]>").IndexOf("]]>", index3 + 9, StringComparison.Ordinal);
					  result.Append(s.Substring(index, index3 - index));
					  result.Append(s.Substring(index3 + 9, end - (index3 + 9)));
					  index = end + 2;
				}
				else
				{
					  result.Append(s.Substring(index, index4 - index));
					  index = (s + "-->").IndexOf("-->", index4, StringComparison.Ordinal) + 2;
				}

				index++;
		  }

		  return result.ToString();
	   }


	   /// <summary>
	   /// Writes the XML element to a string.
	   /// </summary>
	   public override string ToString()
	   {
		  StringWriter writer = new StringWriter();
		  this.write(writer);
		  return writer.ToString();
	   }


	   /// <summary>
	   /// Writes the XML element to a writer.
	   /// </summary>
	   public virtual void write(TextWriter writer)
	   {
		  this.write(writer, 0);
	   }


	   /// <summary>
	   /// Writes the XML element to a writer.
	   /// </summary>
	   public virtual void write(TextWriter writer, int indent)
	   {
		  //TODO PrintWriter @out = new PrintWriter(writer);

		  for (int i = 0; i < indent; i++)
		  {
				writer.Write(' ');
		  }

		  if (string.ReferenceEquals(this.tagName, null))
		  {
				this.writeEncoded(writer, this.contents);
				return;
		  }

          writer.Write('<');
          writer.Write(this.tagName);

		  if (!this.attributes.Empty)
		  {
				System.Collections.IEnumerator l_enum = this.attributes.keys();

				while (l_enum.MoveNext())
				{
                      writer.Write(' ');
					  string key = (string)(l_enum.Current);
					  string value = (string)(this.attributes.get(key));
                      writer.Write(key);
                      writer.Write("=\"");
					  this.writeEncoded(writer, value);
                      writer.Write('"');
				}
		  }

		  if ((!string.ReferenceEquals(this.contents, null)) && (this.contents.Length > 0))
		  {
				if (this.skipLeadingWhitespace)
				{
                      writer.Write('>');

					  for (int i = 0; i < indent + 4; i++)
					  {
							writer.Write(' ');
					  }

                      writer.Write(this.contents);

					  for (int i = 0; i < indent; i++)
					  {
                            writer.Write(' ');
					  }
				}
				else
				{
                      writer.Write('>');
					  this.writeEncoded(writer, this.contents);
				}

                writer.Write("</");
                writer.Write(this.tagName);
                writer.Write('>');
		  }
		  else if (this.children.Count == 0)
		  {
                writer.Write("/>");
		  }
		  else
		  {
			 writer.Write('>');
			 System.Collections.IEnumerator l_enum = this.enumerateChildren();

			 while (l_enum.MoveNext())
			 {
				   XMLElement child = (XMLElement)(l_enum.Current);
				   child.write(writer, indent + 4);
			 }

			 for (int i = 0; i < indent; i++)
			 {
				   writer.Write(' ');
			 }

			 writer.Write("</");
			 writer.Write(this.tagName);
			 writer.Write('>');
		  }
	   }


	   /// <summary>
	   /// Writes a string encoded to a writer.
	   /// </summary>
	   protected internal virtual void writeEncoded(TextWriter @out, string str)
	   {
		  for (int i = 0; i < str.Length; i++)
		  {
				char ch = str[i];

				switch (ch)
				{
					  case '<':
						 @out.Write("&lt;");
						 break;

					  case '>':
						 @out.Write("&gt;");
						 break;

					  case '&':
						 @out.Write("&amp;");
						 break;

					  case '"':
						 @out.Write("&quot;");
						 break;

					  case '\'':
						 @out.Write("&apos;");
						 break;

					  case '\r':
					  case '\n':
						 @out.Write(ch);
						 break;

					  default:
						 if (((int)ch < 32) || ((int)ch > 126))
						 {
							   @out.Write("&#x");
							   @out.Write(Convert.ToString((int)ch, 16));
							   @out.Write(';');
						 }
						 else
						 {
							   @out.Write(ch);
						 }
						break;
				}
		  }
	   }


	   /// <summary>
	   /// Creates a parse exception for when an invalid valueset is given to
	   /// a method.
	   /// </summary>
	   private XMLParseException invalidValueSet(string key)
	   {
		  string msg = "Invalid value set (key = \"" + key + "\")";
		  return new XMLParseException(this.TagName, msg);
	   }


	   /// <summary>
	   /// Creates a parse exception for when an invalid value is given to a
	   /// method.
	   /// </summary>
	   private XMLParseException invalidValue(string key, string value, int lineNr)
	   {
		  string msg = "Attribute \"" + key + "\" does not contain a valid " + "value (\"" + value + "\")";
		  return new XMLParseException(this.TagName, lineNr, msg);
	   }


	   /// <summary>
	   /// The end of the data input has been reached.
	   /// </summary>
	   private XMLParseException unexpectedEndOfData(int lineNr)
	   {
		  string msg = "Unexpected end of data reached";
		  return new XMLParseException(this.TagName, lineNr, msg);
	   }


	   /// <summary>
	   /// A syntax error occured.
	   /// </summary>
	   private XMLParseException syntaxError(string context, int lineNr)
	   {
		  string msg = "Syntax error while parsing " + context;
		  return new XMLParseException(this.TagName, lineNr, msg);
	   }


	   /// <summary>
	   /// A character has been expected.
	   /// </summary>
	   private XMLParseException expectedInput(string charSet, int lineNr)
	   {
		  string msg = "Expected: " + charSet;
		  return new XMLParseException(this.TagName, lineNr, msg);
	   }


	   /// <summary>
	   /// A value is missing for an attribute.
	   /// </summary>
	   private XMLParseException valueMissingForAttribute(string key, int lineNr)
	   {
		  string msg = "Value missing for attribute with key \"" + key + "\"";
		  return new XMLParseException(this.TagName, lineNr, msg);
	   }

	}

}