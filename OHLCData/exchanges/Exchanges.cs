using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.exchanges.binance;

namespace MarketBot
{
	public enum Exchanges
	{
		Localhost,
		Binance,
		Kucoin,
		Ameritrade
	};

	public delegate void ExitPositionCallback(Position pos);

	public class Position
	{
		public static List<Position> Positions = new List<Position>();
		public Exchanges Exchange;
		public string Symbol;
		public decimal Entry;
		public decimal Risk;
		public decimal Profit;
		public SignalType Type;
		public int Period;

		public Position(SymbolData data, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price)
		{
			Create(data.Exchange, data.Symbol, period, signal, entry_price, risk_price, profit_price);
		}

		public Position(Exchanges exchange, string symbol, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price)
		{
			Create(exchange, symbol, period, signal, entry_price, risk_price, profit_price);
		}

		private void Create(Exchanges exchange, string symbol, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price)
		{
			Exchange	= exchange;
			Symbol		= symbol;
			Entry		= entry_price;
			Risk		= risk_price;
			Profit		= profit_price;
			Type		= signal;
			Period		= period;

			Positions.Add(this);
		}

		public void Close()
		{
			Positions.Remove(this);
		}

		public static List<Position> FindPositions(SymbolData data)
		{
			return Positions.Where((r) => r.Exchange == data.Exchange && r.Symbol == data.Symbol).ToList();
		}
	}

	static class ExchangeTasks
	{
		public static void Screener(Exchanges ex, string symbol_regex, OHLCVInterval interval, PeriodCloseCallback callback)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceScreener.Screen(symbol_regex, BinanceOHLCVCollection.ConvertExchangeInterval(interval), callback);
					break;
			}
		}

		public static IExchangeOHLCVCollection CollectOHLCV(Exchanges ex, string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, DateTime? start = null)
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
