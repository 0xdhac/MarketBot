using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using System.Text.RegularExpressions;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.MarketData;

namespace MarketBot.exchanges.binance
{
	static class BinanceMarket
	{
		public static BinanceExchangeInfo ExchangeInfo = null;
		public static void UpdateExchangeInfo()
		{
			using (var client = new BinanceClient())
			{
				var result = client.Spot.System.GetExchangeInfo();
				ExchangeInfo = result.Data;
			}
		}

		public static bool GetStepSizeAdjustedQuantity(string symbol, decimal quantity, out decimal result)
		{
			if(ExchangeInfo == null)
			{
				throw new Exception("GetLotSizeAdjustedQuanity failed. Must call UpdateExchangeInfo before this function.");
			}

			foreach(var item in ExchangeInfo.Symbols)
			{
				if(item.Name == symbol)
				{
					//int places = (1 / item.LotSizeFilter.StepSize).ToString().Count((c) => c == '0');
					result = Math.Floor((quantity) * (1 / item.LotSizeFilter.StepSize)) / (1 / item.LotSizeFilter.StepSize);
					return true;
				}
			}

			result = default;
			return true;
		}

		public static bool GetTickSizeAdjustedQuantity(string symbol, decimal quantity, out decimal result)
		{
			if (ExchangeInfo == null)
			{
				throw new Exception("GetLotSizeAdjustedQuanity failed. Must call UpdateExchangeInfo before this function.");
			}

			foreach (var item in ExchangeInfo.Symbols)
			{
				if (item.Name == symbol)
				{
					//int places = (1 / item.LotSizeFilter.StepSize).ToString().Count((c) => c == '0');
					result = Math.Floor((quantity) * (1 / item.PriceFilter.TickSize)) / (1 / item.PriceFilter.TickSize);
					return true;
				}
			}

			result = default;
			return true;
		}

		public static List<string> GetTradingPairs(string symbol_regex)
		{
			using (var client = new BinanceClient())
			{
				List<string> output = new List<string>();
				var info = client.Spot.System.GetExchangeInfo();
				var data = info.Data;

				Regex r = new Regex(symbol_regex);
				foreach (var symbol in data.Symbols)
				{
					if (r.IsMatch(symbol.Name) &&
						symbol.Permissions.Contains(AccountType.Spot) &&
						symbol.Status == SymbolStatus.Trading &&
						!Blacklist.IsBlacklisted(Exchanges.Binance, symbol.Name))
					{
						output.Add(symbol.Name);
					}
				}

				return output;
			}
		}
	}
}
