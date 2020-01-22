using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microcharts
{
    /// <summary>
    /// Used to model an area between two points on the Chart
    /// </summary>
    public class AreaEntry
    {
        /// <summary>
        /// Create a new AreaEntry
        /// </summary>
        /// <param name="fromValue">Start Value of Area</param>
        /// <param name="toValue">End value of Area</param>
        public AreaEntry(float fromValue, float toValue)
        {
            FromValue = fromValue;
            ToValue = toValue;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public float FromValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public float ToValue { get; }

        /// <summary>
        /// Gets or sets the color of the fill.
        /// </summary>
        /// <value>The color of the fill.</value>
        public SKColor Color { get; set; } = SKColors.Black;
    }
}
