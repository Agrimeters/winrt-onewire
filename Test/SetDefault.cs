using System;
using System.IO;
using System.Collections;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
using OneWireAccessProvider = com.dalsemi.onewire.OneWireAccessProvider;


/// <summary>
/// Utility to set the default 1-Wire adapter and port
/// 
/// @version    0.01, 15 December 2000
/// @author     DS
/// </summary>
public class SetDefault
{

   /// <summary>
   /// Method main
   /// 
   /// </summary>
   /// <param name="args">
   ///  </param>
   public static void Main(string[] args)
   {
	  Properties onewire_properties = new Properties();
	  ArrayList adapters = new ArrayList(3);
	  ArrayList ports = new ArrayList(3);
	  DSPortAdapter adapter;
	  string default_adapter, default_port, port, path;
	  int num;
	  System.IO.FileStream prop_outfile;
	  System.IO.FileStream prop_infile;
	  System.IO.File tst_file;
	  string temp_str, key;

	  // attempt to open the onewire.properties file
      if (System.Environment.GetEnvironmentVariable("os.arch").IndexOf("TINI") != -1)
	  {
		 path = "etc" + File.separator;
	  }
	  else
	  {
		 path = System.Environment.GetEnvironmentVariable("java.home") + File.separator + "lib" + File.separator;
	  }

	  // attempt to open the onewire.properties file
	  try
	  {
		 tst_file = new System.IO.File("onewire.properties");

		 if (tst_file.exists())
		 {
			Debug.WriteLine("");
			Debug.WriteLine("WARNING, onewire.properties file detected in the current directory.");
			Debug.WriteLine("This is NOT the file this application will write but it IS the one");
			Debug.WriteLine("read when attempting to open the default adapter.  This application");
			Debug.WriteLine("writes the file in " + path);
			Debug.WriteLine("To avoid confusion, remove this file from the current directory.");
		 }
	  }
	  catch (Exception)
	  {
		 // DRAIN
	  }

	  // attempt to open the onewire.properties file and print the current defaults
	  try
	  {
		 prop_infile = new System.IO.FileStream(path + "onewire.properties", System.IO.FileMode.Open, System.IO.FileAccess.Read);
		 onewire_properties.load(prop_infile);

		 Debug.WriteLine("");
		 Debug.WriteLine("-----------------------------------------------------------------");
		 Debug.WriteLine("| Current values in '" + path + "onewire.properties':");
		 Debug.WriteLine("-----------------------------------------------------------------");
		 // enumerate through the properties and display
		 for (System.Collections.IEnumerator prop_enum = onewire_properties.keys(); prop_enum.MoveNext();)
		 {
			key = (string)prop_enum.Current;
			Debug.WriteLine(key + "=" + onewire_properties.getProperty(key));
		 }
	  }
	  catch (Exception)
	  {
		 // DRAIN
	  }

	  // menu header
	  Debug.WriteLine("");
	  Debug.WriteLine("-----------------------------------------------------------------");
	  Debug.WriteLine("| Select the new Default Adapter 'onewire.adapter.default'");

	  // clarification for drivers under windows (since (2) DS9097U's)
      if ((System.Environment.GetEnvironmentVariable("os.arch").IndexOf("86") != -1) && 
          (System.Environment.GetEnvironmentVariable("os.name").IndexOf("Windows") != -1))
	  {
		 Debug.WriteLine("|   {} denote native driver");
	  }
	  Debug.WriteLine("-----------------------------------------------------------------");

	  // get the vector of adapters
	  for (System.Collections.IEnumerator adapter_enum = OneWireAccessProvider.enumerateAllAdapters(); adapter_enum.MoveNext();)
	  {

		 // cast the enum as a DSPortAdapter
		 adapter = (DSPortAdapter) adapter_enum.Current;

		 adapters.Add(adapter);
		 Debug.WriteLine("(" + (adapters.Count - 1) + ") " + adapter.AdapterName);
		 try
		 {
			Debug.WriteLine("     ver: " + adapter.AdapterVersion);
			Debug.WriteLine("    desc:" + adapter.PortTypeDescription);
		 }
		 catch (Exception)
		 {
			 ;
		 }
	  }

	  Debug.WriteLine("Enter a number to select the default: ");
	  num = getNumber(0, adapters.Count - 1);

	  // select the adapter
	  adapter = (DSPortAdapter) adapters[num];
	  default_adapter = adapter.AdapterName;

	  Debug.WriteLine("");
	  Debug.WriteLine("-----------------------------------------------------------------");
	  Debug.WriteLine("| Select the new Default Port 'onewire.port.default' on adapter: " + default_adapter);
	  Debug.WriteLine("-----------------------------------------------------------------");

	  // get the ports
	  for (System.Collections.IEnumerator port_enum = adapter.PortNames; port_enum.MoveNext();)
	  {

		 // cast the enum as a String
		 port = (string) port_enum.Current;

		 ports.Add(port);
		 Debug.WriteLine("(" + (ports.Count - 1) + ") " + port);
	  }

	  Debug.WriteLine("Enter a number to select the default: ");
	  num = getNumber(0, ports.Count - 1);

	  // select the port
	  default_port = (string) ports[num];

	  // set to the properities
	  Debug.WriteLine("");
	  Debug.WriteLine("Properties object created");
	  Debug.WriteLine("Attempting to save onewire.properties file");

	  // attempt to open the onewire.properties file
	  try
	  {
		 // remove the two properties we are setting
		 onewire_properties.remove("onewire.adapter.default");
		 onewire_properties.remove("onewire.port.default");

		 // add the new properties
		 onewire_properties.put("onewire.adapter.default", default_adapter);
		 onewire_properties.put("onewire.port.default", default_port);

		 // open the file to write
		 prop_outfile = new System.IO.FileStream(path + "onewire.properties", System.IO.FileMode.Create, System.IO.FileAccess.Write);

		 // enumerate through the properties and write the new file
		 for (System.Collections.IEnumerator prop_enum = onewire_properties.keys(); prop_enum.MoveNext();)
		 {
			key = (string)prop_enum.Current;
			temp_str = key + "=" + onewire_properties.getProperty(key) + "\r" + "\n";
			prop_outfile.WriteByte(temp_str.ToCharArray());
		 }
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }

	  Debug.WriteLine("onewire.properties file saved to " + path + "onewire.properites");
	  Debug.WriteLine("Attempting to open the default adapter on the default port");

	  try
	  {
		 adapter = OneWireAccessProvider.DefaultAdapter;

		 Debug.WriteLine("Success!");
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e);
	  }

	  //TODO Environment.Exit(0);
   }

   /// <summary>
   /// Retrieve user input from the Debug.
   /// </summary>
   /// <param name="min"> minimum number to accept </param>
   /// <param name="max"> maximum number to accept
   /// </param>
   /// <returns> numberic value entered from the Debug. </returns>
   internal static int getNumber(int min, int max)
   {
	  int value = -1;
	  bool fNumber = false;

	  while (fNumber == false)
	  {
		 try
		 {
			string str = getString(1);

			value = int.Parse(str);

			if ((value < min) || (value > max))
			{
			   Debug.WriteLine("Invalid value, range must be " + min + " to " + max);
			   Debug.Write("Please enter value again: ");
			}
			else
			{
			   fNumber = true;
			}
		 }
		 catch (System.FormatException e)
		 {
			Debug.WriteLine("Invalid Numeric Value: " + e.ToString());
			Debug.Write("Please enter value again: ");
		 }
	  }

	  return value;
   }

   /// <summary>
   /// InputStream to read lines
   /// </summary>
   private static System.IO.StreamReader dis = new System.IO.StreamReader(System.in);

   /// <summary>
   /// Retrieve user input from the Debug.
   /// </summary>
   /// <param name="minLength"> minumum length of string
   /// </param>
   /// <returns> string entered from the Debug. </returns>
   internal static string getString(int minLength)
   {
	  string str;
	  bool done = false;

	  try
	  {
		 do
		 {
			str = dis.ReadLine();
			if (str.Length < minLength)
			{
			   Debug.Write("String too short try again:");
			}
			else
			{
			   done = true;
			}
		 } while (!done);

		 return str;
	  }
	  catch (System.IO.IOException e)
	  {
		 Debug.WriteLine("Error in reading from console: " + e);
	  }

	  return "";
   }

}
