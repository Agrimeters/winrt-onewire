// SAX error handler.
// No warranty; no copyright -- use this as you will.
// $Id: ErrorHandler.java,v 1.5 1998/04/27 01:47:13 david Exp $

namespace org.xml.sax
{


	/// <summary>
	/// Basic interface for SAX error handlers.
	///  
	/// <para>If a SAX application needs to implement customized error
	/// handling, it must implement this interface and then register an
	/// instance with the SAX parser using the parser's setErrorHandler
	/// method.  The parser will then report all errors and warnings
	/// through this interface.</para>
	///  
	/// <para> The parser shall use this interface instead of throwing an
	/// exception: it is up to the application whether to throw an
	/// exception for different types of errors and warnings.  Note,
	/// however, that there is no requirement that the parser continue to
	/// provide useful information after a call to fatalError (in other
	/// words, a SAX driver class could catch an exception and report a
	/// fatalError).</para>
	///  
	/// <para>The HandlerBase class provides a default implementation of this
	/// interface, ignoring warnings and recoverable errors and throwing a
	/// SAXParseException for fatal errors.  An application may extend
	/// that class rather than implementing the complete interface
	/// itself.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.Parser#setErrorHandler </seealso>
	/// <seealso cref= org.xml.sax.SAXParseException </seealso>
	/// <seealso cref= org.xml.sax.HandlerBase </seealso>
	public interface ErrorHandler
	{


	  /// <summary>
	  /// Receive notification of a warning.
	  ///  
	  /// <para>SAX parsers will use this method to report conditions that
	  /// are not errors or fatal errors as defined by the XML 1.0
	  /// recommendation.  The default behaviour is to take no action.</para>
	  ///  
	  /// <para>The SAX parser must continue to provide normal parsing events
	  /// after invoking this method: it should still be possible for the
	  /// application to process the document through to the end.</para>
	  /// </summary>
	  /// <param name="exception"> The warning information encapsulated in a
	  ///                  SAX parse exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.SAXParseException  </seealso>
	  void warning(SAXParseException exception);


	  /// <summary>
	  /// Receive notification of a recoverable error.
	  ///  
	  /// <para>This corresponds to the definition of "error" in section 1.2
	  /// of the W3C XML 1.0 Recommendation.  For example, a validating
	  /// parser would use this callback to report the violation of a
	  /// validity constraint.  The default behaviour is to take no
	  /// action.</para>
	  ///  
	  /// <para>The SAX parser must continue to provide normal parsing events
	  /// after invoking this method: it should still be possible for the
	  /// application to process the document through to the end.  If the
	  /// application cannot do so, then the parser should report a fatal
	  /// error even if the XML 1.0 recommendation does not require it to
	  /// do so.</para>
	  /// </summary>
	  /// <param name="exception"> The error information encapsulated in a
	  ///                  SAX parse exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.SAXParseException  </seealso>
	  void error(SAXParseException exception);


	  /// <summary>
	  /// Receive notification of a non-recoverable error.
	  ///  
	  /// <para>This corresponds to the definition of "fatal error" in
	  /// section 1.2 of the W3C XML 1.0 Recommendation.  For example, a
	  /// parser would use this callback to report the violation of a
	  /// well-formedness constraint.</para>
	  ///  
	  /// <para>The application must assume that the document is unusable
	  /// after the parser has invoked this method, and should continue
	  /// (if at all) only for the sake of collecting addition error
	  /// messages: in fact, SAX parsers are free to stop reporting any
	  /// other events once this method has been invoked.</para>
	  /// </summary>
	  /// <param name="exception"> The error information encapsulated in a
	  ///                  SAX parse exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.SAXParseException </seealso>
	  void fatalError(SAXParseException exception);

	}

}