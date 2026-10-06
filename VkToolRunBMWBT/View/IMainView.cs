using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkToolRunBMWBT.Model;

namespace VkToolRunBMWBT.View
{
    public interface IMainView
    {
        // события, вызываемые пользователем из графического интерфейса

        event EventHandler StartBenchmarkRequested;
        event EventHandler SaveReportRequested;

        // методы обновления графического интерфейса

        void DisplayHardwareInfo(HardwareInfo hardwareInfo);
        void DisplayCpuBenchmarkResult(BenchmarkResult result);
        void DisplayGpuBenchmarkResult(BenchmarkResult result);
        void UpdateStatus(string message, int progressPercent);
        void ShowError(string message);
        void EnableControls(bool enable);
    }
}
