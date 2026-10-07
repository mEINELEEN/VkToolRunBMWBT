using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkToolRunBMWBT.Services
{
    public static class RunnerService
    {
        private const string SteamAppId = "3132990";

        /// <summary>
        /// Путь к папке b1\Saved установленной игры (на любом диске)
        /// </summary>
        public static string DetectedSavedFolderPath { get; private set; }

        public static async Task RunBenchmarkAsync(IProgress<string> statusProgress = null)
        {
            statusProgress?.Report("Отправка команды на запуск в Steam...");

            var startInfo = new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SteamAppId}",
                UseShellExecute = true
            };
            Process.Start(startInfo);

            statusProgress?.Report("Ожидание запуска процесса бенчмарка...");
            Process benchmarkProcess = null;
            int attempts = 0;

            while (benchmarkProcess == null && attempts < 90)
            {
                await Task.Delay(1000);
                benchmarkProcess = FindBenchmarkProcess();
                attempts++;
            }

            if (benchmarkProcess == null)
            {
                throw new InvalidOperationException("Не удалось обнаружить запущенный процесс бенчмарка.");
            }

            // Получаем реальный путь к папке игры на ЛЮБОМ диске
            try
            {
                string exePath = benchmarkProcess.MainModule.FileName; // e.g. E:\SteamLibrary\...\b1-Win64-Shipping.exe
                string binDir = Path.GetDirectoryName(exePath);       // ...\b1\Binaries\Win64
                string b1Dir = Directory.GetParent(binDir)?.Parent?.FullName; // ...\b1
                if (!string.IsNullOrEmpty(b1Dir))
                {
                    DetectedSavedFolderPath = Path.Combine(b1Dir, "Saved");
                }
            }
            catch
            {
                // Если нет прав на запрос модуля, используем стандартный путь
            }

            statusProgress?.Report($"Процесс найден ({benchmarkProcess.ProcessName}). Идет проход бенчмарка...");

            await Task.Run(() =>
            {
                benchmarkProcess.WaitForExit();
            });

            await Task.Delay(3000);
        }

        private static Process FindBenchmarkProcess()
        {
            string[] possibleNames = new[]
            {
                "b1-Win64-Shipping",
                "b1Benchmark-Win64-Shipping",
                "b1",
                "b1Benchmark"
            };

            foreach (string name in possibleNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(name);
                    if (processes.Length > 0 && !processes[0].HasExited)
                    {
                        return processes[0];
                    }
                }
                catch { }
            }

            return null;
        }
    }
}