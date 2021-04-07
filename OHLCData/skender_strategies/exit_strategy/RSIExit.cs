using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.skender_strategies.exit_strategy
{
	class RSIExit : ExitStrategy
	{
		private List<RsiResult> Data = new List<RsiResult>();
		private decimal Overbought;
		private decimal Oversold;
		private bool Crossdown;

		public RSIExit(HList<OHLCVPeriod> history, decimal overbought, decimal oversold, bool oncrossdown) : base(history) 
		{
			Overbought = overbought;
			Oversold = oversold;
			Crossdown = oncrossdown;
		}

		public override bool ShouldExit(int period, SignalType signal, out decimal price)
		{
			ShouldUpdate();

			if(period == 0)
			{
				price = default;
				return false;
			}

			if (!Data[period].Rsi.HasValue)
			{
				price = default;
				return false;
			}

			if(Crossdown)
			{
				if (!Data[period - 1].Rsi.HasValue)
				{
					price = default;
					return false;
				}
				else
				{
					switch (signal)
					{
						case SignalType.Long:
							if (Data[period].Rsi.Value < Overbought && Data[period - 1].Rsi.Value >= Overbought)
							{
								price = History[period].Close;
								return true;
							}
							break;
						case SignalType.Short:
							if (Data[period].Rsi.Value > Oversold && Data[period - 1].Rsi.Value <= Oversold)
							{
								price = History[period].Close;
								return true;
							}
							break;
						default:
							break;
					}
				}
			}
			else
			{
				switch (signal)
				{
					case SignalType.Long:
						if (Data[period].Rsi.Value >= Overbought)
						{
							price = History[period].Close;
							return true;
						}
						break;
					case SignalType.Short:
						if (Data[period].Rsi.Value <= Oversold)
						{
							price = History[period].Close;
							return true;
						}
						break;
					default:
						break;
				}
			}

			price = default;
			return false;
		}

		public override void Update(int period, SignalType signal)
		{
			
		}

		public override void UpdateData()
		{
			Data = Indicator.GetRsi(History).ToList();
		}
	}
}
