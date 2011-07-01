using System;
using System.IO;
using DbSnap.Util;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Threading;

namespace DbSnap
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DbSnap v{0} by Chris Lyon", 
                Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine();

            if (args.Length < 3)
            {
                Console.WriteLine(
                    "Usage: {0} <instance> <database> <zipfile>",
                    AppDomain.CurrentDomain.FriendlyName);
                return;
            }

            String serverName = args[0];
            Server server = new Server(serverName);
            String databaseName = args[1];
            Database database = null;
            try
            {
                database = server.Databases[databaseName];
                if (database == null)
                {
                    Console.Error.WriteLine(
                        "Database \"{0}\" is unavailable or doesn't exist on server \"{1}\"", 
                        databaseName, serverName);
                    return;
                }
            }
            catch (ConnectionFailureException ex)
            {
                Console.Error.WriteLine(
                    "Error connecting to server \"{0}\": {1}",
                    serverName, ex.Message);
                return;
            }

            String zipFile = args[2];

            Console.WriteLine(
                "Creating snapshot of database \"{0}\" on server \"{1}\" into \"{2}\"...",
                databaseName, serverName, zipFile);

            DatabaseExporter exporter = new DatabaseExporter(server, database,
                new ScriptingOptions
                {
                    //ScriptDrops = true,
                    //IncludeIfNotExists = true,
                    Indexes = true,
                    XmlIndexes = true,
                    FullTextIndexes = true,
                    FullTextCatalogs = true,
                    Triggers = true,
                    Default = true,
                    Permissions = true,
                    ExtendedProperties = true,
                    DriAll = true
                });

            ThreadPool.SetMaxThreads(8, 1000);

            DateTime start = DateTime.Now;
            exporter.SaveZip(zipFile);
            Console.WriteLine("Completed in {0} seconds.", DateTime.Now.Subtract(start).TotalSeconds);

#if DEBUG
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
#endif
        }
    }
}
