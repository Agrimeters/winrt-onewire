using System;

// SAX exception class.
// No warranty; no copyright -- use this as you will.
// $Id: SAXParseException.java,v 1.5 1998/05/12 01:47:44 david Exp $

namespace org.xml.sax
{

	/// <summary>
	/// Encapsulate an XML parse error or warning.
	///  
	/// <para>This exception will include information for locating the error
	/// in the original XML document.  Note that although the application
	/// will receive a SAXParseException as the argument to the handlers
	/// in the ErrorHandler interface, the application is not actually
	/// required to throw the exception; instead, it can simply read the
	/// information in it and take a different action.</para>
	///  
	/// <para>Since this exception is a subclass of SAXException, it
	/// inherits the ability to wrap another exception.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.SAXException </seealso>
	/// <seealso cref= org.xml.sax.Locator </seealso>
	/// <seealso cref= org.xml.sax.ErrorHandler </seealso>
	public class SAXParseException : SAXException
	{

    
	  //////////////////////////////////////////////////////////////////////
	  // Constructors.
	  //////////////////////////////////////////////////////////////////////

	  /// <summary>
	  /// Create a new SAXParseException from a message and a Locator.
	  ///  
	  /// <para>This constructor is especially useful when an application is
	  /// creating its own exception from within a DocumentHandler
	  /// callback.</para>
	  /// </summary>
	  /// <param name="message"> The error or warning message. </param>
	  /// <param name="locator"> The locator object for the error or warning. </param>
	  /// <seealso cref= org.xml.sax.Locator </seealso>
	  /// <seealso cref= org.xml.sax.Parser#setLocale  </seealso>
	  public SAXParseException(string message, Locator locator) : base(message)
	  {
		this.publicId = locator.PublicId;
		this.systemId = locator.SystemId;
		this.lineNumber = locator.LineNumber;
		this.columnNumber = locator.ColumnNumber;
	  }


	  /// <summary>
	  /// Wrap an existing exception in a SAXParseException.
	  ///  
	  /// <para>This constructor is especially useful when an application is
	  /// creating its own exception from within a DocumentHandler
	  /// callback, and needs to wrap an existing exception that is not a
	  /// subclass of SAXException.</para>
	  /// </summary>
	  /// <param name="message"> The error or warning message, or null to
	  ///                use the message from the embedded exception. </param>
	  /// <param name="locator"> The locator object for the error or warning. </param>
	  /// <param name="e"> Any exception </param>
	  /// <seealso cref= org.xml.sax.Locator </seealso>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public SAXParseException(string message, Locator locator, Exception e) : base(message, e)
	  {
		this.publicId = locator.PublicId;
		this.systemId = locator.SystemId;
		this.lineNumber = locator.LineNumber;
		this.columnNumber = locator.ColumnNumber;
	  }


	  /// <summary>
	  /// Create a new SAXParseException.
	  ///  
	  /// <para>This constructor is most useful for parser writers.</para>
	  ///  
	  /// <para>If the system identifier is a URL, the parser must resolve it
	  /// fully before creating the exception.</para>
	  /// </summary>
	  /// <param name="message"> The error or warning message. </param>
	  /// <param name="publicId"> The public identifer of the entity that generated
	  ///                 the error or warning. </param>
	  /// <param name="systemId"> The system identifer of the entity that generated
	  ///                 the error or warning. </param>
	  /// <param name="lineNumber"> The line number of the end of the text that
	  ///                   caused the error or warning. </param>
	  /// <param name="columnNumber"> The column number of the end of the text that
	  ///                     cause the error or warning. </param>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public SAXParseException(string message, string publicId, string systemId, int lineNumber, int columnNumber) : base(message)
	  {
		this.publicId = publicId;
		this.systemId = systemId;
		this.lineNumber = lineNumber;
		this.columnNumber = columnNumber;
	  }


	  /// <summary>
	  /// Create a new SAXParseException with an embedded exception.
	  ///  
	  /// <para>This constructor is most useful for parser writers who
	  /// need to wrap an exception that is not a subclass of
	  /// SAXException.</para>
	  ///  
	  /// <para>If the system identifier is a URL, the parser must resolve it
	  /// fully before creating the exception.</para>
	  /// </summary>
	  /// <param name="message"> The error or warning message, or null to use
	  ///                the message from the embedded exception. </param>
	  /// <param name="publicId"> The public identifer of the entity that generated
	  ///                 the error or warning. </param>
	  /// <param name="systemId"> The system identifer of the entity that generated
	  ///                 the error or warning. </param>
	  /// <param name="lineNumber"> The line number of the end of the text that
	  ///                   caused the error or warning. </param>
	  /// <param name="columnNumber"> The column number of the end of the text that
	  ///                     cause the error or warning. </param>
	  /// <param name="e"> Another exception to embed in this one. </param>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public SAXParseException(string message, string publicId, string systemId, int lineNumber, int columnNumber, Exception e) : base(message, e)
	  {
		this.publicId = publicId;
		this.systemId = systemId;
		this.lineNumber = lineNumber;
		this.columnNumber = columnNumber;
	  }


	  /// <summary>
	  /// Get the public identifier of the entity where the exception occurred.
	  /// </summary>
	  /// <returns> A string containing the public identifier, or null
	  ///         if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getPublicId </seealso>
	  public virtual string PublicId
	  {
		  get
		  {
			return this.publicId;
		  }
	  }


	  /// <summary>
	  /// Get the system identifier of the entity where the exception occurred.
	  ///  
	  /// <para>If the system identifier is a URL, it will be resolved
	  /// fully.</para>
	  /// </summary>
	  /// <returns> A string containing the system identifier, or null
	  ///         if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getSystemId </seealso>
	  public virtual string SystemId
	  {
		  get
		  {
			return this.systemId;
		  }
	  }


	  /// <summary>
	  /// The line number of the end of the text where the exception occurred.
	  /// </summary>
	  /// <returns> An integer representing the line number, or -1
	  ///         if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getLineNumber </seealso>
	  public virtual int LineNumber
	  {
		  get
		  {
			return this.lineNumber;
		  }
	  }


	  /// <summary>
	  /// The column number of the end of the text where the exception occurred.
	  ///  
	  /// <para>The first column in a line is position 1.</para>
	  /// </summary>
	  /// <returns> An integer representing the column number, or -1
	  ///         if none is available. </returns>
	  /// <seealso cref= org.xml.sax.Locator#getColumnNumber </seealso>
	  public virtual int ColumnNumber
	  {
		  get
		  {
			return this.columnNumber;
		  }
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Internal state.
	  //////////////////////////////////////////////////////////////////////

	  private string publicId;
	  private string systemId;
	  private int lineNumber;
	  private int columnNumber;

	}

}