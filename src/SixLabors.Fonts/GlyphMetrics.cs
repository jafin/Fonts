// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a glyph metric from a particular font face.
    /// </summary>
    public class GlyphMetrics
    {
        private static readonly Vector2 Scale = new Vector2(1, -1);
        private readonly GlyphVector vector;

        internal GlyphMetrics(
            FontMetrics font,
            CodePoint codePoint,
            GlyphVector vector,
            ushort advanceWidth,
            ushort advanceHeight,
            short leftSideBearing,
            short topSideBearing,
            ushort unitsPerEM,
            ushort index,
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
        {
            this.FontMetrics = font;
            this.CodePoint = codePoint;
            this.UnitsPerEm = unitsPerEM;
            this.vector = vector;

            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.Index = index;

            this.Width = this.Bounds.Max.X - this.Bounds.Min.X;
            this.Height = this.Bounds.Max.Y - this.Bounds.Min.Y;
            this.GlyphType = glyphType;
            this.LeftSideBearing = leftSideBearing;
            this.TopSideBearing = topSideBearing;
            this.ScaleFactor = this.UnitsPerEm * 72F;
            this.GlyphColor = glyphColor;
        }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        internal FontMetrics FontMetrics { get; }

        /// <summary>
        /// Gets the Unicode codepoint of the glyph.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets the advance width for horizontal layout, expressed in font units.
        /// </summary>
        public ushort AdvanceWidth { get; }

        /// <summary>
        /// Gets the advance height for vertical layout, expressed in font units.
        /// </summary>
        public ushort AdvanceHeight { get; }

        /// <summary>
        /// Gets the left side bearing for horizontal layout, expressed in font units.
        /// </summary>
        public short LeftSideBearing { get; }

        /// <summary>
        /// Gets the top side bearing for vertical layout, expressed in font units.
        /// </summary>
        public short TopSideBearing { get; }

        /// <summary>
        /// Gets the width, expressed in font units.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height, expressed in font units.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the glyph type.
        /// </summary>
        public GlyphType GlyphType { get; }

        /// <summary>
        /// Gets the color of this glyph when the <see cref="GlyphType"/> is <see cref="GlyphType.ColrLayer"/>
        /// </summary>
        public GlyphColor? GlyphColor { get; }

        /// <inheritdoc cref="IFontMetrics.UnitsPerEm"/>
        public ushort UnitsPerEm { get; }

        /// <inheritdoc cref="IFontMetrics.ScaleFactor"/>
        public float ScaleFactor { get; }

        /// <summary>
        /// Gets the points defining the shape of this glyph
        /// </summary>
        public Vector2[] ControlPoints => this.vector.ControlPoints;

        /// <summary>
        /// Gets at value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve.
        /// </summary>
        public bool[] OnCurves => this.vector.OnCurves;

        /// <summary>
        /// Gets the end points
        /// </summary>
        public ushort[] EndPoints => this.vector.EndPoints;

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        internal Bounds Bounds => this.vector.Bounds;

        /// <summary>
        /// Gets the index.
        /// </summary>
        internal ushort Index { get; }

        internal FontRectangle BoundingBox(Vector2 origin, Vector2 scaledPointSize)
        {
            Vector2 size = this.Bounds.Size() * scaledPointSize / this.ScaleFactor;
            Vector2 loc = new Vector2(this.Bounds.Min.X, this.Bounds.Max.Y) * scaledPointSize / this.ScaleFactor * Scale;

            loc = origin + loc;

            return new FontRectangle(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="lineHeight">The lineHeight the current glyph was draw against to offset topLeft while calling out to IGlyphRenderer.</param>
        /// <exception cref="NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, Vector2 dpi, float lineHeight)
        {
            location *= dpi;

            Vector2 firstPoint = Vector2.Zero;

            Vector2 scaledPoint = dpi * pointSize;

            FontRectangle box = this.BoundingBox(location, scaledPoint);

            var parameters = new GlyphRendererParameters(this, pointSize, dpi);

            if (surface.BeginGlyph(box, parameters))
            {
                if (this.GlyphColor.HasValue && surface is IColorGlyphRenderer colorSurface)
                {
                    colorSurface.SetColor(this.GlyphColor.Value);
                }

                int endOfContour = -1;
                for (int i = 0; i < this.vector.EndPoints.Length; i++)
                {
                    surface.BeginFigure();
                    int startOfContour = endOfContour + 1;
                    endOfContour = this.vector.EndPoints[i];

                    Vector2 prev = Vector2.Zero;
                    Vector2 curr = this.GetPoint(ref scaledPoint, endOfContour) + location;
                    Vector2 next = this.GetPoint(ref scaledPoint, startOfContour) + location;

                    if (this.vector.OnCurves[endOfContour])
                    {
                        surface.MoveTo(curr);
                    }
                    else
                    {
                        if (this.vector.OnCurves[startOfContour])
                        {
                            surface.MoveTo(next);
                        }
                        else
                        {
                            // If both first and last points are off-curve, start at their middle.
                            Vector2 startPoint = (curr + next) / 2;
                            surface.MoveTo(startPoint);
                        }
                    }

                    int length = endOfContour - startOfContour + 1;
                    for (int p = 0; p < length; p++)
                    {
                        prev = curr;
                        curr = next;
                        int currentIndex = startOfContour + p;
                        int nextIndex = startOfContour + ((p + 1) % length);
                        int prevIndex = startOfContour + ((length + p - 1) % length);
                        next = this.GetPoint(ref scaledPoint, nextIndex) + location;

                        if (this.vector.OnCurves[currentIndex])
                        {
                            // This is a straight line.
                            surface.LineTo(curr);
                        }
                        else
                        {
                            Vector2 prev2 = prev;
                            Vector2 next2 = next;

                            if (!this.vector.OnCurves[prevIndex])
                            {
                                prev2 = (curr + prev) / 2;
                                surface.LineTo(prev2);
                            }

                            if (!this.vector.OnCurves[nextIndex])
                            {
                                next2 = (curr + next) / 2;
                            }

                            surface.LineTo(prev2);
                            surface.QuadraticBezierTo(curr, next2);
                        }
                    }

                    surface.EndFigure();
                }
            }

            surface.EndGlyph();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 GetPoint(ref Vector2 scaledPoint, int pointIndex)
        {
            Vector2 point = Scale * (this.vector.ControlPoints[pointIndex] * scaledPoint / this.ScaleFactor); // scale each point as we go, w will now have the correct relative point size

            return point;
        }

        private static void AlignToGrid(ref Vector2 point)
        {
            var floorPoint = new Vector2(MathF.Floor(point.X), MathF.Floor(point.Y));
            Vector2 decimalPart = point - floorPoint;

            decimalPart.X = decimalPart.X < 0.5f ? 0 : 1f;
            decimalPart.Y = decimalPart.Y < 0.5f ? 0 : 1f;

            point = floorPoint + decimalPart;
        }

        private static ControlPointCollection DrawPoints(IGlyphRenderer surface, ControlPointCollection points, Vector2 point)
        {
            switch (points.Count)
            {
                case 0:
                    break;
                case 1:
                    surface.QuadraticBezierTo(
                        points.SecondControlPoint,
                        point);
                    break;
                case 2:
                    surface.CubicBezierTo(
                        points.SecondControlPoint,
                        points.ThirdControlPoint,
                        point);
                    break;
                default:
                    throw new NotSupportedException("Too many control points");
            }

            points.Clear();
            return points;
        }

        private struct ControlPointCollection
        {
            public Vector2 SecondControlPoint;
            public Vector2 ThirdControlPoint;
            public int Count;

            public void Add(Vector2 point)
            {
                switch (this.Count++)
                {
                    case 0:
                        this.SecondControlPoint = point;
                        break;
                    case 1:
                        this.ThirdControlPoint = point;
                        break;
                    default:
                        throw new NotSupportedException("Too many control points");
                }
            }

            public void ReplaceLast(Vector2 point)
            {
                this.Count--;
                this.Add(point);
            }

            public void Clear()
                => this.Count = 0;
        }
    }
}
