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
        /// Запускает бенчмарк через Steam и ожидает его полного завершения
        /// </summary>
        public static async Task RunBenchmarkAsync(IProgress<string> statusProgress = null)
        {
            statusProgress?.Report("Отправка команды на запуск в Steam...");

            // 1. Старт через Steam URI
            var startInfo = new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SteamAppId}",
                UseShellExecute = true
            };
            Process.Start(startInfo);

            statusProgress?.Report("Ожидание запуска процесса бенчмарка...");
            Process benchmarkProcess = null;
            int attempts = 0;

            // 2. Ожидаем подхвата процесса до 90 секунд
            while (benchmarkProcess == null && attempts < 90)
            {
                await Task.Delay(1000);
                benchmarkProcess = FindBenchmarkProcess();
                attempts++;
            }

            if (benchmarkProcess == null)
            {
                throw new InvalidOperationException("Не удалось обнаружить запущенный процесс бенчмарка. Убедитесь, что Steam запущен и игра установлена.");
            }

            statusProgress?.Report($"Процесс найден ({benchmarkProcess.ProcessName}). Идет проход бенчмарка...");

            // 3. Ждем, пока пользователь завершит прогон в бенчмарке и закроет его
            await Task.Run(() =>
            {
                benchmarkProcess.WaitForExit();
            });

            // Небольшая пауза для гарантированного сохранения отчета на диск
            await Task.Delay(3000);
        }

        /// <summary>
        /// Безопасный поиск процесса бенчмарка среди всех вариантов наименований UE5
        /// </summary>
        private static Process FindBenchmarkProcess()
        {
            string[] possibleNames = new[]
            {
                "b1-Win64-Shipping",
                "b1Benchmark-Win64-Shipping",
                "b1",
                "b1Benchmark",
                "b1_benchmark",
                "BlackMythWukongBenchmark"
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
                catch
                {
                    // Игнорируем возможные системные ограничения доступа
                }
            }

            return null;
        }
    }
}
   