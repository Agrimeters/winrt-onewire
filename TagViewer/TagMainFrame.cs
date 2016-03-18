using System;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 2001 Dallas Semiconductor Corporation, All Rights Reserved.
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

using com.dalsemi.onewire.application.tag;

/// <summary>
/// Frame for the main window of the Tag viewer
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class TagMainFrame : JFrame, ActionListener
{
   //--------
   //-------- Variables
   //--------

   /// <summary>
   /// Tagged device that this frame displays </summary>
   protected internal TaggedDevice dev;

   /// <summary>
   /// Main Panel </summary>
   protected internal JPanel mainPanel;

   /// <summary>
   /// Sub panels </summary>
   protected internal JPanel northPanel;
   protected internal JPanel southPanel;
   protected internal JPanel centerPanel;
   protected internal JPanel eastPanel;
   protected internal JPanel westPanel;

   /// <summary>
   /// Panel contents </summary>
   protected internal JLabel logLabel;
   protected internal JTextField logField;
   protected internal JList pathList;
   protected internal JCheckBox scanCheck;
   protected internal JLabel portLabel;
   protected internal JLabel statusLabel;
   protected internal JScrollPane scrollPanel;
   protected internal DefaultListModel listData;

   /// <summary>
   /// Log File names </summary>
   protected internal string logFile;

   //--------
   //-------- Constructors
   //--------

   /// <summary>
   /// Constructor a frame to contain the device data.  Provide
   /// the device and the log file name
   /// </summary>
   public TagMainFrame() : base("1-Wire Tag Viewer")
   {
	  // construct the frame

	  //set the look and feel to the system look and feel
	  try
	  {
		 UIManager.LookAndFeel = UIManager.SystemLookAndFeelClassName;
	  }
	  catch (Exception e)
	  {
		 Console.WriteLine(e.ToString());
		 Console.Write(e.StackTrace);
	  }

	  // add an event listener to end the aplication when the frame is closed
	  addWindowListener(new WindowAdapterAnonymousInnerClassHelper(this, e));

	  // create the main panel
	  mainPanel = new JPanel(new BorderLayout(10,10));

	  // create the sub-pannels
	  northPanel = new JPanel();
	  northPanel.Border = BorderFactory.createLoweredBevelBorder();

	  centerPanel = new JPanel();
	  centerPanel.Layout = new BoxLayout(centerPanel, BoxLayout.Y_AXIS);

	  southPanel = new JPanel();
	  southPanel.Layout = new BoxLayout(southPanel, BoxLayout.Y_AXIS);
	  southPanel.Border = BorderFactory.createLoweredBevelBorder();

	  westPanel = new JPanel();
	  westPanel.Border = BorderFactory.createRaisedBevelBorder();
	  westPanel.Border = BorderFactory.createEmptyBorder(10,10,10,10);

	  eastPanel = new JPanel();
	  eastPanel.Border = BorderFactory.createEmptyBorder(10,10,10,10);

	  // fill the panels

	  // north
	  logLabel = new JLabel("Log Filename: ");
	  northPanel.add(logLabel);

	  logField = new JTextField("log.txt",20);
	  logField.addActionListener(this);
	  northPanel.add(logField);

	  // center 
	  listData = new DefaultListModel();
	  listData.addElement("                                                                     ");
	  listData.addElement("                                                                     ");
	  listData.addElement("                                                                     ");
	  listData.addElement("                                                                     ");
	  pathList = new JList(listData);
	  pathList.VisibleRowCount = 5;
	  scrollPanel = new JScrollPane(pathList);
	  scrollPanel.Border = BorderFactory.createTitledBorder(BorderFactory.createEtchedBorder(), "1-Wire Paths to Search");
	  centerPanel.add(scrollPanel);

	  // west
	  scanCheck = new JCheckBox("Scan 1-Wire Paths for XML Tags",false);
	  scanCheck.addActionListener(this);
	  westPanel.add(scanCheck);

	  // south
	  portLabel = new JLabel("Adapter:");
	  southPanel.add(portLabel);

	  statusLabel = new JLabel("Status:");
	  southPanel.add(statusLabel);

	  // add to main
	  mainPanel.add(northPanel,BorderLayout.NORTH);
	  mainPanel.add(centerPanel,BorderLayout.CENTER);
	  mainPanel.add(southPanel,BorderLayout.SOUTH);
	  mainPanel.add(eastPanel,BorderLayout.EAST);
	  mainPanel.add(westPanel,BorderLayout.WEST);

	  // add to frame
	  ContentPane.add(mainPanel);

	  // pack the frame 
	  pack();

	  // resize the window and put in random location
	  Dimension current_sz = Size;
	  Size = new Dimension(current_sz.width * 5 / 4,current_sz.height);
	  Toolkit tool = Toolkit.DefaultToolkit;
	  Dimension mx = tool.ScreenSize;
	  Dimension sz = Size;
	  Random rand = new Random();
	  setLocation((mx.width - sz.width) / 2, (mx.height - sz.height) / 2);

	  // clear out the listbox data
	  listData.removeAllElements();

	  // make visible
	  Visible = true;
   }

   private class WindowAdapterAnonymousInnerClassHelper : WindowAdapter
   {
	   private readonly TagMainFrame outerInstance;

	   private Exception e;

	   public WindowAdapterAnonymousInnerClassHelper(TagMainFrame outerInstance, Exception e)
	   {
		   this.outerInstance = outerInstance;
		   this.e = e;
	   }

	   public virtual void windowClosing(WindowEvent e)
	   {
           return;
	   }
   }

   //--------
   //-------- Methods
   //--------

   /// <summary>
   /// Implements the ActionListener interface to handle  
   /// button click and data change events
   /// </summary>
   public virtual void actionPerformed(ActionEvent @event)
   {
	  object source = @event.Source;

	  // Currently nothing done with event
   }

   /// <summary>
   /// Gets the state of the scan check box
   /// </summary>
   public virtual bool ScanChecked
   {
	   get
	   {
		  return scanCheck.Selected;
	   }
   }

   /// <summary>
   /// Gets the logfile delay in seconds
   /// </summary>
   /// <returns> logfile name entered </returns>
   public virtual string LogFile
   {
	   get
	   {
		  return logField.Text;
	   }
   }

   /// <summary>
   /// Sets the status message
   /// </summary>
   public virtual string Status
   {
	   set
	   {
		  statusLabel.Text = "Status: " + value;
		  // For easy debug, uncomment this line
		  Debug.WriteLine("Status: " + value); 
	   }
   }

   /// <summary>
   /// Clear the current path list
   /// </summary>
   public virtual void clearPathList()
   {
	  listData.removeAllElements();
   }

   /// <summary>
   /// Add an element to the path list
   /// </summary>
   public virtual void addToPathList(string newPath)
   {
	  listData.addElement(newPath);
   }

   /// <summary>
   /// Sets the label for adapter
   /// </summary>
   public virtual string AdapterLabel
   {
	   set
	   {
		  portLabel.Text = "Adapter: " + value;
	   }
   }
}


