using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.exit_strategy
{
	class TimeLimitExit : ExitStrategy
	{
		public decimal Entry { get; set; }
		public TimeSpan TimeLimit;
		public DateTime EntryDate;
		public bool ForceExit { get; set; }

		public TimeLimitExit(
			HList<OHLCVPeriod> history,
			TimeSpan time,
			DateTime date,
			bool force_exit) : base(history)
		{
			TimeLimit = time;
			EntryDate = date;
			ForceExit = force_exit;
		}

		public override void Update(int period, SignalType entry_type)
		{
			Entry = History[period].Close;
		}

		public override bool ShouldExit(int period, SignalType entry_signal, out decimal price)
		{
			if(History[period].CloseTime - EntryDate >= TimeLimit)
			{
				switch (entry_signal)
				{
					case SignalType.Long:
						if (ForceExit || History[period].Close >= Entry)
						{
							price = History[period].Close;
							return true;
						}
						break;
					case SignalType.Short:
						if (ForceExit || History[period].Close <= Entry)
						{
							price = History[period].Close;
							return true;
						}
						break;
				}
			}


			price = default;
			return false;
		}

		public override void UpdateData()
		{

		}
	}
}
