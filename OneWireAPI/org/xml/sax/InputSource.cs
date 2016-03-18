using System.IO;

// SAX input source.
// No warranty; no copyright -- use this as you will.
// $Id: InputSource.java,v 1.4 1998/05/12 01:44:54 david Exp $

namespace org.xml.sax
{


	/// <summary>
	/// A single input source for an XML entity.
	///  
	/// <para>This class allows a SAX application to encapsulate information
	/// about an input source in a single object, which may include
	/// a public identifier, a system identifier, a byte stream (possibly
	/// with a specified encoding), and/or a character stream.</para>
	///  
	/// <para>There are two places that the application will deliver this
	/// input source to the parser: as the argument to the Parser.parse
	/// method, or as the return value of the EntityResolver.resolveEntity
	/// method.</para>
	///  
	/// <para>The SAX parser will use the InputSource object to determine how
	/// to read XML input.  If there is a character stream available, the
	/// parser will read that stream directly; if not, the parser will use
	/// a byte stream, if available; if neither a character stream nor a
	/// byte stream is available, the parser will attempt to open a URI
	/// connection to the resource identified by the system
	/// identifier.</para>
	///  
	/// <para>An InputSource object belongs to the application: the SAX parser
	/// shall never modify it in any way (it may modify a copy if 
	/// necessary).</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.Parser#parse </seealso>
	/// <seealso cref= org.xml.sax.EntityResolver#resolveEntity </seealso>
	/// <seealso cref= java.io.InputStream </seealso>
	/// <seealso cref= java.io.Reader </seealso>
	public class InputSource
	{

	  /// <summary>
	  /// Zero-argument default constructor.
	  /// </summary>
	  /// <seealso cref= #setPublicId </seealso>
	  /// <seealso cref= #setSystemId </seealso>
	  /// <seealso cref= #setByteStream </seealso>
	  /// <seealso cref= #setCharacterStream </seealso>
	  /// <seealso cref= #setEncoding </seealso>
	  public InputSource()
	  {
	  }


	  /// <summary>
	  /// Create a new input source with a system identifier.
	  ///  
	  /// <para>Applications may use setPublicId to include a 
	  /// public identifier as well, or setEncoding to specify
	  /// the character encoding, if known.</para>
	  ///  
	  /// <para>If the system identifier is a URL, it must be full resolved.</para>
	  /// </summary>
	  /// <param name="systemId"> The system identifier (URI). </param>
	  /// <seealso cref= #setPublicId </seealso>
	  /// <seealso cref= #setSystemId </seealso>
	  /// <seealso cref= #setByteStream </seealso>
	  /// <seealso cref= #setEncoding </seealso>
	  /// <seealso cref= #setCharacterStream </seealso>
	  public InputSource(string systemId)
	  {
		SystemId = systemId;
	  }


	  /// <summary>
	  /// Create a new input source with a byte stream.
	  ///  
	  /// <para>Application writers may use setSystemId to provide a base 
	  /// for resolving relative URIs, setPublicId to include a 
	  /// public identifier, and/or setEncoding to specify the object's
	  /// character encoding.</para>
	  /// </summary>
	  /// <param name="byteStream"> The raw byte stream containing the document. </param>
	  /// <seealso cref= #setPublicId </seealso>
	  /// <seealso cref= #setSystemId </seealso>
	  /// <seealso cref= #setEncoding </seealso>
	  /// <seealso cref= #setByteStream </seealso>
	  /// <seealso cref= #setCharacterStream </seealso>
	  public InputSource(System.IO.Stream byteStream)
	  {
		ByteStream = byteStream;
	  }


	  /// <summary>
	  /// Create a new input source with a character stream.
	  ///  
	  /// <para>Application writers may use setSystemId() to provide a base 
	  /// for resolving relative URIs, and setPublicId to include a 
	  /// public identifier.</para>
	  ///  
	  /// <para>The character stream shall not include a byte order mark.</para>
	  /// </summary>
	  /// <seealso cref= #setPublicId </seealso>
	  /// <seealso cref= #setSystemId </seealso>
	  /// <seealso cref= #setByteStream </seealso>
	  /// <seealso cref= #setCharacterStream </seealso>
	  public InputSource(TextReader characterStream)
	  {
		CharacterStream = characterStream;
	  }


	  /// <summary>
	  /// Set the public identifier for this input source.
	  ///  
	  /// <para>The public identifier is always optional: if the application
	  /// writer includes one, it will be provided as part of the
	  /// location information.</para>
	  /// </summary>
	  /// <param name="publicId"> The public identifier as a string. </param>
	  /// <seealso cref= #getPublicId </seealso>
	  /// <seealso cref= org.xml.sax.Locator#getPublicId </seealso>
	  /// <seealso cref= org.xml.sax.SAXParseException#getPublicId </seealso>
	  public virtual string PublicId
	  {
		  set
		  {
			this.publicId = value;
		  }
		  get
		  {
			return publicId;
		  }
	  }




	  /// <summary>
	  /// Set the system identifier for this input source.
	  ///  
	  /// <para>The system identifier is optional if there is a byte stream
	  /// or a character stream, but it is still useful to provide one,
	  /// since the application can use it to resolve relative URIs
	  /// and can include it in error messages and warnings (the parser
	  /// will attempt to open a connection to the URI only if
	  /// there is no byte stream or character stream specified).</para>
	  ///  
	  /// <para>If the application knows the character encoding of the
	  /// object pointed to by the system identifier, it can register
	  /// the encoding using the setEncoding method.</para>
	  ///  
	  /// <para>If the system ID is a URL, it must be fully resolved.</para>
	  /// </summary>
	  /// <param name="systemId"> The system identifier as a string. </param>
	  /// <seealso cref= #setEncoding </seealso>
	  /// <seealso cref= #getSystemId </seealso>
	  /// <seealso cref= org.xml.sax.Locator#getSystemId </seealso>
	  /// <seealso cref= org.xml.sax.SAXParseException#getSystemId </seealso>
	  public virtual string SystemId
	  {
		  set
		  {
			this.systemId = value;
		  }
		  get
		  {
			return systemId;
		  }
	  }




	  /// <summary>
	  /// Set the byte stream for this input source.
	  ///  
	  /// <para>The SAX parser will ignore this if there is also a character
	  /// stream specified, but it will use a byte stream in preference
	  /// to opening a URI connection itself.</para>
	  ///  
	  /// <para>If the application knows the character encoding of the
	  /// byte stream, it should set it with the setEncoding method.</para>
	  /// </summary>
	  /// <param name="byteStream"> A byte stream containing an XML document or
	  ///        other entity. </param>
	  /// <seealso cref= #setEncoding </seealso>
	  /// <seealso cref= #getByteStream </seealso>
	  /// <seealso cref= #getEncoding </seealso>
	  /// <seealso cref= java.io.InputStream </seealso>
	  public virtual Stream ByteStream
	  {
		  set
		  {
			this.byteStream = value;
		  }
		  get
		  {
			return byteStream;
		  }
	  }




	  /// <summary>
	  /// Set the character encoding, if known.
	  ///  
	  /// <para>The encoding must be a string acceptable for an
	  /// XML encoding declaration (see section 4.3.3 of the XML 1.0
	  /// recommendation).</para>
	  ///  
	  /// <para>This method has no effect when the application provides a
	  /// character stream.</para>
	  /// </summary>
	  /// <param name="encoding"> A string describing the character encoding. </param>
	  /// <seealso cref= #setSystemId </seealso>
	  /// <seealso cref= #setByteStream </seealso>
	  /// <seealso cref= #getEncoding </seealso>
	  public virtual System.Text.Encoding Encoding
	  {
		  set
		  {
			this.encoding = value;
		  }
		  get
		  {
			return encoding;
		  }
	  }




	  /// <summary>
	  /// Set the character stream for this input source.
	  ///  
	  /// <para>If there is a character stream specified, the SAX parser
	  /// will ignore any byte stream and will not attempt to open
	  /// a URI connection to the system identifier.</para>
	  /// </summary>
	  /// <param name="characterStream"> The character stream containing the
	  ///        XML document or other entity. </param>
	  /// <seealso cref= #getCharacterStream </seealso>
	  /// <seealso cref= java.io.Reader </seealso>
	  public virtual TextReader CharacterStream
	  {
		  set
		  {
			this.characterStream = value;
		  }
		  get
		  {
			return characterStream;
		  }
	  }




    
	  //////////////////////////////////////////////////////////////////////
	  // Internal state.
	  //////////////////////////////////////////////////////////////////////

	  private string publicId;
	  private string systemId;
	  private Stream byteStream;
	  private System.Text.Encoding encoding;
	  private TextReader characterStream;

	}

}