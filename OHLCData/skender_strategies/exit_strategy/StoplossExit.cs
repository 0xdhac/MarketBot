using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.exit_strategy
{
	class StoplossExit : ExitStrategy
	{
		public decimal Risk;
		Func<int, SignalType, decimal> RiskSetter;

		public StoplossExit(
			HList<OHLCVPeriod> history,
			Func<int, SignalType, decimal> risk_setter) :
			base(history)
		{
			RiskSetter = risk_setter;
		}

		public override void Update(int period, SignalType entry_type)
		{
			decimal entry = History[period].Close;

			Risk = RiskSetter(period, entry_type);
		}

		public override bool ShouldExit(int period, SignalType entry_signal, out decimal price)
		{
			switch (entry_signal)
			{
				case SignalType.Long:
					if (History[period].Low <= Risk)
					{
						price = Risk;
						return true;
					}

					break;
				case SignalType.Short:
					if (History[period].High >= Risk)
					{
						price = Risk;
						return true;
					}

					break;
			}

			price = default;
			return false;
		}

		public override void UpdateData()
		{

		}
	}
}
