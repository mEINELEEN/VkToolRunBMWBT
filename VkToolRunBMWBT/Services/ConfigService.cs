using System.Text;

namespace VkToolRunBMWBT.Services
{
    /// <summary>
    /// Управляет GameUserSettings.ini. Оригинальные байты держатся в памяти и
    /// восстанавливаются даже при исключении во время любого из проходов.
    /// </summary>
    public static class ConfigService
    {
        private const string GameSettingsSection = "/Script/Engine.GameUserSettings";
        private const string ScalabilitySection = "ScalabilityGroups";

        private static string? _activeConfigFilePath;
        private static byte[]? _originalConfigBytes;
        private static bool _configExistedBeforeBackup;
        private static bool _backupTaken;

        public static string CurrentConfigFilePath => _activeConfigFilePath ?? ResolveConfigFilePath();

        public static void BackupConfig()
        {
            if (_backupTaken)
                throw new InvalidOperationException("Резервная копия уже создана. Сначала восстановите конфигурацию предыдущего теста.");

            _activeConfigFilePath = ResolveConfigFilePath();
            string directory = Path.GetDirectoryName(_activeConfigFilePath)
                ?? throw new InvalidOperationException("Не удалось определить папку GameUserSettings.ini.");

            Directory.CreateDirectory(directory);
            _configExistedBeforeBackup = File.Exists(_activeConfigFilePath);
            _originalConfigBytes = _configExistedBeforeBackup
                ? File.ReadAllBytes(_activeConfigFilePath)
                : Array.Empty<byte>();
            _backupTaken = true;
        }

        public static void RestoreConfig()
        {
            if (!_backupTaken || string.IsNullOrWhiteSpace(_activeConfigFilePath))
                return;

            string path = _activeConfigFilePath;
            string directory = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Не удалось определить папку для восстановления конфигурации.");

            if (_configExistedBeforeBackup)
            {
                Directory.CreateDirectory(directory);
                string temporaryPath = path + ".restore.tmp";
                try
                {
                    File.WriteAllBytes(temporaryPath, _originalConfigBytes ?? Array.Empty<byte>());
                    File.Move(temporaryPath, path, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
            else if (File.Exists(path))
            {
                // Файл был создан только для автотеста — после завершения он не должен оставаться.
                File.Delete(path);
            }

            _originalConfigBytes = null;
            _configExistedBeforeBackup = false;
            _backupTaken = false;
            _activeConfigFilePath = null;
        }

        /// <summary>CPU-проход: 1280×720, рендер 25%, минимальные настройки.</summary>
        public static void ApplyCpuTestConfig()
        {
            ApplyProfile(width: 1280, height: 720, renderPercent: 25, qualityLevel: 0);
        }

        /// <summary>GPU-проход: родное разрешение основного экрана, рендер 100%, Cinematic.</summary>
        public static void ApplyGpuTestConfig(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Разрешение GPU-теста должно быть больше нуля.");

            ApplyProfile(width, height, renderPercent: 100, qualityLevel: 4);
        }

        public static void ApplyGpuTestConfig()
        {
            var resolution = RunnerService.GetDesktopResolution();
            ApplyGpuTestConfig(resolution.Width, resolution.Height);
        }

        private static void ApplyProfile(int width, int height, int renderPercent, int qualityLevel)
        {
            EnsureConfigFileExists();

            string[][] gameSettings =
            {
                new[] { "ResolutionSizeX", width.ToString() },
                new[] { "ResolutionSizeY", height.ToString() },
                new[] { "LastUserConfirmedResolutionSizeX", width.ToString() },
                new[] { "LastUserConfirmedResolutionSizeY", height.ToString() },
                new[] { "DesiredScreenWidth", width.ToString() },
                new[] { "DesiredScreenHeight", height.ToString() },
                new[] { "bUseVSync", "False" },
                new[] { "FrameRateLimit", "0.000000" }
            };

            foreach (string[] item in gameSettings)
                UpdateIniKey(GameSettingsSection, item[0], item[1]);

            UpdateIniKey(ScalabilitySection, "sg.ResolutionQuality", $"{renderPercent}.000000");

            string[] qualityKeys =
            {
                "sg.ViewDistanceQuality",
                "sg.AntiAliasingQuality",
                "sg.ShadowQuality",
                "sg.GlobalIlluminationQuality",
                "sg.ReflectionQuality",
                "sg.PostProcessQuality",
                "sg.TextureQuality",
                "sg.EffectsQuality",
                "sg.FoliageQuality",
                "sg.ShadingQuality"
            };

            foreach (string key in qualityKeys)
                UpdateIniKey(ScalabilitySection, key, qualityLevel.ToString());
        }

        private static string ResolveConfigFilePath()
        {
            string localAppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

            string gameFolder = RunnerService.GetGameInstallFolder();
            if (!string.IsNullOrWhiteSpace(gameFolder))
            {
                string installedPath = Path.Combine(gameFolder, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

                // Если конфиг присутствует в папке установленного приложения, он приоритетный.
                // Если его нет, но есть AppData-конфиг, используем существующий пользовательский файл.
                if (File.Exists(installedPath)) return installedPath;
                if (File.Exists(localAppDataPath)) return localAppDataPath;

                // Поведение соответствует исходному консольному решению: создать конфиг в папке Tool.
                return installedPath;
            }

            return localAppDataPath;
        }

        private static void EnsureConfigFileExists()
        {
            string path = CurrentConfigFilePath;
            string? directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Не удалось определить папку настроек игры.");

            Directory.CreateDirectory(directory);
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    $"[{GameSettingsSection}]\r\n[{ScalabilitySection}]\r\n",
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        /// <summary>
        /// Обновляет ключ только внутри нужной INI-секции. Это важно, когда один ключ
        /// встречается в нескольких секциях; другие разделы файла не затрагиваются.
        /// </summary>
        private static void UpdateIniKey(string sectionName, string key, string value)
        {
            string path = CurrentConfigFilePath;
            var lines = File.Exists(path)
                ? File.ReadAllLines(path, Encoding.UTF8).ToList()
                : new List<string>();

            int sectionStart = FindSectionStart(lines, sectionName);
            if (sectionStart < 0)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                    lines.Add(string.Empty);
                lines.Add($"[{sectionName}]");
                sectionStart = lines.Count - 1;
            }

            int sectionEnd = FindNextSection(lines, sectionStart + 1);
            bool updated = false;
            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (!trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    continue;

                lines[i] = $"{key}={value}";
                updated = true;
            }

            if (!updated)
            {
                sectionEnd = FindNextSection(lines, sectionStart + 1);
                lines.Insert(sectionEnd, $"{key}={value}");
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static int FindSectionStart(List<string> lines, string sectionName)
        {
            string expected = $"[{sectionName.Trim('[', ']')}]";
            for (int i = 0; i < lines.Count; i++)
            {
                if (string.Equals(lines[i].Trim(), expected, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static int FindNextSection(List<string> lines, int startIndex)
        {
            for (int i = startIndex; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.Length >= 2 && line[0] == '[' && line[^1] == ']')
                    return i;
            }
            return lines.Count;
        }
    }
}
