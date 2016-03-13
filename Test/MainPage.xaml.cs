using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//using Windows.Devices.I2c; //IoT only...
using Windows.Devices.Usb;
using Windows.Devices.Enumeration;
using System.Reflection;
using Windows.Storage.Streams;
using System.Resources;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            string[] args = null;
            //string[] args = { "DS9097U", "COM8" };
            //string[] args = { "UsbAdapter", @"USB\VID_04FA&PID_2490\6&f0f8e95&0&6" };
            //string[] args = { "NetAdapter", @"192.168.1.187:6161", };

            // Print default access provider settings
            com.dalsemi.onewire.OneWireAccessProvider.Main(args);

            ReadTemp.Main1(args);
        }
    }
}
