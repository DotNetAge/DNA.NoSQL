//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DNA.Data.Documents
{
    public class XmlRepository<T> : FileBasedRepository<T>
        where T : class
    {
        /// <summary>
        /// Gets the data file extension. without "."
        /// </summary>
        protected override string Extension { get { return "xml"; } }

        public XmlRepository(string basePath) : base(basePath) { }

        public XmlRepository(string basePath, string table) : base(basePath, table) { }

        protected override void LoadDataFromFile(HashSet<T> container)
        {
            var xDoc = XDocument.Load(DataFile);
            var all = xDoc.Root.Elements();
            var properties = typeof(T).GetProperties().Where(w => w.CanWrite);

            foreach (var xEle in all)
            {
                var instance = Activator.CreateInstance<T>();
                var childrenEles = xEle.Elements();
                foreach (var e in childrenEles)
                {
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(e.Name.LocalName));
                    if (prop != null)
                    {
                        object val = Convert.ChangeType(e.Value, prop.PropertyType);
                        prop.SetValue(instance, val, null);
                    }
                }

                container.Add(instance);
            }
        }

        public override int Submit()
        {
            var count = changes;
            var type = typeof(T);

            var rootName = type.Name + "s";
            var nodeName = type.Name;

            if (rootName.Contains("<>"))
            {
                rootName = "objects";
                nodeName = "object";
            }

            var properies = type.GetProperties().Where(p => p.CanRead).ToList();
            var xRoot = new XElement(rootName);

            foreach (var obj in Cache)
            {
                var xEle = new XElement(nodeName);
                foreach (var p in properies)
                    xEle.Add(new XElement(p.Name, p.GetValue(obj, null)));
                xRoot.Add(xEle);
            }

            changes = 0;
            xRoot.Save(DataFile);
            return count;
        }

    }
}
