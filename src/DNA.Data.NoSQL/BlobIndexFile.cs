//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Represent an index file class.
    /// </summary>
    public class BlobIndexFile : IDisposable
    {
        // the path to where the index data is stored.
        private string _indexDataPath;

        // the filestream and binary writer for saving data indexs.
        private FileStream _fileStreamReader;
        private BinaryReader _binaryReader;

        // the filestream and binary writer for reading data indexs.
        private FileStream _fileStreamWriter;
        private BinaryWriter _binaryWriter;

        // the data index file size.
        public Int64 FileSize { get; private set; }

        // create a list of data index's for caching reads. 
        private List<BlobIndex> _cache = new List<BlobIndex>();

        /// <summary>
        /// Initializes a new instance of the BlobIndexFile class.
        /// </summary>
        /// <param name="indexPath">The path to the data index file.</param>
        public BlobIndexFile(string indexPath)
        {
            // set the file path to the data index file.
            _indexDataPath = indexPath;

            // load a file stream for reading
            _fileStreamReader = new FileStream(_indexDataPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write,
                                               (int)DiskBufferSizes.Default, FileOptions.SequentialScan);

            // load a file stream for writing
            _fileStreamWriter = new FileStream(_indexDataPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
                                               (int)DiskBufferSizes.Default, FileOptions.SequentialScan);

            // load the binary reader
            _binaryReader = new BinaryReader(_fileStreamReader);

            // load the binary writer
            _binaryWriter = new BinaryWriter(_fileStreamWriter);

            // load the total size of the index file
            this.FileSize = _binaryWriter.BaseStream.Length;
        }

        /// <summary>
        /// Initializes a new instance of the BlobIndexFile class.
        /// </summary>
        /// <param name="indexPath">The path to the data index file.</param>
        /// <param name="diskReadBufferSize">Disk read buffer size.</param>
        /// <param name="diskWriteBufferSize">Disk write buffer size.</param>
        public BlobIndexFile(string indexPath, DiskBufferSizes diskReadBufferSize, DiskBufferSizes diskWriteBufferSize)
        {
            // set the file path to the data index file.
            _indexDataPath = indexPath;

            // load a file stream for reading
            _fileStreamReader = new FileStream(_indexDataPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write,
                                               (int)diskReadBufferSize, FileOptions.SequentialScan);

            // load a file stream for writing
            _fileStreamWriter = new FileStream(_indexDataPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
                                               (int)diskWriteBufferSize, FileOptions.SequentialScan);

            // load the binary reader
            _binaryReader = new BinaryReader(_fileStreamReader);

            // load the binary writer
            _binaryWriter = new BinaryWriter(_fileStreamWriter);

            // load the total size of the index file
            this.FileSize = _binaryWriter.BaseStream.Length;
        }

        public void DestroyExistingData()
        {
            // remove document store and indexes.
            if (File.Exists(_indexDataPath))
                File.Delete(_indexDataPath);

            // load the total size of the index file
            this.FileSize = _binaryWriter.BaseStream.Length;
        }

        /// <summary>
        /// Adds the Data Index to the Data Index file and checks for duplicate.
        /// Note: The Data Index is the pointer to the record in the entites data file.
        /// </summary>
        /// <param name="searchKey">The Search Key (Primary Key) of the entity being stored in the data file.</param>
        /// <param name="pointer">The Pointer (data position) to where the entity is being stored in the data File.</param>
        /// <param name="length">The Length (number of bytes) of the data being stored in the data file.</param>
        /// <exception cref="ConcurrencyException">When a data index record is found having the same Search Key</exception>
        public void AddIndexCheckForDuplicate(BlobIndex dataIndex)
        {
            // make sure the index does not already exist if duplicate checking is enabled.
            if (DoesIndexExist(dataIndex.DocumentKey))
                throw new Exception("A Data Index record with this Search Key already exists.");

            AddIndex(dataIndex);
        }

        /// <summary>
        /// Adds the Data Index to the Data Index file.
        /// WARNING: This function does not check for duplicates and can cause problems with duplicate Data Index's.
        /// Only use AddIndex if you have some other method of making sure the Data Index's being stored are unique.
        /// e.g Using an Auto incrementing Search Key (Primary Key). Using AddIndexCheckForDuplicate is slower but makes sure there is no duplicates.
        /// Note: The Data Index is the pointer to the record in the entites data file.
        /// </summary>
        public void AddIndex(BlobIndex dataIndex)
        {
            // add the index to the dataindex file.
            _binaryWriter.BaseStream.Position = this.FileSize;
            _binaryWriter.Write(dataIndex.GetBytes());
            _binaryWriter.Flush();

            // advance the file size on. its better that we do it than call the length all the time as its quicker.
            this.FileSize += 32;

            //_cache.Clear();
        }

        /// <summary>
        /// Adds an index to the Data Index file.
        /// Overwrites the first data index found with a 0 for its document key.
        /// </summary>
        public void AddIndexOverwriteDeleted(BlobIndex dataIndex)
        {
            // add the index to the dataindex file.
            _binaryReader.BaseStream.Position = 0;
            while (_binaryReader.BaseStream.Position < this.FileSize)
            {

                // load the bytes, convert to index object and return
                byte[] dataIndexBytes = _binaryReader.ReadBytes(32);
                var existingBlobIndex = BlobIndex.Parse(dataIndexBytes);

                // check if null
                if (existingBlobIndex.DocumentKey == 0)
                {
                    _binaryWriter.BaseStream.Position = _binaryReader.BaseStream.Position;
                    _binaryWriter.Write(dataIndex.GetBytes());
                    _binaryWriter.Flush();
                    return;
                }
            }

            // not found so add to end.
            AddIndex(dataIndex);
        }

        /// <summary>
        /// Updates the data index located in the data index file.
        /// </summary>
        /// <param name="dataIndex">The changed data index to update.</param>
        public void UpdateIndex(BlobIndex dataIndex)
        {
            // add the index to the dataindex file.
            _binaryWriter.BaseStream.Position = dataIndex.Position;
            _binaryWriter.Write(dataIndex.GetBytes());
            _binaryWriter.Flush();
            _cache.Clear();
        }

        /// <summary>
        /// Removes the Data Index from the Data Index file.
        /// Note: Index is Zeroed out.
        /// </summary>
        /// <param name="searchKey">Document Key.</param>
        public void RemoveIndex(long documentKey)
        {
            // load the index to be deleted
            var dataIndex = FindIndex(documentKey);

            // if the index is null then it does not exists so return
            if (dataIndex == null)
                return;

            // found the data index, time to delete.
            // delete by zeroing out the search key and adding a pointer to this data index into the deleted index file.
            // zero out the document key (Primary Key) so that it can't be found again.
            dataIndex.MarkAsDeleted();

            // move to the data index position in the data index file and update.
            _binaryWriter.BaseStream.Position = dataIndex.Position;
            _binaryWriter.Write(dataIndex.GetBytes());
            _binaryWriter.Flush();
            _cache.Clear();
        }

        /// <summary>
        /// Finds the Data Index in the file with the given Search Key (Primary Key).
        /// </summary>
        /// <returns>The Data Index containing the data record info.</returns>
        /// <param name="searchKey">The Document Key (Primary Key) of the entity to find.</param>
        public BlobIndex FindIndex(long documentKey)
        {
            if (documentKey == 0)
                return null;

            // attempt to find the key by calculation first (Only for Autoincremented document collections). 
            // if cant find then search the full index file.
            long positionOfIndex = (documentKey - 1) * 32;
            _binaryReader.BaseStream.Position = positionOfIndex;

            // dont attempt to load bytes when the document key is at the end of the file
            if (this.FileSize > positionOfIndex)
            {
                byte[] dataIndexBytes = _binaryReader.ReadBytes(32);
                var dataIndexInitial = BlobIndex.Parse(dataIndexBytes);
                if (dataIndexInitial.DocumentKey == documentKey)
                {
                    dataIndexInitial.Position = positionOfIndex;
                    return dataIndexInitial;
                }
            }

            // search the index cache before going to disk.
            var fromCache = _cache.SingleOrDefault(key => key.DocumentKey == documentKey && key.DocumentKey > 0);
            if (fromCache != null)
                return fromCache;

            // position of where to continue the search from on disk.
            if (_cache.Count() > 0)
                positionOfIndex = (_cache.Count() - 1) * 32;
            else
                positionOfIndex = 0;

            // loop through the index until search key is found.
            _binaryReader.BaseStream.Position = positionOfIndex;
            while (_binaryReader.BaseStream.Position < this.FileSize)
            {

                // load the bytes, convert to index object and return
                byte[] dataIndexBytes = _binaryReader.ReadBytes(32);
                var dataIndex = BlobIndex.Parse(dataIndexBytes);

                // add the index to the read cache so next search is quicker.
                _cache.Add(dataIndex);

                // if the document key matches return it.
                if (dataIndex.DocumentKey == documentKey)
                {
                    dataIndex.Position = positionOfIndex;
                    return dataIndex;
                }
            }

            // the data index was not found in the data index file.
            return null;
        }

        /// <summary>
        /// Search the Data Index file for an existing Data Index with the provided Search Key (Primary Key).
        /// </summary>
        /// <returns><c>true</c>, If the Data Index exists, <c>false</c> if the Data Index does not exist.</returns>
        /// <param name="searchKey">The Search Key (Primary Key) of the entity to check is already stored.</param>
        public bool DoesIndexExist(long searchKey)
        {
            if (searchKey == 0)
                return false;

            var dataIndex = FindIndex(searchKey);
            if (dataIndex == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Gets the data index for record.
        /// Returns null when at the end of the data index file.
        /// </summary>
        /// <returns>The index for record.</returns>
        /// <param name="recdordNumber">Record number.</param>
        public BlobIndex GetBlobIndex(long dataIndexNumber)
        {
            long positionOfIndex = (dataIndexNumber - 1) * 32;
            _binaryReader.BaseStream.Position = positionOfIndex;

            if (this.FileSize > positionOfIndex)
            {
                byte[] dataIndexBytes = _binaryReader.ReadBytes(32);
                var dataIndex = BlobIndex.Parse(dataIndexBytes);
                if (dataIndex.DocumentKey > 0)
                {
                    dataIndex.Position = positionOfIndex;
                    return dataIndex;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the first available data index with space greater than the size specified in bytes.
        /// The index returned is the size greater than the length with the combined record length and padding bytes.
        /// </summary>
        /// <returns>The data index with space greater than length specified.</returns>
        /// <param name="dataLength">Data length in bytes.</param>
        public BlobIndex GetBlobIndexWithEnoughSpace(long dataLength)
        {
            long positionOfIndex = 0;
            _binaryReader.BaseStream.Position = positionOfIndex;

            // search the index cache before going to disk.
            foreach (var dataIndex in _cache)
            {
                if (dataIndex.RecordLength + dataIndex.PaddingLength > dataLength && dataIndex.DocumentKey != 0)
                {
                    return dataIndex;
                }
            }

            // position of where to continue the search from on disk.
            if (_cache.Count() > 0)
                positionOfIndex = (_cache.Count() - 1) * 32;

            // loop through the index until search key is found.
            _binaryReader.BaseStream.Position = positionOfIndex;
            while (_binaryReader.BaseStream.Position < this.FileSize)
            {

                // load the bytes, convert to index object and return
                byte[] dataIndexBytes = _binaryReader.ReadBytes(32);
                var dataIndex = BlobIndex.Parse(dataIndexBytes);

                // add the index to the read cache so next search is quicker.
                _cache.Add(dataIndex);

                // return the index if there is more space than specified.
                if (dataIndex.RecordLength + dataIndex.PaddingLength >= dataLength && dataIndex.DocumentKey != 0)
                {
                    return dataIndex;
                }
            }
            return null;
        }

        /// <summary>
        /// Releases all resource used by the BlobIndexFile object.
        /// </summary>
        public void Dispose()
        {
            // dispose of the reader and writer
            if (_binaryReader != null)
                _binaryReader.Dispose();

            // close writer
            if (_binaryWriter != null)
                _binaryWriter.Dispose();

            // clear cache
            _cache.Clear();
            _cache = null;
        }

    }
}
