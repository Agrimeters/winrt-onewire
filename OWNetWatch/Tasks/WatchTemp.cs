using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.System.Threading;

namespace Tasks
{
    using com.dalsemi.onewire.adapter;
    using com.dalsemi.onewire.container;
    using com.dalsemi.onewire.utils;

    public sealed class WatchTemp : IBackgroundTask
    {
        TemperatureContainer _tc;
        OneWireContainer _owc;
        DSPortAdapter _adapter;
        OWPath _path;
        string _address;

        BackgroundTaskCancellationReason _cancelReason = BackgroundTaskCancellationReason.Abort;
        volatile bool _cancelRequested = false;
        BackgroundTaskDeferral _deferral = null;
        ThreadPoolTimer _periodicTimer = null;
        IBackgroundTaskInstance _taskInstance = null;

        //
        // The Run method is the entry point of a background task.
        //
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var settings = ApplicationData.Current.LocalSettings;
            _tc = (TemperatureContainer)settings.Values["TemperatureContainer"];
            _path = (OWPath)settings.Values["OWPath"];

            if ((_tc == null) || (_path == null))
            {
                Debugger.Break();
                return;
            }

            // extract out the address and adapter
            _owc = (OneWireContainer)_tc;
            _address = _owc.AddressAsString;
            _adapter = _owc.Adapter;

            //
            // Associate a cancellation handler with the background task.
            //
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            //
            // Get the deferral object from the task instance, and take a reference to the taskInstance;
            //
            _deferral = taskInstance.GetDeferral();
            _taskInstance = taskInstance;

            _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), TimeSpan.FromSeconds(2));
        }

        //
        // Handles background task cancellation.
        //
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
            _cancelReason = reason;

            Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested...");
        }

        private void PeriodicTimerCallback(ThreadPoolTimer timer)
        {
            if (_cancelRequested == false)
            {
                // get exclusive use of port
                _adapter.beginExclusive(true);

                // open a path to the temp device
                _path.open();

                // check if present
                if (_owc.Present)
                {

                    // read the temp device
                    byte[] state = _tc.readDevice();

                    _tc.doTemperatureConvert(state);

                    state = _tc.readDevice();

                    Debug.WriteLine("Temperature of " + _address + " is " + _tc.getTemperature(state) + " C");
                }
                else
                {
                    Debug.WriteLine("Device " + _address + " not present so stopping thread");
                }

                // close the path to the device
                _path.close();

                // release exclusive use of port
                _adapter.endExclusive();
            }
            else
            {
                _periodicTimer.Cancel();

                var settings = ApplicationData.Current.LocalSettings;
                var key = _taskInstance.Task.Name;

                //
                // Write to LocalSettings to indicate that this background task ran.
                //
                settings.Values[key] = "Canceled with reason: " + _cancelReason.ToString();
                Debug.WriteLine("Background " + _taskInstance.Task.Name + settings.Values[key]);

                //
                // Indicate that the background task has completed.
                //
                _deferral.Complete();
            }
        }
    }
}
