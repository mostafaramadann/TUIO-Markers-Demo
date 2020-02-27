/*
	TUIO C# Demo - part of the reacTIVision project
	http://reactivision.sourceforge.net/

	Copyright (c) 2005-2009 Martin Kaltenbrunner <mkalten@iua.upf.edu

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;
using Emgu.CV;
using Emgu.CV.Structure;
using DirectShowLib;
using Emgu.CV.CvEnum;

public class TuioDemo : Form, TuioListener
{
	private TuioClient client;
	private Dictionary<long, TuioDemoObject> objectList;
	private Dictionary<long, TuioCursor> cursorList;
	private object cursorSync = new object();
	private object objectSync = new object();
	public static int width, height;
	private int window_width = 640;
	private int window_height = 480;
	private int window_left = 0;
	private int window_top = 0;
	private int screen_width = Screen.PrimaryScreen.Bounds.Width;
	private int screen_height = Screen.PrimaryScreen.Bounds.Height;

	private bool fullscreen;
	private bool verbose;

	SolidBrush blackBrush = new SolidBrush(Color.Black);
	SolidBrush whiteBrush = new SolidBrush(Color.White);

	SolidBrush grayBrush = new SolidBrush(Color.Gray);
	Pen fingerPen = new Pen(new SolidBrush(Color.Blue), 1);




	System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
	Bitmap screen;
	VideoCapture cap;
	public Dictionary<int, char> myDict = new Dictionary<int, char> { [0] = 'A', [3] = 'C', [4] = 'D', [5] = 'G', [9] = 'O', [10] = 'R', [11] = 'T' };
	public  string word = "";
	public List<String> words = new List<string> { "CAR", "CAT", "DOG" };
	//Graphics g;
	//int time = 0;
	public static bool show = false;
	public static Bitmap bmp;
	int last_sid;
	public static int lastx;
	public static int lasty;
	private IContainer components;
	private Emgu.CV.UI.ImageBox imageBox1;
	public static int frameindex = 0;
	private Emgu.CV.UI.ImageBox imageBox2;
	int frameno = 0;
	int ct = 0;
	private bool clear = false;
	public TuioDemo(int port) {
		InitializeComponent();
		verbose = false;
		fullscreen = false;
		width = window_width;
		height = window_height;

		this.ClientSize = new System.Drawing.Size(width, height);
		this.Name = "TuioDemo";
		this.Text = "TuioDemo";

		this.Closing += new CancelEventHandler(Form_Closing);
		this.KeyDown += new KeyEventHandler(Form_KeyDown);

		this.SetStyle(ControlStyles.AllPaintingInWmPaint |
						ControlStyles.UserPaint |
						ControlStyles.DoubleBuffer, true);

		objectList = new Dictionary<long, TuioDemoObject>(128);
		cursorList = new Dictionary<long, TuioCursor>(128);
		client = new TuioClient(port);
		client.addTuioListener(this);
		client.connect();
		imageBox1.Show();
		//imageBox2.Hide();
		//g=this.CreateGraphics();
		screen = new Bitmap(width, height);
		//t.Tick += T_Tick;
		try
		{
			cap = new VideoCapture(0);
			cap.ImageGrabbed += Cap_ImageGrabbed;
			cap.Start();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}
		//t.Start();
	}

	private void Cap_ImageGrabbed(object sender, EventArgs e)
	{
		try
		{
			this.Text = word;
			if (word.Length > 3)
				word = "";
			if (frameno % 160 == 0 && !show)
			{
				this.Text = "cleared";
				word = "";
			}
		}
		catch (Exception ee)
		{ }
		try
		{
			Mat m = new Mat();
			cap.Retrieve(m);
			m = m.ToImage<Bgr, byte>().Flip(FlipType.Horizontal).Mat;
			if (show)
			{
				Bitmap to_be_shown = new Bitmap(word + "_" + TuioDemo.frameindex + ".bmp");
				to_be_shown.MakeTransparent(Color.FromArgb(to_be_shown.GetPixel(2, 2).ToArgb()));
				Image<Bgr, byte> im1 = new Image<Bgr,byte>(to_be_shown.Width, to_be_shown.Height);
				im1 = to_be_shown.ToImage<Bgr,byte>();
				imageBox2.Image = im1;
				imageBox2.Size = new Size(to_be_shown.Width, to_be_shown.Height);
				imageBox2.Location = new Point(lastx-to_be_shown.Width/2, lasty-to_be_shown.Height/2);
				if(objectList.Count!=0)
				imageBox2.Show();
				else
				imageBox2.Hide();
				if (frameindex + 1 < 5)
					frameindex++;
				else
				{
					frameindex = 0;
					ct++;
					if (ct >= 10 &&!clear)
					{
						try
						{
							word = "";
						}
						catch (Exception ee)
						{ }
						frameindex = 0;
						imageBox2.Hide();
						show = false;
					}
				}

			}
			//////////////////////////////////////////Cards Detection ///////////////////////////
			foreach (TuioDemoObject tobject in objectList.Values)
			{
				int Xpos = (int)(tobject.getX() * TuioDemo.width);
				int Ypos = (int)(tobject.getY() * TuioDemo.height);
				int size = TuioDemo.height / 10;
				if (myDict.ContainsKey(tobject.getSymbolID()))
				{
					CvInvoke.Rectangle(m, new Rectangle(Xpos - size / 2, Ypos - size / 2, size, size), new Bgr(Color.Black).MCvScalar, 50);
					CvInvoke.PutText(m, myDict[tobject.getSymbolID()].ToString(), new Point(Xpos - size / 2 + 25, Ypos - size / 2 + 25), FontFace.HersheyDuplex, 1.0, new Bgr(Color.White).MCvScalar);
					lastx = tobject.getScreenX(640);
					lasty = tobject.getScreenY(480);
					last_sid = tobject.getSymbolID();
					if (word.Length > 0)
					{
						if (word[word.Length - 1] != myDict[last_sid])
							word += myDict[last_sid];

					}
					else
						if (myDict.ContainsKey(last_sid))
						word += myDict[last_sid];
				}
			}
			if (words.Contains(word))
				show = true;
			imageBox1.Image = m.ToImage<Bgr, byte>();
		}
		catch (Exception ee)
		{
			//MessageBox.Show(ee.Message);
			Console.WriteLine(ee.Message);
		}
		frameno++;
	}

	/*private void T_Tick(object sender, EventArgs e)
	{
		if (objectList.Count == 1 )
		{
			try
			{
				foreach (long key in objectList.Keys)
				{

					lastx = objectList[key].getScreenX(640);
					lasty = objectList[key].getScreenY(480);
					last_sid = objectList[key].getSymbolID();
					if (word.Length > 0)
					{
						if (word[word.Length - 1] != myDict[last_sid])
						word += myDict[last_sid];
					}
					else
						if (myDict.ContainsKey(last_sid))
						word += myDict[last_sid];
				}
			}
			catch (Exception ee)
			{
				Console.WriteLine(ee.Message);
			}
		}
		if (frameindex + 1 < 5)
			frameindex++;
		else
			frameindex = 0;
	if (time % 100 == 0)
		{
			word = "";
			time = 0;
			frameindex = 0;
		}
		buffer(g);
		time++;
	}*/

	private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {

		if (e.KeyCode == Keys.Enter)
		{
			clear =!clear;
			word = "";
			this.Text = word;
		}
		if (e.KeyData == Keys.F1) {
			if (fullscreen == false) {

				width = screen_width;
				height = screen_height;
				window_left = this.Left;
				window_top = this.Top;

				this.FormBorderStyle = FormBorderStyle.None;
				this.Left = 0;
				this.Top = 0;
				this.Width = screen_width;
				this.Height = screen_height;

				fullscreen = true;
			} else {

				width = window_width;
				height = window_height;

				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.Left = window_left;
				this.Top = window_top;
				this.Width = window_width;
				this.Height = window_height;

				fullscreen = false;
			}
		} else if (e.KeyData == Keys.Escape) {
			this.Close();

		} else if (e.KeyData == Keys.V) {
			verbose = !verbose;
		}

	}

	private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		client.removeTuioListener(this);

		client.disconnect();
		System.Environment.Exit(0);
	}

	public void addTuioObject(TuioObject o) {
		lock (objectSync) {
			objectList.Add(o.getSessionID(), new TuioDemoObject(o));
		} if (verbose) Console.WriteLine("add obj " + o.getSymbolID() + " (" + o.getSessionID() + ") " + o.getX() + " " + o.getY() + " " + o.getAngle());
	}

	public void updateTuioObject(TuioObject o) {
		lock (objectSync) {

			objectList[o.getSessionID()].update(o);
		}
		if (verbose) Console.WriteLine("set obj " + o.getSymbolID() + " " + o.getSessionID() + " " + o.getX() + " " + o.getY() + " " + o.getAngle() + " " + o.getMotionSpeed() + " " + o.getRotationSpeed() + " " + o.getMotionAccel() + " " + o.getRotationAccel());
	}

	public void removeTuioObject(TuioObject o) {
		lock (objectSync) {
			objectList.Remove(o.getSessionID());
		}
		if (verbose) Console.WriteLine("del obj " + o.getSymbolID() + " (" + o.getSessionID() + ")");
	}

	public void addTuioCursor(TuioCursor c) {
		lock (cursorSync) {
			cursorList.Add(c.getSessionID(), c);
		}
		if (verbose) Console.WriteLine("add cur " + c.getCursorID() + " (" + c.getSessionID() + ") " + c.getX() + " " + c.getY());
	}

	public void updateTuioCursor(TuioCursor c) {
		if (verbose) Console.WriteLine("set cur " + c.getCursorID() + " (" + c.getSessionID() + ") " + c.getX() + " " + c.getY() + " " + c.getMotionSpeed() + " " + c.getMotionAccel());
	}

	public void removeTuioCursor(TuioCursor c) {
		lock (cursorSync) {
			cursorList.Remove(c.getSessionID());
		}
		if (verbose) Console.WriteLine("del cur " + c.getCursorID() + " (" + c.getSessionID() + ")");
	}

	public void refresh(TuioTime frameTime) {
		Invalidate();
	}
	public void drawScene(Graphics g)
	{
		g.Clear(this.BackColor);
		g.FillRectangle(whiteBrush, new Rectangle(0, 0, width, height));
		// draw the cursor path
		if (cursorList.Count > 0)
		{
			lock (cursorSync)
			{
				foreach (TuioCursor tcur in cursorList.Values)
				{
					List<TuioPoint> path = tcur.getPath();
					TuioPoint current_point = path[0];

					for (int i = 0; i < path.Count; i++)
					{
						TuioPoint next_point = path[i];
						g.DrawLine(fingerPen, current_point.getScreenX(width), current_point.getScreenY(height), next_point.getScreenX(width), next_point.getScreenY(height));
						current_point = next_point;
					}
					g.FillEllipse(grayBrush, current_point.getScreenX(width) - height / 100, current_point.getScreenY(height) - height / 100, height / 50, height / 50);
					Font font = new Font("Arial", 10.0f);
					g.DrawString(tcur.getCursorID() + "", font, blackBrush, new PointF(tcur.getScreenX(width) - 10, tcur.getScreenY(height) - 10));
				}
			
			}

		}
		//Application.Idle += delegate (object sender, EventArgs e)
		//{
		/*if (cap != null)
		{
			Mat frame = cap.QueryFrame();
			if (frame != null)
			{
				bmp = frame.ToImage<Bgr, Byte>().Flip(Emgu.CV.CvEnum.FlipType.Horizontal).ToBitmap();
				g.DrawImage(bmp, 0, 0);
			}
		}*/
		/*if (objectList.Count > 0)
		{
			lock (objectSync)
			{
				
			}
		}*/
	
		
	}
	/*	public void buffer(Graphics g)
		{
			Graphics g2 = Graphics.FromImage(screen);
			drawScene(g2);
			g.DrawImage(screen, 0, 0);
		//};
		// draw the objects
		}*/

	private void InitializeComponent()
	{
			this.components = new System.ComponentModel.Container();
			this.imageBox1 = new Emgu.CV.UI.ImageBox();
			this.imageBox2 = new Emgu.CV.UI.ImageBox();
			((System.ComponentModel.ISupportInitialize)(this.imageBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imageBox2)).BeginInit();
			this.SuspendLayout();
			// 
			// imageBox1
			// 
			this.imageBox1.Location = new System.Drawing.Point(0, 0);
			this.imageBox1.Name = "imageBox1";
			this.imageBox1.Size = new System.Drawing.Size(640, 480);
			this.imageBox1.TabIndex = 2;
			this.imageBox1.TabStop = false;
			// 
			// imageBox2
			// 
			this.imageBox2.Location = new System.Drawing.Point(0, 0);
			this.imageBox2.Name = "imageBox2";
			this.imageBox2.Size = new System.Drawing.Size(10, 10);
			this.imageBox2.TabIndex = 2;
			this.imageBox2.TabStop = false;
			// 
			// TuioDemo
			// 
			this.ClientSize = new System.Drawing.Size(624, 441);
			this.Controls.Add(this.imageBox2);
			this.Controls.Add(this.imageBox1);
			this.Name = "TuioDemo";
			((System.ComponentModel.ISupportInitialize)(this.imageBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imageBox2)).EndInit();
			this.ResumeLayout(false);

	}

	public static void Main(String[] argv) {
	 		int port = 0;
			switch (argv.Length) {
				case 1:
					port = int.Parse(argv[0],null);
					if(port==0) goto default;
					break;
				case 0:
					port = 3333;
					break;
				default:
					Console.WriteLine("usage: java TuioDemo [port]");
					System.Environment.Exit(0);
					break;
			}

			TuioDemo app = new TuioDemo(port);
			Application.Run(app);

		}
	}
