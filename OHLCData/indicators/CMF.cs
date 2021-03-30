using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;
using System.Data;

namespace MarketBot.indicators
{
	public class CMF : Indicator
	{
		int Length = 0;
		public CMF(SymbolData data, int length) : base(data, length){}

		public decimal this[int index]
		{
			get => Value<decimal>("value", index);
		}

		public override DataRow Calculate(int period)
		{
			if(period - (int)Inputs[Length] < 0)
			{
				return Data.Rows.Add(false, 0);
			}

			decimal mfv_period_sum = 0;
			decimal volume_sum = 0;
			for (int i = 0; i < (int)Inputs[Length]; i++)
			{
				mfv_period_sum += GetMoneyFlowVolume(period - i);
				volume_sum += Source[period - i].Volume;
			}

			if (volume_sum == 0)
				return Data.Rows.Add(true, 0);
			else
				return Data.Rows.Add(true, mfv_period_sum / volume_sum);
		}

		public decimal GetMoneyFlowVolume(int period)
		{
			OHLCVPeriod pd = Source[period];
			if (pd.High - pd.Low == 0)
				return 0;

			decimal mfm = ((pd.Close - pd.Low) - (pd.High - pd.Close)) / (pd.High - pd.Low);
			return mfm * pd.Volume;
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("calculated", typeof(bool));
			Data.Columns.Add("value", typeof(decimal));
		}

		public override string GetName()
		{
			return "Chaikin Money Flow";
		}
	}
}
