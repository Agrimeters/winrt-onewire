using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.monitor;
using com.dalsemi.onewire.container;
using System.Collections.Generic;

namespace OWNetWatch
{
    class OWNetWatch1 : DeviceMonitorEventListener
    {
        public const string WatchTempBackgroundTaskEntryPoint = "Tasks.WatchTemp";
        public const string WatchTempBackgroundTaskName = "WatchTempTask";
        public static string WatchTempBackgroundTaskProgress = "";
        public static bool WatchTempBackgroundTaskRegistered = false;

        /// <summary>
        /// Main for the OWNetWatch Demo
        /// </summary>
        /// <param name="args"> command line arguments </param>
        public static void Main1(string[] args)
        {
            int delay;

            try
            {
                // get the default adapter  
                DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;

                Debug.WriteLine("");
                Debug.WriteLine("Adapter: " + adapter.AdapterName + " Port: " + adapter.PortName);
                Debug.WriteLine("");

                // clear any previous search restrictions
                adapter.setSearchAllDevices();
                adapter.targetAllFamilies();
                adapter.Speed = DSPortAdapter.SPEED_REGULAR;

                // create the watcher with this adapter
                OWNetWatch1 nw = new OWNetWatch1(adapter);

                // sleep for the specified time
                if (args.Length >= 1)
                {
                    delay = Int32.Parse(args[0]);
                }
                else
                {
                    delay = 20000;
                }

                Debug.WriteLine("Monitor run for: " + delay + "ms");
                Thread.Sleep(delay);

                // clean up
                Debug.WriteLine("Done with monitor run, now cleanup threads");
                nw.killNetWatch();

                // free port used by adapter
                adapter.freePort();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return;
        }

        //--------
        //-------- Variables 
        //--------

        /// <summary>
        /// Network Monitor intance </summary>
        private NetworkDeviceMonitor nm;

        /// <summary>
        /// Vector of temperature watches, used in cleanup </summary>
        private List<BackgroundTaskRegistration> watchers;

        /// <summary>
        /// Container for application settings shared with IBackgroundTask
        /// </summary>
        private ApplicationDataContainer _container;

        //--------
        //-------- Constructor
        //--------

        /// <summary>
        /// Create a 1-Wire Network Watcher
        /// </summary>
        /// <param name="adapter"> for 1-Wire Network to monitor </param>
        public OWNetWatch1(DSPortAdapter adapter)
        {

            // create vector to keep track of temperature watches
            watchers = new List<BackgroundTaskRegistration>();

            // create a network monitor
            nm = new NetworkDeviceMonitor(adapter);

            // add this to the event listers
            nm.addDeviceMonitorEventListener(this);

            var localSettings = ApplicationData.Current.LocalSettings;

            if (!localSettings.Containers.ContainsKey("AppSettings"))
            {
                _container = localSettings.CreateContainer("AppSettings", ApplicationDataCreateDisposition.Always);
            }
            else
            {
                _container = localSettings.Containers["AppSettings"];
            }

            // start the monitor
            var t = Task.Run(() =>
            {
                nm.run();
            });
        }

        /// <summary>
        /// Clean up the threads
        /// </summary>
        public virtual void killNetWatch()
        {
            // kill the network monitor
            nm.killMonitor();

            //
            // Loop through all background tasks and unregister any with WatchTempBackgroundTaskName
            //
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == WatchTempBackgroundTaskName)
                {
                    cur.Value.Unregister(true);
                }
            }
        }

        /// <summary>
        /// Arrival event as a NetworkMonitorEventListener
        /// </summary>
        /// <param name="nme"> NetworkMonitorEvent add </param>
        public virtual void deviceArrival(DeviceMonitorEvent dme)
        {
            for (int i = 0; i < dme.DeviceCount; i++)
            {
                Debug.WriteLine("ADD: " + dme.getPathForContainerAt(i) + dme.getAddressAsStringAt(i));

                // if new devices is a TemperatureContainter the start a thread to read it
                OneWireContainer owc = dme.getContainerAt(i);

                if (owc is TemperatureContainer)
                {
//TODO                    SetValue("TemperatureContainer", owc);
//TODO                    SetValue("OWPath", dme.getPathForContainerAt(i));

                    var t = Task<BackgroundTaskRegistration>.Run(() =>
                    {
                        return RegisterBackgroundTask(WatchTempBackgroundTaskEntryPoint,
                                                      WatchTempBackgroundTaskName,
                                                      new SystemTrigger(SystemTriggerType.TimeZoneChange, false),
                                                      null);
                    });
                    t.Wait();

                    // add to vector for later cleanup
                    watchers.Add(t.Result);
                    t.Result.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
                }
            }
        }

        /// <summary>
        /// Depart event as a NetworkMonitorEventListener
        /// </summary>
        /// <param name="nme"> NetworkMonitorEvent depart </param>
        public virtual void deviceDeparture(DeviceMonitorEvent dme)
        {
            for (int i = 0; i < dme.DeviceCount; i++)
            {
                Debug.WriteLine("REMOVE: " + dme.getPathForContainerAt(i) + dme.getAddressAsStringAt(i));

                // kill the temp watcher
                UnregisterBackgroundTasks(WatchTempBackgroundTaskName);
            }
        }

        /// <summary>
        /// Exception event as a NetworkMonitorEventListener
        /// </summary>
        /// <param name="ex"> Exception </param>
        public virtual void networkException(DeviceMonitorException ex)
        {
            if (ex.Exception is OneWireIOException)
            {
                Debug.Write(".IO.");
            }
            else
            {
                Debug.Write(ex);
            }
            Debug.WriteLine(ex.StackTrace.ToString());
        }

        /// <summary>
        /// Register a background task with the specified taskEntryPoint, name, trigger,
        /// and condition (optional).
        /// </summary>
        /// <param name="taskEntryPoint">Task entry point for the background task.</param>
        /// <param name="name">A name for the background task.</param>
        /// <param name="trigger">The trigger for the background task.</param>
        /// <param name="condition">An optional conditional event that must be true for the task to fire.</param>
        public static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(String taskEntryPoint, String name, IBackgroundTrigger trigger, IBackgroundCondition condition)
        {
            if (TaskRequiresBackgroundAccess(name))
            {
                await BackgroundExecutionManager.RequestAccessAsync();
            }

            var builder = new BackgroundTaskBuilder();

            builder.Name = name;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);

            if (condition != null)
            {
                builder.AddCondition(condition);

                //
                // If the condition changes while the background task is executing then it will
                // be canceled.
                //
                builder.CancelOnConditionLoss = true;
            }

            BackgroundTaskRegistration task = builder.Register();

            UpdateBackgroundTaskStatus(name, true);

            //
            // Remove previous completion status from local settings.
            //
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(name);

            return task;
        }

        /// <summary>
        /// Unregister background tasks with specified name.
        /// </summary>
        /// <param name="name">Name of the background task to unregister.</param>
        public static void UnregisterBackgroundTasks(string name)
        {
            //
            // Loop through all background tasks and unregister any with SampleBackgroundTaskName or
            // SampleBackgroundTaskWithConditionName.
            //
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                Debug.WriteLine("BackgroundTask: " + cur.Value.Name);

                if (cur.Value.Name == name)
                {
                    cur.Value.Unregister(true);
                }
            }

            UpdateBackgroundTaskStatus(name, false);
        }

        /// <summary>
        /// Store the registration status of a background task with a given name.
        /// </summary>
        /// <param name="name">Name of background task to store registration status for.</param>
        /// <param name="registered">TRUE if registered, FALSE if unregistered.</param>
        public static void UpdateBackgroundTaskStatus(String name, bool registered)
        {
            switch (name)
            {
                case WatchTempBackgroundTaskName:
                    WatchTempBackgroundTaskRegistered = registered;
                    break;
            }
        }

        /// <summary>
        /// Determine if task with given name requires background access.
        /// </summary>
        /// <param name="name">Name of background task to query background access requirement.</param>
        public static bool TaskRequiresBackgroundAccess(String name)
        {
#if WINDOWS_PHONE_APP
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Handle background task completion.
        /// </summary>
        /// <param name="task">The task that is reporting completion.</param>
        /// <param name="e">Arguments of the completion report.</param>
        private void OnCompleted(IBackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs args)
        {

        }

        /// <summary>
        /// Writes value to the Local Application settings container
        /// The IBackgroundTask uses this
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void SetValue(string key, object value)
        {
            try
            {
                if (!_container.Values.ContainsKey(key))
                {
                    _container.Values.Add(key, value);
                }
                else
                {
                    _container.Values[key] = value;
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                Debug.WriteLine("This Data type is not allowed");
            }
        }
    }
}
