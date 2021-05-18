using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Sockets;

namespace MarketBot
{
	class BinanceOHLCVCollection : IExchangeOHLCVCollection
	{
		public string Name { get; set; }
		private bool mCollectionFailed = false;
		public bool CollectionFailed 
		{
			get
			{
				return mCollectionFailed;
			}
			set
			{
				if(mCollectionFailed != value && value == false)
				{
					CollectApiOHLCV();
				}
				mCollectionFailed = value;
			}
		}
		private bool FinishedDownloading = false;
		public HList<OHLCVPeriod> Periods { get; set; }
		BinanceSocketClient SocketClient = null;
		UpdateSubscription Subscription = null;
		public bool Connected = false;
		public OHLCVInterval Interval { get; set; }
		public int PeriodCount { get; set; }
		public DateTime? StartDate { get; set; } = null;
		public bool SubscribeToLiveUpdates { get; set; } = false;
		public bool AutoRedownloadOnCollectionFailed { get; set; } = true;
		private Action<IExchangeOHLCVCollection> CollectionFinishedCallback;

		public BinanceOHLCVCollection(string name, OHLCVInterval interval, int periods, Action<IExchangeOHLCVCollection> callback, bool screener_updates, DateTime? start = null)
		{
			Name = name;
			Interval = interval;
			Periods = new HList<OHLCVPeriod>();
			PeriodCount = periods;
			SubscribeToLiveUpdates = screener_updates;
			StartDate = start;
			CollectionFinishedCallback = callback;

			CollectApiOHLCV();
		}

		public OHLCVPeriod this[int index]
		{
			get => Periods[index];
		}

		public void CollectApiOHLCV()
		{
			CollectionFailed = false;
			KlineInterval klv = ConvertToExchangeInterval(Interval);

			// Subscribe to updates in new information
			if (SubscribeToLiveUpdates)
			{
				TryToSubscribe(Interval);
			}

			var periods = PeriodCount;
			// Download historical data
			using (var client = new BinanceClient())
			{
				DateTime dt = (StartDate.HasValue) ? StartDate.Value : DateTime.UtcNow - ExchangeTasks.GetOHLCVIntervalTimeSpan(Interval);
				while (periods > 0)
				{
					int limit = (periods >= 1000) ? 1000 : periods;
					periods -= 1000;

					var data = client.Spot.Market.GetKlines(Name, klv, null, dt, limit);
					if (data.Success)
					{
						foreach (var candle in data.Data.Reverse())
						{
							Periods.Insert(0, ConvertOHLCVPeriod(candle));
						}

						dt = Periods[0].Date.Subtract(new TimeSpan(0, 0, 1));
					}
					else
					{
						Console.WriteLine($"Error collecting symbol data for {Name}{data.Error}");
						mCollectionFailed = true;
					}
				}

				// If position is being redownloaded from a previous failure, re-initialize the position if the position is in a state where it 
				if(FinishedDownloading == true && CollectionFailed == false)
				{
					foreach(var pos in Position.FindPositions(Exchanges.Binance, Name))
					{
						pos.InitPos();
					}
				}

				FinishedDownloading = true;
			}

			CollectionFinishedCallback(this);
		}

		private void Subscription_ConnectionRestored(TimeSpan obj)
		{
			Connected = true;
			Program.Log($"{Name} Connection restored");
			foreach(var pos in Position.FindPositions(Exchanges.Binance, Name))
			{
				pos.InitPos();
			}
		}

		private void Subscription_ConnectionLost()
		{
			Program.Log($"{Name} Connection lost");

			Connected = false;

			Task.Run(() =>
			{
				do
				{
					Functions.CreateTimer(TimeSpan.FromSeconds(30), () =>
					{
						if (!Connected)
						{
							TryToSubscribe(Interval);
						}
					});
				}
				while (Connected == false);
			});
		}

		public bool TryToSubscribe(OHLCVInterval interval)
		{
			SocketClient ??= new BinanceSocketClient(new BinanceSocketClientOptions()
			{
				AutoReconnect = true,
				ReconnectInterval = TimeSpan.FromSeconds(5)
			});

			Subscription?.Close();

			KlineInterval klv = ConvertToExchangeInterval(interval);

			var result = SocketClient.Spot.SubscribeToKlineUpdates(Name, klv, OnKlineUpdate);

			if (result.Success)
			{
				Connected = true;
				Subscription = result.Data;
				Subscription.ConnectionLost += Subscription_ConnectionLost;
				Subscription.ConnectionRestored += Subscription_ConnectionRestored;
			}

			return result.Success;
		}

		private void OnKlineUpdate(IBinanceStreamKlineData obj)
		{
			if(obj.Data.Final == true)
			{
				if(Periods.Count == 0 || (obj.Data.OpenTime - Periods[Periods.Count - 1].Date == ExchangeTasks.GetOHLCVIntervalTimeSpan(ConvertToGeneralInterval(obj.Data.Interval))))
				{
					Periods.Add(ConvertOHLCVPeriod(obj.Data));

					if(FinishedDownloading == true)
					{
						foreach (var pos in Position.FindPositions(Exchanges.Binance, Name))
						{
							pos.OnSymbolKlineUpdate();
						}
					}
				}
				else
				{
					if(Periods[Periods.Count - 1].Date != obj.Data.OpenTime)
					{
						Program.Log($"Discrepancy in KLines: {Name} {Periods[Periods.Count - 1].Date} {obj.Data.OpenTime}");
						CollectionFailed = true;
					}
					else
					{
						Program.Log($"{Name}: {obj.Data.OpenTime - Periods[Periods.Count - 1].Date}");
					}
				}
			}
			else
			{
				//Console.WriteLine($"{Name}: Test");
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
			p.Date = period.OpenTime;
			p.CloseTime = period.CloseTime;
			//p.AssetVolume = period.QuoteVolume;

			return p;
		}
	}
}
