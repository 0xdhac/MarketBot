using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_conditions
{
	class CMFCondition : BaseCondition
	{
		public List<CmfResult> Data = new List<CmfResult>();

		public CMFCondition(HList<OHLCVPeriod> history) : base(history)
		{
			
		}

		public override SignalType[] GetAllowed(int period)
		{
			ShouldUpdate();

			if (Data[period].Cmf.HasValue)
			{
				if(Data[period].Cmf.Value > 0)
				{
					return new SignalType[] { SignalType.Long };
				}
				else if (Data[period].Cmf.Value < 0)
				{
					return new SignalType[] { SignalType.Short };
				}
			}

			return new SignalType[] {};
		}

		public override void UpdateData()
		{
			Data = Indicator.GetCmf(History).ToList();
		}
	}
}
