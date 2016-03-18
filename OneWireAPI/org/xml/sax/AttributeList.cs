// SAX Attribute List Interface.
// No warranty; no copyright -- use this as you will.
// $Id: AttributeList.java,v 1.6 1998/05/12 01:39:48 david Exp $

namespace org.xml.sax
{

	/// <summary>
	/// Interface for an element's attribute specifications.
	///  
	/// <para>The SAX parser implements this interface and passes an instance
	/// to the SAX application as the second argument of each startElement
	/// event.</para>
	///  
	/// <para>The instance provided will return valid results only during the
	/// scope of the startElement invocation (to save it for future
	/// use, the application must make a copy: the AttributeListImpl
	/// helper class provides a convenient constructor for doing so).</para>
	///  
	/// <para>An AttributeList includes only attributes that have been
	/// specified or defaulted: #IMPLIED attributes will not be included.</para>
	///  
	/// <para>There are two ways for the SAX application to obtain information
	/// from the AttributeList.  First, it can iterate through the entire
	/// list:</para>
	///  
	/// <pre>
	/// public void startElement (String name, AttributeList atts) {
	///   for (int i = 0; i < atts.getLength(); i++) {
	///     String name = atts.getName(i);
	///     String type = atts.getType(i);
	///     String value = atts.getValue(i);
	///     [...]
	///   }
	/// }
	/// </pre>
	///  
	/// <para>(Note that the result of getLength() will be zero if there
	/// are no attributes.)
	///  
	/// </para>
	/// <para>As an alternative, the application can request the value or
	/// type of specific attributes:</para>
	///  
	/// <pre>
	/// public void startElement (String name, AttributeList atts) {
	///   String identifier = atts.getValue("id");
	///   String label = atts.getValue("label");
	///   [...]
	/// }
	/// </pre>
	///  
	/// <para>The AttributeListImpl helper class provides a convenience 
	/// implementation for use by parser or application writers.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	/// <seealso cref= org.xml.sax.helpers.AttributeListImpl </seealso>
	public interface AttributeList
	{

	  /// <summary>
	  /// Return the number of attributes in this list.
	  ///  
	  /// <para>The SAX parser may provide attributes in any
	  /// arbitrary order, regardless of the order in which they were
	  /// declared or specified.  The number of attributes may be
	  /// zero.</para>
	  /// </summary>
	  /// <returns> The number of attributes in the list.   </returns>
	  int Length {get;}


	  /// <summary>
	  /// Return the name of an attribute in this list (by position).
	  ///  
	  /// <para>The names must be unique: the SAX parser shall not include the
	  /// same attribute twice.  Attributes without values (those declared
	  /// #IMPLIED without a value specified in the start tag) will be
	  /// omitted from the list.</para>
	  ///  
	  /// <para>If the attribute name has a namespace prefix, the prefix
	  /// will still be attached.</para>
	  /// </summary>
	  /// <param name="i"> The index of the attribute in the list (starting at 0). </param>
	  /// <returns> The name of the indexed attribute, or null
	  ///         if the index is out of range. </returns>
	  /// <seealso cref= #getLength  </seealso>
	  string getName(int i);


	  /// <summary>
	  /// Return the type of an attribute in the list (by position).
	  ///  
	  /// <para>The attribute type is one of the strings "CDATA", "ID",
	  /// "IDREF", "IDREFS", "NMTOKEN", "NMTOKENS", "ENTITY", "ENTITIES",
	  /// or "NOTATION" (always in upper case).</para>
	  ///  
	  /// <para>If the parser has not read a declaration for the attribute,
	  /// or if the parser does not report attribute types, then it must
	  /// return the value "CDATA" as stated in the XML 1.0 Recommentation
	  /// (clause 3.3.3, "Attribute-Value Normalization").</para>
	  ///  
	  /// <para>For an enumerated attribute that is not a notation, the
	  /// parser will report the type as "NMTOKEN".</para>
	  /// </summary>
	  /// <param name="i"> The index of the attribute in the list (starting at 0). </param>
	  /// <returns> The attribute type as a string, or
	  ///         null if the index is out of range. </returns>
	  /// <seealso cref= #getLength </seealso>
	  /// <seealso cref= #getType(java.lang.String) </seealso>
	  string getType(int i);


	  /// <summary>
	  /// Return the value of an attribute in the list (by position).
	  ///  
	  /// <para>If the attribute value is a list of tokens (IDREFS,
	  /// ENTITIES, or NMTOKENS), the tokens will be concatenated
	  /// into a single string separated by whitespace.</para>
	  /// </summary>
	  /// <param name="i"> The index of the attribute in the list (starting at 0). </param>
	  /// <returns> The attribute value as a string, or
	  ///         null if the index is out of range. </returns>
	  /// <seealso cref= #getLength </seealso>
	  /// <seealso cref= #getValue(java.lang.String) </seealso>
	  string getValue(int i);


	  /// <summary>
	  /// Return the type of an attribute in the list (by name).
	  ///  
	  /// <para>The return value is the same as the return value for
	  /// getType(int).</para>
	  ///  
	  /// <para>If the attribute name has a namespace prefix in the document,
	  /// the application must include the prefix here.</para>
	  /// </summary>
	  /// <param name="name"> The name of the attribute. </param>
	  /// <returns> The attribute type as a string, or null if no
	  ///         such attribute exists. </returns>
	  /// <seealso cref= #getType(int) </seealso>
	  string getType(string name);


	  /// <summary>
	  /// Return the value of an attribute in the list (by name).
	  ///  
	  /// <para>The return value is the same as the return value for
	  /// getValue(int).</para>
	  ///  
	  /// <para>If the attribute name has a namespace prefix in the document,
	  /// the application must include the prefix here.</para>
	  /// </summary>
	  /// <param name="i"> The index of the attribute in the list. </param>
	  /// <returns> The attribute value as a string, or null if
	  ///         no such attribute exists. </returns>
	  /// <seealso cref= #getValue(int) </seealso>
	  string getValue(string name);

	}

}