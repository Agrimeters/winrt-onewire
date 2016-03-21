using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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
    using com.dalsemi.onewire.utils;

    /// <summary>
    /// XmlReader based parser handler that handles XML 1-wire tags.
    /// </summary>
    internal class TAGHandlerEx
	{

	   /// <summary>
	   /// Method startDocument
	   /// 
	   /// </summary>
	   public virtual void startDocument()
	   {

		  // Instantiate deviceList and clusterStack
		  deviceList = new List<TaggedDevice>();
		  clusterStack = new Stack<string>(); // keep track of clusters
		  branchStack = new Stack<TaggedDevice>(); // keep track of current branches
		  branchVector = new List<TaggedDevice>(); // keep track of every branch
		  branchVectors = new List<Stack<TaggedDevice>>(); // keep a vector of cloned branchStacks
                                                           // to use in making the OWPaths Vector
		  branchPaths = new List<OWPath>(); // keep track of OWPaths
	   }

	   /// <summary>
	   /// Method endDocument
	   /// 
	   /// </summary>
	   public virtual void endDocument()
	   {
		  // Iterate through deviceList and make all the 
		  // OWPaths from the TaggedDevice's vector of Branches.
		  TaggedDevice device;
		  OWPath branchPath;
		  Stack<TaggedDevice> singleBranchVector;

		  for (int i = 0; i < deviceList.Count; i++)
		  {
			 device = (TaggedDevice) deviceList[i];

			 device.setOWPath(adapter, device.Branches);
		  }

		  // Now, iterate through branchVectors and make all the 
		  // OWPaths for the Vector of OWPaths

		  for (int i = 0; i < branchVectors.Count; i++)
		  {
             singleBranchVector = new Stack<TaggedDevice>(branchVectors[i]);
			 branchPath = new OWPath(adapter);
			 for (int j = 0; j < singleBranchVector.Count; j++)
			 {
				device = singleBranchVector.ToArray()[j];
				branchPath.add(device.DeviceContainer, device.Channel);
			 }
			 branchPaths.Add(branchPath);
		  }
	   }

       /// <summary>
       /// Used to set the Members of a newly minted TaggedDevice object
       /// </summary>
       /// <param name="Addr"></param>
       /// <param name="Type"></param>
       private void setTaggedDeviceMembers(string address, string type)
       {
           currentDevice.setDeviceContainer(adapter, address);
           currentDevice.DeviceType = type;
           currentDevice.ClusterName = getClusterStackAsString(clusterStack, "/");
           // copy branchStack to it's related object in TaggedDevice
           currentDevice.Branches = branchStack.ToArray().ToList<TaggedDevice>();
       }

	   /// <summary>
	   /// Method startElement
	   /// 
	   /// </summary>
	   /// <param name="name"> </param>
	   /// <param name="atts">
	   /// </param>
	   public virtual void startElement(string name, XmlReader reader)
	   {
          switch (name.ToUpper())
          {
             case ("CLUSTER"):
             {
                // Parse cluster elements here, keeping track of them with a Stack.
                try
                {
                   clusterStack.Push(reader.GetAttribute("name"));
                }
                catch (ArgumentNullException) {;}
                catch (InvalidOperationException) {;}
                break;
             }
             case ("LABEL"):
             {
                reader.Read();
			    if (currentDevice == null)
			    {
				   // This means we have a branch instead of a sensor or actuator
				   // so, set label accordingly
				   try
				   {
 				      currentDevice = branchStack.Peek();
				      currentDevice.Label = reader.Value.Trim();
				      currentDevice = null;
				   }
				   catch (InvalidOperationException)
				   {
				      // don't do anything yet.
  				   }
			    }
			    else
			    {
				   currentDevice.Label = reader.Value.Trim();
			    }

   			    //System.out.println("This device's label is: " + currentDevice.label);
                break;
             }
             case ("CHANNEL"):
             {
                reader.Read();
			    if (currentDevice == null)
			    {
				   // This means we have a branch instead of a sensor or actuator
				   // so, set channel accordingly
				   try
				   {
				      currentDevice = branchStack.Peek();
				      currentDevice.ChannelFromString = reader.Value.Trim();
                      currentDevice = null;
				   }
				   catch (InvalidOperationException)
				   {
				      // don't do anything yet.
				   }
			    }
			    else
			    {
				   currentDevice.ChannelFromString = reader.Value.Trim();
                }
                break;
             }
             case ("MAX"):
             {
                reader.Read();
			    currentDevice.Max = reader.Value.Trim();

  			    //System.out.println("This device's max message is: " + currentDevice.max);
                break;
             }
             case ("MIN"):
             {
                reader.Read();
			    currentDevice.Min = reader.Value.Trim();

  			    //System.out.println("This device's min message is: " + currentDevice.min);
                break;
             }
             case ("INIT"):
             {
                reader.Read();
			    currentDevice.Init = reader.Value.Trim();

			    //System.out.println("This device's init message is: " + currentDevice.init);
                break;
             }
             case ("BRANCH"):
             {
		         string attributeAddr = "null";
                 try
                 {
                     attributeAddr = reader.GetAttribute("addr");
                 }
                 catch (ArgumentNullException) {;}

				 currentDevice = new TaggedDevice(); // instantiates object

                 setTaggedDeviceMembers(attributeAddr, "branch");

				 // push the not-quite-finished branch TaggedDevice on the branch stack.
				 branchStack.Push(currentDevice);
				 // put currentDevice in the branch vector that holds all branch objects.
				 branchVector.Add(currentDevice);
				 // put currentDevice in deviceList (if it is of type "branch", of course)
				 deviceList.Add(currentDevice);
                 break;
             }
             case ("SENSOR"):
             case ("ACTUATOR"):
             {
		         string attributeAddr = "null";
		         string attributeType = "null";
                 try
                 {
                     attributeAddr = reader.GetAttribute("addr");
                 }
                 catch (ArgumentNullException) {;}
                 try
                 {
                     attributeType = reader.GetAttribute("type");
                 }
                 catch (ArgumentNullException) {;}

				 // first, find tag type to instantiate by CLASS NAME!
				 // if the tag has a "." in it, it indicates the package 
				 // path was included in the tag type.
		         string className;

				 if (attributeType.IndexOf(".", StringComparison.Ordinal) > 0)
				 {
				     className = attributeType;
				 }
		         else
				 {
				     className = "com.dalsemi.onewire.application.tag." + attributeType;
				 }

				 // instantiate the appropriate object based on tag type (i.e., "Contact", "Switch", etc)
				 try
				 {
				     Type genericClass = Type.GetType(className);

                     currentDevice = (TaggedDevice)Activator.CreateInstance(genericClass);
                 }
                 catch (System.Exception e)
				 {
		             throw new Exception("Can't load 1-Wire Tag Type class (" + className + "): " + e.Message);
				 }

                 setTaggedDeviceMembers(attributeAddr, attributeType);
                 break;
			  }
          }
       }

       /// <summary>
       /// Method endElement
       /// 
       /// </summary>
       /// <param name="name">
       /// </param>
       public virtual void endElement(string name)
	   {
          switch(name.ToUpper())
          {
             case ("SENSOR"):
             case ("ACTUATOR"):
             {
			    //System.out.println(name + " element finished");
			    deviceList.Add(currentDevice);
			    currentDevice = null;
                break;
             }
             case ("BRANCH"):
             {
                // adds a snapshot of the stack to make OWPaths later
                branchVectors.Add(new Stack<TaggedDevice>(branchStack));
                branchStack.Pop();
                currentDevice = null; // !!! not sure if this is needed.
                break;
             }
             case ("CLUSTER"):
             {
                clusterStack.Pop();
                break;
             }
          }
	   }
    
       /// <summary>
       /// Method getTaggedDeviceList
       /// 
       /// 
       /// @return
       /// 
       /// </summary>
       public virtual List<TaggedDevice> TaggedDeviceList
	   {
		   get
		   {
			  return deviceList;
		   }
	   }

	   /// <summary>
	   /// Method setAdapter
	   /// 
	   /// </summary>
	   /// <param name="adapter">
	   /// </param>
	   /// <exception cref="com.dalsemi.onewire.OneWireException">
	   ///  </exception>
	   public virtual DSPortAdapter Adapter
	   {
		   set
		   {
			  this.adapter = value;
		   }
	   }

	   /// <summary>
	   /// Method getAllBranches
	   /// 
	   /// </summary>
	   /// <param name="no"> parameters
	   /// </param>
	   /// <returns> Vector of all TaggedDevices of type "branch".
	   ///  </returns>
	   public virtual List<TaggedDevice> AllBranches
	   {
		   get
		   {
			  return branchVector;
		   }
	   }

	   /// <summary>
	   /// Method getAllBranchPaths
	   /// 
	   /// </summary>
	   /// <param name="no"> parameters
	   /// </param>
	   /// <returns> Vector of all possible OWPaths.
	   ///  </returns>
	   public virtual List<OWPath> AllBranchPaths
	   {
		   get
		   {
			  return branchPaths;
		   }
	   }

	   /// <summary>
	   /// Method getClusterStackAsString
	   /// 
	   /// </summary>
	   /// <param name="clusters"> </param>
	   /// <param name="separator">
	   /// 
	   /// @return
	   ///  </param>
	   private string getClusterStackAsString(Stack<string> clusters, string separator)
	   {
          StringBuilder returnString = new StringBuilder();

          for (int j = 0; j < clusters.Count; j++)
		  {
              returnString.Append(separator);
              returnString.Append(clusters.ToArray()[j]);
		  }

          return returnString.ToString();
	   }

	   /// <summary>
	   /// Field adapter </summary>
	   private DSPortAdapter adapter;

	   /// <summary>
	   /// Field currentDevice </summary>
	   private TaggedDevice currentDevice;

	   /// <summary>
	   /// Field deviceList </summary>
	   private List<TaggedDevice> deviceList;

	   /// <summary>
	   /// Field clusterStack </summary>
	   private Stack<string> clusterStack;

	   /// <summary>
	   /// Field branchStack </summary>
	   private Stack<TaggedDevice> branchStack; // keep a stack of current branches

	   /// <summary>
	   /// Field branchVector </summary>
	   private List<TaggedDevice> branchVector; // to hold all branches

	   /// <summary>
	   /// Field branchVectors </summary>
	   private List<Stack<TaggedDevice>> branchVectors; // to hold all branches to eventually
   									      // make OWPaths

	   /// <summary>
	   /// Field branchPaths </summary>
	   private List<OWPath> branchPaths; // to hold all OWPaths to 1-Wire devices.

	}

}