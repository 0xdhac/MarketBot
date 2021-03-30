using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using MarketBot.indicators;

namespace MarketBot.strategies.position
{
	class ATRRisk : RiskStrategy
	{
		public ATR ATR_Data;

		public override decimal GetRiskPrice(int period, SignalType signal)
		{
			switch (signal)
			{
				case SignalType.Long:
					return GetPreviousLow(period, 14) - ATR_Data[period];
				case SignalType.Short:
					return GetPreviousHigh(period, 14) + ATR_Data[period];
			}

			return 0;
		}

		public ATRRisk(SymbolData data) :
			base(data, $"{{\"indicators\":[{{\"name\":\"ATR\", \"inputs\":[14]}}]}}")
		{
			ATR_Data = (ATR)FindIndicator("ATR", 14);
		}

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = Source.Data.Periods[period].High;

			for(int i = 1; i < length; i++)
			{
				if(high < Source.Data.Periods[period - i].High)
				{
					high = Source.Data.Periods[period - i].High;
				}
			}

			return high;
		}

		public decimal GetPreviousLow(int period, int length)
		{
			decimal low = Source.Data.Periods[period].Low;

			for (int i = 1; i < length; i++)
			{
				if (low > Source.Data.Periods[period - i].Low)
				{
					low = Source.Data.Periods[period - i].Low;
				}
			}

			return low;
		}

		public override string GetName()
		{
			return "ATR Trailing Stoploss";
		}
	}
}
