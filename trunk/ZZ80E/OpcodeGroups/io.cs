using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _in_n()
		{
			byte n = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("IN", "A, (#0x{0:X2})", n);
				
			}

			TStates += 11;

			AF.HiByte = ReadIOPort(AF.HiByte, n);
		}

		private void _in_r_c(ref byte destination, string register)
		{
			if (DebugMode)
			{
				DebugOut("IN", "{0}, (C)", register);
			}

			TStates += 12;

			destination = ReadIOPort(BC.HiByte, BC.LoByte);

			FSign = StaticHelpers.TestBit(destination, 7);
			FZero = destination == 0;
			FHalfCarry = FNegative = false;
			FPV = StaticHelpers.TestParity(destination);
		}

		private void _ini()
		{
			if (DebugMode)
			{
				DebugOut("INI");
				
			}

			TStates += 16;

			Memory[HL] = ReadIOPort(BC.HiByte, BC.LoByte);
			BC.HiByte--;
			HL.Word++;
		}

		private void _inir()
		{
			if (DebugMode)
			{
				DebugOut("INIR");
			}

			BC.HiByte--;

			Memory[HL] = ReadIOPort(BC.HiByte, BC.LoByte);
			
			HL.Word++;

			if (BC.HiByte != 0)
			{
				PC.Word -= 2;
				TStates += 21;
			}
			else
			{
				TStates += 16;
			}
		}

		private void _out_n()
		{
			byte n = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("OUT", "(#0x{0:X2}), A", n);
				
			}

			TStates += 11;
			//if (AF.HiByte == 0x50) System.Diagnostics.Debugger.Break();
			WriteIOPort(AF.HiByte, n, AF.HiByte);
		}

		private void _out_c_r(byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("OUT", "(C), {0}", register);
				
			}

			TStates += 12;
			WriteIOPort(BC.HiByte, BC.LoByte, source);
		}

		private void _outi()
		{
			if (DebugMode)
			{
				DebugOut("OUTI");
				
			}

			TStates += 16;

			byte tmp = Memory[HL];
			BC.HiByte--;

			FSign = StaticHelpers.TestBit(BC.HiByte, 7);
			FZero = BC.HiByte == 0;
			FNegative = true;

			WriteIOPort(BC.HiByte, BC.LoByte, tmp);
			HL.Word++;
		}

		private void _otir()
		{
			if (DebugMode)
			{
				DebugOut("OTIR");			
			}

			byte tmp = Memory[HL];
			BC.HiByte--;

			WriteIOPort(BC.HiByte, BC.LoByte, tmp);
			HL.Word++;

			if (BC.HiByte != 0)
			{
				PC.Word -= 2;
				TStates += 21;
			}
			else
			{
				TStates += 16;
			}
		}

		private void _outd()
		{
			if (DebugMode)
			{
				DebugOut("OUTD");
			}

			TStates += 16;

			byte tmp = Memory[HL];
			BC.HiByte--;

			FSign = StaticHelpers.TestBit(BC.HiByte, 7);
			FZero = BC.HiByte == 0;
			FNegative = true;

			WriteIOPort(BC.HiByte, BC.LoByte, tmp);
			HL.Word--;
		}
	}
}
