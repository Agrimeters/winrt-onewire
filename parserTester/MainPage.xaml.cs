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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace parserTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            string[] args = new string[]
            {
                //"parserTester.Resources.a2d.xml",
                "parserTester.Resources.contact.xml",
                "parserTester.Resources.event.xml",
                "parserTester.Resources.humidity.xml",
                "parserTester.Resources.level.xml",
                "parserTester.Resources.switch.xml",
                "parserTester.Resources.thermal.xml",
                "parserTester.Resources.weatherstation.xml",
            };
            Main.Main1(args);
        }
    }
}
