using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		#region Subs
		private void ___generic_accumulator_8bitsub(byte source, bool borrow)
		{
			if (borrow && FCarry)
			{
				source++;
			}

			byte result = (byte)(AF.HiByte - source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = result == 0;
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, source, result);
			FPV = StaticHelpers.TestSubtractionOverflow(AF.HiByte, source, result);
			FNegative = true;
			FCarry = (uint)(AF.HiByte - source) > 0xFF;

			AF.HiByte = result;
		}

		private void _sub_a_r(byte source, string register, bool borrow)
		{
			if (DebugMode)
			{
				if (borrow)
				{
					DebugOut("SBC", "A, {0}", register);
					
				}
				else
				{
					DebugOut("SUB", "A, {0}", register);
					
				}
			}

			TStates += 4;
			___generic_accumulator_8bitsub(source, borrow);
		}

		private void _sub_a_n(bool borrow)
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				if (borrow)
				{
					DebugOut("SBC", "A, #0x{0:X2}", source);
					
				}
				else
				{
					DebugOut("SUB", "A, #0x{0:X2}", source);
					
				}
			}

			TStates += 7;
			___generic_accumulator_8bitsub(source, borrow);
		}

		private void _sub_a_hl(bool borrow)
		{
			if (DebugMode)
			{
				if (borrow)
				{
					DebugOut("SBC", "A, (HL)");
					
				}
				else
				{
					DebugOut("SUB", "A, (HL)");
					
				}
			}

			TStates += 7;
			___generic_accumulator_8bitsub(Memory[HL], borrow);
		}

		private void _sub_a_ixd(bool borrow)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte source = Memory[IX.Word + offset];

			if (DebugMode)
			{
				if (borrow)
				{
					DebugOut("SBC", "A, (IX+#0x{0:X2})", offset); 
				}
				else
				{
					DebugOut("SUB", "A, (IX+#0x{0:X2})", offset);
					
				}
			}

			TStates += 19;
			___generic_accumulator_8bitsub(source, borrow);
		}

		private void _sub_a_iyd(bool borrow)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte source = Memory[IY.Word + offset];

			if (DebugMode)
			{
				if (borrow)
				{
					DebugOut("SBC", "A, (IY+#0x{0:X2})", offset);
					
				}
				else
				{
					DebugOut("SUB", "A, (IY+#0x{0:X2})", offset);
					
				}
			}

			TStates += 19;
			___generic_accumulator_8bitsub(source, borrow);
		}
		#endregion

		#region Adds
		private void ___generic_accumulator_8bitadd(byte source, bool carry)
		{
			if (carry && FCarry)
			{
				source++;
			}

			byte result = (byte)(AF.HiByte + source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = result == 0;
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, source, result);
			FPV = StaticHelpers.TestAdditionOverflow(AF.HiByte, source, result);
			FNegative = false;
			FCarry = (uint)(AF.HiByte + source) > 0xFF;

			AF.HiByte = result;
		}

		private void _add_a_r(byte source, string register, bool carry)
		{
			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "A, {0}", register);
					
				}

				else
				{
					DebugOut("ADD", "A, {0}", register);
					
				}
			}

			TStates += 4;
			___generic_accumulator_8bitadd(source, carry);
		}

		private void _add_a_n(bool carry)
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "A, #0x{0:X2}", source);
					
				}

				else
				{
					DebugOut("ADD", "A, #0x{0:X2}", source);
					
				}
			}

			TStates += 7;
			___generic_accumulator_8bitadd(source, carry);
		}

		private void _add_a_hl(bool carry)
		{
			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "A, (HL)");
					
				}

				else
				{
					DebugOut("ADD", "A, (HL)");
					
				}
			}

			TStates += 7;
			___generic_accumulator_8bitadd(Memory[HL], carry);
		}

		private void _add_a_ixd(bool carry)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte source = Memory[IX.Word + offset];

			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "A, (IX+#0x{0:X2})", offset);
					
				}
				else
				{
					DebugOut("ADD", "A, (IX+#0x{0:X2})", offset);
					
				}
			}

			TStates += 19;
			___generic_accumulator_8bitadd(source, carry);
		}

		private void _add_a_iyd(bool carry)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte source = Memory[IY.Word + offset];

			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "A, (IY+#0x{0:X2})", offset);
					
				}
				else
				{
					DebugOut("ADD", "A, (IY+#0x{0:X2})", offset);
					
				}
			}

			TStates += 19;
			___generic_accumulator_8bitadd(source, carry);
		}
		#endregion

		#region And
		private void ___generic_accumulator_8bitand(byte source)
		{
			byte result = (byte)(AF.HiByte & source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = result == 0;
			FHalfCarry = true;
			FPV = StaticHelpers.TestParity(result);
			FNegative = false;
			FCarry = false;

			AF.HiByte = result;
		}

		private void _and_r(byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("AND", "{0}", register);
				
			}

			TStates += 4;
			___generic_accumulator_8bitand(source);
		}

		private void _and_n()
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("AND", "#0x{0:X2}", source);
				
			}

			TStates += 7;
			___generic_accumulator_8bitand(source);
		}

		private void _and_hl()
		{
			if (DebugMode)
			{
				DebugOut("AND", "(HL)");
				
			}

			TStates += 7;
			___generic_accumulator_8bitand(Memory[HL]);
		}

		private void _and_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			
			if (DebugMode)
			{
				DebugOut("AND", "(IX+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IX.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitand(source);
		}

		private void _and_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("AND", "(IY+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IY.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitand(source);
		}
		#endregion

		#region Or
		private void ___generic_accumulator_8bitor(byte source)
		{
			byte result = (byte) (AF.HiByte | source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = (result == 0);
			FHalfCarry = false;
			FPV = StaticHelpers.TestParity(result);
			FNegative = false;
			FCarry = false;

			AF.HiByte = result;
		}

		private void _or_r(byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("OR", "{0}", register);
				
			}

			TStates += 4;
			___generic_accumulator_8bitor(source);
		}

		private void _or_n()
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("OR", "#0x{0:X2}", source);
				
			}

			TStates += 7;
			___generic_accumulator_8bitor(source);
		}

		private void _or_hl()
		{
			if (DebugMode)
			{
				DebugOut("OR", "(HL)");
				
			}

			TStates += 7;
			___generic_accumulator_8bitor(Memory[HL]);
		}

		private void _or_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("OR", "(IX+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IX.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitor(source);
		}

		private void _or_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("OR", "(IY+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IY.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitor(source);
		}
		#endregion

		#region Xor
		private void ___generic_accumulator_8bitxor(byte source)
		{
			byte result = (byte) (AF.HiByte ^ source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = (result == 0);
			FHalfCarry = false;
			FPV = StaticHelpers.TestParity(result);
			FNegative = false;
			FCarry = false;

			AF.HiByte = result;
		}

		private void _xor_r(byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("XOR", "{0}", register);
				
			}

			TStates += 4;
			___generic_accumulator_8bitxor(source);
		}

		private void _xor_n()
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("XOR", "#0x{0:X2}", source);
				
			}

			TStates += 7;
			___generic_accumulator_8bitxor(source);
		}

		private void _xor_hl()
		{
			if (DebugMode)
			{
				DebugOut("XOR", "(HL)");
				
			}

			TStates += 7;
			___generic_accumulator_8bitxor(Memory[HL]);
		}

		private void _xor_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("XOR", "(IX+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IX.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitxor(source);
		}

		private void _xor_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("XOR", "(IY+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IY.Word + offset];

			TStates += 19;
			___generic_accumulator_8bitxor(source);
		}
		#endregion

		#region CP
		private void ___generic_accumulator_8bitcp(byte source)
		{
			byte result = (byte)(AF.HiByte - source);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = result == 0;
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, source, result);
			FPV = StaticHelpers.TestSubtractionOverflow(AF.HiByte, source, result);
			FNegative = true;
			FCarry = (uint)(AF.HiByte - source) > 0xFF;
		}

		private void _cp_r(byte soruce, string register)
		{
			if (DebugMode)
			{
				DebugOut("CP", "{0}", register);
				
			}

			TStates += 4;
			___generic_accumulator_8bitcp(soruce);
		}

		private void _cp_n()
		{
			byte source = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("CP", "#0x{0:X2}", source);
				
			}

			TStates += 7;
			___generic_accumulator_8bitcp(source);
		}

		private void _cp_hl()
		{
			if (DebugMode)
			{
				DebugOut("CP", "(HL)");
				
			}

			TStates += 7;
			___generic_accumulator_8bitcp(Memory[HL]);
		}

		private void _cp_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("CP", "(IX+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IX.Word + offset];
			TStates += 19;
			___generic_accumulator_8bitcp(source);
		}

		private void _cp_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("CP", "(IY+#0x{0:X2})", offset);
				
			}

			byte source = Memory[IY.Word + offset];
			TStates += 19;
			___generic_accumulator_8bitcp(source);
		}
		#endregion

		#region Inc
		private byte ___generic_accumulator_8bitinc(byte source)
		{
			byte result = (byte) (source + 1);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = (result == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(source, 1, result);
			FPV = (source == 0x7F);
			FNegative = false;

			return result;
		}

		private void _inc_r(ref byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("INC", "{0}", register);
				
			}

			TStates += 4;
			source = ___generic_accumulator_8bitinc(source);	
		}

		private void _inc_addr_hl()
		{
			if (DebugMode)
			{
				DebugOut("INC", "HL");
				
			}

			TStates += 11;
			Memory[HL] = ___generic_accumulator_8bitinc(Memory[HL]);
		}

		private void _inc_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("INC", "(IX+#0x{0:X2})", offset);
				
			}

			TStates += 23;
			Memory[IX.Word + offset] = ___generic_accumulator_8bitinc(Memory[IX.Word + offset]);
		}

		private void _inc_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("INC", "(IY+#0x{0:X2})", offset);
				
			}

			TStates += 23;
			Memory[IY.Word + offset] = ___generic_accumulator_8bitinc(Memory[IY.Word + offset]);
		}

		private void _inc_ixh()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IXH");
				
			}

			TStates += 4;
			IX.HiByte = ___generic_accumulator_8bitinc(IX.HiByte);
		}

		private void _inc_ixl()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IXL");
				
			}

			TStates += 4;
			IX.LoByte = ___generic_accumulator_8bitinc(IX.LoByte);
		}

		private void _inc_iyh()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IYH");
				
			}

			TStates += 4;
			IY.HiByte = ___generic_accumulator_8bitinc(IY.HiByte);
		}

		private void _inc_iyl()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IYL");
				
			}

			TStates += 4;
			IY.LoByte = ___generic_accumulator_8bitinc(IY.LoByte);
		}
		#endregion

		#region Dec
		private byte ___generic_accumulator_8bitdec(byte source)
		{
			byte result = (byte) (source - 1);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = (result == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(source, 1, result);
			FPV = (source == 0x80);
			FNegative = true;

			return result;
		}

		private void _dec_r(ref byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("DEC", "{0}", register);
				
			}

			TStates += 4;
			source = ___generic_accumulator_8bitdec(source);
		}

		private void _dec_addr_hl()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "HL");
				
			}

			TStates += 11;
			Memory[HL] = ___generic_accumulator_8bitdec(Memory[HL]);
		}

		private void _dec_ixd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("DEC", "(IX+#0x{0:X2})", offset);
				
			}

			TStates += 23;
			Memory[IX.Word + offset] = ___generic_accumulator_8bitdec(Memory[IX.Word + offset]);
		}

		private void _dec_iyd()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("DEC", "(IY+#0x{0:X2})", offset);
				
			}

			TStates += 23;
			Memory[IY.Word + offset] = ___generic_accumulator_8bitdec(Memory[IY.Word + offset]);
		}

		private void _dec_ixh()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IXH");
				
			}

			TStates += 4;
			IX.HiByte = ___generic_accumulator_8bitdec(IX.HiByte);
		}

		private void _dec_ixl()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IXL");
				
			}

			TStates += 4;
			IX.LoByte = ___generic_accumulator_8bitdec(IX.LoByte);
		}

		private void _dec_iyh()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IYH");
				
			}

			TStates += 4;
			IY.HiByte = ___generic_accumulator_8bitdec(IY.HiByte);
		}

		private void _dec_iyl()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IYL");
				
			}

			TStates += 4;
			IY.LoByte = ___generic_accumulator_8bitdec(IY.LoByte);
		}
		#endregion
	}
}
