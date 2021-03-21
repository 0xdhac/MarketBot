using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public enum OHLCVInterval
	{
		OneMinute = 0,
		ThreeMinute,
		FiveMinute,
		TenMinute,
		FifteenMinute,
		ThirtyMinute,
		FortyFiveMinute,
		OneHour,
		TwoHour,
		FourHour,
		SixHour,
		EightHour,
		TwelveHour,
		OneDay,
		ThreeDay,
		OneWeek,
		OneMonth,
		OneYear
	};

	public class OHLCVPeriod
	{
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
		public decimal Close { get; set; }
		public decimal Volume { get; set; }
		public DateTime OpenTime { get; set; }
		public DateTime CloseTime { get; set; }
		//public decimal AssetVolume { get; set; }
	};

	public delegate void OHLCVCollectionCompletedCallback(IExchangeOHLCVCollection callback);

	public interface IExchangeOHLCVCollection
	{
		bool CollectionFailed { get; set; }
		string Name { get; set; }
		HList<OHLCVPeriod> Data { get; set; }
		void CollectApiOHLCV(OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, bool screener_updates, DateTime? start = null);
		OHLCVPeriod this[int index] { get; }
	}

	public class GenericOHLCVCollection : IExchangeOHLCVCollection
	{
		public string Name { get; set; }
		public HList<OHLCVPeriod> Data { get; set; }
		public OHLCVPeriod this[int index] { get => Data[index]; }
		public bool CollectionFailed { get; set; }

		public GenericOHLCVCollection()
		{
			Data = new HList<OHLCVPeriod>();
		}
		public void CollectApiOHLCV(OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, bool screener_updates, DateTime? start = null) { }
	}
}
