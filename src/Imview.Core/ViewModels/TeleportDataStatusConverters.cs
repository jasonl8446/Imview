/*
BSD 3-Clause License

Copyright (c) 2024, Jooty

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Imview.Core.ViewModels;

/// <summary>
/// Converts TeleportDataStatus to a boolean for visibility
/// </summary>
public class TeleportDataStatusToBoolConverter : IValueConverter
{
    public static readonly TeleportDataStatusToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TeleportDataStatus status)
        {
            return status != TeleportDataStatus.NotApplicable;
        }
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts TeleportDataStatus to a colored brush
/// </summary>
public class TeleportDataStatusToColorConverter : IMultiValueConverter
{
    public static readonly TeleportDataStatusToColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is TeleportDataStatus status)
        {
            return status switch
            {
                TeleportDataStatus.Loading => new SolidColorBrush(Colors.Orange),
                TeleportDataStatus.Found => new SolidColorBrush(Colors.Green),
                TeleportDataStatus.NotFound => new SolidColorBrush(Colors.Red),
                TeleportDataStatus.Error => new SolidColorBrush(Colors.DarkRed),
                TeleportDataStatus.NotApplicable => new SolidColorBrush(Colors.Transparent),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        
        return new SolidColorBrush(Colors.Transparent);
    }
}
