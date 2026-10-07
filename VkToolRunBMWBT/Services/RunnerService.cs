using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Microsoft.Win32;

namespace VkToolRunBMWBT.Services
{
    public static class RunnerService
    {
        private const string SteamAppId = "3132990";
        public static string DetectedSavedFolderPath { get; private set; }

        public static async Task RunBenchmarkAsync(IProgress<string> statusProgress = null)
        {
            statusProgress?.Report("Поиск исполняемого файла b1-Win64-Shipping.exe...");

            string exePath = FindGameExecutable();

            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                statusProgress?.Report($"Найден exe: {Path.GetFileName(exePath)}. Запуск с флагом -log...");

                string binDir = Path.GetDirectoryName(exePath);
                string b1Dir = Directory.GetParent(binDir)?.Parent?.FullName;
                if (!string.IsNullOrEmpty(b1Dir))
                {
                    DetectedSavedFolderPath = Path.Combine(b1Dir, "Saved");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-log",
                    WorkingDirectory = binDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            else
            {
                statusProgress?.Report("Запуск через протокол Steam...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{SteamAppId}",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }

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

            if (string.IsNullOrEmpty(DetectedSavedFolderPath))
            {
                string pathWmi = GetProcessPathViaWmi(benchmarkProcess.Id);
                if (!string.IsNullOrEmpty(pathWmi))
                {
                    string binDir = Path.GetDirectoryName(pathWmi);
                    string b1Dir = Directory.GetParent(binDir)?.Parent?.FullName;
                    if (!string.IsNullOrEmpty(b1Dir))
                    {
                        DetectedSavedFolderPath = Path.Combine(b1Dir, "Saved");
                    }
                }
            }

            statusProgress?.Report($"Процесс найден ({benchmarkProcess.ProcessName}). Идет проход бенчмарка...");

            await Task.Run(() =>
            {
                benchmarkProcess.WaitForExit();
            });

            await Task.Delay(2500);
        }

        private static string FindGameExecutable()
        {
            // 1. Поиск через реестр Steam
            try
            {
                string steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null)?.ToString();
                if (string.IsNullOrEmpty(steamPath))
                {
                    steamPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null)?.ToString();
                }

                if (!string.IsNullOrEmpty(steamPath))
                {
                    string target = Path.Combine(steamPath, @"steamapps\common\Black Myth Wukong Benchmark Tool\b1\Binaries\Win64\b1-Win64-Shipping.exe");
                    if (File.Exists(target)) return target;
                }
            }
            catch { }

            // 2. Сканирование внешних и системных дисков
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                try
                {
                    string[] relativePaths = new[]
                    {
                        @"SteamLibrary\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Binaries\Win64\b1-Win64-Shipping.exe",
                        @"Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Binaries\Win64\b1-Win64-Shipping.exe",
                        @"Games\SteamLibrary\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Binaries\Win64\b1-Win64-Shipping.exe"
                    };

                    foreach (var rel in relativePaths)
                    {
                        string full = Path.Combine(drive.Name, rel);
                        if (File.Exists(full)) return full;
                    }
                }
                catch { }
            }

            return null;
        }

        private static Process FindBenchmarkProcess()
        {
            string[] possibleNames = { "b1-Win64-Shipping", "b1Benchmark-Win64-Shipping", "b1" };
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

        private static string GetProcessPathViaWmi(int processId)
        {
            try
            {
                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}";
                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        return mo["ExecutablePath"]?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}