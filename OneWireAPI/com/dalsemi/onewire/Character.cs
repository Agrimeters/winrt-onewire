using System;
using System.Diagnostics;

namespace com.dalsemi.onewire
{
    public class Character
    {
        public static int digit(char ch, int radix)
        {
            //Returns the numeric value of the character ch in the specified radix.
            //If the radix is not in the range MIN_RADIX <= radix <= MAX_RADIX or if the value of ch is not a valid digit in the specified radix, -1 is returned.
            //A character is a valid digit if at least one of the following is true:
            //•The method isDigit is true of the character and the Unicode decimal digit value of the character (or its single - character decomposition) is less than the specified radix. In this case the decimal digit value is returned.
            //•The character is one of the uppercase Latin letters 'A' through 'Z' and its code is less than radix +'A' - 10.In this case, ch - 'A' + 10 is returned.
            //•The character is one of the lowercase Latin letters 'a' through 'z' and its code is less than radix +'a' - 10.In this case, ch - 'a' + 10 is returned.

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