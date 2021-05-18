using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	// True range function and indicator
	class TR : Indicator
	{
		public TR(HList<OHLCVPeriod> data) : base(data) { }

		public override DataRow Calculate(int period)
		{
			if(period > 0)
			{
				decimal tr = GetTR(Source, period);
				return Data.Rows.Add(true, tr);
			}
			else
			{
				return Data.Rows.Add(false, 0);
			}
		}

		public static decimal GetTR(HList<OHLCVPeriod> data, int index)
		{
			if (index > 0)
			{
				return data[index].High - data[index].Low;
			}
			else
			{
				return Math.Max(
					data[index].High - data[index].Low,
						Math.Max(
							Math.Abs(data[index].High - data[index - 1].Close),
							Math.Abs(data[index].Low - data[index - 1].Close)));
			}
		}

		public override string GetName()
		{
			return "True Range";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
