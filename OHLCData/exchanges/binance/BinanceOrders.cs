using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;
using MarketBot.tools;

namespace MarketBot.exchanges.binance
{
	public static class BinanceOrders
	{
		public static GenericOrderType GetGenericOrderType(OrderType type)
		{
			switch (type)
			{
				case OrderType.Limit:
					return GenericOrderType.Market;
				case OrderType.Market:
					return GenericOrderType.Limit;
				default:
					throw new Exception($"OrdeType {type} not supported yet.");
			}
		}

		public static OrderType GetOrderType(GenericOrderType type)
		{
			switch (type)
			{
				case GenericOrderType.Limit:
					return OrderType.Limit;
				case GenericOrderType.Market:
					return OrderType.Market;
				default:
					throw new Exception($"OrdeType {type} not supported yet.");
			}
		}

		public static async void PlaceOrder(Position pos, OrderSide side, OrderType type, int attempts, decimal quantity, decimal? price = null, params int[] ignored_errors)
		{
			// Selling: pos.Filled
			using (var client = new BinanceClient())
			{
				//client.Margin.Borrow() <- BORROW SELL
				//client.Margin.Order.PlaceMarginOrder() BUY REPAY
				//client.Margin.Order.Place

				
				pos.Status = PositionStatus.Ordered;

				WebCallResult<BinancePlacedOrder> task = null;

				if(type == OrderType.Limit)
				{
					//Console.WriteLine(price.Value);
					task = await client.Spot.Order.PlaceOrderAsync(
					pos.Symbol,
					side,
					OrderType.Limit,
					quantity,
					null,
					null,
					price.Value,
					TimeInForce.GoodTillCancel,
					null,
					null,
					OrderResponseType.Result,
					null,
					default);
				}
				else if(type == OrderType.Market || type == OrderType.StopMarket)
				{
					task = await client.Spot.Order.PlaceOrderAsync(
					pos.Symbol,
					side,
					type,
					quantity,
					null,
					null,
					null,
					null,
					null,
					null,
					OrderResponseType.Result,
					null,
					default);
				}
				else
				{
					throw new Exception($"{type} not allowed.");
				}

				if (!task.Success)
				{
					if(task.Error.Code.HasValue && ignored_errors.Contains(task.Error.Code.Value))
					{
						Console.WriteLine($"[{pos.Symbol}] {task.Error}");
						Program.Log($"[{pos.Symbol}] {task.ResponseStatusCode}");

						string setting = Program.GetConfigSetting("MAX_ORDER_ATTEMPTS");
						bool success = int.TryParse(setting, out int max_attempts);

						if (success)
						{
							if (attempts + 1 < max_attempts)
							{
								PlaceOrder(pos, side, type, attempts + 1, quantity, price);
							}
						}
						else
						{
							Program.LogError($"Invalid setting for MAX_ORDER_ATTEMPTS: {setting}");
						}
					}
				}
				else
				{
					Console.WriteLine($"[{pos.Symbol}] Order placed.");
				}
			}
		}

		
		public static async void PlaceOcoOrder(Position pos, int attempts, decimal quantity, params int[] ignored_errors)
		{
			if (pos.Status != PositionStatus.Filled)
			{
				throw new Exception("Attempt to place OCO order on unfilled order.");
			}

			using (var client = new BinanceClient())
			{
				decimal tick_size = 0;
				if(!BinanceMarket.GetTickSize(pos.Symbol, out tick_size))
				{
					throw new Exception($"Error: Symbol not found {pos.Symbol}");
				}

				if(tick_size == 0)
				{
					throw new Exception($"Error: TickSize not found for {pos.Symbol}");
				}
				Console.WriteLine($"[{pos.Symbol}] Quantity: {pos.Quantity}, Profit: {pos.Profit}, Risk: {pos.Risk}");

				pos.Status = PositionStatus.Oco;
				Console.WriteLine($"[{pos.Symbol}] Placing OCO order.");
				var result = await client.Spot.Order.PlaceOcoOrderAsync(
						pos.Symbol,
						OrderSide.Sell,
						quantity,
						pos.Profit,
						pos.Risk,
						pos.Risk - (tick_size * 10),
						null,
						null,
						null,
						null,
						null,
						TimeInForce.GoodTillCancel);

				if (!result.Success)
				{
					//if()
					if(result.Error.Code == -2010)
					{
						var task = await client.Spot.Order.PlaceOrderAsync(
							pos.Symbol,
							OrderSide.Sell,
							OrderType.Market,
							quantity,
							null,
							null,
							null,
							null,
							null,
							null,
							OrderResponseType.Result,
							null,
							default);

						if (!task.Success)
						{
							Console.WriteLine($"[{pos.Symbol}] {task.Error}");
						}
					}
					else if(result.Error.Code == -1001)
					{
						Console.WriteLine($"[{pos.Symbol}] Error connecting to Binance API server. Retrying OCO");
						PlaceOcoOrder(pos, attempts + 1, quantity);
					}
					else
					{
						Console.WriteLine($"[{pos.Symbol}] OCO Order failed: Starting attempt #{attempts + 2}");
						Program.LogError($"{result.Error} {result.ResponseStatusCode}");

						string setting = Program.GetConfigSetting("MAX_OCO_ORDER_ATTEMPTS");
						bool success = int.TryParse(setting, out int max_attempts);

						if (success)
						{
							if (attempts + 1 < max_attempts)
							{
								PlaceOcoOrder(pos, attempts + 1, quantity);
							}
						}
						else
						{
							Program.LogError($"Invalid setting for MAX_OCO_ORDER_ATTEMPTS: {setting}");
						}
					}
				}
			}
		}

		public static void CancelAnyOrders(string symbol, OrderType type, OrderSide side)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelAllOpenOrders(symbol);
			}
		}
	}
}
