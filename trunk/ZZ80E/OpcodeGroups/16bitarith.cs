using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _add_hl_ss(RegisterPair source, string register, bool carry)
		{
			if (DebugMode)
			{
				if (carry)
				{
					DebugOut("ADC", "HL, {0}", register);
				}
				else
				{
					DebugOut("ADD", "HL, {0}", register);
				}
			}

			TStates += 11;

			if (carry && FCarry)
			{
				source.Word++;
			}

			ushort result = (ushort)(HL.Word + source.Word);

			if (carry)
			{
				FSign = StaticHelpers.TestBit(result, 15);
				FZero = result == 0;
				FNegative = false;
				FPV = StaticHelpers.TestAdditionOverflow(HL.Word, source.Word, result);
			}

			FHalfCarry = StaticHelpers.TestHalfCarry(HL.Word, source.Word, result);
			FNegative = false;
			FCarry = (HL.Word + source.Word) > 0xFFFF;

			HL.Word = result;
		}

		private void _sbc_hl_ss(RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("SBC", "HL, {0}", register);				
			}

			TStates += 15;

			if(FCarry)
			{
				source.Word++;
			}

			ushort result = (ushort)(HL.Word - source.Word);

			FSign = StaticHelpers.TestBit(result, 15);
			FZero = result == 0;
			FHalfCarry = StaticHelpers.TestHalfCarry(HL.Word, source.Word, result);
			FPV = StaticHelpers.TestSubtractionOverflow(HL.Word, source.Word, result);
			FNegative = true;
			FCarry = (uint)(HL.Word - source.Word) > 0xFFFF;

			HL.Word = result;
		}

		private void _add_ix_pp(RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("ADD", "IX, {0}", register);				
			}

			TStates += 15;
			
			FHalfCarry = StaticHelpers.TestHalfCarry(IX.Word, source.Word, (ushort)(IX.Word + source.Word));
			FNegative = false;
			FCarry = (IX.Word + source.Word > 0xFFFF);

			IX.Word += source.Word;
		}

		private void _add_iy_pp(RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("ADD", "IY, {0}", register);				
			}

			TStates += 15;
			
			FHalfCarry = StaticHelpers.TestHalfCarry(IY.Word, source.Word, (ushort)(IY.Word + source.Word));
			FNegative = false;
			FCarry = (IY.Word + source.Word > 0xFFFF);

			IY.Word += source.Word;
		}

		private void _inc_ss(ref RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("INC", "{0}", register);				
			}

			TStates += 6;

			source.Word++;
		}

		private void _inc_ix()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IX");
				
			}

			TStates += 10;

			IX.Word++;
		}

		private void _inc_iy()
		{
			if (DebugMode)
			{
				DebugOut("INC", "IY");
				
			}

			TStates += 10;

			IY.Word++;
		}
		//
		private void _dec_ss(ref RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("DEC", "{0}", register);
				
			}

			TStates += 6;

			source.Word--;
		}

		private void _dec_ix()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IX");
				
			}

			TStates += 10;

			IX.Word--;
		}

		private void _dec_iy()
		{
			if (DebugMode)
			{
				DebugOut("DEC", "IY");
				
			}

			TStates += 10;

			IY.Word--;
		}
	}
}
