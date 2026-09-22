using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using KanbanNW.ViewModels;

namespace KanbanNW;

/// <summary>
/// DataTemplate that maps ViewModels to their corresponding Views by convention.
/// Uses reflection to find the View type matching the ViewModel's name.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Creates the View control for the given ViewModel.
    /// </summary>
    /// <param name="param">The ViewModel instance to find a View for.</param>
    /// <returns>The corresponding View control, or a TextBlock indicating not found.</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Convention: ViewModel name ends with "ViewModel", View ends with "View"
        // e.g., MainViewModel -> MainView
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        // Fallback: show error if no matching View found
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Determines if this template matches the given data object.
    /// </summary>
    /// <param name="data">The data object to check.</param>
    /// <returns>True if data is a ViewModelBase (all our ViewModels inherit from it).</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}