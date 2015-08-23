using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _call_nn()
		{
			ushort addr = (ushort) (Memory[PC.Word++] + (Memory[PC.Word++] << 8));

			if (DebugMode)
			{
				DebugOut("CALL", "#0x{0:X4}", addr);
				
			}

			TStates += 17;

			Memory[--SP.Word] = PC.HiByte;
			Memory[--SP.Word] = PC.LoByte;
			PC.Word = addr;
		}

		private void _call_cc_nn()
		{
			ushort addr = (ushort)(Memory[PC.Word++] + (Memory[PC.Word++] << 8));

			byte condition = (byte) ((opcode & 0x38) >> 3);
			string _condition;

			if (DebugMode)
			{
				switch (condition)
				{
					case 0: _condition = "NZ"; break;
					case 1: _condition = "Z"; break;
					case 2: _condition = "NC"; break;
					case 3: _condition = "C"; break;
					case 4: _condition = "PO"; break;
					case 5: _condition = "PE"; break;
					case 6: _condition = "P"; break;
					case 7: _condition = "M"; break;
					default: throw new Z80UnknownOpcode(opcode);
				}

				DebugOut("CALL", "{0}, #0x{1:X4}", _condition, addr);
				
			}

			TStates += 10;

			switch (condition)
			{
				case 0: if (FZero) return; break;
				case 1: if (!FZero) return; break;
				case 2: if (FCarry) return; break;
				case 3: if (!FCarry) return; break;
				case 4: if (FPV) return; break;
				case 5: if (!FPV) return; break;
				case 6: if (FSign) return; break;
				case 7: if (!FSign) return; break;
				default: throw new Z80UnknownOpcode(opcode);
			}

			TStates += 7;
			Memory[--SP.Word] = PC.HiByte;
			Memory[--SP.Word] = PC.LoByte;
			PC.Word = addr;
		}

		private void _ret()
		{
			if (DebugMode)
			{
				DebugOut("RET");
				
			}

			TStates += 10;

			PC.LoByte = Memory[SP.Word++];
			PC.HiByte = Memory[SP.Word++];
		}

		private void _ret_cc()
		{
			byte condition = (byte)((opcode & 0x38) >> 3);
			string _condition;

			if (DebugMode)
			{
				switch (condition)
				{
					case 0: _condition = "NZ"; break;
					case 1: _condition = "Z"; break;
					case 2: _condition = "NC"; break;
					case 3: _condition = "C"; break;
					case 4: _condition = "PO"; break;
					case 5: _condition = "PE"; break;
					case 6: _condition = "P"; break;
					case 7: _condition = "M"; break;
					default: throw new Z80UnknownOpcode(opcode);
				}

				DebugOut("RET", "{0}", _condition);
				
			}

			TStates += 5;

			switch (condition)
			{
				case 0: if (FZero) return; break;
				case 1: if (!FZero) return; break;
				case 2: if (FCarry) return; break;
				case 3: if (!FCarry) return; break;
				case 4: if (FPV) return; break;
				case 5: if (!FPV) return; break;
				case 6: if (FSign) return; break;
				case 7: if (!FSign) return; break;
				default: throw new Z80UnknownOpcode(opcode);
			}

			TStates += 6;

			PC.LoByte = Memory[SP.Word++];
			PC.HiByte = Memory[SP.Word++];
		}

		private void _reti()
		{
			if (DebugMode)
			{
				DebugOut("RETI");				
			}

			TStates += 14;

			PC.LoByte = Memory[SP.Word++];
			PC.HiByte = Memory[SP.Word++];

			IFF1 = IFF2;
		}

		private void _retn()
		{
			if (DebugMode)
			{
				DebugOut("RETN");				
			}

			TStates += 14;

			PC.LoByte = Memory[SP.Word++];
			PC.HiByte = Memory[SP.Word++];

			IFF1 = IFF2;
		}

		private void _rst_p()
		{
			ushort addr = (ushort) (opcode & 0x38);

			if (DebugMode)
			{
				DebugOut("RST", "#0x{0:X2}", addr);
				
			}

			Memory[--SP.Word] = PC.HiByte;
			Memory[--SP.Word] = PC.LoByte;
			PC.Word = addr;
			IFF2 = IFF1 = false;
		}
	}
}
