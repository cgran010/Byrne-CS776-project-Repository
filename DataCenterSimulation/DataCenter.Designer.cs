namespace DataCenterSimulation
{
    partial class DataCenter
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
            if (disposing && (components != null))
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
			this.lblElapsedTime = new System.Windows.Forms.Label();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnStart = new System.Windows.Forms.Button();
			this.lblSimStart = new System.Windows.Forms.Label();
			this.lblSimEnd = new System.Windows.Forms.Label();
			this.lblStartTime = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// lblElapsedTime
			// 
			this.lblElapsedTime.AutoSize = true;
			this.lblElapsedTime.Location = new System.Drawing.Point(272, 180);
			this.lblElapsedTime.Name = "lblElapsedTime";
			this.lblElapsedTime.Size = new System.Drawing.Size(0, 20);
			this.lblElapsedTime.TabIndex = 1;
			// 
			// btnStop
			// 
			this.btnStop.Enabled = false;
			this.btnStop.Location = new System.Drawing.Point(292, 27);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(151, 84);
			this.btnStop.TabIndex = 2;
			this.btnStop.Text = "Stop Simulation";
			this.btnStop.UseVisualStyleBackColor = true;
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(53, 27);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(151, 84);
			this.btnStart.TabIndex = 3;
			this.btnStart.Text = "Start Simulation";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// lblSimStart
			// 
			this.lblSimStart.AutoSize = true;
			this.lblSimStart.Location = new System.Drawing.Point(66, 142);
			this.lblSimStart.Name = "lblSimStart";
			this.lblSimStart.Size = new System.Drawing.Size(126, 20);
			this.lblSimStart.TabIndex = 4;
			this.lblSimStart.Text = "Simulation Start:";
			// 
			// lblSimEnd
			// 
			this.lblSimEnd.AutoSize = true;
			this.lblSimEnd.Location = new System.Drawing.Point(66, 180);
			this.lblSimEnd.Name = "lblSimEnd";
			this.lblSimEnd.Size = new System.Drawing.Size(182, 20);
			this.lblSimEnd.TabIndex = 5;
			this.lblSimEnd.Text = "Current Simulation Time:";
			// 
			// lblStartTime
			// 
			this.lblStartTime.AutoSize = true;
			this.lblStartTime.Location = new System.Drawing.Point(272, 142);
			this.lblStartTime.Name = "lblStartTime";
			this.lblStartTime.Size = new System.Drawing.Size(153, 20);
			this.lblStartTime.TabIndex = 6;
			this.lblStartTime.Text = "Simulation start time";
			// 
			// frmDataCenter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.lblStartTime);
			this.Controls.Add(this.lblSimEnd);
			this.Controls.Add(this.lblSimStart);
			this.Controls.Add(this.btnStart);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.lblElapsedTime);
			this.Name = "frmDataCenter";
			this.Text = "frmDataCenter";
			this.Load += new System.EventHandler(this.frmDataCenter_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblElapsedTime;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblSimStart;
        private System.Windows.Forms.Label lblSimEnd;
        private System.Windows.Forms.Label lblStartTime;
    }
}