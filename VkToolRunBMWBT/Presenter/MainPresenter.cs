using System.Globalization;
using System.Text;
using System.Windows.Forms;
using VkToolRunBMWBT.Model;
using VkToolRunBMWBT.Services;
using VkToolRunBMWBT.View;

namespace VkToolRunBMWBT.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private HardwareInfo? _hardwareInfo;
        private BenchmarkResult? _cpuResult;
        private BenchmarkResult? _gpuResult;
        private bool _isRunning;

        public MainPresenter(IMainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _view.StartBenchmarkRequested += OnStartBenchmarkRequested;
            _view.SaveReportRequested += OnSaveReportRequested;
        }

        private async void OnStartBenchmarkRequested(object? sender, EventArgs e)
        {
            if (_isRunning) return;
            _isRunning = true;
            _cpuResult = null;
            _gpuResult = null;
            _view.ClearBenchmarkResults();
            _view.EnableControls(false);

            bool backupCreated = false;
            bool completedSuccessfully = false;
            try
            {
                _view.UpdateStatus("Сбор информации о компьютере...", 5);
                _hardwareInfo = await Task.Run(HardwareService.GetSystemInfo);
                _view.DisplayHardwareInfo(_hardwareInfo);

                // Сначала обнаруживаем расположение установленного Tool, чтобы изменять его фактический INI.
                _view.UpdateStatus("Поиск Benchmark Tool и конфигурации...", 10);
                RunnerService.GetGameInstallFolder();
                ConfigService.BackupConfig();
                backupCreated = true;

                _view.UpdateStatus("Применение настроек CPU-теста...", 15);
                ConfigService.ApplyCpuTestConfig();
                var cpuProgress = new Progress<string>(message => _view.UpdateStatus(message, 30));
                string cpuJsonPath = await RunnerService.RunBenchmarkAsync(1280, 720, cpuProgress);

                _view.UpdateStatus("Чтение результата CPU-теста...", 45);
                _cpuResult = ResultParserService.GetLatestResult(
                    "CPU Test", "1280x720", "25%", cpuJsonPath);
                _view.DisplayCpuBenchmarkResult(_cpuResult);

                var desktopResolution = RunnerService.GetDesktopResolution();
                _view.UpdateStatus("Применение настроек GPU-теста...", 55);
                ConfigService.ApplyGpuTestConfig(desktopResolution.Width, desktopResolution.Height);
                var gpuProgress = new Progress<string>(message => _view.UpdateStatus(message, 70));
                string gpuJsonPath = await RunnerService.RunBenchmarkAsync(
                    desktopResolution.Width, desktopResolution.Height, gpuProgress);

                _view.UpdateStatus("Чтение результата GPU-теста...", 85);
                _gpuResult = ResultParserService.GetLatestResult(
                    "GPU Test",
                    $"{desktopResolution.Width}x{desktopResolution.Height}",
                    "100%",
                    gpuJsonPath);
                _view.DisplayGpuBenchmarkResult(_gpuResult);

                completedSuccessfully = true;
                _view.UpdateStatus("Оба теста завершены. Результаты получены из JSON Benchmark Tool.", 100);
            }
            catch (OperationCanceledException)
            {
                _view.UpdateStatus("Тест отменён.", 0);
                _view.ShowError("Выполнение benchmark было отменено.");
            }
            catch (Exception ex)
            {
                _view.UpdateStatus("Ошибка при выполнении теста.", 0);
                _view.ShowError($"Не удалось выполнить benchmark: {ex.Message}");
            }
            finally
            {
                if (backupCreated)
                {
                    try
                    {
                        ConfigService.RestoreConfig();
                        _view.UpdateStatus(
                            completedSuccessfully
                                ? "Тест завершён. Исходный GameUserSettings.ini восстановлен."
                                : "Исходный GameUserSettings.ini восстановлен после ошибки.",
                            completedSuccessfully ? 100 : 0);
                    }
                    catch (Exception restoreError)
                    {
                        _view.ShowError(
                            "КРИТИЧЕСКИ ВАЖНО: не удалось восстановить исходный GameUserSettings.ini. " +
                            "Не удаляйте файл *.restore.tmp, если он появился. Ошибка: " + restoreError.Message);
                    }
                }

                _isRunning = false;
                _view.EnableControls(true);
            }
        }

        private void OnSaveReportRequested(object? sender, EventArgs e)
        {
            if (_hardwareInfo == null || _cpuResult == null || _gpuResult == null)
            {
                _view.ShowError("Сначала успешно выполните оба прохода benchmark, затем сохраните отчёт.");
                return;
            }

            try
            {
                using var dialog = new SaveFileDialog
                {
                    Filter = "Markdown File (*.md)|*.md|Text File (*.txt)|*.txt",
                    DefaultExt = "md",
                    AddExtension = true,
                    FileName = $"Wukong_Benchmark_Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                    OverwritePrompt = true
                };

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                var report = new StringBuilder();
                report.AppendLine("# Black Myth: Wukong Benchmark Report");
                report.AppendLine($"**Дата тестирования:** {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                report.AppendLine();
                report.AppendLine("## Характеристики системы");
                report.AppendLine($"- **CPU:** {_hardwareInfo.CpuName}");
                report.AppendLine($"- **GPU:** {_hardwareInfo.GpuName}");
                report.AppendLine($"- **RAM:** {_hardwareInfo.RamCapacity}");
                report.AppendLine($"- **OS:** {_hardwareInfo.OsVersion}");
                report.AppendLine();
                AppendResultToReport(report, "CPU Test (низкая нагрузка на GPU)", _cpuResult);
                AppendResultToReport(report, "GPU Test (родное разрешение дисплея)", _gpuResult);

                File.WriteAllText(dialog.FileName, report.ToString(), new UTF8Encoding(false));
                MessageBox.Show("Отчёт успешно сохранён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Ошибка при сохранении отчёта: {ex.Message}");
            }
        }

        private static void AppendResultToReport(StringBuilder report, string title, BenchmarkResult result)
        {
            report.AppendLine($"## {title}");
            report.AppendLine($"- **Средний FPS:** {FormatMetric(result.AverageFps)}");
            string lowLabel = result.IsOnePercentLowEstimated ? "1% Low FPS (расчётный по CPU/GPU frame time)" : "1% Low FPS";
            report.AppendLine($"- **{lowLabel}:** {FormatOptionalMetric(result.Parcentile99Fps)}");
            report.AppendLine($"- **Минимальный FPS:** {FormatOptionalMetric(result.MinFps)}");
            report.AppendLine($"- **Максимальный FPS:** {FormatOptionalMetric(result.MaxFps)}");
            report.AppendLine($"- **Разрешение:** {result.Resolution}");
            report.AppendLine($"- **Масштаб рендера:** {result.RenderScale}");
            report.AppendLine($"- **Среднее время кадра CPU:** {FormatOptionalMetric(result.CpuFrameTimeMs)} мс");
            report.AppendLine($"- **Среднее время кадра GPU:** {FormatOptionalMetric(result.GpuFrameTimeMs)} мс");
            report.AppendLine($"- **Загрузка CPU/GPU:** {FormatOptionalMetric(result.CpuUsagePercent)}% / {FormatOptionalMetric(result.GpuUsagePercent)}%");
            report.AppendLine($"- **Использование VRAM:** {FormatOptionalMetric(result.VramGb)} GB");
            report.AppendLine($"- **Настройки:** {result.RawSettingSummary}");
            report.AppendLine($"- **JSON:** {result.ResultFilePath}");
            report.AppendLine();
        }

        private static string FormatMetric(double value) =>
            value.ToString("F1", CultureInfo.InvariantCulture);

        private static string FormatOptionalMetric(double value) =>
            value > 0 ? value.ToString("F1", CultureInfo.InvariantCulture) : "нет данных";
    }
}
