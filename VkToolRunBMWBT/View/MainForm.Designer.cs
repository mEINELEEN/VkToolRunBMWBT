namespace VkToolRunBMWBT
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            grpHardware = new GroupBox();
            lblHardwareInfo = new Label();
            btnStart = new Button();
            btnSaveReport = new Button();
            progressBar1 = new ProgressBar();
            lblStatus = new Label();
            grpResult = new GroupBox();
            txtResults = new RichTextBox();
            label1 = new Label();
            grpHardware.SuspendLayout();
            grpResult.SuspendLayout();
            SuspendLayout();
            // 
            // grpHardware
            // 
            grpHardware.BackColor = Color.LavenderBlush;
            grpHardware.Controls.Add(lblHardwareInfo);
            grpHardware.Location = new Point(14, 16);
            grpHardware.Margin = new Padding(3, 4, 3, 4);
            grpHardware.Name = "grpHardware";
            grpHardware.Padding = new Padding(3, 4, 3, 4);
            grpHardware.Size = new Size(887, 141);
            grpHardware.TabIndex = 0;
            grpHardware.TabStop = false;
            grpHardware.Text = "Характеристики компьютера";
            // 
            // lblHardwareInfo
            // 
            lblHardwareInfo.AutoSize = true;
            lblHardwareInfo.Location = new Point(7, 25);
            lblHardwareInfo.Name = "lblHardwareInfo";
            lblHardwareInfo.Size = new Size(266, 20);
            lblHardwareInfo.TabIndex = 0;
            lblHardwareInfo.Text = "Ожидание сбора данных о системе...";
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.LavenderBlush;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = SystemColors.ControlText;
            btnStart.Location = new Point(14, 450);
            btnStart.Margin = new Padding(3, 4, 3, 4);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(887, 48);
            btnStart.TabIndex = 1;
            btnStart.Text = "Запуск автотеста";
            btnStart.UseVisualStyleBackColor = false;
            // 
            // btnSaveReport
            // 
            btnSaveReport.BackColor = Color.LavenderBlush;
            btnSaveReport.Enabled = false;
            btnSaveReport.FlatAppearance.BorderSize = 0;
            btnSaveReport.FlatStyle = FlatStyle.Flat;
            btnSaveReport.ForeColor = SystemColors.ControlText;
            btnSaveReport.Location = new Point(14, 506);
            btnSaveReport.Margin = new Padding(3, 4, 3, 4);
            btnSaveReport.Name = "btnSaveReport";
            btnSaveReport.Size = new Size(887, 48);
            btnSaveReport.TabIndex = 2;
            btnSaveReport.Text = "Сохранить отчет";
            btnSaveReport.UseVisualStyleBackColor = false;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = Color.MidnightBlue;
            progressBar1.Cursor = Cursors.IBeam;
            progressBar1.ForeColor = SystemColors.Desktop;
            progressBar1.Location = new Point(14, 562);
            progressBar1.Margin = new Padding(3, 4, 3, 4);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(888, 21);
            progressBar1.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = SystemColors.Control;
            lblStatus.Location = new Point(401, 588);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(115, 20);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Готов к запуску";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // grpResult
            // 
            grpResult.BackColor = Color.LavenderBlush;
            grpResult.Controls.Add(txtResults);
            grpResult.Controls.Add(label1);
            grpResult.Location = new Point(14, 165);
            grpResult.Margin = new Padding(3, 4, 3, 4);
            grpResult.Name = "grpResult";
            grpResult.Padding = new Padding(3, 4, 3, 4);
            grpResult.Size = new Size(887, 277);
            grpResult.TabIndex = 1;
            grpResult.TabStop = false;
            grpResult.Text = "Результаты тестирования";
            // 
            // txtResults
            // 
            txtResults.Dock = DockStyle.Fill;
            txtResults.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            txtResults.Location = new Point(3, 24);
            txtResults.Margin = new Padding(3, 4, 3, 4);
            txtResults.Name = "txtResults";
            txtResults.ReadOnly = true;
            txtResults.Size = new Size(881, 249);
            txtResults.TabIndex = 1;
            txtResults.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(7, 25);
            label1.Name = "label1";
            label1.Size = new Size(266, 20);
            label1.TabIndex = 0;
            label1.Text = "Ожидание сбора данных о системе...";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.DodgerBlue;
            ClientSize = new Size(914, 614);
            Controls.Add(grpResult);
            Controls.Add(lblStatus);
            Controls.Add(progressBar1);
            Controls.Add(btnSaveReport);
            Controls.Add(btnStart);
            Controls.Add(grpHardware);
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            grpHardware.ResumeLayout(false);
            grpHardware.PerformLayout();
            grpResult.ResumeLayout(false);
            grpResult.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox grpHardware;
        private Label lblHardwareInfo;
        private Button btnStart;
        private Button btnSaveReport;
        private ProgressBar progressBar1;
        private Label lblStatus;
        private GroupBox grpResult;
        private RichTextBox txtResults;
        private Label label1;
    }
}
