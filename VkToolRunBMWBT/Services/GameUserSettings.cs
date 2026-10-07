using System;
using System.IO;
using System.Text.RegularExpressions;
using VkToolRunBMWBT.Model;
namespace VkToolRunBMWBT.Services
{
    public static class GameUserSettingsParser
    {
        /// <summary>
        /// Считывает графические настройки из GameUserSettings.ini и заполняет ими BenchmarkResult
        /// </summary>
        public static void ReadConfigFile(string iniFilePath, BenchmarkResult result)
        {
            if (string.IsNullOrEmpty(iniFilePath) || !File.Exists(iniFilePath))
                return;

            try
            {
                string content = File.ReadAllText(iniFilePath);

                // 1. Извлечение разрешения (ResolutionSizeX x ResolutionSizeY)
                var resX = Regex.Match(content, @"ResolutionSizeX=(\d+)");
                var resY = Regex.Match(content, @"ResolutionSizeY=(\d+)");
                if (resX.Success && resY.Success)
                {
                    result.Resolution = $"{resX.Groups[1].Value}x{resY.Groups[1].Value}";
                }

                // 2. Извлечение масштаба рендеринга (sg.ResolutionQuality)
                var scaleMatch = Regex.Match(content, @"sg\.ResolutionQuality=(\d+)");
                if (scaleMatch.Success)
                {
                    result.RenderScale = $"{scaleMatch.Groups[1].Value}%";
                }

                // 3. Формирование строки описания конфигурации
                result.RawSettingSummary = $"Конфиг: GameUserSettings.ini | Разрешение: {result.Resolution} | Масштаб: {result.RenderScale}";
            }
            catch (Exception ex)
            {
                result.RawSettingSummary = $"Ошибка чтения ini: {ex.Message}";
            }
        }
    }
}