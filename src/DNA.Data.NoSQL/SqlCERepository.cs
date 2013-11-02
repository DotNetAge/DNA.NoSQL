//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Represent a no sql repository base on SQLCe
    /// </summary>
    public class SqlCERepository<T> : IRepository<T>, IDisposable
       where T : class
    {
        SqlCeConnection sqlCnn = null;
        string dataFile = "";

        public SqlCERepository(string basePath, string tableName)
        {
            // create directory
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            if (string.IsNullOrEmpty(tableName))
                tableName = typeof(T).Name;

            this.Table = tableName;

            // assign file paths
            dataFile = Path.Combine(basePath, String.Format(@"{0}.sdf", tableName));

            if (!File.Exists(dataFile))
            {
                var engine = new SqlCeEngine(ConnectionString);
                engine.CreateDatabase();
                CreateTable();
            }
        }


        public string Table { get; set; }

        #region Helper methods

        private void CreateTable()
        {
            var sqlSB = new StringBuilder();
            sqlSB.AppendFormat("CREATE TABLE [{0}] ( ", Table)
                      .Append("ID int NOT NULL IDENTITY(1, 1) PRIMARY KEY,")
                      .Append("Data image NULL )");
            var sql = sqlSB.ToString();
            ExecQuery(sql);
        }

        private string ConnectionString
        {
            get
            {
                return "Data Source=" + dataFile;
            }
        }

        private SqlCeConnection Connection
        {
            get
            {
                if (sqlCnn == null)
                {
                    sqlCnn = new SqlCeConnection(ConnectionString);
                }
                return sqlCnn;
            }
        }

        private SqlCeCommand Command(string commandText)
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();

            return new SqlCeCommand(commandText, Connection);
        }

        private SqlCeDataReader ExecDataReader(string commandText)
        {
            return Command(commandText).ExecuteReader(CommandBehavior.CloseConnection);
        }

        private SqlCeResultSet ExecResultSet(string commandText)
        {
            return Command(commandText).ExecuteResultSet(ResultSetOptions.Scrollable);
        }

        private object ExecScalar(string commandText)
        {
            return Command(commandText).ExecuteScalar();
        }

        private int ExecQuery(string commandText)
        {
            return Command(commandText).ExecuteNonQuery();
        }

        private T Map(SqlCeDataReader reader)
        {
            var id = (int)reader["ID"];
            var data = (byte[])reader["Data"];
            var serializer = new BSonSerializer();
            var obj = serializer.Deserialize<T>(data);
            SetID(obj, id);
            return obj;
        }

        private IEnumerable<T> Map(SqlCeResultSet resultSet)
        {
            var list = new List<T>();

            while (resultSet.Read())
            {
                list.Add(Map((SqlCeDataReader)resultSet));
            }
            resultSet.Close();
            return list;
        }

        private PropertyInfo GetIDProp()
        {
            var type = typeof(T);
            var idProp = type.GetProperty("id");

            if (idProp == null)
                idProp = type.GetProperty("ID");

            if (idProp == null)
                idProp = type.GetProperty("Id");

            return idProp;

        }

        private void SetID(T entity, int id)
        {
            var idProp = GetIDProp();
            if (idProp != null)
                idProp.SetValue(entity, id, null);
        }

        private int GetID(T entity)
        {
            var idProp = GetIDProp();

            if (idProp != null)
                return Convert.ToInt32(idProp.GetValue(entity, null));

            return 0;
        }

        #endregion

        public void Dispose()
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }

        public IQueryable<T> All()
        {
            var resultSet = ExecResultSet(string.Format("SELECT * FROM [{0}]", Table));
            return Map(resultSet).AsQueryable();
        }

        public IQueryable<T> All(out int total, int index = 0, int size = 50)
        {
            total = this.Count();
            var skips = index * size;
            var sql = index == 0 ? string.Format(@"SELECT TOP {0} * FROM [{1}]", size, Table) :
                string.Format(@"SELECT TOP({1}) * FROM [{0}] WHERE [ID] NOT IN (SELECT TOP({2}) [ID] FROM [{0}])", Table, size, skips);

            return Map(ExecResultSet(sql)).AsQueryable();
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

        public int Count()
        {
            return (int)ExecScalar(string.Format("SELECT COUNT(ID) FROM [{0}]", this.Table));
        }

        public int Count(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().Count(predicate.Compile());
        }

        public T Find(params object[] keys)
        {
            var reader = ExecDataReader(string.Format("SELECT * FROM [{0}] WHERE ID={1}", this.Table, keys[0]));
            reader.Read();
            var result= Map(reader);
            reader.Close();
            return result;
        }

        public T Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return All().FirstOrDefault(predicate.Compile());
        }

        public T Create(T t)
        {
            var sql = string.Format(@"INSERT INTO [{0}] ([Data]) Values(@Data)", Table);

            var cmd = Command(sql);
            var serializer = new BSonSerializer();
            var binaryData = serializer.Serialize(t);
            var dataParam = new SqlCeParameter("@Data", binaryData);
            dataParam.SqlDbType = SqlDbType.Image;
            cmd.Parameters.Add(dataParam);
            cmd.ExecuteNonQuery();

            var reader = ExecDataReader(string.Format("SELECT * FROM [{0}] WHERE ([ID] = @@IDENTITY)", Table));
            reader.Read();
            var ob= Map(reader);
            reader.Close();

            var id = GetID(ob);
            SetID(t, id);

            return ob;
        }

        public void Delete(T t)
        {
            var id = GetID(t);
            ExecQuery(string.Format("DELETE FROM [{0}] WHERE ID={1}", this.Table, id));
        }

        public int Delete(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            var vals = Filter(predicate).Select(p => GetID(p)).ToArray();
            return ExecQuery(string.Format("DELETE FROM [{0}] WHERE ID in ({1})", this.Table, string.Join(",", vals)));

        }

        public T Update(T t)
        {
            var id = GetID(t);
            var sql = string.Format("UPDATE [{0}] SET [DATA]=@Data WHERE [ID]={1}", Table, id);
            var cmd = Command(sql);
            var serializer = new BSonSerializer();
            var binaryData = serializer.Serialize(t);
            var dataParam = new SqlCeParameter("@Data", binaryData);
            dataParam.SqlDbType = SqlDbType.Image;
            cmd.Parameters.Add(dataParam);
            cmd.ExecuteNonQuery();
            return t;
        }

        public void Clear()
        {
            ExecQuery(string.Format("DROP TABLE [{0}]", this.Table));
            CreateTable();
        }

        public int Submit()
        {
            return 1;
            //  throw new NotImplementedException();
        }
    }
}
