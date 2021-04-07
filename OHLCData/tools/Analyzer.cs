using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.IO;
using MarketBot.exchanges.binance;
using Ionic.Zip;
using MarketBot.exchanges.localhost;

namespace MarketBot.tools
{
	static class BinanceAnalyzer
	{
		/*
		 * The goal of this class is to find out what the best strategies, risk/reward ratios, bet resizing modes
		 * 
		 */

		// Download folder for symbol data
		private static string DownloadPath = Program.GetConfigSetting("KLINE_DATA_FOLDER");//@"./klines/";

		// Url building vars
		private static string BaseUrl = @"https://data.binance.vision/data";
		private static string Account = @"spot";
		private static string DateRange = @"monthly";
		private static string DataType = @"klines";
		private static HList<OHLCVPeriod> Market = null;
		private static List<Replay> Replays = new List<Replay>();
		private const int MinPeriods = 1600;
		private static Balance Balance = new Balance()
		{
			Starting = 10000,
			Available = 10000,
			Total = 10000
		};

		private static BetSizer BetSizer = new BetSizer()
		{
			BetPercent = (decimal)0.15,
			ResizeEvery = 1,
			Balance = Balance
		};

		// Analyzer settings
		private static List<string> Pairs = null;

		public static void LoadMarket(OHLCVInterval interval)
		{
			var file = BuildFile("BTCUSDT", interval, DateTime.Now.AddMonths(-1));
			new SymbolData("BTCUSDT", new string[] { DownloadPath + file + ".csv" }, interval, CSVConversionMethod.BinanceVision, MarketLoaded);
		}

		public static void MarketLoaded(SymbolData data)
		{
			Market = data.Data.Periods;
			PrintBetas(OHLCVInterval.ThirtyMinute);
		}

		public static void PrintBetas(OHLCVInterval interval)
		{
			foreach(var pair in Pairs)
			{
				var file = DownloadPath + BuildFile(pair, interval, DateTime.Now.AddMonths(-1)) + ".csv";
				if (!File.Exists(file))
					continue;

				SymbolData sym = new SymbolData(pair, new string[] { file }, interval, CSVConversionMethod.BinanceVision, ThrowAwayCallback);

				if (sym.Data.Periods.Count > 0)
				{
					try
					{
						var beta = Skender.Stock.Indicators.Indicator.GetBeta(Market, sym.Data.Periods, 100);

						var bl = beta.ToList();
						Console.WriteLine($"{pair}: {bl[bl.Count - 1].Beta}");
					}
					catch(Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
			}
		}

		private static void ThrowAwayCallback(SymbolData obj)
		{
			throw new NotImplementedException();
		}

		public static void RunReplays(string pattern, OHLCVInterval interval)
		{
			if (Pairs == null)
				Pairs = BinanceMarket.GetTradingPairs(pattern, true);

			// Load kline data into new replay objects
			Program.Print("Loading replay kline data..");
			foreach(var pair in Pairs)
			{
				if (Blacklist.IsBlacklisted(Exchanges.Binance, pair))
					continue;

				var files = BuildFileList(pair, interval, DateTime.UtcNow.AddMonths(-13), DateTime.UtcNow.AddMonths(-1));
				//var files = BuildFileList(pair, interval, new DateTime(2017, 7, 1), new DateTime(2018, 12, 1));

				var replay = new Replay(pair, files, interval);
				if(replay.Symbol.Periods < MinPeriods)
				{
					continue;
				}
				
				Console.Write(".");

				Replays.Add(replay);
			}

			Console.WriteLine();

			// Apply indicators/strategies
			Program.Print("Applying indicators..");
			foreach(var replay in Replays)
			{
				replay.Balance = Balance;
				replay.BetSizer = BetSizer;
				replay.BetSizer.UpdateBetAmount();
				replay.SetupStrategies();
				replay.OnReplayFinished += ReplayFinished;
				Console.Write(".");
			}
			Console.WriteLine();

			Program.Print("Running replays..");
			while(Replays.TrueForAll(r => r.Finished) == false)
			{
				foreach (var replay in Replays)
				{
					if (replay.Finished)
						continue;

					replay.RunNextPeriod();
				}
			}

			Console.WriteLine();
			Console.WriteLine($"Starting amount: ${Balance.Starting:.00}, Ending amount: ${Balance.Total:.00}");
			Console.WriteLine($"GOA: {(Balance.Total - Balance.Starting) / (Balance.Starting) * 100:.00}%");

			var results = Replays.Select(r => r.Results()).ToList();
			foreach(var result in results)
			{
				//if (!result.Profitability.HasValue || result.Profitability < (decimal)1.3)
				//	Console.WriteLine(result.Symbol);
				//Console.WriteLine($"[{result.Symbol}] Profitability: {result.Profitability:.00}");
			}
			/*
			foreach (var replay in Replays)
			{
				var result = replay.Results();

				if (result.Profitability < (decimal)1.3)
					Console.WriteLine($"{replay.Symbol.Symbol}");
			}
			*/
		}

		private static void ReplayFinished(object sender, EventArgs e)
		{
			Console.Write(".");
		}

		public static string[] BuildFileList(string symbol, OHLCVInterval interval, DateTime start, DateTime end)
		{
			var path = DownloadPath;
			//{end_date.Year}-{end_date.Month:00}";

			List<string> files = new List<string>();
			DateTime date = start;
			bool FoundOneFile = false;
			while(date <= end)
			{
				var file = DownloadPath + BuildFile(symbol, interval, date) + ".csv";
				date = date.AddMonths(1);

				if (!File.Exists(file)) // If file doesn't exist
				{
					if (FoundOneFile) // If a file has previously been added to the list and now we suddenly have a file that doesn't exist, end the function to prevent gaps in kline data
					{
						break;
					}
					else
					{
						continue;
					}
				}
				

				FoundOneFile = true;

				files.Add(file);
			}

			return files.ToArray();
		}

		// Download candle stick data from data.binance.vision
		public static void DownloadKlines(string pattern, OHLCVInterval interval)
		{
			if(Pairs == null)
			{
				Pairs = BinanceMarket.GetTradingPairs(pattern, true);
			}

			Program.Print($"Downloading kline data for all symbols matching pattern {pattern}");
			foreach (var pair in Pairs)
			{
				
				var list = DownloadZipFiles(pair, interval);
				foreach(var file in list)
					Extract(file);
				Console.Write(".");
			}
			Console.WriteLine();
		}

		public static List<List<byte>> DownloadZipFiles(string symbol, OHLCVInterval interval)
		{
			// Get current year/month/firstday of month
			var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
			var url = BuildUrl(symbol, interval);
			List<List<byte>> output = new List<List<byte>>();

			try
			{
				while (true)
				{
					date = date.AddMonths(-1);
					var file = BuildFile(symbol, interval, date);
					var file_with_path = DownloadPath + file + ".csv";
					
					if (!File.Exists(file_with_path))
					{
						using (var client = new WebClient())
						{
							byte[] data = client.DownloadData(url + file + ".zip");
							output.Add(data.ToList());
						}
					}
				}
			}
			catch (Exception)
			{
				return output;
			}
		}

		// Grab information about the coin; 24h volume, age of coin, etc..
		public static void DownloadMetadata(string pattern, List<List<byte>> output)
		{

		}

		private static string BuildUrl(string symbol, OHLCVInterval interval)
		{
			string interval_param = GetKlineInterval(interval);

			return $"{BaseUrl}/{Account}/{DateRange}/{DataType}/{symbol}/{interval_param}/";
		}

		private static string BuildFile(string symbol, OHLCVInterval interval, DateTime end_date)
		{
			//<symbol_in_uppercase>-<interval>-<year>-<month>.zip
			string uppercase_symbol = symbol.ToUpper();
			string interval_param = GetKlineInterval(interval);

			return $"{uppercase_symbol}-{interval_param}-{end_date.Year}-{end_date.Month:00}";
		}

		public static string GetKlineInterval(OHLCVInterval interval)
		{
			switch (interval)
			{
				case OHLCVInterval.TwelveHour:
					return "12h";
				case OHLCVInterval.FifteenMinute:
					return "15m";
				case OHLCVInterval.OneDay:
					return "1d";
				case OHLCVInterval.OneHour:
					return "1h";
				case OHLCVInterval.OneMinute:
					return "1m";
				case OHLCVInterval.OneMonth:
					return "1mo";
				case OHLCVInterval.OneWeek:
					return "1w";
				case OHLCVInterval.TwoHour:
					return "2h";
				case OHLCVInterval.ThirtyMinute:
					return "30m";
				case OHLCVInterval.ThreeDay:
					return "3d";
				case OHLCVInterval.ThreeMinute:
					return "3m";
				case OHLCVInterval.FourHour:
					return "4h";
				case OHLCVInterval.FiveMinute:
					return "5m";
				case OHLCVInterval.SixHour:
					return "6h";
				case OHLCVInterval.EightHour:
					return "8h";
				default:
					throw new ArgumentException("Invalid argument", "interval");
			}
		}

		private static bool ValidateKlines(string klines_file, string checksum_file)
		{
			throw new NotImplementedException();
		}

		private static void Extract(List<byte> data)
		{
			using (var stream = new MemoryStream(data.ToArray()))
			using (var zip = ZipFile.Read(stream))
			{
				if (zip.Count != 1)
				{
					throw new Exception("Error: Invalid number of files in zip file");
				}

				zip[0].Extract(DownloadPath);
			}
		}
	}
}
