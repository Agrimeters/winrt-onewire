using System.Threading;

namespace com.dalsemi.onewire
{
    public class Thread
    {
        public static void Sleep (long ms)
        {
            new ManualResetEvent(false).WaitOne((int)ms);
        }
    }
}
