using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		private void _ld_r_r_(ref byte destination, byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("LD", "{0}", register);
				
			}

			TStates += 4;

			destination = source;
		}

		private void _ld_r_n(ref byte destination, string register)
		{
			byte operand = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "{0}, #0x{1:X2}", register, operand);
				
			}

			TStates += 7;

			destination = operand;
		}

		private void _ld_r_hl(ref byte destination, string register)
		{
			if (DebugMode)
			{
				DebugOut("LD", "{0}, (HL)", register);
				
			}

			TStates += 7;
			//if(HL.Word == 0x21e3) { System.Diagnostics.Debugger.Break(); }
			destination = Memory[HL.Word];
		}

		private void _ld_r_iyd(ref byte destination, string register)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "{0}, (IY+#0x{1:X2})", register, offset);
				
			}

			TStates += 19;

			destination = Memory[IY.Word + offset];
		}

		private void _ld_r_ixd(ref byte destination, string register)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "{0}, (IX+#0x{1:X2})", register, offset);
				
			}

			TStates += 19;

			destination = Memory[IX.Word + offset];
		}

		private void _ld_hl_r(byte source, string register)
		{
			if (DebugMode)
			{
				DebugOut("LD", "(HL), {0}", register);
			}

			TStates += 7;

			Memory[HL.Word] = source;
		}

		private void _ld_ixd_r(byte source, string register)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "(IX+#0x{0:X2}), {1}", offset, register);
				
			}

			TStates += 19;

			Memory[IX.Word + offset] = source;
		}

		private void _ld_iyd_r(byte source, string register)
		{
			sbyte offset = (sbyte)Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "(IY+#0x{0:X2}), {1}", offset, register);
				
			}

			TStates += 19;

			Memory[IY.Word + offset] = source;
		}

		private void _ld_hl_n()
		{
			byte operand = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "(HL), #0x{0:X2}", operand);
				
			}

			TStates += 10;

			Memory[HL.Word] = operand;
		}

		private void _ld_ixd_n()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte value = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "(IX+#0x{0:X2}), {1:X2}", offset, value);
				
			}

			TStates += 19;

			Memory[IX.Word + offset] = value;
		}

		private void _ld_iyd_n()
		{
			sbyte offset = (sbyte)Memory[PC.Word++];
			byte value = Memory[PC.Word++];

			if (DebugMode)
			{
				DebugOut("LD", "(IY+#0x{0:X2}), {1:X2}", offset, value);
				
			}

			TStates += 19;

			Memory[IY.Word + offset] = value;
		}

		private void _ld_a_bc()
		{
			if (DebugMode)
			{
				DebugOut("LD", "A, (BC)");
				
			}

			TStates += 7;

			AF.HiByte = Memory[BC.Word];
		}

		private void _ld_a_de()
		{
			if (DebugMode)
			{
				DebugOut("LD", "A, (DE)");
				
			}

			TStates += 7;

			AF.HiByte = Memory[DE.Word];
		}

		private void _ld_a_nn()
		{
			int addr = Memory[PC.Word++] + (Memory[PC.Word++] << 8);

			if (DebugMode)
			{
				DebugOut("LD", "A, (#0x{0:X4})", addr);
				
			}

			TStates += 13;

			AF.HiByte = Memory[addr];
		}

		private void _ld_bc_a()
		{
			if (DebugMode)
			{
				DebugOut("LD", "(BC), A");
				
			}

			TStates += 7;
			Memory[BC] = AF.HiByte;
		}

		private void _ld_de_a()
		{
			if (DebugMode)
			{
				DebugOut("LD", "(DE), A");
				
			}

			TStates += 7;
			Memory[DE] = AF.HiByte;
		}

		private void _ld_nn_a()
		{
			ushort addr = (ushort)(Memory[PC.Word++] + (Memory[PC.Word++] << 8));
			if (DebugMode)
			{
				DebugOut("LD", "(#0x{0:X4}), A", addr);
				
			}

			TStates += 13;
			Memory[addr] = AF.HiByte;
		}

		private void _ld_a_i()
		{
			if (DebugMode)
			{
				DebugOut("LD", "A, I");
				
			}

			TStates += 9;

			AF.HiByte = IR.HiByte;

			FSign = StaticHelpers.TestBit(IR.HiByte, 7);
			FZero = (IR.HiByte == 0);
			FHalfCarry = false;
			FPV = IFF2;
			FNegative = false;
		}

		private void _ld_a_r()
		{
			if (DebugMode)
			{
				DebugOut("LD", "A, R");
				
			}

			TStates += 9;

			AF.HiByte = IR.LoByte;

			FSign = StaticHelpers.TestBit(IR.LoByte, 7);
			FZero = (IR.LoByte == 0);
			FHalfCarry = false;
			FPV = IFF2;
			FNegative = false;
		}

		private void _ld_i_a()
		{
			if (DebugMode)
			{
				DebugOut("LD", "I, A");
				
			}

			TStates += 9;

			IR.HiByte = AF.HiByte;
		}

		private void _ld_r_a()
		{
			if (DebugMode)
			{
				DebugOut("LD", "R, A");
				
			}

			TStates += 9;

			IR.LoByte = AF.HiByte;
		}
	}
}
