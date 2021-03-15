using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Interfaces.SubClients.Margin;
using Binance.Net.Interfaces.SubClients.Futures;
using Binance.Net.Enums;
using MarketBot.interfaces;
using System.Text.RegularExpressions;

namespace MarketBot.exchanges.binance
{
	public class BinanceWallet : Wallet
	{
		public BinanceWallet(Exchanges ex) : base(ex) { }

		public override void UpdateBalance()
		{
			Task.Run(async () =>
			{
				using (var client = new BinanceClient())
				{
					Total = 0;
					var spot_result = await client.General.GetAccountInfoAsync();
					var margin_result = await client.Margin.GetMarginAccountInfoAsync();
					var prices = await client.Spot.Market.GetPricesAsync();

					foreach(var balance in spot_result.Data.Balances)
					{
						foreach (var price in prices.Data)
						{
							string match = balance.Asset + "USDT";

							if (price.Symbol == match)
							{
								
								Total += balance.Total * price.Price;
							}
						}

						if (balance.Asset == "USDT")
						{
							Available = balance.Free;
							Total += balance.Total;
						}
					}

					base.UpdateBalance();
				}
			});

			
		}
	}
}
