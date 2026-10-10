// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// The unit of measure the designer shows in its status bar and rulers.
    /// Configured by the "units" desktop setting: "inches", "cm" or "mm".
    /// </summary>
    internal static class MeasureUnits
    {
        public const string Inches = "inches";
        public const string Centimeters = "cm";
        public const string Millimeters = "mm";

        /// <summary>True for cm and mm.</summary>
        public static bool IsMetric(string units)
        {
            return IsCentimeters(units) || IsMillimeters(units);
        }

        public static bool IsCentimeters(string units)
        {
            return string.Equals(units?.Trim(), Centimeters, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMillimeters(string units)
        {
            return string.Equals(units?.Trim(), Millimeters, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Normalizes a configured value; anything unrecognized is inches.</summary>
        public static string Normalize(string units)
        {
            if (IsCentimeters(units)) return Centimeters;
            if (IsMillimeters(units)) return Millimeters;
            return Inches;
        }

        /// <summary>How many of the unit make up one inch.</summary>
        public static double UnitsPerInch(string units)
        {
            if (IsCentimeters(units)) return 2.54;
            if (IsMillimeters(units)) return 25.4;
            return 1.0;
        }

        /// <summary>Converts a value in inches to the given unit.</summary>
        public static double FromInches(double inches, string units)
        {
            return inches * UnitsPerInch(units);
        }

        /// <summary>Converts a value in points (1/72 inch) to the given unit.</summary>
        public static double FromPoints(double points, string units)
        {
            return FromInches(points / 72.0, units);
        }
    }
}
