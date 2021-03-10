using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	public class VWAP : Indicator<Tuple<bool, decimal>>
	{
		int Length;
		public VWAP(int length) : base()
		{
			Length = length;
		}

		public override void Calculate(int period)
		{
			if (period - Length < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
				return;
			}

			
		}

		public decimal GetVWAP(int period)
		{
			return 0;
		}
	}
}
