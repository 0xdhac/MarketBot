using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;

namespace MarketBot
{
	public static class Blacklist
	{
		private const string FileFormat = "{exchange}_bl.csv";
		private const string Folder = "cfg";

		public static bool AddSymbol(Exchanges exchange, string symbol)
		{
			string file = FileFormat.Replace("{exchange}", exchange.ToString());

			string path = $"./{Folder}/{FileFormat}";

			// Append to the file.
			CsvConfiguration config;
			if (File.Exists(path))
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					// Don't write the header again.
					HasHeaderRecord = false
				};
			}
			else
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					// Don't write the header again.
					HasHeaderRecord = true
				};
			}

			using (var writer = new StreamWriter(path))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecord(symbol);
			}

			return false;
		}

		public static bool RemoveSymbol(Exchanges exchange, string symbol)
		{
			string file = FileFormat.Replace("{exchange}", exchange.ToString());

			string path = $"./{Folder}/{FileFormat}";

			// Append to the file.
			CsvConfiguration config;
			if (File.Exists(path))
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					// Don't write the header again.
					HasHeaderRecord = false
				};
			}
			else
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					// Don't write the header again.
					HasHeaderRecord = true
				};
			}

			using (var writer = new StreamWriter(path))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecord(symbol);
			}

			return false;
		}
	}
}
