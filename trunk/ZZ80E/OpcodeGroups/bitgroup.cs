using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		#region Set
		private void _set_b_r(byte pos, ref byte target, string register)
		{
			if (DebugMode)
			{
				DebugOut("SET", "{0}, {1}", pos, register);
				
			}

			TStates += 8;
			target = StaticHelpers.SetBit(target, pos, true);
		}

		private void _set_b_hl(byte pos)
		{
			if (DebugMode)
			{
				DebugOut("SET", "{0}, (HL)", pos);
				
			}

			TStates += 15;
			Memory[HL] = StaticHelpers.SetBit(Memory[HL], pos, true);
		}

		private void _set_b_ixd(byte pos, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SET", "{0}, (IX+#0x{1:X2})", pos, offset);
				
			}

			TStates += 23;
			Memory[IX.Word + offset] = StaticHelpers.SetBit(Memory[IX.Word + offset], pos, true);
		}

		private void _set_b_iyd(byte pos, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SET", "{0}, (IY+#0x{1:X2})", pos, offset);
				
			}

			TStates += 23;
			Memory[IY.Word + offset] = StaticHelpers.SetBit(Memory[IY.Word + offset], pos, true);
		}
		#endregion

		#region Bit
		private void _bit_b_r(byte pos, byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("BIT", "{0}, {1}", pos, register);
				
			}

			TStates += 8;
			FZero = !StaticHelpers.TestBit(source, pos);
			FSign = (pos == 7) && (FZero == false);
			FHalfCarry = true;
			FPV = FZero;
			FNegative = false;
		}

		private void _bit_b_hl(byte pos)
		{
			if (DebugMode)
			{
				DebugOut("BIT", "{0}, (HL)", pos);
				
			}

			TStates += 12;
			
			FZero = !StaticHelpers.TestBit(Memory[HL], pos);
			FSign = (pos == 7) && (FZero == false);
			FHalfCarry = true;
			FPV = FZero;
			FNegative = false;
		}

		private void _bit_b_ixd(byte pos, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("BIT", "{0}, (IX+{1:X2})", pos, offset);
				
			}

			TStates += 20;

			FZero = !StaticHelpers.TestBit(Memory[IX.Word + offset], pos);

			FSign = (pos == 7) && (FZero == false);
			FHalfCarry = true;
			FPV = FZero;
			FNegative = false;
		}

		private void _bit_b_iyd(byte pos, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("BIT", "{0}, (IY+{1:X2})", pos, offset);
				
			}

			TStates += 20;

			FZero = !StaticHelpers.TestBit(Memory[IY.Word + offset], pos);

			FSign = (pos == 7) && (FZero == false);
			FHalfCarry = true;
			FPV = FZero;
			FNegative = false;
		}
		#endregion

		#region Res
		private void _res_b_r(int bit, ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("RES", "{0}, {1}", bit, register);
				
			}

			TStates += 8;

			value = StaticHelpers.SetBit(value, bit, false);
		}

		private void _res_b_hl(int bit)
		{
			if (DebugMode)
			{
				DebugOut("RES", "(HL)");
				
			}

			TStates += 16;

			Memory[HL] = StaticHelpers.SetBit(Memory[HL], bit, false);
		}

		private void _res_b_ix_d(int bit, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RES", "{0}, (IX+#{1:X2})", bit, offset);
				
			}

			TStates += 23;

			Memory[IX.Word + offset] = StaticHelpers.SetBit(Memory[IX.Word + offset], bit, false);
		}

		private void _res_b_iy_d(int bit, sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RES", "{0}, (IY+#{1:X2})", bit, offset);
				
			}

			TStates += 23;

			Memory[IY.Word + offset] = StaticHelpers.SetBit(Memory[IY.Word + offset], bit, false);
		}
		#endregion

		private void _rld()
		{
			if (DebugMode)
			{
				DebugOut("RLD");				
			}

			TStates += 18;

			byte a_hi = (byte)((AF.HiByte & 0xF0) >> 4);
			byte a_lo = (byte)(AF.HiByte & 0xF);

			byte m_hi = (byte)((Memory[HL] & 0xF0) >> 4);
			byte m_lo = (byte)(Memory[HL] & 0xF);

			Memory[HL] = a_lo;
			Memory[HL] |= (byte)(m_lo << 4);

			AF.HiByte = m_hi;
			AF.HiByte |= (byte)(a_hi << 4);

			FSign = StaticHelpers.TestBit(AF.HiByte, 7);
			FZero = AF.HiByte == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(AF.HiByte);
		}

		private void _rrd()
		{
			if (DebugMode)
			{
				DebugOut("RRD");				
			}

			TStates += 18;

			byte a_hi = (byte)((AF.HiByte & 0xF0) >> 4);
			byte a_lo = (byte)(AF.HiByte & 0xF);

			byte m_hi = (byte)((Memory[HL] & 0xF0) >> 4);
			byte m_lo = (byte)(Memory[HL] & 0xF);

			Memory[HL] = m_hi;
			Memory[HL] |= (byte)(a_lo << 4);

			AF.HiByte = m_lo;
			AF.HiByte |= (byte)(a_hi << 4);

			FSign = StaticHelpers.TestBit(AF.HiByte, 7);
			FZero = AF.HiByte == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(AF.HiByte);
		}
	}
}
