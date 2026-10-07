using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkToolRunBMWBT.View;
using VkToolRunBMWBT.Model;
using VkToolRunBMWBT.Services;

namespace VkToolRunBMWBT.Presenter
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        public MainPresenter(IMainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            // офрмляем подписку на сабытия
            _view.StartBenchmarkRequested += OnStartBenchmarkRequested;
            _view.SaveReportRequested += OnSaveReportRequested;
        }

        

        private async void OnStartBenchmarkRequested(object sender, EventArgs e)
        {
            _view.EnableControls(false);
            try
            {
                
                _view.UpdateStatus("Сбор информации о компе...", 10);
                var hardwareInfo = await Task.Run(() => HardwareService.GetSystemInfo());
                _view.DisplayHardwareInfo(hardwareInfo);

                // резервная копия пользовательских настроек
                ConfigService.BackupConfig();

                _view.UpdateStatus("Применение настроек CPU...", 25);
                ConfigService.ApplyCpuTestConfig();

                _view.UpdateStatus("Запуск теста CPU...", 35);
                await Task.Delay(1000); // имитация задержки

                _view.UpdateStatus("Применение настроек GPU...", 65);
                ConfigService.ApplyGpuTestConfig();

                _view.UpdateStatus("Запуск теста GPU...", 75);
                await Task.Delay(1000); // имитация задержки
                
                _view.UpdateStatus("Тест завершен.", 100);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Произошла ошибка при выполнении теста: {ex.Message}");
                _view.UpdateStatus("Ошибка при выполнении теста.", 0);
            }
            finally
            {
                ConfigService.RestoreConfig();
                _view.EnableControls(true);
            }
        }

        private void OnSaveReportRequested(object sender, EventArgs e)
        {
            // логика сохранения отчета
            
        }
    }
}
