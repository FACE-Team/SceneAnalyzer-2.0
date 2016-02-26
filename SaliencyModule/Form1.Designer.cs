namespace SaliencyModule
{
    partial class SaliencyModule
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
            this.lblYarpServer = new System.Windows.Forms.Label();
            this.lblYarpPort = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblpoint = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblYarpServer
            // 
            this.lblYarpServer.AutoSize = true;
            this.lblYarpServer.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblYarpServer.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblYarpServer.Location = new System.Drawing.Point(12, 73);
            this.lblYarpServer.Name = "lblYarpServer";
            this.lblYarpServer.Size = new System.Drawing.Size(127, 13);
            this.lblYarpServer.TabIndex = 11;
            this.lblYarpServer.Text = "YarpServer : Stopped";
            // 
            // lblYarpPort
            // 
            this.lblYarpPort.AutoSize = true;
            this.lblYarpPort.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblYarpPort.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblYarpPort.Location = new System.Drawing.Point(12, 52);
            this.lblYarpPort.Name = "lblYarpPort";
            this.lblYarpPort.Size = new System.Drawing.Size(67, 13);
            this.lblYarpPort.TabIndex = 10;
            this.lblYarpPort.Text = "YarpPort :";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblStatus.Location = new System.Drawing.Point(85, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(49, 13);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "Stopped";
            // 
            // lblpoint
            // 
            this.lblpoint.AllowDrop = true;
            this.lblpoint.AutoSize = true;
            this.lblpoint.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblpoint.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblpoint.Location = new System.Drawing.Point(92, 30);
            this.lblpoint.Name = "lblpoint";
            this.lblpoint.Size = new System.Drawing.Size(85, 13);
            this.lblpoint.TabIndex = 8;
            this.lblpoint.Text = "X = 0 ; Y = 0";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.label2.Location = new System.Drawing.Point(11, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 17);
            this.label2.TabIndex = 7;
            this.label2.Text = "Point :";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 23);
            this.label1.TabIndex = 6;
            this.label1.Text = "Status :";
            // 
            // SaliencyModule
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(5F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(346, 102);
            this.ControlBox = false;
            this.Controls.Add(this.lblYarpServer);
            this.Controls.Add(this.lblYarpPort);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblpoint);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Consolas", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.Name = "SaliencyModule";
            this.Text = "SaliencyModule";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SaliencyModule_FormClosed);
            this.Load += new System.EventHandler(this.SaliencyModule_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblYarpServer;
        private System.Windows.Forms.Label lblYarpPort;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblpoint;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}

