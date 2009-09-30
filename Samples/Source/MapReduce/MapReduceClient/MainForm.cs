#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lokad.Cloud.Samples.MapReduce;

namespace MapReduceClient
{
	public partial class MainForm : Form
	{
		string _currentFileName = null;
		MapReduceJob<Bitmap, Histogram, Histogram> _mapReduceJob = null;

		public MainForm()
		{
			InitializeComponent();
		}

		private void _btnBrowse_Click(object sender, EventArgs e)
		{
			using(var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Image Files (*.bmp; *.jpg; *.png)|*.bmp;*.jpg;*.png";
				dialog.Title = "Select Input Image File";
				dialog.Multiselect = false;
				dialog.CheckFileExists = true;
				if(dialog.ShowDialog() == DialogResult.OK)
				{
					_currentFileName = dialog.FileName;
					_picPreview.Image = Bitmap.FromFile(_currentFileName);
				}
			}

			_btnStart.Enabled = _currentFileName != null;
		}

		private void _btnStart_Click(object sender, EventArgs e)
		{
			_btnStart.Enabled = false;
			_btnBrowse.Enabled = false;
			_prgProgress.Style = ProgressBarStyle.Marquee;

			_mapReduceJob = new MapReduceJob<Bitmap, Histogram, Histogram>(
				Setup.Container.Resolve<Lokad.Cloud.IBlobStorageProvider>(),
				Setup.Container.Resolve<Lokad.Cloud.IQueueStorageProvider>());

			// ##############################
			// Split image and push job

			_timer.Start();
		}

		private void _timer_Tick(object sender, EventArgs e)
		{
			// ##############################
			// Check job status
		}

	}

}
