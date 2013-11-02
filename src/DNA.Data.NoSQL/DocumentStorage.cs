//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Represents a simple document storage use to save objects.
    /// </summary>
    public class DocumentStorage : IUnitOfWorks, IDisposable
    {
        private IDictionary<Type, object> repositoryTable = new Dictionary<Type, object>();

        /// <summary>
        ///  Initializes a new instance of DocumentStorage class.
        /// </summary>
        /// <param name="basePath">Specified the storage path to save data files.</param>
        public DocumentStorage(string basePath)
        {
            this.BaseDataPath = basePath;
            if (!System.IO.Directory.Exists(BaseDataPath))
                System.IO.Directory.CreateDirectory(BaseDataPath);

            DocumentType = "sql";
        }

        /// <summary>
        ///  Initializes a new instance of DocumentStorage class.
        /// </summary>
        /// <param name="basePath">Specified the storage path to save data files.</param>
        /// <param name="docType">Specified the document storage type.</param>
        public DocumentStorage(string basePath, string docType)
            : this(basePath)
        {
            this.DocumentType = docType;
        }

        ///// <summary>
        /////   Initializes a new instance of DocumentStorage class.
        ///// </summary>
        ///// <param name="basePath">Specified the storage path to save data files.</param>
        ///// <param name="docType">Specified the document storage type.</param>
        ///// <param name="table">Specified the database file name.</param>
        //public DocumentStorage(string basePath, string docType, string table) : this(basePath, docType) { TableName = table; }

        public string DocumentType { get; set; }

        /// <summary>
        /// Gets / Sets the base data doucments path
        /// </summary>
        public string BaseDataPath { get; set; }

        /// <summary>
        /// Gets/Sets the base data table name.
        /// </summary>
        public string TableName { get; set; }

        private IRepository<T> GetRepository<T>()
            where T : class
        {
            IRepository<T> repository = null;
            var key = typeof(T);

            if (repositoryTable.ContainsKey(key))
                repository = (IRepository<T>)repositoryTable[key];
            else
            {
                repository = RepositoryFactory.Create<T>(DocumentType, BaseDataPath);
                repositoryTable.Add(key, repository);
            }

            return repository;
        }

        /// <summary>
        /// Get object instance by specified id.
        /// </summary>
        /// <typeparam name="T">Specified the object type.</typeparam>
        /// <param name="id">Specified the object id.</param>
        /// <returns>if success returns the object otherwrise return null.</returns>
        public T Find<T>(object id) where T : class
        {
            return GetRepository<T>().Find(id);
        }

        /// <summary>
        /// Add an object to database
        /// </summary>
        /// <typeparam name="T">Specified the object type.</typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public T Add<T>(T t) where T : class
        {
            return GetRepository<T>().Create(t);
        }

        /// <summary>
        /// Add objects to database.
        /// </summary>
        /// <typeparam name="T">Specified the object type.</typeparam>
        /// <param name="items">The objects collection.</param>
        /// <returns></returns>
        public IEnumerable<T> Add<T>(IEnumerable<T> items) where T : class
        {
            var list = new List<T>();
            foreach (var item in items)
                list.Add(Add(item));
            return list;
        }

        public void Update<T>(T t) where T : class
        {
            GetRepository<T>().Update(t);
        }

        public void Delete<T>(T t) where T : class
        {
            GetRepository<T>().Delete(t);
        }

        public void Delete<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            GetRepository<T>().Delete(predicate);
        }

        public int SaveChanges()
        {
            var saved = 0;
            foreach (var k in repositoryTable.Keys)
            {
                var submit = repositoryTable[k].GetType().GetMethod("Submit");
                saved += (int)submit.Invoke(repositoryTable[k], null);
            }
            return saved;
        }

        public void Dispose()
        {
            foreach (var k in repositoryTable.Keys)
            {
                var disposable = repositoryTable[k] as IDisposable;
                disposable.Dispose();
            }

            repositoryTable.Clear();
            GC.SuppressFinalize(this);
        }

        public System.Linq.IQueryable<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
            where T : class
        {
            return GetRepository<T>().Filter(predicate);
        }

        public IQueryable<T> Where<T,Key>(Expression<Func<T, Key>> sortingSelector, Expression<Func<T, bool>> filter, out int total, SortingOrders sortby = SortingOrders.Asc, int index = 0, int size = 50)
                        where T : class
        {
            return GetRepository<T>().Filter(sortingSelector, filter, out total, sortby, index, size);
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
        {
            return GetRepository<T>().Find(predicate);
        }

        public System.Linq.IQueryable<T> All<T>() where T : class
        {
            return GetRepository<T>().All();
        }

        public IQueryable<T> All<T>(out int total, int index, int size) where T : class
        {
            return GetRepository<T>().All(out total, index, size);
        }

        public int Count<T>() where T : class
        {
            return GetRepository<T>().Count();
        }

        public int Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
        {
            return GetRepository<T>().Count(predicate);
        }

        public void Config(IConfiguration settings)
        {
            var configuration = settings as DocumentStorageConfiguation;
            if (configuration != null)
            {
                var reset = false;
                if (!string.IsNullOrEmpty(configuration.DocumentType) && !configuration.DocumentType.Equals(this.DocumentType))
                {
                    this.DocumentType = configuration.DocumentType;
                    reset = true;
                }

                if (!string.IsNullOrEmpty(configuration.BaseDataPath))
                {
                    this.BaseDataPath = this.BaseDataPath;
                    reset = true;
                }

                if (reset)
                {
                    foreach (var k in repositoryTable.Keys)
                    {
                        var disposable = repositoryTable[k] as IDisposable;
                        disposable.Dispose();
                    }
                    repositoryTable.Clear();
                }

            }
        }

        public void Clear<T>() where T : class
        {
            GetRepository<T>().Clear();
        }

    }
}
