using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkToolRunBMWBT.Model
{

    // для сведений о конфигурации ПК
    public class HardwareInfo
    {
        public string CpuName { get; set; } = "Не найдено";
        public string GpuName { get; set; } = "Не найдено";
        public string RamName { get; set; } = "Не найдено";
        public string OsVersion { get; set; } = "Не найдено";
    }
}
