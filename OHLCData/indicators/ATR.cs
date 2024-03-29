﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class ATR : Indicator<Tuple<bool, decimal>>
	{
		int Length;

		public ATR(SymbolData data, int length) : base(data, length)
		{
			Length = length;
		}

		public override void Calculate(int period)
		{
			if(period - Length < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
			}
			else
			{
				IndicatorData.Add(new Tuple<bool, decimal>(true, GetATR(Source.Data.Periods, period, (int)Inputs[0])));
			}
		}

		public static decimal GetATR(HList<OHLCVPeriod> data, int index, int length)
		{
			if(index - length < 0)
			{
				return 0;
			}

			decimal sum_tr = 0;
			for(int i = 0; i < length; i++)
			{
				sum_tr += TR.GetTR(data, index - i);
			}

			return sum_tr / length;
		}

		public override string GetName()
		{
			return "Average True Range";
		}
	}
}
