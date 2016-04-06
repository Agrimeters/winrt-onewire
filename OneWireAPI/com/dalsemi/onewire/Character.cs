using System;
using System.Diagnostics;

namespace com.dalsemi.onewire
{
    public class Character
    {
        public static int digit(char ch, int radix)
        {
            try
            {
                if (radix == 16)
                    return Int32.Parse(ch.ToString(), System.Globalization.NumberStyles.HexNumber);
                else if (radix == 10)
                    return Int32.Parse(ch.ToString(), System.Globalization.NumberStyles.Number);
                else
                    Debugger.Break();
            }
            catch (Exception e)
            {
                Debugger.Break();
                throw e;
            }

            return 0;
        }
    }
}