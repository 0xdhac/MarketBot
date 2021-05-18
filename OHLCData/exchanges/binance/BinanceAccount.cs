using Binance.Net;
using Binance.Net.Objects.Spot.WalletData;
using MarketBot.tools;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;

namespace MarketBot.exchanges.binance
{
	public static class BinanceAccount
	{
		public static void LoadPositions()
		{
			
		}

		public static bool GetBalance(string asset, out decimal total_balance)
		{
			using (var client = new BinanceClient())
			{
				var info = client.General.GetAccountInfo();

				foreach(var balance in info.Data.Balances)
				{
					if (balance.Asset.Equals(asset))
					{
						total_balance = balance.Total;
						return true;
					}
				}
			}

			total_balance = default;
			return false;
		}
	}
}
