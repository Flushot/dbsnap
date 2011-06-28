using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;

namespace DbSnap.Util
{
    public static class DatabaseUtils
    {
        public static Type FromSqlType(String sqlType)
        {
            switch (sqlType)
            {
                case "FN":
                case "TF":
                    return typeof(UserDefinedFunction);
                case "P":
                    return typeof(StoredProcedure);
                case "V":
                    return typeof(View);
                case "U":
                    return typeof(Table);
                default:
                    throw new ArgumentException(sqlType);
            }
        }
    }
}
