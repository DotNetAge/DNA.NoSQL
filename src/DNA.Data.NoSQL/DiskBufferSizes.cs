//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license


namespace DNA.Data.Documents
{
    /// <summary>
    /// Enum disk buffer sizes.
    /// </summary>
    public enum DiskBufferSizes
    {
        /// <summary>
        /// The default 8 Bytes.
        /// </summary>
        Default = 8,
        /// <summary>
        /// Small 8 KB (8192 Bytes).
        /// </summary>
        Small = 8192,
        /// <summary>
        /// Medium 16 KB (16384 Bytes).
        /// </summary>
        Medium = 16384,
        /// <summary>
        /// Large 32 KB (32768 Bytes).
        /// </summary>
        Large = 32768,
        /// <summary>
        /// Larger 64 KB (65536 Bytes).
        /// </summary>
        Larger = 65536,
        /// <summary>
        /// Largest 128 KB (131072 Bytes).
        /// </summary>
        Maximum = 131072,
    }
}
