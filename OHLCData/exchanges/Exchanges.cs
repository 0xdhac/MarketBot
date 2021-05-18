using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;
using MarketBot.exchanges.binance;
using MarketBot.interfaces;
using MarketBot.skender_strategies;
using MarketBot.tools;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarketBot
{
	public enum Exchanges
	{
		Localhost,
		Binance
	};

	public enum PositionStatus
	{
		Started, // When the position object is first created
		EntryOrderPlaced, //* After the buy order is placed. OrderId must be set before calling SaveToDb
		EntryOrderFailed, // If the buy order fails to fill even partially
		EntryOrderSucceeded, //* If the buy order fills of partially fills
		StandingBy, //*
		ExitPriceReached, //*
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
		FilledQuantity,
		OcoProfitLegPrice,
		OcoLossLegPrice,
		OcoLastUpdate,
		Interval,
		ExitedQuantity
	}

	public enum GenericOrderType
	{
		Market,
		Limit
	}

	public enum GenericOrderSide
	{
		Buy,
		Sell
	}

	public enum GenericOrderStatus
	{
		New,
		Rejected,
		Canceled,
		PartiallyFilled,
		Filled,
		Other
	}

	public class Position
	{
		public static List<Position> Positions = new List<Position>();
		public List<ExitStrategy> ExitStrategies { get; set; } = new List<ExitStrategy>();
		public PriceSetter ProfitSetter { get; set; } = null;
		public PriceSetter RiskSetter { get; set; } = null;
		public OHLCVInterval Interval { get; set; }
		public Exchanges Exchange { get; set; }
		public string Symbol { get; set; }
		public decimal Entry { get; set; }
		public SignalType Type { get; set; }
		public decimal DesiredQuantity { get; set; }
		public decimal ExitQuantity { get; set; } = 0;
		private PositionStatus mStatus = PositionStatus.Started;
		public decimal Commission { get; set; } = 0;
		public DateTime Date;
		public string AccountType { get; set; }
		public DateTime? LastOcoDate { get; set; } = null;
		public IExchangeOHLCVCollection PriceHistory { get; set; } = null;
		public decimal FilledQuantity { get; set; }

#nullable enable
		public decimal? Risk { get; set; }
		public decimal? Profit { get; set; }
		public decimal? Exit { get; set; }
		public string? Commission_Asset { get; set; }
		public string? OrderId;
		public long? ExitOrderId;
		public long? Id { get; set; }
#nullable disable

		/* When to save MySQL record
		 * 
		 * When position is created
		 * On creation of entry order
		 * Whenever there is an entry order update (Changes in: Quantity filled, order status)
		 * On creation of exit order
		 * On exit order update
		 */

		public Position(Exchanges exchange, string symbol, OHLCVInterval interval, SignalType signal, DateTime date, decimal entry_price, decimal quantity)
		{
			Exchange = exchange;
			Symbol = symbol;
			Entry = entry_price;
			DesiredQuantity = quantity;
			Type = signal;
			Commission = 0;
			Date = date;
			AccountType = "Spot";

			Positions.Add(this);
		}

		public PositionStatus Status
		{
			get
			{
				return mStatus;
			}
			set
			{
				if (mStatus != value)
				{
					mStatus = value;
					SaveToDb();
					OnStatusChange();
				}
			}
		}

		public void EnterMarket()
		{
			SaveToDb();

			RealtimeBot.BuyCount++;

			OrderId = Functions.GetRandomString(15);
			ExchangeTasks.PlaceOrder(Exchange, Symbol, DesiredQuantity, Entry, OrderId, GenericOrderType.Limit, GenericOrderSide.Buy);
		}

		public void OrderStreamUpdate(BinanceStreamOrderUpdate update)
		{
			if(update.Side == OrderSide.Buy)
			{
				OnEntryOrderUpdate(update);
			}
			else if(update.Side == OrderSide.Sell)
			{
				OnExitOrderUpdate(update);
			}
		}

		public void OnEntryOrderUpdate(BinanceStreamOrderUpdate update)
		{
			bool force = false;
			var temp = Status;

			if (update.Status == OrderStatus.New)
			{
				Status = PositionStatus.EntryOrderPlaced;

				var seconds = int.Parse(Program.GetConfigSetting("CANCEL_BUY_ORDER_AFTER_X_SECONDS"));

				Functions.CreateTimer(TimeSpan.FromSeconds(seconds), () =>
				{
					if(FilledQuantity == 0m)
					{
						ExchangeTasks.CancelOrder(Exchange, Symbol, OrderId);
					}
				});
			}
			else if (update.Status == OrderStatus.Canceled)
			{
				if (FilledQuantity > 0m)
				{
					Status = PositionStatus.EntryOrderSucceeded;
				}
				else
				{
					Status = PositionStatus.EntryOrderFailed;
				}
			}
			else if (update.Status == OrderStatus.Expired)
			{
				Status = PositionStatus.EntryOrderFailed;
			}
			else if (update.Status == OrderStatus.PartiallyFilled)
			{
				FilledQuantity += update.LastQuantityFilled;
				Commission_Asset = update.CommissionAsset;
				if (!Commission_Asset.Equals("BNB"))
				{
					Commission += update.Commission;
				}
			}
			else if (update.Status == OrderStatus.Filled)
			{
				FilledQuantity += update.LastQuantityFilled;
				Commission_Asset = update.CommissionAsset;
				if (!Commission_Asset.Equals("BNB"))
				{
					Commission += update.Commission;
				}
				Status = PositionStatus.EntryOrderSucceeded;
			}
			else if (update.Status == OrderStatus.Rejected)
			{
				Status = PositionStatus.EntryOrderFailed;
			}
			else
			{
				throw new Exception($"{update.Status}");
			}

			if(temp == Status && force == true)
			{
				SaveToDb();
			}
		}

		public void OnExitOrderUpdate(BinanceStreamOrderUpdate update)
		{
			var temp = Status;
			bool force = false;

			if(update.Status == OrderStatus.Canceled)
			{
				// Situations:
				// OCO Gets canceled (do nothing if oco gets canceled, check if status is StandingBy)
				if (Status == PositionStatus.StandingBy)
					return;
			}
			else if(update.Status == OrderStatus.Expired)
			{
				// Market sell if I'm setting a time limit for the order to limit sell within certain number of seconds and this order cancels because it doesn't sell in time
				// Sell FilledQuantity - ExitQuantity
			}
			else if(update.Status == OrderStatus.Rejected)
			{
				throw new Exception($"{update.Status}");
			}
			else if(update.Status == OrderStatus.New)
			{
				if(update.CreateTime != update.UpdateTime)
				{
					// Order is old
					Status = PositionStatus.ExitPriceReached;

					// CREATE TIMER HERE THAT MARKET SELLS AFTER X SECONDS IF STATUS != Exited. MAKE SURE TO UPDATE ExitOrderId to match the one set in the order
					var seconds = int.Parse(Program.GetConfigSetting("CANCEL_AND_MARKET_SELL_ORDER_AFTER_X_SECONDS"));

					Functions.CreateTimer(TimeSpan.FromSeconds(seconds), () =>
					{
						if (Status != PositionStatus.Exited)
						{
							ExchangeTasks.CancelAllOrders(Exchange, Symbol);
							ExchangeTasks.PlaceOrder(Exchange, Symbol, FilledQuantity - ExitQuantity, null, Functions.GetRandomString(15), GenericOrderType.Market, GenericOrderSide.Sell);
						}
					});
				}
				else
				{
					// Order is new
					ExitOrderId = update.OrderListId;
					Status = PositionStatus.StandingBy;
					force = true;
				}
			}
			else if(update.Status == OrderStatus.Filled)
			{
				Exit = update.Price;
				ExitQuantity += update.LastQuantityFilled;
				Status = PositionStatus.Exited;
				force = true;
			}
			else if(update.Status == OrderStatus.PartiallyFilled)
			{
				Exit = update.Price;
				ExitQuantity += update.LastQuantityFilled;
				Status = PositionStatus.ExitPriceReached;
				force = true;
			}

			if(force == true)
			{
				SaveToDb();
			}
		}

		public void OnOcoOrderUpdate(BinanceStreamOrderList update)
		{
			if(update.ListOrderStatus == ListOrderStatus.Executing)
			{
				ExitOrderId = update.OrderListId;
				SaveToDb();
			}
		}

		public void OnSymbolKlineUpdate()
		{
			if(Status == PositionStatus.StandingBy)
			{
				Debug.Assert(ProfitSetter != null);

				// Cancel OCO Order
				ExchangeTasks.CancelAllOcosBySymbol(Exchange, Symbol);

				// Update Profit Value, Make sure exit value stays the same
				Profit = ExchangeTasks.GetTickSizeAdjustedValue(Exchange, Symbol, ProfitSetter.GetPrice(PriceHistory.Periods.Count - 1, Type));
				decimal quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchange, Symbol, FilledQuantity - Commission - ExitQuantity);
				if(quantity > 0m)
				{
					// Create new OCO
					ExchangeTasks.CreateOco(Exchanges.Binance, Symbol, GenericOrderSide.Sell, quantity, Profit.Value, Risk.Value);
				}
			}
		}

		public void OnStatusChange()
		{
			if(Status == PositionStatus.EntryOrderSucceeded)
			{
				Debug.Assert(ProfitSetter != null && Risk.HasValue);

				Profit = ExchangeTasks.GetTickSizeAdjustedValue(Exchange, Symbol, ProfitSetter.GetPrice(PriceHistory.Periods.Count - 1, Type));
				decimal quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchange, Symbol, FilledQuantity - Commission - ExitQuantity);
				ExchangeTasks.CreateOco(Exchanges.Binance, Symbol, GenericOrderSide.Sell, quantity, Profit.Value, Risk.Value);
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
		public static object _locker = new object();

		public void SaveToDb()
		{
			lock (_locker)
			{
				string query = null;
				if (Status == PositionStatus.Started)
				{
					query = @$"INSERT INTO `trades` (`symbol`, `signal`, `quantity`, `entry_price`, `account_type`, `status`, `exchange`, `date`, `interval`, `oco_profit_leg_price`, `oco_loss_leg_price`)
VALUES ('{Symbol}', '{Type}', {DesiredQuantity}, {Entry}, 'Spot', '{Status}', '{Exchange}', '{DateTime.UtcNow}', '{Interval}', '{Profit}', '{Risk}')";
				}
				else if (Status == PositionStatus.EntryOrderPlaced)
				{
					Debug.Assert(Id.HasValue && !string.IsNullOrEmpty(OrderId));
					query = $@"UPDATE `trades` SET `status` = '{Status}', `orderid` = '{OrderId}'  WHERE `id` = {Id}";
				}
				else if (Status == PositionStatus.EntryOrderFailed)
				{
					Debug.Assert(Id.HasValue);
					query = $@"UPDATE `trades` SET `status` = '{Status}' WHERE `id` = {Id}";
				}
				else if (Status == PositionStatus.EntryOrderSucceeded)
				{
					Debug.Assert(Id.HasValue && !string.IsNullOrEmpty(Commission_Asset) && FilledQuantity > 0);
					query = $@"UPDATE `trades` SET `status` = '{Status}', `commission` = {Commission}, `commission_asset` = '{Commission_Asset}', `filled_quantity` = '{FilledQuantity}' WHERE `id` = {Id}";
				}
				else if (Status == PositionStatus.StandingBy)
				{
					Debug.Assert(Id.HasValue && ExitOrderId.HasValue && Profit.HasValue && Risk.HasValue);
					query = $@"UPDATE `trades` SET `status` = '{Status}', `exit_orderid` = '{ExitOrderId}', `oco_profit_leg_price` = {Profit.Value}, `oco_loss_leg_price` = {Risk.Value}, `oco_last_update` = CURRENT_TIMESTAMP() WHERE `id` = {Id}";
				}
				else if (Status == PositionStatus.ExitPriceReached)
				{
					query = $@"UPDATE `trades` SET `status` = '{Status}', `exited_quantity` = '{ExitQuantity}'";
				}
				else if (Status == PositionStatus.Exited)
				{
					Debug.Assert(Id.HasValue && Exit.HasValue);
					query = $@"UPDATE `trades` SET `status` = '{Status}', `exited_quantity` = '{ExitQuantity}', `exit_price` = {Exit.Value} WHERE `id` = {Id}";
				}

				Debug.Assert(!string.IsNullOrEmpty(query));

				var command = new MySqlCommand();
				command.CommandText = query;
				command.Connection = Program.Connection;
				command.ExecuteNonQuery();

				if (Status == PositionStatus.Started)
					Id = command.LastInsertedId;
			}
		}

		public static Position LoadSymbolPositions(string symbol)
		{
			lock (_locker)
			{
				string query = $"SELECT * FROM `trades` WHERE `symbol` LIKE '{symbol}' AND (`status` LIKE 'EntryOrderPlaced' OR `status` LIKE 'EntryOrderSucceeded' OR `status` LIKE 'ExitPriceReached' OR `status` LIKE 'StandingBy')";
				var command = new MySqlCommand();
				command.Connection = Program.Connection;
				command.CommandText = query;
				var reader = command.ExecuteReader();
				Position pos = null;

				if (reader.Read())
				{
					string orderid = reader.IsDBNull((int)TradesTable.OrderId) ? null : reader.GetString((int)TradesTable.OrderId);
					SignalType signal = (SignalType)Enum.Parse(typeof(SignalType), reader.GetString((int)TradesTable.Signal));
					decimal quantity = reader.GetDecimal((int)TradesTable.Quantity);
					decimal entry_price = reader.GetDecimal((int)TradesTable.EntryPrice);
					string account_type = reader.GetString((int)TradesTable.AccountType);
					decimal commission = reader.IsDBNull((int)TradesTable.Commission) ? 0m : reader.GetDecimal((int)TradesTable.Commission);
					string commission_asset = reader.IsDBNull((int)TradesTable.CommissionAsset) ? null : reader.GetString((int)TradesTable.CommissionAsset);
					long? exit_orderid = reader.IsDBNull((int)TradesTable.ExitOrderId) ? null : reader.GetInt64((int)TradesTable.ExitOrderId);
					Exchanges exchange = (Exchanges)Enum.Parse(typeof(Exchanges), reader.GetString((int)TradesTable.Exchange));
					DateTime date = DateTime.Parse(reader.GetString((int)TradesTable.Date));
					uint id = reader.GetUInt32((int)TradesTable.Id);
					decimal filled_quantity = reader.IsDBNull((int)TradesTable.FilledQuantity) ? 0m : reader.GetDecimal((int)TradesTable.FilledQuantity);
					PositionStatus status = (PositionStatus)Enum.Parse(typeof(PositionStatus), reader.GetString((int)TradesTable.Status));
					OHLCVInterval interval = (OHLCVInterval)Enum.Parse(typeof(OHLCVInterval), reader.GetString((int)TradesTable.Interval));
					decimal exited_quantity = reader.IsDBNull((int)TradesTable.ExitedQuantity) ? 0m : reader.GetDecimal((int)TradesTable.ExitedQuantity);
					decimal? risk_price = reader.IsDBNull((int)TradesTable.OcoLossLegPrice) ? null : reader.GetDecimal((int)TradesTable.OcoLossLegPrice);
					decimal? profit_price = reader.IsDBNull((int)TradesTable.OcoProfitLegPrice) ? null : reader.GetDecimal((int)TradesTable.OcoProfitLegPrice);
					DateTime? oco_last_update = reader.IsDBNull((int)TradesTable.OcoLastUpdate) ? null : DateTime.Parse(reader.GetString((int)TradesTable.OcoLastUpdate));
					pos = new Position(exchange, symbol, interval, signal, date, entry_price, quantity)
					{
						OrderId = orderid,
						AccountType = account_type,
						Commission = commission,
						Commission_Asset = commission_asset,
						mStatus = status,
						ExitOrderId = exit_orderid,
						Date = date,
						Id = id,
						FilledQuantity = filled_quantity,
						ExitQuantity = exited_quantity,
						Risk = risk_price,
						Profit = profit_price,
						LastOcoDate = oco_last_update,
						Interval = interval
					};
				}

				reader.Close();
				return pos;
			}
		}

		public void InitPos()
		{
			switch (Status)
			{
				case PositionStatus.EntryOrderPlaced:
					InitEntryOrderPlaced();
					break;
				case PositionStatus.EntryOrderSucceeded:
					InitEntryOrderSucceeded();
					break;
				case PositionStatus.ExitPriceReached:
					InitExitPriceReached();
					break;
				case PositionStatus.StandingBy:
					InitStandingBy();
					break;
			}
		}

		public void InitEntryOrderPlaced()
		{
			Debug.Assert(!string.IsNullOrEmpty(OrderId) && ProfitSetter != null && Risk.HasValue);
			// Check if order succeeded/filled. If it did, create exit order. If it didn't, set to EntryOrderFailed
			/*
			 * if(OrderSucceeded)
			 *		if(Order Would NOT have exited since it was ordered)
			 *			CreateExitOrder
			 *		else
			 *			MarketSell	
			 * else
			 *		Set as EntryOrderFailed
			 */

			var order = BinanceOrders.GetOrder(Symbol, OrderId);

			FilledQuantity = order.QuantityFilled;

			if(order.Status == OrderStatus.PartiallyFilled)
			{
				ExchangeTasks.CancelOrder(Exchanges.Binance, Symbol, OrderId);
			}

			if(FilledQuantity > 0m)
			{
				Status = PositionStatus.EntryOrderSucceeded;

				Profit = ExchangeTasks.GetTickSizeAdjustedValue(Exchange, Symbol, ProfitSetter.GetPrice(PriceHistory.Periods.Count - 1, Type));
				decimal quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchange, Symbol, FilledQuantity - Commission - ExitQuantity);
				var id = ExchangeTasks.CreateOco(Exchanges.Binance, Symbol, GenericOrderSide.Sell, quantity, Profit.Value, Risk.Value);

				if(id == -1)
				{
					if(ExchangeTasks.FindBalance(Exchange, Symbol, out var balance))
					{
						ExchangeTasks.PlaceOrder(Exchange, Symbol, balance, null, Functions.GetRandomString(15), GenericOrderType.Market, GenericOrderSide.Sell);
					}
					else
					{
						Console.WriteLine($"Could not find balance for symbol {Symbol}");
					}
				}
				else
				{
					ExitOrderId = id;
				}
			}
			else
			{
				Status = PositionStatus.EntryOrderFailed;
			}
		}

		public void InitEntryOrderSucceeded()
		{
			/*
			* 	if(Order Would NOT have exited since it was ordered)
			*		CreateExitOrder
			*	else
			*		MarketSell	
			*/
			// If the order is < 30 minutes old, create exit order. Else market sell and set as Exited
			OnStatusChange();
		}

		public void InitExitOrderPlaced()
		{
			Debug.Assert(BinanceMarket.ExchangeInfo != null);
			/*
			* if(ExitPriceReached)
			*		if(ExitOrderFilled)
			*			Set as Exited
			*		else
			*			Market sell (Will be set as exited in the stream func)
			*
			* else if(ExitOrderSuccesfullyPlaced)
			*		Recreate exit oco
			* else
			*		Check currently owned quantity of asset and create exit order again
			*/

			var info = ExchangeTasks.OcoExists(Exchange, Symbol);
			bool filled = false;
			decimal quantity = 0m;
			decimal price = 0m;
			if (info != null && info.Status == OcoStatus.Done)
			{
				foreach(var id in info.Orders)
				{
					var order = ExchangeTasks.GetOrderResult(Exchange, Symbol, null, id);
					quantity += order.QuantityFilled;
					if(order.Status == GenericOrderStatus.Filled)
					{
						filled = true;
					}

					if(order.Price.HasValue)
					{
						price = order.Price.Value;
					}
				}

				ExitQuantity += quantity;
				if (filled == true)
				{
					Exit = price;
					Status = PositionStatus.Exited;
				}
				else
				{
					Status = PositionStatus.ExitPriceReached;

					if (ExchangeTasks.FindBalance(Exchange, Symbol, out var balance))
					{
						ExchangeTasks.PlaceOrder(Exchange, Symbol, balance, null, Functions.GetRandomString(15), GenericOrderType.Market, GenericOrderSide.Sell);
					}
					else
					{
						Console.WriteLine($"Could not find balance for symbol {Symbol}");
					}
				}
			}
			else if(info != null && info.Status == OcoStatus.Started)
			{
				if(LastOcoDate + ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval) < DateTime.UtcNow)
				{
					Profit = ExchangeTasks.GetTickSizeAdjustedValue(Exchange, Symbol, ProfitSetter.GetPrice(PriceHistory.Periods.Count - 1, Type));
					decimal quant = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchange, Symbol, FilledQuantity - Commission - ExitQuantity);
					ExchangeTasks.CreateOco(Exchanges.Binance, Symbol, GenericOrderSide.Sell, quant, Profit.Value, Risk.Value);
				}
			}
			else
			{
				// Check currently owned quantity of asset and create exit order again
				if(!ExchangeTasks.FindBalance(Exchange, Symbol, out decimal balance))
				{
					Program.LogError($"Couldn't find balance for symbol {Symbol}");
				}
				ExchangeTasks.CancelAllOcosBySymbol(Exchange, Symbol);
				if(!ExchangeTasks.GetSymbolPrice(Exchange, Symbol, out decimal p))
				{
					Program.LogError($"Couldn't find price for symbol {Symbol}");
				}

				if(!ExchangeTasks.GetMinNotional(Exchange, Symbol, out decimal min_notional))
				{
					Program.LogError($"Couldn't find minnotional for symbol {Symbol}");
				}

				if (price * balance > min_notional)
				{
					ExchangeTasks.PlaceOrder(Exchange, Symbol, balance, null, Functions.GetRandomString(15), GenericOrderType.Market, GenericOrderSide.Sell);
				}
				else
				{
					Exit = price;
					Status = PositionStatus.Exited;
				}
			}
		}

		public void InitExitPriceReached()
		{
			/*
			* if(ExitOrderFilled)
			*		Set as Exited
			* else
			*		Market sell
			*/
			var info = ExchangeTasks.OcoExists(Exchange, Symbol);
			bool filled = false;
			decimal quantity = 0m;
			decimal price = 0m;
			if(info != null)
			{
				foreach (var id in info.Orders)
				{
					var order = ExchangeTasks.GetOrderResult(Exchange, Symbol, null, id);
					quantity += order.QuantityFilled;
					if (order.Status == GenericOrderStatus.Filled)
					{
						filled = true;
					}

					if (order.Price.HasValue)
					{
						price = order.Price.Value;
					}
				}
			}
			

			ExitQuantity += quantity;
			if (filled == true)
			{
				Exit = price;
				Status = PositionStatus.Exited;
			}
			else
			{
				if (ExchangeTasks.FindBalance(Exchange, Symbol, out var balance))
				{
					ExchangeTasks.GetMinNotional(Exchange, Symbol, out decimal min_notional);
					ExchangeTasks.GetSymbolPrice(Exchange, Symbol, out decimal avg_price);
					if(balance * avg_price > min_notional)
					{
						ExchangeTasks.PlaceOrder(Exchange, Symbol, balance, null, Functions.GetRandomString(15), GenericOrderType.Market, GenericOrderSide.Sell);
					}
					else
					{
						Exit = avg_price;
						Status = PositionStatus.Exited;
					}
				}
				else
				{
					Console.WriteLine($"Could not find balance for symbol {Symbol}");
				}
			}
		}

		public void InitStandingBy()
		{
			/*
			 * if(OCO was set to Done)
			 *		if(Quantity of asset was traded)
			 *			Set as Exited
			 *		else
			 *			Market sell
			 * else if(OCO was last created before the current candle)
			 *		Recreate OCO
			 */

			var info = ExchangeTasks.OcoExists(Exchange, Symbol);

			if(info != null && info.Status == OcoStatus.Done)
			{
				Status = PositionStatus.ExitPriceReached;
				InitExitPriceReached();
			}
			else if (LastOcoDate + ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval) < DateTime.UtcNow)
			{
				// Check currently owned quantity of asset and create exit order again
				if (!ExchangeTasks.FindBalance(Exchange, Symbol, out decimal balance))
				{
					Program.LogError($"Couldn't find balance for symbol {Symbol}");
				}
				
				if (!ExchangeTasks.GetSymbolPrice(Exchange, Symbol, out decimal price))
				{
					Program.LogError($"Couldn't find price for symbol {Symbol}");
				}

				if (!ExchangeTasks.GetMinNotional(Exchange, Symbol, out decimal min_notional))
				{
					Program.LogError($"Couldn't find minnotional for symbol {Symbol}");
				}

				ExchangeTasks.CancelAllOcosBySymbol(Exchange, Symbol);
				Console.WriteLine($"Price: {price}, Balance: {balance}, MinNotional: {min_notional}");
				ExitQuantity = FilledQuantity - balance;
				SaveToDb();
				if (price * balance > min_notional)
				{
					balance = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchange, Symbol, balance);
					Profit = ExchangeTasks.GetTickSizeAdjustedValue(Exchange, Symbol, ProfitSetter.GetPrice(PriceHistory.Periods.Count - 1, Type));
					ExchangeTasks.CreateOco(Exchange, Symbol, GenericOrderSide.Sell, balance, Profit.Value, Risk.Value);
				}
				else
				{
					Exit = price;
					Status = PositionStatus.Exited;
				}
			}
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

		public static bool PlaceOrder(Exchanges ex, string symbol, decimal quantity, decimal? price, string orderid, GenericOrderType type, GenericOrderSide side)
		{
			Debug.Assert(type == GenericOrderType.Limit && price.HasValue || type == GenericOrderType.Market && !price.HasValue);

			switch(ex)
			{
				case Exchanges.Binance:
					return BinanceOrders.PlaceOrder(
						symbol, 
						BinanceOrders.GenericOrderSideToBinanceOrderSide(side), 
						BinanceOrders.GetOrderType(type),
						orderid,
						0, 
						quantity,
						price.HasValue?price:null);
				default:
					return false;
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
					return new BinanceOHLCVCollection(symbol, interval, periods, callback, screener_update, start);
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
					throw new Exception("Don't know what to do here 28 30 31 30.5 idfk dumb calender");
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

		internal static long CreateOco(Exchanges exchange, string symbol, GenericOrderSide side, decimal quantity, decimal profit, decimal risk)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					var id = Functions.GetRandomString(15);
					OrderSide converted_side = Converter.FromOrderSide(side);
					return BinanceOrders.PlaceOcoOrder(symbol, profit, risk, 0, quantity, converted_side, id).Result;
				default:
					return -1;
			}
		}

		internal static void CancelOrder(Exchanges exchange, string symbol, string orderlistid)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					BinanceOrders.CancelByClientOrderId(symbol, orderlistid);
					break;
			}
		}

		internal static void CancelAllOrders(Exchanges exchange, string symbol)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					BinanceOrders.CancelAnyOrders(symbol);
					break;
			}
		}

		internal static void CancelOco(Exchanges exchange, string symbol, long id)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					BinanceOrders.CancelOcoByOrderListId(symbol, id);
					break;
			}
		}

		internal static void CancelAllOcosBySymbol(Exchanges exchange, string symbol, long? orderid = null, string id = null)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					BinanceOrders.CancelAllOcosForSymbol(symbol, orderid, id);
					break;
			}
		}

		internal static bool FindBalance(Exchanges exchange, string symbol, out decimal balance)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					var regex = new Regex(Program.GetConfigSetting("BINANCE_SYMBOL_REGEX"));
					var asset = regex.Replace(symbol, "");
					return BinanceAccount.GetBalance(asset, out balance);
				default:
					balance = default;
					return false;
			}
		}

		public static OcoInfo OcoExists(Exchanges exchange, string symbol = null, long? id = null)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					return OcoInfo.FromBinance(BinanceOrders.GetOcoOrder(id, symbol));
				default:
					return null;
			}
		}

		public static OrderResult GetOrderResult(Exchanges exchange, string symbol, string clientorderid, long? id = null)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					return OrderResult.FromBinance(BinanceOrders.GetOrder(symbol, clientorderid, id));
				default:
					return null;
			}
		}

		public static bool GetSymbolPrice(Exchanges exchange, string symbol, out decimal price)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					return BinanceMarket.GetSymbolPrice(symbol, out price);
				default:
					price = default;
					return false;
			}
		}

		internal static bool GetMinNotional(Exchanges exchange, string symbol, out decimal min_notional)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					Debug.Assert(BinanceMarket.ExchangeInfo != null);
					var seo = BinanceMarket.ExchangeInfo.Symbols.FirstOrDefault(s => s.Name.Equals(symbol));
					if(seo == default)
					{
						min_notional = default;
						return false;
					}
					else
					{
						min_notional = seo.MinNotionalFilter.MinNotional;
						return true;
					}
				default:
					min_notional = default;
					return false;
			}
		}

		public static void ForceSellAll(Exchanges exchange)
		{
			switch (exchange)
			{
				case Exchanges.Binance:
					BinanceOrders.MarketSell();
					break;
			}
		}
	}
}
