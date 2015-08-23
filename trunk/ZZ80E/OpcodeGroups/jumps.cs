using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _jp_nn()
		{
			ushort addr = (ushort) (Memory[PC.Word++] + (Memory[PC.Word++] << 8));

			if (DebugMode)
			{
				DebugOut("JP", "#0x{0:X4}", addr);
				
			}

			TStates += 10;
			PC.Word = addr;
		}

		private void _jp_cc_nn()
		{
			ushort addr = (ushort) (Memory[PC.Word++] + (Memory[PC.Word++] << 8));

			/*
			 * Get the actual condition
			 * bits 5,4,3
			 */

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

				DebugOut("JP", "{0}, #0x{1:X4}", _condition, addr);
				
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

			PC.Word = addr;
		}

		private void _jr_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("JR", "#0x{0:X2}", offset);
				
			}

			TStates += 12;

			PC.Word = (ushort)(PC.Word + offset);
		}

		private void _jr_c_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("JR", "C, #0x{0:X2}", offset);
				
			}

			if (!FCarry)
			{
				TStates += 7;
				return;
			}

			TStates += 12;
			PC.Word = (ushort)(PC.Word + offset);
		}

		private void _jr_nc_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("JR", "NC, #0x{0:X2}", offset);
				
			}

			if (FCarry)
			{
				TStates += 7;
				return;
			}

			TStates += 12;
			PC.Word = (ushort)(PC.Word + offset);
		}

		private void _jr_z_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("JR", "Z, #0x{0:X2}", offset);
				
			}

			if (!FZero)
			{
				TStates += 7;
				return;
			}

			TStates += 12;
			PC.Word = (ushort)(PC.Word + offset);
		}

		private void _jr_nz_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("JR", "NZ, #0x{0:X2}", offset);
				
			}

			if (FZero)
			{
				TStates += 7;
				return;
			}

			TStates += 12;
			PC.Word = (ushort)(PC.Word + offset);
		}

		private void _jp_hl()
		{
			if (DebugMode)
			{
				DebugOut("JP", "(HL)");
				
			}

			TStates += 4;
			PC = HL;
		}

		private void _jp_ix()
		{
			if (DebugMode)
			{
				DebugOut("JP", "(IX)");
				
			}

			TStates += 8;
			PC = IX;
		}

		private void _jp_iy()
		{
			if (DebugMode)
			{
				DebugOut("JP", "(IY)");
				
			}

			TStates += 8;
			PC = IY;
		}

		private void _djnz_e()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("DJNZ", "{0:X2}", offset);
				
			}

			BC.HiByte--;

			if (BC.HiByte != 0)
			{
				TStates += 13;
				PC.Word = (ushort)(PC.Word + offset);
			}
			else
			{
				TStates += 8;
			}
		}
	}
}
