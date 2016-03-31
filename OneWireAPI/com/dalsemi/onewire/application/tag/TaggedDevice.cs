using System;
using System.Collections.Generic;

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
    using com.dalsemi.onewire.utils;

    /// <summary>
    /// This class provides a default object for a tagged 1-Wire device.
    /// </summary>
    public class TaggedDevice
    {
        /// <summary>
        /// Creates an object for the device with the supplied address and device type connected
        /// to the supplied port adapter. </summary>
        /// <param name="adapter"> The adapter serving the sensor. </param>
        /// <param name="NetAddress"> The 1-Wire network address of the sensor. </param>
        /// <param name="netAddress"> </param>
        public TaggedDevice(DSPortAdapter adapter, string netAddress)
        {
            this.DeviceContainer = adapter.getDeviceContainer(netAddress);
        }

        /// <summary>
        /// Creates an object for the device.
        /// </summary>
        public TaggedDevice()
        {
        }

        /* ********* Setters for this object *********** */

        /// <summary>
        /// Sets the 1-Wire Container for the tagged device.
        /// </summary>
        public virtual void setDeviceContainer(DSPortAdapter adapter, string netAddress)
        {
            DeviceContainer = adapter.getDeviceContainer(netAddress);
        }

        /// <summary>
        /// Sets the device type for the tagged device.
        /// </summary>
        /// <param name="tType"> </param>
        public virtual string DeviceType { get; set; }

        /// <summary>
        /// Sets the label for the tagged device.
        /// </summary>
        /// <param name="Label"> </param>
        public virtual string Label { get; set; }

        /// <summary>
        /// Sets the channel for the tagged device from a String.
        /// </summary>
        /// <param name="Channel"> </param>
        public virtual string ChannelFromString
        {
            set
            {
                Channel = Int32.Parse(value);
            }
        }

        /// <summary>
        /// Sets the channel for the tagged device from an int.
        /// </summary>
        /// <param name="Channel"> </param>
        public virtual int Channel { get; set; }

        /// <summary>
        /// Sets the init (initialization String) for the
        /// tagged device.
        /// </summary>
        /// <param name="init"> </param>
        public virtual string Init { get; set; }

        /// <summary>
        /// Sets the cluster name for the tagged device.
        /// </summary>
        /// <param name="cluster"> </param>
        public virtual string ClusterName { get; set; }

        /// <summary>
        /// Sets the vector of branches to get to the tagged device.
        /// </summary>
        /// <param name="branches"> </param>
        public virtual List<TaggedDevice> Branches { get; set; }

        /// <summary>
        /// Sets the OWPath for the tagged device.  An
        /// OWPath is a description of how to
        /// physically get to a 1-Wire device through a
        /// set of nested 1-Wire switches.
        /// </summary>
        /// <param name="branchOWPath"> </param>
        public virtual OWPath OWPath { get; set; }

        /// <summary>
        /// Sets the OWPath for the tagged device.  An
        /// OWPath is a description of how to
        /// physically get to a 1-Wire device through a
        /// set of nested 1-Wire switches.
        /// </summary>
        /// <param name="adapter"> </param>
        /// <param name="Branches"> </param>
        public virtual void setOWPath(DSPortAdapter adapter, List<TaggedDevice> Branches)
        {
            OWPath = new OWPath(adapter);

            TaggedDevice TDevice;

            for (int i = 0; i < Branches.Count; i++)
            {
                TDevice = Branches[i];

                OWPath.add(TDevice.DeviceContainer, TDevice.Channel);
            }
        }

        /* ********* Getters for this object *********** */

        /// <summary>
        /// Gets the 1-Wire Container for the tagged device.
        /// </summary>
        /// <returns> The 1-Wire container for the tagged device. </returns>
        public virtual OneWireContainer DeviceContainer { get; private set; }

        /// <summary>
        /// Gets the channel for the tagged device as a String.
        /// </summary>
        /// <returns> The channel for the tagged device as a String. </returns>
        public virtual string ChannelAsString
        {
            get
            {
                return Channel.ToString();
            }
        }

        /// <summary>
        /// Gets the max string for the tagged device.
        /// </summary>
        /// <returns> String  Gets the max string </returns>
        public virtual string Max { get; set; }

        /// <summary>
        /// Gets the min string for the tagged device.
        /// </summary>
        /// <returns> String  Gets the min string </returns>
        public virtual string Min { get; set; }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TaggedDevice)
            {
                TaggedDevice td = (TaggedDevice)o;
                return (td.DeviceContainer.Equals(this.DeviceContainer)) && (td.DeviceType.Equals(this.DeviceType)) && (td.Min.Equals(this.Min)) && (td.Max.Equals(this.Max)) && (td.Init.Equals(this.Init)) && (td.ClusterName.Equals(this.ClusterName)) && (td.Label.Equals(this.Label));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (DeviceContainer.ToString() + Label).GetHashCode();
        }

        public override string ToString()
        {
            return Label;
        }

        /// <summary>
        /// ********* Properties (fields) for this object ********** </summary>

        /// <summary>
        /// A true or false describing the state of the tagged device.
        /// </summary>
        public bool? state;
    }
}