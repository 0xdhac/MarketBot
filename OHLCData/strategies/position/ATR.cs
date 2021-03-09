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

		public ATR(SymbolData data) : base(data) { }

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = Pair.Data.Data[period].High;

			for(int i = 1; i < length; i++)
			{
				if(high < Pair.Data.Data[period - i].High)
				{
					high = Pair.Data.Data[period - i].High;
				}
			}

			return high;
		}

		public decimal GetPreviousLow(int period, int length)
		{
			decimal low = Pair.Data.Data[period].Low;

			for (int i = 1; i < length; i++)
			{
				if (low > Pair.Data.Data[period - i].Low)
				{
					low = Pair.Data.Data[period - i].Low;
				}
			}

			return low;
		}

		public override void ApplyIndicators()
		{
			ATR_Data = (indicators.ATR)Pair.RequireIndicator("ATR", new KeyValuePair<string, object>("Length", 14));
		}

		public override string GetName()
		{
			return "ATR Trailing Stoploss";
		}
	}
}
