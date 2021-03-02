using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	enum Exchange
	{
		Binance,
		Kucoin,
		Ameritrade
	};

	static class Exchanges
	{
		public static IExchangeOHLCVCollection CollectOHLCV(Exchange ex, string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback)
		{
			switch (ex)
			{
				case Exchange.Binance:
					BinanceOHLCVCollection collection = new BinanceOHLCVCollection();
					collection.CollectOHLCV(symbol, interval, periods, callback);
					return collection;
				default:
					throw new Exception("Invalid exchange");
			}
		}
	}
}
