using System;

namespace Zoofware.ZZ80E
{
	public class Memory
	{
		private byte[] data;

		public virtual byte this[int index]
		{
			get
			{
				return data[index];
			}
			set
			{
				data[index] = value;
			}
		}

		public byte this[RegisterPair index]
		{
			get
			{
				return this[index.Word];
			}
			set
			{
				this[index.Word] = value;
			}
		}

		/// <summary>
		/// Loads bytes into memory
		/// </summary>
		/// <param name="array"></param>
		public virtual void Load(byte[] src)
		{
			data = src;
		}
	}
}
