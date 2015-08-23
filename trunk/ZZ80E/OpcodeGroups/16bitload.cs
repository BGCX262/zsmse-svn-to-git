using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _ld_dd_nn(ref RegisterPair destination, string register)
		{
			byte loByte = Memory[PC.Word++];
			byte hiByte = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "{0}, #0x{1:X4}", register, loByte + (hiByte << 8));
				
			}

			TStates += 10;

			destination.HiByte = hiByte;
			destination.LoByte = loByte;
		}

		private void _ld_ix_nn()
		{
			byte loByte = Memory[PC.Word++];
			byte hiByte = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "IX, #0x{0:X4}", loByte + (hiByte << 8));
				
			}

			TStates += 14;

			IX.HiByte = hiByte;
			IX.LoByte = loByte;
		}

		private void _ld_iy_nn()
		{
			byte loByte = Memory[PC.Word++];
			byte hiByte = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "IY, #0x{0:X4}", loByte + (hiByte << 8));
				
			}

			TStates += 14;

			IY.HiByte = hiByte;
			IY.LoByte = loByte;
		}

		private void _ld_hl_addr()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "HL, (#0x{0:X4})", addr);
				
			}

			TStates += 16;

			HL.LoByte = Memory[addr];
			HL.HiByte = Memory[addr + 1];
		}

		private void _ld_dd_addr(ref RegisterPair destination, string register)
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "{0}, (#0x{1:X4})", register, addr);
				
			}

			TStates += 20;

			destination.LoByte = Memory[addr];
			destination.HiByte = Memory[addr + 1];
		}

		private void _ld_ix_addr()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "IX, (#0x{0:X4})", addr);
				
			}

			TStates += 20;

			IX.LoByte = Memory[addr];
			IX.HiByte = Memory[addr + 1];
		}

		private void _ld_iy_addr()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "IY, (#0x{0:X4})", addr);
				
			}

			TStates += 20;

			IY.LoByte = Memory[addr];
			IY.HiByte = Memory[addr + 1];
		}

		private void _ld_addr_hl()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "(#0x{0:X4}), HL", addr);
				
			}

			TStates += 16;

			Memory[addr] = HL.LoByte;
			Memory[addr + 1] = HL.HiByte;
		}

		private void _ld_addr_dd(RegisterPair source, string register)
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "(#0x{0:X4}), {1}", addr, register);
				
			}

			TStates += 20;

			Memory[addr] = source.LoByte;
			Memory[addr + 1] = source.HiByte;
		}

		private void _ld_addr_ix()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "(#0x{0:X4}), IX", addr);
				
			}

			TStates += 20;

			Memory[addr] = IX.LoByte;
			Memory[addr + 1] = IX.HiByte;
		}

		private void _ld_addr_iy()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "(#0x{0:X4}), IY", addr);
				
			}

			TStates += 20;

			Memory[addr] = IY.LoByte;
			Memory[addr + 1] = IY.HiByte;
		}

		private void _ld_sp_hl()
		{
			if (DebugMode)
			{
				DebugOut("LD", "SP, HL");
				
			}

			TStates += 6;

			SP = HL;
		}

		private void _ld_sp_ix()
		{
			if (DebugMode)
			{
				DebugOut("LD", "SP, IX");
				
			}

			TStates += 10;

			SP = IX;
		}

		private void _ld_sp_iy()
		{
			if (DebugMode)
			{
				DebugOut("LD", "SP, IY");
				
			}

			TStates += 10;

			SP = IY;
		}

		private void _push_qq(RegisterPair source, string register)
		{
			if (DebugMode)
			{
				DebugOut("PUSH", "{0}", register);
				
			}

			TStates += 11;

			SP.Word--;
			Memory[SP.Word] = source.HiByte;
			SP.Word--;
			Memory[SP.Word] = source.LoByte;
		}

		private void _push_ix()
		{
			if (DebugMode)
			{
				DebugOut("PUSH", "IX");
				
			}

			TStates += 15;

			Memory[--SP.Word] = IX.HiByte;
			Memory[--SP.Word] = IX.LoByte;
		}

		private void _push_iy()
		{
			if (DebugMode)
			{
				DebugOut("PUSH", "IY");
				
			}

			TStates += 15;

			Memory[--SP.Word] = IY.HiByte;
			Memory[--SP.Word] = IY.LoByte;
		}

		private void _pop_qq(ref RegisterPair destination, string register)
		{
			if (DebugMode)
			{
				DebugOut("POP", "{0}", register);
				
			}

			TStates += 10;

			destination.LoByte = Memory[SP.Word++];
			destination.HiByte = Memory[SP.Word++];
		}

		private void _pop_ix()
		{
			if (DebugMode)
			{
				DebugOut("POP", "IX");
				
			}

			TStates += 14;

			IX.LoByte = Memory[SP.Word++];
			IX.HiByte = Memory[SP.Word++];
		}

		private void _pop_iy()
		{
			if (DebugMode)
			{
				DebugOut("POP", "IY");
				
			}

			TStates += 14;

			IY.LoByte = Memory[SP.Word++];
			IY.HiByte = Memory[SP.Word++];
		}
	}
}
