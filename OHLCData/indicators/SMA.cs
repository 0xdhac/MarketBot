using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class SMA : Indicator<Tuple<bool, decimal>>
	{
		public int Length;

		public SMA(SymbolData data, int length) : base(data, length)
		{
			Length = length;
		}

		public new decimal this[int index]
		{
			get => IndicatorData[index].Item2;
		}

		public override void Calculate(int period)
		{
			if(period + 1 < Length)
			{
				Tuple<bool, decimal> idx = new Tuple<bool, decimal>(false, 0);
				IndicatorData.Add(idx);
			}
			else
			{
				decimal sum = 0;
				
				for (int i = period; i > period - Length; i--)
				{
					sum += Source[i].Close;
				}

				decimal sma = sum / Length;
				IndicatorData.Add(new Tuple<bool, decimal>(true, sma));
			}
		}

		public static decimal GetSMA(decimal sum, int length)
		{
			return sum / length;
		}

		public override string GetName()
		{
			return "Simple Moving Average";
		}
	}
}
