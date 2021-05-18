using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class ATR : Indicator
	{
		public ATR(HList<OHLCVPeriod> data, int length) : base(data, length)
		{
			
		}

		public decimal this[int index]
		{
			get => Value<decimal>("value", index);
		}

		public override DataRow Calculate(int period)
		{
			if (period - (int)Inputs[0] < 0)
			{
				return Data.Rows.Add(false, 0);
			}
			else
			{
				decimal atr = GetATR(Source, period, (int)Inputs[0]);
				return Data.Rows.Add(true, atr);
			}
		}

		public static decimal GetATR(HList<OHLCVPeriod> data, int index, int length)
		{
			if (index - length < 0)
			{
				return 0;
			}

			decimal sum_tr = 0;
			for (int i = 0; i < length; i++)
			{
				sum_tr += TR.GetTR(data, index - i);
			}

			return sum_tr / length;
		}

		public override string GetName()
		{
			return "Average True Range";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
