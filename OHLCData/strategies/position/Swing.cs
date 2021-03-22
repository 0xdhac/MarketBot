using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.strategies.position
{
	class Swing : RiskStrategy
	{
		public int Length;

		public override decimal GetRiskPrice(int period, SignalType signal)
		{
			switch (signal)
			{
				case SignalType.Long:
					return GetPreviousLow(period, Length);
				case SignalType.Short:
					return GetPreviousHigh(period, Length);
			}

			return 0;
		}

		public Swing(SymbolData data, int length) : base(data) 
		{
			Length = length;
		}

		public decimal GetPreviousHigh(int period, int length)
		{
			decimal high = Source.Data.Data[period].High;

			for (int i = 1; i < length; i++)
			{
				if (high < Source.Data.Data[period - i].High)
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
			return "Swing low/high";
		}
	}
}

