//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DNA.Data.Documents
{
    public class QueueFile<T>
        where T : class
    {
        string file = "";
        long count = -1;

        public QueueFile(string directory, string table = "")
        {
            if (string.IsNullOrEmpty(table))
                table = typeof(T).Name;

            file = Path.Combine(directory, table + ".qdb");

            if (!File.Exists(file))
                File.WriteAllText(file, "");
            else
                Count();
        }

        public virtual bool IsEmpty
        {
            get { return count == 0; }
        }

        /// <summary>
        /// Reset the queue and empty the data file.
        /// </summary>
        public void Clear()
        {
            count = 0;
            File.WriteAllText(file, "");
        }

        /// <summary>
        /// Dequeue the object.
        /// </summary>
        /// <returns></returns>
        public virtual T Dequeue()
        {
            if (count == 0)
                throw new Exception("The queue is empty.");

            var jsonStr = "";
            using (var reader = new StreamReader(file))
            {
                jsonStr = reader.ReadLine();
            }

            var obj = JsonConvert.DeserializeObject<T>(jsonStr);
            count--;

            if (count > 0)
            {
                var lines = File.ReadAllLines(file).ToList();
                lines.RemoveAt(0);
                File.WriteAllLines(file, lines.ToArray());
            }
            else
                File.WriteAllText(file, "");

            return obj;

        }

        public virtual T Enqueue(T entity)
        {
            var json = JsonConvert.SerializeObject(entity);
            if (count > 0)
            {
                var list = File.ReadLines(file).ToList();
                list.Insert(0, json);
                File.WriteAllLines(file, list.ToArray());
            }
            else
                File.WriteAllLines(file, new string[] { json });
            count++;
            return entity;
        }

        public virtual T Peek()
        {
            using (var stream = File.OpenRead(file))
            {
                using (var reader = new StreamReader(stream))
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        return JsonConvert.DeserializeObject<T>(line);
                    return default(T);
                }
            }
        }

        public long Count()
        {
            if (count == -1)
                count = File.ReadLines(file).LongCount();
            return count;
        }

        private static IEnumerable<string> GetLines(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

    }
}
