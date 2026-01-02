using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameLibrary.AvaloniaUI.Helpers;

namespace GameLibrary.AvaloniaUI.Controls.Modals;

public partial class Modal_Loading : UserControl
{
    public Modal_Loading()
    {
        InitializeComponent();
    }


    public async Task LoadTasks(bool showLoading, params Func<Task>[] tasks)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        if (showLoading)
        {
            lbl_Loading.Content = "Loading...";

            DateTime lastTime = DateTime.UtcNow;
            double averageTaskLength = 0;
            int tasksCompleted = 0;

            foreach (Func<Task> entry in tasks)
            {
                try
                {
                    await entry();

                    tasksCompleted++;

                    averageTaskLength = averageTaskLength + ((DateTime.UtcNow - lastTime).TotalSeconds - averageTaskLength) / tasksCompleted;
                    lastTime = DateTime.UtcNow;

                    float remainingPercentage = (float)Math.Round((tasksCompleted / (float)tasks.Length) * 100f);
                    float remainingTime = (float)Math.Round(averageTaskLength * (tasks.Length - tasksCompleted));

                    await Dispatcher.UIThread.InvokeAsync(() => lbl_Loading.Content = $"{remainingPercentage}% {remainingTime}");
                }
                catch (Exception e)
                {
                    await DialogHelper.OpenDatabaseExceptionDialog(e, "");
                }

            }
        }
        else
        {
            lbl_Loading.Content = "Loading";
            await Task.WhenAll(tasks.Select(x => x()));
        }
    }
}