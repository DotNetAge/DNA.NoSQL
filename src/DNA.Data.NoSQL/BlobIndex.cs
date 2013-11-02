//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Represent an index class.
    /// </summary>
    public class BlobIndex
    {
        /// <summary>
        /// Gets or sets the Document Key of the document stored in the datafile.
        /// </summary>
        public Int64 DocumentKey { get; private set; }  	// 8 Bytes	(The Document Primary Key)

        /// <summary>
        /// Gets or sets the Pointer (Position) of the document in the datafile.
        /// </summary>
        public Int64 Pointer { get; private set; }    	// 8 Bytes	(The Position of the dcocument in the data file)

        /// <summary>
        /// Gets or sets the length of the Document in (Bytes) stored in the datafile
        /// </summary>
        public Int32 RecordLength { get; private set; }   // 4 Bytes  (The Length in bytes of the document in the data file)  

        /// <summary>
        /// Gets or sets the length of the padding bytes located after the document which allows room for the document to grow
        /// without having to relocate the document in the datafile.
        /// </summary>
        public Int32 PaddingLength { get; private set; }  // 4 Bytes  (The Length of padding bytes for allowing the document to expand)

        // 24 Bytes In Use                           
        // 8 Bytes Reserved (Padding)   
        // 32 Bytes Total Data (To align with iPhone 8KB Block size   


        /// <summary>
        /// The position of the data index in the data index file.
        /// Note: This property is not stored in the Data Index file.
        /// Its used as a temporary helper property by find operations for locating.
        /// </summary>
        public Int64 Position { get; set; }

        /// <summary>
        /// On an update value is set to True if the document needs relocated in the data file.
        /// </summary>
        public bool RequiresRelocation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonoDS.Storage.DataIndex"/> class.
        /// </summary>
        /// <param name="documentKey">The Document key being stored in the data file.</param>
        /// <param name="pointer">The Pointer to the document key located in the data file.</param>
        /// <param name="recordLength">The total Record length of the data located in the data file.</param>
        /// <param name="paddingFactorPercent">The Padding factor percentage (Amount of extra padding space for data increases).</param>
        public BlobIndex(Int64 documentKey, Int64 pointer, Int32 recordLength, int paddingFactorPercent)
        {
            this.DocumentKey = documentKey;
            this.Pointer = pointer;
            this.RecordLength = recordLength;

            GeneratePaddingLength(paddingFactorPercent);
        }

        /// <summary>
        /// Marks the index to the document record as deleted by zeroing out the document key.
        /// This prevents searches from finding the index to the document data.
        /// </summary>
        public void MarkAsDeleted()
        {
            this.DocumentKey = 0;
        }

        /// <summary>
        /// Updates the record length and re-calculates the padding length.
        /// </summary>
        public void UpdateRecordLength(Int32 newRecordLength)
        {
            var paddingChangeDifference = this.RecordLength - newRecordLength;
            this.PaddingLength = this.PaddingLength + paddingChangeDifference;
            this.RecordLength = newRecordLength;

            if (this.PaddingLength < 0)
            {
                this.RequiresRelocation = true;
                this.PaddingLength = 0;
            }
        }

        /// <summary>
        /// Changes the document key.
        /// </summary>
        /// <param name="documentKey">Document key.</param>
        public void ChangeDocumentKey(Int64 documentKey)
        {
            this.DocumentKey = documentKey;
        }

        /// <summary>
        /// Updates the record pointer and generates a new Padding Length.
        /// Note: This should be called when a document has been relocated to the end of the document datafile.
        /// </summary>
        /// <param name="newPointer">New pointer.</param>
        /// <param name="paddingFactor">Padding factor.</param>
        public void UpdateRecordPointer(Int64 newPointer, Int32 paddingFactor)
        {
            this.Pointer = newPointer;
            this.RequiresRelocation = false;

            GeneratePaddingLength(paddingFactor);
        }

        public byte[] GetBytes()
        {
            // convert properties into bytes
            byte[] documentKey = BitConverter.GetBytes(this.DocumentKey);
            byte[] pointer = BitConverter.GetBytes(this.Pointer);
            byte[] recordLength = BitConverter.GetBytes(this.RecordLength);
            byte[] paddingLength = BitConverter.GetBytes(this.PaddingLength);

            // allocate the 32 byte array
            byte[] bytes = new byte[32];

            // copy property bytes into the allocated array
            Buffer.BlockCopy(documentKey, 0, bytes, 0, 8);
            Buffer.BlockCopy(pointer, 0, bytes, 8, 8);
            Buffer.BlockCopy(recordLength, 0, bytes, 16, 4);
            Buffer.BlockCopy(paddingLength, 0, bytes, 20, 4);

            // If the system architecture is little-endian (that is, little end first), 
            // reverse the byte array. 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            // return the allocated bytes containing properties.
            return bytes;
        }

        /// <summary>
        // Parse a single chunk of bytes into a single data index. 
        /// </summary>
        /// <returns>DataIndex object containing record info.</returns>
        /// <param name="bytes">The array of Bytes to parse (Must be 32 Bytes).</param>
        public static BlobIndex Parse(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("byte array is required.");
            if (bytes.Length != 32)
                throw new ArgumentException("byte array of length 32 is required.");

            // If the system architecture is little-endian (that is, little end first), 
            // reverse the byte array. 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            // create an empty index and then set properties from loaded bytes.
            var dataIndex = new BlobIndex(0, 0, 0, 0);
            dataIndex.DocumentKey = BitConverter.ToInt64(bytes, 0);
            dataIndex.Pointer = BitConverter.ToInt64(bytes, 8);
            dataIndex.RecordLength = BitConverter.ToInt32(bytes, 16);
            dataIndex.PaddingLength = BitConverter.ToInt32(bytes, 20);

            // return the data index.
            return dataIndex;
        }

        private void GeneratePaddingLength(Int32 paddingFactorPercentage)
        {
            // make sure above 1 as 1 is 0 bytes padding factor
            // 1.5 is 50%
            if (paddingFactorPercentage <= 1)
                paddingFactorPercentage = 0;

            // Padding Factor of 1.5 50%
            double percent = (double)paddingFactorPercentage / 100;
            this.PaddingLength = Convert.ToInt32(this.RecordLength * percent);
        }
    }
}
