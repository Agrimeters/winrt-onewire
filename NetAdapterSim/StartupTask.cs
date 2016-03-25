using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace NetAdapterSim
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
            string[] args = new string[] { "NetStartSim", @"C:\Data\Users\DefaultAccount\AppData\Local\Packages\NetAdapterSim-uwp_rmc58jpf3676r\LocalState\log.txt", "true" };
            com.dalsemi.onewire.adapter.NetAdapterSim.Main(args);
        }
    }
}
