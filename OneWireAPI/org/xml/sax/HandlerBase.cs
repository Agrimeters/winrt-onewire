// SAX default handler base class.
// No warranty; no copyright -- use this as you will.
// $Id: HandlerBase.java,v 1.5 1998/04/27 01:47:23 david Exp $

namespace org.xml.sax
{

	/// <summary>
	/// Default base class for handlers.
	///  
	/// <para>This class implements the default behaviour for four SAX
	/// interfaces: EntityResolver, DTDHandler, DocumentHandler,
	/// and ErrorHandler.</para>
	///  
	/// <para>Application writers can extend this class when they need to
	/// implement only part of an interface; parser writers can
	/// instantiate this class to provide default handlers when the
	/// application has not supplied its own.</para>
	///  
	/// <para>Note that the use of this class is optional.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.EntityResolver </seealso>
	/// <seealso cref= org.xml.sax.DTDHandler </seealso>
	/// <seealso cref= org.xml.sax.DocumentHandler </seealso>
	/// <seealso cref= org.xml.sax.ErrorHandler </seealso>
	public class HandlerBase : EntityResolver, DTDHandler, DocumentHandler, ErrorHandler
	{

    
	  //////////////////////////////////////////////////////////////////////
	  // Default implementation of the EntityResolver interface.
	  //////////////////////////////////////////////////////////////////////

	  /// <summary>
	  /// Resolve an external entity.
	  ///  
	  /// <para>Always return null, so that the parser will use the system
	  /// identifier provided in the XML document.  This method implements
	  /// the SAX default behaviour: application writers can override it
	  /// in a subclass to do special translations such as catalog lookups
	  /// or URI redirection.</para>
	  /// </summary>
	  /// <param name="publicId"> The public identifer, or null if none is
	  ///                 available. </param>
	  /// <param name="systemId"> The system identifier provided in the XML 
	  ///                 document. </param>
	  /// <returns> The new input source, or null to require the
	  ///         default behaviour. </returns>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.EntityResolver#resolveEntity </seealso>
	  public virtual InputSource resolveEntity(string publicId, string systemId)
	  {
		return null;
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Default implementation of DTDHandler interface.
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Receive notification of a notation declaration.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass if they wish to keep track of the notations
	  /// declared in a document.</para>
	  /// </summary>
	  /// <param name="name"> The notation name. </param>
	  /// <param name="publicId"> The notation public identifier, or null if not
	  ///                 available. </param>
	  /// <param name="systemId"> The notation system identifier. </param>
	  /// <seealso cref= org.xml.sax.DTDHandler#notationDecl </seealso>
	  public virtual void notationDecl(string name, string publicId, string systemId)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of an unparsed entity declaration.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to keep track of the unparsed entities
	  /// declared in a document.</para>
	  /// </summary>
	  /// <param name="name"> The entity name. </param>
	  /// <param name="publicId"> The entity public identifier, or null if not
	  ///                 available. </param>
	  /// <param name="systemId"> The entity system identifier. </param>
	  /// <param name="notationName"> The name of the associated notation. </param>
	  /// <seealso cref= org.xml.sax.DTDHandler#unparsedEntityDecl </seealso>
	  public virtual void unparsedEntityDecl(string name, string publicId, string systemId, string notationName)
	  {
		// no op
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Default implementation of DocumentHandler interface.
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Receive a Locator object for document events.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass if they wish to store the locator for use
	  /// with other document events.</para>
	  /// </summary>
	  /// <param name="locator"> A locator for all SAX document events. </param>
	  /// <seealso cref= org.xml.sax.DocumentHandler#setDocumentLocator </seealso>
	  /// <seealso cref= org.xml.sax.Locator </seealso>
	  public virtual Locator DocumentLocator
	  {
		  set
		  {
			// no op
		  }
	  }


	  /// <summary>
	  /// Receive notification of the beginning of the document.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to take specific actions at the beginning
	  /// of a document (such as allocating the root node of a tree or
	  /// creating an output file).</para>
	  /// </summary>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#startDocument </seealso>
	  public virtual void startDocument()
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of the end of the document.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to take specific actions at the beginning
	  /// of a document (such as finalising a tree or closing an output
	  /// file).</para>
	  /// </summary>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#endDocument </seealso>
	  public virtual void endDocument()
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of the start of an element.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to take specific actions at the start of
	  /// each element (such as allocating a new tree node or writing
	  /// output to a file).</para>
	  /// </summary>
	  /// <param name="name"> The element type name. </param>
	  /// <param name="attributes"> The specified or defaulted attributes. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#startElement </seealso>
	  public virtual void startElement(string name, AttributeList attributes)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of the end of an element.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to take specific actions at the end of
	  /// each element (such as finalising a tree node or writing
	  /// output to a file).</para>
	  /// </summary>
	  /// <param name="name"> The element type name. </param>
	  /// <param name="attributes"> The specified or defaulted attributes. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#endElement </seealso>
	  public virtual void endElement(string name)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of character data inside an element.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method to take specific actions for each chunk of character data
	  /// (such as adding the data to a node or buffer, or printing it to
	  /// a file).</para>
	  /// </summary>
	  /// <param name="ch"> The characters. </param>
	  /// <param name="start"> The start position in the character array. </param>
	  /// <param name="length"> The number of characters to use from the
	  ///               character array. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#characters </seealso>
	  public virtual void characters(char[] ch, int start, int length)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of ignorable whitespace in element content.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method to take specific actions for each chunk of ignorable
	  /// whitespace (such as adding data to a node or buffer, or printing
	  /// it to a file).</para>
	  /// </summary>
	  /// <param name="ch"> The whitespace characters. </param>
	  /// <param name="start"> The start position in the character array. </param>
	  /// <param name="length"> The number of characters to use from the
	  ///               character array. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#ignorableWhitespace </seealso>
	  public virtual void ignorableWhitespace(char[] ch, int start, int length)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of a processing instruction.
	  ///  
	  /// <para>By default, do nothing.  Application writers may override this
	  /// method in a subclass to take specific actions for each
	  /// processing instruction, such as setting status variables or
	  /// invoking other methods.</para>
	  /// </summary>
	  /// <param name="target"> The processing instruction target. </param>
	  /// <param name="data"> The processing instruction data, or null if
	  ///             none is supplied. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.DocumentHandler#processingInstruction </seealso>
	  public virtual void processingInstruction(string target, string data)
	  {
		// no op
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Default implementation of the ErrorHandler interface.
	  //////////////////////////////////////////////////////////////////////


	  /// <summary>
	  /// Receive notification of a parser warning.
	  ///  
	  /// <para>The default implementation does nothing.  Application writers
	  /// may override this method in a subclass to take specific actions
	  /// for each warning, such as inserting the message in a log file or
	  /// printing it to the console.</para>
	  /// </summary>
	  /// <param name="e"> The warning information encoded as an exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.ErrorHandler#warning </seealso>
	  /// <seealso cref= org.xml.sax.SAXParseException </seealso>
	  public virtual void warning(SAXParseException e)
	  {
		// no op
	  }


	  /// <summary>
	  /// Receive notification of a recoverable parser error.
	  ///  
	  /// <para>The default implementation does nothing.  Application writers
	  /// may override this method in a subclass to take specific actions
	  /// for each error, such as inserting the message in a log file or
	  /// printing it to the console.</para>
	  /// </summary>
	  /// <param name="e"> The warning information encoded as an exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.ErrorHandler#warning </seealso>
	  /// <seealso cref= org.xml.sax.SAXParseException </seealso>
	  public virtual void error(SAXParseException e)
	  {
		// no op
	  }


	  /// <summary>
	  /// Report a fatal XML parsing error.
	  ///  
	  /// <para>The default implementation throws a SAXParseException.
	  /// Application writers may override this method in a subclass if
	  /// they need to take specific actions for each fatal error (such as
	  /// collecting all of the errors into a single report): in any case,
	  /// the application must stop all regular processing when this
	  /// method is invoked, since the document is no longer reliable, and
	  /// the parser may no longer report parsing events.</para>
	  /// </summary>
	  /// <param name="e"> The error information encoded as an exception. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= org.xml.sax.ErrorHandler#fatalError </seealso>
	  /// <seealso cref= org.xml.sax.SAXParseException </seealso>
	  public virtual void fatalError(SAXParseException e)
	  {
		throw e;
	  }

	}

}