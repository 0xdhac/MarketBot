using MarketBot.exchanges.binance;
using System;
using System.Collections.Generic;
using System.Linq;
using MarketBot.interfaces;

namespace MarketBot
{
	public enum Exchanges
	{
		Localhost,
		Binance,
		Kucoin,
		Ameritrade
	};

	public enum PositionStatus
	{
		New,
		Ordered,
		Filled,
		Oco
	}

	//public delegate void OrderUpdateCallback(OrderInfo info);
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
		public bool Real;
		public decimal Quantity;
		public PositionStatus Status;
		public decimal Commission;

		public Position(SymbolData data, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price, decimal quantity, bool real)
		{
			Create(data.Exchange, data.Symbol, period, signal, entry_price, risk_price, profit_price, quantity, real);
		}

		public Position(Exchanges exchange, string symbol, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price, decimal quantity, bool real)
		{
			Create(exchange, symbol, period, signal, entry_price, risk_price, profit_price, quantity, real);
		}

		private void Create(Exchanges exchange, string symbol, int period, SignalType signal, decimal entry_price, decimal risk_price, decimal profit_price, decimal quantity, bool real)
		{
			Exchange	= exchange;
			Symbol		= symbol;
			Entry		= real ? ExchangeTasks.GetTickSizeAdjustedQuantity(exchange, symbol, entry_price) : entry_price;
			Risk		= real ? ExchangeTasks.GetTickSizeAdjustedQuantity(exchange, symbol, risk_price): risk_price;
			Profit		= real ? ExchangeTasks.GetTickSizeAdjustedQuantity(exchange, symbol, profit_price): profit_price;
			Quantity	= real ? ExchangeTasks.GetStepSizeAdjustedQuantity(exchange, symbol, quantity) : quantity;
			Type		= signal;
			Period		= period;
			Real		= real;
			Status		= PositionStatus.New;
			Commission	= 0;

			Positions.Add(this);

			if(Real == true)
			{
				Console.WriteLine($"[{symbol}] Position created. Type = {Type}, Entry @ {Entry}, Risk @ {Risk}, Profit @ {Profit}");
				ExchangeTasks.PlaceOrder(Exchange, symbol, this);
			}
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
		public static void UpdateExchangeInfo(Exchanges ex)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceMarket.UpdateExchangeInfo();
					break;
			}
		}
		public static void GetTradingPairs(Exchanges ex, string symbol_regex, Action<Exchanges, List<string>> callback)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					callback(ex, BinanceMarket.GetTradingPairs(symbol_regex));
					break;
			}
		}

		public static void Screener(Exchanges ex, string symbol_regex, OHLCVInterval interval, Action<SymbolData> callback)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					new BinanceScreener().Screen(symbol_regex, BinanceOHLCVCollection.ConvertToExchangeInterval(interval), callback);
					break;
			}
		}

		public static Wallet GetWallet(Exchanges ex, EventHandler callback)
		{
			Wallet w = null;

			switch (ex)
			{
				case Exchanges.Binance:
					w = new BinanceWallet(ex);
					w.BalanceUpdated += callback;
					w.UpdateBalance();
					break;
			}

			return w;
		}

		public static decimal GetStepSizeAdjustedQuantity(Exchanges ex, string symbol, decimal quantity)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceMarket.GetStepSizeAdjustedQuantity(symbol, quantity, out quantity);
					return quantity;
			}

			return 0;
		}

		public static decimal GetTickSizeAdjustedQuantity(Exchanges ex, string symbol, decimal quantity)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceMarket.GetTickSizeAdjustedQuantity(symbol, quantity, out quantity);
					return quantity;
			}

			return 0;
		}



		public static void PlaceOrder(Exchanges ex, string symbol, Position pos)
		{
			switch(ex)
			{
				case Exchanges.Binance:
					BinanceOrders.PlaceOrder(pos, pos.Type == SignalType.Long?Binance.Net.Enums.OrderSide.Buy:throw new Exception("NOT SUPPORTED YET"), Binance.Net.Enums.OrderType.Limit, 0, pos.Entry);
					break;
			}
		}

		public static void CreateUserStream(Exchanges ex)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceStream.CreateUserStream();
					break;
			}
		}

		public static IExchangeOHLCVCollection CollectOHLCV(Exchanges ex, string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, bool screener_update, DateTime? start = null)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceOHLCVCollection collection = new BinanceOHLCVCollection(symbol);
					collection.CollectOHLCV(interval, periods, callback, screener_update);
					return collection;
				default:
					throw new Exception("Invalid exchange");
			}
		}

		public static TimeSpan GetOHLCVIntervalTimeSpan(OHLCVInterval interval)
		{
			switch (interval)
			{
				case OHLCVInterval.EightHour:
					return new TimeSpan(8, 0, 0);
				case OHLCVInterval.FifteenMinute:
					return new TimeSpan(0, 15, 0);
				case OHLCVInterval.FiveMinute:
					return new TimeSpan(0, 5, 0);
				case OHLCVInterval.FourHour:
					return new TimeSpan(4, 0, 0);
				case OHLCVInterval.OneDay:
					return new TimeSpan(1, 0, 0, 0);
				case OHLCVInterval.OneHour:
					return new TimeSpan(1, 0, 0);
				case OHLCVInterval.OneMinute:
					return new TimeSpan(0, 1, 0);
				case OHLCVInterval.OneMonth:
					throw new Exception("Don't know what to do here");
				case OHLCVInterval.OneWeek:
					return new TimeSpan(7, 0, 0, 0);
				case OHLCVInterval.SixHour:
					return new TimeSpan(6, 0, 0);
				case OHLCVInterval.ThirtyMinute:
					return new TimeSpan(0, 30, 0);
				case OHLCVInterval.ThreeDay:
					return new TimeSpan(30, 0, 0, 0);
				case OHLCVInterval.ThreeMinute:
					return new TimeSpan(0, 3, 0);
				case OHLCVInterval.TwelveHour:
					return new TimeSpan(12, 0, 0);
				case OHLCVInterval.TwoHour:
					return new TimeSpan(2, 0, 0);
				default:
					throw new Exception("Invalid OHLCVInterval. Binance does not support this interval.");
			}
		}
	}
}
