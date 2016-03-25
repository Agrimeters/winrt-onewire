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
    public sealed class NetAdapterHostTask : IBackgroundTask
    {
        private static bool DEBUG = false;

        private NetAdapterHost _host;

        private string _adapterName = null, _adapterPort = null;
        private bool _multithread = true, _multicast = true;
        private string _secret = NetAdapterConstants_Fields.DEFAULT_SECRET;
        private string _listenPort = NetAdapterConstants_Fields.DEFAULT_PORT;
        private int _mcPort = NetAdapterConstants_Fields.DEFAULT_MULTICAST_PORT;
        private string _mcGroup = NetAdapterConstants_Fields.DEFAULT_MULTICAST_GROUP;

        private BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            SetupLogging();
            GetProperties();
            SetupServer();

            _host.OnDispose += (sender, e) => deferral.Complete();

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
                    EventListener listener = new StorageFileEventListener(logFile);
                    listener.EnableEvents(OneWireEventSource.Log, EventLevel.Verbose);
                }
                else
                {
                    Debug.WriteLine("Log location: " + ApplicationData.Current.LocalFolder.Path);
                    EventListener listener = new StorageFileEventListener("log");
                    listener.EnableEvents(OneWireEventSource.Log, EventLevel.Informational);
                }
            }
            else
            {
                Debug.WriteLine("Log location: " + ApplicationData.Current.LocalFolder.Path);
                EventListener listener = new StorageFileEventListener("log");
                listener.EnableEvents(OneWireEventSource.Log, EventLevel.Informational);
            }
        }

        private void GetProperties()
        {
            string test = OneWireAccessProvider.getProperty("onewire.adapter.default");
            if (!string.ReferenceEquals(test, null))
            {
                _adapterName = test;
            }

            test = OneWireAccessProvider.getProperty("onewire.port.default");
            if (!string.ReferenceEquals(test, null))
            {
                _adapterPort = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.ListenPort");
            if (!string.ReferenceEquals(test, null))
            {
                _listenPort = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Multithread");
            if (!string.ReferenceEquals(test, null))
            {
                _multithread = Convert.ToBoolean(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Secret");
            if (!string.ReferenceEquals(test, null))
            {
                _secret = test;
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.Multicast");
            if (!string.ReferenceEquals(test, null))
            {
                _multicast = Convert.ToBoolean(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.MulticastPort");
            if (!string.ReferenceEquals(test, null))
            {
                _mcPort = int.Parse(test);
            }

            test = OneWireAccessProvider.getProperty("NetAdapter.MulticastGroup");
            if (!string.ReferenceEquals(test, null))
            {
                _mcGroup = test;
            }
        }

        private void SetupServer()
        {
            DSPortAdapter adapter;

            if (string.ReferenceEquals(_adapterName, null) || string.ReferenceEquals(_adapterPort, null))
            {
                adapter = OneWireAccessProvider.DefaultAdapter;
            }
            else
            {
                adapter = OneWireAccessProvider.getAdapter(_adapterName, _adapterPort);
            }

            OneWireEventSource.Log.Info(
                "  Adapter Name: " + adapter.AdapterName + "\r\n" + 
                "  Adapter Name: " + adapter.PortName + "\r\n" + 
                "  Host Listen Port: " + _listenPort + "\r\n" + 
                "  Multithreaded Host: " + (_multithread ? "Enabled" : "Disabled") + "\r\n" + 
                "  Shared Secret: '" + _secret + "'\r\n" + 
                "  Multicast: " + (_multicast ? "Enabled" : "Disabled") + "\r\n" + 
                "  Multicast Port: " + _mcPort + "\r\n" + 
                "  Multicast Group: " + _mcGroup + "\r\n");

            // Create the NetAdapterHost
            _host = new NetAdapterHost(adapter, _listenPort, _multithread);

            // set the shared secret
            _host.Secret = _secret;

            if (_multicast)
            {
                OneWireEventSource.Log.Info("Starting Multicast Listener");
                _host.createMulticastListener(_mcPort, _mcGroup);
            }
        }

        private void StartServer()
        {
            OneWireEventSource.Log.Info("Starting NetAdapter Host");
            _host.StartServer();
        }
    }
}
