using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace KanbanNW.Converters;

/// <summary>
/// Returns true if tasks can be added to this column (not Done or Deleted Tasks).
/// </summary>
public class CanAddTaskConverter : IValueConverter
{
    /// <summary>
    /// Converts a column name to a boolean indicating if tasks can be added.
    /// </summary>
    /// <param name="value">The column name.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>True if tasks can be added, false otherwise.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name)
            return name != "Done" && name != "Deleted Tasks";
        return true;
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a highlight background brush if the tab's project matches the current project.
/// </summary>
public class TabBackgroundConverter : IValueConverter
{
    /// <summary>
    /// Converts a project ID to a background brush.
    /// </summary>
    /// <param name="value">The tab's project ID.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The current project ID.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>A brush for the tab background.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int tabProjectId && parameter is int currentProjectId)
        {
            return tabProjectId == currentProjectId
                ? Avalonia.Media.Brush.Parse("#1A5276")
                : Avalonia.Media.Brush.Parse("#2C3E50");
        }
        return Avalonia.Media.Brush.Parse("#2C3E50");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}