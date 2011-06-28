using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using System.Reflection;

namespace DbSnap.Util
{
    /// <summary>
    /// 
    /// </summary>
    public class SmoWrapper
    {
        private readonly SqlSmoObject _obj;
        private readonly String _schema, _name;
        private readonly bool _hasSchema;

        /// <summary>
        /// 
        /// </summary>
        public SqlSmoObject SmoObject { get { return _obj; } }

        /// <summary>
        /// 
        /// </summary>
        public String Schema { get { return _schema; } }

        /// <summary>
        /// 
        /// </summary>
        public String Name { get { return _name; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public SmoWrapper(SqlSmoObject obj)
        {
            _obj = obj;
            _name = ((ScriptNameObjectBase)SmoObject).Name;
            if (typeof(ScriptSchemaObjectBase).IsAssignableFrom(SmoObject.GetType()))
            {
                _hasSchema = true;
                _schema = ((ScriptSchemaObjectBase)SmoObject).Schema;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static T GetPropertyValue<T>(Object obj, String name)
        {
            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in props)
                if (pi.Name == name)
                    return (T)pi.GetValue(obj, null);

            return default(T);
        }

        private bool? _isSystemObject;

        /// <summary>
        /// 
        /// </summary>
        public bool IsSystemObject
        {
            get
            {
                if (!_isSystemObject.HasValue)
                    _isSystemObject = GetPropertyValue<bool>(
                        SmoObject, "IsSystemObject");

                return _isSystemObject.Value;
            }
        }

        private ExtendedPropertyCollection _extendedProperties;

        /// <summary>
        /// 
        /// </summary>
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                if (_extendedProperties == null)
                    _extendedProperties = GetPropertyValue<ExtendedPropertyCollection>(
                        SmoObject, "ExtendedProperties");

                return _extendedProperties;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public String QualifiedName
        {
            get
            {
                if (_hasSchema)
                    return String.Format("{0}.{1}", Schema, Name);
                else
                    return Name;
            }
        }

        public override string ToString()
        {
            return QualifiedName;
        }
    }
}
