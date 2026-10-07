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

                // 1. Динамический путь к папке Saved на внешнем диске (из RunnerService)
                if (!string.IsNullOrEmpty(RunnerService.DetectedSavedFolderPath))
                {
                    candidateDirectories.Add(RunnerService.DetectedSavedFolderPath);
                }

                // 2. Стандартный путь AppData на диске C:
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
                    byte[] fileBytes;
                    using (var stream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileBytes = new byte[stream.Length];
                        stream.Read(fileBytes, 0, fileBytes.Length);
                    }

                    if (latestFile.Extension.Equals(".sav", StringComparison.OrdinalIgnoreCase) ||
                        latestFile.Extension.Equals(".temp", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseBinarySavFile(fileBytes, result);
                    }
                    else
                    {
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
        /// Вытаскивает значения IntProperty, Int64Property и FloatProperty из бинарного файла .sav UE5
        /// </summary>
        private static void ParseBinarySavFile(byte[] bytes, BenchmarkResult result)
        {
            try
            {
                string rawAscii = Encoding.ASCII.GetString(bytes);
                string[] targetProperties = new[] { "IntProperty", "Int64Property", "FloatProperty" };

                var foundValues = new List<(string Name, double Value)>();

                foreach (var propType in targetProperties)
                {
                    int index = 0;
                    while ((index = rawAscii.IndexOf(propType, index, StringComparison.Ordinal)) != -1)
                    {
                        string propName = ExtractPropertyNameBefore(bytes, index);
                        double val = ExtractValueAfter(bytes, index + propType.Length, propType);

                        if (val >= 1.0 && val <= 500.0)
                        {
                            foundValues.Add((propName, val));
                        }

                        index += propType.Length;
                    }
                }

                // 1. Сопоставление по имени свойства в сейве
                foreach (var item in foundValues)
                {
                    string name = item.Name.ToLowerInvariant();
                    if (result.AverageFps == 0 && (name.Contains("avg") || name.Contains("average") || (name.Contains("fps") && !name.Contains("min") && !name.Contains("percentile"))))
                    {
                        result.AverageFps = Math.Round(item.Value, 1);
                    }
                    else if (result.Parcentile99Fps == 0 && (name.Contains("min") || name.Contains("low") || name.Contains("percentile") || name.Contains("5th") || name.Contains("99")))
                    {
                        result.Parcentile99Fps = Math.Round(item.Value, 1);
                    }
                }

                // 2. Резервный вариант: если имена свойств не совпали, берутся первые логичные значения из сейва
                if (result.AverageFps == 0 && foundValues.Count > 0)
                {
                    var fpsCandidates = foundValues.Where(v => v.Value != 50.0 && v.Value != 100.0 && v.Value != 60.0).ToList();
                    if (fpsCandidates.Count > 0)
                    {
                        result.AverageFps = Math.Round(fpsCandidates[0].Value, 1);
                        if (fpsCandidates.Count > 1)
                        {
                            result.Parcentile99Fps = Math.Round(fpsCandidates[1].Value, 1);
                        }
                    }
                    else
                    {
                        result.AverageFps = Math.Round(foundValues[0].Value, 1);
                    }
                }
            }
            catch { }
        }

        private static string ExtractPropertyNameBefore(byte[] bytes, int propTypeIndex)
        {
            int nameEnd = propTypeIndex - 1;
            while (nameEnd > 0 && bytes[nameEnd] == 0) nameEnd--;

            int nameStart = nameEnd;
            while (nameStart > 0 && bytes[nameStart] >= 32 && bytes[nameStart] <= 126)
            {
                nameStart--;
            }

            if (nameStart < nameEnd)
            {
                return Encoding.ASCII.GetString(bytes, nameStart + 1, nameEnd - nameStart);
            }
            return string.Empty;
        }

        private static double ExtractValueAfter(byte[] bytes, int startIndex, string propType)
        {
            for (int offset = 8; offset <= 32; offset++)
            {
                if (startIndex + offset + 4 <= bytes.Length)
                {
                    if (propType == "IntProperty")
                    {
                        int intVal = BitConverter.ToInt32(bytes, startIndex + offset);
                        if (intVal >= 5 && intVal <= 300) return intVal;
                    }
                    else if (propType == "FloatProperty")
                    {
                        float floatVal = BitConverter.ToSingle(bytes, startIndex + offset);
                        if (floatVal >= 5.0f && floatVal <= 300.0f && !float.IsNaN(floatVal) && !float.IsInfinity(floatVal))
                        {
                            return floatVal;
                        }
                    }
                    else if (propType == "Int64Property" && startIndex + offset + 8 <= bytes.Length)
                    {
                        long longVal = BitConverter.ToInt64(bytes, startIndex + offset);
                        if (longVal >= 5 && longVal <= 300) return longVal;
                    }
                }
            }
            return 0;
        }

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