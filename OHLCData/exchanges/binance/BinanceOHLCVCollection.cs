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
		public string Name { get; set; }
		public bool CollectionFailed { get; set; }
		public CustomList<OHLCVPeriod> Data { get; set; }

		public BinanceOHLCVCollection(string name)
		{
			Name = name;
			CollectionFailed = false;
			Data = new CustomList<OHLCVPeriod>();
		}

		public OHLCVPeriod this[int index]
		{
			get => Data[index];
		}

		public void CollectOHLCV(OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, bool screener_updates, DateTime? start = null)
		{
			KlineInterval klv = ConvertToExchangeInterval(interval);

			Task.Run(() =>
			{
				// Subscribe to updates in new information
				if (screener_updates)
				{
					using (var socket_client = new BinanceSocketClient())
					{
						socket_client.Spot.SubscribeToKlineUpdatesAsync(Name, klv, OnKlineUpdate);
					}
				}

				// Download historical data
				using (var client = new BinanceClient())
				{
					DateTime dt = (start.HasValue) ? start.Value : DateTime.UtcNow - new TimeSpan(0, 1, 0);
					while (periods > 0)
					{
						int limit = (periods >= 1000) ? 1000 : periods;
						periods -= 1000;

						var data = client.Spot.Market.GetKlines(Name, klv, null, dt, limit);
						if (data.Success)
						{
							foreach (var candle in data.Data.Reverse())
							{
								Data.Insert(0, ConvertOHLCVPeriod(candle));
							}

							dt = Data[0].OpenTime.Subtract(new TimeSpan(0, 0, 1));
						}
						else
						{
							Console.WriteLine($"Error collecting symbol data for {Name}{data.Error}");
							CollectionFailed = true;
						}
					}
				}

				callback(this);
			});
		}

		private void OnKlineUpdate(IBinanceStreamKlineData obj)
		{
			if(obj.Data.Final == true)
			{
				if(Data.Count == 0 || (obj.Data.OpenTime - Data[Data.Count - 1].OpenTime == ExchangeTasks.GetOHLCVIntervalTimeSpan(ConvertToGeneralInterval(obj.Data.Interval))))
				{
					Data.Add(ConvertOHLCVPeriod(obj.Data));
				}
				else
				{
					if(Data[Data.Count - 1].OpenTime != obj.Data.OpenTime)
					{
						Program.Log($"Discrepancy in KLines: {Name} {Data[Data.Count - 1].OpenTime} {obj.Data.OpenTime}");
						CollectionFailed = true;
					}
				}
			}
		}


		public static KlineInterval ConvertToExchangeInterval(OHLCVInterval interval)
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

		public static OHLCVInterval ConvertToGeneralInterval(KlineInterval interval)
		{
			switch (interval)
			{
				case KlineInterval.EightHour:
					return OHLCVInterval.EightHour;
				case KlineInterval.FifteenMinutes:
					return OHLCVInterval.FifteenMinute;
				case KlineInterval.FiveMinutes:
					return OHLCVInterval.FiveMinute;
				case KlineInterval.FourHour:
					return OHLCVInterval.FourHour;
				case KlineInterval.OneDay:
					return OHLCVInterval.OneDay;
				case KlineInterval.OneHour:
					return OHLCVInterval.OneHour;
				case KlineInterval.OneMinute:
					return OHLCVInterval.OneMinute;
				case KlineInterval.OneMonth:
					return OHLCVInterval.OneMonth;
				case KlineInterval.OneWeek:
					return OHLCVInterval.OneWeek;
				case KlineInterval.SixHour:
					return OHLCVInterval.SixHour;
				case KlineInterval.ThirtyMinutes:
					return OHLCVInterval.ThirtyMinute;
				case KlineInterval.ThreeDay:
					return OHLCVInterval.ThreeDay;
				case KlineInterval.ThreeMinutes:
					return OHLCVInterval.ThreeMinute;
				case KlineInterval.TwelveHour:
					return OHLCVInterval.TwelveHour;
				case KlineInterval.TwoHour:
					return OHLCVInterval.TwoHour;
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
			//p.AssetVolume = period.QuoteVolume;

			return p;
		}
	}
}
