using System;

namespace com.dalsemi.onewire
{
    public class Thread
    {
        public static void Sleep (long ms)
        {
            new System.Threading.ManualResetEvent(false).WaitOne((int)ms);
        }

        public static void yield()
        {
            new System.Threading.ManualResetEvent(false).WaitOne(1);
        }
    }
}
