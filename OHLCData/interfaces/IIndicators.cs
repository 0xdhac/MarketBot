using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public interface IIndicator
	{
		void AttachSource(IExchangeOHLCVCollection source);
		void FullCalculate();
	}
	public abstract class Indicator<T>: IIndicator
	{
		public List<T> IndicatorData { get; set; }
		public CustomList<OHLCVPeriod> DataSource;

		public Indicator()
		{
			IndicatorData = new List<T>();
		}

		public T this[int index]
		{
			get => IndicatorData[index];
		}

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
