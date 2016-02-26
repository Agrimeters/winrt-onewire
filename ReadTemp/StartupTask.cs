using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Threading;
using System.Diagnostics;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace ReadTemp
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //
            // Create the deferral by requesting it from the task instance.
            //
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //
            // Call asynchronous method(s) using the await keyword.
            //
            string[] args = { "DS9097U_COM1" }; //"DS2490_USB1" "DS2482_I2C1"

            // Print default access provider settings
            com.dalsemi.onewire.OneWireAccessProvider.Main(args);

            ReadTemp.Main(args);

            //
            // Once the asynchronous method(s) are done, close the deferral.
            //
            deferral.Complete();
        }

    }
}
