using System;
using System.Windows.Forms;
using VkToolRunBMWBT.View;
using VkToolRunBMWBT.Presenter;

namespace VkToolRunBMWBT
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {

            ApplicationConfiguration.Initialize();

            // создаем view и presenter
            var mainForm = new MainForm();
            var presenter = new MainPresenter(mainForm);

            Application.Run(mainForm);
        }
    }
}