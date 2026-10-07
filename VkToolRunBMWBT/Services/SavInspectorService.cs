using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VkToolRunBMWBT.Services
{
    public static class SavInspectorService
    {
        public static string InspectManually()
        {
            string filePath = "";

            // Открываем диалоговое окно выбора файла в STA-потоке (требование WinForms)
            var t = new Thread(() =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "ВЫБЕРИТЕ ФАЙЛ (UserSettingSaveGame.sav ИЛИ лог из папки Logs)";
                    ofd.Filter = "Файлы UE5|*.*|Сохранения (.sav)|*.sav|Логи (.log)|*.log";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        filePath = ofd.FileName;
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (string.IsNullOrEmpty(filePath))
            {
                return "Выбор файла отменен пользователем.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== ДИАГНОСТИКА ФАЙЛА: {Path.GetFileName(filePath)} ===");

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);

                // Если ты выберешь текстовый лог (.log или .txt)
                if (filePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("--- [ТЕКСТОВЫЙ ЛОГ: СТРОКИ СО СЛОВАМИ FPS, BENCHMARK, AVERAGE] ---");
                    string text = Encoding.UTF8.GetString(bytes);
                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        // Фильтруем лог: выводим только то, где есть зацепки про FPS
                        if (line.IndexOf("fps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            line.IndexOf("average", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            line.IndexOf("benchmark", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sb.AppendLine(line.Trim());
                        }
                    }
                }
                else
                {
                    // Если ты выберешь бинарный .sav
                    string asciiText = Encoding.ASCII.GetString(bytes);
                    var jsonMatches = Regex.Matches(asciiText, @"\{[^{}]*\}");

                    sb.AppendLine("--- [1. НАЙДЕННЫЕ JSON-СТРУКТУРЫ] ---");
                    foreach (Match m in jsonMatches) if (m.Value.Length > 5) sb.AppendLine(m.Value);

                    sb.AppendLine("\n--- [2. ВСЕ ТЕКСТОВЫЕ КЛЮЧИ ИЗ БИНАРНИКА] ---");
                    var asciiStrings = ExtractPrintableStrings(bytes, Encoding.ASCII, 4);
                    foreach (var s in asciiStrings) if (Regex.IsMatch(s, @"[a-zA-Z]{4,}")) sb.AppendLine("  " + s);
                }

                // Сохраняем и открываем Блокнот
                string dumpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_dump.txt");
                File.WriteAllText(dumpPath, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dumpPath,
                    UseShellExecute = true
                });

                return $"Дамп открыт в Блокноте!";
            }
            catch (Exception ex)
            {
                return $"Ошибка при чтении файла: {ex.Message}";
            }
        }
        private static List<string> ExtractPrintableStrings(byte[] bytes, Encoding encoding, int minLength)
        {
            var results = new List<string>();
            var currentBytes = new List<byte>();
            int step = (encoding == Encoding.Unicode) ? 2 : 1;

            for (int i = 0; i < bytes.Length; i += step)
            {
                char ch = ' ';
                if (encoding == Encoding.ASCII) ch = (char)bytes[i];
                else if (i + 1 < bytes.Length) ch = BitConverter.ToChar(bytes, i);

                if (!char.IsControl(ch) && ch != '\0')
                {
                    currentBytes.Add(bytes[i]);
                    if (step == 2) currentBytes.Add(bytes[i + 1]);
                }
                else
                {
                    if (currentBytes.Count >= minLength * step)
                    {
                        results.Add(encoding.GetString(currentBytes.ToArray()).Trim());
                    }
                    currentBytes.Clear();
                }
            }
            return results;
        }
    }
}