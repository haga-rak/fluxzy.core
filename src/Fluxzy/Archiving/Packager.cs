// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Har;
using Fluxzy.Saz;

namespace Fluxzy
{
    public static class Packager
    {
        /// <summary>
        /// Export a dump directory as HttpArchive
        /// </summary>
        /// <param name="directory">Dump directory</param>
        /// <param name="filePath">The outputed archive file</param>
        /// <param name="savingSetting">Save settings</param>
        public static void ExportAsHttpArchive(string directory, string filePath, HttpArchiveSavingSetting? savingSetting = null)
        {
            var packager = new HttpArchivePackager(savingSetting);

            using var fileStream = File.Create(filePath);

            Task.Run(async () => await packager.Pack(directory, fileStream, null)).GetAwaiter().GetResult();
        }

		/// <summary>
		/// Export a dump directory to a fluxzy file.
		/// This is the recommended file format as it can holds raw capture datas. 
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="filePath"></param>
		public static void Export(string directory, string filePath)
		{
			var packager = new FxzyDirectoryPackager();

			using var fileStream = File.Create(filePath);
			Task.Run(async () => await packager.Pack(directory, fileStream, null)).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Export a dump directory to a saz file. This is an experimental feature. 
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="filePath"></param>
		[Obsolete]
		public static void ExportAsSaz(string directory, string filePath)
		{
			var packager = new SazPackager();

			using var fileStream = File.Create(filePath);
			Task.Run(async () => await packager.Pack(directory, fileStream, null)).GetAwaiter().GetResult();
		}
    }
}
