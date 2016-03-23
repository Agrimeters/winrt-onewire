using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace com.dalsemi.onewire.logging
{
    /// <summary>
    /// We are intercepting the ETW logging messages and deverting to file.
    /// </summary>
    class StorageFileEventListener : EventListener
    {
        /// <summary>
        /// Storage file to be used to write logs
        /// </summary>
        private StorageFile m_StorageFile = null;

        /// <summary>
        /// Name of the current event listener
        /// </summary>
        private string m_Name;

        /// <summary>
        /// The format to be used by logging.
        /// </summary>
        private string m_Format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tType: {1}\tId: {2}\tMessage: '{3}'";

        private SemaphoreSlim m_SemaphoreSlim = new SemaphoreSlim(1);

        public StorageFileEventListener(string name)
        {
            this.m_Name = name;

            System.Diagnostics.Debug.WriteLine("StorageFileEventListener for {0} has name {1}", GetHashCode(), name);

            AssignLocalFile();
        }

        private async void AssignLocalFile()
        {
            var local = ApplicationData.Current.LocalFolder;
            m_StorageFile = await local.CreateFileAsync(m_Name.Replace(" ", "_") + ".log",
              CreationCollisionOption.OpenIfExists);
        }

        private async void WriteToFile(IEnumerable<string> lines)
        {
            await m_SemaphoreSlim.WaitAsync();

            await Task.Run(async () =>
            {
                try
                {
                    await FileIO.AppendLinesAsync(m_StorageFile, lines);
                }
                catch (Exception ex)
                {
                    // TODO:
                }
                finally
                {
                    m_SemaphoreSlim.Release();
                }
            });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (m_StorageFile == null) return;

            var lines = new List<string>();

            var newFormatedLine = string.Format(m_Format, DateTime.Now, eventData.Level, eventData.EventId, eventData.Payload[0]);

            System.Diagnostics.Debug.WriteLine(newFormatedLine);

            lines.Add(newFormatedLine);

            WriteToFile(lines);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            System.Diagnostics.Debug.WriteLine("OnEventSourceCreated for Listener {0} - {1} got eventSource {2}", GetHashCode(), m_Name, eventSource.Name);
        }
    }
}
