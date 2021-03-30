using Binance.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.exchanges.binance
{
	public static class BinanceAccount
	{
		public static void LoadPositions()
		{
			using (var client = new BinanceClient())
			{
				//client.
				var orders = client.Spot.Order.GetOpenOcoOrders();
				//client.Brokerage.
				foreach(var oco_order in orders.Data)
				{
					new Position(Exchanges.Binance, oco_order.Symbol, 0, SignalType.Long, 0, 0, 0, 0, true);
					//new Po
				}
			}
		}
	}
}
