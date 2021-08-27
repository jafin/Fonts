// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables.General.Gsub
{
    /// <summary>
    /// This lookup provides a mechanism whereby any other lookup type’s subtables are stored at a 32-bit offset location
    /// in the GSUB table. This is needed if the total size of the subtables exceeds the 16-bit limits of the various
    /// other offsets in the GSUB table. In this specification, the subtable stored at the 32-bit offset location is
    /// termed the "extension" subtable.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-7-extension-substitution"/>
    /// </summary>
    internal sealed class ExtensionSubstitutionSubTable
    {
        private ExtensionSubstitutionSubTable()
        {
        }

        public static LookupSubTable Load(
            BigEndianBinaryReader reader,
            long offset,
            Func<ushort, BigEndianBinaryReader, long, LookupSubTable> subTableLoader)
        {
            // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
            // | Type     | Name                | Description                                                                                                                        |
            // +==========+=====================+====================================================================================================================================+
            // | uint16   | substFormat         | Format identifier. Set to 1.                                                                                                       |
            // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
            // | uint16   | extensionLookupType | Lookup type of subtable referenced by extensionOffset (that is, the extension subtable).                                           |
            // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
            // | Offset32 | extensionOffset     | Offset to the extension subtable, of lookup type extensionLookupType, relative to the start of the ExtensionSubstFormat1 subtable. |
            // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort format = reader.ReadUInt16();
            ushort extensionLookupType = reader.ReadUInt16();
            uint extensionOffset = reader.ReadOffset32();

            // The extensionLookupType field must be set to any lookup type other than 7.
            // All subtables in a LookupType 7 lookup must have the same extensionLookupType.
            if (extensionLookupType == 7)
            {
                // Don't throw, we'll just ignore.
                return new NotImplementedSubTable();
            }

            // Read the lookup table again with the updated offset.
            return subTableLoader.Invoke(extensionLookupType, reader, offset + extensionOffset);
        }
    }
}