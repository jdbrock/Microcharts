﻿// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microcharts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SkiaSharp;

    /// <summary>
    /// ![chart](../images/Point.png)
    /// 
    /// Point chart.
    /// </summary>
    public class PointChart : Chart
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microcharts.PointChart"/> class.
        /// </summary>
        public PointChart()
        {
            this.LabelOrientation = Orientation.Default;
            this.ValueLabelOrientation = Orientation.Default;

            YAxisTextPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                //Typeface = base.Typeface
                Style = SKPaintStyle.StrokeAndFill,
                //TextSize = 30
            };

            YAxisLinesPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(0x13),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
            };
        }

        #endregion

        #region Fields

        private Orientation labelOrientation, valueLabelOrientation;

        #endregion

        #region Properties

        public bool ShowYAxisText { get; set; } = false;
        public bool ShowYAxisLines { get; set; } = false;

        //TODO : calculate this automagically, based on available area height and text height
        public int YAxisMaxTicks { get; set; } = 5;

        public Position YAxisPosition { get; set; } = Position.Right;

        /// <summary>
        /// Y Axis Paint
        /// </summary>
        public SKPaint YAxisTextPaint { get; set; }

        /// <summary>
        /// Y Axis Paint
        /// </summary>
        public SKPaint YAxisLinesPaint { get; set; }

        /// <summary>
        /// Gets or sets the size of the point.
        /// </summary>
        /// <value>The size of the point.</value>
        public float PointSize { get; set; } = 14;

        /// <summary>
        /// Gets or sets the point mode.
        /// </summary>
        /// <value>The point mode.</value>
        public PointMode PointMode { get; set; } = PointMode.Circle;

        /// <summary>
        /// Gets or sets the point area alpha.
        /// </summary>
        /// <value>The point area alpha.</value>
        public byte PointAreaAlpha { get; set; } = 100;

        /// <summary>
        /// Gets or sets the text orientation of labels.
        /// </summary>
        /// <value>The label orientation.</value>
        public Orientation LabelOrientation
        {
            get => this.labelOrientation;
            set => this.labelOrientation = (value == Orientation.Default) ? Orientation.Vertical : value;
        }

        /// <summary>
        /// Gets or sets the text orientation of value labels.
        /// </summary>
        /// <value>The label orientation.</value>
        public Orientation ValueLabelOrientation
        {
            get => this.valueLabelOrientation;
            set => this.valueLabelOrientation = (value == Orientation.Default) ? Orientation.Vertical : value;
        }

        /// <summary>
        /// Labels to be shown on the X Axis
        /// </summary>
        public IEnumerable<string> XAxisLabels { get; set; }

        private float ValueRange => this.MaxValue - this.MinValue;

        #endregion

        #region Methods

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            if (this.Entries != null)
            {
                int yAxisWidth = 0;
                float yAxisXShift = 0;
                List<float> yAxisIntervalLabels = new List<float>();

                if (ShowYAxisText || ShowYAxisLines)
                {
                    yAxisWidth = width;

                    var enumerable = this.Entries.ToList(); // to avoid double enumeration

                    NiceScale.Calculate(MinValue == 0 ? enumerable.Min(e => e.Value) : MinValue, MaxValue == 0 ? enumerable.Max(e => e.Value) : MaxValue, YAxisMaxTicks,
                        out var range, out var tickSpacing, out var niceMin, out var niceMax);

                    var ticks = (int)(range / tickSpacing);

                    yAxisIntervalLabels = Enumerable.Range(0, ticks)
                        .Select(i => (float)(niceMax - (i * tickSpacing)))
                        .ToList();

                    var longestYAxisLabel = yAxisIntervalLabels.Aggregate(string.Empty, (max, cur) =>
                        max.Length > cur.ToString().Length ? max : cur.ToString()
                    );
                    var longestYAxisLabelWidth = this.MeasureLabel(longestYAxisLabel, YAxisTextPaint).Width;

                    yAxisWidth = (int)(width - longestYAxisLabelWidth);

                    if (YAxisPosition == Position.Left)
                        yAxisXShift = longestYAxisLabelWidth;

                    // to reduce chart width
                    width = yAxisWidth;
                }

                var labels = XAxisLabels != null && XAxisLabels.Any() ? XAxisLabels.ToArray() : this.Entries.Select(x => x.Label).ToArray();
                var labelSizes = this.MeasureLabels(labels);
                var footerHeight = this.CalculateFooterHeaderHeight(labelSizes, this.LabelOrientation, labels);

                var valueLabels = this.Entries.Select(x => x.ValueLabel).ToArray();
                var valueLabelSizes = this.MeasureLabels(valueLabels);
                var headerHeight = this.CalculateFooterHeaderHeight(valueLabelSizes, this.ValueLabelOrientation, valueLabels);

                var itemSize = this.CalculateItemSize(width, height, footerHeight, headerHeight, labels.Length);
                var origin = this.CalculateYOrigin(itemSize.Height, headerHeight);
                var points = this.CalculatePoints(itemSize, origin, headerHeight, yAxisXShift);
                var labelsPoints = this.CalculateLabelsPoints(itemSize, labels.Count(), yAxisXShift);

                var cnt = 0;
                if (ShowYAxisText || ShowYAxisLines)
                {
                    var intervals = yAxisIntervalLabels
                        .Select(t => new ValueTuple<string, SKPoint>
                        (
                            t.ToString(),
                            new SKPoint(YAxisPosition == Position.Left ? yAxisXShift : width, CalculatePoint(t, cnt++, itemSize, origin, headerHeight).Y)
                        ))
                        .ToList();

                    if (ShowYAxisText)
                        this.DrawYAxisText(canvas, intervals);

                    if (ShowYAxisLines)
                    {
                        var lines = intervals.Select(tup =>
                        {
                            (_, SKPoint pt) = tup;
                            if (YAxisPosition == Position.Right)
                                return SKRect.Create(0, pt.Y, width, 0);
                            else
                                return SKRect.Create(yAxisXShift, pt.Y, width, 0);
                        });
                        this.DrawYAxisLines(canvas, lines);
                    }
                }

                this.DrawAreas(canvas, points, itemSize, origin, headerHeight);
                this.DrawPoints(canvas, points);
                this.DrawHeader(canvas, valueLabels, valueLabelSizes, points, itemSize, height, headerHeight);
                this.DrawFooter(canvas, labels, labelSizes, labelsPoints, itemSize, height, footerHeight);
            }
        }

        private SKPoint[] CalculateLabelsPoints(SKSize itemSize, int itemsCount, float originX = 0)
        {
            var result = new List<SKPoint>();

            for (int i = 0; i < itemsCount; i++)
            {
                result.Add(CalculatePointX(i, itemSize, originX));
            }

            return result.ToArray();
        }

        protected float CalculateYOrigin(float itemHeight, float headerHeight)
        {
            if (this.MaxValue <= 0)
            {
                return headerHeight;
            }

            if (this.MinValue > 0)
            {
                return headerHeight + itemHeight;
            }

            return headerHeight + ((this.MaxValue / this.ValueRange) * itemHeight);
        }

        protected SKSize CalculateItemSize(int width, int height, float footerHeight, float headerHeight, int pointsCount)
        {
            var w = (width - ((pointsCount + 1) * this.Margin)) / pointsCount;
            var h = height - this.Margin - footerHeight - headerHeight;
            return new SKSize(w, h);
        }

        protected SKPoint[] CalculatePoints(SKSize itemSize, float origin, float headerHeight, float originX = 0)
        {
            var result = new List<SKPoint>();

            for (int i = 0; i < this.Entries.Count(); i++)
            {
                var entry = this.Entries.ElementAt(i);
                var value = entry.Value;

                result.Add(CalculatePoint(value, i, itemSize, origin, headerHeight, originX));
            }

            return result.ToArray();
        }
        protected SKPoint CalculatePoint(float value, int i, SKSize itemSize, float origin, float headerHeight, float originX = 0)
        {
            var x = originX + this.Margin + (itemSize.Width / 2) + (i * (itemSize.Width + this.Margin));
            var y = headerHeight + ((1 - this.AnimationProgress) * (origin - headerHeight) + (((this.MaxValue - value) / this.ValueRange) * itemSize.Height) * this.AnimationProgress);
            return new SKPoint(x, y);
        }

        protected SKPoint CalculatePointX(int i, SKSize itemSize, float originX = 0)
        {
            var x = originX + this.Margin + (itemSize.Width / 2) + (i * (itemSize.Width + this.Margin));
            return new SKPoint(x, 0);
        }

        protected virtual void DrawHeader(SKCanvas canvas, string[] labels, SKRect[] labelSizes, SKPoint[] points, SKSize itemSize, int height, float headerHeight)
        {
            this.DrawLabels(canvas,
                            labels,
                            points.Select(p => new SKPoint(p.X, headerHeight - this.Margin)).ToArray(),
                            labelSizes,
                            this.Entries.Select(x => x.Color.WithAlpha((byte)(255 * this.AnimationProgress))).ToArray(),
                            this.ValueLabelOrientation,
                            true,
                            itemSize,
                            height);
        }

        protected virtual void DrawFooter(SKCanvas canvas, string[] labels, SKRect[] labelSizes, SKPoint[] points, SKSize itemSize, int height, float footerHeight)
        {
            this.DrawLabels(canvas,
                            labels,
                            points.Select(p => new SKPoint(p.X, height - footerHeight + this.Margin)).ToArray(),
                            labelSizes,
                            this.Entries.Select(x => this.LabelColor).ToArray(),
                            this.LabelOrientation,
                            false,
                            itemSize,
                            height);
        }

        protected void DrawPoints(SKCanvas canvas, SKPoint[] points)
        {
            if (points.Length > 0 && PointMode != PointMode.None)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var entry = this.Entries.ElementAt(i);
                    var point = points[i];
                    canvas.DrawPoint(point, entry.Color, this.PointSize, this.PointMode);
                }
            }
        }

        protected virtual void DrawAreas(SKCanvas canvas, SKPoint[] points, SKSize itemSize, float origin, float headerHeight)
        {
            if (points.Length > 0 && this.PointAreaAlpha > 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var entry = this.Entries.ElementAt(i);
                    var point = points[i];
                    var y = Math.Min(origin, point.Y);

                    using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, origin), new SKPoint(0, point.Y), new[] { entry.Color.WithAlpha(this.PointAreaAlpha), entry.Color.WithAlpha((byte)(this.PointAreaAlpha / 3)) }, null, SKShaderTileMode.Clamp))
                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = entry.Color.WithAlpha(this.PointAreaAlpha),
                    })
                    {
                        paint.Shader = shader;
                        var height = Math.Max(2, Math.Abs(origin - point.Y));
                        canvas.DrawRect(SKRect.Create(point.X - (this.PointSize / 2), y, this.PointSize, height), paint);
                    }
                }
            }
        }

        protected virtual void DrawLabels(SKCanvas canvas, string[] texts, SKPoint[] points, SKRect[] sizes, SKColor[] colors, Orientation orientation, bool isTop, SKSize itemSize, float height)
        {
            if (points.Length > 0)
            {
                var maxWidth = sizes.Max(x => x.Width);
                var avgHeightAdustment = 0d;

                if (isTop == false)
                {
                    avgHeightAdustment = sizes.Average(s => s.Height);
                }

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];

                    if (!string.IsNullOrEmpty(texts[i]))
                    {
                        using (new SKAutoCanvasRestore(canvas))
                        {
                            using (var paint = new SKPaint())
                            {
                                paint.TextSize = this.LabelTextSize;
                                paint.IsAntialias = true;
                                paint.Color = LabelColor;
                                paint.IsStroke = false;
                                paint.Typeface = base.Typeface;
                                var bounds = sizes[i];
                                var text = texts[i];

                                if (text != null)
                                {
                                    if (orientation == Orientation.Vertical)
                                    {
                                        var y = point.Y;
                                        if (isTop)
                                        {
                                            y -= bounds.Width;
                                        }

                                        canvas.RotateDegrees(90);
                                        canvas.Translate(y, -point.X + (bounds.Height / 2));
                                    }
                                    else
                                    {
                                        if (bounds.Width > itemSize.Width)
                                        {
                                            text = text.Substring(0, Math.Min(3, text.Length));
                                            paint.MeasureText(text, ref bounds);
                                        }

                                        if (bounds.Width > itemSize.Width)
                                        {
                                            text = text.Substring(0, Math.Min(1, text.Length));
                                            paint.MeasureText(text, ref bounds);
                                        }


                                        var y = point.Y;
                                        if (isTop)
                                        {
                                            y -= bounds.Height;
                                        }

                                        canvas.Translate(point.X - (bounds.Width / 2), y);
                                    }

                                    canvas.DrawText(text, 0, 0, paint);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shows a Y axis
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="yAxisWidth"></param>
        /// <param name="intervals"></param>
        protected virtual void DrawYAxisText(SKCanvas canvas, IEnumerable<(string Label, SKPoint Point)> intervals)
        {
            var pt = YAxisTextPaint.Clone();
            pt.TextAlign = YAxisPosition == Position.Left ? SKTextAlign.Right : SKTextAlign.Left;

            foreach (var @int in intervals)
                canvas.DrawTextCenteredVertically(@int.Label, pt, @int.Point.X, @int.Point.Y);
        }

        /// <summary>
        /// Draws interval lines
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="intervals"></param>
        protected virtual void DrawYAxisLines(SKCanvas canvas, IEnumerable<SKRect> intervals)
        {
            foreach (var @int in intervals)
                canvas.DrawLine(Margin / 2 + @int.Left, @int.Top, @int.Right - Margin / 2, @int.Bottom, YAxisLinesPaint);
        }

        /// <summary>
        /// Calculates the height of the footer.
        /// </summary>
        /// <returns>The footer height.</returns>
        /// <param name="valueLabelSizes">Value label sizes.</param>
        /// <param name="labels">Value labels.</param>
        protected float CalculateFooterHeaderHeight(SKRect[] valueLabelSizes, Orientation orientation, string[] labels)
        {
            var result = this.Margin;

            if (labels.Any(e => !string.IsNullOrEmpty(e)))
            {
                if (orientation == Orientation.Vertical)
                {
                    var maxValueWidth = valueLabelSizes.Max(x => x.Width);
                    if (maxValueWidth > 0)
                    {
                        result += maxValueWidth + this.Margin;
                    }
                }
                else
                {
                    result += this.LabelTextSize + this.Margin;
                }
            }

            return result;
        }

        /// <summary>
        /// Measures the value labels.
        /// </summary>
        /// <returns>The value labels.</returns>
        protected SKRect[] MeasureLabels(string[] labels, SKPaint paint = null)
        {
            if (paint == null)
            {
                paint = new SKPaint();
                paint.TextSize = this.LabelTextSize;
            }

            return labels.Select(text =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    return SKRect.Empty;
                }

                var bounds = new SKRect();
                paint.MeasureText(text, ref bounds);
                return bounds;
            }).ToArray();
        }

        /// <summary>
        /// Measures the value label.
        /// </summary>
        /// <returns>The value label.</returns>
        protected SKRect MeasureLabel(string label, SKPaint paint = null)
            => this.MeasureLabels(new string[1] { label }, paint)
                .First();


        #endregion
    }
}
