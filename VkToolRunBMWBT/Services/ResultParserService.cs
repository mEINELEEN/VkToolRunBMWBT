using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkToolRunBMWBT.Model;


namespace VkToolRunBMWBT.Services
{ 
    public static class ResultParserService
    {
        private static string ResultsFolderPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"b1\Saved\BenchmarkResult");

        public static BenchmarkResult GetLatestResult(string testName, string expectedResolution, string expectedScale)
        {
            var result = new BenchmarkResult
            {
                TestName = testName,
                Resolution = expectedResolution,
                RenderScale = expectedScale // <-- Заглавная R
            };

            try
            {
                if (!Directory.Exists(ResultsFolderPath))
                {
                    result.AverageFps = 0;
                    result.Parcentile99Fps = 0;
                    result.RawSettingSummary = "Папка с отчетами бенчмарка не найдена.";
                    return result;
                }

                var directory = new DirectoryInfo(ResultsFolderPath);
                var latestFile = directory.GetFiles()
                                         .OrderByDescending(f => f.LastWriteTime)
                                         .FirstOrDefault();

                if (latestFile != null)
                {
                    string content = File.ReadAllText(latestFile.FullName);
                    ParseFileContent(content, result);
                }
                else
                {
                    result.RawSettingSummary = "Файлы отчетов в папке не обнаружены.";
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
                    line.Contains("Средний FPS", StringComparison.OrdinalIgnoreCase))
                {
                    result.AverageFps = ExtractNumberFromLine(line);
                }
                else if (line.Contains("99%", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("1% Low", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("Min FPS", StringComparison.OrdinalIgnoreCase))
                {
                    result.Parcentile99Fps = ExtractNumberFromLine(line);
                }
            }

            result.RawSettingSummary = $"Разрешение: {result.Resolution}, Масштаб: {result.RenderScale}"; // <-- Заглавная R
        }

        private static double ExtractNumberFromLine(string line)
        {
            var parts = line.Split(new[] { ':', '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (double.TryParse(part.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    return val;
                }
            }
            return 0.0;
        }
    }
}