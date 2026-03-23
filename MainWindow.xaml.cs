using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Velopack;
using Velopack.Sources;

namespace tododeploy
{
    public sealed partial class MainWindow : Window
    {
        private const string RepoUrl = "https://github.com/mohammdowais/tododeploy";
        private readonly TodoRepository _repository = new();
        private readonly AppLogger _logger = new();
        private TodoListViewModel? _editingList;
        private bool _isLoaded;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Todo Studio";
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            TodoLists = new ObservableCollection<TodoListViewModel>();
            DbPathText.Text = $"DB: {_repository.DbPath}";
            LogPathText.Text = $"Log: {_logger.LogFilePath}";
            Activated += MainWindow_Activated;
            _ = _logger.LogAsync("app.start", "Window created in full screen mode.");
        }

        public ObservableCollection<TodoListViewModel> TodoLists { get; }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await _repository.InitializeAsync();
            TodoLists.Clear();
            foreach (var list in await _repository.GetListsAsync())
            {
                TodoLists.Add(list);
            }

            RefreshOverview();
            RenderLists();
            await _logger.LogAsync("data.load", $"Loaded {TodoLists.Count} lists from SQLite.");
        }

        private async void SaveListButton_Click(object sender, RoutedEventArgs e)
        {
            var listTitle = (ListTitleTextBox.Text ?? string.Empty).Trim();
            var firstItem = (FirstItemTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(listTitle))
            {
                await _logger.LogAsync("list.save.blocked", "Attempted save without a title.");
                await ShowMessageAsync("List title required", "Enter a list title before saving.");
                return;
            }

            if (_editingList is null)
            {
                var newList = new TodoListViewModel { Title = listTitle };
                newList.Id = await _repository.InsertListAsync(listTitle);

                if (!string.IsNullOrWhiteSpace(firstItem))
                {
                    var item = new TodoItemViewModel { ListId = newList.Id, Title = firstItem };
                    item.Id = await _repository.InsertItemAsync(newList.Id, firstItem);
                    newList.Items.Add(item);
                    newList.RaiseItemSummaryChanged();
                }

                TodoLists.Insert(0, newList);
                await _logger.LogAsync("list.create", $"Created list '{listTitle}' ({newList.Id}).");
            }
            else
            {
                var previousTitle = _editingList.Title;
                _editingList.Title = listTitle;
                await _repository.UpdateListTitleAsync(_editingList.Id, listTitle);

                if (!string.IsNullOrWhiteSpace(firstItem))
                {
                    var item = new TodoItemViewModel { ListId = _editingList.Id, Title = firstItem };
                    item.Id = await _repository.InsertItemAsync(_editingList.Id, firstItem);
                    _editingList.Items.Add(item);
                    _editingList.RaiseItemSummaryChanged();
                    await _logger.LogAsync("item.create", $"Added item '{firstItem}' to list '{listTitle}'.");
                }

                await _logger.LogAsync("list.edit", $"Renamed list '{previousTitle}' to '{listTitle}'.");
            }

            ResetComposer();
            RefreshOverview();
            RenderLists();
        }

        private async void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            await _logger.LogAsync("list.edit.cancel", "Cancelled list editing.");
            ResetComposer();
        }

        private async void EditListButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TodoListViewModel list })
            {
                return;
            }

            _editingList = list;
            ComposerTitleText.Text = "Edit list";
            SaveListButton.Content = "Update list";
            CancelEditButton.Visibility = Visibility.Visible;
            ListTitleTextBox.Text = list.Title;
            FirstItemTextBox.Text = string.Empty;
            ListTitleTextBox.Focus(FocusState.Programmatic);
            await _logger.LogAsync("list.edit.start", $"Editing list '{list.Title}' ({list.Id}).");
        }

        private async void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TodoListViewModel list })
            {
                return;
            }

            await _repository.DeleteListAsync(list.Id);
            TodoLists.Remove(list);
            if (_editingList == list)
            {
                ResetComposer();
            }

            RefreshOverview();
            RenderLists();
            await _logger.LogAsync("list.delete", $"Deleted list '{list.Title}' ({list.Id}).");
        }

        private async void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TodoListViewModel list })
            {
                return;
            }

            var title = (list.NewItemDraft ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            var item = new TodoItemViewModel { ListId = list.Id, Title = title };
            item.Id = await _repository.InsertItemAsync(list.Id, title);
            list.Items.Add(item);
            list.NewItemDraft = string.Empty;
            list.RaiseItemSummaryChanged();
            RefreshOverview();
            RenderLists();
            await _logger.LogAsync("item.create", $"Added item '{title}' to list '{list.Title}'.");
        }

        private async void ToggleDoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TodoItemViewModel item } || sender is not ToggleButton toggle)
            {
                return;
            }

            var isDone = toggle.IsChecked == true;
            item.IsDone = isDone;
            await _repository.UpdateItemDoneAsync(item.Id, isDone);
            RenderLists();
            await _logger.LogAsync("item.toggle", $"Set item '{item.Title}' done={isDone}.");
        }

        private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TodoItemViewModel item })
            {
                return;
            }

            await _repository.DeleteItemAsync(item.Id);
            var parentList = TodoLists.FirstOrDefault(list => list.Id == item.ListId);
            if (parentList is null)
            {
                return;
            }

            parentList.Items.Remove(item);
            parentList.RaiseItemSummaryChanged();
            RefreshOverview();
            RenderLists();
            await _logger.LogAsync("item.delete", $"Deleted item '{item.Title}' from list '{parentList.Title}'.");
        }

        private async void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter.Kind != AppWindowPresenterKind.Overlapped)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            }

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
                await _logger.LogAsync("window.minimize", "Minimized app window.");
            }
        }

        private async void ExitFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                await _logger.LogAsync("window.fullscreen.exit", "Exited full screen mode.");
            }
        }

        private void RefreshOverview()
        {
            ListCountText.Text = TodoLists.Count.ToString();
        }

        private void ResetComposer()
        {
            _editingList = null;
            ComposerTitleText.Text = "Create a list";
            SaveListButton.Content = "Save list";
            CancelEditButton.Visibility = Visibility.Collapsed;
            ListTitleTextBox.Text = string.Empty;
            FirstItemTextBox.Text = string.Empty;
        }

        private void RenderLists()
        {
            ListsHost.Children.Clear();

            if (TodoLists.Count == 0)
            {
                ListsHost.Children.Add(new Border
                {
                    Padding = new Thickness(24),
                    Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 41, 59)),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(28),
                    Child = new TextBlock
                    {
                        Text = "No saved lists yet. Create one above.",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        FontSize = 16
                    }
                });
                return;
            }

            foreach (var list in TodoLists)
            {
                ListsHost.Children.Add(BuildListCard(list));
            }
        }

        private UIElement BuildListCard(TodoListViewModel list)
        {
            var card = new Border
            {
                Padding = new Thickness(24),
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(28)
            };

            var stack = new StackPanel { Spacing = 18 };
            card.Child = stack;

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Spacing = 4 };
            titleStack.Children.Add(new TextBlock
            {
                Text = list.Title,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 22,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = list.ItemCountText,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 148, 163, 184))
            });
            header.Children.Add(titleStack);

            var actionBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(actionBar, 1);

            var editButton = new Button { Content = "Edit", Tag = list };
            editButton.Click += EditListButton_Click;
            var deleteButton = new Button { Content = "Delete", Tag = list };
            deleteButton.Click += DeleteListButton_Click;
            actionBar.Children.Add(editButton);
            actionBar.Children.Add(deleteButton);
            header.Children.Add(actionBar);
            stack.Children.Add(header);

            var addGrid = new Grid { ColumnSpacing = 10 };
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addItemBox = new TextBox
            {
                Header = "Add item",
                PlaceholderText = "Add another task to this list",
                MinHeight = 52,
                Text = list.NewItemDraft
            };
            addItemBox.TextChanged += (_, _) => list.NewItemDraft = addItemBox.Text;
            addGrid.Children.Add(addItemBox);

            var addItemButton = new Button
            {
                Content = "Add item",
                Tag = list,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            addItemButton.Click += AddItemButton_Click;
            Grid.SetColumn(addItemButton, 1);
            addGrid.Children.Add(addItemButton);
            stack.Children.Add(addGrid);

            foreach (var item in list.Items)
            {
                stack.Children.Add(BuildItemRow(item));
            }

            return card;
        }

        private UIElement BuildItemRow(TodoItemViewModel item)
        {
            var border = new Border
            {
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 22, 32, 50)),
                CornerRadius = new CornerRadius(20)
            };

            var grid = new Grid { ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            border.Child = grid;

            grid.Children.Add(new TextBlock
            {
                Text = item.Title,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Opacity = item.ItemOpacity,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            });

            var doneToggle = new ToggleButton
            {
                Content = item.StatusText,
                IsChecked = item.IsDone,
                Tag = item
            };
            doneToggle.Click += ToggleDoneButton_Click;
            Grid.SetColumn(doneToggle, 1);
            grid.Children.Add(doneToggle);

            var deleteButton = new Button { Content = "Delete", Tag = item };
            deleteButton.Click += DeleteItemButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            return border;
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdatesButton.IsEnabled = false;
            await _logger.LogAsync("updates.check.start", "Checking GitHub Releases for updates.");

            try
            {
                var manager = new UpdateManager(new GithubSource(RepoUrl, string.Empty, false));

                if (!manager.IsInstalled)
                {
                    UpdateStatusText.Text = "Updates are available only from an installed GitHub release build.";
                    await _logger.LogAsync("updates.check.unavailable", "Current app is not a Velopack installed build.");
                    await ShowMessageAsync(
                        "Updates unavailable",
                        "This copy is not running from a Velopack-installed release yet. Install the app from a GitHub Release package first, then this button can download and apply updates.");
                    return;
                }

                if (manager.UpdatePendingRestart is not null)
                {
                    var pending = manager.UpdatePendingRestart;
                    UpdateStatusText.Text = "An update is already downloaded and ready to install.";
                    await _logger.LogAsync("updates.pending", $"Version {pending.Version} is waiting for restart.");

                    if (await AskToInstallAsync($"Version {pending.Version} is ready to install. Restart and apply it now?"))
                    {
                        await _logger.LogAsync("updates.apply", $"Applying pending version {pending.Version}.");
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
                    await _logger.LogAsync("updates.none", $"No newer release found. Current version {currentVersion}.");
                    await ShowMessageAsync("No updates", $"No newer GitHub Release was found. Current version: {currentVersion}.");
                    return;
                }

                var targetVersion = update.TargetFullRelease.Version.ToString();
                UpdateStatusText.Text = $"Downloading version {targetVersion}...";
                await _logger.LogAsync("updates.download.start", $"Downloading version {targetVersion}.");
                await manager.DownloadUpdatesAsync(update, progress =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusText.Text = $"Downloading version {targetVersion}... {progress}%";
                    });
                });

                UpdateStatusText.Text = $"Version {targetVersion} is ready to install.";
                await _logger.LogAsync("updates.download.complete", $"Downloaded version {targetVersion}.");
                if (await AskToInstallAsync($"Version {targetVersion} has been downloaded. Install it and restart now?"))
                {
                    await _logger.LogAsync("updates.apply", $"Applying version {targetVersion}.");
                    manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Update check failed.";
                await _logger.LogAsync("updates.error", ex.ToString());
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


