using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire;
using com.dalsemi.onewire.application.tag;
using com.dalsemi.onewire.utils;

public class Main
{
    /// <summary>
    /// Vector of devices that have been 'tagged' </summary>
    public static List<TaggedDevice> taggedDevices;

    /// <summary>
    /// 1-Wire search paths </summary>
    public static List<OWPath> paths;

    public Main(string[] files)
	{
        try
        {
            DSPortAdapter adapter;
            adapter = OneWireAccessProvider.DefaultAdapter;
            TAGParserEx p = new TAGParserEx(adapter);

            Debug.WriteLine("starting...");

            foreach(var file in files)
            {
                // attempt to parse it
                parseStream(p, loadResourceFile(file), new OWPath(adapter), true);
            }
        }
		catch (Exception ex)
		{
			Debug.WriteLine("Exception " + ex.GetType().FullName + ": " + ex.Message);
			Debug.WriteLine(ex.ToString());
			Debug.Write(ex.StackTrace);
		}
	}


    /// <summary>
    /// Parse the provided XML input stream with the provided parser.
    /// Gather the new TaggedDevices and OWPaths into the global vectors
    /// 'taggedDevices' and 'paths'.
    /// </summary>
    /// <param name="parser"> parser to parse 1-Wire XML files </param>
    /// <param name="stream">  XML file stream </param>
    /// <param name="currentPath">  OWPath that was opened to get to this file </param>
    /// <param name="autoSpawnFrames"> true if new DeviceFrames are spawned with
    ///        new taggedDevices discovered </param>
    /// <returns> true an XML file was successfully parsed. </returns>
    public static bool parseStream(TAGParserEx parser, Stream stream, OWPath currentPath, bool autoSpawnFrames)
    {
        bool rslt = false;
        OWPath tempPath;

        try
        {
            // parse the file
            List<TaggedDevice> new_devices = parser.parse(stream);

            // get the new paths
            List<OWPath> new_paths = parser.OWPaths;

            Debug.WriteLine("Success, XML parsed with " + new_devices.Count + " devices " + new_paths.Count + " paths");

            // add the new devices to the old list
            for (int i = 0; i < new_devices.Count; i++)
            {
                TaggedDevice current_device = (TaggedDevice)new_devices[i];

                // update this devices OWPath depending on where we got it if its OWPath is empty
                tempPath = current_device.OWPath;
                if (!tempPath.AllOWPathElements.MoveNext())
                {
                    // replace this devices path with the current path
                    tempPath.copy(currentPath);
                    current_device.OWPath = tempPath;
                }

                // add the new device to the device list
                taggedDevices.Add(current_device);
            }

            // add the new paths
            for (int i = 0; i < new_paths.Count; i++)
            {
                paths.Add(new_paths[i]);
            }

            rslt = true;

        }
        catch (org.xml.sax.SAXException se)
        {
            Debug.WriteLine("XML error: " + se);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error: " + e);
        }

        return rslt;
    }

    /// <summary>
    /// Loads resource file to be used as Input Stream to drive program
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Stream loadResourceFile(string file)
    {
        try
        {
            Assembly asm = typeof(parserTester.MainPage).GetTypeInfo().Assembly;
            return asm.GetManifestResourceStream(file);
        }
        catch (Exception)
        {
            Debug.WriteLine("Can't find resource: " + file);
        }
        return null;
    }


    public static void Main1(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.WriteLine("usage: parserTester XMLfile");
            return;
        }
       Main test = new Main(args);
    }
}
