using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{ 
	public abstract class Indicator
	{
		public CustomList<OHLCVPeriod> DataSource;

		public virtual void AttachSource(IExchangeOHLCVCollection source)
		{
			DataSource = source.Data;
			DataSource.OnAdd += PeriodAdded;
			FullCalculate();
		}

		public abstract void Calculate(int period);

		public virtual void FullCalculate()
		{
			for(int period = 0; period < DataSource.Count; period++)
			{
				Calculate(period);
			}
		}

		public virtual void PeriodAdded(object sender, EventArgs e)
		{
			Calculate(DataSource.Count - 1);
		}
	}
}
