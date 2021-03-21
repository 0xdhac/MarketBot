﻿using System;
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

		public EMA(int length) : base(length)
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

		public static decimal GetEMA(decimal current_value, int length, decimal previous_ema)
		{
			decimal weight = (decimal)2.0 / (length + (decimal)1.0);
			return (current_value * weight) + (previous_ema * (1 - weight));
		}
	}
}
