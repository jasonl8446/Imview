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
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Imview.Core.Converters;

public class CoordinateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float coordinate && parameter is string axis)
        {
            // Convert game coordinates to canvas coordinates
            // Based on real data, zones can range from -20000 to +20000 or even larger
            // We'll scale this down significantly to fit a large canvas (20000x20000)
            var scaleFactor = 0.25; // Scale down to 1/4 size to fit larger ranges
            var canvasCenter = 10000.0; // Center of a 20000x20000 canvas
            
            double result;
            if (axis == "X")
            {
                result = canvasCenter + (coordinate * scaleFactor);
            }
            else if (axis == "Y")
            {
                // Flip Y axis since canvas Y increases downward but game Y increases upward
                result = canvasCenter - (coordinate * scaleFactor);
            }
            else
            {
                return 0.0;
            }
            
            // Debug log for specific coordinates we're tracking
            if (Math.Abs(coordinate + 611) < 1 || Math.Abs(coordinate - 702.3) < 1) // Track our test object
            {
                Console.WriteLine($"TEST OBJECT coordinate conversion: {axis}={coordinate:F1} -> Canvas {axis}={result:F1}");
            }
            
            return result;
        }
        
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ObjectTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string objectType)
        {
            return objectType switch
            {
                "NPC" => new SolidColorBrush(Color.FromRgb(100, 150, 255)),      // Light blue
                "Shopkeeper" => new SolidColorBrush(Color.FromRgb(255, 200, 50)), // Gold
                "Professor" => new SolidColorBrush(Color.FromRgb(150, 255, 150)), // Light green
                "Building" => new SolidColorBrush(Color.FromRgb(150, 100, 50)),   // Brown
                "Volume" => new SolidColorBrush(Color.FromRgb(255, 100, 100)),    // Red
                "Object" => new SolidColorBrush(Color.FromRgb(150, 150, 150)),    // Gray
                _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))            // Dark gray
            };
        }
        
        return new SolidColorBrush(Color.FromRgb(100, 100, 100));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ObjectTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string objectType)
        {
            return objectType switch
            {
                "NPC" => "N",
                "Shopkeeper" => "$",
                "Professor" => "P",
                "Building" => "■",
                "Volume" => "○",
                "Object" => "•",
                _ => "?"
            };
        }
        
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}