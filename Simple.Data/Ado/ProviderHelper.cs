﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.IO;

namespace Simple.Data.Ado
{
    class ProviderHelper
    {
        private static readonly ConcurrentDictionary<string, IConnectionProvider> Cache = new ConcurrentDictionary<string,IConnectionProvider>();

        public static IConnectionProvider GetProviderByConnectionString(string connectionString)
        {
            return new SqlConnectionProvider(connectionString);
        }

        public static IConnectionProvider GetProviderByFilename(string filename)
        {
            return Cache.GetOrAdd(filename, LoadProvider);
        }

        private static IConnectionProvider LoadProvider(string filename)
        {
            string extension = GetFileExtension(filename);

            var provider = ComposeProvider(extension);

            provider.SetConnectionString(string.Format("data source={0}", filename));
            return provider;
        }

        private static string GetFileExtension(string filename)
        {
            var extension = Path.GetExtension(filename);;

            if (extension == null) throw new ArgumentException("Unrecognised file.");
            return extension.TrimStart('.').ToLower();
        }

        private static IConnectionProvider ComposeProvider(string extension)
        {
            using (var container = CreateContainer())
            {
                var export = container.GetExport<IConnectionProvider>(extension);
                if (export == null) throw new ArgumentException("Unrecognised file.");
                return export.Value;
            }
        }

        private static CompositionContainer CreateContainer()
        {
            var path = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
            path = Path.GetDirectoryName(path);
            if (path == null) throw new ArgumentException("Unrecognised file.");

            var catalog = new DirectoryCatalog(path, "Simple.Data.*.dll");
            return new CompositionContainer(catalog);
        }
    }
}
