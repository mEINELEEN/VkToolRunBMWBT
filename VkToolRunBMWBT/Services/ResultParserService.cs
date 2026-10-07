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
                var candidateDirectories = new List<string>();

                // 1. Путь к папке Saved на внешнем накопителе
                if (!string.IsNullOrEmpty(RunnerService.DetectedSavedFolderPath))
                {
                    candidateDirectories.Add(RunnerService.DetectedSavedFolderPath);
                }

                // 2. Стандартная папка %LOCALAPPDATA%\b1\Saved на диске C:
                string localAppDataSaved = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"b1\Saved");
                candidateDirectories.Add(localAppDataSaved);

                foreach (var dirPath in candidateDirectories)
                {
                    if (Directory.Exists(dirPath))
                    {
                        var dir = new DirectoryInfo(dirPath);
                        // Рекурсивно ищем все файлы .sav, .temp, .log, .txt, .json
                        var files = dir.GetFiles("*.*", SearchOption.AllDirectories)
                                       .Where(f => f.Extension.Equals(".sav", StringComparison.OrdinalIgnoreCase) ||
                                                   f.Extension.Equals(".temp", StringComparison.OrdinalIgnoreCase) ||
                                                   f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                                                   f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                                                   f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                                                   f.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                                       .OrderByDescending(f => f.LastWriteTime);

                        foreach (var file in files)
                        {
                            string textContent = ReadFileWithMultipleEncodings(file.FullName);
                            if (ParseContent(textContent, result))
                            {
                                break;
                            }
                        }

                        if (result.AverageFps > 0) break;
                    }
                }

                result.RawSettingSummary = $"Разрешение: {result.Resolution}, Масштаб: {result.RenderScale}";
            }
            catch (Exception ex)
            {
                result.RawSettingSummary = $"Ошибка при чтении результатов: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Безопасное чтение байтов файла и двойное декодирование (UTF-8 и UTF-16LE Unicode)
        /// </summary>
        private static string ReadFileWithMultipleEncodings(string filePath)
        {
            try
            {
                byte[] bytes;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                }

                // Декодируем байты в обычный текст и Unicode (UTF-16LE для .sav файлов UE5)
                string utf8Text = Encoding.UTF8.GetString(bytes);
                string unicodeText = Encoding.Unicode.GetString(bytes);
                return utf8Text + "\n" + unicodeText;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ParseContent(string content, BenchmarkResult result)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            bool foundAny = false;

            // Поиск Среднего FPS
            var avgMatch = Regex.Match(content, @"(?:Average\s*FPS|Avg\s*FPS|Средний\s*FPS|AverageFPS|AvgFPS)\D*?(\d+([.,]\d+)?)", RegexOptions.IgnoreCase);
            if (avgMatch.Success && double.TryParse(avgMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double avg) && avg > 0)
            {
                result.AverageFps = avg;
                foundAny = true;
            }

            // Поиск 1% Low / 5-го перцентиля FPS
            var lowMatch = Regex.Match(content, @"(?:1%\s*Low|5-й\s*перцентиль|Min\s*FPS|99%|Percentile|MinFPS)\D*?(\d+([.,]\d+)?)", RegexOptions.IgnoreCase);
            if (lowMatch.Success && double.TryParse(lowMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double low) && low > 0)
            {
                result.Parcentile99Fps = low;
                foundAny = true;
            }

            return foundAny;
        }
    }
}