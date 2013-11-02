//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Represent a blob repository base on a sample KV database
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlobRepository<T> : IRepository<T>, IDisposable
       where T : class
    {
        #region Fields
        // paths to the data path and indexes.
        private string _dataFilePath;
        private string _dataIndexFilePath;
        private string _deletedDataIndexFilePath;

        // the document store entity type for checking document is valid.
        private string _entityName;

        //// the data index file is used for saving/loading documents in the datastore.
        //private BlobIndexFile _dataIndexFile;
        //// the data index file is used for saving/loading pointers to deleted documents in the data file.
        //private BlobIndexFile _deletedDataIndexFile;

        // the filestream and binary reader for querying documents from the data file.
        //private FileStream _fileStreamReader;
        //private BinaryReader _binaryReader;

        // the filestream and binary writer for saving documents to the data file.
        //private FileStream _fileStreamWriter;
        //private BinaryWriter _binaryWriter;
        private readonly IEntitySerializer _serializer;

        #endregion

        public BlobRepository(string basePath, string tableName)
        {

            // create directory
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            if (string.IsNullOrEmpty(tableName))
                tableName = typeof(T).Name;

            // assign file paths
            _dataFilePath = Path.Combine(basePath, String.Format(@"{0}.ndb", tableName));
            _dataIndexFilePath = Path.Combine(basePath, String.Format(@"{0}.idx", tableName));
            _deletedDataIndexFilePath = Path.Combine(basePath, String.Format(@"{0}_deleted.idx", tableName));

            // assign the type of entities that are to be stores in this document store;
            _entityName = tableName;

            // assign serializer
            _serializer = new BSonSerializer();

            // call function to initialise document store
            Initialize();
        }

        /// <summary>
        /// Gets the size of the document data file in Bytes.
        /// </summary>
        /// <value>The size of the file in Bytes.</value>
        public Int64 FileSize { get; private set; }

        /// <summary>
        /// Gets or sets the padding factor for each document stored.
        /// This is how much extra space is assigned to each document for allowing the document to expand.
        /// </summary>
        /// <value>The padding factor percentage.</value>
        public int PaddingFactor { get; set; }

        // return the data header from the start of the data file/
        private BlobDataFileHeader GetDataHeader()
        {
            // if the data file is empty create a new header
            if (this.FileSize == 0)
            {
                var header = new BlobDataFileHeader();
                UpdateDataHeader(header);
                this.FileSize = 64;
                return header;
            }

            BlobDataFileHeader dataHeader = null;
            using (var _fileStreamReader = new FileStream(_dataFilePath, FileMode.OpenOrCreate,
                FileAccess.Read,
                FileShare.Write,
                (int)DiskBufferSizes.Default,
                FileOptions.SequentialScan))
            {
                using (var _binaryReader = new BinaryReader(_fileStreamReader))
                {
                    _binaryReader.BaseStream.Position = 0;
                    var headerBytes = _binaryReader.ReadBytes(64);
                    dataHeader = BlobDataFileHeader.Parse(headerBytes);
                }
            }

            return dataHeader;
        }

        // update the data header located at the start of the data file.
        private void UpdateDataHeader(BlobDataFileHeader dataHeader)
        {
            var headerBytes = dataHeader.GetBytes();
            using (var _fileStreamWriter = new FileStream(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
                                               (int)DiskBufferSizes.Default, FileOptions.SequentialScan))
            {
                using (var _binaryWriter = new BinaryWriter(_fileStreamWriter))
                {
                    _binaryWriter.BaseStream.Position = 0;
                    _binaryWriter.Write(headerBytes);
                    _binaryWriter.Flush();
                }
            }
        }

        /// <summary>
        /// Initialise this instance. 
        /// </summary>
        public void Initialize()
        {
            Dispose();

            //// load a file stream for reading
            //_fileStreamReader = new FileStream(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write,
            //                                   (int)DiskBufferSizes.Default, FileOptions.SequentialScan);

            //// load a file stream for writing
            //_fileStreamWriter = new FileStream(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
            //                                   (int)DiskBufferSizes.Default, FileOptions.SequentialScan);

            //// load the binary reader
            //_binaryReader = new BinaryReader(_fileStreamReader);

            //// load the binary writer
            //_binaryWriter = new BinaryWriter(_fileStreamWriter);

            //// create the data index processor for storing document key indexes that reference to document records.
            //_dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default);

            //// create the deleted data index processor for storing data indexes to deleted or moved document records.
            //_deletedDataIndexFile = new BlobIndexFile(_deletedDataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default);

            // set if the datastore is empty or not.
            if (!File.Exists(_dataFilePath))
            {
                DestroyExistingData();
            }

            // assign file size
            //this.FileSize = _binaryReader.BaseStream.Length;
        }

        /// <summary>
        /// Destroys all document data and indexes for the entity belonging to the current instance.
        /// </summary>
        public void DestroyExistingData()
        {
            // remove document store and indexes.
            if (File.Exists(_dataFilePath))
                File.Delete(_dataFilePath);

            if (File.Exists(_dataIndexFilePath))
                File.Delete(_dataIndexFilePath);

            if (File.Exists(_deletedDataIndexFilePath))
                File.Delete(_deletedDataIndexFilePath);


            // load the total size of the index file
            using (var _fileStreamWriter = new FileStream(_dataFilePath, FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.Read, (int)DiskBufferSizes.Default, FileOptions.SequentialScan))
            {

                using (var _binaryWriter = new BinaryWriter(_fileStreamWriter))
                {
                    this.FileSize = _binaryWriter.BaseStream.Length;
                }
            }

            //using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            //{
            //    _dataIndexFile.DestroyExistingData();
            //}
        }

        public void Dispose()
        {
            //// close the index processor
            //if (_dataIndexFile != null)
            //    _dataIndexFile.Dispose();

            //// close the deleted index processor
            //if (_deletedDataIndexFile != null)
            //    _deletedDataIndexFile.Dispose();

            //// dispose of the reader and writer
            //if (_binaryReader != null)
            //    _binaryReader.Dispose();

            //// close writer
            //if (_binaryWriter != null)
            //    _binaryWriter.Dispose();
        }

        public IQueryable<T> All()
        {
            // create a list to hold the documents.
            var documentList = new List<T>();
            long count = 1;

            using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            {
                using (var _fileStreamReader = new FileStream(_dataFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.Read, FileShare.Write,
                    (int)DiskBufferSizes.Default,
                    FileOptions.SequentialScan))
                {
                    using (var _binaryReader = new BinaryReader(_fileStreamReader))
                    {
                        var dataIndex = _dataIndexFile.GetBlobIndex(count);
                        while (dataIndex != null)
                        {

                            // load the data from the pointer specified in the data index.
                            // locate the data in the data store file.
                            _binaryReader.BaseStream.Position = dataIndex.Pointer;
                            var dataBytes = _binaryReader.ReadBytes(dataIndex.RecordLength);

                            // parse bytes into an entity and then return
                            var entity = _serializer.Deserialize<T>(dataBytes);
                            documentList.Add(entity);

                            count++;
                            dataIndex = _dataIndexFile.GetBlobIndex(count);
                        }
                    }
                }
            }

            return documentList.AsQueryable<T>();
        }

        public IQueryable<T> All(out int total, int index = 0, int size = 50)
        {
            var entities = All();
            total = entities.Count();
            var skipCount = size * index;
            if (skipCount > 0)
                return entities.Skip(skipCount).Take(size).AsQueryable<T>();
            else
                return entities.Take(size).AsQueryable<T>();
        }

        public IQueryable<T> Filter(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().Where(predicate.Compile()).AsQueryable<T>();
        }

        public IQueryable<T> Filter<Key>(System.Linq.Expressions.Expression<Func<T, Key>> sortingSelector, System.Linq.Expressions.Expression<Func<T, bool>> filter, out int total, SortingOrders sortby = SortingOrders.Asc, int index = 0, int size = 50)
        {
            var entities = All();
            var query = sortby == SortingOrders.Asc ? entities.OrderBy(sortingSelector.Compile()).Where(filter.Compile())
                : entities.OrderByDescending(sortingSelector.Compile()).Where(filter.Compile());

            total = query.Count();
            var skipCount = index * size;
            return skipCount > 0 ? query.Skip(skipCount).Take(size).AsQueryable<T>() : query.Take(size).AsQueryable<T>();
        }

        public bool Contains(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().Count(predicate.Compile()) > 0;
        }

        public long Count()
        {
            var header = GetDataHeader();
            return header.RecordCount;
        }

        int IRepository<T>.Count() { return (int)this.Count(); }

        public int Count(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().Count(predicate.Compile());
        }

        public T Find(params object[] keys)
        {
            byte[] dataBytes = null;
            using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            {
                using (var _fileStreamReader = new FileStream(_dataFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.Read, FileShare.Write,
                    (int)DiskBufferSizes.Default,
                    FileOptions.SequentialScan))
                {
                    using (var _binaryReader = new BinaryReader(_fileStreamReader))
                    {

                        var dataIndex = _dataIndexFile.FindIndex((long)Convert.ChangeType(keys[0], typeof(long)));
                        if (dataIndex == null)
                            return default(T);
                        // locate the data in the data store file.
                        _binaryReader.BaseStream.Position = dataIndex.Pointer;
                        dataBytes = _binaryReader.ReadBytes(dataIndex.RecordLength);
                    }
                }
            }
            // parse bytes into an entity and then return
            var entity = _serializer.Deserialize<T>(dataBytes);
            return entity;
        }

        public T Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().FirstOrDefault(predicate.Compile());
        }

        public T Create(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("Entity argument can't be null");

            // make sure the entity name matches the document store type.
            string requiredEntityName = entity.GetType().Name;
            if (_entityName != requiredEntityName)
                throw new ArgumentException("Entity type is not valid for this data store.");

            // make sure entity has key field
            if (!InternalReflectionHelper.PropertyExists(entity, "Id"))
                throw new Exception("Entity must have an Id property and be of type short, integer or long." +
                    "This is used as the primary key for the entity being stored.");

            // load the document key from the entity as its needed for adding to the index.
            var documentKey = InternalReflectionHelper.GetPropertyValueInt64(entity, "Id");

            // get the data store header so we can generate keys and store record counts.
            var header = GetDataHeader();

            // boolean so we know to check for duplicate or not on insert.
            // duplicates only need checked when the user has specified the document key.
            bool checkForDuplicate = true;
            if (documentKey == 0)
                checkForDuplicate = false;

            // get the next document key from the data file header record.
            documentKey = header.GenerateNextRecord(documentKey);

            // update the entity value so that the callers entity gets the saved document key.
            InternalReflectionHelper.SetPropertyValue(entity, "Id", documentKey);

            // parse the document into a binary json document for storing in the data file.
            byte[] binaryJson = _serializer.Serialize<T>(entity);

            using (var _deletedDataIndexFile = new BlobIndexFile(_deletedDataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            {
                // create the data index with the data pointer at the end of the document.
                // check to see if there is a deleted slot that can be used to store the data.
                var dataIndex = _deletedDataIndexFile.GetBlobIndexWithEnoughSpace(binaryJson.Length);
                if (dataIndex != null)
                {
                    // assign this document key to the deleted index.
                    dataIndex.ChangeDocumentKey(documentKey);
                    dataIndex.UpdateRecordLength(binaryJson.Length);
                }
                else
                {
                    // create a new data index.
                    dataIndex = new BlobIndex(documentKey, this.FileSize, binaryJson.Length, this.PaddingFactor);

                    // update the size of the datafile
                    this.FileSize = this.FileSize + dataIndex.RecordLength + dataIndex.PaddingLength;
                }

                using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
                {
                    // create the data index (AddIndex throws ConcurrencyException so no data will save)
                    if (checkForDuplicate)
                        _dataIndexFile.AddIndexCheckForDuplicate(dataIndex);
                    else
                        _dataIndexFile.AddIndex(dataIndex);
                }

                // remove the index from the deleted index file if it exists
                _deletedDataIndexFile.RemoveIndex(dataIndex.DocumentKey);

                using (var _fileStreamWriter = new FileStream(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
                                                   (int)DiskBufferSizes.Default, FileOptions.SequentialScan))
                {

                    using (var _binaryWriter = new BinaryWriter(_fileStreamWriter))
                    {
                        // add the data record to the data file
                        _binaryWriter.BaseStream.Position = dataIndex.Pointer;

                        // write the record
                        _binaryWriter.Write(binaryJson);

                        // write the padding.
                        if (dataIndex.PaddingLength > 0)
                        {
                            _binaryWriter.Write(new Byte[dataIndex.PaddingLength]);
                        }

                        // save the data
                        _binaryWriter.Flush();
                    }
                }
                // update the header record
                UpdateDataHeader(header);
            }

            return entity;
        }

        public void Delete(T t)
        {
            using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            {
                var searchKey = InternalReflectionHelper.GetPropertyValueInt32<T>(t, "Id");
                var dataIndex = _dataIndexFile.FindIndex(searchKey);

                if (dataIndex == null)
                    return;

                using (var _deletedDataIndexFile = new BlobIndexFile(_deletedDataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
                {
                    // create a new deleted data index in deleted file pointing to deleted data.
                    _deletedDataIndexFile.AddIndexOverwriteDeleted(dataIndex);
                }

                // no need to delete the data just mark the index entry as deleted
                _dataIndexFile.RemoveIndex(searchKey);

                // update the record count.
                var header = GetDataHeader();
                header.RemoveRecord();
                UpdateDataHeader(header);
            }
        }

        public int Delete(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            throw new NotSupportedException();
        }

        public T Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("Entity argument can't be null");

            // make sure the entity name matches the document store type.
            string requiredEntityName = entity.GetType().Name;
            if (_entityName != requiredEntityName)
                throw new ArgumentException("Entity type is not valid for this data store.");

            // make sure entity has key field
            if (!InternalReflectionHelper.PropertyExists(entity, "Id"))
                throw new Exception("Entity must have an Id property and be of type short, integer or long." +
                    "This is used as the primary key for the entity being stored.");

            // load the document key from the entity as its needed for adding to the index.
            var documentKey = InternalReflectionHelper.GetPropertyValueInt64(entity, "Id");

            using (var _dataIndexFile = new BlobIndexFile(_dataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
            {
                // search the data store for the document.
                var dataIndex = _dataIndexFile.FindIndex(documentKey);
                if (dataIndex == null)
                    throw new Exception("Could not find an existing entity to update.");

                // either overwrite entity in existing data slot or append to the the end of the file.
                // parse the document into a binary json document
                byte[] binaryJson = _serializer.Serialize<T>(entity);

                // write the data record to the data file
                // update the record index to the documents data size.
                dataIndex.UpdateRecordLength(binaryJson.Length);
                
                using (var _fileStreamWriter = new FileStream(_dataFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
                                                 (int)DiskBufferSizes.Default, FileOptions.SequentialScan))
                {

                    using (var _binaryWriter = new BinaryWriter(_fileStreamWriter))
                    {
                        // check to see if the record needs relocated.
                        // if it does then set the position to the end of the file.
                        if (dataIndex.RequiresRelocation == true)
                        {
                            using (var _deletedDataIndexFile = new BlobIndexFile(_deletedDataIndexFilePath, DiskBufferSizes.Larger, DiskBufferSizes.Default))
                            {
                                // create a deleted record pointer to the data file.
                                _deletedDataIndexFile.AddIndexOverwriteDeleted(dataIndex);
                            }

                            // set position of the document to the end of the file
                            _binaryWriter.BaseStream.Position = this.FileSize;

                            // update the record pointer in the data index
                            dataIndex.UpdateRecordPointer(_binaryWriter.BaseStream.Position, this.PaddingFactor);

                            // update the file size.
                            this.FileSize = this.FileSize + dataIndex.RecordLength + dataIndex.PaddingLength;
                        }
                        else
                        {
                            // set the position of where the document is in the datafile.
                            _binaryWriter.BaseStream.Position = dataIndex.Pointer;
                        }

                        // make changes to the data
                        _binaryWriter.Write(binaryJson);

                        // write the padding bytes
                        if (dataIndex.PaddingLength > 0)
                        {
                            _binaryWriter.Write(new Byte[dataIndex.PaddingLength]);
                        }

                        // update data index pointer to point to new location.
                        _dataIndexFile.UpdateIndex(dataIndex);

                        // save the data
                        _binaryWriter.Flush();
                    }
                }
            }

            return entity;
        }

        public int Submit()
        {
            return 0;
        }

        public virtual void Clear()
        {
            DestroyExistingData();
        }
    }



}
