//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DNA.Data.Documents
{
    public class JSONRepository<T> : FileBasedRepository<T>
        where T : class
    {
        public JSONRepository(string basePath) : base(basePath) { }

        public JSONRepository(string basePath, string table) : base(basePath, table) { }

        protected override string Extension
        {
            get { return "json"; }
        }

        protected override void LoadDataFromFile(HashSet<T> container)
        {
            using (var reader = new StreamReader(DataFile))
            {
                var array = JsonConvert.DeserializeObject<T[]>(reader.ReadToEnd());
                Cache = new HashSet<T>(array);
            }
        }

        public override int Submit()
        {
            try
            {
                File.WriteAllText(DataFile, JsonConvert.SerializeObject(Cache.ToArray()));
            }
            catch (Exception e)
            {
                throw e;
            }

            return changes;
        }
    }
}
