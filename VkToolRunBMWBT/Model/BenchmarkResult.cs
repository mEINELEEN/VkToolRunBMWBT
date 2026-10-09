namespace VkToolRunBMWBT.Model
{
    /// <summary>Результаты одного прохода Black Myth: Wukong Benchmark Tool.</summary>
    public class BenchmarkResult
    {
        public string TestName { get; set; } = string.Empty;
        public double AverageFps { get; set; }

        // В старом проекте свойство так названо. Имя сохранено, чтобы не ломать существующие ссылки.
        public double Parcentile99Fps { get; set; }
        public bool IsOnePercentLowEstimated { get; set; }
        public double MinFps { get; set; }
        public double MaxFps { get; set; }
        public double CpuFrameTimeMs { get; set; }
        public double GpuFrameTimeMs { get; set; }
        public double CpuUsagePercent { get; set; }
        public double GpuUsagePercent { get; set; }
        public double VramGb { get; set; }

        public string Resolution { get; set; } = string.Empty;
        public string RayTrasingSetting { get; set; } = string.Empty;
        public string OverallQualityPreset { get; set; } = string.Empty;
        public string RawSettingSummary { get; set; } = string.Empty;
        public string RenderScale { get; set; } = string.Empty;
        public string ResultFilePath { get; set; } = string.Empty;
    }
}
