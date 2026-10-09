using System.Globalization;
using System.Text.Json;
using VkToolRunBMWBT.Model;

namespace VkToolRunBMWBT.Services
{
    /// <summary>Извлекает метрики из JSON, который создаёт сам Benchmark Tool.</summary>
    public static class ResultParserService
    {
        public static BenchmarkResult GetLatestResult(
            string testName,
            string expectedResolution,
            string expectedScale,
            string? resultFilePath = null)
        {
            string path = string.IsNullOrWhiteSpace(resultFilePath)
                ? FindLatestResultFile()
                : resultFilePath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("JSON-файл результата benchmark не найден.", path);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("JSON-результат benchmark имеет неожиданный формат: ожидался объект.");

            double? averageFps = ReadNumber(root, "FPSAvg", "AverageFPS", "AvgFPS");
            if (!averageFps.HasValue || averageFps.Value <= 0)
                throw new InvalidDataException("В JSON benchmark отсутствует корректное поле FPSAvg.");

            var result = new BenchmarkResult
            {
                TestName = testName,
                ResultFilePath = path,
                AverageFps = averageFps.Value,
                MinFps = ReadNumber(root, "FPSMin", "MinFPS") ?? 0,
                MaxFps = ReadNumber(root, "FPSMax", "MaxFPS") ?? 0,
                Resolution = ReadString(root, "ScreenResolution", "Resolution") ?? expectedResolution,
                RenderScale = FormatPercent(ReadNumber(root, "ImageQuality", "RenderScale", "ResolutionQuality"), expectedScale),
                CpuUsagePercent = ReadNumber(root, "CPUAvg", "CPUUsage", "CpuUsagePercent") ?? 0,
                GpuUsagePercent = ReadNumber(root, "GPUAvg", "GPUUsage", "GpuUsagePercent") ?? 0,
                VramGb = ReadNumber(root, "VideoMem", "VramGb", "VRAMUsed") ?? 0,
                RayTrasingSetting = FormatRayTracing(ReadNumber(root, "Rtx", "RayTracing", "RayTracingQuality")),
                OverallQualityPreset = FormatQuality(ReadNumber(root, "ShadowQuality", "OverallQuality", "Quality"))
            };

            ReadFrameRecords(root, result);
            double? storedOnePercentLow = ReadOnePercentLow(root);
            if (storedOnePercentLow.HasValue && storedOnePercentLow.Value > 0)
            {
                result.Parcentile99Fps = storedOnePercentLow.Value;
            }
            else
            {
                result.Parcentile99Fps = CalculateOnePercentLow(root);
                result.IsOnePercentLowEstimated = result.Parcentile99Fps > 0;
            }

            string motionBlur = FormatMotionBlur(ReadNumber(root, "MotionBlur"));
            string viewDistance = FormatQuality(ReadNumber(root, "ViewDistance"));
            result.RawSettingSummary =
                $"Файл: {Path.GetFileName(path)} | Разрешение: {result.Resolution} | Рендер: {result.RenderScale} | " +
                $"Качество: {result.OverallQualityPreset} | Дальность: {viewDistance} | " +
                $"Ray Tracing: {result.RayTrasingSetting} | Motion Blur: {motionBlur}";

            return result;
        }

        private static string FindLatestResultFile()
        {
            var candidates = new List<string>();
            candidates.Add(Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "b1", "Saved", "BenchMarkHistory", "Tool"));
            if (!string.IsNullOrWhiteSpace(RunnerService.DetectedSavedFolderPath))
                candidates.Add(Path.Combine(RunnerService.DetectedSavedFolderPath, "BenchMarkHistory", "Tool"));

            return candidates
                .Where(Directory.Exists)
                .SelectMany(directory =>
                {
                    try { return Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly); }
                    catch { return Array.Empty<string>(); }
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault() ?? string.Empty;
        }

        private static void ReadFrameRecords(JsonElement root, BenchmarkResult result)
        {
            if (!TryGetProperty(root, "Records", out JsonElement records) || records.ValueKind != JsonValueKind.Array)
                return;

            double cpuTotal = 0;
            double gpuTotal = 0;
            int cpuCount = 0;
            int gpuCount = 0;

            foreach (JsonElement record in records.EnumerateArray())
            {
                double? cpuTime = ReadNumber(record, "CPUFrameTime", "CpuFrameTimeMs");
                double? gpuTime = ReadNumber(record, "GPUFrameTime", "GpuFrameTimeMs");
                if (cpuTime.HasValue && cpuTime.Value > 0)
                {
                    cpuTotal += cpuTime.Value;
                    cpuCount++;
                }
                if (gpuTime.HasValue && gpuTime.Value > 0)
                {
                    gpuTotal += gpuTime.Value;
                    gpuCount++;
                }
            }

            if (cpuCount > 0) result.CpuFrameTimeMs = cpuTotal / cpuCount;
            if (gpuCount > 0) result.GpuFrameTimeMs = gpuTotal / gpuCount;
        }

        private static double? ReadOnePercentLow(JsonElement root) =>
            ReadNumber(root, "FPS1Low", "FPSLow1", "FPS1PercentLow", "FPSLow1Percent",
                "FPS99Percentile", "FPSPercentile99", "Percentile99FPS", "OnePercentLowFPS");

        private static double CalculateOnePercentLow(JsonElement root)
        {
            if (!TryGetProperty(root, "Records", out JsonElement records) || records.ValueKind != JsonValueKind.Array)
                return 0;

            var fpsSamples = new List<double>();
            var frameTimeSamples = new List<double>();

            foreach (JsonElement record in records.EnumerateArray())
            {
                double? fps = ReadNumber(record, "FPS", "FrameRate", "FPSValue", "FrameFPS");
                if (fps.HasValue && fps.Value > 0 && fps.Value < 1000)
                {
                    fpsSamples.Add(fps.Value);
                    continue;
                }

                // Benchmark Tool публикует CPUFrameTime и GPUFrameTime для каждой записи.
                // Максимум двух времён — узкое место кадра; FPS кадра приблизительно равен 1000 / ms.
                double? cpuTime = ReadNumber(record, "CPUFrameTime", "CpuFrameTimeMs");
                double? gpuTime = ReadNumber(record, "GPUFrameTime", "GpuFrameTimeMs");
                double limitingFrameTime = Math.Max(cpuTime ?? 0, gpuTime ?? 0);
                if (limitingFrameTime > 0 && limitingFrameTime < 10000)
                    frameTimeSamples.Add(1000.0 / limitingFrameTime);
            }

            if (fpsSamples.Count > 0)
                return AverageLowestOnePercent(fpsSamples);

            // Если JSON не содержит per-frame FPS, оцениваем 1% Low из CPU/GPU-времени кадра.
            return frameTimeSamples.Count > 0 ? AverageLowestOnePercent(frameTimeSamples) : 0;
        }

        private static double AverageLowestOnePercent(List<double> samples)
        {
            samples.Sort();
            int onePercentCount = Math.Max(1, (int)Math.Ceiling(samples.Count * 0.01));
            return samples.Take(onePercentCount).Average();
        }

        private static double? ReadNumber(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (!TryGetProperty(element, name, out JsonElement value)) continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double numeric))
                    return numeric;

                if (value.ValueKind == JsonValueKind.String &&
                    double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out numeric))
                    return numeric;
            }
            return null;
        }

        private static string? ReadString(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (!TryGetProperty(element, name, out JsonElement value)) continue;
                if (value.ValueKind == JsonValueKind.String)
                {
                    string? text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
                if (value.ValueKind == JsonValueKind.Number) return value.ToString();
            }
            return null;
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
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

        private static string FormatPercent(double? value, string fallback)
        {
            if (!value.HasValue) return fallback;
            return $"{value.Value.ToString("0.##", CultureInfo.InvariantCulture)}%";
        }

        private static string FormatRayTracing(double? value)
        {
            if (!value.HasValue) return "неизвестно";
            return value.Value <= 0 ? "выключен" : $"уровень {value.Value:0}";
        }

        private static string FormatQuality(double? value)
        {
            if (!value.HasValue) return "неизвестно";
            int level = (int)Math.Round(value.Value);
            return level switch
            {
                0 => "Low",
                1 => "Low",
                2 => "Medium",
                3 => "High",
                4 => "Very High",
                5 => "Cinematic",
                _ => $"уровень {level}"
            };
        }

        private static string FormatMotionBlur(double? value)
        {
            if (!value.HasValue) return "неизвестно";
            return (int)Math.Round(value.Value) switch
            {
                0 => "выключен",
                1 => "Weak",
                2 => "Strong",
                _ => $"уровень {value.Value:0}"
            };
        }
    }
}
