using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.UserStream;
using CryptoExchange.Net;
using CryptoExchange.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.tools
{
	public static class APITesting
	{
		public static bool Enabled = false;
		static BinanceSocketClient SocketClient = null;
		static UpdateSubscription Subscription = null;

		public static void StartTester()
		{
			Enabled = true;
			Program.Print("Starting API Tester");
			CreateUserStream();
		}

		public static void StopTester()
		{
			Enabled = false;
			Program.Print("Stopping API Tester");
			EndUserStream();
		}

		public async static void CreateUserStream()
		{
			if (SocketClient != null)
			{
				await SocketClient.Unsubscribe(Subscription);
			}

			using (var client = new BinanceClient())
			{
				var key = client.Spot.UserStream.StartUserStream().Data;

				SocketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
				{
					AutoReconnect = true,
					ReconnectInterval = TimeSpan.FromSeconds(5),
				});

				var result = await SocketClient.Spot.SubscribeToUserDataUpdatesAsync(key, OrderUpdate, OcoOrderUpdate, PositionUpdate, BalanceUpdate);

				Subscription = result.Data;

				Subscription.ConnectionLost += Data_ConnectionLost;
				Subscription.ConnectionRestored += Data_ConnectionRestored;
				Subscription.ActivityPaused += Data_ActivityPaused;
				Subscription.ActivityUnpaused += Data_ActivityUnpaused;
			}
		}

		private static void EndUserStream()
		{
			SocketClient.UnsubscribeAll();
		}

		public static void CancelOco(string symbol, string id)
		{
			using (var client = new BinanceClient())
			{
				var result = client.Spot.Order.CancelOcoOrder(symbol, null, id);
				//client.Spot.Order.
				if (!result.Success)
				{
					Console.WriteLine(result.Error);
				}
			}
		}

		public static void CancelOrder(string symbol, long id)
		{
			using (var client = new BinanceClient())
			{
				throw new NotImplementedException();
				var result = client.Spot.Order.CancelOrder(symbol, id, null);
				//client.Spot.Order.
				if (!result.Success)
				{
					Console.WriteLine(result.Error);
				}
			}
		}

		public static void ListOcoOrders()
		{
			using (var client = new BinanceClient())
			{
				var result = client.Spot.Order.GetOcoOrders();
				//client.Spot.Order.
				if (!result.Success)
				{
					Console.WriteLine(result.Error);
				}
				else
				{
					foreach(var order in result.Data)
					{
						Console.WriteLine($"[{order.Symbol}] - ListOrderId: {order.OrderListId} Status: {order.ListOrderStatus}");
					}
				}
			}
		}

		public static void ListOpenOcoOrders()
		{
			using (var client = new BinanceClient())
			{
				var result = client.Spot.Order.GetOpenOcoOrders();

				if (!result.Success)
				{
					Console.WriteLine(result.Error);
				}
				else
				{
					foreach (var order in result.Data)
					{
						Console.WriteLine($"[{order.Symbol}] - ListOrderId: {order.OrderListId} Status: {order.ListOrderStatus}");
					}
				}
			}
		}

		public static void CreateTestOrder(string symbol, decimal bet_amount)
		{
			
		}

		private static void Data_ActivityUnpaused()
		{
			
		}

		private static void Data_ActivityPaused()
		{
			
		}

		private static void Data_ConnectionRestored(TimeSpan obj)
		{
			
		}

		private static void Data_ConnectionLost()
		{
			
		}

		private static void BalanceUpdate(BinanceStreamBalanceUpdate obj)
		{
			
		}

		private static void PositionUpdate(BinanceStreamPositionsUpdate obj)
		{
			foreach(var balance in obj.Balances)
			{
				Console.WriteLine($"PositionUpdate: ([{balance.Asset}] Locked: {balance.Locked} Free: {balance.Free} Total: {balance.Total})");
			}
		}

		private static void OcoOrderUpdate(BinanceStreamOrderList obj)
		{
			Console.WriteLine($"OcoOrderUpdate: ([{obj.Symbol}] ListClientOrderId: {obj.ListClientOrderId}, OrderListId: {obj.OrderListId}, ListOrderStatus: {obj.ListOrderStatus}, ListStatusType: {obj.ListStatusType})");
		}

		private static void OrderUpdate(BinanceStreamOrderUpdate obj)
		{
			Console.WriteLine($"OrderUpdate: ([{obj.Symbol}] Status: {obj.Status}, ClientOrderId: {obj.ClientOrderId}, OrderListId: {obj.OrderListId}, OrderId: {obj.OrderId}, ExecutionType: {obj.ExecutionType}, {obj.CreateTime}, {obj.UpdateTime})");
		}
	}
}
