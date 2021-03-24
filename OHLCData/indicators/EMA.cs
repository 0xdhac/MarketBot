using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	public class EMA : Indicator<Tuple<bool, decimal>>
	{
		public EMA(SymbolData data, int length) : base(data, length)
		{
		}

		public override void Calculate(int period)
		{
			if (period - (int)Inputs[0] < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
			}
			else
			{
				decimal ema_yesterday;
				if(IndicatorData[period - 1].Item1 == false)
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
					ema_yesterday = IndicatorData[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / ((int)Inputs[0] + (decimal)1.0);

				decimal ema_today = Source[period].Close * weight + ema_yesterday * (1 - weight);

				IndicatorData.Add(new Tuple<bool, decimal>(true, ema_today));
			}
		}

		public static decimal GetEMA(decimal current_value, int length, decimal previous_ema)
		{
			decimal weight = (decimal)2.0 / (length + (decimal)1.0);
			return (current_value * weight) + (previous_ema * (1 - weight));
		}

		public override string GetName()
		{
			return "Exponential Moving Average";
		}
	}
}
