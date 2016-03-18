using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire;
using com.dalsemi.onewire.application.tag;

public class Main
{
	public Main(string filename)
	{
		try
		{
			DSPortAdapter adapter;
			adapter = OneWireAccessProvider.DefaultAdapter;
			TAGParser p = new TAGParser(adapter);

            Debug.WriteLine("starting...");

            OWCluster maincluster = p.parse(loadResourceFile("parserTester.8x1.xml"));
			Debug.WriteLine("Clusters: ");

			System.Collections.IEnumerator clusters = p.getClusters(loadResourceFile("parserTester.8x1.xml"));
			while (clusters.MoveNext())
			{
				OWCluster cluster = (OWCluster)clusters.Current;
				Debug.WriteLine("    cluster:" + cluster.Description + " in cluster " + cluster.ContainerCluster);
				for (System.Collections.IEnumerator e = p.getDevices(cluster); e.MoveNext();)
				{
					OWDevice dev = (OWDevice)e.Current;
					Debug.WriteLine("        device:" + dev.Description + " in cluster " + dev.Cluster);
				}
			}

			OWSwitch[] Switches = new OWSwitch[4];
			System.Collections.IEnumerator sw = p.getDevices(loadResourceFile("parserTester.8x1.xml"), new OWSwitchFilter());

			while (sw.MoveNext())
			{
				OWSwitch Switch = (OWSwitch)sw.Current;
				if (Switch.Description.Equals("LED1"))
				{
					Switches[0] = Switch;
				}
				if (Switch.Description.Equals("LED2"))
				{
					Switches[1] = Switch;
				}
				if (Switch.Description.Equals("LED3"))
				{
					Switches[2] = Switch;
				}
				if (Switch.Description.Equals("LED4 AND BUZZER"))
				{
					Switches[3] = Switch;
				}
			}

			OWLevelSensor[] Sensors = new OWLevelSensor[4];
			for (System.Collections.IEnumerator e = p.getDevices(loadResourceFile("parserTester.8x1.xml"), new OWLevelSensorFilter()); e.MoveNext();)
			{
				OWLevelSensor Sensor = (OWLevelSensor)e.Current;
				Debug.WriteLine(Sensor.Description);
				if (Sensor.Description.Equals("Push-Button 1"))
				{
					Sensors[0] = Sensor;
				}
				if (Sensor.Description.Equals("Push-Button 2"))
				{
					Sensors[1] = Sensor;
				}
				if (Sensor.Description.Equals("Push-Button 3"))
				{
					Sensors[2] = Sensor;
				}
				if (Sensor.Description.Equals("Push-Button 4"))
				{
					Sensors[3] = Sensor;
				}
			}

			while (true)
			{
				for (int i = 0; i < 4; i++)
				{
					((OWSwitch)Switches[i]).toggle(0);
					if (!Sensors[i].Level)
					{
						Debug.WriteLine(Sensors[i].Description + " active.");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Exception " + ex.GetType().FullName + ": " + ex.Message);
			Debug.WriteLine(ex.ToString());
			Debug.Write(ex.StackTrace);
		}
	}

	public static void Main1(string[] args)
	{
		if (args.Length != 1)
		{
			Debug.WriteLine("usage: parserTester XMLfile");
            return;
		}
		Main test = new Main(args[0]);
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

}
