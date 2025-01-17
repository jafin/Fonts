// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic for laying out and measuring text.
    /// </summary>
    public static class TextMeasurer
    {
        private static readonly GlyphBounds[] EmptyGlyphMetricArray = new GlyphBounds[0];

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(string text, RendererOptions options)
            => TextMeasurerInt.Default.Measure(text.AsSpan(), options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(ReadOnlySpan<char> text, RendererOptions options)
            => TextMeasurerInt.Default.Measure(text, options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(string text, RendererOptions options)
            => TextMeasurerInt.Default.MeasureBounds(text.AsSpan(), options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, RendererOptions options)
            => TextMeasurerInt.Default.MeasureBounds(text, options);

        /// <summary>
        /// Measures the character bounds of the text. For each control character the list contains a <c>null</c> element.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <param name="characterBounds">The list of character bounds of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty bounds.</returns>
        public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, RendererOptions options, out GlyphBounds[] characterBounds)
            => TextMeasurerInt.Default.TryMeasureCharacterBounds(text, options, out characterBounds);

        internal static FontRectangle GetSize(IReadOnlyList<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.Count == 0)
            {
                return FontRectangle.Empty;
            }

            float left = glyphLayouts.Min(x => x.Location.X);
            float right = glyphLayouts.Max(x => x.Location.X + x.Width);

            // Location is bottom left of the line. We offset the bottom by the top to handle the ascender
            float top = glyphLayouts.Min(x => x.Location.Y - x.LineHeight);
            float bottom = glyphLayouts.Max(x => x.Location.Y - x.LineHeight + x.Height) - top;

            Vector2 topLeft = new Vector2(left, top) * dpi;
            Vector2 bottomRight = new Vector2(right, bottom) * dpi;

            Vector2 size = bottomRight - topLeft;
            return new FontRectangle(topLeft.X, topLeft.Y, size.X, size.Y);
        }

        internal static FontRectangle GetBounds(IReadOnlyList<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.Count == 0)
            {
                return FontRectangle.Empty;
            }

            bool hasSize = false;

            float left = int.MaxValue;
            float top = int.MaxValue;
            float bottom = int.MinValue;
            float right = int.MinValue;

            for (int i = 0; i < glyphLayouts.Count; i++)
            {
                GlyphLayout c = glyphLayouts[i];
                if (!CodePoint.IsNewLine(c.CodePoint))
                {
                    hasSize = true;
                    FontRectangle box = c.BoundingBox(dpi);
                    if (left > box.Left)
                    {
                        left = box.Left;
                    }

                    if (top > box.Top)
                    {
                        top = box.Top;
                    }

                    if (bottom < box.Bottom)
                    {
                        bottom = box.Bottom;
                    }

                    if (right < box.Right)
                    {
                        right = box.Right;
                    }
                }
            }

            if (!hasSize)
            {
                return FontRectangle.Empty;
            }

            float width = right - left;
            float height = bottom - top;

            return new FontRectangle(left, top, width, height);
        }

        internal static bool TryGetCharacterBounds(IReadOnlyList<GlyphLayout> glyphLayouts, Vector2 dpi, out GlyphBounds[] characterBounds)
        {
            bool hasSize = false;
            if (glyphLayouts.Count == 0)
            {
                characterBounds = EmptyGlyphMetricArray;
                return hasSize;
            }

            var characterBoundsList = new GlyphBounds[glyphLayouts.Count];

            for (int i = 0; i < glyphLayouts.Count; i++)
            {
                GlyphLayout c = glyphLayouts[i];

                // TODO: This sets the hasSize value to the last layout... is this correct?
                if (!CodePoint.IsNewLine(c.CodePoint))
                {
                    hasSize = true;
                }

                characterBoundsList[i] = new GlyphBounds(c.CodePoint, c.BoundingBox(dpi));
            }

            characterBounds = characterBoundsList;
            return hasSize;
        }

        internal class TextMeasurerInt
        {
            private readonly TextLayout layoutEngine;

            internal TextMeasurerInt(TextLayout layoutEngine)
                => this.layoutEngine = layoutEngine;

            /// <summary>
            /// Initializes a new instance of the <see cref="TextMeasurerInt"/> class.
            /// </summary>
            internal TextMeasurerInt()
            : this(TextLayout.Default)
            {
            }

            internal static TextMeasurerInt Default { get; set; } = new TextMeasurerInt();

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal FontRectangle MeasureBounds(ReadOnlySpan<char> text, RendererOptions options)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetBounds(glyphsToRender, new Vector2(options.DpiX, options.DpiY));
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <param name="characterBounds">The character bounds list.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, RendererOptions options, out GlyphBounds[] characterBounds)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return TryGetCharacterBounds(glyphsToRender, new Vector2(options.DpiX, options.DpiY), out characterBounds);
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal FontRectangle Measure(ReadOnlySpan<char> text, RendererOptions options)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetSize(glyphsToRender, new Vector2(options.DpiX, options.DpiY));
            }
        }
    }
}
