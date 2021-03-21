using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	public class CMF : Indicator<Tuple<bool, decimal>>
	{
		int Length;
		public CMF(int length) : base(length)
		{
			Length = length;
		}

		public override void Calculate(int period)
		{
			if(period - Length < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
				return;
			}

			decimal mfv_period_sum = 0;
			decimal volume_sum = 0;
			for (int i = 0; i < Length; i++)
			{
				mfv_period_sum += GetMoneyFlowVolume(period - i);
				volume_sum += DataSource[period - i].Volume;
			}

			if(volume_sum == 0)
				IndicatorData.Add(new Tuple<bool, decimal>(true, 0));
			else
				IndicatorData.Add(new Tuple<bool, decimal>(true, mfv_period_sum / volume_sum));
		}

		public decimal GetMoneyFlowVolume(int period)
		{
			OHLCVPeriod pd = DataSource[period];
			if (pd.High - pd.Low == 0)
				return 0;

			decimal mfm = ((pd.Close - pd.Low) - (pd.High - pd.Close)) / (pd.High - pd.Low);
			return mfm * pd.Volume;
		}
	}
}
