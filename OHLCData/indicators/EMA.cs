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
		public int Length;

		public EMA(int length) : base()
		{
			Length = length;
		}

		public override void Calculate(int period)
		{
			if (period - Length < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
			}
			else
			{
				decimal ema_yesterday;
				if(IndicatorData[period - 1].Item1 == false)
				{
					decimal sum = 0;

					for (int i = period; i > period - Length; i--)
					{
						sum += DataSource[i].Close;
					}

					ema_yesterday = SMA.GetSMA(sum, Length);
				}
				else
				{
					ema_yesterday = IndicatorData[period - 1].Item2;
				}

				decimal weight = (decimal)2.0 / (Length + (decimal)1.0);

				decimal ema_today = DataSource[period].Close * weight + ema_yesterday * (1 - weight);

				IndicatorData.Add(new Tuple<bool, decimal>(true, ema_today));
			}
		}

		/*
		public static decimal GetEMA(decimal[,] list, int index, int length)
		{
			// Index 0 = Value, Index 1 = EMA
			if(index - length < 0)
				return 0;

			decimal weight = (decimal)2.0 / (length + (decimal)1.0);

			decimal previous_ema;
			if(list[index - 1,1] > (decimal)0.0)
			{
				previous_ema = list[index - 1, 1];
			}
			else
			{
				decimal sum = 0;
				for (int i = 0; i < length; i++)
				{
					sum += list[index - i, 0];
				}

				previous_ema = sum / length;
			}

			return list[index,0] * weight + previous_ema * (1 - weight);
		}
		*/

		public static decimal GetEMA(decimal current_value, int length, decimal previous_ema)
		{
			decimal weight = (decimal)2.0 / (length + (decimal)1.0);
			return (current_value * weight) + (previous_ema * (1 - weight));
		}
	}
}
