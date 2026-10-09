using System;
using System.Text;
using System.Windows.Forms;
using VkToolRunBMWBT.Model;
using VkToolRunBMWBT.View;

namespace VkToolRunBMWBT
{
    public partial class MainForm : Form, IMainView
    {
        public event EventHandler StartBenchmarkRequested;
        public event EventHandler SaveReportRequested;

        public MainForm()
        {
            InitializeComponent();

            btnStart.Click += (s, e) => StartBenchmarkRequested?.Invoke(this, EventArgs.Empty);
            btnSaveReport.Click += (s, e) => SaveReportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        protected void OnStartBenchmarkRequested() => StartBenchmarkRequested?.Invoke(this, EventArgs.Empty);
        protected void OnSaveReportRequested() => SaveReportRequested?.Invoke(this, EventArgs.Empty);

        #region Реализация интерфейса IMainView

        public void DisplayHardwareInfo(HardwareInfo hardwareInfo)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => DisplayHardwareInfo(hardwareInfo)));
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Процессор (CPU): {hardwareInfo.CpuName}");
            sb.AppendLine($"Видеокарта (GPU): {hardwareInfo.GpuName} GB");
            sb.AppendLine($"Оперативная память (RAM): {hardwareInfo.RamCapacity}");
            sb.AppendLine($"Операционная система (OS): {hardwareInfo.OsVersion}");
            lblHardwareInfo.Text = sb.ToString();
        }
        public void DisplayCpuBenchmarkResult(BenchmarkResult result)
        {
            AppendResultToLog("--- [Результат теста процессора (CPU)] ---", result);
        }
        public void DisplayGpuBenchmarkResult(BenchmarkResult result)
        {
            AppendResultToLog("--- [Результат теста видеокарты (GPU)] ---", result);
        }
        private void AppendResultToLog(string title, BenchmarkResult result)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendResultToLog(title, result)));
                return;
            }
            txtResults.AppendText($"{title}{Environment.NewLine}");
            txtResults.AppendText($"Средний FPS: {result.AverageFps:F1}{Environment.NewLine}");
            txtResults.AppendText($"1% Low FPS: {result.Parcentile99Fps:F1}{Environment.NewLine}");
            txtResults.AppendText($"Разрешение: {result.Resolution:F1}{Environment.NewLine}");
            txtResults.AppendText($"Настройки: {result.RawSettingSummary:F1}{Environment.NewLine}");
            txtResults.AppendText(new string('-', 40) + Environment.NewLine + Environment.NewLine);

        }
        public void UpdateStatus(string message, int progressPercent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message, progressPercent)));
                return;
            }
            lblStatus.Text = $"Статус: {message}";
            progressBar1.Value = Math.Clamp(progressPercent, 0, 100);
        }
        public void ShowError(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowError(message)));
                return;
            }
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void EnableControls(bool enable)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => EnableControls(enable)));
                return;
            }
            btnStart.Enabled = enable;
            btnSaveReport.Enabled = enable;
        }

        public void ClearBenchmarkResults()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClearBenchmarkResults));
                return;
            }

            txtResults.Clear();
        }

        #endregion

    }
}
