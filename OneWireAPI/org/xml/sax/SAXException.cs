using System;

// SAX exception class.
// No warranty; no copyright -- use this as you will.
// $Id: SAXException.java,v 1.6 1998/05/01 21:00:01 david Exp $

namespace org.xml.sax
{

	/// <summary>
	/// Encapsulate a general SAX error or warning.
	///  
	/// <para>This class can contain basic error or warning information from
	/// either the XML parser or the application: a parser writer or
	/// application writer can subclass it to provide additional
	/// functionality.  SAX handlers may throw this exception or
	/// any exception subclassed from it.</para>
	///  
	/// <para>If the application needs to pass through other types of
	/// exceptions, it must wrap those exceptions in a SAXException
	/// or an exception derived from a SAXException.</para>
	///  
	/// <para>If the parser or application needs to include information about a
	/// specific location in an XML document, it should use the
	/// SAXParseException subclass.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.SAXParseException </seealso>
	public class SAXException : Exception
	{


	  /// <summary>
	  /// Create a new SAXException.
	  /// </summary>
	  /// <param name="message"> The error or warning message. </param>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public SAXException(string message) : base()
	  {
		this.message = message;
		this.exception = null;
	  }


	  /// <summary>
	  /// Create a new SAXException wrapping an existing exception.
	  ///  
	  /// <para>The existing exception will be embedded in the new
	  /// one, and its message will become the default message for
	  /// the SAXException.</para>
	  /// </summary>
	  /// <param name="e"> The exception to be wrapped in a SAXException. </param>
	  public SAXException(Exception e) : base()
	  {
		this.message = null;
		this.exception = e;
	  }


	  /// <summary>
	  /// Create a new SAXException from an existing exception.
	  ///  
	  /// <para>The existing exception will be embedded in the new
	  /// one, but the new exception will have its own message.</para>
	  /// </summary>
	  /// <param name="message"> The detail message. </param>
	  /// <param name="e"> The exception to be wrapped in a SAXException. </param>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public SAXException(string message, Exception e) : base()
	  {
		this.message = message;
		this.exception = e;
	  }


	  /// <summary>
	  /// Return a detail message for this exception.
	  ///  
	  /// <para>If there is a embedded exception, and if the SAXException
	  /// has no detail message of its own, this method will return
	  /// the detail message from the embedded exception.</para>
	  /// </summary>
	  /// <returns> The error or warning message. </returns>
	  /// <seealso cref= org.xml.sax.Parser#setLocale </seealso>
	  public virtual string Message
	  {
		  get
		  {
			if (string.ReferenceEquals(message, null) && exception != null)
			{
			  return exception.Message;
			}
			else
			{
			  return this.message;
			}
		  }
	  }


	  /// <summary>
	  /// Return the embedded exception, if any.
	  /// </summary>
	  /// <returns> The embedded exception, or null if there is none. </returns>
	  public virtual Exception Exception
	  {
		  get
		  {
			return exception;
		  }
	  }


	  /// <summary>
	  /// Convert this exception to a string.
	  /// </summary>
	  /// <returns> A string version of this exception. </returns>
	  public override string ToString()
	  {
		return Message;
	  }


    
	  //////////////////////////////////////////////////////////////////////
	  // Internal state.
	  //////////////////////////////////////////////////////////////////////

	  private string message;
	  private Exception exception;

	}

}