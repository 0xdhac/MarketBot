using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.strategies.position
{
	class ATR : RiskStrategy
	{
		public indicators.ATR ATR_Data;

		public override decimal GetRiskPrice(int period, SignalType signal)
		{
			switch (signal)
			{
				case SignalType.Long:
					return GetPreviousLow(period, 14) - ATR_Data.IndicatorData[period].Item2;
				case SignalType.Short:
					return GetPreviousHigh(period, 14) + ATR_Data.IndicatorData[period].Item2;
			}

			return 0;
		}

		public ATR(SymbolData data) :
			base(data, $"{{\"indicators\":[{{\"name\":\"ATR\", \"inputs\":[14]}}]}}")
		{
			ATR_Data = (indicators.ATR)FindIndicator("ATR", 14);
		}

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = Source.Data.Data[period].High;

			for(int i = 1; i < length; i++)
			{
				if(high < Source.Data.Data[period - i].High)
				{
					high = Source.Data.Data[period - i].High;
				}
			}

			return high;
		}

		public decimal GetPreviousLow(int period, int length)
		{
			decimal low = Source.Data.Data[period].Low;

			for (int i = 1; i < length; i++)
			{
				if (low > Source.Data.Data[period - i].Low)
				{
					low = Source.Data.Data[period - i].Low;
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
