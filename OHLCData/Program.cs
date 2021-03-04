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
				//LogVerbosity = LogVerbosity.Debug,
				//LogWriters = new List<TextWriter> { Console.Out }
			});

			BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
			{
				ApiCredentials = new ApiCredentials(api_key, secret_key),
				//LogVerbosity = LogVerbosity.Debug,
				//LogWriters = new List<TextWriter> { Console.Out }
			});

			SMA Indicator = new SMA(20);
			SymbolData sym_data = new SymbolData(Exchanges.Binance, OHLCVInterval.FifteenMinute, "BTCUSDT", 200);
			sym_data.ApplyIndicator(Indicator);

			foreach(var indicator in sym_data.Indicators)
			{
				Console.WriteLine(indicator.GetType().Name);
			}

			Console.ReadLine();
		}

		static void testcb(IExchangeOHLCVCollection data)
		{
			
		}

		// Create replay with given candle chart data and strategy. The replay will iterate through each candle and run the set strategy each time looking for a buy. Make sure not to open a position when already in one.
	}
}
