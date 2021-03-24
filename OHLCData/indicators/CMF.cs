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
		int Length = 0;
		public CMF(SymbolData data, int length) : base(data, length){}

		public new decimal this[int index]
		{
			get => IndicatorData[index].Item2;
		}

		public override void Calculate(int period)
		{
			if(period - (int)Inputs[Length] < 0)
			{
				IndicatorData.Add(new Tuple<bool, decimal>(false, 0));
				return;
			}

			decimal mfv_period_sum = 0;
			decimal volume_sum = 0;
			for (int i = 0; i < (int)Inputs[Length]; i++)
			{
				mfv_period_sum += GetMoneyFlowVolume(period - i);
				volume_sum += Source[period - i].Volume;
			}

			if(volume_sum == 0)
				IndicatorData.Add(new Tuple<bool, decimal>(true, 0));
			else
				IndicatorData.Add(new Tuple<bool, decimal>(true, mfv_period_sum / volume_sum));
		}

		public decimal GetMoneyFlowVolume(int period)
		{
			OHLCVPeriod pd = Source[period];
			if (pd.High - pd.Low == 0)
				return 0;

			decimal mfm = ((pd.Close - pd.Low) - (pd.High - pd.Close)) / (pd.High - pd.Low);
			return mfm * pd.Volume;
		}

		public override string GetName()
		{
			return "Chaikin Money Flow";
		}
	}
}
