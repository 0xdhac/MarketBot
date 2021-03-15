using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class Wallet
	{
		public decimal Total = 0;
		public decimal Available = 0;

		public Dictionary<string, Dictionary<string, decimal>> Balances = new Dictionary<string, Dictionary<string, decimal>>();
		public Exchanges Exchange { get; set; }

		public Wallet(Exchanges exchange)
		{
			Exchange = exchange;
		}

		public EventHandler BalanceUpdated;
		public virtual void UpdateBalance()
		{
			if(null != BalanceUpdated)
			{
				BalanceUpdated(this, null);
			}
		}
	}
}
