// SAX entity resolver.
// No warranty; no copyright -- use this as you will.
// $Id: EntityResolver.java,v 1.7 1998/05/12 01:43:58 david Exp $

namespace org.xml.sax
{


	/// <summary>
	/// Basic interface for resolving entities.
	///  
	/// <para>If a SAX application needs to implement customized handling
	/// for external entities, it must implement this interface and
	/// register an instance with the SAX parser using the parser's
	/// setEntityResolver method.</para>
	///  
	/// <para>The parser will then allow the application to intercept any
	/// external entities (including the external DTD subset and external
	/// parameter entities, if any) before including them.</para>
	///  
	/// <para>Many SAX applications will not need to implement this interface,
	/// but it will be especially useful for applications that build
	/// XML documents from databases or other specialised input sources,
	/// or for applications that use URI types other than URLs.</para>
	///  
	/// <para>The following resolver would provide the application
	/// with a special character stream for the entity with the system
	/// identifier "http://www.myhost.com/today":</para>
	///  
	/// <pre>
	/// import org.xml.sax.EntityResolver;
	/// import org.xml.sax.InputSource;
	///  
	/// public class MyResolver implements EntityResolver {
	///   public InputSource resolveEntity (String publicId, String systemId)
	///   {
	///     if (systemId.equals("http://www.myhost.com/today")) {
	///              // return a special input source
	///       MyReader reader = new MyReader();
	///       return new InputSource(reader);
	///     } else {
	///              // use the default behaviour
	///       return null;
	///     }
	///   }
	/// }
	/// </pre>
	///  
	/// <para>The application can also use this interface to redirect system
	/// identifiers to local URIs or to look up replacements in a catalog
	/// (possibly by using the public identifier).</para>
	///  
	/// <para>The HandlerBase class implements the default behaviour for
	/// this interface, which is simply always to return null (to request
	/// that the parser use the default system identifier).</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.Parser#setEntityResolver </seealso>
	/// <seealso cref= org.xml.sax.InputSource </seealso>
	/// <seealso cref= org.xml.sax.HandlerBase  </seealso>
	public interface EntityResolver
	{


	  /// <summary>
	  /// Allow the application to resolve external entities.
	  ///  
	  /// <para>The Parser will call this method before opening any external
	  /// entity except the top-level document entity (including the
	  /// external DTD subset, external entities referenced within the
	  /// DTD, and external entities referenced within the document
	  /// element): the application may request that the parser resolve
	  /// the entity itself, that it use an alternative URI, or that it
	  /// use an entirely different input source.</para>
	  ///  
	  /// <para>Application writers can use this method to redirect external
	  /// system identifiers to secure and/or local URIs, to look up
	  /// public identifiers in a catalogue, or to read an entity from a
	  /// database or other input source (including, for example, a dialog
	  /// box).</para>
	  ///  
	  /// <para>If the system identifier is a URL, the SAX parser must
	  /// resolve it fully before reporting it to the application.</para>
	  /// </summary>
	  /// <param name="publicId"> The public identifier of the external entity
	  ///        being referenced, or null if none was supplied. </param>
	  /// <param name="systemId"> The system identifier of the external entity
	  ///        being referenced. </param>
	  /// <returns> An InputSource object describing the new input source,
	  ///         or null to request that the parser open a regular
	  ///         URI connection to the system identifier. </returns>
	  /// <exception cref="org.xml.sax.SAXException"> Any SAX exception, possibly
	  ///            wrapping another exception. </exception>
	  /// <exception cref="System.IO.IOException"> A Java-specific IO exception,
	  ///            possibly the result of creating a new InputStream
	  ///            or Reader for the InputSource. </exception>
	  /// <seealso cref= org.xml.sax.InputSource </seealso>
	  InputSource resolveEntity(string publicId, string systemId);

	}

}