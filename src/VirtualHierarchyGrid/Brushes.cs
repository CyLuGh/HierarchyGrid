using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VirtualHierarchyGrid
{
    // @see https://github.com/morhetz/gruvbox
    public static class GruvBoxBrushes
    {
        public static SolidColorBrush LightRed { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#fb4934"));
        public static SolidColorBrush DarkRed { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#cc241d"));

        public static SolidColorBrush LightGreen { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b8bb26"));
        public static SolidColorBrush DarkGreen { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#98971a"));

        public static SolidColorBrush LightYellow { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#fabd2f"));
        public static SolidColorBrush DarkYellow { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d79921"));

        public static SolidColorBrush LightBlue { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#83a598"));
        public static SolidColorBrush DarkBlue { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#458588"));

        public static SolidColorBrush LightPurple { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3869b"));
        public static SolidColorBrush DarkPurple { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b16286"));

        public static SolidColorBrush LightAqua { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#8ec07c"));
        public static SolidColorBrush DarkAqua { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#689d6a"));

        public static SolidColorBrush LightOrange { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#fe8019"));
        public static SolidColorBrush DarkOrange { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d65d0e"));

        public static SolidColorBrush DarkGray { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#928374"));
        public static SolidColorBrush LightGray { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#a89984"));

        public static SolidColorBrush Background { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#fbf1c7"));
        public static SolidColorBrush BackgroundLighter { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f9f5d7"));

        public static SolidColorBrush Background1 { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ebdbb2"));
        public static SolidColorBrush Background2 { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d5c4a1"));
        public static SolidColorBrush Background3 { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#bdae93"));
        public static SolidColorBrush Background4 { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#a89984"));

        public static SolidColorBrush Foreground { get; } = (SolidColorBrush)(new BrushConverter().ConvertFrom("#3c3836"));
    }
}