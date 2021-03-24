using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public delegate void IndicatorStepthroughCallback(HList<object> output, params object[] input);

	public interface IIndicator
	{
		List<object> Inputs { get; set; }
		void FullCalculate();
	}

	public abstract class Indicator<T>: IIndicator, Expandum
	{
		public List<object> Inputs { get; set; }
		public HList<T> IndicatorData { get; set; }
		public SymbolData Source;

		public Indicator(SymbolData source, params object[] inputs)
		{
			Inputs = inputs.ToList();

			IndicatorData = new HList<T>();

			Source = source;
			OnSourceAttached();
			Source.Data.Periods.OnAdd += PeriodAdded;
			FullCalculate();
		}

		public T this[int index]
		{
			get => IndicatorData[index];
		}

		public abstract void Calculate(int period);

		public virtual void OnSourceAttached() { }

		public virtual void FullCalculate()
		{
			for(int period = 0; period < Source.Data.Periods.Count; period++)
			{
				Calculate(period);
			}
		}

		public virtual void PeriodAdded(object sender, EventArgs e)
		{
			Calculate(Source.Data.Periods.Count - 1);
		}

		public virtual dynamic ToExpando()
		{
			Dictionary<string, object> expando = new Dictionary<string, object>();

			expando.Add(GetType().Name, Inputs.ToArray());

			return expando;
		}

		public abstract string GetName();

		public override string ToString()
		{
			return GetName();
		}
	}
}
