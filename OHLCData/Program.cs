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

namespace MarketBot
{
	class Program
	{
		static void Main(string[] args)
		{
			BinanceClient.SetDefaultOptions(new BinanceClientOptions()
			{
				ApiCredentials = new ApiCredentials("uxxCQbHuAvZNKsH2gOqewAsnymgt8qvzFOCqoKgazVhGlXVx4rvuWGA0wQIvkzNM", "YyysydbbeM1t3PEOBat0X7y5OgTr43P7LlhVzmLGTacON8yFPzY76KsOm2WRgI58"),
				//LogVerbosity = LogVerbosity.Debug,
				//LogWriters = new List<TextWriter> { Console.Out }
			});

			BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
			{
				ApiCredentials = new ApiCredentials("uxxCQbHuAvZNKsH2gOqewAsnymgt8qvzFOCqoKgazVhGlXVx4rvuWGA0wQIvkzNM", "YyysydbbeM1t3PEOBat0X7y5OgTr43P7LlhVzmLGTacON8yFPzY76KsOm2WRgI58"),
				//LogVerbosity = LogVerbosity.Debug,
				//LogWriters = new List<TextWriter> { Console.Out }
			});

			using (var client = new BinanceClient())
			{
				SymbolData sym_data = new SymbolData("BTCUSDT", KlineInterval.FifteenMinutes, 10000, testcb);
				//Console.WriteLine(price.Data.BestAskPrice);
				/*
				// Spot.Order | Spot order info endpoints
				client.Spot.Order.GetAllOrders("BTCUSDT");
				// Spot.System | Spot system endpoints
				client.Spot.System.GetExchangeInfo();
				// Spot.UserStream | Spot user stream endpoints. Should be used to subscribe to a user stream with the socket client
				client.Spot.UserStream.StartUserStream();
				// Spot.Futures | Transfer to/from spot from/to the futures account + cross-collateral endpoints
				client.Spot.Futures.TransferFuturesAccount("ASSET", 1, FuturesTransferType.FromSpotToUsdtFutures);
				*/
			}

			Console.ReadLine();

			// Create replay with given candle chart data and strategy. The replay will iterate through each candle and run the set strategy each time looking for a buy. Make sure not to open a position when already in one.
		}

		static void testcb(SymbolData symbolData)
		{
			int i = 0;
			foreach(var d in symbolData.Data)
			{
				Console.WriteLine(d.High);
			}
			
		}
	}
}
