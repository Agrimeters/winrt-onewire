using System;

// SAX parser factory.
// No warranty; no copyright -- use this as you will.
// $Id: ParserFactory.java,v 1.5 1998/05/01 20:58:23 david Exp $

namespace org.xml.sax.helpers
{



	/// <summary>
	/// Java-specific class for dynamically loading SAX parsers.
	///  
	/// <para>This class is not part of the platform-independent definition
	/// of SAX; it is an additional convenience class designed
	/// specifically for Java XML application writers.  SAX applications
	/// can use the static methods in this class to allocate a SAX parser
	/// dynamically at run-time based either on the value of the
	/// `org.xml.sax.parser' system property or on a string containing the class
	/// name.</para>
	///  
	/// <para>Note that the application still requires an XML parser that
	/// implements SAX.</para>
	///  
	/// @author David Megginson (ak117@freenet.carleton.ca)
	/// @version 1.0 </summary>
	/// <seealso cref= org.xml.sax.Parser </seealso>
	/// <seealso cref= java.lang.Class </seealso>
	public class ParserFactory
	{


	  /// <summary>
	  /// Private null constructor.
	  /// private ParserFactor ()
	  /// {
	  /// }
	  /// 
	  /// 
	  /// /**
	  /// Create a new SAX parser using the `org.xml.sax.parser' system property.
	  ///  
	  /// <para>The named class must exist and must implement the
	  /// org.xml.sax.Parser interface.</para>
	  /// </summary>
	  /// <exception cref="java.lang.NullPointerException"> There is no value
	  ///            for the `org.xml.sax.parser' system property. </exception>
	  /// <exception cref="java.lang.ClassNotFoundException"> The SAX parser
	  ///            class was not found (check your CLASSPATH). </exception>
	  /// <exception cref="IllegalAccessException"> The SAX parser class was
	  ///            found, but you do not have permission to load
	  ///            it. </exception>
	  /// <exception cref="InstantiationException"> The SAX parser class was
	  ///            found but could not be instantiated. </exception>
	  /// <exception cref="java.lang.ClassCastException"> The SAX parser class
	  ///            was found and instantiated, but does not implement
	  ///            org.xml.sax.Parser. </exception>
	  /// <seealso cref= #makeParser(java.lang.String) </seealso>
	  /// <seealso cref= org.xml.sax.Parser </seealso>
	  public static Parser makeParser()
	  {
		string className = System.Environment.GetEnvironmentVariable("org.xml.sax.parser");
		if (string.ReferenceEquals(className, null))
		{
		  throw new System.NullReferenceException("No value for sax.parser property");
		}
		else
		{
		  return makeParser(className);
		}
	  }


	  /// <summary>
	  /// Create a new SAX parser object using the class name provided.
	  ///  
	  /// <para>The named class must exist and must implement the
	  /// org.xml.sax.Parser interface.</para>
	  /// </summary>
	  /// <param name="className"> A string containing the name of the
	  ///                  SAX parser class. </param>
	  /// <exception cref="java.lang.ClassNotFoundException"> The SAX parser
	  ///            class was not found (check your CLASSPATH). </exception>
	  /// <exception cref="IllegalAccessException"> The SAX parser class was
	  ///            found, but you do not have permission to load
	  ///            it. </exception>
	  /// <exception cref="InstantiationException"> The SAX parser class was
	  ///            found but could not be instantiated. </exception>
	  /// <exception cref="java.lang.ClassCastException"> The SAX parser class
	  ///            was found and instantiated, but does not implement
	  ///            org.xml.sax.Parser. </exception>
	  /// <seealso cref= #makeParser() </seealso>
	  /// <seealso cref= org.xml.sax.Parser </seealso>
	  public static Parser makeParser(string className)
	  {
        return (Parser) Activator.CreateInstance(Type.GetType(className));
	  }

	}


}