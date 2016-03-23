// SAX parser interface.
// No warranty; no copyright -- use this as you will.
// $Id: Parser.java,v 0.8 1998/05/12 01:46:40 david Exp $

namespace org.xml.sax
{



	/// <summary>
	/// Basic interface for SAX (Simple API for XML) parsers.
	///  
	/// <para>All SAX parsers must implement this basic interface: it allows
	/// applications to register handlers for different types of events
	/// and to initiate a parse from a URI, or a character stream.</para>
	///  
	/// <para>All SAX parsers must also implement a zero-argument constructor
	/// (though other constructors are also allowed).</para>
	///  
	/// <para>SAX parsers are reusable but not re-entrant: the application
	/// may reuse a parser object (possibly with a different input source)
	/// once the first parse has completed successfully, but it may not
	/// invoke the parse() methods recursively within a parse.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.EntityResolver </seealso>
	/// <seealso cref= org.xml.sax.DTDHandler </seealso>
	/// <seealso cref= org.xml.sax.DocumentHandler </seealso>
	/// <seealso cref= org.xml.sax.ErrorHandler </seealso>
	/// <seealso cref= org.xml.sax.HandlerBase </seealso>
	/// <seealso cref= org.xml.sax.InputSource </seealso>
	public interface Parser
	{

	  /// <summary>
	  /// Allow an application to register a custom entity resolver.
	  ///  
	  /// <para>If the application does not register an entity resolver, the
	  /// SAX parser will resolve system identifiers and open connections
	  /// to entities itself (this is the default behaviour implemented in
	  /// HandlerBase).</para>
	  ///  
	  /// <para>Applications may register a new or different entity resolver
	  /// in the middle of a parse, and the SAX parser must begin using
	  /// the new resolver immediately.</para>
	  /// </summary>
	  /// <param name="resolver"> The object for resolving entities. </param>
	  /// <seealso cref= EntityResolver </seealso>
	  /// <seealso cref= HandlerBase </seealso>
	  EntityResolver EntityResolver {set;}


	  /// <summary>
	  /// Allow an application to register a DTD event handler.
	  ///  
	  /// <para>If the application does not register a DTD handler, all DTD
	  /// events reported by the SAX parser will be silently
	  /// ignored (this is the default behaviour implemented by
	  /// HandlerBase).</para>
	  ///  
	  /// <para>Applications may register a new or different
	  /// handler in the middle of a parse, and the SAX parser must
	  /// begin using the new handler immediately.</para>
	  /// </summary>
	  /// <param name="handler"> The DTD handler. </param>
	  /// <seealso cref= DTDHandler </seealso>
	  /// <seealso cref= HandlerBase </seealso>
	  DTDHandler DTDHandler {set;}


	  /// <summary>
	  /// Allow an application to register a document event handler.
	  ///  
	  /// <para>If the application does not register a document handler, all
	  /// document events reported by the SAX parser will be silently
	  /// ignored (this is the default behaviour implemented by
	  /// HandlerBase).</para>
	  ///  
	  /// <para>Applications may register a new or different handler in the
	  /// middle of a parse, and the SAX parser must begin using the new
	  /// handler immediately.</para>
	  /// </summary>
	  /// <param name="handler"> The document handler. </param>
	  /// <seealso cref= DocumentHandler </seealso>
	  /// <seealso cref= HandlerBase </seealso>
	  DocumentHandler DocumentHandler {set;}


	  /// <summary>
	  /// Allow an application to register an error event handler.
	  ///  
	  /// <para>If the application does not register an error event handler,
	  /// all error events reported by the SAX parser will be silently
	  /// ignored, except for fatalError, which will throw a SAXException
	  /// (this is the default behaviour implemented by HandlerBase).</para>
	  ///  
	  /// <para>Applications may register a new or different handler in the
	  /// middle of a parse, and the SAX parser must begin using the new
	  /// handler immediately.</para>
	  /// </summary>
	  /// <param name="handler"> The error handler. </param>
	  /// <seealso cref= ErrorHandler </seealso>
	  /// <seealso cref= SAXException </seealso>
	  /// <seealso cref= HandlerBase </seealso>
	  ErrorHandler ErrorHandler {set;}


	  /// <summary>
	  /// Parse an XML document.
	  ///  
	  /// <para>The application can use this method to instruct the SAX parser
	  /// to begin parsing an XML document from any valid input
	  /// source (a character stream, a byte stream, or a URI).</para>
	  ///  
	  /// <para>Applications may not invoke this method while a parse is in
	  /// progress (they should create a new Parser instead for each
	  /// additional XML document).  Once a parse is complete, an
	  /// application may reuse the same Parser object, possibly with a
	  /// different input source.</para>
	  /// </summary>
	  /// <param name="source"> The input source for the top-level of the
	  ///        XML document. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <exception cref="System.IO.IOException"> An IO exception from the parser,
	  ///            possibly from a byte stream or character stream
	  ///            supplied by the application. </exception>
	  /// <seealso cref= org.xml.sax.InputSource </seealso>
	  /// <seealso cref= #parse(java.lang.String) </seealso>
	  /// <seealso cref= #setEntityResolver </seealso>
	  /// <seealso cref= #setDTDHandler </seealso>
	  /// <seealso cref= #setDocumentHandler </seealso>
	  /// <seealso cref= #setErrorHandler </seealso>
	  void parse(InputSource source);


	  /// <summary>
	  /// Parse an XML document from a system identifier (URI).
	  ///  
	  /// <para>This method is a shortcut for the common case of reading a
	  /// document from a system identifier.  It is the exact
	  /// equivalent of the following:</para>
	  ///  
	  /// <pre>
	  /// parse(new InputSource(systemId));
	  /// </pre>
	  ///  
	  /// <para>If the system identifier is a URL, it must be fully resolved
	  /// by the application before it is passed to the parser.</para>
	  /// </summary>
	  /// <param name="systemId"> The system identifier (URI). </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <exception cref="System.IO.IOException"> An IO exception from the parser,
	  ///            possibly from a byte stream or character stream
	  ///            supplied by the application. </exception>
	  /// <seealso cref= #parse(org.xml.sax.InputSource) </seealso>
	  void parse(string systemId);

	}

}