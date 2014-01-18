namespace Routix
{
    partial class Routix
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
            this.components = new System.ComponentModel.Container();
            this.conToCloudButton = new System.Windows.Forms.Button();
            this.cloudIPTextBox = new System.Windows.Forms.TextBox();
            this.conLabel = new System.Windows.Forms.Label();
            this.log = new System.Windows.Forms.TextBox();
            this.cloudPortTextBox = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.networkNumerLabel = new System.Windows.Forms.Label();
            this.subnetNumberLabel = new System.Windows.Forms.Label();
            this.networkNumberTextBox = new System.Windows.Forms.TextBox();
            this.subnetTextBox = new System.Windows.Forms.TextBox();
            this.sendTextBox = new System.Windows.Forms.TextBox();
            this.sendButton = new System.Windows.Forms.Button();
            this.gViewer = new Microsoft.Glee.GraphViewerGdi.GViewer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.sendTopologyButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // conToCloudButton
            // 
            this.conToCloudButton.Location = new System.Drawing.Point(9, 140);
            this.conToCloudButton.Name = "conToCloudButton";
            this.conToCloudButton.Size = new System.Drawing.Size(117, 32);
            this.conToCloudButton.TabIndex = 0;
            this.conToCloudButton.Text = "Połącz";
            this.conToCloudButton.UseVisualStyleBackColor = true;
            this.conToCloudButton.Click += new System.EventHandler(this.conToCloudButton_Click);
            // 
            // cloudIPTextBox
            // 
            this.cloudIPTextBox.Location = new System.Drawing.Point(9, 75);
            this.cloudIPTextBox.Name = "cloudIPTextBox";
            this.cloudIPTextBox.Size = new System.Drawing.Size(117, 20);
            this.cloudIPTextBox.TabIndex = 1;
            this.cloudIPTextBox.Text = "127.0.0.1";
            // 
            // conLabel
            // 
            this.conLabel.AutoSize = true;
            this.conLabel.Location = new System.Drawing.Point(9, 56);
            this.conLabel.Name = "conLabel";
            this.conLabel.Size = new System.Drawing.Size(108, 13);
            this.conLabel.TabIndex = 2;
            this.conLabel.Text = "IP chmury sterowania";
            // 
            // log
            // 
            this.log.BackColor = System.Drawing.SystemColors.Window;
            this.log.Location = new System.Drawing.Point(9, 467);
            this.log.Multiline = true;
            this.log.Name = "log";
            this.log.ReadOnly = true;
            this.log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.log.Size = new System.Drawing.Size(719, 145);
            this.log.TabIndex = 13;
            // 
            // cloudPortTextBox
            // 
            this.cloudPortTextBox.Location = new System.Drawing.Point(9, 114);
            this.cloudPortTextBox.Name = "cloudPortTextBox";
            this.cloudPortTextBox.Size = new System.Drawing.Size(117, 20);
            this.cloudPortTextBox.TabIndex = 24;
            this.cloudPortTextBox.Text = "13000";
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(9, 98);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(117, 13);
            this.portLabel.TabIndex = 23;
            this.portLabel.Text = "Port chmury sterowania";
            // 
            // networkNumerLabel
            // 
            this.networkNumerLabel.AutoSize = true;
            this.networkNumerLabel.Location = new System.Drawing.Point(9, 9);
            this.networkNumerLabel.Name = "networkNumerLabel";
            this.networkNumerLabel.Size = new System.Drawing.Size(62, 13);
            this.networkNumerLabel.TabIndex = 25;
            this.networkNumerLabel.Text = "Numer sieci";
            // 
            // subnetNumberLabel
            // 
            this.subnetNumberLabel.AutoSize = true;
            this.subnetNumberLabel.Location = new System.Drawing.Point(9, 36);
            this.subnetNumberLabel.Name = "subnetNumberLabel";
            this.subnetNumberLabel.Size = new System.Drawing.Size(80, 13);
            this.subnetNumberLabel.TabIndex = 26;
            this.subnetNumberLabel.Text = "Numer podsieci";
            // 
            // networkNumberTextBox
            // 
            this.networkNumberTextBox.Location = new System.Drawing.Point(95, 9);
            this.networkNumberTextBox.Name = "networkNumberTextBox";
            this.networkNumberTextBox.Size = new System.Drawing.Size(31, 20);
            this.networkNumberTextBox.TabIndex = 27;
            // 
            // subnetTextBox
            // 
            this.subnetTextBox.Location = new System.Drawing.Point(95, 33);
            this.subnetTextBox.Name = "subnetTextBox";
            this.subnetTextBox.Size = new System.Drawing.Size(31, 20);
            this.subnetTextBox.TabIndex = 28;
            // 
            // sendTextBox
            // 
            this.sendTextBox.Location = new System.Drawing.Point(9, 619);
            this.sendTextBox.Name = "sendTextBox";
            this.sendTextBox.Size = new System.Drawing.Size(637, 20);
            this.sendTextBox.TabIndex = 29;
            this.sendTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.sendTextBox_KeyPress);
            // 
            // sendButton
            // 
            this.sendButton.Enabled = false;
            this.sendButton.Location = new System.Drawing.Point(652, 616);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(76, 23);
            this.sendButton.TabIndex = 30;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // gViewer
            // 
            this.gViewer.AsyncLayout = false;
            this.gViewer.AutoScroll = true;
            this.gViewer.BackwardEnabled = false;
            this.gViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gViewer.ForwardEnabled = false;
            this.gViewer.Graph = null;
            this.gViewer.Location = new System.Drawing.Point(0, 0);
            this.gViewer.MouseHitDistance = 0.05D;
            this.gViewer.Name = "gViewer";
            this.gViewer.NavigationVisible = true;
            this.gViewer.PanButtonPressed = false;
            this.gViewer.SaveButtonVisible = true;
            this.gViewer.Size = new System.Drawing.Size(596, 452);
            this.gViewer.TabIndex = 31;
            this.gViewer.ZoomF = 1D;
            this.gViewer.ZoomFraction = 0.5D;
            this.gViewer.ZoomWindowThreshold = 0.05D;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gViewer);
            this.panel1.Location = new System.Drawing.Point(132, 9);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(596, 452);
            this.panel1.TabIndex = 32;
            // 
            // sendTopologyButton
            // 
            this.sendTopologyButton.Location = new System.Drawing.Point(9, 414);
            this.sendTopologyButton.Name = "sendTopologyButton";
            this.sendTopologyButton.Size = new System.Drawing.Size(117, 46);
            this.sendTopologyButton.TabIndex = 33;
            this.sendTopologyButton.Text = "ustal Topologię między RC";
            this.sendTopologyButton.UseVisualStyleBackColor = true;
            this.sendTopologyButton.Click += new System.EventHandler(this.sendTopology_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(9, 385);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(117, 23);
            this.progressBar1.TabIndex = 34;
            // 
            // Routix
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 646);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.sendTopologyButton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.sendTextBox);
            this.Controls.Add(this.subnetTextBox);
            this.Controls.Add(this.networkNumberTextBox);
            this.Controls.Add(this.subnetNumberLabel);
            this.Controls.Add(this.networkNumerLabel);
            this.Controls.Add(this.cloudPortTextBox);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.log);
            this.Controls.Add(this.conLabel);
            this.Controls.Add(this.cloudIPTextBox);
            this.Controls.Add(this.conToCloudButton);
            this.Name = "Routix";
            this.Text = "Routix";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button conToCloudButton;
        private System.Windows.Forms.TextBox cloudIPTextBox;
        private System.Windows.Forms.Label conLabel;
        private System.Windows.Forms.TextBox log;
        private System.Windows.Forms.TextBox cloudPortTextBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.Label networkNumerLabel;
        private System.Windows.Forms.Label subnetNumberLabel;
        private System.Windows.Forms.TextBox networkNumberTextBox;
        private System.Windows.Forms.TextBox subnetTextBox;
        private System.Windows.Forms.TextBox sendTextBox;
        private System.Windows.Forms.Button sendButton;
        private Microsoft.Glee.GraphViewerGdi.GViewer gViewer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button sendTopologyButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ProgressBar progressBar1;

    }
}