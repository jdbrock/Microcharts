﻿// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microcharts
{
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

        #endregion

        #region Methods

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
                            if (this.LineMode == LineMode.Spline)
                            {
                                var entry = this.Entries.ElementAt(i);
                                var nextEntry = this.Entries.ElementAt(i + 1);
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
                        //var startX = points.First().X;
                        //var endX = points.Last().X;
                        //var startY = points.Max(i => i.Y);
                        //var endY = 0f;
                        //if (pointsTo != null)
                        //{
                        //    endY = pointsTo.Min(i => i.Y);
                        //}

                        //var shader = SKShader.CreateLinearGradient(
                        //            new SKPoint(startX, startY),
                        //            new SKPoint(endX, endY),
                        //            entries.Select(x => x.Color.WithAlpha((byte)(this.LineAreaAlpha * this.AnimationProgress))).ToArray(),
                        //            null,
                        //            SKShaderTileMode.Clamp);
                        paint.Shader = EnableYFadeOutGradient ? SKShader.CreateCompose(shaderY, shaderX, SKBlendMode.SrcOut) : shaderX;

                        var path = new SKPath();

                        var initialY = origin;
                        var finishY = origin;
                        if (pointsTo != null)
                        {
                            initialY = pointsTo.Max(p => p.Y);
                            finishY = pointsTo.Min(p => p.Y);
                        }

                        path.MoveTo(points.First().X, initialY);
                        path.LineTo(points.First());

                        var last = (this.LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                        for (int i = 0; i < last; i++)
                        {
                            if (this.LineMode == LineMode.Spline)
                            {
                                var entry = entries.ElementAt(i);
                                var nextEntry = entries.ElementAt(i + 1);
                                var cubicInfo = this.CalculateCubicInfo(points, i, itemSize);
                                path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                            }
                            else if (this.LineMode == LineMode.Straight)
                            {
                                path.LineTo(points[i]);
                            }
                        }
                        
                        path.LineTo(points.Last().X, finishY);

                        path.Close();

                        canvas.DrawPath(path, paint);
                    }
                }
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