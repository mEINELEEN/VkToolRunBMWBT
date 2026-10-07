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
        /// <summary>
        /// Находит последний созданный лог/отчет и парсит результаты
        /// </summary>
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
                // Потенциальные пути сохранения отчетов Unreal Engine 5
                string localAppDataSaved = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"b1\Saved");

                string[] candidateDirectories = new[]
                {
                    Path.Combine(localAppDataSaved, "BenchmarkResult"),
                    Path.Combine(localAppDataSaved, "Logs"),
                    localAppDataSaved
                };

                FileInfo latestFile = null;

                foreach (var dirPath in candidateDirectories)
                {
                    if (Directory.Exists(dirPath))
                    {
                        var dir = new DirectoryInfo(dirPath);
                        // Ищем последние текстовые логи, csv или json файлы
                        var file = dir.GetFiles("*.*", SearchOption.AllDirectories)
                                      .Where(f => f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                                      .OrderByDescending(f => f.LastWriteTime)
                                      .FirstOrDefault();

                        if (file != null && (latestFile == null || file.LastWriteTime > latestFile.LastWriteTime))
                        {
                            latestFile = file;
                        }
                    }
                }

                if (latestFile != null)
                {
                    string content = File.ReadAllText(latestFile.FullName);
                    ParseFileContent(content, result);
                }
                else
                {
                    result.RawSettingSummary = $"Разрешение: {expectedResolution}, Масштаб: {expectedScale}";
                }
            }
            catch (Exception ex)
            {
                result.RawSettingSummary = $"Ошибка при чтении результатов: {ex.Message}";
            }

            return result;
        }

        private static void ParseFileContent(string content, BenchmarkResult result)
        {
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("Average FPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Avg FPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("AverageFPS", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Средний FPS", StringComparison.OrdinalIgnoreCase))
                {
                    double val = ExtractNumberFromLine(line);
                    if (val > 0) result.AverageFps = val;
                }
                else if (line.Contains("99%", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("1% Low", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Min FPS", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("Percentile", StringComparison.OrdinalIgnoreCase))
                {
                    double val = ExtractNumberFromLine(line);
                    if (val > 0) result.Parcentile99Fps = val;
                }
            }

            result.RawSettingSummary = $"Разрешение: {result.Resolution}, Масштаб: {result.RenderScale}";
        }

        private static double ExtractNumberFromLine(string line)
        {
            var matches = Regex.Matches(line, @"\d+([.,]\d+)?");
            foreach (Match match in matches)
            {
                if (double.TryParse(match.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    if (val > 0 && val < 1000) return val;
                }
            }
            return 0.0;
        }
    }
}