using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketBot
{
	public class HList<T> : List<T>
	{
		public event EventHandler OnAdd;
		public event EventHandler OnAdd_Post;
		public event EventHandler OnAdd_PrePost;

		public event EventHandler OnRemoveAt;
		public event EventHandler OnRemoveAt_Post;

		public event EventHandler OnRemove;
		public event EventHandler OnRemove_Post;

		public new void Add(T item)
		{
			if (null != OnAdd)
			{
				OnAdd(this, null);
			}

			base.Add(item);

			if(null != OnAdd_PrePost)
			{
				OnAdd_PrePost(this, null);
			}

			if(null != OnAdd_Post)
			{
				OnAdd_Post(this, null);
			}
		}

		public new void RemoveAt(int index)
		{
			if (null != OnRemoveAt)
			{
				OnRemoveAt(this, null);
			}

			base.RemoveAt(index);

			if (null != OnRemoveAt_Post)
			{
				OnRemoveAt_Post(this, null);
			}
		}

		public new void Remove(T item)
		{
			if (null != OnRemove)
			{
				OnRemove(this, null);
			}

			base.Remove(item);

			if (null != OnRemove_Post)
			{
				OnRemove_Post(this, null);
			}
		}
	}
}
