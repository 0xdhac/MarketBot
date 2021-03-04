using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public enum Exchanges
	{
		Binance,
		Kucoin,
		Ameritrade
	};

	static class ExchangeTasks
	{
		public static IExchangeOHLCVCollection CollectOHLCV(Exchanges ex, string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceOHLCVCollection collection = new BinanceOHLCVCollection();
					collection.CollectOHLCV(symbol, interval, periods, callback);
					return collection;
				default:
					throw new Exception("Invalid exchange");
			}
		}
	}
}
