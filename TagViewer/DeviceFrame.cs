using System;
using System.IO;
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

using TaggedDevice = com.dalsemi.onewire.application.tag.TaggedDevice;

/// <summary>
/// Base window/frame class for the tagged device sensor/actuators.
/// 
/// @version    0.00, 28 Aug 2001
/// @author     DS
/// </summary>
public class DeviceFrame : JFrame, ActionListener
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
   protected internal JPanel topPanel;
   protected internal JPanel centerPanel;
   protected internal JPanel bottomPanel;

   /// <summary>
   /// Panel contents </summary>
   protected internal JLabel mainLabel;
   protected internal JLabel timeLabel;
   protected internal JLabel clusterLabel;
   protected internal JLabel pathLabel;
   protected internal JCheckBox logCheck;
   protected internal JButton readButton;
   protected internal JComboBox pollCombo;

   /// <summary>
   /// delay time in seconds </summary>
   protected internal int pollDelay;

   /// <summary>
   /// Button click state </summary>
   protected internal bool readButtonClick;

   /// <summary>
   /// Utility to format numbers </summary>
   protected internal NumberFormat num_format;

   /// <summary>
   /// Last reading for tracking of a 'change' </summary>
   protected internal string lastReading;

   /// <summary>
   /// LogFile name </summary>
   protected internal string logFile;

   //--------
   //-------- Constructors
   //--------

   /// <summary>
   /// Constructor a frame to contain the device data.  Provide
   /// the device and the log file name
   /// </summary>
   public DeviceFrame(TaggedDevice dev, string logFile) : base(dev.DeviceContainer.AddressAsString)
   {
	  // construct the frame

	  // init
	  pollDelay = 0;
	  readButtonClick = false;
	  num_format = NumberFormat.Instance;
	  num_format.MaximumFractionDigits = 2;
	  num_format.MinimumFractionDigits = 0;
	  num_format.MinimumIntegerDigits = 2;
	  num_format.GroupingUsed = false;
	  lastReading = "none";

	  // get ref to the tagged device and log file
	  this.dev = dev;
	  this.logFile = logFile;

	  // set the look and feel to the system look and feel
	  try
	  {
		 UIManager.LookAndFeel = UIManager.SystemLookAndFeelClassName;
	  }
	  catch (Exception e)
	  {
		 Debug.WriteLine(e.ToString());
		 Debug.Write(e.StackTrace);
	  }

	  // add an event listener to end the aplication when the frame is closed
	  addWindowListener(new WindowAdapterAnonymousInnerClassHelper(this, e));

	  // create the main panel
	  mainPanel = new JPanel(new GridLayout(3,1));

	  // create the sub-panels
	  topPanel = new JPanel();
	  topPanel.Layout = new BoxLayout(topPanel, BoxLayout.Y_AXIS);
	  topPanel.Border = BorderFactory.createEmptyBorder(10,10,10,10);

	  centerPanel = new JPanel();
	  centerPanel.Layout = new BoxLayout(centerPanel, BoxLayout.Y_AXIS);
	  centerPanel.Border = BorderFactory.createEmptyBorder(10,10,10,10);
	  centerPanel.Background = Color.white;

	  bottomPanel = new JPanel();
	  bottomPanel.Layout = new BoxLayout(bottomPanel, BoxLayout.Y_AXIS);
	  bottomPanel.Border = BorderFactory.createEmptyBorder(10,10,10,10);

	  // fill the panels
	  // top
	  clusterLabel = new JLabel("Cluster: " + dev.ClusterName);
	  topPanel.add(clusterLabel);

	  mainLabel = new JLabel(dev.Label);
	  mainLabel.HorizontalAlignment = JLabel.CENTER;
	  mainLabel.Font = new Font("SansSerif",Font.PLAIN,20);
	  topPanel.add(mainLabel);

	  logCheck = new JCheckBox("Logging Enable",false);
	  logCheck.addActionListener(this);
	  topPanel.add(logCheck);

	  // center
	  timeLabel = new JLabel("Last Reading: none");
	  timeLabel.HorizontalAlignment = JLabel.CENTER;
	  centerPanel.add(timeLabel);

	  // bottom
	  readButton = new JButton("Read Once");
	  readButton.AlignmentX = Component.LEFT_ALIGNMENT;
	  readButton.addActionListener(this);
	  bottomPanel.add(readButton);

	  string[] selectionStrings = new string[] {"No Polling", "1 second", "30 seconds", "1 minute", "10 minutes", "1 hour"};
	  pollCombo = new JComboBox(selectionStrings);
	  pollCombo.Editable = false;
	  pollCombo.AlignmentX = Component.LEFT_ALIGNMENT;
	  pollCombo.addActionListener(this);
	  bottomPanel.add(pollCombo);

	  pathLabel = new JLabel("Path: " + dev.OWPath.ToString());
	  pathLabel.AlignmentX = Component.LEFT_ALIGNMENT;
	  bottomPanel.add(pathLabel);

	  // add to main
	  mainPanel.add(topPanel);
	  mainPanel.add(centerPanel);
	  mainPanel.add(bottomPanel);

	  // add to frame
	  ContentPane.add(mainPanel);

	  // pack the frame 
	  pack();

	  // resize the window and put in random location
	  Dimension current_sz = Size;
	  Size = new Dimension(current_sz.width * 3 / 2,current_sz.height);
	  Toolkit tool = Toolkit.DefaultToolkit;
	  Dimension mx = tool.ScreenSize;
	  Dimension sz = Size;
	  Random rand = new Random();
	  setLocation(rand.Next((mx.width - sz.width) / 2), rand.Next((mx.height - sz.height) / 2));

	  // make visible
	  Visible = true;
   }

   private class WindowAdapterAnonymousInnerClassHelper : WindowAdapter
   {
	   private readonly DeviceFrame outerInstance;

	   private Exception e;

	   public WindowAdapterAnonymousInnerClassHelper(DeviceFrame outerInstance, Exception e)
	   {
		   this.outerInstance = outerInstance;
		   this.e = e;
	   }

	   public virtual void windowClosing(WindowEvent e)
	   {
		   Environment.Exit(0);
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

	  if (source == readButton)
	  {
		 readButtonClick = true;
	  }
	  else if (source == pollCombo)
	  {
		 switch (pollCombo.SelectedIndex)
		 {
			case 0:
				pollDelay = 0;
				break;
			case 1:
				pollDelay = 1;
				break;
			case 2:
				pollDelay = 30;
				break;
			case 3:
				pollDelay = 60;
				break;
			case 4:
				pollDelay = 600;
				break;
			case 5:
				pollDelay = 3600;
				break;
		 }
	  }
   }

   /// <summary>
   /// Gets the state of the log check box
   /// </summary>
   public virtual bool LogChecked
   {
	   get
	   {
		  return logCheck.Selected;
	   }
   }

   /// <summary>
   /// Checks to see if the read button has been clicked.  The
   /// state gets reset with this call.
   /// </summary>
   public virtual bool ReadButtonClick
   {
	   get
	   {
		  bool rt = readButtonClick;
		  readButtonClick = false;
		  return rt;
	   }
   }

   /// <summary>
   /// Gets the poll delay in seconds
   /// </summary>
   public virtual int PollDelay
   {
	   get
	   {
		  return pollDelay;
	   }
   }

   /// <summary>
   /// Sets reading time label to the current time
   /// </summary>
   public virtual void showTime(string header)
   {
	  // print time stamp
	  lastReading = num_format.format(DateTime.Now.Month) + 
            "/" + num_format.format(DateTime.Now.Day) + 
            "/" + num_format.format(DateTime.Now.Year) + " " + 
            num_format.format(DateTime.Now.Hour) + ":" + 
            num_format.format(DateTime.Now.Minute) + ":" + 
            num_format.format(DateTime.Now.Second) + "." + 
            (num_format.format(DateTime.Now.Millisecond) / 10);

	  timeLabel.Text = header + lastReading;
   }

   /// <summary>
   /// Hides the 'read' items when frame is in actuator mode
   /// </summary>
   public virtual void hideReadItems()
   {
	  pollCombo.Visible = false;
	  readButton.Visible = false;
   }

   /// <summary>
   /// Logs the current reading with the provided value
   /// </summary>
   public virtual void log(string value)
   {
	  // construct the string to log
	  string log_string = new string(dev.ClusterName + "," + mainLabel.Text + "," + dev.OWPath.ToString() + Title + "," + lastReading + "," + value);
	  try
	  {
		 TextWriter writer = new TextWriter(new FileStream(logFile, (FileMode.OpenOrCreate | FileMode.Append)));
		 writer.Write(log_string);
		 writer.Flush();
		 writer.Dispose();
	  }
	  catch (FileNotFoundException e)
	  {
		 Debug.WriteLine(e);
	  }
   }

}


