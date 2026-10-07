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
            grpHardware.Location = new Point(12, 12);
            grpHardware.Name = "grpHardware";
            grpHardware.Size = new Size(776, 106);
            grpHardware.TabIndex = 0;
            grpHardware.TabStop = false;
            grpHardware.Text = "Характеристики компьютера";
            // 
            // lblHardwareInfo
            // 
            lblHardwareInfo.AutoSize = true;
            lblHardwareInfo.Location = new Point(6, 19);
            lblHardwareInfo.Name = "lblHardwareInfo";
            lblHardwareInfo.Size = new Size(211, 15);
            lblHardwareInfo.TabIndex = 0;
            lblHardwareInfo.Text = "Ожидание сбора данных о системе...";
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.LavenderBlush;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = SystemColors.ControlText;
            btnStart.Location = new Point(12, 301);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(776, 36);
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
            btnSaveReport.Location = new Point(12, 343);
            btnSaveReport.Name = "btnSaveReport";
            btnSaveReport.Size = new Size(776, 36);
            btnSaveReport.TabIndex = 2;
            btnSaveReport.Text = "Сохранить отчет";
            btnSaveReport.UseVisualStyleBackColor = false;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = Color.MidnightBlue;
            progressBar1.Cursor = Cursors.IBeam;
            progressBar1.ForeColor = SystemColors.Desktop;
            progressBar1.Location = new Point(12, 385);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(774, 16);
            progressBar1.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = SystemColors.Control;
            lblStatus.Location = new Point(351, 404);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(92, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Готов к запуску";
            // 
            // grpResult
            // 
            grpResult.BackColor = Color.LavenderBlush;
            grpResult.Controls.Add(txtResults);
            grpResult.Controls.Add(label1);
            grpResult.Location = new Point(12, 124);
            grpResult.Name = "grpResult";
            grpResult.Size = new Size(776, 171);
            grpResult.TabIndex = 1;
            grpResult.TabStop = false;
            grpResult.Text = "Результаты тестирования";
            // 
            // txtResults
            // 
            txtResults.Dock = DockStyle.Fill;
            txtResults.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            txtResults.Location = new Point(3, 19);
            txtResults.Name = "txtResults";
            txtResults.ReadOnly = true;
            txtResults.Size = new Size(770, 149);
            txtResults.TabIndex = 1;
            txtResults.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 19);
            label1.Name = "label1";
            label1.Size = new Size(211, 15);
            label1.TabIndex = 0;
            label1.Text = "Ожидание сбора данных о системе...";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.DodgerBlue;
            ClientSize = new Size(800, 421);
            Controls.Add(grpResult);
            Controls.Add(lblStatus);
            Controls.Add(progressBar1);
            Controls.Add(btnSaveReport);
            Controls.Add(btnStart);
            Controls.Add(grpHardware);
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
