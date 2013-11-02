//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;

namespace DNA.Data.Documents
{
    public class RepositoryFactory
    {
        public static IRepository<T> Create<T>(string type, string basePath, string table="")
            where T : class
        {
            if (type.Equals("json", StringComparison.OrdinalIgnoreCase))
                return new JSONRepository<T>(basePath, table);
            
            if (type.Equals("sql", StringComparison.OrdinalIgnoreCase))
                return new SqlCERepository<T>(basePath, table);

            if (type.Equals("bson", StringComparison.OrdinalIgnoreCase) || type.Equals("blob", StringComparison.OrdinalIgnoreCase))
                return new BlobRepository<T>(basePath, table);

            return new XmlRepository<T>(basePath, table);

        }
    }
}
