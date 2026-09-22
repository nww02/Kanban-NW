using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using KanbanNW.Models;

namespace KanbanNW.Converters;

/// <summary>
/// Converts a TaskType enum to a SolidColorBrush for UI rendering.
/// </summary>
public class TypeToColorConverter : IValueConverter
{
    private static readonly Dictionary<TaskType, Color> _colors = new()
    {
        [TaskType.None]      = Color.Parse("#FFFFFF"),
        [TaskType.Red]       = Color.Parse("#EF5350"),
        [TaskType.Orange]    = Color.Parse("#FF7043"),
        [TaskType.Yellow]    = Color.Parse("#FFCA28"),
        [TaskType.Green]     = Color.Parse("#66BB6A"),
        [TaskType.Blue]      = Color.Parse("#42A5F5"),
        [TaskType.Purple]    = Color.Parse("#AB47BC"),
        [TaskType.Black]     = Color.Parse("#455A64")
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskType type)
        {
            return new SolidColorBrush(_colors.TryGetValue(type, out var c) ? c : Colors.White);
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a TaskType enum to its hex color string (for ColorPicker).
/// </summary>
public class TypeToColorNameConverter : IValueConverter
{
    private static readonly Dictionary<TaskType, string> _colors = new()
    {
        [TaskType.None]      = "#FFFFFF",
        [TaskType.Red]       = "#EF5350",
        [TaskType.Orange]    = "#FF7043",
        [TaskType.Yellow]    = "#FFCA28",
        [TaskType.Green]     = "#66BB6A",
        [TaskType.Blue]      = "#42A5F5",
        [TaskType.Purple]    = "#AB47BC",
        [TaskType.Black]     = "#455A64"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskType type)
        {
            return _colors.TryGetValue(type, out var c) ? c : "#FFFFFF";
        }
        return "#FFFFFF";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a strikethrough TextDecoration when the task is complete.
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
    private static readonly TextDecorationCollection Strikethrough = new()
    {
        new TextDecoration { Location = TextDecorationLocation.Strikethrough }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isComplete && isComplete)
            return Strikethrough;
        return new TextDecorationCollection();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

/// <summary>
/// Returns true if a string is not null or whitespace.
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
            return !string.IsNullOrWhiteSpace(s);
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a background brush for column headers based on whether they're system columns.
/// </summary>
public class ColumnHeaderBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSystem && isSystem)
            return new SolidColorBrush(Color.Parse("#E3F2FD")); // Light blue for system columns
        return new SolidColorBrush(Color.Parse("#E8EAF6")); // Light indigo for user columns
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a background brush for due dates based on urgency.
/// </summary>
public class DueDateBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dueDate)
        {
            if (dueDate < DateTimeOffset.Now)
                return new SolidColorBrush(Color.Parse("#EF5350")); // Red for overdue
            return new SolidColorBrush(Color.Parse("#FFA726")); // Orange otherwise
        }
        return new SolidColorBrush(Color.Parse("#FFA726"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the value is not null.
/// </summary>
public class NotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a DateTimeOffset? to a short date string (yyyy-MM-dd).
/// </summary>
public class DueDateToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dt)
            return $"Due: {dt:d}";
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns "Edit Task" or "New Task" based on whether we're editing.
/// </summary>
public class EditNewTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEditing && isEditing)
            return "Edit Task";
        return "New Task";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEditing && isEditing)       
        return "New Task";
  
            return "Edit Task";

    }
}

/// <summary>
/// Returns a preview of the description (first ~80 chars with ellipsis).
/// </summary>
public class DescriptionPreviewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            var trimmed = s.Trim();
            return trimmed.Length > 80 ? trimmed[..80] + "..." : trimmed;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the column name is "Deleted Tasks".
/// </summary>
public class IsDeletedColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string name && name == "Deleted Tasks";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the column name is NOT "Deleted Tasks".
/// </summary>
public class IsNotDeletedColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string name && name != "Deleted Tasks";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a TaskType enum to its custom display name (from cache).
/// </summary>
public class TypeToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskType type)
            return TaskTypeNameCache.GetDisplayName(type);
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a hex color string (e.g. "#2C3E50") to a SolidColorBrush for UI bindings.
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return new SolidColorBrush(Color.Parse(hex));
            }
            catch
            {
                // fall through to default
            }
        }
        return new SolidColorBrush(Color.Parse("#2C3E50"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a contrasting foreground brush (black or white) for a given hex color.
/// Uses perceived luminance: returns White for dark colors, Black for light colors.
/// </summary>
public class ContrastTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var c = Color.Parse(hex);
                // Perceived luminance formula: 0.299R + 0.587G + 0.114B
                var luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                return new SolidColorBrush(luminance < 128 ? Colors.White : Colors.Black);
            }
            catch
            {
                // fall through
            }
        }
        return new SolidColorBrush(Colors.Black);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns "✓" for true, null for false — used for menu checkmarks.
/// </summary>
public class BoolToCheckmarkConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return "✓";
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is "✓";
    }
}