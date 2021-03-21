using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class MACD : Indicator<Tuple<bool, decimal, bool, decimal>>
	{
		int Short_EMA_Length;
		int Long_EMA_Length;
		int Signal_EMA_Length;

		List<Tuple<bool, decimal>> ShortEMA = new List<Tuple<bool, decimal>>();
		List<Tuple<bool, decimal>> LongEMA = new List<Tuple<bool, decimal>>();

		public MACD(int short_ema, int long_ema, int signal_ema) : base(short_ema, long_ema, signal_ema)
		{
			Short_EMA_Length = short_ema;
			Long_EMA_Length = long_ema;
			Signal_EMA_Length = signal_ema;
		}

		public override void Calculate(int period)
		{
			// Calculate short ema, long ema, then signal ema

			if (period - Short_EMA_Length < 0)
			{
				ShortEMA.Add(new Tuple<bool, decimal>(false, 0));
			}
			else
			{
				// Short EMA
				decimal ema_yesterday = (decimal)0.0;
				if (ShortEMA[period - 1].Item1 == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - Short_EMA_Length; i--)
					{
						sum += DataSource[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, Short_EMA_Length);
				}
				else
				{
					ema_yesterday = ShortEMA[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / (Short_EMA_Length + (decimal)1.0);

				decimal short_ema = DataSource[period].Close * weight + ema_yesterday * (1 - weight);

				ShortEMA.Add(new Tuple<bool, decimal>(true, short_ema));
			}

			if(period - Long_EMA_Length < 0)
			{
				LongEMA.Add(new Tuple<bool, decimal>(false, 0));
			}
			else
			{
				// Long EMA
				decimal ema_yesterday = 0;
				if (LongEMA[period - 1].Item1 == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - Long_EMA_Length; i--)
					{
						sum += DataSource[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, Long_EMA_Length);
				}
				else
				{
					ema_yesterday = LongEMA[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / (Long_EMA_Length + (decimal)1.0);

				decimal long_ema = DataSource[period].Close * weight + ema_yesterday * (1 - weight);

				LongEMA.Add(new Tuple<bool, decimal>(true, long_ema));
			}

			bool macd_exists = false;
			decimal macd_value = 0;
			if (!(period - Short_EMA_Length < 0 || period - Long_EMA_Length < 0))
			{
				// Calculate MACD, use true/macd
				macd_exists = true;
				macd_value = ShortEMA[period].Item2 - LongEMA[period].Item2;
			}

			bool signal_exists = false;
			decimal signal_value = 0;
			if(!(period - (Long_EMA_Length + Signal_EMA_Length) < 0))
			{
				// Calculate signal, use true/signal
				// Signal EMA
				decimal ema_yesterday;
				if (IndicatorData[period - 1].Item3 == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - Signal_EMA_Length; i--)
					{
						sum += ShortEMA[i].Item2 - LongEMA[i].Item2;
					}

					ema_yesterday = SMA.GetSMA(sum, Signal_EMA_Length);
				}
				else
				{
					ema_yesterday = IndicatorData[period - 1].Item4;
				}

				decimal weight = (decimal)2.0 / (Signal_EMA_Length + (decimal)1.0);
				signal_value = macd_value * weight + ema_yesterday * (1 - weight);
				signal_exists = true;
			}

			IndicatorData.Add(new Tuple<bool, decimal, bool, decimal>(macd_exists, macd_value, signal_exists, signal_value));
		}
	}
}
