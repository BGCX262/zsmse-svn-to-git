using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	/// <summary>
	/// A struct representing a register pair (2 8bit registers)
	/// </summary>
	public struct RegisterPair
	{
		/// <summary>
		/// Most significant byte of the register pair
		/// </summary>
		public byte HiByte;

		/// <summary>
		/// Least significant byte of the register pair
		/// </summary>
		public byte LoByte;

		/// <summary>
		/// Combination of the hi and lo bytes
		/// (little-endian)
		/// </summary>
		public ushort Word
		{
			get { return (ushort) ((HiByte << 8) + LoByte); }
			set
			{
				HiByte = (byte) ((value & 0xFF00) >> 8);
				LoByte = (byte) (value & 0x00FF);
			}
		}

		public override string ToString()
		{
			return string.Format("Hi: {0:X2}, Lo: {1:X2}", HiByte, LoByte);
		}

		/*#region Operator Overloads
		public static RegisterPair operator ++(RegisterPair r)
		{
			r.Word++;
			return r;
		}

		public static RegisterPair operator --(RegisterPair r)
		{
			r.Word--;
			return r;
		}
		#endregion*/
	}
}
