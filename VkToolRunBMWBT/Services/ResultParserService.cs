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

                // 1. Динамический путь к внешней папке (диск E:, D: и т.д.)
                if (!string.IsNullOrEmpty(RunnerService.DetectedSavedFolderPath))
                {
                    candidateDirectories.Add(RunnerService.DetectedSavedFolderPath);
                }

                // 2. Локальный путь AppData
                string localAppDataSaved = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"b1\Saved");
                candidateDirectories.Add(localAppDataSaved);

                FileInfo latestFile = null;

                foreach (var dirPath in candidateDirectories)
                {
                    if (Directory.Exists(dirPath))
                    {
                        var dir = new DirectoryInfo(dirPath);
                        var file = dir.GetFiles("*.*", SearchOption.AllDirectories)
                                      .Where(f => f.Extension.Equals(".sav", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".temp", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                                                  f.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
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
                    byte[] fileBytes = File.ReadAllBytes(latestFile.FullName);

                    // Если это бинарный файл сейва .sav / .temp
                    if (latestFile.Extension.Equals(".sav", StringComparison.OrdinalIgnoreCase) ||
                        latestFile.Extension.Equals(".temp", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseBinarySavFile(fileBytes, result);
                    }
                    else
                    {
                        // Текстовый файл (.log, .txt, .csv)
                        string textContent = Encoding.UTF8.GetString(fileBytes);
                        ParseTextContent(textContent, result);
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
        /// Парсинг бинарных файлов сохранения Unreal Engine 5 (.sav)
        /// </summary>
        private static void ParseBinarySavFile(byte[] bytes, BenchmarkResult result)
        {
            try
            {
                // Ищем байтовые структуры FloatProperty в сейве UE5
                string rawAscii = Encoding.ASCII.GetString(bytes);

                // Сканируем все возможные вхождения ключевых слов
                List<float> extractedFloats = new List<float>();

                int index = 0;
                while ((index = rawAscii.IndexOf("FloatProperty", index, StringComparison.Ordinal)) != -1)
                {
                    // В UE5 значение float лежит со смещением 13–25 байт после слова FloatProperty
                    for (int offset = 12; offset <= 28; offset += 4)
                    {
                        if (index + 13 + offset + 4 <= bytes.Length)
                        {
                            float val = BitConverter.ToSingle(bytes, index + 13 + offset);
                            // Игровой FPS обычно находится в диапазоне от 5 до 300
                            if (val >= 5.0f && val <= 300.0f)
                            {
                                extractedFloats.Add(val);
                            }
                        }
                    }
                    index += 13;
                }

                if (extractedFloats.Count >= 2)
                {
                    // Первый вытащенный float — Среднее значение, второй — 1% Low / 5-й перцентиль
                    result.AverageFps = Math.Round(extractedFloats[0], 1);
                    result.Parcentile99Fps = Math.Round(extractedFloats[1], 1);
                }
                else if (extractedFloats.Count == 1)
                {
                    result.AverageFps = Math.Round(extractedFloats[0], 1);
                }
            }
            catch { }
        }

        /// <summary>
        /// Парсинг обычных текстовых логов (.log / .txt)
        /// </summary>
        private static void ParseTextContent(string content, BenchmarkResult result)
        {
            var avgMatch = Regex.Match(content, @"(?:Average\s*FPS|Avg\s*FPS|Средний\s*FPS|AverageFPS)\D*?(\d+([.,]\d+)?)", RegexOptions.IgnoreCase);
            if (avgMatch.Success && double.TryParse(avgMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double avg))
            {
                result.AverageFps = avg;
            }

            var lowMatch = Regex.Match(content, @"(?:1%\s*Low|5-й\s*перцентиль|Min\s*FPS|Percentile)\D*?(\d+([.,]\d+)?)", RegexOptions.IgnoreCase);
            if (lowMatch.Success && double.TryParse(lowMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double low))
            {
                result.Parcentile99Fps = low;
            }
        }
    }
}