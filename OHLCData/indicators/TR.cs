using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	// True range function and indicator
	class TR : Indicator<Tuple<decimal>>
	{
		public TR(SymbolData data) : base(data) { }
		public new decimal this[int index]
		{
			get => IndicatorData[index].Item1;
		}

		public override void Calculate(int period)
		{
			IndicatorData.Add(new Tuple<decimal>(GetTR(Source.Data.Periods, period)));
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
	}
}
