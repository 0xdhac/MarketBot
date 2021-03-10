using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.indicators;

namespace MarketBot.strategies.signals
{
	class Break : Strategy
	{
		private EMA TrendLine;
		private int Lookback;

		public Break(SymbolData data, int lookback) : base(data)
		{
			Lookback = lookback;
		}

		public override void ApplyIndicators()
		{
			TrendLine = (EMA)DataSource.RequireIndicator("EMA",
				new KeyValuePair<string, object>("Length", 200));
		}

		public override SignalType StrategyConditions(int new_period, int old_period)
		{
			

			return SignalType.None;
		}

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = DataSource.Data[period].High;

			for (int i = 1; i < length; i++)
			{
				if (high < DataSource.Data[period - i].High)
				{
					high = DataSource.Data[period - i].High;
				}
			}

			return high;
		}

		public decimal GetPreviousLow(int period, int length)
		{
			decimal low = DataSource.Data[period].Low;

			for (int i = 1; i < length; i++)
			{
				if (low > DataSource.Data[period - i].Low)
				{
					low = DataSource.Data[period - i].Low;
				}
			}

			return low;
		}

		public override string GetName()
		{
			return "Break Previous High/Low";
		}
	}
}
