using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using KanbanNW.Models;

namespace KanbanNW.Converters;

public class TypeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskType type)
        {
            return type switch
            {
                Models.TaskType.None => new SolidColorBrush(Color.Parse("#FFFFFF")),
                Models.TaskType.Red => new SolidColorBrush(Color.Parse("#EF5350")),
                Models.TaskType.Orange => new SolidColorBrush(Color.Parse("#FF7043")),
                Models.TaskType.Yellow => new SolidColorBrush(Color.Parse("#FFCA28")),
                Models.TaskType.Green => new SolidColorBrush(Color.Parse("#66BB6A")),
                Models.TaskType.Blue => new SolidColorBrush(Color.Parse("#42A5F5")),
                Models.TaskType.Purple => new SolidColorBrush(Color.Parse("#AB47BC")),
                Models.TaskType.Black => new SolidColorBrush(Color.Parse("#455A64")),
                _ => new SolidColorBrush(Color.Parse("#FFFFFF"))
            };
        }
        return new SolidColorBrush(Color.Parse("#FFFFFF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TypeToColorNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskType type)
        {
            return type switch
            {
                Models.TaskType.None => "#FFFFFF",
                Models.TaskType.Red => "#EF5350",
                Models.TaskType.Orange => "#FF7043",
                Models.TaskType.Yellow => "#FFCA28",
                Models.TaskType.Green => "#66BB6A",
                Models.TaskType.Blue => "#42A5F5",
                Models.TaskType.Purple => "#AB47BC",
                Models.TaskType.Black => "#455A64",
                _ => "#42A5F5"
            };
        }
        return "#42A5F5";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns a strikethrough TextDecorationCollection when value is true, empty otherwise.
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
        throw new NotImplementedException();
    }
}

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
/// Returns the first ~80 characters of a string for card preview.
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
/// Converts a TaskType enum to its user-configured display name.
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
/// Converts a hex color string (e.g. "#2C3E50") to an IBrush for Border.Background bindings.
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
/// Returns a contrasting foreground brush (black or white) for a given
/// hex color string, based on perceived luminance.
/// Used to keep tab text legible regardless of the user-chosen tab color.
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
                var luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                return new SolidColorBrush(luminance < 140 ? Colors.White : Colors.Black);
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