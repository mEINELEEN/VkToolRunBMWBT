using System.Globalization;
using System.Text.RegularExpressions;
using VkToolRunBMWBT.Model;

namespace VkToolRunBMWBT.Services
{
    /// <summary>Считывает фактические настройки из INI. FPS берутся из JSON, а не из этого файла.</summary>
    public static class GameUserSettingsParser
    {
        public static void ReadConfigFile(string iniFilePath, BenchmarkResult result)
        {
            if (string.IsNullOrWhiteSpace(iniFilePath) || !File.Exists(iniFilePath) || result == null)
                return;

            try
            {
                string content = File.ReadAllText(iniFilePath);
                var resolutionX = Regex.Match(content, @"(?im)^\s*ResolutionSizeX\s*=\s*(\d+)");
                var resolutionY = Regex.Match(content, @"(?im)^\s*ResolutionSizeY\s*=\s*(\d+)");
                if (resolutionX.Success && resolutionY.Success)
                    result.Resolution = $"{resolutionX.Groups[1].Value}x{resolutionY.Groups[1].Value}";

                var scale = Regex.Match(content, @"(?im)^\s*sg\.ResolutionQuality\s*=\s*(\d+(?:[.,]\d+)?)");
                if (scale.Success && double.TryParse(scale.Groups[1].Value.Replace(',', '.'), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double scaleValue))
                {
                    result.RenderScale = $"{scaleValue:0.##}%";
                }

                var rt = Regex.Match(content, @"(?im)^\s*(?:r\.RayTracing|RayTracingQuality|sg\.RayTracingQuality)\s*=\s*(\d+)");
                if (rt.Success)
                {
                    int level = int.Parse(rt.Groups[1].Value, CultureInfo.InvariantCulture);
                    result.RayTrasingSetting = level == 0 ? "выключен" : $"уровень {level}";
                }

                result.RawSettingSummary =
                    $"Конфиг: {Path.GetFileName(iniFilePath)} | Разрешение: {result.Resolution} | Масштаб: {result.RenderScale}";
            }
            catch (Exception ex)
            {
                result.RawSettingSummary = $"Не удалось прочитать INI: {ex.Message}";
            }
        }
    }
}
