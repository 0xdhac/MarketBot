using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	class SMA : Indicator
	{
		public int Length;

		public SMA(SymbolData data, int length) : base(data, length)
		{
			Length = length;
		}

		public override DataRow Calculate(int period)
		{
			if(period + 1 < Length)
			{
				return Data.Rows.Add(false, 0);
			}
			else
			{
				decimal sum = 0;
				
				for (int i = period; i > period - Length; i--)
				{
					sum += Source[i].Close;
				}

				decimal sma = sum / Length;
				return Data.Rows.Add(true, sma);
			}
		}

		public static decimal GetSMA(decimal sum, int length)
		{
			return sum / length;
		}

		public static decimal GetSMA(List<decimal> values)
		{
			return values.Sum() / values.Count;
		}

		public override string GetName()
		{
			return "Simple Moving Average";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
