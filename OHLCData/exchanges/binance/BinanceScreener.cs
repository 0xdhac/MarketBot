using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;

namespace MarketBot.exchanges.binance
{
	public class BinanceScreener
	{
		// Only want symbols that aren't part of the blacklisted symbol list
		public static void Screen(string symbol_regex, KlineInterval interval, PeriodCloseCallback callback)
		{
			using (var client = new BinanceClient())
			{
				var info = client.Spot.System.GetExchangeInfo();
				var data = info.Data;

				Regex r = new Regex(symbol_regex);
				foreach(var symbol in data.Symbols)
				{
					//SymbolStatus.
					//symbol.Status
					if (r.IsMatch(symbol.Name) && symbol.Permissions.Contains(AccountType.Spot) && symbol.Status == SymbolStatus.Trading)
					{
						using (var socket_client = new BinanceSocketClient())
						{
							socket_client.Spot.SubscribeToKlineUpdatesAsync(symbol.Name, interval, OnKlineUpdate);
						}
					}
				}
			}
		}

		private static void OnKlineUpdate(IBinanceStreamKlineData data)
		{
			if (data.Data.Final)
			{
				Console.WriteLine($"{data.Symbol}");
			}
		}
	}
}
