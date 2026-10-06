using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkToolRunBMWBT.Model
{
    // результаты одного прогона бенчмарка

    public class BenchmarkResult
    {
        public string TestName { get; set; } = string.Empty;
        public double AverageFps { get; set; }
        public double Parcentile99Fps { get; set; } // 1% low fps
        public string Resolution { get; set; } = string.Empty;
        public string RayTrasingSetting { get; set; } = string.Empty;
        public string OverallQualityPreset { get; set; } = string.Empty;
        public string RswSettingSummary { get; set; } = string.Empty; // текстовая сводка настроек



    }

}
