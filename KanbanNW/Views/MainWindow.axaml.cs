using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        // Clean up the drag ghost
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

        // Accept the drag
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnColumnDrop(object? sender, DragEventArgs e)
    {
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
                // Same-column drop on the column header — keep the task where it is
                e.Handled = true;
                return;
            }

            if (DataContext is MainViewModel vm)
            {
                // Dropped on the column outside the ListBox: append at end
                // Pass a large index; MoveTaskToColumnAtPosition clamps to the end
                vm.MoveTaskToColumnAtPosition(taskId, targetCol.Id, int.MaxValue);
            }
        }
        e.Handled = true;
    }

    private void OnTaskDrop(object? sender, DragEventArgs e)
    {
        var taskIdStr = e.DataTransfer.TryGetValue(TaskIdFormat);
        var sourceColumnIdStr = e.DataTransfer.TryGetValue(SourceColumnIdFormat);
        if (taskIdStr == null || sourceColumnIdStr == null)
            return;

        if (!int.TryParse(taskIdStr, out var taskId) ||
            !int.TryParse(sourceColumnIdStr, out var sourceColumnId))
            return;

        // Walk up the parent chain to find the ListBox and the target ColumnViewModel
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

        // Get drop position relative to the ListBox
        var dropPos = e.GetPosition(listBox);
        var itemCount = listBox.ItemCount;

        int insertIndex;
        if (itemCount == 0)
        {
            insertIndex = 0;
        }
        else
        {
            // Find the closest realized container by its vertical midpoint
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
            {
                // No containers realized — append at end
                insertIndex = itemCount;
            }
            else
            {
                // Above or below the closest item's midpoint?
                var closestContainer = listBox.ContainerFromIndex(closestIndex);
                if (closestContainer == null)
                {
                    insertIndex = closestIndex;
                }
                else
                {
                    var closestMidY = closestContainer.Bounds.Y + closestContainer.Bounds.Height / 2.0;
                    insertIndex = dropPos.Y > closestMidY ? closestIndex + 1 : closestIndex;
                }
            }
        }

        vm.MoveTaskToColumnAtPosition(taskId, targetCol.Id, insertIndex);
        e.Handled = true;
    }
}