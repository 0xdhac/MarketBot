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

		public Break(SymbolData data, int lookback) : 
			base(data, $"{{0:{{name:\"EMA\",params:\"200\"}}}}")
		{
			Lookback = lookback;

			TrendLine = (EMA)FindIndicator("EMA", 200);
		}

		public override SignalType StrategyConditions(int new_period, int old_period)
		{
			

			return SignalType.None;
		}

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = Source.Data[period].High;

			for (int i = 1; i < length; i++)
			{
				if (high < Source.Data[period - i].High)
				{
					high = Source.Data[period - i].High;
				}
			}

			return high;
		}

		public decimal GetPreviousLow(int period, int length)
		{
			decimal low = Source.Data[period].Low;

			for (int i = 1; i < length; i++)
			{
				if (low > Source.Data[period - i].Low)
				{
					low = Source.Data[period - i].Low;
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
