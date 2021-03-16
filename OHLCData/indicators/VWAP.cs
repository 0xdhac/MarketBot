using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	public class VWAP : Indicator<Tuple<decimal, decimal, decimal>>
	{/*if outside of standard deviation band(2) then no entry
if outside of sd band(.2-.5) then entry
if above 9period ema, only long
if below 9period ema, only short*/
		public VWAP() : base() { }

		public new decimal this[int index]
		{
			get => IndicatorData[index].Item3;
		}

		public override void Calculate(int period)
		{
			decimal cumulative_price = 0;
			decimal cumulative_volume = 0;

			decimal current_typical_price = ((DataSource[period].High + DataSource[period].Low + DataSource[period].Close) / (decimal)3.0) * DataSource[period].Volume;
			decimal current_volume = DataSource[period].Volume;

			if (period != 0)
			{
				if(DataSource[period - 1].OpenTime.Day != DataSource[period].OpenTime.Day)
				{
					cumulative_price = current_typical_price;
					cumulative_volume = current_volume;
				}
				else
				{
					cumulative_price = IndicatorData[period - 1].Item1 + current_typical_price;
					cumulative_volume = IndicatorData[period - 1].Item2 + current_volume;
				}
			}

			if (cumulative_volume != 0)
			{
				IndicatorData.Add(new Tuple<decimal, decimal, decimal>(cumulative_price, cumulative_volume, cumulative_price / cumulative_volume));
			}
			else
			{
				IndicatorData.Add(new Tuple<decimal, decimal, decimal>(cumulative_price, cumulative_volume, 0));
			}
		}

		public decimal GetVWAP(decimal typical_price_average, decimal volume)
		{
			// Typical price is the average of high, low, and close for length of period
			return 0;
		}
	}
}
