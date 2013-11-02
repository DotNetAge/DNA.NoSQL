//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;

namespace DNA.Data.Documents
{
    public class QueueStorage : IDisposable, IQueues
    {
        public string BasePath { get; private set; }

        public QueueStorage(string basePath)
        {
            BasePath = basePath;
            if (!System.IO.Directory.Exists(BasePath))
                System.IO.Directory.CreateDirectory(BasePath); 
        }

        public T Enqueue<T>(T entity) where T : class
        {
            return GetQueueFile<T>().Enqueue(entity);
        }

        public T Dequeue<T>() where T : class
        {
            return GetQueueFile<T>().Dequeue();
        }

        public long Count<T>() where T : class
        {
            return GetQueueFile<T>().Count();
        }

        public T Peek<T>() where T : class
        {
            return GetQueueFile<T>().Peek();
        }

        public bool IsEmpty<T>() where T : class
        {
            return GetQueueFile<T>().IsEmpty;
        }

        /// <summary>
        /// Clear and reset the queue file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Clear<T>() where T : class
        {
            GetQueueFile<T>().Clear();
        }

        private Dictionary<Type, object> queueTable = new Dictionary<Type, object>();

        private QueueFile<T> GetQueueFile<T>()
            where T : class
        {
            var type = typeof(T);
            if (queueTable.ContainsKey(type))
                return (QueueFile<T>)queueTable[type];
            var q = new QueueFile<T>(BasePath, type.Name);
            queueTable.Add(type, q);
            return q;
        }

        public void Dispose()
        {
            queueTable.Clear();
            GC.SuppressFinalize(this);
        }
    }


}
