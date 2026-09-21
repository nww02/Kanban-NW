using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using KanbanNW.ViewModels;

namespace KanbanNW.Views;

public partial class MainWindow : Window
{
    private static readonly DataFormat<string> TaskIdFormat =
        DataFormat.CreateInProcessFormat<string>("kanban-task-id");
    private static readonly DataFormat<string> SourceColumnIdFormat =
        DataFormat.CreateInProcessFormat<string>("kanban-source-column");

    // Drag ghost state – the floating visual that follows the cursor
    private Border? _dragGhost;
    private double _dragGhostOffX;
    private double _dragGhostOffY;

    // Drop placeholder – a blank item inserted into the column's Tasks
    // collection during drag so that other items are pushed aside,
    // showing the user exactly where the task will land.
    private static readonly TaskViewModel _dragPlaceholder = new()
    {
        Id = -1,
        Title = "",
        IsDragPlaceholder = true
    };
    private int _placeholderColumnId = -1;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainViewModel vm)
        {
            vm.ShowTaskEditorCallback = ShowTaskEditorAsync;
            vm.ShowConfirmCallback = ShowConfirmAsync;
            vm.ShowSaveFileCallback = ShowSaveFileAsync;
        }
    }

    private async Task ShowTaskEditorAsync(TaskEditorViewModel editor)
    {
        var dialog = new TaskEditorWindow
        {
            DataContext = editor
        };
        editor.CloseAction = () => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private async Task<bool> ShowConfirmAsync(string message)
    {
        return await ConfirmDialog.ShowAsync(this, message);
    }

    private async Task<string?> ShowSaveFileAsync()
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Export Project",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } }
            }
        };
        var file = await this.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }

    private async void OnCategoriesClick(object? sender, RoutedEventArgs e)
    {
        await CategoryEditorWindow.ShowAsync(this, new Data.KanbanDbContext());

        // Refresh the board to show updated category names
        if (DataContext is MainViewModel vm && vm.CurrentProjectId > 0)
        {
            vm.LoadColumnsForProject(vm.CurrentProjectId);
        }
    }

    private void OnToggleShowDeleted(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowDeletedTasks = !vm.ShowDeletedTasks;
        }
    }

    private async void OnAppearanceClick(object? sender, RoutedEventArgs e)
    {
        await AppearanceSettingsWindow.ShowAsync(this);
    }

    private async void OnColumnsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.CurrentProjectId > 0)
        {
            await ColumnsEditorWindow.ShowAsync(this, vm.CurrentProjectId);
            vm.LoadColumnsForProject(vm.CurrentProjectId);
        }
    }

    private async void OnProjectsClick(object? sender, RoutedEventArgs e)
    {
        await ProjectEditorWindow.ShowAsync(this, new Data.KanbanDbContext());

        if (DataContext is MainViewModel vm)
        {
            vm.LoadProjects();
            if (vm.CurrentProjectId > 0)
            {
                vm.LoadColumnsForProject(vm.CurrentProjectId);
            }
        }
    }

    private async void OnHelpClick(object? sender, RoutedEventArgs e)
    {
        await HelpWindow.ShowAsync(this);
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        await AboutWindow.ShowAsync(this);
    }

    private void OnProjectTabClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is int projectId)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SwitchToProject(projectId);
            }
        }
    }

    private async void OnTaskPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not TaskViewModel task)
            return;

        // Visual feedback: dim the source task
        border.Opacity = 0.3;

        // Make the placeholder match the dragged task's size
        _dragPlaceholder.PlaceholderHeight = border.Bounds.Height;

        // Create a "ghost" Border that will follow the cursor
        var ghost = new Border
        {
            Width = border.Bounds.Width,
            Height = border.Bounds.Height,
            CornerRadius = border.CornerRadius,
            BorderThickness = border.BorderThickness,
            BorderBrush = new SolidColorBrush(Colors.Gray),
            Background = new SolidColorBrush(Color.Parse("#383838")),
            Opacity = 0.85
        };

        // Show the task title inside the ghost
        ghost.Child = new TextBlock
        {
            Text = task.Title,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(10, 8),
            TextWrapping = TextWrapping.Wrap
        };

        // Position the ghost at the cursor
        var pointerPos = e.GetPosition(this);
        _dragGhostOffX = border.Bounds.Width / 2;
        _dragGhostOffY = 10;
        Canvas.SetLeft(ghost, pointerPos.X - _dragGhostOffX);
        Canvas.SetTop(ghost, pointerPos.Y - _dragGhostOffY);
        DragOverlay.Children.Add(ghost);
        _dragGhost = ghost;

        var transfer = new DataTransfer();
        var item = new DataTransferItem();
        item.Set(TaskIdFormat, task.Id.ToString());
        item.Set(SourceColumnIdFormat, task.ColumnId.ToString());
        transfer.Add(item);

        var result = await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move | DragDropEffects.Copy);

        // Clean up drag visuals
        RemovePlaceholder();
        DragOverlay.Children.Remove(ghost);
        _dragGhost = null;

        // If the drop succeeded (result != None), the border is orphaned
        // because the task was removed from the source column. Otherwise
        // restore the opacity so the task reappears.
        if (result == DragDropEffects.None)
        {
            border.Opacity = 1.0;
            // No actual drag happened — treat as a click and open the editor
            if (DataContext is MainViewModel vm)
                vm.EditTaskCommand.Execute(task);
        }
    }

    private void OnColumnDragOver(object? sender, DragEventArgs e)
    {
        // Move the drag ghost to follow the cursor
        if (_dragGhost != null)
        {
            var pos = e.GetPosition(DragOverlay);
            Canvas.SetLeft(_dragGhost, pos.X - _dragGhostOffX);
            Canvas.SetTop(_dragGhost, pos.Y - _dragGhostOffY);
        }

        // Show a placeholder where the task would be inserted
        UpdateDropPlaceholder(e);

        // Accept the drag
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    /// <summary>
    /// Inserts (or moves) the <see cref="_dragPlaceholder"/> into the target
    /// column's <c>Tasks</c> collection at the position indicated by the
    /// cursor, pushing existing items aside to show the drop location.
    /// </summary>
    private void UpdateDropPlaceholder(DragEventArgs e)
    {
        // Find which column we're over using the ItemsControl containers
        // (reliable during platform drag where GetVisualAt may fail)
        var (column, listBox) = FindColumnAtCursorX(e);
        if (column == null || listBox == null || column.DataContext is not ColumnViewModel columnVm)
        {
            RemovePlaceholder();
            return;
        }

        // Calculate the insert index from the cursor position
        var listBoxPoint = e.GetPosition(listBox);
        var insertIndex = CalculateInsertIndex(listBox, listBoxPoint);

        // Get the dragged task's ID to avoid showing a placeholder over itself
        var draggedIdStr = e.DataTransfer.TryGetValue(TaskIdFormat);
        int.TryParse(draggedIdStr, out var draggedTaskId);

        // If the dragged task is still in this column (dimmed in place), check
        // whether the insert index points to where it already sits — dropping
        // there would put it back where it came from, so suppress the placeholder.
        int draggedIndex = -1;
        if (draggedTaskId > 0)
        {
            for (int i = 0; i < columnVm.Tasks.Count; i++)
            {
                if (columnVm.Tasks[i].Id == draggedTaskId) { draggedIndex = i; break; }
            }
        }
        if (draggedIndex >= 0 &&
            (insertIndex == draggedIndex || insertIndex == draggedIndex + 1))
        {
            RemovePlaceholder();
            return;
        }

        // If the placeholder is already at the right spot, nothing to do
        if (_placeholderColumnId == columnVm.Id && TasksIndexOfPlaceholder(columnVm.Tasks) == insertIndex)
            return;

        // Remove from the previous column
        RemovePlaceholder();

        // Clamp and insert into the new column
        var tasks = columnVm.Tasks;
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > tasks.Count) insertIndex = tasks.Count;
        tasks.Insert(insertIndex, _dragPlaceholder);
        _placeholderColumnId = columnVm.Id;
    }

    /// <summary>
    /// Returns the index of the drag placeholder in the given task list,
    /// or -1 if it is not present.
    /// </summary>
    private static int TasksIndexOfPlaceholder(ObservableCollection<TaskViewModel> tasks)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].IsDragPlaceholder)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Removes the drag placeholder from whichever column it currently
    /// occupies.
    /// </summary>
    private void RemovePlaceholder()
    {
        if (_placeholderColumnId < 0) return;

        if (DataContext is MainViewModel vm)
        {
            foreach (var col in vm.Columns)
            {
                if (col.Id == _placeholderColumnId)
                {
                    col.Tasks.Remove(_dragPlaceholder);
                    break;
                }
            }
        }
        _placeholderColumnId = -1;
    }

    /// <summary>
    /// Finds the column container and its ListBox at the cursor X position
    /// by walking the ColumnsItemsControl's item containers.
    /// This is reliable during platform drag where GetVisualAt may fail.
    /// </summary>
    private (Border? border, ListBox? listBox) FindColumnAtCursorX(DragEventArgs e)
    {
        var point = e.GetPosition(ColumnsItemsControl);
        if (point.X < 0) return (null, null);

        for (int i = 0; i < ColumnsItemsControl.ItemCount; i++)
        {
            var container = ColumnsItemsControl.ContainerFromIndex(i);
            if (container == null) continue;

            var bounds = container.Bounds;
            if (point.X < bounds.X || point.X >= bounds.X + bounds.Width)
                continue;

            // Found — walk this container's subtree for the column Border and ListBox
            return FindBorderAndListBox(container);
        }

        return (null, null);
    }

    /// <summary>
    /// Searches a visual subtree for a Border with ColumnViewModel DataContext
    /// and a ListBox.
    /// </summary>
    private static (Border? border, ListBox? listBox) FindBorderAndListBox(Visual root)
    {
        Border? foundBorder = null;
        ListBox? foundListBox = null;

        if (root is Border b && b.DataContext is ColumnViewModel)
            foundBorder = b;
        if (root is ListBox lb)
            foundListBox = lb;

        if (foundBorder != null && foundListBox != null)
            return (foundBorder, foundListBox);

        foreach (var child in root.GetVisualChildren())
        {
            if (child is not Visual v) continue;
            var (childBorder, childListBox) = FindBorderAndListBox(v);
            childBorder ??= foundBorder;
            childListBox ??= foundListBox;
            if (childBorder != null && childListBox != null)
                return (childBorder, childListBox);
            foundBorder ??= childBorder;
            foundListBox ??= childListBox;
        }

        return (foundBorder, foundListBox);
    }

    /// <summary>
    /// Calculates the insert index for a drop position within a ListBox.
    /// </summary>
    private static int CalculateInsertIndex(ListBox listBox, Point dropPos)
    {
        var itemCount = listBox.ItemCount;
        if (itemCount == 0) return 0;

        int closestIndex = 0;
        double closestDist = double.MaxValue;
        bool foundAny = false;

        for (int i = 0; i < itemCount; i++)
        {
            var container = listBox.ContainerFromIndex(i);
            if (container == null) continue;
            foundAny = true;

            var midY = container.Bounds.Y + container.Bounds.Height / 2.0;
            var dist = Math.Abs(dropPos.Y - midY);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        if (!foundAny)
            return itemCount;

        var closestContainer = listBox.ContainerFromIndex(closestIndex);
        if (closestContainer == null)
            return closestIndex;

        var closestMidY = closestContainer.Bounds.Y + closestContainer.Bounds.Height / 2.0;
        return dropPos.Y > closestMidY ? closestIndex + 1 : closestIndex;
    }

    /// <summary>
    /// Removes the placeholder and handles the actual drop logic for column-level drops.
    /// </summary>
    private void OnColumnDrop(object? sender, DragEventArgs e)
    {
        RemovePlaceholder();

        var taskIdStr = e.DataTransfer.TryGetValue(TaskIdFormat);
        var sourceColumnIdStr = e.DataTransfer.TryGetValue(SourceColumnIdFormat);
        if (taskIdStr == null || sourceColumnIdStr == null)
            return;

        if (!int.TryParse(taskIdStr, out var taskId) ||
            !int.TryParse(sourceColumnIdStr, out var sourceColumnId))
            return;

        if (sender is Border border && border.DataContext is ColumnViewModel targetCol)
        {
            if (targetCol.Id == sourceColumnId)
            {
                e.Handled = true;
                return;
            }

            if (DataContext is MainViewModel vm)
            {
                vm.MoveTaskToColumnAtPosition(taskId, targetCol.Id, int.MaxValue);
            }
        }
        e.Handled = true;
    }

    /// <summary>
    /// Removes the placeholder and handles the actual drop logic for task-level drops.
    /// </summary>
    private void OnTaskDrop(object? sender, DragEventArgs e)
    {
        RemovePlaceholder();

        var taskIdStr = e.DataTransfer.TryGetValue(TaskIdFormat);
        var sourceColumnIdStr = e.DataTransfer.TryGetValue(SourceColumnIdFormat);
        if (taskIdStr == null || sourceColumnIdStr == null)
            return;

        if (!int.TryParse(taskIdStr, out var taskId) ||
            !int.TryParse(sourceColumnIdStr, out var sourceColumnId))
            return;

        // Walk up to find the ListBox and target column
        ListBox? listBox = null;
        ColumnViewModel? targetCol = null;

        var current = sender as StyledElement;
        while (current != null)
        {
            if (current is ListBox lb)
                listBox = lb;
            if (current is Border border && border.DataContext is ColumnViewModel col)
                targetCol = col;
            current = current.Parent as StyledElement;
        }

        if (listBox == null || targetCol == null)
            return;
        if (DataContext is not MainViewModel vm)
            return;

        var dropPos = e.GetPosition(listBox);
        var insertIndex = CalculateInsertIndex(listBox, dropPos);
        vm.MoveTaskToColumnAtPosition(taskId, targetCol.Id, insertIndex);
        e.Handled = true;
    }
}