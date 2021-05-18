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
using MarketBot.interfaces;

namespace MarketBot
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

		public static BinanceOrder GetOrder(string symbol, string clientorderid = null, long? orderid = null)
		{
			using (var client = new BinanceClient())
			{
				if (!string.IsNullOrEmpty(clientorderid))
				{
					return client.Spot.Order.GetOrder(symbol, null, clientorderid).Data;
				}
				else if (orderid.HasValue)
				{
					return client.Spot.Order.GetOrder(symbol, orderid).Data;
				}
				else
				{
					throw new Exception("ClientOrderId and OrderId both null");
				}
				
			}
		}

		public static BinanceOrderOcoList GetOcoOrder(long? id, string symbol = null)
		{
			if(id.HasValue)
			{
				using (var client = new BinanceClient())
				{
					var list = client.Spot.Order.GetOcoOrder(id).Data;
					return list;
				}
			}
			else if(!string.IsNullOrEmpty(symbol))
			{
				return GetOcoOrderBySymbol(symbol);
			}
			else
			{
				throw new Exception("Not found: id or symbol");
			}
		}

		public static BinanceOrderOcoList GetOcoOrderBySymbol(string symbol)
		{
			using (var client = new BinanceClient())
			{
				var list = client.Spot.Order.GetOpenOcoOrders();
				foreach(var order in list.Data)
				{
					if(order.Symbol == symbol)
					{
						return order;
					}
				}

				return null;
			}
		}

		public static bool PlaceOrder(string symbol, OrderSide side, OrderType type, string orderid, int attempts, decimal quantity, decimal? price = null, params int[] ignored_errors)
		{
			// Selling: pos.Filled
			using (var client = new BinanceClient())
			{
				//client.Spot.
				//client.Margin.Borrow() <- BORROW SELL
				//client.Margin.Order.PlaceMarginOrder() BUY REPAY
				//client.Margin.Order.Place

				WebCallResult<BinancePlacedOrder> task = null;

				if(type == OrderType.Limit)
				{
					//Console.WriteLine(price.Value);
					task = client.Spot.Order.PlaceOrder(
					symbol,
					side,
					OrderType.Limit,
					quantity,
					null,
					orderid,
					price.Value,
					TimeInForce.GoodTillCancel,
					null,
					null,
					OrderResponseType.Result,
					null,
					default);
				}
				else if(type == OrderType.Market)
				{
					task = client.Spot.Order.PlaceOrder(
					symbol,
					side,
					type,
					quantity,
					null,
					orderid,
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
						Console.WriteLine($"[{symbol}] {task.Error}");
						Program.Log($"[{symbol}] {task.ResponseStatusCode}");

						if (int.TryParse(Program.GetConfigSetting("MAX_ORDER_ATTEMPTS"), out int max_attempts))
						{
							if (attempts + 1 < max_attempts)
							{
								return PlaceOrder(symbol, side, type, orderid, attempts + 1, quantity, price);
							}
						}
						else
						{
							Program.LogError($"Invalid setting for MAX_ORDER_ATTEMPTS");
						}
					}
				}
				else
				{
					Console.WriteLine($"[{symbol}] Order placed.");
					return true;
				}
			}

			return false;
		}

		
		public static async Task<long> PlaceOcoOrder(string symbol, decimal profit, decimal risk, int attempts, decimal quantity, OrderSide side, string id, params int[] ignored_errors)
		{
			using (var client = new BinanceClient())
			{
				decimal tick_size = 0;
				if(!BinanceMarket.GetTickSize(symbol, out tick_size))
				{
					throw new Exception($"Error: Symbol not found {symbol}");
				}

				if(tick_size == 0)
				{
					throw new Exception($"Error: TickSize not found for {symbol}");
				}
				Console.WriteLine($"[{symbol}] Quantity: {quantity}, Profit: {profit}, Risk: {risk}");

				var result = await client.Spot.Order.PlaceOcoOrderAsync(
						symbol,
						side,
						quantity,
						profit,
						risk,
						risk,
						id,
						null,
						null,
						null,
						null,
						TimeInForce.GoodTillCancel);

				if (!result.Success)
				{
					/*if(result.Error.Code == -2010)
					{
						var task = await client.Spot.Order.PlaceOrderAsync(
							symbol,
							side,
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
							Console.WriteLine($"[{symbol}] {task.Error}");
						}
					}
					else */
					if(result.Error.Code == -1001)
					{
						Console.WriteLine($"[{symbol}] Error connecting to Binance API server. Retrying OCO");
						if (int.TryParse(Program.GetConfigSetting("MAX_OCO_ORDER_ATTEMPTS"), out int max_attempts))
						{
							if (attempts + 1 < max_attempts)
							{
								return await PlaceOcoOrder(symbol, profit, risk, attempts + 1, quantity, side, id);
							}
						}
						else
						{
							return -1;
						}
					}
					else
					{
						Console.WriteLine($"[{symbol}] OCO Order failed ({result.Error.Code}: {result.Error.Message}): Starting attempt #{attempts + 2}");
						Program.LogError($"{result.Error} {result.ResponseStatusCode}");

						if (int.TryParse(Program.GetConfigSetting("MAX_OCO_ORDER_ATTEMPTS"), out int max_attempts))
						{
							if (attempts + 1 < max_attempts)
							{
								return await PlaceOcoOrder(symbol, profit, risk, attempts + 1, quantity, side, id);
							}
						}
						else
						{
							Program.LogError($"Invalid setting for MAX_OCO_ORDER_ATTEMPTS");
							return -1;
						}
					}
				}
				else
				{
					return result.Data.OrderListId;
				}
			}

			return -1;
		}

		public static async void MarketSell()
		{
			if(BinanceMarket.ExchangeInfo == null)
			{
				BinanceMarket.UpdateExchangeInfo();
			}
			using (var client = new BinanceClient())
			{
				var spot_result = await client.General.GetAccountInfoAsync();
				var prices = await client.Spot.Market.GetPricesAsync();
				foreach (var result in spot_result.Data.Balances)
				{
					var symbol = BinanceMarket.ExchangeInfo.Symbols.FirstOrDefault(s => s.Name.Equals($"{result.Asset}USDT"));
					if (symbol == default)
						continue;

					try
					{
						client.Spot.Order.CancelAllOpenOrders($"{result.Asset}USDT");
						var price = prices.Data.First(p => p.Symbol.Equals($"{result.Asset}USDT"));
						if (!ExchangeTasks.GetMinNotional(Exchanges.Binance, $"{result.Asset}USDT", out decimal min_notional))
						{
							Program.LogError($"Couldn't find minnotional for symbol {result.Asset}USDT");
						}

						if(price.Price * result.Total > min_notional)
						{
							var quantity = ExchangeTasks.GetStepSizeAdjustedQuantity(Exchanges.Binance, $"{result.Asset}USDT", result.Total);
							if(PlaceOrder($"{result.Asset}USDT", OrderSide.Sell, OrderType.Market, Functions.GetRandomString(15), 0, quantity))
							{
								Console.WriteLine($"Sold asset {result.Asset}");
							}
						}
					}
					catch(Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				}
			}
		}

		public static void CancelAnyOrders(string symbol)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelAllOpenOrders(symbol);
			}
		}

		internal static void CancelByClientOrderId(string symbol, string id)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelOrder(symbol, null, id);
			}
		}

		internal static void CancelByOrderId(string symbol, long id)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelOrder(symbol, id, null);
			}
		}

		internal static void CancelOcoByClientOrderId(string symbol, string id)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelOcoOrder(symbol, null, id);
			}
		}

		internal static void CancelOcoByOrderListId(string symbol, long id)
		{
			using (var client = new BinanceClient())
			{
				client.Spot.Order.CancelOcoOrder(symbol, id, null);
			}
		}

		public static void CancelAllOcosForSymbol(string symbol, long? orderid = null, string exitid = null)
		{
			using (var client = new BinanceClient())
			{
				if(orderid.HasValue)
				{
					client.Spot.Order.CancelOcoOrder(symbol, orderid);
				}
				else if (!string.IsNullOrEmpty(exitid))
				{
					client.Spot.Order.CancelOcoOrder(symbol, null, exitid);
				}
				else
				{
					var orders = client.Spot.Order.GetOcoOrders();
					foreach(var order in orders.Data)
					{
						if (order.Symbol.Equals(symbol))
						{
							client.Spot.Order.CancelOcoOrder(symbol, order.OrderListId);
						}
					}
				}
			}
		}

		public static IEnumerable<BinanceOrder> GetOrders(string symbol)
		{
			using (var client = new BinanceClient())
			{
				var orders = client.Spot.Order.GetAllOrders(symbol);

				return orders.Data;
			}
		}

		public static OrderSide GenericOrderSideToBinanceOrderSide(GenericOrderSide side)
		{
			switch (side)
			{
				case GenericOrderSide.Buy:
					return OrderSide.Buy;
				case GenericOrderSide.Sell:
					return OrderSide.Sell;
				default:
					throw new ArgumentException();
			}
		}
	}

	public partial class OcoInfo
	{
		public static OcoInfo FromBinance(BinanceOrderOcoList list)
		{
			if (list == null)
				return null;

			var converted_order_list = new List<long>();
			foreach(var order in list.Orders)
			{
				converted_order_list.Add(order.OrderId);
			}

			OcoStatus status = list.ListOrderStatus == ListOrderStatus.Done ? OcoStatus.Done : list.ListOrderStatus == ListOrderStatus.Executing ? OcoStatus.Started : OcoStatus.Rejected;
			return new OcoInfo()
			{
				Status = status,
				Orders = converted_order_list
			};
		}
	}

	public partial class OrderResult
	{
		public static OrderResult FromBinance(BinanceOrder order)
		{
			return new OrderResult()
			{
				Exchange = Exchanges.Binance,
				QuantityFilled = order.QuantityFilled,
				OrderSide = Converter.ToOrderSide(order.Side),
				OrderType = Converter.ToOrderType(order.Type),
				Symbol = order.Symbol,
				OrderListId = order.OrderListId,
				OrderId = order.OrderId,
				Status = Converter.ToOrderStatus(order.Status),
				Price = order.AverageFillPrice
			};
		}
	}
}
