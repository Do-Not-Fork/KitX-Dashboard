﻿using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace KitX_Dashboard.Converters;

internal class Selection2VisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        return (int)value == 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        return (bool)value ? 0 : 1;
    }
}

//
//                                      (@@@)     (@@@@@)
//                                (@@)     (@@@@@@@)        (@@@@@@@)
//                          (@@@@@@@)   (@@@@@)       (@@@@@@@@@@@)
//                     (@@@)     (@@@@@@@)   (@@@@@@)             (@@@)
//                (@@@@@@)    (@@@@@@)                (@)
//            (@@@)  (@@@@)           (@@)
//         (@@)              (@@@)
//        .-.               
//        ] [    .-.      _    .-----.
//      ."   """"   """""" """"| .--`
//     (:--:--:--:--:--:--:--:-| [___    .------------------------.
//      |C&amp;O  :  :  :  :  :  : [_9_] |'='|.----------------------.|
//     /|.___________________________|___|'--.___.--.___.--.___.-'| 
//    / ||_.--.______.--.______.--._ |---\'--\-.-/==\-.-/==\-.-/-'/--
//   /__;^=(==)======(==)======(==)=^~^^^ ^^^^(-)^^^^(-)^^^^(-)^^^ aac
// ~~~^~~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~^~~~
//
