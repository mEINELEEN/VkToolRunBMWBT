using System;
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
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        protected void OnStartBenchmarkRequested() => StartBenchmarkRequested?.Invoke(this, EventArgs.Empty);
        protected void OnSaveReportRequested() => SaveReportRequested?.Invoke(this, EventArgs.Empty);

        #region Реализация интерфейса IMainView

        public void DisplayHardwareInfo(HardwareInfo hardwareInfo)
        {
            // реализация отображения информации о железе
        }
        public void DisplayCpuBenchmarkResult(BenchmarkResult result)
        {
            // реализация отображения результатов теста CPU
        }
        public void DisplayGpuBenchmarkResult(BenchmarkResult result)
        {
            // реализация отображения результатов теста GPU
        }
        public void UpdateStatus(string message, int progressPercent)
        {
            // реализация обновления статуса и прогресса
        }
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void EnableControls(bool enable)
        {
            // реализация включения/отключения элементов управления
        }
        #endregion

    }
}
