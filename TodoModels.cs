using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace tododeploy;

public sealed class TodoListViewModel : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _newItemDraft = string.Empty;
    private double _cardWidth = 560;

    public long Id { get; set; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string NewItemDraft
    {
        get => _newItemDraft;
        set => SetProperty(ref _newItemDraft, value);
    }

    public double CardWidth
    {
        get => _cardWidth;
        set => SetProperty(ref _cardWidth, value);
    }

    public ObservableCollection<TodoItemViewModel> Items { get; } = new();

    public int ItemCount => Items.Count;

    public string ItemCountText => ItemCount == 1 ? "1 item" : $"{ItemCount} items";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaiseItemSummaryChanged()
    {
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(ItemCountText));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class TodoItemViewModel : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private bool _isDone;

    public long Id { get; set; }

    public long ListId { get; set; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (SetProperty(ref _isDone, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(ItemOpacity));
            }
        }
    }

    public string StatusText => IsDone ? "Done" : "Mark done";

    public double ItemOpacity => IsDone ? 0.6 : 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
