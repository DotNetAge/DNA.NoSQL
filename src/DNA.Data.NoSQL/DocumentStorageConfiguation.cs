//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

namespace DNA.Data.Documents
{
    public class DocumentStorageConfiguation:IConfiguration
    {
        /// <summary>
        /// Gets/Sets the document file format the possible value is one of xml | json |blob
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Gets / Sets the base data doucments path
        /// </summary>
        public string BaseDataPath { get; set; }
    }
}
