using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using System.IO;
using dotenv.net;
using MarketBot.indicators;

namespace MarketBot
{
	class Program
	{
		static void Main(string[] args)
		{
			DotEnv.Config(true, "./cfg/api.env");

			string api_key = Environment.GetEnvironmentVariable("BINANCE_API_KEY");
			string secret_key = Environment.GetEnvironmentVariable("BINANCE_SECRET_KEY");

			BinanceClient.SetDefaultOptions(new BinanceClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Info,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				LogVerbosity = LogVerbosity.Info,
				LogWriters = new List<TextWriter> { Console.Out }
			});

			//new Replay(Exchanges.Binance, "ADAUSDT", OHLCVInterval.OneMinute, 14400, DateTime.UtcNow);
			//new Replay(Exchanges.Localhost, "./ADAUSDT_OneMinute.csv", OHLCVInterval.OneMinute, 0, null);

			ExchangeTasks.Screener(Exchanges.Binance, "USDT$", OHLCVInterval.OneMinute, Save);
			//SymbolData data = new SymbolData(Exchanges.Binance, OHLCVInterval.OneMinute, "EGLDUSDT", 150000, Save);

			Console.ReadLine();
		}

		private static void Save(SymbolData data)
		{
			Console.WriteLine("Saving");
			OHLCDataToCSV.Convert(data.Data.Data, $"./{data.Symbol}_{data.Interval}.csv");
			Console.WriteLine("Saved");
		}
	}
}
