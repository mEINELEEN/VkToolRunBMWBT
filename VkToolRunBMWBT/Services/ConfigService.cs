using System;
using System.Collections.Generic;
using System.IO;

namespace VkToolRunBMWBT.Services
{
    public static class ConfigService
    {
        // путь к файлу настроек Unreal Engine 5 для Black Myth: Wukong Benchmark
        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"b1\Saved\Config\Windows\GameUserSettings.ini");

        private static string BackupFilePath => ConfigFilePath + ".bak";

        // создает резервную копию оригинального файла настроек
    
        public static void BackupConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    File.Copy(ConfigFilePath, BackupFilePath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при резервном копировании конфига: {ex.Message}");
            }
        }

        // восстанавливает оригинальный файл настроек из резервной копии

        public static void RestoreConfig()
        {
            try
            {
                if (File.Exists(BackupFilePath))
                {
                    File.Copy(BackupFilePath, ConfigFilePath, overwrite: true);
                    File.Delete(BackupFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при восстановлении конфига: {ex.Message}");
            }
        }


        // применяет настройки для CPU-теста (Минимальная нагрузка на GPU)
 
        public static void ApplyCpuTestConfig()
        {
            EnsureConfigFileExists();

            // Пониженное разрешение и рендер-скейл 50%
            UpdateIniKey("ResolutionSizeX", "1280");
            UpdateIniKey("ResolutionSizeY", "720");
            UpdateIniKey("LastUserConfirmedResolutionSizeX", "1280");
            UpdateIniKey("LastUserConfirmedResolutionSizeY", "720");
            UpdateIniKey("DesiredScreenWidth", "1280");
            UpdateIniKey("DesiredScreenHeight", "720");
            UpdateIniKey("sg.ResolutionQuality", "50.000000");

            // Качество графики: Low (0)
            UpdateIniKey("sg.ViewDistanceQuality", "0");
            UpdateIniKey("sg.AntiAliasingQuality", "0");
            UpdateIniKey("sg.ShadowQuality", "0");
            UpdateIniKey("sg.GlobalIlluminationQuality", "0");
            UpdateIniKey("sg.ReflectionQuality", "0");
            UpdateIniKey("sg.PostProcessQuality", "0");
            UpdateIniKey("sg.TextureQuality", "0");
            UpdateIniKey("sg.EffectsQuality", "0");
            UpdateIniKey("sg.FoliageQuality", "0");
            UpdateIniKey("sg.ShadingQuality", "0");

            // Отключение ограничений FPS
            UpdateIniKey("bUseVSync", "False");
            UpdateIniKey("FrameRateLimit", "0.000000");
        }


        // Применяет настройки для GPU-теста (Максимальная нагрузка на GPU)

        public static void ApplyGpuTestConfig()
        {
            EnsureConfigFileExists();

            // Максимальное разрешение и рендер-скейл 100%
            UpdateIniKey("ResolutionSizeX", "3840");
            UpdateIniKey("ResolutionSizeY", "2160");
            UpdateIniKey("LastUserConfirmedResolutionSizeX", "3840");
            UpdateIniKey("LastUserConfirmedResolutionSizeY", "2160");
            UpdateIniKey("DesiredScreenWidth", "3840");
            UpdateIniKey("DesiredScreenHeight", "2160");
            UpdateIniKey("sg.ResolutionQuality", "100.000000");

            // Качество графики: Cinematic/Ultra (3 или 4)
            UpdateIniKey("sg.ViewDistanceQuality", "3");
            UpdateIniKey("sg.AntiAliasingQuality", "3");
            UpdateIniKey("sg.ShadowQuality", "3");
            UpdateIniKey("sg.GlobalIlluminationQuality", "3");
            UpdateIniKey("sg.ReflectionQuality", "3");
            UpdateIniKey("sg.PostProcessQuality", "3");
            UpdateIniKey("sg.TextureQuality", "3");
            UpdateIniKey("sg.EffectsQuality", "3");
            UpdateIniKey("sg.FoliageQuality", "3");
            UpdateIniKey("sg.ShadingQuality", "3");

            // Отключение вертикальной синхронизации
            UpdateIniKey("bUseVSync", "False");
            UpdateIniKey("FrameRateLimit", "0.000000");
        }

        private static void EnsureConfigFileExists()
        {
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(ConfigFilePath))
            {
                File.WriteAllText(ConfigFilePath, "[/Script/Engine.GameUserSettings]\n[ScalabilityGroups]\n");
            }
        }


        // Вспомогательный метод обновления ключа в INI-файле без нарушения структуры

        private static void UpdateIniKey(string key, string value)
        {
            if (!File.Exists(ConfigFilePath)) return;

            string[] lines = File.ReadAllLines(ConfigFilePath);
            bool keyFound = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={value}";
                    keyFound = true;
                    break;
                }
            }

            if (!keyFound)
            {
                var lineList = new List<string>(lines) { $"{key}={value}" };
                lines = lineList.ToArray();
            }

            File.WriteAllLines(ConfigFilePath, lines);
        }
    }
}