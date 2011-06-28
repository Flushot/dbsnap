using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized;

namespace DbSnap.Util
{
    /// <summary>
    /// 
    /// </summary>
    public class ScriptWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly ScriptingOptions _options;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="options"></param>
        public ScriptWriter(TextWriter writer, ScriptingOptions options)
        {
            _writer = writer;
            _options = options;
        }

        ~ScriptWriter()
        {
            if (_writer != null)
                _writer.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wrapper"></param>
        public void WriteDefinition(SmoWrapper wrapper)
        {
            Console.WriteLine("[{0}] {1}", wrapper.SmoObject.GetType().Name, wrapper.QualifiedName);
            WriteDefinition((IScriptable)wrapper.SmoObject);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptable"></param>
        public void WriteDefinition(IScriptable scriptable)
        {
            WriteScript(scriptable.Script(_options));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        public void WriteScript(StringCollection lines)
        {
            foreach (String line in lines)
                _writer.WriteLine(line);
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteHeader()
        {
            WriteComment();
            WriteComment(String.Format("Exported by DbSnap on {0:MM/dd/yy} at {0:t}", DateTime.Now));
            WriteComment();
            _writer.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comment"></param>
        public void WriteBlockTitle(String title)
        {
            _writer.WriteLine();
            _writer.WriteLine(String.Format("--- {0} ---", title));
        }

        public void WriteComment()
        {
            _writer.WriteLine("--");
        }
        public void WriteComment(String comment)
        {
            _writer.WriteLine(String.Format("-- {0}", comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wrapper"></param>
        public void WriteExtendedProperties(SmoWrapper wrapper)
        {
            ExtendedPropertyCollection extendedProperties = wrapper.ExtendedProperties;
            if (extendedProperties != null && extendedProperties.Count > 0)
            {
                WriteBlockTitle("Extended Properties");
                foreach (ExtendedProperty prop in extendedProperties)
                    WriteDefinition(prop);
            }
        }

        #region IDisposable Members

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_writer != null)
                    _writer.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
