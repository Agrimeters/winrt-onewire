using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StartNetAdapterHost
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        private DSPortAdapter adapter;
        private NetAdapterHost host;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            adapter = OneWireAccessProvider.DefaultAdapter;

            host = new NetAdapterHost(adapter, true);

            Debug.WriteLine("Starting Multicast Listener");
            host.createMulticastListener();

            Debug.WriteLine("Starting NetAdapter Host");
            host.StartServer();
        }

        ~MainPage()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (host != null)
                    host.Dispose();
            }
        }

    }
}
