using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net;
using Binance.Net.Interfaces;

namespace MarketBot
{
	class BinanceOHLCVCollection : IExchangeOHLCVCollection
	{
		public CustomList<OHLCVPeriod> Data { get; set; }

		public BinanceOHLCVCollection()
		{
			Data = new CustomList<OHLCVPeriod>();
		}

		public void CollectOHLCV(string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback)
		{
			KlineInterval klv = ConvertExchangeInterval(interval);
			Task.Run(() =>
			{
				using (var client = new BinanceClient())
				{
					DateTime dt = DateTime.UtcNow;
					while (periods > 0)
					{
						int limit = (periods >= 1000) ? 1000 : periods;
						periods -= 1000;

						var data = client.Spot.Market.GetKlines(symbol, klv, null, dt, limit);
						foreach (var candle in data.Data)
						{
							Data.Add(ConvertOHLCVPeriod(candle));
						}

						dt = Data[Data.Count - 1].OpenTime.Subtract(new TimeSpan(0, 0, 1));
					}
				}

				callback(this);
			});
		}

		public KlineInterval ConvertExchangeInterval(OHLCVInterval interval)
		{
			switch (interval)
			{
				case OHLCVInterval.EightHour:
					return KlineInterval.EightHour;
				case OHLCVInterval.FifteenMinute:
					return KlineInterval.FifteenMinutes;
				case OHLCVInterval.FiveMinute:
					return KlineInterval.FiveMinutes;
				case OHLCVInterval.FourHour:
					return KlineInterval.FourHour;
				case OHLCVInterval.OneDay:
					return KlineInterval.OneDay;
				case OHLCVInterval.OneHour:
					return KlineInterval.OneHour;
				case OHLCVInterval.OneMinute:
					return KlineInterval.OneMinute;
				case OHLCVInterval.OneMonth:
					return KlineInterval.OneMonth;
				case OHLCVInterval.OneWeek:
					return KlineInterval.OneWeek;
				case OHLCVInterval.SixHour:
					return KlineInterval.SixHour;
				case OHLCVInterval.ThirtyMinute:
					return KlineInterval.ThirtyMinutes;
				case OHLCVInterval.ThreeDay:
					return KlineInterval.ThreeDay;
				case OHLCVInterval.ThreeMinute:
					return KlineInterval.ThreeMinutes;
				case OHLCVInterval.TwelveHour:
					return KlineInterval.TwelveHour;
				case OHLCVInterval.TwoHour:
					return KlineInterval.TwoHour;
				default:
					throw new Exception("Invalid OHLCVInterval. Binance does not support this interval.");

			}
		}

		OHLCVPeriod ConvertOHLCVPeriod(IBinanceKline period)
		{
			OHLCVPeriod p = new OHLCVPeriod();
			p.Open = period.Open;
			p.High = period.High;
			p.Low = period.Low;
			p.Close = period.Close;
			p.Volume = period.BaseVolume;
			p.OpenTime = period.OpenTime;
			p.CloseTime = period.CloseTime;

			return p;
		}
	}
}
