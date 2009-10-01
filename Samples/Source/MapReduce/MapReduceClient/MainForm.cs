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
using System.Threading;

namespace Lokad.Cloud.Samples.MapReduce
{
	public partial class MainForm : Form
	{
		string _currentFileName = null;
		MapReduceJob<Bitmap, Histogram, Histogram> _mapReduceJob = null;
		Histogram _currentHistogram = null;

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
					_picPreview.ImageLocation = _currentFileName;
					_currentHistogram = null;
					_pnlHistogram.Refresh();
				}
			}

			_btnStart.Enabled = _currentFileName != null;
		}

		private void _btnStart_Click(object sender, EventArgs e)
		{
			_btnStart.Enabled = false;
			_btnBrowse.Enabled = false;
			_prgProgress.Style = ProgressBarStyle.Marquee;
			_currentHistogram = null;
			_pnlHistogram.Refresh();

			_mapReduceJob = new MapReduceJob<Bitmap, Histogram, Histogram>(
				Setup.Container.Resolve<Lokad.Cloud.IBlobStorageProvider>(),
				Setup.Container.Resolve<Lokad.Cloud.IQueueStorageProvider>());

			// Do this asynchronously because it requires a few seconds
			ThreadPool.QueueUserWorkItem(new WaitCallback((s) =>
			{
				using(var input = (Bitmap)Bitmap.FromFile(_currentFileName))
				{
					var slices = Helpers.SliceBitmapAsPng(input, 14);

					// Queue slices
					_mapReduceJob.PushItems(Helpers.GetMapReduceFunctions(), new List<object>(slices), 4);
					//_currentHistogram = Helpers.ComputeHistogram(input);
					//_pnlHistogram.Refresh();
				}

				BeginInvoke(new Action(() => _timer.Start()));
			}));
		}

		private void _timer_Tick(object sender, EventArgs e)
		{
			_timer.Stop();
			ThreadPool.QueueUserWorkItem(new WaitCallback((s) =>
			{
				// Check job status
				bool completed = _mapReduceJob.IsCompleted();

				if(completed)
				{
					_currentHistogram = _mapReduceJob.GetResult();
					_mapReduceJob.DeleteJobData();
				}

				BeginInvoke(new Action(() =>
				{
					if(completed)
					{
						_pnlHistogram.Refresh();
						_btnStart.Enabled = true;
						_btnBrowse.Enabled = true;
						_prgProgress.Style = ProgressBarStyle.Blocks;
					}
					else _timer.Start();
				}));
			}));
		}

		private void _pnlHistogram_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.Clear(_pnlHistogram.BackColor);
			if(_currentHistogram == null) return;

			double maxFreq = _currentHistogram.GetMaxFrequency();
			for(int i = 0; i < _currentHistogram.Frequencies.Length; i++)
			{
				e.Graphics.DrawLine(Pens.Black,
					i, _pnlHistogram.Height,
					i, _pnlHistogram.Height - (float)(_pnlHistogram.Height * _currentHistogram.Frequencies[i] / maxFreq));
			}
		}

	}

}
