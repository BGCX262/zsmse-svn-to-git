using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _ex_de_hl()
		{
			if (DebugMode)
			{
				DebugOut("EX", "DE, HL");
			}

			TStates += 4;

			RegisterPair tmp = DE;

			DE = HL;
			HL = tmp;
		}

		private void _ex_af_af_()
		{
			if (DebugMode)
			{
				DebugOut("EX", "AF, AF'");
			}

			TStates += 4;

			RegisterPair tmp = AF;

			AF = AF_;
			AF_ = tmp;
		}

		private void _exx()
		{
			if (DebugMode)
			{
				DebugOut("EXX", null);
			}

			TStates += 4;

			RegisterPair tmp = BC;

			BC = BC_;
			BC_ = tmp;

			tmp = DE;

			DE = DE_;
			DE_ = tmp;

			tmp = HL;

			HL = HL_;
			HL_ = tmp;
		}

		private void _ex_sp_hl()
		{
			if (DebugMode)
			{
				DebugOut("EX", "(SP), HL");
			}

			TStates += 19;

			byte tmp = HL.LoByte;

			HL.LoByte = Memory[SP.Word];
			Memory[SP.Word] = tmp;

			tmp = HL.HiByte;

			HL.HiByte = Memory[SP.Word + 1];
			Memory[SP.Word + 1] = tmp;
		}

		private void _ex_sp_ix()
		{
			if (DebugMode)
			{
				DebugOut("EX", "(SP), IX");
			}

			TStates += 23;

			byte tmp = IX.LoByte;

			IX.LoByte = Memory[SP.Word];
			Memory[SP.Word] = tmp;

			tmp = IX.HiByte;

			IX.HiByte = Memory[SP.Word + 1];
			Memory[SP.Word + 1] = tmp;
		}

		private void _ex_sp_iy()
		{
			if (DebugMode)
			{
				DebugOut("EX", "(SP), IY");
			}

			TStates += 23;

			byte tmp = IX.LoByte;

			IY.LoByte = Memory[SP.Word];
			Memory[SP.Word] = tmp;

			tmp = IY.HiByte;

			IY.HiByte = Memory[SP.Word + 1];
			Memory[SP.Word + 1] = tmp;
		}

		private void _ldi()
		{
			if (DebugMode)
			{
				DebugOut("LDI", null);
			}

			TStates += 16;

			Memory[DE.Word] = Memory[HL.Word];

			DE.Word++;
			HL.Word++;
			BC.Word--;

			FHalfCarry = false;
			FPV = (BC.Word != 0);
			FNegative = false;
		}

		private void _ldir()
		{
			if (DebugMode)
			{
				DebugOut("LDIR", null);
			}

			Memory[DE.Word++] = Memory[HL.Word++];
			BC.Word--;

			if (BC.Word == 0)
			{
				TStates += 16;
			}
			else
			{
				PC.Word -= 2;
				TStates += 21;
			}

			FHalfCarry = FNegative = FPV = false;
		}


		private void _ldd()
		{
			if (DebugMode)
			{
				DebugOut("LDD");
			}

			TStates += 16;

			Memory[DE.Word] = Memory[HL.Word];
			DE.Word--;
			HL.Word--;
			BC.Word--;

			FHalfCarry = false;
			FPV = (BC.Word != 0);
			FNegative = false;
		}

		private void _lddr()
		{
			if (DebugMode)
			{
				DebugOut("LDDR");
			}

			uint states;

			if (BC.Word == 0)
			{
				BC.Word = ushort.MaxValue;
				states = 16;
			}
			else
			{
				states = 21;
			}

			do
			{
				TStates += states;

				Memory[DE.Word] = Memory[HL.Word];
				DE.Word--;
				HL.Word--;
				BC.Word--;
			} while (BC.Word > 0);

			FHalfCarry = false;
			FPV = false;
			FNegative = false;
		}

		private void _cpi()
		{
			if (DebugMode)
			{
				DebugOut("CPI");
			}

			TStates += 16;

			byte compare = (byte)(AF.HiByte - Memory[HL]);

			BC.Word--;

			FSign = StaticHelpers.TestBit(compare, 7);
			FZero = (compare == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, Memory[HL], compare);
			FPV = (BC.Word != 0);
			FNegative = true;

			HL.Word++;
		}

		private void _cpir()
		{
			if (DebugMode)
			{
				DebugOut("CPIR");
			}

			byte compare, states;

			if (BC.Word == 0)
			{
				states = 16;
				BC.Word = 0xFFFF;
			}
			else
			{
				states = 21;
			}

			do
			{
				TStates += states;

				compare = (byte)(AF.HiByte - Memory[HL]);
				HL.Word++;
				BC.Word--;
			} while (BC.Word > 0 && compare != 0);

			FSign = StaticHelpers.TestBit(compare, 7);
			FZero = (compare == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, Memory[HL.Word - 1], compare);
			FPV = (BC.Word != 0);
			FNegative = true;
		}

		private void _cpd()
		{
			if (DebugMode)
			{
				DebugOut("CPD");
			}

			TStates += 16;

			byte compare = (byte)(AF.HiByte - Memory[HL]);

			BC.Word--;

			FSign = StaticHelpers.TestBit(compare, 7);
			FZero = (compare == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, Memory[HL], compare);
			FPV = (BC.Word != 0);
			FNegative = true;

			HL.Word--;
		}

		private void _cpdr()
		{
			if (DebugMode)
			{
				DebugOut("CPDR");
			}

			byte compare, states;

			if (BC.Word == 0)
			{
				states = 16;
				BC.Word = 0xFFFF;
			}
			else
			{
				states = 21;
			}

			do
			{
				TStates += states;

				compare = (byte)(AF.HiByte - Memory[HL]);
				HL.Word--;
				BC.Word--;
			} while (BC.Word > 0 && compare != 0);

			FSign = StaticHelpers.TestBit(compare, 7);
			FZero = (compare == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(AF.HiByte, Memory[HL.Word + 1], compare);
			FPV = (BC.Word != 0);
			FNegative = true;
		}
	}
}
