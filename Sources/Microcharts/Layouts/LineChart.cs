// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microcharts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SkiaSharp;

    /// <summary>
    /// ![chart](../images/Line.png)
    /// 
    /// Line chart.
    /// </summary>
    public class LineChart : PointChart
    {
        private const int TooltipYOffset = 50;
        private int _touchRadius = 33;

        private bool _shouldDrawTooltip = false;
        private SKPoint _tooltipPoint;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microcharts.LineChart"/> class.
        /// </summary>
        public LineChart()
        {
            this.PointSize = 10;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the size of the line.
        /// </summary>
        /// <value>The size of the line.</value>
        public float LineSize { get; set; } = 3;

        /// <summary>
        /// Size of text of tooltip when tooltip is enabled
        /// </summary>
        public float TooltipTextSize { get; set; } = 50;

        /// <summary>
        /// Radius for the rounding of the tooltip's corners
        /// </summary>
        public float TooltipRadius { get; set; } = 20;

        /// <summary>
        /// Color of tooltip text
        /// </summary>
        public SKColor TooltipTextColor { get; set; } = SKColors.White;

        /// <summary>
        /// Background color of Tooltip rect
        /// </summary>
        public SKColor TooltipBackgroundColor { get; set; } = SKColor.Parse("#4f4f4f");

        /// <summary>
        /// Gets or sets the line mode.
        /// </summary>
        /// <value>The line mode.</value>
        public LineMode LineMode { get; set; } = LineMode.Spline;

        /// <summary>
        /// Gets or sets the alpha of the line area.
        /// </summary>
        /// <value>The line area alpha.</value>
        public byte LineAreaAlpha { get; set; } = 32;

        /// <summary>
        /// Enables or disables a fade out gradient for the line area in the Y direction
        /// </summary>
        /// <value>The state of the fadeout gradient.</value>
        public bool EnableYFadeOutGradient { get; set; } = false;

        /// <summary>
        /// The entry for which a tooltip is shown if tooltips are enabled. Will be null otherwise.
        /// </summary>
        public ChartEntry TouchedEntry { get; private set; }

        /// <summary>
        /// The touch event handler's logic will be executed based on this property. If left as false, tooltips will not be shown.
        /// </summary>
        public bool IsTooltipEnabled { get; set; }

        /// <summary>
        /// A collection of additional values that can be shown on the chart. Can be used to draw several separate lines on the chart.
        /// </summary>
        public IEnumerable<AreaEntry> AreaEntries { get; set; }

        /// <summary>
        /// Indicates whether the points for the AreaEntries should be drawn
        /// </summary>
        public bool ShouldDrawAreaPoints { get; set; } = true;

        #endregion

        #region Methods

        public override void TapCanvas(SKPoint locationTapped)
        {
            base.TapCanvas(locationTapped);
            if (IsTooltipEnabled)
            {
                for (int i = 0; i < EntriesPoints.Length; i++)
                {
                    SKPoint point = (SKPoint)EntriesPoints[i];
                    var distance = Math.Pow(point.X - locationTapped.X, 2) + Math.Pow(point.Y - locationTapped.Y, 2);
                    if (distance <= Math.Pow(_touchRadius, 2))
                    {
                        if (_tooltipPoint != point)
                        {
                            TouchedEntry = Entries.ElementAt(i);
                            _shouldDrawTooltip = true;
                            _tooltipPoint = point;
                            return;
                        }
                    }
                }
            }

            _shouldDrawTooltip = false;
            _tooltipPoint = default;
            TouchedEntry = null;
        }

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            base.DrawContent(canvas, width, height);
            if (AreaEntries != null)
            {
                var fromEntries = AreaEntries.Select(ae => new ChartEntry(ae.FromValue) { Color = ae.Color });
                var toEntries = AreaEntries.Select(ae => new ChartEntry(ae.ToValue) { Color = ae.Color });
                var fromEntriesPoints = CalculatePoints(ItemSize, Origin, HeaderHeight, fromEntries, YAxisXShift);
                var toEntriesPoints = CalculatePoints(ItemSize, Origin, HeaderHeight, toEntries, YAxisXShift);

                if (ShouldDrawAreaPoints)
                {
                    DrawPoints(canvas, fromEntriesPoints, fromEntries);
                    DrawPoints(canvas, toEntriesPoints, toEntries);
                }

                this.DrawArea(canvas, fromEntriesPoints, ItemSize, Origin, fromEntries, toEntriesPoints);
                this.DrawLine(canvas, fromEntriesPoints, ItemSize, fromEntries);
                this.DrawLine(canvas, toEntriesPoints, ItemSize, toEntries);
            }

            if (_shouldDrawTooltip)
            {
                int pointForDrawingIndex = Array.IndexOf(EntriesPoints, _tooltipPoint);

                if (pointForDrawingIndex >= 0)
                {
                    DrawTooltip(canvas, Entries.ElementAt(pointForDrawingIndex));
                }
            }
        }

        protected override void DrawAreas(SKCanvas canvas, SKPoint[] points, SKSize itemSize, float origin,
            float headerHeight, IEnumerable<ChartEntry> entries, SKPoint[] pointsTo)
        {
            //base.DrawAreas(canvas, points, itemSize, origin);
            this.DrawArea(canvas, points, itemSize, origin, entries, pointsTo);
            this.DrawLine(canvas, points, itemSize, entries);
        }

        protected void DrawLine(SKCanvas canvas, SKPoint[] points, SKSize itemSize, IEnumerable<ChartEntry> entries)
        {
            if (points.Length > 1 && this.LineMode != LineMode.None)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = this.LineSize,
                    IsAntialias = true,
                })
                {
                    using (var shader = this.CreateXGradient(points, entries))
                    {
                        paint.Shader = shader;

                        var path = new SKPath();

                        path.MoveTo(points.First());

                        var last = (this.LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                        for (int i = 0; i < last; i++)
                        {
                            var entry = entries.ElementAt(i);
                            if (this.LineMode == LineMode.Spline)
                            {
                                var nextEntry = entries.ElementAt(i + 1);
                                var cubicInfo = this.CalculateCubicInfo(points, i, itemSize);
                                path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                            }
                            else if (this.LineMode == LineMode.Straight)
                            {
                                path.LineTo(points[i]);
                            }
                        }

                        canvas.DrawPath(path, paint);
                    }
                }
            }
        }

        protected void DrawArea(SKCanvas canvas, SKPoint[] points, SKSize itemSize, float origin, IEnumerable<ChartEntry> entries, SKPoint[] pointsTo = null)
        {
            if (this.LineAreaAlpha > 0 && points.Length > 1)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White,
                    IsAntialias = true,
                })
                {
                    using (var shaderX = this.CreateXGradient(points, entries, (byte)(this.LineAreaAlpha * this.AnimationProgress)))
                    using (var shaderY = this.CreateYGradient(points, (byte)(this.LineAreaAlpha * this.AnimationProgress)))
                    {
                        paint.Shader = EnableYFadeOutGradient ? SKShader.CreateCompose(shaderY, shaderX, SKBlendMode.SrcOut) : shaderX;

                        var path = new SKPath();

                        var initialY = origin;
                        var finishY = origin;
                        if (pointsTo != null)
                        {
                            initialY = pointsTo.First().Y;
                            finishY = pointsTo.Last().Y;
                        }

                        path.MoveTo(points.First().X, initialY);
                        path.LineTo(points.First());

                        var last = (this.LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                        for (int i = 0; i < last; i++)
                        {
                            if (this.LineMode == LineMode.Spline)
                            {
                                var cubicInfo = this.CalculateCubicInfo(points, i, itemSize);
                                path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                            }
                            else if (this.LineMode == LineMode.Straight)
                            {
                                path.LineTo(points[i]);
                            }
                        }

                        if (pointsTo != null)
                        {
                            last = (this.LineMode == LineMode.Spline) ? 1 : 0;
                            if (LineMode == LineMode.Spline)
                            {
                                path.LineTo(pointsTo.Last());
                            }

                            for (int i = pointsTo.Length - 1; i >= last; i--)
                            {
                                if (this.LineMode == LineMode.Spline)
                                {
                                    var cubicInfo = this.CalculateReverseCubicInfo(pointsTo, i, itemSize);
                                    path.CubicTo(cubicInfo.control, cubicInfo.previousControl, cubicInfo.previousPoint);
                                }
                                else if (this.LineMode == LineMode.Straight)
                                {
                                    path.LineTo(pointsTo[i]);
                                }
                            }
                        }
                        
                        path.LineTo(points.Last().X, finishY);

                        path.Close();

                        canvas.DrawPath(path, paint);
                    }
                }
            }
        }

        private void DrawTooltip(SKCanvas canvas, ChartEntry entry)
        {
            using (var textPaint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = TooltipTextColor,
                TextAlign = SKTextAlign.Center,
                TextSize = TooltipTextSize
            })
            {
                var topBottomMargin = TooltipTextSize;
                var tooltipTextYPosition = _tooltipPoint.Y - TooltipYOffset - topBottomMargin;
                var textPath = textPaint.GetTextPath(entry.Label, _tooltipPoint.X, tooltipTextYPosition);
                using (var tooltipBackgroundPaint = new SKPaint
                {
                    Style = SKPaintStyle.StrokeAndFill,
                    Color = TooltipBackgroundColor
                })
                {
                    var leftRightMargin = TooltipTextSize / 2;
                    canvas.DrawRoundRect(new SKRect(textPath.Bounds.Left - leftRightMargin, textPath.Bounds.Top - topBottomMargin, textPath.Bounds.Right + leftRightMargin, textPath.Bounds.Bottom + topBottomMargin), TooltipRadius, TooltipRadius, tooltipBackgroundPaint);
                }

                canvas.DrawText(entry.Label, _tooltipPoint.X, tooltipTextYPosition, textPaint);
            }
        }

        private (SKPoint point, SKPoint control, SKPoint nextPoint, SKPoint nextControl) CalculateCubicInfo(SKPoint[] points, int i, SKSize itemSize)
        {
            var point = points[i];
            var nextPoint = points[i + 1];
            var controlOffset = new SKPoint(itemSize.Width * 0.8f, 0);
            var currentControl = point + controlOffset;
            var nextControl = nextPoint - controlOffset;
            return (point, currentControl, nextPoint, nextControl);
        }

        private (SKPoint point, SKPoint control, SKPoint previousPoint, SKPoint previousControl) CalculateReverseCubicInfo(SKPoint[] points, int i, SKSize itemSize)
        {
            var point = points[i];
            var previousPoint = points[i - 1];
            var controlOffset = new SKPoint(itemSize.Width * 0.8f, 0);
            var currentControl = point - controlOffset;
            var previousControl = previousPoint + controlOffset;
            return (point, currentControl, previousPoint, previousControl);
        }

        private SKShader CreateXGradient(SKPoint[] points, IEnumerable<ChartEntry> entries, byte alpha = 255)
        {
            var startX = points.First().X;
            var endX = points.Last().X;
            var rangeX = endX - startX;

            return SKShader.CreateLinearGradient(
                new SKPoint(startX, 0),
                new SKPoint(endX, 0),
                entries.Select(x => x.Color.WithAlpha(alpha)).ToArray(),
                null,
                SKShaderTileMode.Clamp);
        }

        private SKShader CreateYGradient(SKPoint[] points, byte alpha = 255)
        {
            var startY = points.Max(i => i.Y);
            var endY = 0f;

            return SKShader.CreateLinearGradient(
                new SKPoint(0, startY),
                new SKPoint(0, endY),
                new SKColor[] {SKColors.White.WithAlpha(alpha), SKColors.White.WithAlpha(0)},
                null,
                SKShaderTileMode.Clamp);
        }

        #endregion
    }
}