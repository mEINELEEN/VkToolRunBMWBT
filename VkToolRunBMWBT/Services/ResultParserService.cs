using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VkToolRunBMWBT.Model;


namespace VkToolRunBMWBT.Services
{
    public static class ResultParserService
    {
        public static BenchmarkResult GetLatestResult(string testName, string expectedResolution, string expectedScale)
        {
        
            var result = new BenchmarkResult
            {
                TestName = testName,
                Resolution = expectedResolution,
                RenderScale = expectedScale
            };

            try
            {
                // 1. Определяем путь к файлу GameUserSettings.ini
                string iniPath = GetIniFilePath();

                // 2. Считываем параметры графики из .ini файла в модель result
                GameUserSettingsParser.ReadConfigFile(iniPath, result);
            }
            catch (Exception ex)
            {
                result.RawSettingSummary = $"Ошибка чтения .ini: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Поиск файла GameUserSettings.ini в папки игры или %LOCALAPPDATA%
        /// </summary>
        private static string GetIniFilePath()
        {
            // Проверяем путь, найденный при запуске процесса
            if (!string.IsNullOrEmpty(RunnerService.DetectedSavedFolderPath))
            {
                string dynamicPath = Path.Combine(RunnerService.DetectedSavedFolderPath, @"Config\Windows\GameUserSettings.ini");
                if (File.Exists(dynamicPath))
                {
                    return dynamicPath;
                }
            }

            // Стандартный путь в AppData
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"b1\Saved\Config\Windows\GameUserSettings.ini");
        }
        

        private static string FindLatestLogFile(out string debugFilesList)
        {
            debugFilesList = "Папка с логами пуста";
            var candidateDirs = new List<string>();

            if (!string.IsNullOrEmpty(RunnerService.DetectedSavedFolderPath))
            {
                candidateDirs.Add(Path.Combine(RunnerService.DetectedSavedFolderPath, "Logs"));
            }

            string localAppDataLogs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"b1\Saved\Logs");
            candidateDirs.Add(localAppDataLogs);

            FileInfo newestLog = null;
            var foundNames = new List<string>();

            foreach (var dirPath in candidateDirs)
            {
                if (Directory.Exists(dirPath))
                {
                    var dir = new DirectoryInfo(dirPath);
                    var files = dir.GetFiles("*.*", SearchOption.AllDirectories);

                    foreach (var f in files)
                    {
                        foundNames.Add(f.Name);
                        if ((f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                             f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) &&
                            !f.Name.StartsWith("cef", StringComparison.OrdinalIgnoreCase) &&
                            !f.Name.StartsWith("UnrealCEF", StringComparison.OrdinalIgnoreCase))
                        {
                            if (newestLog == null || f.LastWriteTime > newestLog.LastWriteTime)
                            {
                                newestLog = f;
                            }
                        }
                    }
                }
            }

            if (foundNames.Count > 0)
            {
                debugFilesList = string.Join(", ", foundNames.Distinct());
            }

            return newestLog?.FullName;
        }

        private static string ReadLogFileWithShare(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        private static void ParseLogContent(string content, BenchmarkResult result)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i];

                if (result.AverageFps == 0 &&
                   (line.Contains("Average FPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Avg FPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("AverageFPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("FPS:", StringComparison.OrdinalIgnoreCase)))
                {
                    double val = ExtractNumber(line);
                    if (val > 0) result.AverageFps = val;
                }

                if (result.Parcentile99Fps == 0 &&
                   (line.Contains("1% Low", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Min FPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Percentile", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Low", StringComparison.OrdinalIgnoreCase)))
                {
                    double val = ExtractNumber(line);
                    if (val > 0) result.Parcentile99Fps = val;
                }

                if (result.AverageFps > 0 && result.Parcentile99Fps > 0) break;
            }

            if (result.AverageFps == 0)
            {
                var match = Regex.Match(content, @"(?:Average|Avg|FPS)\D*?(\d+([.,]\d+)?)", RegexOptions.IgnoreCase);
                if (match.Success && double.TryParse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double avg))
                {
                    result.AverageFps = avg;
                }
            }
        }

        private static double ExtractNumber(string line)
        {
            var matches = Regex.Matches(line, @"\d+([.,]\d+)?");
            foreach (Match m in matches)
            {
                if (double.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    if (val > 0 && val < 1000) return val;
                }
            }
            return 0;
        }
    }
}