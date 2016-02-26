using System;
using System.Collections;
using System.Diagnostics;
using Windows.Storage;
using System.Reflection;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2005 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */

// OneWireAccessProvider.java
namespace com.dalsemi.onewire
{

	// imports
	using com.dalsemi.onewire.adapter;


	/// <summary>
	/// The OneWireAccessProvider class manages the Dallas Semiconductor
	/// adapter class derivatives of <code>DSPortAdapter</code>.  An enumeration of all
	/// available adapters can be accessed through the
	/// member function <code>EnumerateAllAdapters</code>.  This enables an
	/// application to be adapter independent. There are also facilities to get a system
	/// appropriate default adapter/port combination.<para>
	/// 
	/// <H3> Usage </H3>
	/// 
	/// <DL>
	/// <DD> <H4> Example 1</H4>
	/// Get an instance of the default 1-Wire adapter.  The adapter will be ready
	/// to use if no exceptions are thrown.
	/// <PRE> <CODE>
	///  try
	///  {
	///     DSPortAdapter adapter = OneWireAccessProvider.getDefaultAdapter();
	/// 
	///     System.out.println("Adapter: " + adapter.getAdapterName() + " Port: " + adapter.getPortName());
	/// 
	///     // use the adapter ...
	/// 
	///  }
	///  catch(Exception e)
	///  {
	///     System.out.println("Default adapter not present: " + e);
	///  }
	/// </CODE> </PRE>
	/// </DL>
	/// 
	/// <DL>
	/// <DD> <H4> Example 2</H4>
	/// Enumerate through the available adapters and ports.
	/// <PRE> <CODE>
	///  DSPortAdapter adapter;
	///  String        port;
	/// 
	///  // get the adapters
	///  for (Enumeration adapter_enum = OneWireAccessProvider.enumerateAllAdapters();
	///                                  adapter_enum.hasMoreElements(); )
	///  {
	///     // cast the enum as a DSPortAdapter
	///     adapter = ( DSPortAdapter ) adapter_enum.nextElement();
	/// 
	///     System.out.print("Adapter: " + adapter.getAdapterName() + " with ports: ");
	/// 
	///     // get the ports
	///     for (Enumeration port_enum = adapter.getPortNames();
	///             port_enum.hasMoreElements(); )
	///     {
	///        // cast the enum as a String
	///        port = ( String ) port_enum.nextElement();
	/// 
	///        System.out.print(port + " ");
	///     }
	/// 
	///     System.out.println();
	///  }
	/// </CODE> </PRE>
	/// </DL>
	/// 
	/// <DL>
	/// <DD> <H4> Example 3</H4>
	/// Display the default adapter name and port without getting an instance of the adapter.
	/// <PRE> <CODE>
	///  System.out.println("Default Adapter: " +
	///                      OneWireAccessProvider.getProperty("onewire.adapter.default"));
	///  System.out.println("Default Port: " +
	///                      OneWireAccessProvider.getProperty("onewire.port.default"));
	/// </CODE> </PRE>
	/// </DL>
	/// 
	/// </para>
	/// </summary>
	/// <seealso cref= com.dalsemi.onewire.adapter.DSPortAdapter
	/// 
	/// @version    0.00, 30 August 2000
	/// @author     DS </seealso>
	public class OneWireAccessProvider
	{

	   /// <summary>
	   /// Smart default port
	   /// </summary>
	   //TODO private static string smartDefaultPort = "COM1";

	   /// <summary>
	   /// Override adapter variables
	   /// </summary>
	   private static bool useOverrideAdapter = false;
	   private static DSPortAdapter overrideAdapter = null;

	   /// <summary>
	   /// System Version String
	   /// </summary>
	   private const string owapi_version = "1.10";


	   /// <summary>
	   /// Don't allow anyone to instantiate.
	   /// </summary>
	   private OneWireAccessProvider()
	   {
	   }

	   /// <summary>
	   /// Returns a version string, representing the release number on official releases,
	   /// or release number and release date on incrememental releases.
	   /// </summary>
	   /// <returns> Current OneWireAPI version </returns>
	   public static string Version
	   {
		   get
		   {
			  return owapi_version;
		   }
	   }

	   /// <summary>
	   /// Main method returns current version info, and default adapter setting.
	   /// </summary>
	   /// <param name="args"> cmd-line arguments, ignored for now. </param>
	   public static void Main(string[] args)
	   {
		  Debug.WriteLine("1-Wire API for Java (Desktop), v" + owapi_version);
		  Debug.WriteLine("Copyright (C) 1999-2006 Dallas Semiconductor Corporation, All Rights Reserved.");
		  Debug.WriteLine("");
		  Debug.WriteLine("Default Adapter: " + getProperty("onewire.adapter.default"));
		  Debug.WriteLine("   Default Port: " + getProperty("onewire.port.default"));
		  Debug.WriteLine("");
		  Debug.WriteLine("Download latest API and examples from:");
		  Debug.WriteLine("http://www.maxim-ic.com/products/ibutton/software/1wire/1wire_api.cfm");
		  Debug.WriteLine("");
	   }

	   /// <summary>
	   /// Gets an <code>Enumeration</code> of all 1-Wire
	   /// adapter types supported.  Using this enumeration with the port enumeration for
	   /// each adapter, a search can be done to find all available hardware adapters.
	   /// </summary>
	   /// <returns>  <code>Enumeration</code> of <code>DSPortAdapters</code> in the system </returns>
	   public static System.Collections.IEnumerator enumerateAllAdapters()
	   {
		  ArrayList adapter_vector = new ArrayList(3);
		  DSPortAdapter adapter_instance;
		  Type adapter_class;
		  string class_name = null;
		  bool TMEX_loaded = false;
		  bool serial_loaded = false;

		  // check for override
		  if (useOverrideAdapter)
		  {
			 adapter_vector.Add(overrideAdapter);
			 return (adapter_vector.GetEnumerator());
		  }

		  // only try native TMEX if on x86 Windows platform
		  if ((System.Environment.GetEnvironmentVariable("os.arch").IndexOf("86") != -1) && 
              (System.Environment.GetEnvironmentVariable("os.name").IndexOf("Windows") != -1))
		  {
			 // loop through the TMEX adapters
			 for (int port_type = 0; port_type <= 15; port_type++)
			 {

				// try to load the adapter classes
				try
				{
				   adapter_instance = (DSPortAdapter)(new com.dalsemi.onewire.adapter.TMEXAdapter(port_type));

				   // only add it if it has some ports
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				   if (adapter_instance.PortNames.Current != null) //.hasMoreElements()
                   {
					  adapter_vector.Add(adapter_instance);
					  TMEX_loaded = true;
				   }
				}
				catch (System.Exception)
				{
				   // DRAIN
				}
			 }
		  }

		  // get the pure java adapter
		  try
		  {
			 adapter_class = Type.GetType("com.dalsemi.onewire.adapter.USerialAdapter");
             adapter_instance = (DSPortAdapter) Activator.CreateInstance(adapter_class);

			 // check if has any ports (common javax.comm problem)
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			 if (!(adapter_instance.PortNames.Current != null)) //TODO .hasMoreElements()
             {
				if (!TMEX_loaded)
				{
				   Debug.WriteLine("Warning: serial communications API not setup properly, no ports in enumeration ");
				   Debug.WriteLine("Pure-Java DS9097U adapter will not work, not added to adapter enum");
				}
			 }
			 else
			 {
				adapter_vector.Add(adapter_instance);
				serial_loaded = true;
			 }
		  }
		  catch (System.TypeLoadException e)
		  {
			 if (!TMEX_loaded)
			 {
				Debug.WriteLine("");
				Debug.WriteLine("WARNING: Could not load serial comm API for pure-Java DS9097U adapter: " + e);
				Debug.WriteLine("This message can be safely ignored if you are using TMEX Drivers or");
				Debug.WriteLine("the NetAdapter to connect to the 1-Wire Network.");
				Debug.WriteLine("");
			 }
		  }
		  catch (System.MemberAccessException e)
		  {
			 if (!TMEX_loaded)
			 {
				Debug.WriteLine("");
				Debug.WriteLine("WARNING: Could not load serial comm API for pure-Java DS9097U adapter: " + e);
				Debug.WriteLine("This message can be safely ignored if you are using TMEX Drivers or");
				Debug.WriteLine("the NetAdapter to connect to the 1-Wire Network.");
				Debug.WriteLine("");
			 }
		  }
		  catch (System.Exception)
		  {
			 // DRAIN
		  }

		  if (!TMEX_loaded && !serial_loaded)
		  {
			 Debug.WriteLine("");
			 Debug.WriteLine("Standard drivers for 1-Wire are not found.");
			 Debug.WriteLine("Please download the latest drivers from http://www.ibutton.com ");
			 Debug.WriteLine("Or install RXTX Serial Communications API from http://www.rxtx.org ");
			 Debug.WriteLine("");
		  }

		  // get the network adapter
		  try
		  {
			 adapter_class = Type.GetType("com.dalsemi.onewire.adapter.NetAdapter");
             adapter_instance = (DSPortAdapter)Activator.CreateInstance(adapter_class);
             adapter_vector.Add(adapter_instance);
		  }
		  catch (System.MemberAccessException e)
		  {
			 Debug.WriteLine("Warning: Could not load NetAdapter: " + e);
		  }
		  catch (System.Exception)
		  {
             Debugger.Break();
			 // DRAIN
		  }

		  // get adapters from property file with keys 'onewire.register.adapter0-15'
		  try
		  {
			 // loop through the possible registered adapters
			 for (int reg_num = 0; reg_num <= 15; reg_num++)
			 {
				class_name = getProperty("onewire.register.adapter" + reg_num);

				// done if no property by that name
				if (string.ReferenceEquals(class_name, null))
				{
				   break;
				}

				// add it to the enum
				adapter_class = Type.GetType(class_name);
                adapter_instance = (DSPortAdapter) Activator.CreateInstance(adapter_class);

				adapter_vector.Add(adapter_instance);
			 }
		  }
		  catch (System.MethodAccessException)
		  {
			 Debug.WriteLine("Warning: Adapter \"" + class_name + "\" was registered in " + "properties file, but the class could not be loaded");
		  }
		  catch (System.TypeLoadException)
		  {
			 Debug.WriteLine("Adapter \"" + class_name + "\" was registered in properties file, " + " but the class was not found");
		  }
		  catch (System.Exception)
		  {
			 // DRAIN
		  }

		  // check for no adapters
		  if (adapter_vector.Count == 0)
		  {
			 Debug.WriteLine("No 1-Wire adapter classes found");
		  }

		  return (adapter_vector.GetEnumerator());
	   }

	   /// <summary>
	   /// Finds, opens, and verifies the specified adapter on the
	   /// indicated port.
	   /// </summary>
	   /// <param name="adapterName"> string name of the adapter (match to result
	   ///             of call to getAdapterName() method in DSPortAdapter) </param>
	   /// <param name="portName"> string name of the port used in the method
	   ///             selectPort() in DSPortAdapter
	   /// </param>
	   /// <returns>  <code>DSPortAdapter</code> if adapter present
	   /// </returns>
	   /// <exception cref="OneWireIOException"> when communcation with the adapter fails </exception>
	   /// <exception cref="OneWireException"> when the port or adapter not present </exception>
	   public static DSPortAdapter getAdapter(string adapterName, string portName)
	   {
		  DSPortAdapter adapter , found_adapter = null;

		  // check for override
		  if (useOverrideAdapter)
		  {
			 return overrideAdapter;
		  }

		  // enumerature through available adapters to find the correct one
		  for (System.Collections.IEnumerator adapter_enum = enumerateAllAdapters(); adapter_enum.MoveNext();)
		  {
			 // cast the enum as a DSPortAdapter
			 adapter = (DSPortAdapter) adapter_enum.Current;

			 // see if this is the type of adapter we want
			 if ((found_adapter != null) || (!adapter.AdapterName.Equals(adapterName)))
			 {
				// not this adapter, then just cleanup
				try
				{
				   adapter.freePort();
				}
				catch (System.Exception)
				{
				   // DRAIN
				}
				continue;
			 }

			 // attempt to open and verify the adapter
			 if (adapter.selectPort(portName))
			 {
				adapter.beginExclusive(true);

				try
				{
				   // check for the adapter
				   if (adapter.adapterDetected())
				   {
					  found_adapter = adapter;
				   }
				   else
				   {

					  // close the port just opened
					  adapter.freePort();

					  throw new OneWireException("Port found \"" + portName + "\" but Adapter \"" + adapterName + "\" not detected");
				   }
				}
				finally
				{
				   adapter.endExclusive();
				}
			 }
			 else
			 {
				throw new OneWireException("Specified port \"" + portName + "\" could not be selected for adapter \"" + adapterName + "\"");
			 }
		  }

		  // if adapter found then return it
		  if (found_adapter != null)
		  {
			 return found_adapter;
		  }

		  // adapter by that name not found
		  throw new OneWireException("Specified adapter name \"" + adapterName + "\" is not known");
	   }

	   /// <summary>
	   /// Finds, opens, and verifies the default adapter and
	   /// port.  Looks for the default adapter/port in the following locations:
	   /// <para>
	   /// <ul>
	   /// <li> Use adapter/port in System.properties for onewire.adapter.default,
	   ///      and onewire.port.default properties tags.</li>
	   /// <li> Use adapter/port from onewire.properties file in current directory
	   ///      or < java.home >/lib/ (Desktop) or /etc/ (TINI)</li>
	   /// <li> Use smart default
	   ///      <ul>
	   ///      <li> Desktop
	   ///           <ul>
	   ///           <li> First, TMEX default (Win32 only)
	   ///           <li> Second, if TMEX not present, then DS9097U/(first serial port)
	   ///           </ul>
	   ///      <li> TINI, TINIExternalAdapter on port serial1
	   ///      </ul>
	   /// </ul>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <returns>  <code>DSPortAdapter</code> if default adapter present
	   /// </returns>
	   /// <exception cref="OneWireIOException"> when communcation with the adapter fails </exception>
	   /// <exception cref="OneWireException"> when the port or adapter not present </exception>
	   public static DSPortAdapter DefaultAdapter
	   {
		   get
		   {
			  if (useOverrideAdapter)
			  {
				  return overrideAdapter;
			  }
    
			  return getAdapter(getProperty("onewire.adapter.default"), getProperty("onewire.port.default"));
		   }
	   }

        /// <summary>
        /// Gets the specfied onewire property.
        /// Looks for the property in the following locations:
        /// <para>
        /// <ul>
        /// <li> In System.properties
        /// <li> In onewire.properties file in current directory
        ///      or < java.home >/lib/ (Desktop) or /etc/ (TINI)
        /// <li> 'smart' default if property is 'onewire.adapter.default'
        ///      or 'onewire.port.default'
        /// </ul>
        /// 
        /// </para>
        /// </summary>
        /// <param name="propName"> string name of the property to read
        /// </param>
        /// <returns>  <code>String</code> representing the property value or <code>null</code> if
        ///          it could not be found (<code>onewire.adapter.default</code> and
        ///          <code>onewire.port.default</code> may
        ///          return a 'smart' default even if property not present) </returns>
        public static string getProperty(string propName)
        {
            try
            {
                if (useOverrideAdapter)
                {
                    if (propName.Equals("onewire.adapter.default"))
                    {
                        return overrideAdapter.AdapterName;
                    }
                    if (propName.Equals("onewire.port.default"))
                    {
                        return overrideAdapter.PortName;
                    }
                }
            }
            catch (System.Exception)
            {
                //just drain it and let the normal method run...
            }

            //Properties onewire_properties = new Properties();
            //System.IO.FileStream prop_file = null;
            string ret_str = null;
            DSPortAdapter adapter_instance;
            Type adapter_class;

            // try system properties
            try
            {
                ret_str = System.Environment.GetEnvironmentVariable(propName);
            }
            catch (System.Exception)
            {
                ret_str = null;
            }

            // if defaults not found then try onewire.properties file
            if (string.ReferenceEquals(ret_str, null))
            {

                // attempt to open the onewire.properties file in two locations
                // .\onewire.properties (Local) or <java.home>\lib\onewire.properties (Roaming)
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                ret_str = (string)localSettings.Values[propName];

                // check to see if we now have the value
                if (string.ReferenceEquals(ret_str, null))
                {
                    var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                    ret_str = (string)roamingSettings.Values[propName];
                }

                // if defaults still not found then check TMEX default
                if (string.ReferenceEquals(ret_str, null))
                {
                    try
                    {
                        if (propName.Equals("onewire.adapter.default"))
                        {
                            ret_str = "TMEXAdapter.DefaultAdapterName";
                        }
                        else if (propName.Equals("onewire.port.default"))
                        {
                            ret_str = "TMEXAdapter.DefaultPortName";
                        }

                        // if did not get real string then null out
                        if (!string.ReferenceEquals(ret_str, null))
                        {
                            if (ret_str.Length <= 0)
                            {
                                ret_str = null;
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // DRAIN
                    }
                }

                // if STILL not found then just pick DS9097U on 'smartDefaultPort'
                if (string.ReferenceEquals(ret_str, null))
                {
                    if (propName.Equals("onewire.adapter.default"))
                    {
                        ret_str = "DS9097U";
                    }
                    else if (propName.Equals("onewire.port.default"))
                    {
                        try
                        {
                            adapter_class = Type.GetType("com.dalsemi.onewire.adapter.USerialAdapter");
                            adapter_instance = (DSPortAdapter)Activator.CreateInstance(adapter_class);

                            // check if has any ports (common javax.comm problem)
                            //JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                            //				   if (adapter_instance.PortNames.hasMoreElements())
                            //				   {
                            ////JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                            //					  ret_str = (string) adapter_instance.PortNames.nextElement();
                            //				   }
                        }
                        catch (System.Exception)
                        {
                            // DRAIN
                        }
                    }
                }
            }

            return ret_str;
        }

        /// <summary>
        /// Sets an overriding adapter.  This adapter will be returned from
        /// getAdapter and getDefaultAdapter despite what was requested.
        /// </summary>
        /// <param name="adapter"> adapter to be the override
        /// </param>
        /// <seealso cref=    #getAdapter </seealso>
        /// <seealso cref=    #getDefaultAdapter </seealso>
        /// <seealso cref=    #clearUseOverridingAdapter </seealso>
        public static DSPortAdapter UseOverridingAdapter
	   {
		   set
		   {
				useOverrideAdapter = true;
				overrideAdapter = value;
		   }
	   }

	   /// <summary>
	   /// Clears the overriding adapter.  The operation of
	   /// getAdapter and getDefaultAdapter will be returned to normal.
	   /// </summary>
	   /// <seealso cref=    #getAdapter </seealso>
	   /// <seealso cref=    #getDefaultAdapter </seealso>
	   /// <seealso cref=    #setUseOverridingAdapter </seealso>
	   public static void clearUseOverridingAdapter()
	   {
			useOverrideAdapter = false;
			overrideAdapter = null;
	   }
	}

}