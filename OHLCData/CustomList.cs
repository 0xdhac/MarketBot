using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public class CustomList<T> : List<T>
	{
		public event EventHandler OnAdd;
		public event EventHandler OnAdd_Post;

		public new void Add(T item)
		{
			base.Add(item);
			if (null != OnAdd)
			{
				OnAdd(this, null);
			}

			if(null != OnAdd_Post)
			{
				OnAdd_Post(this, null);
			}
		}
	}
}
