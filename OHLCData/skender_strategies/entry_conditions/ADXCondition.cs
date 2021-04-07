using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_conditions
{
	class ADXCondition : BaseCondition
	{
		public List<AdxResult> Data;
		private decimal ADX;
		public ADXCondition(HList<OHLCVPeriod> history, decimal adx) : base(history)
		{
			ADX = adx;
		}

		public override SignalType[] GetAllowed(int period)
		{
			ShouldUpdate();

			if (!Data[period].Adx.HasValue || !Data[period].Mdi.HasValue || !Data[period].Pdi.HasValue)
				return new SignalType[] { };
			
			if(Data[period].Adx.Value >= ADX)
			{
				if (Data[period].Pdi.Value > Data[period].Mdi.Value)
				{
					return new SignalType[] { SignalType.Long };
				}

				if (Data[period].Pdi.Value < Data[period].Mdi.Value)
				{
					return new SignalType[] { SignalType.Short };
				}
			}

			return new SignalType[] { };
		}

		public override void UpdateData()
		{
			Data = Indicator.GetAdx(History).ToList();
		}
	}
}
