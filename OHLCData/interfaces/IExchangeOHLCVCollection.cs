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
	};

	public delegate void OHLCVCollectionCompletedCallback(IExchangeOHLCVCollection callback);

	public interface IExchangeOHLCVCollection
	{
		CustomList<OHLCVPeriod> Data { get; set; }
		void CollectOHLCV(string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, DateTime? start = null);
		OHLCVPeriod this[int index] { get; }
	}

	public class GenericOHLCVCollection : IExchangeOHLCVCollection
	{
		public CustomList<OHLCVPeriod> Data { get; set; }
		public OHLCVPeriod this[int index] { get => Data[index]; }

		public GenericOHLCVCollection()
		{
			Data = new CustomList<OHLCVPeriod>();
		}
		public void CollectOHLCV(string symbol, OHLCVInterval interval, int periods, OHLCVCollectionCompletedCallback callback, DateTime? start = null) { }
	}
}
