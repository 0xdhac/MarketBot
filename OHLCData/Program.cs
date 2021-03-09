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
			
			Replay r = new Replay(Exchanges.Binance, "BTCUSDT", OHLCVInterval.FortyFiveMinute, 30000, DateTime.UtcNow);

			Console.ReadLine();
		}
	}
}
