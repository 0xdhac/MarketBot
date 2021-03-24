using MarketBot.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.indicators
{
	class ADX : Indicator<Tuple<bool, decimal, decimal, decimal>>
	{
		public int Length;

		public ADX(SymbolData data, int length) : base(data, length)
		{
			Length = length;
		}

		public override void OnSourceAttached()
		{
			
		}

		public override void Calculate(int period)
		{
			if(period == 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal, decimal, decimal>(false, 0, 0, 0));
				return;
			}

			// Calculate upmove/downmove
			decimal upmove = Source[period].High - Source[period - 1].High;
			decimal downmove = Source[period].Low - Source[period - 1].Low;

			// Calculate +DM
			decimal plus_dm = 0;
			if (upmove > downmove && upmove > 0)
				plus_dm = upmove;

			// Calculate -DM
			decimal minus_dm = 0;
			if (downmove > upmove && downmove > 0)
				minus_dm = downmove;

			//decimal ema = EMA(plus_dm)
			//decimal sma = SMA
			Tuple<decimal, decimal> test = new Tuple<decimal, decimal>((decimal)0.15, (decimal)0.18);
			//test.
		}

		public override string GetName()
		{
			return "Average Directional Index";
		}
	}
}
