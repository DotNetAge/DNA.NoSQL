//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DNA.Data.Documents
{
    public abstract class FileBasedRepository<T> : IRepository<T>, IDisposable
        where T : class
    {
        private HashSet<T> cache;
        protected int changes = 0;

        /// <summary>
        /// Gets/Sets the data file name.
        /// </summary>
        protected string DataFile { get; set; }

        /// <summary>
        /// Gets/Sets the table name
        /// </summary>
        protected string TableName { get; set; }

        /// <summary>
        /// Gets the data file extension. without "."
        /// </summary>
        protected abstract string Extension { get; }

        public FileBasedRepository(string basePath)
        {
            var type = typeof(T);
            var formattedName = type.FullName;
            if (formattedName.StartsWith("<>"))
                formattedName = type.Name.Replace("<>", "$$").Replace("`", "-");

            TableName = formattedName;
            DataFile = Path.Combine(basePath, formattedName + "." + Extension);
        }

        public FileBasedRepository(string basePath, string table)
            : this(basePath)
        {
            if (!string.IsNullOrEmpty(table))
            {
                TableName = table;
                DataFile = Path.Combine(basePath, table + "." + Extension);
            }
        }

        protected HashSet<T> Cache
        {
            get
            {
                if (cache == null)
                {
                    cache = new HashSet<T>();
                    if (File.Exists(DataFile))
                        LoadDataFromFile(cache);
                }
                return cache;
            }
            set
            {
                cache = value;
            }
        }

        protected abstract void LoadDataFromFile(HashSet<T> container);

        public virtual IQueryable<T> All()
        {
            return Cache.AsQueryable<T>();
        }

        public virtual IQueryable<T> All(out int total, int index = 0, int size = 50)
        {
            total = Cache.Count;
            var skipCount = size * index;
            if (skipCount > 0)
                return Cache.Skip(skipCount).Take(size).AsQueryable<T>();
            else
                return Cache.Take(size).AsQueryable<T>();
        }

        public virtual IQueryable<T> Filter(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return Cache.Where(predicate.Compile()).AsQueryable<T>();
        }

        public virtual IQueryable<T> Filter<Key>(System.Linq.Expressions.Expression<Func<T, Key>> sortingSelector,
            System.Linq.Expressions.Expression<Func<T, bool>> filter,
            out int total,
            SortingOrders sortby = SortingOrders.Asc,
            int index = 0,
            int size = 50)
        {
            var query = sortby == SortingOrders.Asc ? Cache.OrderBy(sortingSelector.Compile()).Where(filter.Compile())
                : Cache.OrderByDescending(sortingSelector.Compile()).Where(filter.Compile());

            total = query.Count();
            var skipCount = index * size;
            return skipCount > 0 ? query.Skip(skipCount).Take(size).AsQueryable<T>() : query.Take(size).AsQueryable<T>();
        }

        public virtual bool Contains(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return Cache.Count(predicate.Compile()) > 0;
        }

        public virtual int Count()
        {
            return Cache.Count;
        }

        public virtual int Count(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return Cache.Count(predicate.Compile());
        }

        T IRepository<T>.Find(params object[] keys)
        {
            throw new NotSupportedException();
        }

        public virtual T Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            var p = predicate.Compile();
            return Cache.FirstOrDefault(p);
        }

        public virtual T Create(T t)
        {
            if (Cache.Contains(t))
                return null;

            if (Cache.Add(t))
            {
                changes++;
                return t;
            }

            return null;
        }

        public virtual void Delete(T t)
        {
            if (Cache.Contains(t))
            {
                Cache.Remove(t);
                changes++;
            }
        }

        public virtual int Delete(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            var p = predicate.Compile();
            var obj = Cache.FirstOrDefault(p);
            if (obj != null)
            {
                Cache.Remove(obj);
                changes++;
                return 1;
            }
            return 0;
        }

        public virtual T Update(T t)
        {
            Cache.Remove(t);
            Cache.Add(t);
            changes++;
            return t;
        }

        public virtual void Clear()
        {
            cache.Clear();
            if (File.Exists(DataFile))
                File.Delete(DataFile);
        }

        public abstract int Submit();

        public void Dispose()
        {
            cache.Clear();
            cache = null;
            GC.SuppressFinalize(this);
        }
    }
}
