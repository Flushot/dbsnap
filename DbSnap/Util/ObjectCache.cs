using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using System.Data.SqlClient;

namespace DbSnap.Util
{
    public class ObjectCache
    {
        public class CachedObject
        {
            public int Id { get; private set; }
            public String Name { get; private set; }
            public String TypeStr { get; private set; }
            public Type Type { get; private set; }

            public CachedObject(int id, String name, String typeStr)
            {
                Id = id;
                Name = name;
                TypeStr = typeStr;
                Type = DatabaseUtils.FromSqlType(TypeStr);
            }

            public String Label
            {
                get
                {
                    switch (TypeStr)
                    {
                        case "FN":
                        case "TF": return "Functions";
                        case "P": return "Stored Procedures";
                        case "V": return "Views";
                        case "U": return "Tables";
                        default: throw new ArgumentException(TypeStr);
                    }
                }
            }

            public SqlSmoObject Activate(Database database)
            {
                switch (TypeStr)
                {
                    case "FN":
                    case "TF": return database.UserDefinedFunctions.ItemById(Id);
                    case "P": return database.StoredProcedures.ItemById(Id);
                    case "V": return database.Views.ItemById(Id);
                    case "U": return database.Tables.ItemById(Id);
                    default: throw new ArgumentException(TypeStr);
                }
            }
        }

        private List<CachedObject> _cache = new List<CachedObject>();
        private int _cacheIndex = 0;

        public ObjectCache(Server server, Database database)
        {
            // Populate cache
            SqlConnection conn = new SqlConnection(server.ConnectionContext.ConnectionString);
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    String.Concat(
                        "select object_id, name, type from [", database.Name, "].sys.objects ",
                        "where is_ms_shipped = 0 and type in ('U', 'V', 'P', 'TF', 'FN') ",
                        "order by type, name"), conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _cache.Add(
                            new CachedObject(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2).Trim()));
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public int Count
        {
            get
            {
                int count = _cache.Count;
                return count;
            }
        }

        public void Reset()
        {
            _cacheIndex = 0;
        }

        public CachedObject GetNext()
        {
            lock (_cache)
            {
                if (_cacheIndex >= _cache.Count)
                {
                    // Cache has no more remaining items.
                    return null;
                }

                return _cache[_cacheIndex++];
            }
        }
    }
}
