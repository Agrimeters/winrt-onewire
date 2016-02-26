using System;
using System.Collections;

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

	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using com.dalsemi.onewire.container;
	using OWPath = com.dalsemi.onewire.utils.OWPath;


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
		  this.DeviceContainer_Renamed = adapter.getDeviceContainer(netAddress);
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
		  DeviceContainer_Renamed = adapter.getDeviceContainer(netAddress);
	   }

	   /// <summary>
	   /// Sets the device type for the tagged device.
	   /// </summary>
	   /// <param name="tType"> </param>
	   public virtual string DeviceType
	   {
		   set
		   {
			  DeviceType_Renamed = value;
		   }
		   get
		   {
			  return DeviceType_Renamed;
		   }
	   }

	   /// <summary>
	   /// Sets the label for the tagged device.
	   /// </summary>
	   /// <param name="Label"> </param>
	   public virtual string Label
	   {
		   set
		   {
			  label = value;
		   }
		   get
		   {
			  return label;
		   }
	   }

	   /// <summary>
	   /// Sets the channel for the tagged device from a String.
	   /// </summary>
	   /// <param name="Channel"> </param>
	   public virtual string ChannelFromString
	   {
		   set
		   {
			  channel = System.Convert.ToInt32(value);
		   }
	   }

	   /// <summary>
	   /// Sets the channel for the tagged device from an int.
	   /// </summary>
	   /// <param name="Channel"> </param>
	   public virtual int Channel
	   {
		   set
		   {
			  channel = new int?(value);
		   }
		   get
		   {
			  return channel.Value;
		   }
	   }

	   /// <summary>
	   /// Sets the init (initialization String) for the
	   /// tagged device.
	   /// </summary>
	   /// <param name="init"> </param>
	   public virtual string Init
	   {
		   set
		   {
			  init = value;
		   }
		   get
		   {
			  return init;
		   }
	   }

	   /// <summary>
	   /// Sets the cluster name for the tagged device.
	   /// </summary>
	   /// <param name="cluster"> </param>
	   public virtual string ClusterName
	   {
		   set
		   {
			  clusterName = value;
		   }
		   get
		   {
			  return clusterName;
		   }
	   }

	   /// <summary>
	   /// Sets the vector of branches to get to the tagged device.
	   /// </summary>
	   /// <param name="branches"> </param>
	   public virtual ArrayList Branches
	   {
		   set
		   {
			  branchVector = value;
		   }
		   get
		   {
			  return branchVector;
		   }
	   }

	   /// <summary>
	   /// Sets the OWPath for the tagged device.  An
	   /// OWPath is a description of how to
	   /// physically get to a 1-Wire device through a
	   /// set of nested 1-Wire switches.
	   /// </summary>
	   /// <param name="branchOWPath"> </param>
	   public virtual OWPath OWPath
	   {
		   set
		   {
			  branchPath = value;
		   }
		   get
		   {
			  return branchPath;
		   }
	   }

	   /// <summary>
	   /// Sets the OWPath for the tagged device.  An
	   /// OWPath is a description of how to
	   /// physically get to a 1-Wire device through a
	   /// set of nested 1-Wire switches.
	   /// </summary>
	   /// <param name="adapter"> </param>
	   /// <param name="Branches"> </param>
	   public virtual void setOWPath(DSPortAdapter adapter, ArrayList Branches)
	   {
		  branchPath = new OWPath(adapter);

		  TaggedDevice TDevice;

		  for (int i = 0; i < Branches.Count; i++)
		  {
			 TDevice = (TaggedDevice) Branches[i];

			 branchPath.add(TDevice.DeviceContainer, TDevice.Channel);
		  }
	   }


		/* ********* Getters for this object *********** */


	   /// <summary>
	   /// Gets the 1-Wire Container for the tagged device.
	   /// </summary>
	   /// <returns> The 1-Wire container for the tagged device. </returns>
	   public virtual OneWireContainer DeviceContainer
	   {
		   get
		   {
			  return DeviceContainer_Renamed;
		   }
	   }



	   /// <summary>
	   /// Gets the channel for the tagged device as a String.
	   /// </summary>
	   /// <returns> The channel for the tagged device as a String. </returns>
	   public virtual string ChannelAsString
	   {
		   get
		   {
			  return channel.ToString();
		   }
	   }



	   /// <summary>
	   /// Gets the max string for the tagged device.
	   /// </summary>
	   /// <returns> String  Gets the max string </returns>
	   public virtual string Max
	   {
		   get
		   {
			  return max;
		   }
	   }

	   /// <summary>
	   /// Gets the min string for the tagged device.
	   /// </summary>
	   /// <returns> String  Gets the min string </returns>
	   public virtual string Min
	   {
		   get
		   {
			  return min;
		   }
	   }




	   public override bool Equals(object o)
	   {
		  if (o == this)
		  {
			 return true;
		  }

		  if (o is TaggedDevice)
		  {
			 TaggedDevice td = (TaggedDevice)o;
			 return (td.DeviceContainer_Renamed.Equals(this.DeviceContainer_Renamed)) && (td.DeviceType_Renamed.Equals(this.DeviceType_Renamed)) && (td.min.Equals(this.min)) && (td.max.Equals(this.max)) && (td.init.Equals(this.init)) && (td.clusterName.Equals(this.clusterName)) && (td.label.Equals(this.label));
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
	   /// 1-Wire Container for the tagged device.
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public OneWireContainer DeviceContainer_Renamed;

	   /// <summary>
	   /// Device type for the device (i.e., contact, switch, d2a, etc.).
	   /// </summary>
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
	   public string DeviceType_Renamed;

	   /// <summary>
	   /// Label for the "name" of the device.
	   /// </summary>
	   public string label;

	   /// <summary>
	   /// The channel on which to probe for info.
	   /// </summary>
	   public int? channel;

	   /// <summary>
	   /// A string message representing a high or maximum value.
	   /// </summary>
	   public string max;

	   /// <summary>
	   /// A string message representing a low or minimum value.
	   /// </summary>
	   public string min;

	   /// <summary>
	   /// A true or false describing the state of the tagged device.
	   /// </summary>
	   public bool? state;

	   /// <summary>
	   /// An initialization parameter for the tagged device.
	   /// </summary>
	   public string init;

	   /// <summary>
	   /// The name of the cluster to which the tagged device is associated.
	   /// Nested clusters will have a forward slash ("/") between each
	   /// cluster, much like a path.
	   /// </summary>
	   public string clusterName;

	   /// <summary>
	   /// A Vector of branches describing how to physically get to
	   /// the tagged device through a set of 1-Wire switches.
	   /// </summary>
	   public ArrayList branchVector;

	   /// <summary>
	   /// This is an OWPath describing how to physically get to
	   /// the tagged device through a set of nested 1-Wire branches
	   /// (switches).
	   /// </summary>
	   private OWPath branchPath;
	}

}