using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace MarketBot.skender_strategies.exit_strategy
{
	class ADXExit : ExitStrategy
	{
		public List<AdxResult> Data;
		public ADXExit(HList<OHLCVPeriod> history) : base(history)
		{
			
		}

		public override void Update(int period, SignalType entry_type)
		{
			
		}

		public override bool ShouldExit(int period, SignalType entry_type, out decimal price)
		{
			ShouldUpdate();

			switch (entry_type)
			{
				case SignalType.Long:
					if (Data[period].Mdi.Value > Data[period].Pdi.Value)
					{
						price = History[period].Close;
						return true;
					}
						
					break;
				case SignalType.Short:
					if (Data[period].Mdi.Value < Data[period].Pdi.Value)
					{
						price = History[period].Close;
						return true;
					}
						
					break;
			}

			price = default;
			return false;
		}

		public override void UpdateData()
		{
			Data = Indicator.GetAdx(History).ToList();
		}
	}
}
