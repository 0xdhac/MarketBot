using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public class Scheme : Expandum
	{
		// Strategies
		EntrySignaler EntryStrategy = null;
		RiskStrategy RiskStrategy = null;

		//Settings
		decimal RewardRisk = (decimal)3;

		public Scheme(dynamic scheme)
		{

		}

		public Scheme(EntrySignaler entry, RiskStrategy risk, decimal profit_ratio)
		{
			EntryStrategy = entry;
			RiskStrategy = risk;
			RewardRisk = profit_ratio;
		}

		// Create a setting for each symbol for whether or not a given signal should be reversed
		//Scheme randomized inputs
		// All indicator values
		// Risk/reward ratio

		//Factors that determine how good a strategy is
		// Win loss ratio
		// Risk reward ratio
		// Average number of periods per trade (Increases with risk reward ratio)
		// Size of risk
		// How big 

		// Risk/Reward Close to 1:1, Risk bigger the better. Using the ADX indicator might help to improve win rate and decrease average periods per trade
		public dynamic ToExpando()
		{
			throw new NotImplementedException();
		}
	}
}
