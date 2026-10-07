using System;
using System.Management;
using VkToolRunBMWBT.Model;

namespace VkToolRunBMWBT.Services
{
    public static class HardwareService
    {
        
        public static HardwareInfo GetSystemInfo()
        {
            var info = new HardwareInfo();

            try
            {
                // получаем модель процессора
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.CpuName = obj["Name"]?.ToString().Trim() ?? "Неизвестный CPU";
                        break;
                    }
                }

                // получаем наименование видеокарты
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string gpuName = obj["Name"]?.ToString().Trim();
                        // Игнорируем базовые/виртуальные драйверы, если есть несколько адаптеров
                        if (!string.IsNullOrEmpty(gpuName) && !gpuName.Contains("Basic Display"))
                        {
                            info.GpuName = gpuName;
                            break;
                        }
                    }
                }

                // получаем общий объем ОЗУ
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    ulong totalBytes = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["Capacity"] != null && ulong.TryParse(obj["Capacity"].ToString(), out ulong capacity))
                        {
                            totalBytes += capacity;
                        }
                    }

                    if (totalBytes > 0)
                    {
                        double gb = totalBytes / (1024.0 * 1024.0 * 1024.0);
                        info.RamCapacity = $"{Math.Round(gb, 1)} GB";
                    }
                }

                // получаем название операционной системы
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string caption = obj["Caption"]?.ToString().Trim() ?? "Windows";
                        string arch = obj["OSArchitecture"]?.ToString().Trim() ?? "";
                        info.OsVersion = $"{caption} ({arch})".Trim();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // если сбой WMI, возвращаем объект с сообщением об ошибке
                info.CpuName = $"Ошибка сбора данных: {ex.Message}";
            }

            return info;
        }
    }
}