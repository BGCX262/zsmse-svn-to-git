using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		/// <summary>
		/// Generic rotate method.
		/// </summary>
		/// <param name="value">Value to rotate.</param>
		/// <param name="rotateRight">If true then bits will be rotated right, else left.</param>
		/// <param name="circular">If false then the value will be rotated through the carry flag. Otherwise, the value will act as if joined at both ends.</param>
		/// <returns>Rotated value</returns>
		private byte ___generic_rotate(byte value, bool rotateRight, bool circular)
		{
			bool bit;
			int bitNum;

			if (rotateRight)
			{
				bitNum = 7;
				bit = StaticHelpers.TestBit(value, 0);
				value >>= 1;
			}
			else
			{
				bitNum = 0;
				bit = StaticHelpers.TestBit(value, 7);
				value <<= 1;
			}

			if (circular)
			{
				FCarry = bit;
				value = StaticHelpers.SetBit(value, bitNum, FCarry);
			}

			else
			{
				value = StaticHelpers.SetBit(value, bitNum, FCarry);
				FCarry = bit;
			}

			FSign = StaticHelpers.TestBit(value, 7);
			FZero = value == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(value);

			return value;
		}

		private void _rlca()
		{
			if (DebugMode)
			{
				DebugOut("RLCA");
				
			}

			TStates += 4;

			FCarry = StaticHelpers.TestBit(AF.HiByte, 7);

			AF.HiByte <<= 1;

			AF.HiByte = StaticHelpers.SetBit(AF.HiByte, 0, FCarry);

			FHalfCarry = FNegative = false;

			FHalfCarry = FNegative = false;
		}

		private void _rla()
		{
			if (DebugMode)
			{
				DebugOut("RLA");
				
			}

			TStates += 4;

			bool bit = FCarry;

			FCarry = StaticHelpers.TestBit(AF.HiByte, 7);

			AF.HiByte <<= 1;

			AF.HiByte = StaticHelpers.SetBit(AF.HiByte, 0, bit);

			FHalfCarry = FNegative = false;

			FHalfCarry = FNegative = false;
		}

		private void _rrca()
		{
			if (DebugMode)
			{
				DebugOut("RRCA");
				
			}

			TStates += 4;

			FCarry = StaticHelpers.TestBit(AF.HiByte, 0);

			AF.HiByte >>= 1;

			AF.HiByte = StaticHelpers.SetBit(AF.HiByte, 7, FCarry);

			FHalfCarry = FNegative = false;
		}

		private void _rra()
		{
			if (DebugMode)
			{
				DebugOut("RRA");
				
			}

			TStates += 4;

			bool bit = StaticHelpers.TestBit(AF.HiByte, 0);

			AF.HiByte >>= 1;

			AF.HiByte = StaticHelpers.SetBit(AF.HiByte, 7, FCarry);
			FCarry = bit;

			FHalfCarry = FNegative = false;
		}

		#region Rotate Left Circular
		private void _rlc(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("RLC", register);
				
			}

			TStates += 8;

			value = ___generic_rotate(value, false, true);
		}

		private void _rlc_hl()
		{
			if (DebugMode)
			{
				DebugOut("RLC", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_rotate(Memory[HL], false, true);
		}

		private void _rlc_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RLC", "(IX+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IX.Word + offset] = ___generic_rotate(Memory[IX.Word + offset], false, true);
		}

		private void _rlc_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RLC", "(IY+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IY.Word + offset] = ___generic_rotate(Memory[IY.Word + offset], false, true);
		}
		#endregion

		#region Rotate Left
		private void _rl(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("RL", register);
				
			}

			TStates += 8;

			value = ___generic_rotate(value, false, false);
		}

		private void _rl_hl()
		{
			if (DebugMode)
			{
				DebugOut("RL", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_rotate(Memory[HL], false, false);
		}

		private void _rl_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RL", "(IX+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IX.Word + offset] = ___generic_rotate(Memory[IX.Word + offset], false, false);
		}

		private void _rl_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RL", "(IY+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IY.Word + offset] = ___generic_rotate(Memory[IY.Word + offset], false, false);
		}
		#endregion

		#region Rotate Right Circular
		private void _rrc(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("RRC", register);
				
			}

			TStates += 8;

			value = ___generic_rotate(value, true, true);
		}

		private void _rrc_hl()
		{
			if (DebugMode)
			{
				DebugOut("RRC", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_rotate(Memory[HL], true, true);
		}

		private void _rrc_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RRC", "(IX+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IX.Word + offset] = ___generic_rotate(Memory[IX.Word + offset], true, true);
		}

		private void _rrc_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RRC", "(IY+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IY.Word + offset] = ___generic_rotate(Memory[IY.Word + offset], true, true);
		}
		#endregion

		#region Rotate Right
		private void _rr(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("RR", register);
				
			}

			TStates += 8;

			value = ___generic_rotate(value, true, false);
		}

		private void _rr_hl()
		{
			if (DebugMode)
			{
				DebugOut("RR", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_rotate(Memory[HL], true, false);
		}

		private void _rr_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RR", "(IX+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IX.Word + offset] = ___generic_rotate(Memory[IX.Word + offset], true, false);
		}

		private void _rr_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("RR", "(IY+#{0:X2})", offset);
				
			}

			TStates += 23;
			
			Memory[IY.Word + offset] = ___generic_rotate(Memory[IY.Word + offset], true, false);
		}
		#endregion

		#region Shift Left Arithmetic
		private byte ___generic_sla(byte value)
		{
			FCarry = StaticHelpers.TestBit(value, 7);

			value <<= 1;

			FSign = StaticHelpers.TestBit(value, 7);
			FZero = value == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(value);

			return value;
		}

		private void _sla_r(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("SLA", register);
				
			}

			TStates += 8;

			value = ___generic_sla(value);
		}

		private void _sla_hl()
		{
			if (DebugMode)
			{
				DebugOut("SLA", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_sla(Memory[HL]);
		}

		private void _sla_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SLA", "(IX+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IX.Word + offset] = ___generic_sla(Memory[IX.Word + offset]);
		}

		private void _sla_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SLA", "(IY+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IY.Word + offset] = ___generic_sla(Memory[IY.Word + offset]);
		}
		#endregion

		#region Shift Right Arithmetic
		private byte ___generic_sra(byte value)
		{
			FCarry = StaticHelpers.TestBit(value, 0);
			FSign = StaticHelpers.TestBit(value, 7);

			value >>= 1;

			value = StaticHelpers.SetBit(value, 7, FSign);
			FZero = value == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(value);

			return value;
		}

		private void _sra_r(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("SRA", register);
				
			}

			TStates += 8;

			value = ___generic_sra(value);
		}

		private void _sra_hl()
		{
			if (DebugMode)
			{
				DebugOut("SRA", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_sra(Memory[HL]);
		}

		private void _sra_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SRA", "(IX+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IX.Word + offset] = ___generic_sra(Memory[IX.Word + offset]);
		}

		private void _sra_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SRA", "(IY+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IY.Word + offset] = ___generic_sra(Memory[IY.Word + offset]);
		}
		#endregion

		#region Shift Right Logical
		private byte ___generic_srl(byte value)
		{
			FCarry = StaticHelpers.TestBit(value, 0);
			
			value >>= 1;

			FSign = StaticHelpers.TestBit(value, 7);
			FZero = value == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(value);

			return value;
		}

		private void _srl_r(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("SRL", register);
				
			}

			TStates += 8;

			value = ___generic_srl(value);
		}

		private void _srl_hl()
		{
			if (DebugMode)
			{
				DebugOut("SRL", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_srl(Memory[HL]);
		}

		private void _srl_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SRL", "(IX+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IX.Word + offset] = ___generic_srl(Memory[IX.Word + offset]);
		}

		private void _srl_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SRL", "(IY+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IY.Word + offset] = ___generic_srl(Memory[IY.Word + offset]);
		}
		#endregion

		#region Shift Left Logical
		private byte ___generic_sll(byte value)
		{
			FCarry = StaticHelpers.TestBit(value, 7);

			value <<= 1;
			value = StaticHelpers.SetBit(value, 0, true);

			FSign = StaticHelpers.TestBit(value, 7);
			FZero = value == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(value);

			return value;
		}

		private void _sll_r(ref byte value, string register)
		{
			if (DebugMode)
			{
				DebugOut("SLL", register);
				
			}

			TStates += 8;

			value = ___generic_sll(value);
		}

		private void _sll_hl()
		{
			if (DebugMode)
			{
				DebugOut("SLL", "(HL)");
				
			}

			TStates += 15;

			Memory[HL] = ___generic_sll(Memory[HL]);
		}

		private void _sll_ix_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SLL", "(IX+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IX.Word + offset] = ___generic_sll(Memory[IX.Word + offset]);
		}

		private void _sll_iy_d(sbyte offset)
		{
			if (DebugMode)
			{
				DebugOut("SLL", "(IY+#{1:X2})", offset);
				
			}

			TStates += 23;

			Memory[IY.Word + offset] = ___generic_sll(Memory[IY.Word + offset]);
		}
		#endregion
	}
}
