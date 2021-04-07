using MarketBot.exchanges.binance;
using System;
using System.Collections.Generic;
using System.Linq;
using MarketBot.interfaces;
using MarketBot.tools;
using MarketBot.skender_strategies;
using MarketBot.skender_strategies.exit_strategy;
using Binance.Net;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;

namespace MarketBot
{
	public enum Exchanges
	{
		Localhost,
		Binance
	};

	public enum PositionStatus
	{
		Started,
		Ordered,
		Failed,
		WaitingForExit,
		Exited
	};

	public enum TradesTable
	{
		OrderId,
		Symbol,
		Signal,
		Quantity,
		EntryPrice,
		ExitPrice,
		AccountType,
		Status,
		Commission,
		CommissionAsset,
		ExitOrderId,
		Exchange,
		Id,
		Date,
		FilledQuantity
	}

	public enum GenericOrderType
	{
		Market,
		Limit
	}

	public class Position
	{
		public static List<Position> Positions = new List<Position>();
		public List<ExitStrategy> ExitStrategies { get; set; } = new List<ExitStrategy>();
		public Exchanges Exchange { get; set; }
		public string Symbol { get; set; }
		public decimal Entry { get; set; }
		public SignalType Type { get; set; }
		public bool Real;
		public decimal DesiredQuantity { get; set; }
		public decimal? FilledQuantity { get; set; }
		public PositionStatus Status = PositionStatus.Started;
		public decimal? Commission { get; set; } = 0;
		public string Commission_Asset { get; set; }
		public string OrderId = null;
		public string ExitOrderId = null;
		public uint? Id { get; set; }
		public DateTime Date;
		public string AccountType { get; set; }

		/* When to save MySQL record
		 * 
		 * When position is created
		 * On creation of entry order
		 * Whenever there is an entry order update (Changes in: Quantity filled, order status)
		 * On creation of exit order
		 * On exit order update
		 */

		public Position(Exchanges exchange, string symbol, SignalType signal, DateTime date, decimal entry_price, decimal quantity, bool real)
		{
			Exchange = exchange;
			Symbol = symbol;
			Entry = real ? ExchangeTasks.GetTickSizeAdjustedValue(exchange, symbol, entry_price) : entry_price;
			DesiredQuantity = real ? ExchangeTasks.GetStepSizeAdjustedQuantity(exchange, symbol, quantity) : quantity;
			Type = signal;
			Real = real;
			Status = PositionStatus.Started;
			Commission = 0;
			Date = date;
			AccountType = "Spot";

			/*
			 * MySQL Table Columns:
			 * - Symbol
			 * - OrderId (Primary key)
			 * - Date
			 * - Signal
			 * - Quantity
			 * - Quantity filled
			 * - Entry price
			 * - Exit price (Can be NULL if trade unfinished)
			 * - Account type (Spot)
			 * - Order status
			 * - Real
			 * - Commission
			 * - Commission Asset
			 */

			Positions.Add(this);
		}

		public void CreateOrder()
		{
			throw new Exception("Make sure risk/profit are fixed before calling this.");
			//RealtimeBot.BuyCount++;
			//ExchangeTasks.PlaceOrder(Exchange, this, GenericOrderType.Limit);
		}

		public void OrderStreamUpdate(BinanceStreamOrderUpdate update)
		{
			if(update.OriginalClientOrderId == OrderId)
			{

			}
			else if(update.OriginalClientOrderId == ExitOrderId)
			{

			}
		}

		public void OnSymbolKlineUpdate()
		{
			foreach(var exit in ExitStrategies)
			{
				if(exit.ShouldExit(exit.History.Count - 1, Type, out decimal price))
				{
					// Place order for market sell OR do some algebra and calculate new stoploss and profits
				}
			}
		}

		public void Close()
		{
			Positions.Remove(this);
		}

		public static List<Position> FindPositions(Exchanges exchange, string symbol)
		{
			return Positions.Where((r) => r.Exchange == exchange && r.Symbol == symbol).ToList();
		}

		public void SaveToDb()
		{
			if(Id == null)
			{

			}
			//string query = "INSERT INTO `trades` (`symbol`, `signal`, `quantity`"
		}

		public static void LoadFromDb()
		{
			using (var client = new BinanceClient())
			{
				string query = "SELECT * FROM `trades` WHERE `status` LIKE 'WaitingForExit' OR `status` LIKE 'Ordered'";

				var command = new MySqlCommand();
				command.Connection = Program.Connection;
				command.CommandText = query;
				var reader = command.ExecuteReader();

				while (reader.Read())
				{
					try
					{
						string orderid			= reader.IsDBNull((int)TradesTable.OrderId)?null:reader.GetString((int)TradesTable.OrderId);
						string symbol			= reader.GetString((int)TradesTable.Symbol);
						SignalType signal		= (SignalType)Enum.Parse(typeof(SignalType), reader.GetString((int)TradesTable.Signal));
						decimal quantity		= reader.GetDecimal((int)TradesTable.Quantity);
						decimal entry_price		= reader.GetDecimal((int)TradesTable.EntryPrice);
						string account_type		= reader.GetString((int)TradesTable.AccountType);
						decimal commission		= reader.GetDecimal((int)TradesTable.Commission);
						string commission_asset = reader.GetString((int)TradesTable.CommissionAsset);
						string exit_orderid		= reader.GetString((int)TradesTable.ExitOrderId);
						Exchanges exchange		= (Exchanges)Enum.Parse(typeof(Exchanges), reader.GetString((int)TradesTable.Exchange));
						DateTime date			= DateTime.Parse(reader.GetString((int)TradesTable.Date));
						uint id					= reader.GetUInt32((int)TradesTable.Id);
						decimal filled_quantity = reader.GetDecimal((int)TradesTable.FilledQuantity);
						PositionStatus status = (PositionStatus)Enum.Parse(typeof(PositionStatus), reader.GetString((int)TradesTable.Status));

						new Position(exchange, symbol, signal, date, entry_price, quantity, true)
						{
							OrderId = orderid,
							AccountType = account_type,
							Commission = commission,
							Commission_Asset = commission_asset,
							Status = status,
							ExitOrderId = exit_orderid,
							Date = date,
							Id = id,
							FilledQuantity = filled_quantity
						};
					}
					catch(Exception ex)
					{
						Program.LogError(ex.Message);
					}
				}
			}
		}
	}

	static class ExchangeTasks
	{
		public static void LoadPositions(Exchanges ex)
		{
			switch (ex)
			{
				case Exchanges.Binance:
					Position.LoadFromDb();
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
						pos.DesiredQuantity,
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
