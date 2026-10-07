using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkToolRunBMWBT.View;
using VkToolRunBMWBT.Model;
using VkToolRunBMWBT.Services;

namespace VkToolRunBMWBT.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private HardwareInfo _hardwareInfo;
        private BenchmarkResult _cpuResult;
        private BenchmarkResult _gpuResult;

        public MainPresenter(IMainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            // офрмляем подписку на сабытия
            _view.StartBenchmarkRequested += OnStartBenchmarkRequested;
            _view.SaveReportRequested += OnSaveReportRequested;
        }



        private async void OnStartBenchmarkRequested(object sender, EventArgs e)
        {
            _view.EnableControls(false);
            try
            {

                _view.UpdateStatus("Сбор информации о компе...", 10);
                var hardwareInfo = await Task.Run(() => HardwareService.GetSystemInfo());
                _view.DisplayHardwareInfo(hardwareInfo);

                // резервная копия пользовательских настроек
                ConfigService.BackupConfig();

                //

                _view.UpdateStatus("Применение настроек CPU...", 15);
                ConfigService.ApplyCpuTestConfig();
                var cpuProgress = new Progress<string>(msg => _view.UpdateStatus(msg, 30));
                await RunnerService.RunBenchmarkAsync(cpuProgress);

                //

                _view.UpdateStatus("Анализ теста CPU...", 45);
                _cpuResult = ResultParserService.GetLatestResult("CPU Test", "1280x720", "50%");
                _view.DisplayCpuBenchmarkResult(_cpuResult);

                //

                _view.UpdateStatus("Применение настроек GPU...", 55);
                ConfigService.ApplyGpuTestConfig();
                var _gpuProgress = new Progress<string>(msg => _view.UpdateStatus(msg, 70));
                await RunnerService.RunBenchmarkAsync(_gpuProgress);

                _view.UpdateStatus("Анализ теста GPU...", 85);
                _gpuResult = ResultParserService.GetLatestResult("GPU Test", "3840x2160", "100%");
                _view.DisplayGpuBenchmarkResult(_gpuResult);

                _view.UpdateStatus("Тест завершен.", 100);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Произошла ошибка при выполнении теста: {ex.Message}");
                _view.UpdateStatus("Ошибка при выполнении теста.", 0);
            }
            finally
            {
                ConfigService.RestoreConfig();
                _view.EnableControls(true);
            }
        }

        private void OnSaveReportRequested(object sender, EventArgs e)
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Markdown File (*.md)|*.md|Text File (*.txt)|*.txt";
                    sfd.FileName = $"Wukong_Benchmark_Report_{DateTime.Now:yyyyMMdd_HHmmss}";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("# Black Myth: Wukong Benchmark Report");
                        sb.AppendLine($"**Дата тестирования:** {DateTime.Now:dd.MM.yyyy HH:mm}");
                        sb.AppendLine();
                        sb.AppendLine("## Характеристики системы");
                        sb.AppendLine($"- **CPU:** {_hardwareInfo?.CpuName}");
                        sb.AppendLine($"- **GPU:** {_hardwareInfo?.GpuName}");
                        sb.AppendLine($"- **RAM:** {_hardwareInfo?.RamCapacity}");
                        sb.AppendLine($"- **OS:** {_hardwareInfo?.OsVersion}");
                        sb.AppendLine();
                        sb.AppendLine("## Результаты тестирования");
                        sb.AppendLine($"### 1. CPU Test (Низкая нагрузка на GPU)");
                        sb.AppendLine($"- **Средний FPS:** {_cpuResult?.AverageFps:F1}");
                        sb.AppendLine($"- **1% Low FPS:** {_cpuResult?.Parcentile99Fps:F1}");
                        sb.AppendLine($"- **Настройки:** {_cpuResult?.RawSettingSummary}");
                        sb.AppendLine();
                        sb.AppendLine($"### 2. GPU Test (Максимальная нагрузка на GPU)");
                        sb.AppendLine($"- **Средний FPS:** {_gpuResult?.AverageFps:F1}");
                        sb.AppendLine($"- **1% Low FPS:** {_gpuResult?.Parcentile99Fps:F1}");
                        sb.AppendLine($"- **Настройки:** {_gpuResult?.RawSettingSummary}");

                        File.WriteAllText(sfd.FileName, sb.ToString());
                        MessageBox.Show("Отчет успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _view.ShowError($"Ошибка при сохранении отчета: {ex.Message}");
            }
        }
    }
}
