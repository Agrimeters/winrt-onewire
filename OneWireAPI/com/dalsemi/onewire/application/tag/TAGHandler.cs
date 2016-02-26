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

	using DocumentHandler = org.xml.sax.DocumentHandler;
	using ErrorHandler = org.xml.sax.ErrorHandler;
	using SAXParseException = org.xml.sax.SAXParseException;
	using Locator = org.xml.sax.Locator;
	using SAXException = org.xml.sax.SAXException;
	using AttributeList = org.xml.sax.AttributeList;
	using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;
	using OWPath = com.dalsemi.onewire.utils.OWPath;


	/// <summary>
	/// SAX parser handler that handles XML 1-wire tags.
	/// </summary>
	internal class TAGHandler : ErrorHandler, DocumentHandler
	{

	   /// <summary>
	   /// Method setDocumentLocator
	   /// 
	   /// </summary>
	   /// <param name="locator">
	   ///  </param>
	   public virtual Locator DocumentLocator
	   {
		   set
		   {
		   }
	   }

	   /// <summary>
	   /// Method startDocument
	   /// 
	   /// </summary>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void startDocument()
	   {

		  // Instantiate deviceList and clusterStack
		  deviceList = new ArrayList();
		  clusterStack = new Stack(); // keep track of clusters
		  branchStack = new Stack(); // keep track of current branches
		  branchVector = new ArrayList(); // keep track of every branch
		  branchVectors = new ArrayList(); // keep a vector of cloned branchStacks
										 // to use in making the OWPaths Vector
		  branchPaths = new ArrayList(); // keep track of OWPaths
	   }

	   /// <summary>
	   /// Method endDocument
	   /// 
	   /// </summary>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void endDocument()
	   {

		  // Iterate through deviceList and make all the 
		  // OWPaths from the TaggedDevice's vector of Branches.
		  TaggedDevice device;
		  OWPath branchPath;
		  ArrayList singleBranchVector;

		  for (int i = 0; i < deviceList.Count; i++)
		  {
			 device = (TaggedDevice) deviceList[i];

			 device.setOWPath(adapter, device.Branches);
		  }

		  // Now, iterate through branchVectors and make all the 
		  // OWPaths for the Vector of OWPaths

		  for (int i = 0; i < branchVectors.Count; i++)
		  {
			 singleBranchVector = (ArrayList) branchVectors[i];
			 branchPath = new OWPath(adapter);
			 for (int j = 0; j < singleBranchVector.Count; j++)
			 {
				device = (TaggedDevice) singleBranchVector[i];

				branchPath.add(device.DeviceContainer, device.Channel);
			 }
			 branchPaths.Add(branchPath);
		  }
	   }

	   /// <summary>
	   /// Method startElement
	   /// 
	   /// </summary>
	   /// <param name="name"> </param>
	   /// <param name="atts">
	   /// </param>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void startElement(string name, AttributeList atts)
	   {
		  currentElement = name; //save current element name

		  string attributeAddr = "null";
		  string attributeType = "null";
		  string className;
		  int i = 0;

		  // Parse cluster elements here, keeping track of them with a Stack.
		  if (name.ToUpper().Equals("CLUSTER"))
		  {
			 for (i = 0; i < atts.Length; i++)
			 {
				if (atts.getName(i).ToUpper().Equals("NAME"))
				{
				   clusterStack.Push(atts.getValue(i));
				}
			 }
		  }

		  // Parse sensor, actuator, and branch elements here
		  if (name.ToUpper().Equals("SENSOR") || name.ToUpper().Equals("ACTUATOR") || name.ToUpper().Equals("BRANCH"))
		  {
			 for (i = 0; i < atts.Length; i++)
			 {
				string attName = atts.getName(i);

				if (attName.ToUpper().Equals("ADDR"))
				{
				   attributeAddr = atts.getValue(i);
				}

				if (attName.ToUpper().Equals("TYPE"))
				{
				   attributeType = atts.getValue(i);
				}
			 }

			 // instantiate the appropriate object based on tag type 
			 // (i.e., "Contact", "Switch", etc).  The only exception 
			 // is of type "branch"
			 if (name.ToUpper().Equals("BRANCH"))
			 {
				attributeType = "branch";
				currentDevice = new TaggedDevice(); // instantiates object
			 }
			 else
			 {

				// first, find tag type to instantiate by CLASS NAME!
				// if the tag has a "." in it, it indicates the package 
				// path was included in the tag type.
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
			 }

			 // set the members (fields) of the TaggedDevice object
			 currentDevice.setDeviceContainer(adapter, attributeAddr);
			 currentDevice.DeviceType = attributeType;
			 currentDevice.ClusterName = getClusterStackAsString(clusterStack, "/");
			 currentDevice.Branches = (ArrayList) branchStack.Clone(); // copy branchStack to it's related object in TaggedDevice

			 // ** do branch specific work here: **
			 if (name.Equals("branch"))
			 {

				// push the not-quite-finished branch TaggedDevice on the branch stack.
				branchStack.Push(currentDevice);

				// put currentDevice in the branch vector that holds all branch objects.
				branchVector.Add(currentDevice);

				// put currentDevice in deviceList (if it is of type "branch", of course)
				deviceList.Add(currentDevice);
			 }
		  }
	   }

	   /// <summary>
	   /// Method endElement
	   /// 
	   /// </summary>
	   /// <param name="name">
	   /// </param>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void endElement(string name)
	   {
		  if (name.ToUpper().Equals("SENSOR") || name.ToUpper().Equals("ACTUATOR"))
		  {

			 //System.out.println(name + " element finished");
			 deviceList.Add(currentDevice);

			 currentDevice = null;
		  }

		  if (name.ToUpper().Equals("BRANCH"))
		  {
			 branchVectors.Add(branchStack.Clone()); // adds a snapshot of
															// the stack to 
															// make OWPaths later

			 branchStack.Pop();

			 currentDevice = null; // !!! not sure if this is needed.
		  }

		  if (name.ToUpper().Equals("CLUSTER"))
		  {
			 clusterStack.Pop();
		  }
	   }

	   /// <summary>
	   /// Method characters
	   /// 
	   /// </summary>
	   /// <param name="ch"> </param>
	   /// <param name="start"> </param>
	   /// <param name="length">
	   /// </param>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void characters(char[] ch, int start, int length)
	   {
		  if (currentElement.ToUpper().Equals("LABEL"))
		  {
			 if (currentDevice == null)
			 {

				// This means we have a branch instead of a sensor or actuator
				// so, set label accordingly
				try
				{
				   currentDevice = (TaggedDevice) branchStack.Peek();

				   currentDevice.Label = new string(ch, start, length);

				   currentDevice = null;
				}
				catch (EmptyStackException)
				{

				   // don't do anything yet.
				}
			 }
			 else
			 {
				currentDevice.Label = new string(ch, start, length);
			 }

			 //System.out.println("This device's label is: " + currentDevice.label);
		  }

		  if (currentElement.ToUpper().Equals("CHANNEL"))
		  {
			 if (currentDevice == null)
			 {

				// This means we have a branch instead of a sensor or actuator
				// so, set channel accordingly
				try
				{
				   currentDevice = (TaggedDevice) branchStack.Peek();

				   currentDevice.ChannelFromString = new string(ch, start, length);

				   currentDevice = null;
				}
				catch (EmptyStackException)
				{

				   // don't do anything yet.
				}
			 }
			 else
			 {
				currentDevice.ChannelFromString = new string(ch, start, length);
			 }
		  }

		  if (currentElement.ToUpper().Equals("MAX"))
		  {
			 currentDevice.max = new string(ch, start, length);

			 //System.out.println("This device's max message is: " + currentDevice.max);
		  }

		  if (currentElement.ToUpper().Equals("MIN"))
		  {
			 currentDevice.min = new string(ch, start, length);

			 //System.out.println("This device's min message is: " + currentDevice.min);
		  }

		  if (currentElement.ToUpper().Equals("INIT"))
		  {
			 currentDevice.Init = new string(ch, start, length);

			 //System.out.println("This device's init message is: " + currentDevice.init);
		  }
	   }

	   /// <summary>
	   /// Method ignorableWhitespace
	   /// 
	   /// </summary>
	   /// <param name="ch"> </param>
	   /// <param name="start"> </param>
	   /// <param name="length">
	   /// </param>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void ignorableWhitespace(char[] ch, int start, int length)
	   {
	   }

	   /// <summary>
	   /// Method processingInstruction
	   /// 
	   /// </summary>
	   /// <param name="target"> </param>
	   /// <param name="data">
	   /// </param>
	   /// <exception cref="SAXException">
	   ///  </exception>
	   public virtual void processingInstruction(string target, string data)
	   {
	   }

	   /// <summary>
	   /// Method getTaggedDeviceList
	   /// 
	   /// 
	   /// @return
	   /// 
	   /// </summary>
	   public virtual ArrayList TaggedDeviceList
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
	   /// Method fatalError
	   /// 
	   /// </summary>
	   /// <param name="exception">
	   /// </param>
	   /// <exception cref="SAXParseException">
	   ///  </exception>
	   public virtual void fatalError(SAXParseException exception)
	   {
		  Debug.WriteLine(exception);

		  throw exception;
	   }

	   /// <summary>
	   /// Method error
	   /// 
	   /// </summary>
	   /// <param name="exception">
	   /// </param>
	   /// <exception cref="SAXParseException">
	   ///  </exception>
	   public virtual void error(SAXParseException exception)
	   {
		  Debug.WriteLine(exception);

		  throw exception;
	   }

	   /// <summary>
	   /// Method warning
	   /// 
	   /// </summary>
	   /// <param name="exception">
	   ///  </param>
	   public virtual void warning(SAXParseException exception)
	   {
		  Debug.WriteLine(exception);
	   }

	   /// <summary>
	   /// Method getAllBranches
	   /// 
	   /// </summary>
	   /// <param name="no"> parameters
	   /// </param>
	   /// <returns> Vector of all TaggedDevices of type "branch".
	   ///  </returns>
	   public virtual ArrayList AllBranches
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
	   public virtual ArrayList AllBranchPaths
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
	   private string getClusterStackAsString(Stack clusters, string separator)
	   {
		  string returnString = "";

		  for (int j = 0; j < clusters.Count; j++)
		  {
			 returnString = returnString + separator + (string) clusters.elementAt(j);
		  }

		  return returnString;
	   }

	   /// <summary>
	   /// Field adapter </summary>
	   private DSPortAdapter adapter;

	   /// <summary>
	   /// Field currentElement </summary>
	   private string currentElement;

	   /// <summary>
	   /// Field currentDevice </summary>
	   private TaggedDevice currentDevice;

	   /// <summary>
	   /// Field deviceList </summary>
	   private ArrayList deviceList;

	   /// <summary>
	   /// Field clusterStack </summary>
	   private Stack clusterStack;

	   /// <summary>
	   /// Field branchStack </summary>
	   private Stack branchStack; // keep a stack of current branches

	   /// <summary>
	   /// Field branchVector </summary>
	   private ArrayList branchVector; // to hold all branches

	   /// <summary>
	   /// Field branchVectors </summary>
	   private ArrayList branchVectors; // to hold all branches to eventually
									  // make OWPaths

	   /// <summary>
	   /// Field branchPaths </summary>
	   private ArrayList branchPaths; // to hold all OWPaths to 1-Wire devices.

	}

}