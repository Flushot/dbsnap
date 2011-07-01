using System;
using System.IO;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using DbSnap.Util;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace DbSnap.Util
{
    /// <summary>
    /// Exports database structure
    /// </summary>
    public class DatabaseExporter
    {
        protected delegate bool SaveAllowedDelegate(SmoWrapper obj);

        /// <summary>
        /// Schemas that should be ignored
        /// </summary>
        private static readonly String[] _ignoredSchemas =
        {
            "db_accessadmin", "db_backupoperator", "db_datareader", "db_datawriter",
            "db_ddladmin", "db_denydatareader", "db_denydatawriter", "db_owner",
            "db_securityadmin", "dbo", "guest", "INFORMATION_SCHEMA", "sys"
        };

        /// <summary>
        /// Database roles that should be ignored
        /// </summary>
        private static readonly String[] _ignoredRoles =
        {
            "db_accessadmin", "db_backupoperator", "db_datareader", "db_datawriter",
            "db_ddladmin", "db_denydatareader", "db_denydatawriter", "db_owner",
            "db_securityadmin", "public"
        };

        /// <summary>
        /// Users that should be ignored
        /// </summary>
        private static readonly String[] _ignoredUsers =
        {
            "dbo", "guest", "sys", "INFORMATION_SCHEMA"
        };

        private readonly Server _server;
        private readonly Database _database;
        private readonly ScriptingOptions _options;

        /// <summary>
        /// Database server
        /// </summary>
        public Server Server { get { return _server; } }

        /// <summary>
        /// Database
        /// </summary>
        public Database Database { get { return _database; } }

        /// <summary>
        /// Creates an exporter for a given database.
        /// </summary>
        /// <param name="server">Database server</param>
        /// <param name="database">Database</param>
        public DatabaseExporter(Server server, Database database, ScriptingOptions options)
        {
            _server = server;
            _database = database;
            _options = options;
        }

        /// <summary>
        /// Exports structure as a ZIP file.
        /// If a file already exists, it will be overwritten.
        /// </summary>
        /// <param name="zipFile">File name to create</param>
        public void SaveZip(String fileName)
        {
            String tempPath = Path.Combine(Path.GetTempPath(),
                "DbSnap_" + Guid.NewGuid().ToString());

            try
            {
                SaveFolder(tempPath);

                FileStream fileOut = File.Create(fileName);
                using (ZipOutputStream zipOut = new ZipOutputStream(fileOut))
                {
                    zipOut.SetLevel(9); // 0-9, 9 being the highest level of compression
                    ZipFolder(zipOut, tempPath, tempPath);
                    zipOut.IsStreamOwner = true; // Close underlying stream
                    zipOut.Close();
                }
            }
            finally
            {
                // Clean up
                Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Recursively zips a folder.
        /// </summary>
        /// <param name="zipOut">ZipOutputStream to write folder contents to</param>
        /// <param name="basePath">Initial folder</param>
        /// <param name="folder">Subfolder (should be the same as initial folder if this is the first call)</param>
        protected void ZipFolder(ZipOutputStream zipOut, String basePath, String folder)
        {
            // Add files
            String[] files = Directory.GetFiles(folder);
            foreach (String filename in files)
            {
                FileInfo fi = new FileInfo(filename);
                String relative = PathUtils.GetRelativePath(filename, basePath);

                // Begin entry
                zipOut.PutNextEntry(
                    new ZipEntry(ZipEntry.CleanName(relative))
                    {
                        DateTime = fi.LastWriteTime,
                        Size = fi.Length
                    }
                );

                // Write data (in 4k chunks)
                byte[] buffer = new byte[4096];
                using (FileStream reader = File.OpenRead(filename))
                    StreamUtils.Copy(reader, zipOut, buffer);

                // End entry
                zipOut.CloseEntry();
            }

            // Traverse subdirectories
            String[] directories = Directory.GetDirectories(folder);
            foreach (String directory in directories)
                ZipFolder(zipOut, basePath, directory);
        }

        private void InitServer(Server server)
        {
            // Speed up queries by including these important fields
            _server.SetDefaultInitFields(typeof(StoredProcedure), "IsSystemObject");
            _server.SetDefaultInitFields(typeof(Table), "IsSystemObject");
            _server.SetDefaultInitFields(typeof(View), "IsSystemObject");
            _server.SetDefaultInitFields(typeof(DatabaseDdlTrigger), "IsSystemObject");
        }

        /// <summary>
        /// Exports structure to a folder (not zipped.)
        /// If the folder already exists, existing files/folders won't be deleted.
        /// </summary>
        /// <param name="basePath">Folder to save to</param>
        public void SaveFolder(String folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Multithreaded
            ObjectCache cache = new ObjectCache(_server, _database);
            int workerCount = cache.Count;
            ManualResetEvent finishedEvent = new ManualResetEvent(false);

            int maxWorkers, maxIOCP;
            ThreadPool.GetMaxThreads(out maxWorkers, out maxIOCP);
            Console.WriteLine("Queueing {0} workers using {1} threads...", workerCount, maxWorkers);
            if (workerCount == 0)
            {
                finishedEvent.Set();
            }
            else
            {
                for (int i = 0; i < workerCount; ++i)
                {
                    ThreadPool.QueueUserWorkItem(delegate(Object state)
                    {
                        SaveCachedObject(folder, cache.GetNext());

                        Interlocked.Decrement(ref workerCount);
                        if (workerCount == 0)
                            finishedEvent.Set();
                    });
                }
            }

            InitServer(_server);
            SaveObjects(folder, "Schemas", _database.Schemas,
                delegate(SmoWrapper obj)
                {
                    return !StringUtils.IsInArray(obj.Name, _ignoredSchemas, true);
                });
            SaveObjects(folder, "XML Schemas", _database.XmlSchemaCollections);
            SaveObjects(folder, "Users", _database.Users,
                delegate(SmoWrapper obj)
                {
                    return !StringUtils.IsInArray(obj.Name, _ignoredUsers, true);
                });
            SaveObjects(folder, "Roles", _database.Roles,
                delegate(SmoWrapper obj)
                {
                    return !StringUtils.IsInArray(obj.Name, _ignoredRoles, true);
                });
            SaveObjects(folder, "Assemblies", _database.Assemblies);
            SaveObjects(folder, "Types", _database.UserDefinedTypes);
            SaveObjects(folder, "Data Types", _database.UserDefinedDataTypes);
            SaveObjects(folder, "Aggregates", _database.UserDefinedAggregates);
            SaveObjects(folder, "Synonyms", _database.Synonyms);
            SaveObjects(folder, "Partition Functions", _database.PartitionFunctions);
            SaveObjects(folder, "Partition Schemes", _database.PartitionSchemes);
            SaveObjects(folder, "Rules", _database.Rules);
            SaveObjects(folder, "Triggers", _database.Triggers);

            finishedEvent.WaitOne(Timeout.Infinite, true);
        }

        /// <summary>
        /// Saves all objects in database as *.sql scripts in subdirectories
        /// organized by object type.
        /// </summary>
        /// <param name="basePath">Base directory to save scripts to.</param>
        /// <param name="label">Label of subfolder.</param>
        /// <param name="collection">Collection of SMOs to save.</param>
        protected void SaveObjects(String basePath, String label, SmoCollectionBase collection)
        {
            SaveObjects(basePath, label, collection, null);
        }

        /// <summary>
        /// Saves all objects in database as *.sql scripts in subdirectories
        /// organized by object type.
        /// </summary>
        /// <param name="basePath">Base directory to save scripts to.</param>
        /// <param name="label">Label of subfolder.</param>
        /// <param name="collection">Collection of SMOs to save.</param>
        /// <param name="extendedSave">Optional custom save delegate (if not null, callee is completely responsible for saving.)</param>
        protected void SaveObjects(String basePath, String label, 
            SmoCollectionBase collection, SaveAllowedDelegate saveAllowed)
        {
            String folder = Path.Combine(basePath, label);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            foreach (SqlSmoObject obj in collection)
            {
                SmoWrapper wrapper = new SmoWrapper(obj);
                if (wrapper.IsSystemObject)
                    continue;

                if (saveAllowed == null || (saveAllowed != null && saveAllowed(wrapper)))
                {
                    using (ScriptWriter scriptWriter = new ScriptWriter(
                            new StreamWriter(
                                Path.Combine(folder, String.Format("{0}.sql",
                                StringUtils.NormalizeFilename(
                                    wrapper.QualifiedName)))), _options))
                    {
                        scriptWriter.WriteDefinition(wrapper);
                    }
                }
            }
        }

        protected void SaveCachedObject(String basePath, ObjectCache.CachedObject cachedObj)
        {
            Server server = new Server(_server.Name);
            InitServer(server);

            Database database = server.Databases[_database.Name];

            String folder = Path.Combine(basePath, cachedObj.Label);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            SqlSmoObject obj = cachedObj.Activate(database);
            SmoWrapper wrapper = new SmoWrapper(obj);
            if (wrapper.IsSystemObject)
                return;

            using (ScriptWriter scriptWriter = new ScriptWriter(
                    new StreamWriter(
                        Path.Combine(folder, String.Format("{0}.sql",
                        StringUtils.NormalizeFilename(
                            wrapper.QualifiedName)))), _options))
            {
                scriptWriter.WriteDefinition(wrapper);
            }
        }
    }
}
