// SAX locator interface for document events.
// No warranty; no copyright -- use this as you will.
// $Id: Locator.java,v 0.5 1998/05/12 01:45:55 david Exp $

namespace org.xml.sax
{


	/// <summary>
	/// Interface for associating a SAX event with a document location.
	///  
	/// <para>If a SAX parser provides location information to the SAX
	/// application, it does so by implementing this interface and then
	/// passing an instance to the application using the document
	/// handler's setDocumentLocator method.  The application can use the
	/// object to obtain the location of any other document handler event
	/// in the XML source document.</para>
	///  
	/// <para>Note that the results returned by the object will be valid only
	/// during the scope of each document handler method: the application
	/// will receive unpredictable results if it attempts to use the
	/// locator at any other time.</para>
	///  
	/// <para>SAX parsers are not required to supply a locator, but they are
	/// very strong encouraged to do so.  If the parser supplies a
	/// locator, it must do so before reporting any other document events.
	/// If no locator has been set by the time the application receives
	/// the startDocument event, the application should assume that a
	/// locator is not available.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.DocumentHandler#setDocumentLocator  </seealso>
	public interface Locator
	{


	  /// <summary>
	  /// Return the public identifier for the current document event.
	  /// <para>This will be the public identifier
	  /// </para>
	  /// </summary>
	  /// <returns> A string containing the public identifier, or
	  ///         null if none is available. </returns>
	  /// <seealso cref= #getSystemId </seealso>
	  string PublicId {get;}


	  /// <summary>
	  /// Return the system identifier for the current document event.
	  ///  
	  /// <para>If the system identifier is a URL, the parser must resolve it
	  /// fully before passing it to the application.</para>
	  /// </summary>
	  /// <returns> A string containing the system identifier, or null
	  ///         if none is available. </returns>
	  /// <seealso cref= #getPublicId </seealso>
	  string SystemId {get;}


	  /// <summary>
	  /// Return the line number where the current document event ends.
	  /// Note that this is the line position of the first character
	  /// after the text associated with the document event. </summary>
	  /// <returns> The line number, or -1 if none is available. </returns>
	  /// <seealso cref= #getColumnNumber </seealso>
	  int LineNumber {get;}


	  /// <summary>
	  /// Return the column number where the current document event ends.
	  /// Note that this is the column number of the first
	  /// character after the text associated with the document
	  /// event.  The first column in a line is position 1. </summary>
	  /// <returns> The column number, or -1 if none is available. </returns>
	  /// <seealso cref= #getLineNumber </seealso>
	  int ColumnNumber {get;}

	}

}