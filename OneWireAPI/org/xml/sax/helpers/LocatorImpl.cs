// SAX default implementation for Locator.
// No warranty; no copyright -- use this as you will.
// $Id: LocatorImpl.java,v 1.2 1998/04/27 01:48:00 david Exp $

namespace org.xml.sax.helpers
{


	/// <summary>
	/// Provide an optional convenience implementation of Locator.
	///  
	/// <para>This class is available mainly for application writers, who
	/// can use it to make a persistent snapshot of a locator at any
	/// point during a document parse:</para>
	///  
	/// <pre>
	/// Locator locator;
	/// Locator startloc;
	///  
	/// public void setLocator (Locator locator)
	/// {
	///         // note the locator
	///   this.locator = locator;
	/// }
	///  
	/// public void startDocument ()
	/// {
	///         // save the location of the start of the document
	///         // for future use.
	///   Locator startloc = new LocatorImpl(locator);
	/// }
	/// </pre>
	///  
	/// <para>Normally, parser writers will not use this class, since it
	/// is more efficient to provide location information only when
	/// requested, rather than constantly updating a Locator object.</para>
	/// </summary>
	/// <seealso cref= org.xml.sax.Locator </seealso>
	public class LocatorImpl : Locator
	{


	  /// <summary>
	  /// Zero-argument constructor.
	  ///  
	  /// <para>This will not normally be useful, since the main purpose
	  /// of this class is to make a snapshot of an existing Locator.</para>
	  /// </summary>
	  public LocatorImpl()
	  {
	  }


	  /// <summary>
	  /// Copy constructor.
	  ///  
	  /// <para>Create a persistent copy of the current state of a locator.
	  /// When the original locator changes, this copy will still keep
	  /// the original values (and it can be used outside the scope of
	  /// DocumentHandler methods).</para>
	  /// </summary>
	  /// <param name="locator"> The locator to copy. </param>
	  public LocatorImpl(Locator locator)
	  {
		PublicId = locator.PublicId;
		SystemId = locator.SystemId;
		LineNumber = locator.LineNumber;
		ColumnNumber = locator.ColumnNumber;
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Implementation of org.xml.sax.Locator
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Return the saved public identifier.
	  /// </summary>
	  /// <returns> The public identifier as a string, or null if none
	  ///         is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getPublicId </seealso>
	  /// <seealso cref= #setPublicId </seealso>
	  public virtual string PublicId
	  {
		  get
		  {
			return publicId;
		  }
		  set
		  {
			this.publicId = value;
		  }
	  }


	  /// <summary>
	  /// Return the saved system identifier.
	  /// </summary>
	  /// <returns> The system identifier as a string, or null if none
	  ///         is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getSystemId </seealso>
	  /// <seealso cref= #setSystemId </seealso>
	  public virtual string SystemId
	  {
		  get
		  {
			return systemId;
		  }
		  set
		  {
			this.systemId = value;
		  }
	  }


	  /// <summary>
	  /// Return the saved line number (1-based).
	  /// </summary>
	  /// <returns> The line number as an integer, or -1 if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getLineNumber </seealso>
	  /// <seealso cref= #setLineNumber </seealso>
	  public virtual int LineNumber
	  {
		  get
		  {
			return lineNumber;
		  }
		  set
		  {
			this.lineNumber = value;
		  }
	  }


	  /// <summary>
	  /// Return the saved column number (1-based).
	  /// </summary>
	  /// <returns> The column number as an integer, or -1 if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getColumnNumber </seealso>
	  /// <seealso cref= #setColumnNumber </seealso>
	  public virtual int ColumnNumber
	  {
		  get
		  {
			return columnNumber;
		  }
		  set
		  {
			this.columnNumber = value;
		  }
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Setters for the properties (not in org.xml.sax.Locator)
	  //////////////////////////////////////////////////////////////////////










    
	  //////////////////////////////////////////////////////////////////////
	  // Internal state.
	  //////////////////////////////////////////////////////////////////////

	  private string publicId;
	  private string systemId;
	  private int lineNumber;
	  private int columnNumber;

	}


}