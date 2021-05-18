using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.entry_conditions
{
	class SuperTrendCondition : BaseCondition
	{
		public List<SuperTrendResult> Data;

		public SuperTrendCondition(HList<OHLCVPeriod> history) : base(history)
		{
			
		}

		public override SignalType[] GetAllowed(int period)
		{
			ShouldUpdate();

			if (!Data[period].SuperTrend.HasValue)
				return new SignalType[] { };

			if (History[period].Low > Data[period].SuperTrend.Value)
			{
				return new SignalType[] { SignalType.Long };
			}
			else if (History[period].High < Data[period].SuperTrend.Value)
			{
				return new SignalType[] { SignalType.Short };
			}

			return new SignalType[] { };
		}

		public override void UpdateData()
		{
			Data = Indicator.GetSuperTrend(History).ToList();
		}
	}
}
