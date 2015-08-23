using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _daa()
		{
			if (DebugMode)
			{
				DebugOut("DAA");
				
			}

			TStates += 4;

			/*
			 * Taken from http://wikiti.brandonw.net/?title=Z80_Instruction_Set
			 * 
			 * tmp := a,
			 *	if nf then
			 *		if hf or [a AND 0x0f > 9] then tmp -= 0x06
			 *		if cf or [a > 0x99] then tmp -= 0x60
			 *	else
			 *		if hf or [a AND 0x0f > 9] then tmp += 0x06
			 *		if cf or [a > 0x99] then tmp += 0x60
			 *	endif,
			 *	tmp => flags, cf := cf OR [a > 0x99],
			 *	hf := a.4 XOR tmp.4, a := tmp
			 */

			byte tmp = AF.HiByte;

			if (FNegative)
			{
				if (FHalfCarry || ((AF.HiByte & 0x0F) > 9)) tmp -= 0x06;
				if (FCarry || (AF.HiByte > 0x99)) tmp -= 0x60;
			}
			else
			{
				if (FHalfCarry || ((AF.HiByte & 0x0F) > 9)) tmp += 0x06;
				if (FCarry || (AF.HiByte > 0x99)) tmp += 0x60;
			}

			FHalfCarry = StaticHelpers.TestBit(AF.HiByte, 4) ^ StaticHelpers.TestBit(tmp, 4);
			FCarry = FCarry | (AF.HiByte > 0x99);

			AF.HiByte = tmp;

			FSign = StaticHelpers.TestBit(AF.HiByte, 7);
			FZero = AF.HiByte == 0;
			FPV = StaticHelpers.TestParity(AF.HiByte);

		}

		private void _cpl()
		{
			if (DebugMode)
			{
				DebugOut("CPL");
				
			}

			TStates += 4;

			AF.HiByte = (byte) ~AF.HiByte;

			FHalfCarry = true;
			FNegative = true;
		}

		private void _neg()
		{
			if (DebugMode)
			{
				DebugOut("NEG");
				
			}

			TStates += 8;

			FPV = (AF.HiByte == 0x80);
			FCarry = (AF.HiByte != 0x00);

			byte result = (byte) (0 - AF.HiByte);

			FSign = StaticHelpers.TestBit(result, 7);
			FZero = (result == 0);
			FHalfCarry = StaticHelpers.TestHalfCarry(0, AF.HiByte, result);
			FNegative = true;

			AF.HiByte = result;
		}

		private void _ccf()
		{
			if (DebugMode)
			{
				DebugOut("CCF");
				
			}

			TStates += 4;

			FHalfCarry = FCarry;
			FNegative = false;
			FCarry = !FCarry;
		}

		private void _scf()
		{
			if (DebugMode)
			{
				DebugOut("SCF");
				
			}

			TStates += 4;

			FHalfCarry = false;
			FNegative = false;
			FCarry = true;
		}

		private void _nop()
		{
			if (DebugMode)
			{
				DebugOut("NOP");
				
			}

			TStates += 4;
		}

		private void _halt()
		{
			if (DebugMode)
			{
				DebugOut("HALT");
				
			}

			TStates += 4;

			HaltLoop = true;
		}

		private void _di()
		{
			if (DebugMode)
			{
				DebugOut("DI");
				
			}

			TStates += 4;

			IFF1 = IFF2 = false;
		}

		private void _ei()
		{
			if (DebugMode)
			{
				DebugOut("EI");
				
			}

			TStates += 4;
			
			/*
			 * Force the next instruction to be executed before interrupts
			 * are enabled again.
			 */

			TStates += Step();
			
			IFF1 = IFF2 = true;
		}

		private void _im0()
		{
			if (DebugMode)
			{
				DebugOut("IM", "0");
				
			}

			TStates += 8;

			InterruptMode = InterruptMode.MODE0;
		}

		private void _im1()
		{
			if (DebugMode)
			{
				DebugOut("IM", "1");
				
			}

			TStates += 8;

			InterruptMode = InterruptMode.MODE1;
		}

		private void _im2()
		{
			if (DebugMode)
			{
				DebugOut("IM", "2");
				
			}

			TStates += 8;

			InterruptMode = InterruptMode.MODE2;
		}
	}
}
