
using System.Text;
using System.Text.RegularExpressions;

namespace VkToolRunBMWBT.Services
{
    /// <summary>
    /// Управляет GameUserSettings.ini.
    /// Исходные байты восстанавливаются после завершения тестирования.
    /// </summary>
    public static class ConfigService
    {
        private const string GameSettingsSection =
            "/Script/GSGameSettings.GSGameUserSettings";

        private const string ScalabilitySection = "ScalabilityGroups";

        private const string GameUiSettingsSection =
            "/Script/GSGameSettings.GSGameUserSettings";

        private static string? _activeConfigFilePath;
        private static byte[]? _originalConfigBytes;
        private static bool _configExistedBeforeBackup;
        private static bool _backupTaken;

        public static string CurrentConfigFilePath =>
            _activeConfigFilePath ?? ResolveConfigFilePath();

        public static void BackupConfig()
        {
            if (_backupTaken)
            {
                throw new InvalidOperationException(
                    "Резервная копия уже создана. " +
                    "Сначала восстановите конфигурацию предыдущего теста.");
            }

            _activeConfigFilePath = ResolveConfigFilePath();

            string directory =
                Path.GetDirectoryName(_activeConfigFilePath)
                ?? throw new InvalidOperationException(
                    "Не удалось определить папку GameUserSettings.ini.");

            Directory.CreateDirectory(directory);

            _configExistedBeforeBackup =
                File.Exists(_activeConfigFilePath);

            _originalConfigBytes = _configExistedBeforeBackup
                ? File.ReadAllBytes(_activeConfigFilePath)
                : Array.Empty<byte>();

            _backupTaken = true;
        }

        public static void RestoreConfig()
        {
            if (!_backupTaken ||
                string.IsNullOrWhiteSpace(_activeConfigFilePath))
            {
                return;
            }

            string path = _activeConfigFilePath;

            string directory =
                Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException(
                    "Не удалось определить папку для восстановления конфигурации.");

            if (_configExistedBeforeBackup)
            {
                Directory.CreateDirectory(directory);

                string temporaryPath = path + ".restore.tmp";

                try
                {
                    File.WriteAllBytes(
                        temporaryPath,
                        _originalConfigBytes ?? Array.Empty<byte>());

                    File.Move(temporaryPath, path, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }
            else if (File.Exists(path))
            {
                // Файл отсутствовал до теста, поэтому удаляем созданный файл.
                File.Delete(path);
            }

            _originalConfigBytes = null;
            _configExistedBeforeBackup = false;
            _backupTaken = false;
            _activeConfigFilePath = null;
        }

        /// <summary>
        /// CPU: 1280x720, масштаб рендеринга 25%, минимальное качество.
        /// </summary>
        public static void ApplyCpuTestConfig()
        {
            ApplyProfile(
                width: 1280,
                height: 720,
                renderPercent: 25,
                qualityLevel: 0);
        }

        /// <summary>
        /// GPU: разрешение основного монитора, масштаб 100%, Cinematic.
        /// </summary>
        public static void ApplyGpuTestConfig(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Разрешение GPU-теста должно быть больше нуля.");
            }

            ApplyProfile(
                width,
                height,
                renderPercent: 100,
                qualityLevel: 4);
        }

        public static void ApplyGpuTestConfig()
        {
            var resolution = RunnerService.GetDesktopResolution();

            ApplyGpuTestConfig(
                resolution.Width,
                resolution.Height);
        }

        private static void ApplyProfile(
            int width,
            int height,
            int renderPercent,
            int qualityLevel)
        {
            EnsureConfigFileExists();

            int renderWidth = Math.Max(
                1,
                (int)Math.Round(width * renderPercent / 100.0));

            int renderHeight = Math.Max(
                1,
                (int)Math.Round(height * renderPercent / 100.0));

            string[][] gameSettings =
            {
                new[] { "ResolutionSizeX", width.ToString() },
                new[] { "ResolutionSizeY", height.ToString() },

                new[] { "LastUserConfirmedResolutionSizeX", width.ToString() },
                new[] { "LastUserConfirmedResolutionSizeY", height.ToString() },

                // Внутреннее разрешение рендеринга.
                new[] { "DesiredScreenWidth", renderWidth.ToString() },
                new[] { "DesiredScreenHeight", renderHeight.ToString() },

                new[] { "LastUserConfirmedDesiredScreenWidth", renderWidth.ToString() },
                new[] { "LastUserConfirmedDesiredScreenHeight", renderHeight.ToString() },

                new[] { "bUseVSync", "False" },
                new[] { "FrameRateLimit", "0.000000" }
            };

            foreach (string[] item in gameSettings)
            {
                UpdateIniKey(
                    GameSettingsSection,
                    item[0],
                    item[1]);
            }

            // Синхронизируем сохранённую настройку графического интерфейса.
            UpdateUiSettingData(
                "ImageQuality",
                renderHeight.ToString());

            // Масштаб рендеринга в ScalabilityGroups.
            UpdateIniKey(
                ScalabilitySection,
                "sg.ResolutionQuality",
                $"{renderPercent}.000000");

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
            {
                UpdateIniKey(
                    ScalabilitySection,
                    key,
                    qualityLevel.ToString());
            }
        }

        private static string ResolveConfigFilePath()
        {
            string localAppDataPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "b1",
                "Saved",
                "Config",
                "Windows",
                "GameUserSettings.ini");

            string gameFolder = RunnerService.GetGameInstallFolder();

            if (!string.IsNullOrWhiteSpace(gameFolder))
            {
                string installedPath = Path.Combine(
                    gameFolder,
                    "b1",
                    "Saved",
                    "Config",
                    "Windows",
                    "GameUserSettings.ini");

                if (File.Exists(installedPath))
                {
                    return installedPath;
                }

                if (File.Exists(localAppDataPath))
                {
                    return localAppDataPath;
                }

                return installedPath;
            }

            return localAppDataPath;
        }

        private static void EnsureConfigFileExists()
        {
            string path = CurrentConfigFilePath;

            string? directory = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "Не удалось определить папку настроек игры.");
            }

            Directory.CreateDirectory(directory);

            if (!File.Exists(path))
            {
                File.WriteAllText(
                    path,
                    $"[{GameSettingsSection}]\r\n" +
                    $"[{ScalabilitySection}]\r\n" +
                    "[/Script/Engine.GameUserSettings]\r\n",
                    new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// Изменяет ключ только в заданной секции INI.
        /// </summary>
        private static void UpdateIniKey(
            string sectionName,
            string key,
            string value)
        {
            string path = CurrentConfigFilePath;

            var lines = File.Exists(path)
                ? File.ReadAllLines(path, Encoding.UTF8).ToList()
                : new List<string>();

            int sectionStart =
                FindSectionStart(lines, sectionName);

            if (sectionStart < 0)
            {
                if (lines.Count > 0 &&
                    !string.IsNullOrWhiteSpace(lines[^1]))
                {
                    lines.Add(string.Empty);
                }

                lines.Add($"[{sectionName}]");
                sectionStart = lines.Count - 1;
            }

            int sectionEnd =
                FindNextSection(lines, sectionStart + 1);

            bool updated = false;

            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                string trimmed = lines[i].TrimStart();

                if (!trimmed.StartsWith(
                    key + "=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lines[i] = $"{key}={value}";
                updated = true;
            }

            if (!updated)
            {
                sectionEnd =
                    FindNextSection(lines, sectionStart + 1);

                lines.Insert(sectionEnd, $"{key}={value}");
            }

            File.WriteAllText(
                path,
                string.Join(Environment.NewLine, lines) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        /// <summary>
        /// Обновляет значение ImageQuality в UISettingData.
        /// </summary>
        private static void UpdateUiSettingData(
            string key,
            string value)
        {
            string path = CurrentConfigFilePath;

            var lines =
                File.ReadAllLines(path, Encoding.UTF8).ToList();

            int sectionStart =
                FindSectionStart(lines, GameUiSettingsSection);

            if (sectionStart < 0)
            {
                throw new InvalidDataException(
                    $"В GameUserSettings.ini не найдена секция " +
                    $"[{GameUiSettingsSection}]. " +
                    "Запустите Benchmark Tool вручную один раз, " +
                    "затем повторите тест.");
            }

            int sectionEnd =
                FindNextSection(lines, sectionStart + 1);

            int dataLineIndex = -1;

            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                if (lines[i].TrimStart().StartsWith(
                    "UISettingData=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    dataLineIndex = i;
                    break;
                }
            }

            if (dataLineIndex < 0)
            {
                throw new InvalidDataException(
                    "В секции настроек игры не найден UISettingData. " +
                    "Тест остановлен, чтобы не запускать его " +
                    "с неподходящим масштабом.");
            }

            string originalLine = lines[dataLineIndex];

            string pattern =
                @"\(""" + Regex.Escape(key) +
                @"""\s*,\s*""[^""]*""\)";

            string replacement = $"(\"{key}\", \"{value}\")";

            string updatedLine = Regex.Replace(
                originalLine,
                pattern,
                replacement,
                RegexOptions.IgnoreCase);

            if (string.Equals(
                updatedLine,
                originalLine,
                StringComparison.Ordinal))
            {
                int closingParen = updatedLine.LastIndexOf(')');

                if (closingParen < 0)
                {
                    throw new InvalidDataException(
                        "Формат UISettingData не распознан.");
                }

                string prefix =
                    updatedLine[..closingParen].TrimEnd();

                string separator =
                    prefix.EndsWith("(",
                        StringComparison.Ordinal)
                        ? string.Empty
                        : ",";

                updatedLine =
                    prefix +
                    separator +
                    $"(\"{key}\", \"{value}\")" +
                    updatedLine[closingParen..];
            }

            lines[dataLineIndex] = updatedLine;

            File.WriteAllText(
                path,
                string.Join(Environment.NewLine, lines) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static int FindSectionStart(
            List<string> lines,
            string sectionName)
        {
            string expected =
                $"[{sectionName.Trim('[', ']')}]";

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.Equals(
                    lines[i].Trim(),
                    expected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindNextSection(
            List<string> lines,
            int startIndex)
        {
            for (int i = startIndex; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                if (line.Length >= 2 &&
                    line[0] == '[' &&
                    line[^1] == ']')
                {
                    return i;
                }
            }

            return lines.Count;
        }
    }
}