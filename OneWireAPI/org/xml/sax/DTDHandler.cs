// SAX DTD handler.
// No warranty; no copyright -- use this as you will.
// $Id: DTDHandler.java,v 1.5 1998/05/12 01:43:24 david Exp $

namespace org.xml.sax
{

	/// <summary>
	/// Receive notification of basic DTD-related events.
	///  
	/// <para>If a SAX application needs information about notations and
	/// unparsed entities, then the application implements this 
	/// interface and registers an instance with the SAX parser using 
	/// the parser's setDTDHandler method.  The parser uses the 
	/// instance to report notation and unparsed entity declarations to 
	/// the application.</para>
	///  
	/// <para>The SAX parser may report these events in any order, regardless
	/// of the order in which the notations and unparsed entities were
	/// declared; however, all DTD events must be reported after the
	/// document handler's startDocument event, and before the first
	/// startElement event.</para>
	///  
	/// <para>It is up to the application to store the information for 
	/// future use (perhaps in a hash table or object tree).
	/// If the application encounters attributes of type "NOTATION",
	/// "ENTITY", or "ENTITIES", it can use the information that it
	/// obtained through this interface to find the entity and/or
	/// notation corresponding with the attribute value.</para>
	///  
	/// <para>The HandlerBase class provides a default implementation
	/// of this interface, which simply ignores the events.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.Parser#setDTDHandler </seealso>
	/// <seealso cref= org.xml.sax.HandlerBase  </seealso>
	public interface DTDHandler
	{


	  /// <summary>
	  /// Receive notification of a notation declaration event.
	  ///  
	  /// <para>It is up to the application to record the notation for later
	  /// reference, if necessary.</para>
	  ///  
	  /// <para>If a system identifier is present, and it is a URL, the SAX
	  /// parser must resolve it fully before passing it to the
	  /// application.</para>
	  /// </summary>
	  /// <param name="name"> The notation name. </param>
	  /// <param name="publicId"> The notation's public identifier, or null if
	  ///        none was given. </param>
	  /// <param name="systemId"> The notation's system identifier, or null if
	  ///        none was given. </param>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <seealso cref= #unparsedEntityDecl </seealso>
	  /// <seealso cref= org.xml.sax.AttributeList </seealso>
	  void notationDecl(string name, string publicId, string systemId);


	  /// <summary>
	  /// Receive notification of an unparsed entity declaration event.
	  ///  
	  /// <para>Note that the notation name corresponds to a notation
	  /// reported by the notationDecl() event.  It is up to the
	  /// application to record the entity for later reference, if
	  /// necessary.</para>
	  ///  
	  /// <para>If the system identifier is a URL, the parser must resolve it
	  /// fully before passing it to the application.</para>
	  /// </summary>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <param name="name"> The unparsed entity's name. </param>
	  /// <param name="publicId"> The entity's public identifier, or null if none
	  ///        was given. </param>
	  /// <param name="systemId"> The entity's system identifier (it must always
	  ///        have one). </param>
	  /// <param name="notation"> name The name of the associated notation. </param>
	  /// <seealso cref= #notationDecl </seealso>
	  /// <seealso cref= org.xml.sax.AttributeList </seealso>
	  void unparsedEntityDecl(string name, string publicId, string systemId, string notationName);

	}

}