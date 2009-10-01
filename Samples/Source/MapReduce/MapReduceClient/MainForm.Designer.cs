namespace Lokad.Cloud.Samples.MapReduce
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this._grpInput = new System.Windows.Forms.GroupBox();
			this._btnBrowse = new System.Windows.Forms.Button();
			this._picPreview = new System.Windows.Forms.PictureBox();
			this._grpProgress = new System.Windows.Forms.GroupBox();
			this._prgProgress = new System.Windows.Forms.ProgressBar();
			this._btnStart = new System.Windows.Forms.Button();
			this._grpResult = new System.Windows.Forms.GroupBox();
			this._pnlHistogram = new System.Windows.Forms.Panel();
			this._timer = new System.Windows.Forms.Timer(this.components);
			this._grpInput.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._picPreview)).BeginInit();
			this._grpProgress.SuspendLayout();
			this._grpResult.SuspendLayout();
			this.SuspendLayout();
			// 
			// _grpInput
			// 
			this._grpInput.Controls.Add(this._btnBrowse);
			this._grpInput.Controls.Add(this._picPreview);
			this._grpInput.Location = new System.Drawing.Point(12, 12);
			this._grpInput.Name = "_grpInput";
			this._grpInput.Size = new System.Drawing.Size(286, 328);
			this._grpInput.TabIndex = 0;
			this._grpInput.TabStop = false;
			this._grpInput.Text = "1 - Select Input Image";
			// 
			// _btnBrowse
			// 
			this._btnBrowse.Location = new System.Drawing.Point(205, 299);
			this._btnBrowse.Name = "_btnBrowse";
			this._btnBrowse.Size = new System.Drawing.Size(75, 23);
			this._btnBrowse.TabIndex = 1;
			this._btnBrowse.Text = "Browse...";
			this._btnBrowse.UseVisualStyleBackColor = true;
			this._btnBrowse.Click += new System.EventHandler(this._btnBrowse_Click);
			// 
			// _picPreview
			// 
			this._picPreview.Location = new System.Drawing.Point(6, 19);
			this._picPreview.Name = "_picPreview";
			this._picPreview.Size = new System.Drawing.Size(274, 274);
			this._picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this._picPreview.TabIndex = 0;
			this._picPreview.TabStop = false;
			// 
			// _grpProgress
			// 
			this._grpProgress.Controls.Add(this._prgProgress);
			this._grpProgress.Controls.Add(this._btnStart);
			this._grpProgress.Location = new System.Drawing.Point(304, 12);
			this._grpProgress.Name = "_grpProgress";
			this._grpProgress.Size = new System.Drawing.Size(286, 48);
			this._grpProgress.TabIndex = 1;
			this._grpProgress.TabStop = false;
			this._grpProgress.Text = "2 - Launch Histogram Analysis";
			// 
			// _prgProgress
			// 
			this._prgProgress.Location = new System.Drawing.Point(87, 19);
			this._prgProgress.Name = "_prgProgress";
			this._prgProgress.Size = new System.Drawing.Size(193, 23);
			this._prgProgress.TabIndex = 1;
			// 
			// _btnStart
			// 
			this._btnStart.Enabled = false;
			this._btnStart.Location = new System.Drawing.Point(6, 19);
			this._btnStart.Name = "_btnStart";
			this._btnStart.Size = new System.Drawing.Size(75, 23);
			this._btnStart.TabIndex = 0;
			this._btnStart.Text = "Start";
			this._btnStart.UseVisualStyleBackColor = true;
			this._btnStart.Click += new System.EventHandler(this._btnStart_Click);
			// 
			// _grpResult
			// 
			this._grpResult.Controls.Add(this._pnlHistogram);
			this._grpResult.Location = new System.Drawing.Point(304, 66);
			this._grpResult.Name = "_grpResult";
			this._grpResult.Size = new System.Drawing.Size(286, 274);
			this._grpResult.TabIndex = 2;
			this._grpResult.TabStop = false;
			this._grpResult.Text = "3 - View Histogram";
			// 
			// _pnlHistogram
			// 
			this._pnlHistogram.Location = new System.Drawing.Point(15, 37);
			this._pnlHistogram.Name = "_pnlHistogram";
			this._pnlHistogram.Size = new System.Drawing.Size(257, 200);
			this._pnlHistogram.TabIndex = 0;
			this._pnlHistogram.Paint += new System.Windows.Forms.PaintEventHandler(this._pnlHistogram_Paint);
			// 
			// _timer
			// 
			this._timer.Interval = 5000;
			this._timer.Tick += new System.EventHandler(this._timer_Tick);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(604, 354);
			this.Controls.Add(this._grpResult);
			this.Controls.Add(this._grpProgress);
			this.Controls.Add(this._grpInput);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Map/Reduce Sample - Image Histogram";
			this._grpInput.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._picPreview)).EndInit();
			this._grpProgress.ResumeLayout(false);
			this._grpResult.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox _grpInput;
		private System.Windows.Forms.GroupBox _grpProgress;
		private System.Windows.Forms.ProgressBar _prgProgress;
		private System.Windows.Forms.Button _btnStart;
		private System.Windows.Forms.PictureBox _picPreview;
		private System.Windows.Forms.Button _btnBrowse;
		private System.Windows.Forms.GroupBox _grpResult;
		private System.Windows.Forms.Timer _timer;
		private System.Windows.Forms.Panel _pnlHistogram;
	}
}