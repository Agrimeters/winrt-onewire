/*---------------------------------------------------------------------------
 * Copyright (C) 1999-2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.application.tag
{
    using com.dalsemi.onewire.adapter;
    using com.dalsemi.onewire.container;

    /// <summary>
    /// This class provides a default object for the Humidity type
    /// of a tagged 1-Wire device.
    /// </summary>
    public class Humidity : TaggedDevice, TaggedSensor
    {
        /// <summary>
        /// Creates an object for the device.
        /// </summary>
        public Humidity() : base()
        {
        }

        /// <summary>
        /// Creates an object for the device with the supplied address and device type connected
        /// to the supplied port adapter. </summary>
        /// <param name="adapter"> The adapter serving the sensor. </param>
        /// <param name="netAddress"> The 1-Wire network address of the sensor. </param>
        public Humidity(DSPortAdapter adapter, string netAddress) : base(adapter, netAddress)
        {
        }

        /// <summary>
        /// The readSensor method returns a relative humidity reading
        /// in %RH
        ///
        /// @param--none.
        /// </summary>
        /// <returns> String humidity in %RH </returns>
        public virtual string readSensor()
        {
            HumidityContainer hc = DeviceContainer as HumidityContainer;

            // read the device first to get the state
            byte[] state = hc.readDevice();

            // convert humidity
            hc.doHumidityConvert(state);

            // construct the return string
            string return_string = (int)roundDouble(hc.getHumidity(state)) + "%";
            if (hc.Relative)
            {
                return_string += "RH";
            }

            return return_string;
        }

        /// <summary>
        /// The roundDouble method returns a double rounded to the
        /// nearest digit in the "ones" position.
        ///
        /// @param--double
        /// </summary>
        /// <returns> double rounded to the nearest digit in the "ones"
        /// position. </returns>
        private double roundDouble(double d)
        {
            return (double)((int)(d + ((d > 0) ? 0.5 : -0.5)));
        }
    }
}