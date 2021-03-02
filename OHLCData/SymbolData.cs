using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net;

namespace MarketBot
{
	public class SymbolData
	{
		public delegate void FinishedCallback(SymbolData symbolData);
		private FinishedCallback m_Callback;
		public List<IBinanceKline> Data = new List<IBinanceKline>();

		public SymbolData(string symbol, KlineInterval interval, int periods, FinishedCallback callback)
		{
			m_Callback = callback;

			Collect(symbol, interval, periods);
		}
		public void Collect(string symbol, KlineInterval interval, int periods)
		{
			Task.Run(() =>
			{
				using (var client = new BinanceClient())
				{
					DateTime dt = DateTime.Now;
					while (periods > 0)
					{
						int limit = (periods >= 1000) ? 1000 : periods;
						periods -= 1000;
						
						var data = client.Spot.Market.GetKlines(symbol, interval, null, dt, limit);
						foreach(var candle in data.Data.Reverse())
						{
							Data.Add(candle);
						}

						dt = Data[Data.Count - 1].OpenTime - new TimeSpan(0, 0, 1);
					}
				}

				m_Callback(this);
			});
		}
	}
}
