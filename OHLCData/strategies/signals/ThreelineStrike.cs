using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	public class ThreelineStrike : Strategy
	{
		private int EmaLength;
		private EMA EMA;

		public ThreelineStrike(SymbolData data, int ema_length) :
			base(data, $"{{\"indicators\":[{{\"name\":\"EMA\", \"inputs\":[{ema_length}]}}]}}")
		{
			EmaLength = ema_length;

			EMA = (EMA)FindIndicator("EMA", EmaLength);
		}

		public override SignalType StrategyConditions(int old_period, int new_period)
		{
			if (new_period < 300)
				return SignalType.None;
			// 3 red candles followed by a green candle larger candle that closes above the oldest red candle


			HList<OHLCVPeriod> kline = Source.Data.Data;
			decimal candle1 = kline[new_period - 3].Close - kline[new_period - 3].Open; // Negative means red
			decimal candle2 = kline[new_period - 2].Close - kline[new_period - 2].Open;
			decimal candle3 = kline[new_period - 1].Close - kline[new_period - 1].Open;

			if (kline[new_period].Low > EMA[new_period].Item2 &&
				candle1 < 0 && candle2 < 0 && candle3 < 0 &&
				kline[new_period].Close > kline[new_period - 3].Open)
				return SignalType.Long;

			if (kline[new_period].High < EMA[new_period].Item2 &&
				candle1 > 0 && candle2 > 0 && candle3 > 0 &&
				kline[new_period].Close < kline[new_period - 3].Open)
				return SignalType.Short;

			return SignalType.None;
		}

		public override string GetName()
		{
			return "Three Line Strike";
		}
	}
}
