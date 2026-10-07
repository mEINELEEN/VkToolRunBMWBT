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
        // app ID бенчмарка Black Myth: Wukong в Steam
        private const string SteamAppId = "3132990";
        private const string ProcessName = "b1Benchmark";

        // запускает бенчмарк через Steam и ожидает его полного завершения
        public static async Task RunBenchmarkAsync(IProgress<string> statusProgress = null)
        {
            statusProgress?.Report("Отправка команды на запуск в Steam...");

            // запуск игры через Steam URI
            var startInfo = new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SteamAppId}",
                UseShellExecute = true
            };
            Process.Start(startInfo);

            // ожидание появления процесса в системе
            statusProgress?.Report("Ожидание запуска процесса бенчмарка...");
            Process benchmarkProcess = null;
            int attempts = 0;

            while (benchmarkProcess == null && attempts < 45) // Ожидаем старта до 45 секунд
            {
                await Task.Delay(1000);
                var processes = Process.GetProcessesByName(ProcessName);
                if (processes.Length == 0)
                {
                    // проверяем также альтернативное имя исполняемого файла b1
                    processes = Process.GetProcessesByName("b1");
                }

                if (processes.Length > 0)
                {
                    benchmarkProcess = processes[0];
                }
                attempts++;
            }

            if (benchmarkProcess == null)
            {
                throw new InvalidOperationException("Не удалось обнаружить запущенный процесс бенчмарка. Убедитесь, что Steam запущен.");
            }

            // ожидание завершения прогона бенчмарка
            statusProgress?.Report("Идет проход бенчмарка... Пожалуйста, не закрывайте окно.");

            await Task.Run(() =>
            {
                benchmarkProcess.WaitForExit();
            });

            // ебольшая пауза для сохранения итогового файла результатов на диск
            await Task.Delay(3000);
        }
    }
}