using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.price_setter
{
	class RiskRewardRatio : PriceSetter
	{
		decimal ProfitRatio;
		public RiskRewardRatio(HList<OHLCVPeriod> history, decimal ratio) : base(history)
		{
			ProfitRatio = ratio;
		}

		public override decimal GetPrice(int period, SignalType signal)
		{
			// Need: entry price and risk price
			throw new NotImplementedException();
		}

		public override void UpdateData()
		{
			throw new NotImplementedException();
		}
	}
}
