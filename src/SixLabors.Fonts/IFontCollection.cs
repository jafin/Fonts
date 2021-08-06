// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readable and writable collection of fonts.
    /// </summary>
    /// <seealso cref="IReadOnlyFontCollection" />
    public interface IFontCollection : IReadOnlyFontCollection
    {
        /// <summary>
        /// Adds a font to the collection.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>The newly added <see cref="FontFamily"/>.</returns>
        FontFamily Add(Stream fontStream);
    }
}
