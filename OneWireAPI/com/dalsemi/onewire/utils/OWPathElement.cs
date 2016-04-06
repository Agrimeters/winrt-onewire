/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */

namespace com.dalsemi.onewire.utils
{
    using com.dalsemi.onewire.container;

    /// <summary>
    /// 1-Wire&#174 Network path element.  Instances of this class are
    /// used to represent a single branch of a complex 1-Wire network.
    ///
    /// <H3> Usage </H3>
    ///
    /// <DL>
    /// <DD> <H4> Example</H4>
    /// Enumerate through the 1-wire path elements in the 1-Wire path 'path' and print information:
    /// <PRE> <CODE>
    ///   OWPathElement path_element;
    ///
    ///   // enumerature through the path elements
    ///   for (Enumeration path_enum = path.getAllOWPathElements();
    ///           path_enum.hasMoreElements(); )
    ///   {
    ///
    ///      // cast the enum as a OWPathElement
    ///      path_element = (OWPathElement)path_enum.nextElement();
    ///
    ///      // print info
    ///      System.out.println("Address: " + path_element.getContainer().getAddressAsString());
    ///      System.out.println("Channel number: " + path_element.getChannel());
    ///   }
    /// </CODE> </PRE>
    /// </DL>
    /// </summary>
    /// <seealso cref= com.dalsemi.onewire.utils.OWPath </seealso>
    /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer
    ///
    /// @version    0.00, 18 September 2000
    /// @author     DS </seealso>
    public class OWPathElement
    {
        //--------
        //-------- Variables
        //--------

        /// <summary>
        /// OneWireContainer of the path element </summary>
        private OneWireContainer owc;

        /// <summary>
        /// Channel the path is on </summary>
        private int channel;

        //--------
        //-------- Constructors
        //--------

        /// <summary>
        /// Don't allow without OneWireContainer and channel.
        /// </summary>
        private OWPathElement()
        {
        }

        /// <summary>
        /// Create a new 1-Wire path element.
        /// </summary>
        /// <param name="owcInstance"> device that is the path element. Must implement
        ///         SwitchContainer. </param>
        /// <param name="channelNumber"> channel number of the 1-Wire path  </param>
        public OWPathElement(OneWireContainer owcInstance, int channelNumber)
        {
            owc = owcInstance;
            channel = channelNumber;
        }

        /// <summary>
        /// Get the 1-Wire container for this 1-Wire path element.
        /// </summary>
        /// <returns> OneWireContainer of this 1-Wire path element
        /// </returns>
        /// <seealso cref= com.dalsemi.onewire.container.OneWireContainer </seealso>
        public virtual OneWireContainer Container
        {
            get
            {
                return owc;
            }
        }

        /// <summary>
        /// Get the channel number of this 1-Wire path element.
        /// </summary>
        /// <returns> channel number of this element </returns>
        public virtual int Channel
        {
            get
            {
                return channel;
            }
        }
    }
}