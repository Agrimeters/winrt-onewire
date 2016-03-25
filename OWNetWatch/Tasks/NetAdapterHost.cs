using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Windows.ApplicationModel.Background;
using Windows.Storage;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.logging;


namespace BackgroundTasks
{
    public sealed class StartNetAdapterHostTask : IBackgroundTask
    {
        private static bool DEBUG = false;

        private NetAdapterHost m_host;

        private string m_adapterName = null, m_adapterPort = null;
        private bool m_multithread = true, m_multicast = true;
        private string m_secret = NetAdapterConstants_Fields.DEFAULT_SECRET;
        private string m_listenPort = NetAdapterConstants_Fields.DEFAULT_PORT;
        private int m_mcPort = NetAdapterConstants_Fields.DEFAULT_MULTICAST_PORT;
        private string m_mcGroup = NetAdapterConstants_Fields.DEFAULT_MULTICAST_GROUP;

        private BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            SetupLogging();
            GetProperties();
            SetupServer();

            m_host.OnDispose += (sender, e) => deferral.Complete();

            StartServer();
        }

        private void SetupLogging()
        {
            string enable = OneWireAccessProvider.getProperty("onewire.debug");
            if (!string.ReferenceEquals(enable, null) && enable.ToLower().Equals("true"))
            {
                DEBUG = true;
            }
            else
            {
                DEBUG = false;
            }

            string logFile = OneWireAccessProvider.getProperty("onewire.debug.logfile");
            if (!string.ReferenceEquals(logFile, null))
            {
                if (DEBUG)
                {
                    // ignore any absolute path provided, only use filename
                    string[] strtok = logFile.Split(new char[] { '\\' });
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    string logFilePath = localFolder.Path + "\\" + strtok[strtok.Length - 1].Trim();

                    Debug.WriteLine("Log location: " + ApplicationData.Current.LocalFolder.Path);
                    EventListener verboseListener = new StorageFileEventListener(logFile);
                    verboseListener.EnableEvents(OneWireEventSource.Log, EventLevel.Verbose);
                }
                else
                {
                    Debug.WriteLine("Log location: " + ApplicationData.Current.LocalFolder.Path);
                    EventListener verboseListener = new StorageFileEventListener("log");
                    verboseListener.EnableEvents(OneWireEventSource.Log, EventLevel.Error);
                }
            }
        }

        private void GetProperties()
        {
            string test = OneWireAccessProvider.getProperty("onewire.adapter.default");
            if (!string.ReferenceEquals(test, null))
            {
                m_adapterName = test;
            }

            test = OneWireAccessProvider.getProperty("onewire.port.default");
            if (!string.ReferenceEquals(test, null))
            {
                m_adapterPort = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.ListenPort");
            if (!string.ReferenceEquals(test, null))
            {
                m_listenPort = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Multithread");
            if (!string.ReferenceEquals(test, null))
            {
                m_multithread = Convert.ToBoolean(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Secret");
            if (!string.ReferenceEquals(test, null))
            {
                m_secret = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Multicast");
            if (!string.ReferenceEquals(test, null))
            {
                m_multicast = Convert.ToBoolean(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.MulticastPort");
            if (!string.ReferenceEquals(test, null))
            {
                m_mcPort = int.Parse(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.MulticastGroup");
            if (!string.ReferenceEquals(test, null))
            {
                m_mcGroup = test;
            }
        }

        private void SetupServer()
        {
            DSPortAdapter adapter;

            if (string.ReferenceEquals(m_adapterName, null) || string.ReferenceEquals(m_adapterPort, null))
            {
                adapter = OneWireAccessProvider.DefaultAdapter;
            }
            else
            {
                adapter = OneWireAccessProvider.getAdapter(m_adapterName, m_adapterPort);
            }

            OneWireEventSource.Log.Info(
                "  Adapter Name: " + adapter.AdapterName + "\r\n" + 
                "  Adapter Name: " + adapter.PortName + "\r\n" + 
                "  Host Listen Port: " + m_listenPort + "\r\n" + 
                "  Multithreaded Host: " + (m_multithread ? "Enabled" : "Disabled") + "\r\n" + 
                "  Shared Secret: '" + m_secret + "'\r\n" + 
                "  Multicast: " + (m_multicast ? "Enabled" : "Disabled") + "\r\n" + 
                "  Multicast Port: " + m_mcPort + "\r\n" + 
                "  Multicast Group: " + m_mcGroup + "\r\n");

            // Create the NetAdapterHost
            m_host = new NetAdapterHost(adapter, m_listenPort, m_multithread);

            // set the shared secret
            m_host.Secret = m_secret;

            if (m_multicast)
            {
                OneWireEventSource.Log.Info("Starting Multicast Listener");
                m_host.createMulticastListener(m_mcPort, m_mcGroup);
            }
        }

        private void StartServer()
        {
            OneWireEventSource.Log.Info("Starting NetAdapter Host");
            m_host.StartServer();
        }
    }
}
