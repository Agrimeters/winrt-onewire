using System.Collections.Generic;

// SAX default implementation for AttributeList.
// No warranty; no copyright -- use this as you will.
// $Id: AttributeListImpl.java,v 1.3 1998/05/12 01:41:12 david Exp $

namespace org.xml.sax.helpers
{



	/// <summary>
	/// Convenience implementation for AttributeList.
	///  
	/// <para>This class provides a convenience implementation of the SAX
	/// AttributeList class.  This implementation is useful both
	/// for SAX parser writers, who can use it to provide attributes
	/// to the application, and for SAX application writers, who can
	/// use it to create a persistent copy of an element's attribute
	/// specifications:</para>
	///  
	/// <pre>
	/// private AttributeList myatts;
	///  
	/// public void startElement (String name, AttributeList atts)
	/// {
	///              // create a persistent copy of the attribute list
	///              // for use outside this method
	///   myatts = new AttributeListImpl(atts);
	///   [...]
	/// }
	/// </pre>
	///  
	/// <para>Please note that SAX parsers are not required to use this
	/// class to provide an implementation of AttributeList; it is
	/// supplied only as an optional convenience.  In particular, 
	/// parser writers are encouraged to invent more efficient
	/// implementations.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.AttributeList </seealso>
	/// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	public class AttributeListImpl : AttributeList
	{

	  /// <summary>
	  /// Create an empty attribute list.
	  ///  
	  /// <para>This constructor is most useful for parser writers, who
	  /// will use it to create a single, reusable attribute list that
	  /// can be reset with the clear method between elements.</para>
	  /// </summary>
	  /// <seealso cref= #addAttribute </seealso>
	  /// <seealso cref= #clear </seealso>
	  public AttributeListImpl()
	  {
	  }


	  /// <summary>
	  /// Construct a persistent copy of an existing attribute list.
	  ///  
	  /// <para>This constructor is most useful for application writers,
	  /// who will use it to create a persistent copy of an existing
	  /// attribute list.</para>
	  /// </summary>
	  /// <param name="atts"> The attribute list to copy </param>
	  /// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	  public AttributeListImpl(AttributeList atts)
	  {
		AttributeList = atts;
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Methods specific to this class.
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Set the attribute list, discarding previous contents.
	  ///  
	  /// <para>This method allows an application writer to reuse an
	  /// attribute list easily.</para>
	  /// </summary>
	  /// <param name="atts"> The attribute list to copy. </param>
	  public virtual AttributeList AttributeList
	  {
		  set
		  {
			int count = value.Length;
    
			clear();
    
			for (int i = 0; i < count; i++)
			{
			  addAttribute(value.getName(i), value.getType(i), value.getValue(i));
			}
		  }
	  }


	  /// <summary>
	  /// Add an attribute to an attribute list.
	  ///  
	  /// <para>This method is provided for SAX parser writers, to allow them
	  /// to build up an attribute list incrementally before delivering
	  /// it to the application.</para>
	  /// </summary>
	  /// <param name="name"> The attribute name. </param>
	  /// <param name="type"> The attribute type ("NMTOKEN" for an enumeration). </param>
	  /// <param name="value"> The attribute value (must not be null). </param>
	  /// <seealso cref= #removeAttribute </seealso>
	  /// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	  public virtual void addAttribute(string name, string type, string value)
	  {
		names.Add(name);
		types.Add(type);
		values.Add(value);
	  }


	  /// <summary>
	  /// Remove an attribute from the list.
	  ///  
	  /// <para>SAX application writers can use this method to filter an
	  /// attribute out of an AttributeList.  Note that invoking this
	  /// method will change the length of the attribute list and
	  /// some of the attribute's indices.</para>
	  ///  
	  /// <para>If the requested attribute is not in the list, this is
	  /// a no-op.</para>
	  /// </summary>
	  /// <param name="name"> The attribute name. </param>
	  /// <seealso cref= #addAttribute </seealso>
	  public virtual void removeAttribute(string name)
	  {
		int i = names.IndexOf(name);

		if (i >= 0)
		{
		  names.RemoveAt(i);
		  types.RemoveAt(i);
		  values.RemoveAt(i);
		}
	  }


	  /// <summary>
	  /// Clear the attribute list.
	  ///  
	  /// <para>SAX parser writers can use this method to reset the attribute
	  /// list between DocumentHandler.startElement events.  Normally,
	  /// it will make sense to reuse the same AttributeListImpl object
	  /// rather than allocating a new one each time.</para>
	  /// </summary>
	  /// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	  public virtual void clear()
	  {
		names.Clear();
		types.Clear();
		values.Clear();
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Implementation of org.xml.sax.AttributeList
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Return the number of attributes in the list.
	  /// </summary>
	  /// <returns> The number of attributes in the list. </returns>
	  /// <seealso cref= org.xml.sax.AttributeList#getLength </seealso>
	  public virtual int Length
	  {
		  get
		  {
			return names.Count;
		  }
	  }


	  /// <summary>
	  /// Get the name of an attribute (by position).
	  /// </summary>
	  /// <param name="i"> The position of the attribute in the list. </param>
	  /// <returns> The attribute name as a string, or null if there
	  ///         is no attribute at that position. </returns>
	  /// <seealso cref= org.xml.sax.AttributeList#getName(int) </seealso>
	  public virtual string getName(int i)
	  {
		try
		{
		  return (string)names[i];
		}
		catch (System.IndexOutOfRangeException)
		{
		  return null;
		}
	  }


	  /// <summary>
	  /// Get the type of an attribute (by position).
	  /// </summary>
	  /// <param name="i"> The position of the attribute in the list. </param>
	  /// <returns> The attribute type as a string ("NMTOKEN" for an
	  ///         enumeration, and "CDATA" if no declaration was
	  ///         read), or null if there is no attribute at
	  ///         that position. </returns>
	  /// <seealso cref= org.xml.sax.AttributeList#getType(int) </seealso>
	  public virtual string getType(int i)
	  {
		try
		{
		  return (string)types[i];
		}
		catch (System.IndexOutOfRangeException)
		{
		  return null;
		}
	  }


	  /// <summary>
	  /// Get the value of an attribute (by position).
	  /// </summary>
	  /// <param name="i"> The position of the attribute in the list. </param>
	  /// <returns> The attribute value as a string, or null if
	  ///         there is no attribute at that position. </returns>
	  /// <seealso cref= org.xml.sax.AttributeList#getValue(int) </seealso>
	  public virtual string getValue(int i)
	  {
		try
		{
		  return (string)values[i];
		}
		catch (System.IndexOutOfRangeException)
		{
		  return null;
		}
	  }


	  /// <summary>
	  /// Get the type of an attribute (by name).
	  /// </summary>
	  /// <param name="name"> The attribute name. </param>
	  /// <returns> The attribute type as a string ("NMTOKEN" for an
	  ///         enumeration, and "CDATA" if no declaration was
	  ///         read). </returns>
	  /// <seealso cref= org.xml.sax.AttributeList#getType(java.lang.String) </seealso>
	  public virtual string getType(string name)
	  {
		return getType(names.IndexOf(name));
	  }


	  /// <summary>
	  /// Get the value of an attribute (by name).
	  /// </summary>
	  /// <param name="name"> The attribute name. </param>
	  /// <seealso cref= org.xml.sax.AttributeList#getValue(java.lang.String) </seealso>
	  public virtual string getValue(string name)
	  {
		return getValue(names.IndexOf(name));
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Internal state.
	  //////////////////////////////////////////////////////////////////////

	  internal List<string> names = new List<string>();
	  internal List<string> types = new List<string>();
	  internal List<string> values = new List<string>();

	}

}