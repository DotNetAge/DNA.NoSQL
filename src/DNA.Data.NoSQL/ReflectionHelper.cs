//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;

namespace DNA.Data.Documents
{
    internal class InternalReflectionHelper
    {
        internal static bool PropertyExists<T>(T entity, string propertyName)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static long GetPropertyValueInt16<T>(T entity, string propertyName)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Convert.ToInt16(p.GetValue(entity, null));
                }
            }
            return 0;
        }

        internal static long GetPropertyValueInt64<T>(T entity, string propertyName)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Convert.ToInt64(p.GetValue(entity, null));
                }
            }
            return 0;
        }

        internal static int GetPropertyValueInt32<T>(T entity, string propertyName)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Convert.ToInt32(p.GetValue(entity, null));
                }
            }
            return 0;
        }

        internal static void SetPropertyValue<T>(T entity, string propertyName, long propertyValue)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    string propertyTypeName = p.PropertyType.Name;
                    if (propertyTypeName == "Int64")
                    {
                        p.SetValue(entity, propertyValue, null);
                    }
                    else if (propertyTypeName == "Int32")
                    {
                        p.SetValue(entity, Convert.ToInt32(propertyValue), null);
                    }
                    else if (propertyTypeName == "Int16")
                    {
                        p.SetValue(entity, Convert.ToInt16(propertyValue), null);
                    }
                    return;
                }
            }
        }

        internal static void SetPropertyValue<T>(T entity, string propertyName, int propertyValue)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    string propertyTypeName = p.PropertyType.Name;
                    if (propertyTypeName == "Int64")
                    {
                        p.SetValue(entity, Convert.ToInt64(propertyValue), null);
                    }
                    else if (propertyTypeName == "Int32")
                    {
                        p.SetValue(entity, propertyValue, null);
                    }
                    else if (propertyTypeName == "Int16")
                    {
                        p.SetValue(entity, Convert.ToInt16(propertyValue), null);
                    }
                    return;
                }
            }
        }

        internal static void SetPropertyValue<T>(T entity, string propertyName, short propertyValue)
        {
            var propertyInfo = entity.GetType().GetProperties();
            foreach (var p in propertyInfo)
            {
                if (p.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    string propertyTypeName = p.PropertyType.Name;
                    if (propertyTypeName == "Int64")
                    {
                        p.SetValue(entity, Convert.ToInt64(propertyValue), null);
                    }
                    else if (propertyTypeName == "Int32")
                    {
                        p.SetValue(entity, Convert.ToInt32(propertyValue), null);
                    }
                    else if (propertyTypeName == "Int16")
                    {
                        p.SetValue(entity, propertyValue, null);
                    }
                    return;
                }
            }
        }

    }
}
