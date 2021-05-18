using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using System.Data;

namespace MarketBot.indicators
{
	class MACD : Indicator
	{
		List<Tuple<bool, decimal>> ShortEMA = new List<Tuple<bool, decimal>>();
		List<Tuple<bool, decimal>> LongEMA = new List<Tuple<bool, decimal>>();

		public MACD(HList<OHLCVPeriod> data, int short_ema, int long_ema, int signal_ema) : base(data, short_ema, long_ema, signal_ema){}

		public override DataRow Calculate(int period)
		{
			// Calculate short ema, long ema, then signal ema

			if (period - (int)Inputs[0] < 0)
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

					for (int i = period; i > period - (int)Inputs[0]; i--)
					{
						sum += Source[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, (int)Inputs[0]);
				}
				else
				{
					ema_yesterday = ShortEMA[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / ((int)Inputs[0] + (decimal)1.0);

				decimal short_ema = Source[period].Close * weight + ema_yesterday * (1 - weight);

				ShortEMA.Add(new Tuple<bool, decimal>(true, short_ema));
			}

			if(period - (int)Inputs[1] < 0)
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

					for (int i = period; i > period - (int)Inputs[1]; i--)
					{
						sum += Source[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, (int)Inputs[1]);
				}
				else
				{
					ema_yesterday = LongEMA[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / ((int)Inputs[1] + (decimal)1.0);

				decimal long_ema = Source[period].Close * weight + ema_yesterday * (1 - weight);

				LongEMA.Add(new Tuple<bool, decimal>(true, long_ema));
			}

			bool macd_exists = false;
			decimal macd_value = 0;
			if (!(period - (int)Inputs[0] < 0 || period - (int)Inputs[1] < 0))
			{
				// Calculate MACD, use true/macd
				macd_exists = true;
				macd_value = ShortEMA[period].Item2 - LongEMA[period].Item2;
			}

			bool signal_exists = false;
			decimal signal_value = 0;
			if(!(period - ((int)Inputs[1] + (int)Inputs[2]) < 0))
			{
				// Calculate signal, use true/signal
				// Signal EMA
				decimal ema_yesterday;
				if (Value<bool>("signal_exists", period - 1) == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - (int)Inputs[2]; i--)
					{
						sum += ShortEMA[i].Item2 - LongEMA[i].Item2;
					}

					ema_yesterday = SMA.GetSMA(sum, (int)Inputs[2]);
				}
				else
				{
					ema_yesterday = Value<decimal>("signal_value", period - 1);
				}

				decimal weight = (decimal)2.0 / ((int)Inputs[2] + (decimal)1.0);
				signal_value = macd_value * weight + ema_yesterday * (1 - weight);
				signal_exists = true;
			}

			return Data.Rows.Add(macd_exists, macd_value, signal_exists, signal_value);
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("macd_calculated", typeof(bool));
			Data.Columns.Add("macd_value", typeof(decimal));
			Data.Columns.Add("signal_calculated", typeof(bool));
			Data.Columns.Add("signal_value", typeof(decimal));
		}

		public override string GetName()
		{
			return "Moving Average Convergence Divergence";
		}
	}
}
