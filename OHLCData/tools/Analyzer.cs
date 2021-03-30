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

namespace MarketBot.tools
{
	static class BinanceAnalyzer
	{
		/*
		 * The goal of this class is to find out what the best strategies, risk/reward ratios, bet resizing modes
		 * 
		 */

		// Download folder for symbol data
		private static string DownloadPath = @"./klines/";

		// Url building vars
		private static string BaseUrl = @"https://data.binance.vision/data";
		private static string Account = @"spot";
		private static string DateRange = @"monthly";
		private static string DataType = @"klines";
		private static HList<OHLCVPeriod> Market = null;

		// Analyzer settings
		private static List<string> Pairs = null;
		private static OHLCVInterval Interval;


		static decimal total_profit = 0;
		static decimal total_profitability = 0;
		static int num_symbols = 0;

		public static void Run(string pattern, OHLCVInterval interval, bool download = true)
		{
			Console.WriteLine("Running Analyzer");

			Interval = interval;
			Pairs = BinanceMarket.GetTradingPairs(pattern, true);
			if(download)
				DownloadKlines(pattern, interval);

			RunReplays();
			//LoadMarket();
			
		}

		public static void LoadMarket()
		{
			var file = BuildFile("BTCUSDT", OHLCVInterval.ThirtyMinute, DateTime.Now.AddMonths(-1));
			new SymbolData("BTCUSDT", new string[] { DownloadPath + file + ".csv" }, OHLCVInterval.ThirtyMinute, CSVConversionMethod.BinanceVision, MarketLoaded);
		}

		public static void MarketLoaded(SymbolData data)
		{
			Market = data.Data.Periods;
			PrintBetas();
		}

		public static void PrintBetas()
		{
			foreach(var pair in Pairs)
			{
				var file = DownloadPath + BuildFile(pair, OHLCVInterval.ThirtyMinute, DateTime.Now.AddMonths(-1)) + ".csv";
				if (!File.Exists(file))
					continue;

				SymbolData sym = new SymbolData(pair, new string[] { file }, OHLCVInterval.ThirtyMinute, CSVConversionMethod.BinanceVision, ThrowAwayCallback);

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
			
		}

		public static void RunReplays()
		{
			foreach(var pair in Pairs)
			{
				new Replay(Exchanges.Localhost, pair, Interval, 0, null, Results);
				Position.Positions.RemoveAll(s => true);
			}

			Console.WriteLine($"Results Avg: {(total_profit / num_symbols):.00}");
			Console.WriteLine($"Results Prof: {(total_profitability / num_symbols):.00}");
		}

		public static void Results(ReplayResults r)
		{
			total_profit += r.EndTotal;
			total_profitability += r.Profitability;
			num_symbols++;

			Console.WriteLine($"{r.Symbol}: {r.Profitability:.00} ({r.Trades}) ${r.EndTotal:.00}");
		}

		public static void WhitelistResults(ReplayResults r)
		{
			// Good trade: Profitability >= 1.3
			// Trades >= 50

			decimal min_profitability = (decimal)1.3;
			int min_trades = 50;

			if(r.Profitability >= min_profitability &&
				r.Trades >= min_trades)
			{
				total_profit += r.EndTotal;
				total_profitability += r.Profitability;
				num_symbols++;

				Console.WriteLine($"{r.Symbol}: {r.Profitability:.00} ({r.Trades}) ${r.EndTotal:.00}");
			}
		}

		public static void BlacklistResults(ReplayResults r)
		{
			// Good trade: Profitability >= 1.3
			// Trades >= 50

			decimal max_profitability = (decimal)1.3;
			int max_trades = 30;

			if (r.Profitability < max_profitability ||
				r.Trades < max_trades)
			{
				total_profit += r.EndTotal;
				total_profitability += r.Profitability;
				num_symbols++;

				//Console.WriteLine($"{r.Symbol}: {r.Profitability:.00} ({r.Trades}) ${r.EndTotal:.00}");
				Console.WriteLine($"{r.Symbol}");
			}
		}

		// Download candle stick data from data.binance.vision
		public static void DownloadKlines(string pattern, OHLCVInterval interval)
		{
			if(Pairs == null)
			{
				throw new NullReferenceException("Null reference to 'Pairs' object.");
			}

			foreach(var pair in Pairs)
			{
				Console.WriteLine($"Downloading {pair} : {interval}");
				var list = DownloadZipFiles(pair, interval);
				foreach(var file in list)
					Extract(file);
			}
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

					if(!File.Exists(DownloadPath + file + ".csv"))
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
