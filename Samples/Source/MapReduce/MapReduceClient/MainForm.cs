#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Samples.MapReduce
{
	public partial class MainForm : Form
	{
		string _currentFileName;
		MapReduceJob<byte[], Histogram> _mapReduceJob;
		Histogram _currentHistogram;

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

			var storage = CloudStorage
				.ForAzureConnectionString(Properties.Settings.Default.DataConnectionString)
				.BuildStorageProviders();

			_mapReduceJob = new MapReduceJob<byte[], Histogram>(storage.BlobStorage, storage.QueueStorage);

			// Do this asynchronously because it requires a few seconds
			ThreadPool.QueueUserWorkItem(s =>
				{
					using (var input = (Bitmap) Bitmap.FromFile(_currentFileName))
					{
						var slices = Helpers.SliceBitmapAsPng(input, 14);

						// Queue slices
						_mapReduceJob.PushItems(new HistogramMapReduceFunctions(), slices, 4);
						//_currentHistogram = Helpers.ComputeHistogram(input);
						//_pnlHistogram.Refresh();
					}

					BeginInvoke(new Action(() => _timer.Start()));
				});
		}

		private void _timer_Tick(object sender, EventArgs e)
		{
			_timer.Stop();
			ThreadPool.QueueUserWorkItem(s =>
				{
					// Check job status
					bool completed = _mapReduceJob.IsCompleted();

					if (completed)
					{
						_currentHistogram = _mapReduceJob.GetResult();
						_mapReduceJob.DeleteJobData();
					}

					BeginInvoke(new Action(() =>
						{
							if (completed)
							{
								_pnlHistogram.Refresh();
								_btnStart.Enabled = true;
								_btnBrowse.Enabled = true;
								_prgProgress.Style = ProgressBarStyle.Blocks;
							}
							else _timer.Start();
						}));
				});
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
