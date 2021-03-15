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
		public static async void PlaceOrder(Position pos, OrderSide side, OrderType type, int attempts, decimal? price = null)
		{
			using (var client = new BinanceClient())
			{
				//client.Margin.Borrow() <- BORROW SELL
				//client.Margin.Order.PlaceMarginOrder() BUY REPAY
				//client.Margin.Order.Place
				pos.Status = PositionStatus.Ordered;

				WebCallResult<BinancePlacedOrder> task = null;

				if(type == OrderType.Limit)
				{
					task = await client.Spot.Order.PlaceOrderAsync(
					pos.Symbol,
					side,
					OrderType.Limit,
					pos.Quantity,
					null,
					null,
					price.Value,
					TimeInForce.FillOrKill,
					null,
					null,
					OrderResponseType.Result,
					null,
					default);
				}
				else if(type == OrderType.Market)
				{
					task = await client.Spot.Order.PlaceOrderAsync(
					pos.Symbol,
					side,
					OrderType.Market,
					pos.Quantity,
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
				Console.WriteLine($"[{pos.Symbol}] Placing order.");

				if (!task.Success)
				{
					Console.WriteLine($"[{pos.Symbol}] {task.Error}");
					Program.Log($"[{pos.Symbol}] {task.Error}");
				}
			}
		}

		
		public static async void PlaceOcoOrder(Position pos, int attempts)
		{
			if (pos.Status != PositionStatus.Filled)
			{
				throw new Exception("Attempt to place OCO order on unfilled order.");
			}

			using (var client = new BinanceClient())
			{
				decimal tick_size = 0;
				foreach(var symbol in BinanceMarket.ExchangeInfo.Symbols)
				{
					if(symbol.Name == pos.Symbol)
					{
						tick_size = symbol.PriceFilter.TickSize;
					}
				}

				if(tick_size == 0)
				{
					throw new Exception($"Error: TickSize not found for {pos.Symbol}");
				}
				//Console.WriteLine($"{pos.Symbol}: Quantity: {pos.Quantity}, Profit: {pos.Profit}, Risk: {pos.Risk}");

				Console.WriteLine($"[{pos.Symbol}] Placing OCO order.");
				var result = await client.Spot.Order.PlaceOcoOrderAsync(
						pos.Symbol,
						OrderSide.Sell,
						pos.Quantity,
						pos.Profit,
						pos.Risk,
						pos.Risk - (tick_size * 10),
						null,
						null,
						null,
						null,
						null,
						TimeInForce.FillOrKill);

				pos.Status = PositionStatus.Oco;

				if (!result.Success)
				{
					if(result.Error.Code == -2010)
					{
						Console.WriteLine($"[{pos.Symbol}] Oco stop limit failed to exit immediately. Market selling instead.");
						var task = await client.Spot.Order.PlaceOrderAsync(
							pos.Symbol,
							OrderSide.Sell,
							OrderType.Market,
							pos.Quantity,
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
						PlaceOcoOrder(pos, attempts + 1);
					}
					else
					{
						Program.LogError(result.ResponseStatusCode + " " + result.Error);

						ConsoleColor color = Console.ForegroundColor;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(result.ResponseStatusCode + " " + result.Error);
						Console.ForegroundColor = color;
					}
				}
			}
		}
	}
}
