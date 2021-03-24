using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public class TupleList
	{
		Dictionary<int, dynamic> List = new Dictionary<int, dynamic>();

		private List<Type> TypeList = new List<Type>();
		public TupleList(params string[] list)
		{
			int i = 0;
			foreach(var type in list)
			{
				Type t;
				try
				{
					t = Assembly.Load("decimal").GetType();
					//decimal

					if(t == null)
					{
						Console.WriteLine("null");
					}
					var listType = typeof(List<>);
					var constructedListType = listType.MakeGenericType(t);
					var instance = Activator.CreateInstance(constructedListType);
					List.Add(i++, instance);
					TypeList.Add(t);
				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		public void Add(params object[] values)
		{
			if (values.Length != TypeList.Count)
				throw new ArrayTypeMismatchException();

			if (!TypeList.TrueForAll((t) => values[TypeList.IndexOf(t)].GetType() == t))
				throw new ArrayTypeMismatchException();

			int i = 0;
			foreach(var obj in values)
			{
				List[i].Add(values[i]);

				i++;
			}
		}
	}
}
