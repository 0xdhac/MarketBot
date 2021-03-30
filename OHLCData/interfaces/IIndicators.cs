using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot.interfaces
{
	public abstract class Indicator: Expandum
	{
		public List<object> Inputs = new List<object>();
		public DataTable Data = new DataTable();
		public SymbolData Source;
		public EventHandler OnCalculate;

		public Indicator(SymbolData source, params object[] inputs)
		{
			Inputs.AddRange(inputs);
			Source = source;
			Source.Data.Periods.OnAdd_PrePost += PeriodAdded;
			BuildDataTable();
			FullCalculate();
		}

		public abstract DataRow Calculate(int period);

		public abstract void BuildDataTable();

		public V Value<V>(string column, int index)
		{
			 return (V)Data.Rows[index][column];
		}

		public List<C> GetColumn<C>(string column)
		{
			return Data.AsEnumerable().Select(s => (C)s[column]).ToList();
		}

		public List<C> GetColumn<C>(string column, int index, int count)
		{
			return Data.AsEnumerable().ToList().GetRange(index, count).Select(s => (C)s[column]).ToList();
		}

		public virtual void FullCalculate()
		{
			for(int period = 0; period < Source.Data.Periods.Count; period++)
			{
				Calculate(period);

				if(null != OnCalculate)
				{
					OnCalculate(this, null);
				}
			}
		}

		public virtual void PeriodAdded(object sender, EventArgs e)
		{
			Calculate(Source.Data.Periods.Count - 1);

			if (null != OnCalculate)
			{
				OnCalculate(this, null);
			}
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
