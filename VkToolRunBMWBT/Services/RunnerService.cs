using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VkToolRunBMWBT.Services
{
    /// <summary>
    /// Запускает Benchmark Tool через Steam, ожидает новый JSON-результат и закрывает
    /// только процессы бенчмарка, которые были запущены этим проходом.
    /// </summary>
    public static class RunnerService
    {
        private const string SteamAppId = "3132990";
        private static readonly string[] BenchmarkProcessNames =
        {
            "b1_benchmark",
            "b1-Win64-Shipping",
            "b1Benchmark-Win64-Shipping"
        };

        private sealed record ResultFileSnapshot(string Hash, long Length, DateTime LastWriteTimeUtc);

        public static string? DetectedSavedFolderPath { get; private set; }
        public static string? DetectedGameFolderPath { get; private set; }

        public static string GetGameInstallFolder()
        {
            if (IsValidGameFolder(DetectedGameFolderPath))
                return DetectedGameFolderPath!;

            string[] registryKeys =
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3132990",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3132990",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3132990"
            };

            foreach (string key in registryKeys)
            {
                try
                {
                    string? candidate = Registry.GetValue(key, "InstallLocation", null)?.ToString();
                    if (IsValidGameFolder(candidate))
                        return RememberGameFolder(candidate!);
                }
                catch (Exception)
                {
                    // Не все ключи существуют на каждой системе. Продолжаем с другими способами.
                }
            }

            string? steamFolder = GetSteamFolder();
            var libraryRoots = new List<string>();
            if (!string.IsNullOrWhiteSpace(steamFolder))
            {
                libraryRoots.Add(steamFolder);
                string vdfPath = Path.Combine(steamFolder, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    try
                    {
                        string vdf = File.ReadAllText(vdfPath);
                        foreach (Match match in Regex.Matches(vdf, "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                        {
                            string library = match.Groups[1].Value.Replace("\\\\", "\\");
                            if (Directory.Exists(library) && !libraryRoots.Contains(library, StringComparer.OrdinalIgnoreCase))
                                libraryRoots.Add(library);
                        }
                    }
                    catch (Exception)
                    {
                        // Продолжаем с уже найденной библиотекой Steam.
                    }
                }
            }

            foreach (string root in libraryRoots)
            {
                string candidate = Path.Combine(root, "steamapps", "common", "Black Myth Wukong Benchmark Tool");
                if (IsValidGameFolder(candidate))
                    return RememberGameFolder(candidate);
            }

            // Последние распространённые расположения библиотек Steam.
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                string[] relativePaths =
                {
                    @"SteamLibrary\steamapps\common\Black Myth Wukong Benchmark Tool",
                    @"Games\SteamLibrary\steamapps\common\Black Myth Wukong Benchmark Tool",
                    @"Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool"
                };

                foreach (string relativePath in relativePaths)
                {
                    string candidate = Path.Combine(drive.RootDirectory.FullName, relativePath);
                    if (IsValidGameFolder(candidate))
                        return RememberGameFolder(candidate);
                }
            }

            return string.Empty;
        }

        public static (int Width, int Height) GetDesktopResolution()
        {
            // System.Windows.Forms.Screen берёт текущий основной дисплей и корректно работает
            // в WinForms-приложении без добавления отдельных Win32-зависимостей.
            var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds;
            if (bounds.HasValue && bounds.Value.Width > 0 && bounds.Value.Height > 0)
                return (bounds.Value.Width, bounds.Value.Height);

            return (1920, 1080);
        }

        // Совместимость с прежним вызовом RunnerService.RunBenchmarkAsync(progress).
        public static Task<string> RunBenchmarkAsync(
            IProgress<string>? statusProgress = null,
            CancellationToken cancellationToken = default)
        {
            var resolution = GetDesktopResolution();
            return RunBenchmarkAsync(resolution.Width, resolution.Height, statusProgress, cancellationToken);
        }

        public static async Task<string> RunBenchmarkAsync(
            int width,
            int height,
            IProgress<string>? statusProgress = null,
            CancellationToken cancellationToken = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Разрешение benchmark должно быть больше нуля.");

            EnsureBenchmarkIsNotAlreadyRunning();

            string steamExe = GetSteamExecutablePath();
            if (!File.Exists(steamExe))
                throw new FileNotFoundException("Не найден steam.exe. Проверьте установку Steam.", steamExe);

            string gameFolder = GetGameInstallFolder();
            if (string.IsNullOrWhiteSpace(gameFolder))
            {
                statusProgress?.Report("Путь к папке игры не найден; пробую запуск через Steam по App ID.");
            }

            Dictionary<string, ResultFileSnapshot> oldSnapshots = SnapshotResultFiles();
            DateTime startedAtUtc = DateTime.UtcNow;

            try
            {
                statusProgress?.Report($"Запуск Benchmark Tool через Steam ({width}×{height})...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamExe,
                    WorkingDirectory = Path.GetDirectoryName(steamExe) ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Это ключевой момент: Steam запускает benchmark-сцену сразу, без ручного нажатия в меню.
                string[] arguments =
                {
                    "-applaunch", SteamAppId,
                    "-benchmark",
                    "-windowed",
                    "-forceres",
                    $"-ResX={width.ToString(CultureInfo.InvariantCulture)}",
                    $"-ResY={height.ToString(CultureInfo.InvariantCulture)}"
                };
                foreach (string argument in arguments)
                    startInfo.ArgumentList.Add(argument);

                using Process? steamProcess = Process.Start(startInfo);
                if (steamProcess == null)
                    throw new InvalidOperationException("Steam не удалось запустить.");

                statusProgress?.Report("Benchmark запущен. Ожидание нового JSON-файла с результатами...");
                DateTime deadline = DateTime.UtcNow.AddMinutes(5);
                var lastAnnouncedProcess = string.Empty;

                while (DateTime.UtcNow < deadline)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Process? benchmarkProcess = FindBenchmarkProcess();
                    if (benchmarkProcess != null)
                    {
                        using (benchmarkProcess)
                        {
                            string processName = benchmarkProcess.ProcessName;
                            if (!string.Equals(processName, lastAnnouncedProcess, StringComparison.OrdinalIgnoreCase))
                            {
                                statusProgress?.Report($"Работает процесс {processName}. Ожидаю завершение замеров...");
                                lastAnnouncedProcess = processName;
                            }
                        }
                    }

                    Dictionary<string, ResultFileSnapshot> currentSnapshots = SnapshotResultFiles();
                    foreach (KeyValuePair<string, ResultFileSnapshot> item in currentSnapshots)
                    {
                        bool changed = !oldSnapshots.TryGetValue(item.Key, out ResultFileSnapshot? oldSnapshot) ||
                                       oldSnapshot.Hash != item.Value.Hash ||
                                       oldSnapshot.Length != item.Value.Length ||
                                       oldSnapshot.LastWriteTimeUtc != item.Value.LastWriteTimeUtc;
                        if (!changed)
                            continue;

                        if (IsCompleteBenchmarkJson(item.Key))
                        {
                            // Небольшая пауза и повторная проверка защищают от чтения JSON во время записи.
                            await Task.Delay(400, cancellationToken).ConfigureAwait(false);
                            ResultFileSnapshot? stableSnapshot = TryGetFileSnapshot(item.Key);
                            if (stableSnapshot != null &&
                                stableSnapshot.Hash == item.Value.Hash &&
                                stableSnapshot.Length == item.Value.Length &&
                                stableSnapshot.LastWriteTimeUtc == item.Value.LastWriteTimeUtc &&
                                IsCompleteBenchmarkJson(item.Key))
                            {
                                statusProgress?.Report("Результат benchmark получен.");
                                return item.Key;
                            }
                        }
                    }

                    await Task.Delay(800, cancellationToken).ConfigureAwait(false);
                }

                string folders = string.Join(Environment.NewLine, GetResultDirectories());
                throw new TimeoutException(
                    "Benchmark Tool не создал новый корректный JSON с FPS за 5 минут. " +
                    "Проверьте, запускается ли тест в Steam и завершилась ли сцена. Папки результатов:" +
                    Environment.NewLine + folders);
            }
            finally
            {
                // Steam не закрываем. Завершаем только benchmark-процессы, стартовавшие в этом проходе.
                KillBenchmarkProcessesStartedAfter(startedAtUtc);
            }
        }

        private static string GetSteamExecutablePath()
        {
            string? steamFolder = GetSteamFolder();
            if (!string.IsNullOrWhiteSpace(steamFolder))
            {
                string candidate = Path.Combine(steamFolder, "steam.exe");
                if (File.Exists(candidate)) return candidate;
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string fallback = Path.Combine(programFilesX86, "Steam", "steam.exe");
            if (File.Exists(fallback)) return fallback;

            throw new FileNotFoundException("Не удалось найти Steam. Установите Steam или проверьте ключ SteamPath в реестре.");
        }

        private static string? GetSteamFolder()
        {
            string[] keys =
            {
                @"HKEY_CURRENT_USER\Software\Valve\Steam",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
            };

            foreach (string key in keys)
            {
                try
                {
                    string? value = Registry.GetValue(key, "SteamPath", null)?.ToString();
                    if (string.IsNullOrWhiteSpace(value))
                        value = Registry.GetValue(key, "InstallPath", null)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        value = value.Replace('/', Path.DirectorySeparatorChar);
                        if (Directory.Exists(value)) return Path.GetFullPath(value);
                    }
                }
                catch (Exception)
                {
                    // Пробуем другой раздел реестра.
                }
            }

            string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            return Directory.Exists(fallback) ? fallback : null;
        }

        private static bool IsValidGameFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return false;
            return File.Exists(Path.Combine(folder, "b1_benchmark.exe")) ||
                   File.Exists(Path.Combine(folder, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe"));
        }

        private static string RememberGameFolder(string folder)
        {
            DetectedGameFolderPath = Path.GetFullPath(folder);
            DetectedSavedFolderPath = Path.Combine(DetectedGameFolderPath, "b1", "Saved");
            return DetectedGameFolderPath;
        }

        private static void EnsureBenchmarkIsNotAlreadyRunning()
        {
            foreach (string name in BenchmarkProcessNames)
            {
                Process[] processes;
                try { processes = Process.GetProcessesByName(name); }
                catch { continue; }

                foreach (Process process in processes)
                {
                    using (process)
                    {
                        try
                        {
                            if (!process.HasExited)
                                throw new InvalidOperationException(
                                    "Benchmark Tool уже запущен. Закройте его перед новым тестом, чтобы не получить старые результаты.");
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("уже запущен", StringComparison.OrdinalIgnoreCase))
                        {
                            throw;
                        }
                        catch
                        {
                            // Процесс мог завершиться между опросами; продолжаем.
                        }
                    }
                }
            }
        }

        private static Process? FindBenchmarkProcess()
        {
            foreach (string name in BenchmarkProcessNames)
            {
                try
                {
                    foreach (Process process in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            if (!process.HasExited) return process;
                        }
                        catch { }
                        process.Dispose();
                    }
                }
                catch { }
            }
            return null;
        }

        private static void KillBenchmarkProcessesStartedAfter(DateTime startedAtUtc)
        {
            foreach (string name in BenchmarkProcessNames)
            {
                Process[] processes;
                try { processes = Process.GetProcessesByName(name); }
                catch { continue; }

                foreach (Process process in processes)
                {
                    using (process)
                    {
                        try
                        {
                            if (process.HasExited) continue;
                            DateTime processStarted = process.StartTime.ToUniversalTime();
                            if (processStarted < startedAtUtc.AddSeconds(-3)) continue;

                            process.Kill(entireProcessTree: true);
                            process.WaitForExit(10000);
                        }
                        catch
                        {
                            // Процесс мог закрыться сам или быть недоступен из-за ограничений прав.
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetResultDirectories()
        {
            yield return Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "b1", "Saved", "BenchMarkHistory", "Tool");

            if (!string.IsNullOrWhiteSpace(DetectedSavedFolderPath))
                yield return Path.Combine(DetectedSavedFolderPath, "BenchMarkHistory", "Tool");
        }

        private static Dictionary<string, ResultFileSnapshot> SnapshotResultFiles()
        {
            var result = new Dictionary<string, ResultFileSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (string directory in GetResultDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(directory)) continue;
                string[] files;
                try { files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly); }
                catch { continue; }

                foreach (string file in files)
                {
                    ResultFileSnapshot? snapshot = TryGetFileSnapshot(file);
                    if (snapshot != null) result[file] = snapshot;
                }
            }
            return result;
        }

        private static ResultFileSnapshot? TryGetFileSnapshot(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                string hash = Convert.ToHexString(SHA256.HashData(stream));
                var info = new FileInfo(filePath);
                info.Refresh();
                return new ResultFileSnapshot(hash, info.Length, info.LastWriteTimeUtc);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsCompleteBenchmarkJson(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
                return TryReadNumber(document.RootElement, "FPSAvg", out double averageFps) && averageFps > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadNumber(JsonElement element, string name, out double value)
        {
            value = 0;
            if (!TryGetPropertyIgnoreCase(element, name, out JsonElement property)) return false;
            if (property.ValueKind == JsonValueKind.Number) return property.TryGetDouble(out value);
            return property.ValueKind == JsonValueKind.String &&
                   double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }
            value = default;
            return false;
        }
    }
}
