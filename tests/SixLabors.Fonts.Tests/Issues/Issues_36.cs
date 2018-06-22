﻿using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_36
    {

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void TextWidthFroTabOnlyTextSouldBeSingleTabWidthMultipliedByTabCount(int tabCount)
        {
            Font font = CreateFont("\t x");

            SizeF tabWidth = TextMeasurer.MeasureBounds("\t", new RendererOptions(font, (72 * font.EmSize))).Size;
            string tabString = "".PadRight(tabCount, '\t');
            SizeF tabCountWidth = TextMeasurer.MeasureBounds(tabString, new RendererOptions(font, (72 * font.EmSize))).Size;

            Assert.Equal(tabWidth.Width * tabCount, tabCountWidth.Width, 2);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void TextWidthFroTabOnlyTextSouldBeSingleTabWidthMultipliedByTabCountMinusX(int tabCount)
        {
            Font font = CreateFont("\t x");

            SizeF xWidth = TextMeasurer.MeasureBounds("x", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWidth = TextMeasurer.MeasureBounds("\tx", new RendererOptions(font, (72 * font.EmSize))).Size;
            string tabString = "x".PadLeft(tabCount+1, '\t');
            SizeF tabCountWidth = TextMeasurer.MeasureBounds(tabString, new RendererOptions(font, (72 * font.EmSize))).Size;

            float singleTabWidth = tabWidth.Width - xWidth.Width;
            float finalTabWidth = tabCountWidth.Width - xWidth.Width;
            Assert.Equal(singleTabWidth * tabCount, finalTabWidth, 2);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
