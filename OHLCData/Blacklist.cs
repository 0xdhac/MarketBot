using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;
using MarketBot.tools;

namespace MarketBot
{
	public class BlacklistItem
	{
		public string Symbol { get; set; }

		public BlacklistItem(string symbol)
		{
			Symbol = symbol;
		}
	}
	public static class Blacklist
	{
		private const string FileFormat = "{exchange}_bl.csv";
		private const string Folder = "cfg";

		public static bool AddSymbol(Exchanges exchange, string symbol)
		{
			if(IsBlacklisted(exchange, symbol))
			{
				return false;
			}

			string file = FileFormat.Replace("{exchange}", exchange.ToString());

			string path = $"./{Folder}/{file}";

			CsvConfiguration config;
			if (File.Exists(path))
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					// Don't write the header again.
					HasHeaderRecord = false,
				};
			}
			else
			{
				config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					HasHeaderRecord = true
				};
			}

			List<BlacklistItem> ls = new List<BlacklistItem>() { new BlacklistItem(symbol) };
			using(var stream = File.Open(path, FileMode.Append))
			using (var writer = new StreamWriter(stream))
			using (var csv = new CsvWriter(writer, config))
			{
				csv.WriteRecords(ls);
			}

			// Update RealTimeBot
			foreach(var dict in RealtimeBot.TradingPairs)
			{
				if(dict.Key == exchange)
				{
					foreach(var pair in dict.Value)
					{
						if(pair.Key == symbol)
						{
							dict.Value.Remove(pair.Key);

							dict.Value.Add(symbol, true);
						}
					}
				}
			}

			return true;
		}

		public static bool RemoveSymbol(Exchanges exchange, string symbol)
		{
			bool found = false;
			string file = FileFormat.Replace("{exchange}", exchange.ToString());

			string path = $"./{Folder}/{file}";

			if (!File.Exists(path))
				return false;

			CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = false
			};

			List<BlacklistItem> records;
			using (var reader = new StreamReader(path))
			using (var csv = new CsvReader(reader, config))
			{
				records = csv.GetRecords<BlacklistItem>().ToList();
			}

			foreach(var record in records)
			{
				if(record.Symbol == symbol)
				{
					records.Remove(record);
					found = true;
					break;
				}
			}

			using (var writer = new StreamWriter(path))
			using (var csv = new CsvWriter(writer, config))
			{
				csv.WriteRecords(records);
			}

			// Update RealTimeBot
			foreach (var dict in RealtimeBot.TradingPairs)
			{
				if (dict.Key == exchange)
				{
					foreach (var pair in dict.Value)
					{
						if (pair.Key == symbol)
						{
							dict.Value.Remove(pair.Key);

							dict.Value.Add(symbol, false);
						}
					}
				}
			}

			return found;
		}

		public static bool IsBlacklisted(Exchanges exchange, string symbol)
		{
			bool found = false;
			string file = FileFormat.Replace("{exchange}", exchange.ToString());

			string path = $"./{Folder}/{file}";

			if (!File.Exists(path))
				return false;

			CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = false,
			};

			List<BlacklistItem> records = new List<BlacklistItem>() { new BlacklistItem(symbol) };
			using (var reader = new StreamReader(path))
			using (var csv = new CsvReader(reader, config))
			{
				records = csv.GetRecords<BlacklistItem>().ToList();
			}

			foreach (var record in records)
			{
				if (record.Symbol == symbol)
				{
					return true;
				}
			}

			return found;
		}
	}
}
