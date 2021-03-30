using MarketBot.exchanges.binance;
using System;
using System.Collections.Generic;
using System.Linq;
using MarketBot.interfaces;
using MarketBot.tools;

namespace MarketBot
{
	public enum Exchanges
	{
		Localhost,
		Binance
	};

	public enum PositionStatus
	{
		New,
		Ordered,
		Filled,
		Oco
	};

	public enum GenericOrderType
	{
		Market,
		Limit
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
		public decimal Filled = 0;
		public SignalType Type;
		public int Period;
		public bool Real;
		public decimal Quantity;
		public PositionStatus Status;
		public decimal Commission;
		public long OrderId;

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
			Entry		= real ? ExchangeTasks.GetTickSizeAdjustedValue(exchange, symbol, entry_price) : entry_price;

			if (real)
			{
				decimal tick_size;
				if (!ExchangeTasks.GetTickSize(exchange, symbol, out tick_size))
				{
					throw new Exception($"[{symbol}] Tick size not found for symbol");
				}
				Entry += (tick_size * 5);
			}

			Risk		= real ? ExchangeTasks.GetTickSizeAdjustedValue(exchange, symbol, risk_price): risk_price;
			Profit		= real ? ExchangeTasks.GetTickSizeAdjustedValue(exchange, symbol, profit_price): profit_price;
			Quantity	= real ? ExchangeTasks.GetStepSizeAdjustedQuantity(exchange, symbol, quantity) : quantity;
			Type		= signal;
			Period		= period;
			Real		= real;
			Status		= PositionStatus.New;
			Commission	= 0;

			//Console.WriteLine($"{symbol}: {entry_price}, {risk_price} {Math.Abs((risk_price - entry_price) / entry_price)}, {profit_price} {Math.Abs((profit_price - entry_price) / entry_price)}");
			//Console.WriteLine($"{symbol}: {Entry}, {Risk} {Math.Abs((Risk - Entry) / Entry)}, {Profit} {Math.Abs((Profit - Entry) / Entry)}");

			Positions.Add(this);

			if(Real == true)
			{
			}
		}

		public void CreateOrder()
		{
			RealtimeBot.BuyCount++;
			ExchangeTasks.PlaceOrder(Exchange, this, GenericOrderType.Limit);
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
		public static void LoadPositions(Exchanges ex)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceAccount.LoadPositions();
					break;
			}
		}
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

		public static bool GetTickSize(Exchanges ex, string symbol, out decimal output)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceMarket.GetTickSize(symbol, out output);
					return true;
			}

			output = default;
			return false;
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

		public static decimal GetTickSizeAdjustedValue(Exchanges ex, string symbol, decimal quantity)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceMarket.GetTickSizeAdjustedValue(symbol, quantity, out quantity);
					return quantity;
			}

			return 0;
		}



		public static void PlaceOrder(Exchanges ex, Position pos, GenericOrderType type)
		{
			switch(ex)
			{
				case Exchanges.Binance:
					BinanceOrders.PlaceOrder(
						pos, 
						pos.Type == SignalType.Long?Binance.Net.Enums.OrderSide.Buy:throw new Exception("NOT SUPPORTED YET"), 
						BinanceOrders.GetOrderType(type), 
						0, 
						pos.Quantity,
						pos.Entry);
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

		public static IExchangeOHLCVCollection CollectOHLCV(Exchanges ex, string symbol, OHLCVInterval interval, int periods, Action<IExchangeOHLCVCollection> callback, bool screener_update, DateTime? start = null)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					BinanceOHLCVCollection collection = new BinanceOHLCVCollection(symbol);
					collection.CollectApiOHLCV(interval, periods, callback, screener_update);
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
