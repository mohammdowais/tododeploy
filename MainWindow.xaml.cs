using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Velopack;
using Velopack.Sources;
using Windows.Graphics;

namespace tododeploy
{
    public sealed partial class MainWindow : Window
    {
        private const string RepoUrl = "https://github.com/mohammdowais/tododeploy";

        public MainWindow()
        {
            InitializeComponent();
            Title = "Todo Studio";
            AppWindow.Resize(new SizeInt32(1460, 940));
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdatesButton.IsEnabled = false;

            try
            {
                var manager = new UpdateManager(new GithubSource(RepoUrl, string.Empty, false));

                if (!manager.IsInstalled)
                {
                    UpdateStatusText.Text = "Updates are available only from an installed GitHub release build.";
                    await ShowMessageAsync(
                        "Updates unavailable",
                        "This copy is not running from a Velopack-installed release yet. Install the app from a GitHub Release package first, then this button can download and apply updates.");
                    return;
                }

                if (manager.UpdatePendingRestart is not null)
                {
                    var pending = manager.UpdatePendingRestart;
                    UpdateStatusText.Text = "An update is already downloaded and ready to install.";

                    if (await AskToInstallAsync($"Version {pending.Version} is ready to install. Restart and apply it now?"))
                    {
                        manager.ApplyUpdatesAndRestart(pending);
                    }

                    return;
                }

                UpdateStatusText.Text = "Checking GitHub Releases for updates...";
                var update = await manager.CheckForUpdatesAsync();

                if (update is null)
                {
                    var currentVersion = manager.CurrentVersion?.ToString() ?? "current";
                    UpdateStatusText.Text = $"You're up to date on version {currentVersion}.";
                    await ShowMessageAsync("No updates", $"No newer GitHub Release was found. Current version: {currentVersion}.");
                    return;
                }

                var targetVersion = update.TargetFullRelease.Version.ToString();
                UpdateStatusText.Text = $"Downloading version {targetVersion}...";
                await manager.DownloadUpdatesAsync(update, progress =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusText.Text = $"Downloading version {targetVersion}... {progress}%";
                    });
                });

                UpdateStatusText.Text = $"Version {targetVersion} is ready to install.";
                if (await AskToInstallAsync($"Version {targetVersion} has been downloaded. Install it and restart now?"))
                {
                    manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Update check failed.";
                await ShowMessageAsync("Update check failed", ex.Message);
            }
            finally
            {
                CheckForUpdatesButton.IsEnabled = true;
            }
        }

        private async Task<bool> AskToInstallAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Update available",
                Content = message,
                PrimaryButtonText = "Install and restart",
                CloseButtonText = "Later",
                XamlRoot = RootGrid.XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = RootGrid.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
