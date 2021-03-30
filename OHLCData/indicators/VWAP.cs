using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketBot.interfaces;

namespace MarketBot.indicators
{
	public class VWAP : Indicator
	{	
		/*
		 * if outside of standard deviation band(2) then no entry
		 * if outside of sd band(.2-.5) then entry
		 * if above 9period ema, only long
		 * if below 9period ema, only short
		 */

		public VWAP(SymbolData data) : base(data) { }

		public override DataRow Calculate(int period)
		{
			decimal cumulative_price = 0;
			decimal cumulative_volume = 0;

			decimal current_typical_price = ((Source[period].High + Source[period].Low + Source[period].Close) / (decimal)3.0) * Source[period].Volume;
			decimal current_volume = Source[period].Volume;

			if (period != 0)
			{
				if(Source[period - 1].Date.Day != Source[period].Date.Day)
				{
					cumulative_price = current_typical_price;
					cumulative_volume = current_volume;
				}
				else
				{
					cumulative_price = Value<decimal>("price", period - 1) + current_typical_price;
					cumulative_volume = Value<decimal>("volume", period - 1) + current_volume;
				}
			}

			if (cumulative_volume != 0)
			{
				return Data.Rows.Add(cumulative_price, cumulative_volume, cumulative_price / cumulative_volume);
			}
			else
			{
				return Data.Rows.Add(cumulative_price, cumulative_volume, 0);
			}
		}

		public decimal GetVWAP(decimal typical_price_average, decimal volume)
		{
			// Typical price is the average of high, low, and close for length of period
			return 0;
		}

		public override string GetName()
		{
			return "Volume Weighted Average Price";
		}

		public override void BuildDataTable()
		{
			Data.Columns.Add("price", typeof(decimal));
			Data.Columns.Add("volume", typeof(decimal));
			Data.Columns.Add("value", typeof(decimal));
		}
	}
}
