using System;
using System.Collections.Generic;
using System.Text;

using Zoofware.ZZ80E;

namespace Zoofware.ZZ80E
{
	public enum InterruptMode
	{
		MODE0,
		MODE1,
		MODE2,
	};

	public partial class CPU
	{
		/// <summary>
		/// 16bit Register Pairs
		/// </summary>
		private RegisterPair IR, AF, BC, DE, HL, IX, IY, SP, PC, AF_, BC_, DE_, HL_;

		/// <summary>
		/// Interrupt flip-flop
		/// If IFF1 is set then maskable interrupts are enabled 
		/// </summary>
		private bool IFF1, IFF2;

		/// <summary>
		/// The current interrupt mode
		/// </summary>
		private InterruptMode InterruptMode;

		/// <summary>
		/// Number of T States completed.
		/// This can be used by a frontend to try and control execution speed
		/// i.e pausing after x cycles have been executed
		/// </summary>
		public uint TStates{ get; set; }

		#region Flag Helpers
		/// <summary>
		/// The F(lag) Register
		/// </summary>
		private byte Flags
		{
			get { return AF.LoByte; }
			set { AF.LoByte = value; }
		}

		/// <summary>
		/// The sign flag; bit 7 of register F
		/// </summary>
		private bool FSign
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 7); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 7, value); }
		}

		/// <summary>
		/// The zero flag; bit 6 of register F
		/// </summary>
		private bool FZero
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 6); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 6, value); }
		}

		/// <summary>
		/// bit 5 of register F
		/// </summary>
		private bool Fb5
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 5); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 5, value); }
		}

		/// <summary>
		/// The half carry flag; bit 4 of register F
		/// </summary>
		private bool FHalfCarry
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 4); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 4, value); }
		}

		/// <summary>
		/// bit 3 of register F
		/// </summary>
		private bool Fb3
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 3); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 3, value); }
		}

		/// <summary>
		/// The parity / overflow flag; bit 2 of register F
		/// </summary>
		private bool FPV
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 2); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 2, value); }
		}

		/// <summary>
		/// The negative flag; bit 1 of register F
		/// </summary>
		private bool FNegative
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 1); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 1, value); }
		}

		/// <summary>
		/// The carry flag; bit 0 of register F
		/// </summary>
		private bool FCarry
		{
			get { return StaticHelpers.TestBit(AF.LoByte, 0); }
			set { AF.LoByte = StaticHelpers.SetBit(AF.LoByte, 0, value); }
		}
		#endregion

		/// <summary>
		/// Memory (RAM+ROM)
		/// </summary>
		private Memory Memory;

		/// <summary>
		/// Contains the current opcode being executed
		/// </summary>
		private ushort opcode;

		/// <summary>
		/// Contains the value of PC at the start of the fetch cycle
		/// </summary>
		public ushort ProgramCounter;

		public ushort StackPointer
		{
			get { return SP.Word; }
			set { SP.Word = value; }
		}

		/// <summary>
		/// Toggles debug output (this will reduce performance)
		/// </summary>
		public bool DebugMode
		{
			get { return _DebugMode; }
			set	{ _DebugMode = value; }
		}


		private bool _DebugMode;

		public string CurrentOpcode;
		public System.IO.StreamWriter DebugStream;

		/// <summary>
		/// Non Maskable Interrupt Pending.
		/// When set to true, the CPU will perform a restart to 0x66.
		/// </summary>
		public bool NMIPending;

		/// <summary>
		/// Maskable Interrupt Pending.
		/// When set to true, the CPU will respond according to InterruptMode.
		/// </summary>
		public bool INTPending;

		/// <summary>
		/// If true then the CPU will do nothing at the start
		/// of a fetch-execute cycle until an interrupt is recieved.
		/// </summary>
		private bool HaltLoop;

		private ushort InterruptReturn;

		public CPU()
		{
			IR = AF = BC = DE = HL = IX = IY = SP = PC = AF_ = BC_ = DE_ = HL_ = new RegisterPair();

			Memory = new Memory();
		}

		public CPU(Memory memory)
			: this()
		{
			this.Memory = memory; 
		}
		
		/// <summary>
		/// Performs the next fetch -> execute cycle
		/// </summary>
		public uint Step()
		{
			/*
			 * Deal with any pending interrupts
			 */
			//if (PC.Word == 0x269) System.Diagnostics.Debugger.Break();
			if (NMIPending)
			{
				InterruptReturn = PC.Word;
				HaltLoop = false;
				Memory[--SP.Word] = PC.HiByte;
				Memory[--SP.Word] = PC.LoByte;
				PC.Word = 0x66;
				NMIPending = false;
				IFF1 = false;
				return 0;
			}

			if (INTPending && InterruptMode == InterruptMode.MODE1 && IFF1)
			{
				InterruptReturn = PC.Word;
				HaltLoop = false;
				Memory[--SP.Word] = PC.HiByte;
				Memory[--SP.Word] = PC.LoByte;
				PC.Word = 0x38;
				IFF1 = IFF2 = false;
				INTPending = false;
				return 0;
			}

			/*
			 * A halt loop is needed here and not in the actual
			 * halt method so that interrupts can still be dealt with.
			 * Having it in _halt() would block the thread and mess up
			 * the program counter when it's saved to the stack
			 */

			if (HaltLoop)
			{
				TStates += 4;
				return 4;
			}

			uint tstates = TStates;

			ProgramCounter = PC.Word;

			/*
			 * Z80 opcodes are either 1 or 2 bytes long. Those that are 2 bytes 
			 * have a prefix (CB, DD, ED and FD). PC will be auto incremented 
			 * for the size of the opcode.
			 * 
			 * Each opcode can have as many as 2 operands. PC won't be auto incremnted
			 * for any operands, as the size of the operand is only known to the opcode function.
			 * There is no way of pre determining it.
			 * 
			 * There are a few edge cases (namely in the Bit group), which can be dealt with seperately.
			 */

			opcode = Memory[PC.Word++];

			/*
			 * If the current byte is a prefix, then fetch the next byte too
			 */

			if (opcode == 0xCB || opcode == 0xDD || opcode == 0xED || opcode == 0xFD)
			{
				/*
				 * The prefix needs to be in the most significant byte
				 */

				opcode <<= 8;

				opcode += Memory[PC.Word++];
			}

			/*
			 * Now do the actual decoding of the opcode and execute the
			 * relavent function. The opcodes are in the same order as they
			 * are found in the Zilog Z80 manual. A copy of which is now firmly 
			 * etched into my cerebral cortex.
			 * 
			 * Hopefully many of these methods will be placed inline by the compiler.
			 * 
			 * Alot of opcodes could have been implemented by the same method. This has been 
			 * avoided intentionally to increase clarity in what is doing what.
			 */

			switch (opcode)
			{
				#region 8 bit load group
				case 0x000A: _ld_a_bc(); break;
				case 0x001A: _ld_a_de(); break;
				case 0x003A: _ld_a_nn(); break;

				case 0x0078: _ld_r_r_(ref AF.HiByte, BC.HiByte, "A, B"); break;
				case 0x0079: _ld_r_r_(ref AF.HiByte, BC.LoByte, "A, C"); break;
				case 0x007A: _ld_r_r_(ref AF.HiByte, DE.HiByte, "A, D"); break;
				case 0x007B: _ld_r_r_(ref AF.HiByte, DE.LoByte, "A, E"); break;
				case 0x007C: _ld_r_r_(ref AF.HiByte, HL.HiByte, "A, H"); break;
				case 0x007D: _ld_r_r_(ref AF.HiByte, HL.LoByte, "A, L"); break;
				case 0x007E: _ld_r_hl(ref AF.HiByte, "A"); break;
				case 0xDD7E: _ld_r_ixd(ref AF.HiByte, "A"); break;
				case 0xFD7E: _ld_r_iyd(ref AF.HiByte, "A"); break;
				case 0x007F: _ld_r_r_(ref AF.HiByte, AF.HiByte, "A, A"); break;

				case 0x003E: _ld_r_n(ref AF.HiByte, "A"); break;

				case 0x0040: _ld_r_r_(ref BC.HiByte, BC.HiByte, "B, B"); break;
				case 0x0041: _ld_r_r_(ref BC.HiByte, BC.LoByte, "B, C"); break;
				case 0x0042: _ld_r_r_(ref BC.HiByte, DE.HiByte, "B, D"); break;
				case 0x0043: _ld_r_r_(ref BC.HiByte, DE.LoByte, "B, E"); break;
				case 0x0044: _ld_r_r_(ref BC.HiByte, HL.HiByte, "B, H"); break;
				case 0x0045: _ld_r_r_(ref BC.HiByte, HL.LoByte, "B, L"); break;
				case 0x0046: _ld_r_hl(ref BC.HiByte, "B"); break;
				case 0xDD46: _ld_r_ixd(ref BC.HiByte, "B"); break;
				case 0xFD46: _ld_r_iyd(ref BC.HiByte, "B"); break;
				case 0x0047: _ld_r_r_(ref BC.HiByte, AF.HiByte, "B, A"); break;

				case 0x0006: _ld_r_n(ref BC.HiByte, "B"); break;

				case 0x0048: _ld_r_r_(ref BC.LoByte, BC.HiByte, "C, B"); break;
				case 0x0049: _ld_r_r_(ref BC.LoByte, BC.LoByte, "C, C"); break;
				case 0x004A: _ld_r_r_(ref BC.LoByte, DE.HiByte, "C, D"); break;
				case 0x004B: _ld_r_r_(ref BC.LoByte, DE.LoByte, "C, E"); break;
				case 0x004C: _ld_r_r_(ref BC.LoByte, HL.HiByte, "C, H"); break;
				case 0x004D: _ld_r_r_(ref BC.LoByte, HL.LoByte, "C, L"); break;
				case 0x004E: _ld_r_hl(ref BC.LoByte, "C"); break;
				case 0xDD4E: _ld_r_ixd(ref BC.LoByte, "C"); break;
				case 0xFD4E: _ld_r_iyd(ref BC.LoByte, "C"); break;
				case 0x004F: _ld_r_r_(ref BC.LoByte, AF.HiByte, "C, A"); break;

				case 0x000E: _ld_r_n(ref BC.LoByte, "C"); break;

				case 0x0050: _ld_r_r_(ref DE.HiByte, BC.HiByte, "D, B"); break;
				case 0x0051: _ld_r_r_(ref DE.HiByte, BC.LoByte, "D, C"); break;
				case 0x0052: _ld_r_r_(ref DE.HiByte, DE.HiByte, "D, D"); break;
				case 0x0053: _ld_r_r_(ref DE.HiByte, DE.LoByte, "D, E"); break;
				case 0x0054: _ld_r_r_(ref DE.HiByte, HL.HiByte, "D, H"); break;
				case 0x0055: _ld_r_r_(ref DE.HiByte, HL.LoByte, "D, L"); break;
				case 0x0056: _ld_r_hl(ref DE.HiByte, "D"); break;
				case 0xDD56: _ld_r_ixd(ref DE.HiByte, "D"); break;
				case 0xFD56: _ld_r_iyd(ref DE.HiByte, "D"); break;
				case 0x0057: _ld_r_r_(ref DE.HiByte, AF.HiByte, "D, A"); break;

				case 0x0016: _ld_r_n(ref DE.HiByte, "D"); break;

				case 0x0058: _ld_r_r_(ref DE.LoByte, BC.HiByte, "E, B"); break;
				case 0x0059: _ld_r_r_(ref DE.LoByte, BC.LoByte, "E, C"); break;
				case 0x005A: _ld_r_r_(ref DE.LoByte, DE.HiByte, "E, D"); break;
				case 0x005B: _ld_r_r_(ref DE.LoByte, DE.LoByte, "E, E"); break;
				case 0x005C: _ld_r_r_(ref DE.LoByte, HL.HiByte, "E, H"); break;
				case 0x005D: _ld_r_r_(ref DE.LoByte, HL.LoByte, "E, L"); break;
				case 0x005E: _ld_r_hl(ref DE.LoByte, "E"); break;
				case 0xDD5E: _ld_r_ixd(ref DE.LoByte, "E"); break;
				case 0xFD5E: _ld_r_iyd(ref DE.LoByte, "E"); break;
				case 0x005F: _ld_r_r_(ref DE.LoByte, AF.HiByte, "E, A"); break;

				case 0x001E: _ld_r_n(ref DE.LoByte, "E"); break;

				case 0x0060: _ld_r_r_(ref HL.HiByte, BC.HiByte, "H, B"); break;
				case 0x0061: _ld_r_r_(ref HL.HiByte, BC.LoByte, "H, C"); break;
				case 0x0062: _ld_r_r_(ref HL.HiByte, DE.HiByte, "H, D"); break;
				case 0x0063: _ld_r_r_(ref HL.HiByte, DE.LoByte, "H, E"); break;
				case 0x0064: _ld_r_r_(ref HL.HiByte, HL.HiByte, "H, H"); break;
				case 0x0065: _ld_r_r_(ref HL.HiByte, HL.LoByte, "H, L"); break;
				case 0x0066: _ld_r_hl(ref HL.HiByte, "H"); break;
				case 0xDD66: _ld_r_ixd(ref HL.HiByte, "H"); break;
				case 0xFD66: _ld_r_iyd(ref HL.HiByte, "H"); break;
				case 0x0067: _ld_r_r_(ref HL.HiByte, AF.HiByte, "H, A"); break;

				case 0x0026: _ld_r_n(ref HL.HiByte, "H"); break;

				case 0x0068: _ld_r_r_(ref HL.LoByte, BC.HiByte, "L, B"); break;
				case 0x0069: _ld_r_r_(ref HL.LoByte, BC.LoByte, "L, C"); break;
				case 0x006A: _ld_r_r_(ref HL.LoByte, DE.HiByte, "L, D"); break;
				case 0x006B: _ld_r_r_(ref HL.LoByte, DE.LoByte, "L, E"); break;
				case 0x006C: _ld_r_r_(ref HL.LoByte, HL.HiByte, "L, H"); break;
				case 0x006D: _ld_r_r_(ref HL.LoByte, HL.LoByte, "L, L"); break;
				case 0x006E: _ld_r_hl(ref HL.LoByte, "L"); break;
				case 0xDD6E: _ld_r_ixd(ref HL.LoByte, "L"); break;
				case 0xFD6E: _ld_r_iyd(ref HL.LoByte, "L"); break;
				case 0x006F: _ld_r_r_(ref HL.LoByte, AF.HiByte, "L, A"); break;

				case 0x002E: _ld_r_n(ref HL.LoByte, "L"); break;

				case 0x0070: _ld_hl_r(BC.HiByte, "B"); break;
				case 0x0071: _ld_hl_r(BC.LoByte, "C"); break;
				case 0x0072: _ld_hl_r(DE.HiByte, "D"); break;
				case 0x0073: _ld_hl_r(DE.LoByte, "E"); break;
				case 0x0074: _ld_hl_r(HL.HiByte, "H"); break;
				case 0x0075: _ld_hl_r(HL.LoByte, "L"); break;
				case 0x0077: _ld_hl_r(AF.HiByte, "A"); break;

				case 0x0036: _ld_hl_n(); break;
				case 0xDD36: _ld_ixd_n(); break;
				case 0xFD36: _ld_iyd_n(); break;

				case 0x0002: _ld_bc_a(); break;
				case 0x0012: _ld_de_a(); break;
				case 0x0032: _ld_nn_a(); break;

				case 0xDD70: _ld_ixd_r(BC.HiByte, "B"); break;
				case 0xDD71: _ld_ixd_r(BC.LoByte, "C"); break;
				case 0xDD72: _ld_ixd_r(DE.HiByte, "D"); break;
				case 0xDD73: _ld_ixd_r(DE.LoByte, "E"); break;
				case 0xDD74: _ld_ixd_r(HL.HiByte, "H"); break;
				case 0xDD75: _ld_ixd_r(HL.LoByte, "L"); break;
				case 0xDD77: _ld_ixd_r(AF.HiByte, "A"); break;

				case 0xFD70: _ld_iyd_r(BC.HiByte, "B"); break;
				case 0xFD71: _ld_iyd_r(BC.LoByte, "C"); break;
				case 0xFD72: _ld_iyd_r(DE.HiByte, "D"); break;
				case 0xFD73: _ld_iyd_r(DE.LoByte, "E"); break;
				case 0xFD74: _ld_iyd_r(HL.HiByte, "H"); break;
				case 0xFD75: _ld_iyd_r(HL.LoByte, "L"); break;
				case 0xFD77: _ld_iyd_r(AF.HiByte, "A"); break;

				case 0xED57: _ld_a_i(); break;
				case 0xED5F: _ld_a_r(); break;
				case 0xED47: _ld_i_a(); break;
				case 0xED4F: _ld_r_a(); break;

				case 0xDD26: _ld_r_n(ref IX.HiByte, "IXh"); break;
				case 0xDD2E: _ld_r_n(ref IX.LoByte, "IXl"); break;
				case 0xFD26: _ld_r_n(ref IY.HiByte, "IYh"); break;
				case 0xFD2E: _ld_r_n(ref IY.LoByte, "IYl"); break;
				#endregion

				#region 16 bit load group
				case 0x0001: _ld_dd_nn(ref BC, "BC"); break;
				case 0x0011: _ld_dd_nn(ref DE, "DE"); break;
				case 0x0021: _ld_dd_nn(ref HL, "HL"); break;
				case 0x0031: _ld_dd_nn(ref SP, "SP"); break;
				case 0xDD21: _ld_ix_nn(); break;
				case 0xFD21: _ld_iy_nn(); break;
				case 0x002A: _ld_hl_addr(); break;
				case 0xED4B: _ld_dd_addr(ref BC, "BC"); break;
				case 0xED5B: _ld_dd_addr(ref DE, "DE"); break;
				case 0xED6B: _ld_dd_addr(ref HL, "HL"); break;
				case 0xED7B: _ld_dd_addr(ref SP, "SP"); break;
				case 0xDD2A: _ld_ix_addr(); break;
				case 0xFD2A: _ld_iy_addr(); break;
				case 0x0022: _ld_addr_hl(); break;
				case 0xED43: _ld_addr_dd(BC, "BC"); break;
				case 0xED53: _ld_addr_dd(DE, "DE"); break;
				case 0xED63: _ld_addr_dd(HL, "HL"); break;
				case 0xED73: _ld_addr_dd(SP, "SP"); break;
				case 0xDD22: _ld_addr_ix(); break;
				case 0xFD22: _ld_addr_iy(); break;
				case 0x00F9: _ld_sp_hl(); break;
				case 0xDDF9: _ld_sp_ix(); break;
				case 0xFDF9: _ld_sp_iy(); break;
				case 0x00C5: _push_qq(BC, "BC"); break;
				case 0x00D5: _push_qq(DE, "DE"); break;
				case 0x00E5: _push_qq(HL, "HL"); break;
				case 0x00F5: _push_qq(AF, "AF"); break;
				case 0xDDE5: _push_ix(); break;
				case 0xFDE5: _push_iy(); break;
				case 0x00C1: _pop_qq(ref BC, "BC"); break;
				case 0x00D1: _pop_qq(ref DE, "DE"); break;
				case 0x00E1: _pop_qq(ref HL, "HL"); break;
				case 0x00F1: _pop_qq(ref AF, "AF"); break;
				case 0xDDE1: _pop_ix(); break;
				case 0xFDE1: _pop_iy(); break;
				#endregion

				#region Exchange, Block Transfer and Search Group
				case 0x00EB: _ex_de_hl(); break;
				case 0x0008: _ex_af_af_(); break;
				case 0x00D9: _exx(); break;
				case 0x00E3: _ex_sp_hl(); break;
				case 0xDDE3: _ex_sp_ix(); break;
				case 0xFDE3: _ex_sp_iy(); break;
				case 0xEDA0: _ldi(); break;
				case 0xEDB0: _ldir(); break;
				case 0xEDA8: _ldd(); break;
				case 0xEDB8: _lddr(); break;
				case 0xEDA1: _cpi(); break;
				case 0xEDB1: _cpir(); break;
				case 0xEDA9: _cpd(); break;
				case 0xEDB9: _cpdr(); break;
				#endregion

				#region 8 bit Arithmetic Group
				case 0x0080: _add_a_r(BC.HiByte, "B", false); break;
				case 0x0081: _add_a_r(BC.LoByte, "C", false); break;
				case 0x0082: _add_a_r(DE.HiByte, "D", false); break;
				case 0x0083: _add_a_r(DE.LoByte, "E", false); break;
				case 0x0084: _add_a_r(HL.HiByte, "H", false); break;
				case 0x0085: _add_a_r(HL.LoByte, "L", false); break;
				case 0x0087: _add_a_r(AF.HiByte, "A", false); break;
				case 0x00C6: _add_a_n(false); break;
				case 0x0086: _add_a_hl(false); break;
				case 0xDD86: _add_a_ixd(false); break;
				case 0xFD86: _add_a_iyd(false); break;

				//adc
				case 0x0088: _add_a_r(BC.HiByte, "B", true); break;
				case 0x0089: _add_a_r(BC.LoByte, "C", true); break;
				case 0x008A: _add_a_r(DE.HiByte, "D", true); break;
				case 0x008B: _add_a_r(DE.LoByte, "E", true); break;
				case 0x008C: _add_a_r(HL.HiByte, "H", true); break;
				case 0x008D: _add_a_r(HL.LoByte, "L", true); break;
				case 0x008F: _add_a_r(AF.HiByte, "A", true); break;
				case 0x00CE: _add_a_n(true); break;
				case 0x008E: _add_a_hl(true); break;
				case 0xDD8E: _add_a_ixd(true); break;
				case 0xFD8E: _add_a_iyd(true); break;

				case 0x0090: _sub_a_r(BC.HiByte, "B", false); break;
				case 0x0091: _sub_a_r(BC.LoByte, "C", false); break;
				case 0x0092: _sub_a_r(DE.HiByte, "D", false); break;
				case 0x0093: _sub_a_r(DE.LoByte, "E", false); break;
				case 0x0094: _sub_a_r(HL.HiByte, "H", false); break;
				case 0x0095: _sub_a_r(HL.LoByte, "L", false); break;
				case 0x0097: _sub_a_r(AF.HiByte, "A", false); break;
				case 0x00D6: _sub_a_n(false); break;
				case 0x0096: _sub_a_hl(false); break;
				case 0xDD96: _sub_a_ixd(false); break;
				case 0xFD96: _sub_a_iyd(false); break;

				//sbc
				case 0x0098: _sub_a_r(BC.HiByte, "B", true); break;
				case 0x0099: _sub_a_r(BC.LoByte, "C", true); break;
				case 0x009A: _sub_a_r(DE.HiByte, "D", true); break;
				case 0x009B: _sub_a_r(DE.LoByte, "E", true); break;
				case 0x009C: _sub_a_r(HL.HiByte, "H", true); break;
				case 0x009D: _sub_a_r(HL.LoByte, "L", true); break;
				case 0x009F: _sub_a_r(AF.HiByte, "A", true); break;
				case 0x00DE: _sub_a_n(true); break;
				case 0x009E: _sub_a_hl(true); break;
				case 0xDD9E: _sub_a_ixd(true); break;
				case 0xFD9E: _sub_a_iyd(true); break;

				//and
				case 0x00A0: _and_r(BC.HiByte, "B"); break;
				case 0x00A1: _and_r(BC.LoByte, "C"); break;
				case 0x00A2: _and_r(DE.HiByte, "D"); break;
				case 0x00A3: _and_r(DE.LoByte, "E"); break;
				case 0x00A4: _and_r(HL.HiByte, "H"); break;
				case 0x00A5: _and_r(HL.LoByte, "L"); break;
				case 0x00A7: _and_r(AF.HiByte, "A"); break;
				case 0x00E6: _and_n(); break;
				case 0x00A6: _and_hl(); break;
				case 0xDDA6: _and_ixd(); break;
				case 0xFDA6: _and_iyd(); break;

				//or
				case 0x00B0: _or_r(BC.HiByte, "B"); break;
				case 0x00B1: _or_r(BC.LoByte, "C"); break;
				case 0x00B2: _or_r(DE.HiByte, "D"); break;
				case 0x00B3: _or_r(DE.LoByte, "E"); break;
				case 0x00B4: _or_r(HL.HiByte, "H"); break;
				case 0x00B5: _or_r(HL.LoByte, "L"); break;
				case 0x00B7: _or_r(AF.HiByte, "A"); break;
				case 0x00F6: _or_n(); break;
				case 0x00B6: _or_hl(); break;
				case 0xDDB6: _or_ixd(); break;
				case 0xFDB6: _or_iyd(); break;

				//xor
				case 0x00A8: _xor_r(BC.HiByte, "B"); break;
				case 0x00A9: _xor_r(BC.LoByte, "C"); break;
				case 0x00AA: _xor_r(DE.HiByte, "D"); break;
				case 0x00AB: _xor_r(DE.LoByte, "E"); break;
				case 0x00AC: _xor_r(HL.HiByte, "H"); break;
				case 0x00AD: _xor_r(HL.LoByte, "L"); break;
				case 0x00AF: _xor_r(AF.HiByte, "A"); break;
				case 0x00EE: _xor_n(); break;
				case 0x00AE: _xor_hl(); break;
				case 0xDDAE: _xor_ixd(); break;
				case 0xFDAE: _xor_iyd(); break;

				//cp
				case 0x00B8: _cp_r(BC.HiByte, "B"); break;
				case 0x00B9: _cp_r(BC.LoByte, "C"); break;
				case 0x00BA: _cp_r(DE.HiByte, "D"); break;
				case 0x00BB: _cp_r(DE.LoByte, "E"); break;
				case 0x00BC: _cp_r(HL.HiByte, "H"); break;
				case 0x00BD: _cp_r(HL.LoByte, "L"); break;
				case 0x00BF: _cp_r(AF.HiByte, "A"); break;
				case 0x00FE: _cp_n(); break;
				case 0x00BE: _cp_hl(); break;
				case 0xDDBE: _cp_ixd(); break;
				case 0xFDBE: _cp_iyd(); break;

				//inc
				case 0x003C: _inc_r(ref AF.HiByte, "A"); break;
				case 0x0004: _inc_r(ref BC.HiByte, "B"); break;
				case 0x000C: _inc_r(ref BC.LoByte, "C"); break;
				case 0x0014: _inc_r(ref DE.HiByte, "D"); break;
				case 0x001C: _inc_r(ref DE.LoByte, "E"); break;
				case 0x0024: _inc_r(ref HL.HiByte, "H"); break;
				case 0x002C: _inc_r(ref HL.LoByte, "L"); break;
				case 0x0034: _inc_addr_hl(); break;
				case 0xDD34: _inc_ixd(); break;
				case 0xFD34: _inc_iyd(); break;
				case 0xDD24: _inc_ixh(); break;
				case 0xFD24: _inc_iyh(); break;
				case 0xDD2C: _inc_ixl(); break;
				case 0xFD2C: _inc_iyl(); break;

				//dec
				case 0x003D: _dec_r(ref AF.HiByte, "A"); break;
				case 0x0005: _dec_r(ref BC.HiByte, "B"); break;
				case 0x000D: _dec_r(ref BC.LoByte, "C"); break;
				case 0x0015: _dec_r(ref DE.HiByte, "D"); break;
				case 0x001D: _dec_r(ref DE.LoByte, "E"); break;
				case 0x0025: _dec_r(ref HL.HiByte, "H"); break;
				case 0x002D: _dec_r(ref HL.LoByte, "L"); break;
				case 0x0035: _dec_addr_hl(); break;
				case 0xDD35: _dec_ixd(); break;
				case 0xFD35: _dec_iyd(); break;
				case 0xDD25: _dec_ixh(); break;
				case 0xFD25: _dec_iyh(); break;
				case 0xDD2D: _dec_ixl(); break;
				case 0xFD2D: _dec_iyl(); break;
				#endregion

				#region General Puropose Arithmetic and CPU Control Group
				case 0x0027: _daa(); break;
				case 0x002F: _cpl(); break;
				case 0xED44: _neg(); break;
				case 0x003F: _ccf(); break;
				case 0x0037: _scf(); break;
				case 0x0000: _nop(); break;
				case 0x0076: _halt(); break;
				case 0x00F3: _di(); break;
				case 0x00FB: _ei(); break;
				case 0xED46: _im0(); break;
				case 0xED56: _im1(); break;
				case 0xED5E: _im2(); break;
				#endregion

				#region 16bit Arithmetic Group 
				case 0xED4A: _add_hl_ss(BC, "BC", true); break;
				case 0xED5A: _add_hl_ss(DE, "DE", true); break;
				case 0xED6A: _add_hl_ss(HL, "HL", true); break;
				case 0xED7A: _add_hl_ss(SP, "SP", true); break;
				case 0x0009: _add_hl_ss(BC, "BC", false); break;
				case 0x0019: _add_hl_ss(DE, "DE", false); break;
				case 0x0029: _add_hl_ss(HL, "HL", false); break;
				case 0x0039: _add_hl_ss(SP, "SP", false); break;

				case 0xDD09: _add_ix_pp(BC, "BC"); break;
				case 0xDD19: _add_ix_pp(DE, "DE"); break;
				case 0xDD29: _add_ix_pp(IX, "IX"); break;
				case 0xDD39: _add_ix_pp(SP, "SP"); break;

				case 0xFD09: _add_iy_pp(BC, "BC"); break;
				case 0xFD19: _add_iy_pp(DE, "DE"); break;
				case 0xFD29: _add_iy_pp(IY, "IY"); break;
				case 0xFD39: _add_iy_pp(SP, "SP"); break;

				case 0xED42: _sbc_hl_ss(BC, "BC"); break;
				case 0xED52: _sbc_hl_ss(DE, "DE"); break;
				case 0xED62: _sbc_hl_ss(HL, "HL"); break;
				case 0xED72: _sbc_hl_ss(SP, "SP"); break;

				case 0x0003: _inc_ss(ref BC, "BC"); break;
				case 0x0013: _inc_ss(ref DE, "DE"); break;
				case 0x0023: _inc_ss(ref HL, "HL"); break;
				case 0x0033: _inc_ss(ref SP, "SP"); break;
				case 0xDD23: _inc_ix(); break;
				case 0xFD23: _inc_iy(); break;

				case 0x000B: _dec_ss(ref BC, "BC"); break;
				case 0x001B: _dec_ss(ref DE, "DE"); break;
				case 0x002B: _dec_ss(ref HL, "HL"); break;
				case 0x003B: _dec_ss(ref SP, "SP"); break;
				case 0xDD2B: _dec_ix(); break;
				case 0xFD2B: _dec_iy(); break;
				#endregion

				#region Rotate and Shift Group
				case 0x0007: _rlca(); break;
				case 0x0017: _rla(); break;
				case 0x000F: _rrca(); break;
				case 0x001F: _rra(); break;

				// RLC r
				case 0xCB00: _rlc(ref BC.HiByte, "B"); break;
				case 0xCB01: _rlc(ref BC.LoByte, "C"); break;
				case 0xCB02: _rlc(ref DE.HiByte, "D"); break;
				case 0xCB03: _rlc(ref DE.LoByte, "E"); break;
				case 0xCB04: _rlc(ref HL.HiByte, "H"); break;
				case 0xCB05: _rlc(ref HL.LoByte, "L"); break;
				case 0xCB06: _rlc_hl(); break;
				case 0xCB07: _rlc(ref AF.HiByte, "A"); break;

				// RL m
				case 0xCB10: _rl(ref BC.HiByte, "B"); break;
				case 0xCB11: _rl(ref BC.LoByte, "C"); break;
				case 0xCB12: _rl(ref DE.HiByte, "D"); break;
				case 0xCB13: _rl(ref DE.LoByte, "E"); break;
				case 0xCB14: _rl(ref HL.HiByte, "H"); break;
				case 0xCB15: _rl(ref HL.LoByte, "L"); break;
				case 0xCB16: _rl_hl(); break;
				case 0xCB17: _rl(ref AF.HiByte, "A"); break;

				// RRC r
				case 0xCB08: _rrc(ref BC.HiByte, "B"); break;
				case 0xCB09: _rrc(ref BC.LoByte, "C"); break;
				case 0xCB0A: _rrc(ref DE.HiByte, "D"); break;
				case 0xCB0B: _rrc(ref DE.LoByte, "E"); break;
				case 0xCB0C: _rrc(ref HL.HiByte, "H"); break;
				case 0xCB0D: _rrc(ref HL.LoByte, "L"); break;
				case 0xCB0E: _rrc_hl(); break;
				case 0xCB0F: _rrc(ref AF.HiByte, "A"); break;

				// RR r
				case 0xCB18: _rr(ref BC.HiByte, "B"); break;
				case 0xCB19: _rr(ref BC.LoByte, "C"); break;
				case 0xCB1A: _rr(ref DE.HiByte, "D"); break;
				case 0xCB1B: _rr(ref DE.LoByte, "E"); break;
				case 0xCB1C: _rr(ref HL.HiByte, "H"); break;
				case 0xCB1D: _rr(ref HL.LoByte, "L"); break;
				case 0xCB1E: _rr_hl(); break;
				case 0xCB1F: _rr(ref AF.HiByte, "A"); break;

				// SLA r
				case 0xCB20: _sla_r(ref BC.HiByte, "B"); break;
				case 0xCB21: _sla_r(ref BC.LoByte, "C"); break;
				case 0xCB22: _sla_r(ref DE.HiByte, "D"); break;
				case 0xCB23: _sla_r(ref DE.LoByte, "E"); break;
				case 0xCB24: _sla_r(ref HL.HiByte, "H"); break;
				case 0xCB25: _sla_r(ref HL.LoByte, "L"); break;
				case 0xCB26: _sla_hl(); break;
				case 0xCB27: _sla_r(ref AF.HiByte, "A"); break;
				
				// SRA r
				case 0xCB28: _sra_r(ref BC.HiByte, "B"); break;
				case 0xCB29: _sra_r(ref BC.LoByte, "C"); break;
				case 0xCB2A: _sra_r(ref DE.HiByte, "D"); break;
				case 0xCB2B: _sra_r(ref DE.LoByte, "E"); break;
				case 0xCB2C: _sra_r(ref HL.HiByte, "H"); break;
				case 0xCB2D: _sra_r(ref HL.LoByte, "L"); break;
				case 0xCB2E: _sra_hl(); break;
				case 0xCB2F: _sra_r(ref AF.HiByte, "A"); break;

				// SLL r
				case 0xCB30: _sll_r(ref BC.HiByte, "B"); break;
				case 0xCB31: _sll_r(ref BC.LoByte, "C"); break;
				case 0xCB32: _sll_r(ref DE.HiByte, "D"); break;
				case 0xCB33: _sll_r(ref DE.LoByte, "E"); break;
				case 0xCB34: _sll_r(ref HL.HiByte, "H"); break;
				case 0xCB35: _sll_r(ref HL.LoByte, "L"); break;
				case 0xCB36: _sll_hl(); break;
				case 0xCB37: _sll_r(ref AF.HiByte, "A"); break;

				// SRL r
				case 0xCB38: _srl_r(ref BC.HiByte, "B"); break;
				case 0xCB39: _srl_r(ref BC.LoByte, "C"); break;
				case 0xCB3A: _srl_r(ref DE.HiByte, "D"); break;
				case 0xCB3B: _srl_r(ref DE.LoByte, "E"); break;
				case 0xCB3C: _srl_r(ref HL.HiByte, "H"); break;
				case 0xCB3D: _srl_r(ref HL.LoByte, "L"); break;
				case 0xCB3E: _srl_hl(); break;
				case 0xCB3F: _srl_r(ref AF.HiByte, "A"); break;

				case 0xED6F: _rld(); break;
				case 0xED67: _rrd(); break;
				#endregion

				#region Bit, Test Set
				#region BIT
				case 0xCB40: _bit_b_r(0, BC.HiByte, "B"); break;
				case 0xCB41: _bit_b_r(0, BC.LoByte, "C"); break;
				case 0xCB42: _bit_b_r(0, DE.HiByte, "D"); break;
				case 0xCB43: _bit_b_r(0, DE.LoByte, "E"); break;
				case 0xCB44: _bit_b_r(0, HL.HiByte, "H"); break;
				case 0xCB45: _bit_b_r(0, HL.LoByte, "L"); break;
				case 0xCB46: _bit_b_hl(0); break;
				case 0xCB47: _bit_b_r(0, AF.HiByte, "A"); break;

				case 0xCB48: _bit_b_r(1, BC.HiByte, "B"); break;
				case 0xCB49: _bit_b_r(1, BC.LoByte, "C"); break;
				case 0xCB4A: _bit_b_r(1, DE.HiByte, "D"); break;
				case 0xCB4B: _bit_b_r(1, DE.LoByte, "E"); break;
				case 0xCB4C: _bit_b_r(1, HL.HiByte, "H"); break;
				case 0xCB4D: _bit_b_r(1, HL.LoByte, "L"); break;
				case 0xCB4E: _bit_b_hl(1); break;
				case 0xCB4F: _bit_b_r(1, AF.HiByte, "A"); break;

				case 0xCB50: _bit_b_r(2, BC.HiByte, "B"); break;
				case 0xCB51: _bit_b_r(2, BC.LoByte, "C"); break;
				case 0xCB52: _bit_b_r(2, DE.HiByte, "D"); break;
				case 0xCB53: _bit_b_r(2, DE.LoByte, "E"); break;
				case 0xCB54: _bit_b_r(2, HL.HiByte, "H"); break;
				case 0xCB55: _bit_b_r(2, HL.LoByte, "L"); break;
				case 0xCB56: _bit_b_hl(2); break;
				case 0xCB57: _bit_b_r(2, AF.HiByte, "A"); break;

				case 0xCB58: _bit_b_r(3, BC.HiByte, "B"); break;
				case 0xCB59: _bit_b_r(3, BC.LoByte, "C"); break;
				case 0xCB5A: _bit_b_r(3, DE.HiByte, "D"); break;
				case 0xCB5B: _bit_b_r(3, DE.LoByte, "E"); break;
				case 0xCB5C: _bit_b_r(3, HL.HiByte, "H"); break;
				case 0xCB5D: _bit_b_r(3, HL.LoByte, "L"); break;
				case 0xCB5E: _bit_b_hl(3); break;
				case 0xCB5F: _bit_b_r(3, AF.HiByte, "A"); break;

				case 0xCB60: _bit_b_r(4, BC.HiByte, "B"); break;
				case 0xCB61: _bit_b_r(4, BC.LoByte, "C"); break;
				case 0xCB62: _bit_b_r(4, DE.HiByte, "D"); break;
				case 0xCB63: _bit_b_r(4, DE.LoByte, "E"); break;
				case 0xCB64: _bit_b_r(4, HL.HiByte, "H"); break;
				case 0xCB65: _bit_b_r(4, HL.LoByte, "L"); break;
				case 0xCB66: _bit_b_hl(4); break;
				case 0xCB67: _bit_b_r(4, AF.HiByte, "A"); break;

				case 0xCB68: _bit_b_r(5, BC.HiByte, "B"); break;
				case 0xCB69: _bit_b_r(5, BC.LoByte, "C"); break;
				case 0xCB6A: _bit_b_r(5, DE.HiByte, "D"); break;
				case 0xCB6B: _bit_b_r(5, DE.LoByte, "E"); break;
				case 0xCB6C: _bit_b_r(5, HL.HiByte, "H"); break;
				case 0xCB6D: _bit_b_r(5, HL.LoByte, "L"); break;
				case 0xCB6E: _bit_b_hl(5); break;
				case 0xCB6F: _bit_b_r(5, AF.HiByte, "A"); break;

				case 0xCB70: _bit_b_r(6, BC.HiByte, "B"); break;
				case 0xCB71: _bit_b_r(6, BC.LoByte, "C"); break;
				case 0xCB72: _bit_b_r(6, DE.HiByte, "D"); break;
				case 0xCB73: _bit_b_r(6, DE.LoByte, "E"); break;
				case 0xCB74: _bit_b_r(6, HL.HiByte, "H"); break;
				case 0xCB75: _bit_b_r(6, HL.LoByte, "L"); break;
				case 0xCB76: _bit_b_hl(6); break;
				case 0xCB77: _bit_b_r(6, AF.HiByte, "A"); break;

				case 0xCB78: _bit_b_r(7, BC.HiByte, "B"); break;
				case 0xCB79: _bit_b_r(7, BC.LoByte, "C"); break;
				case 0xCB7A: _bit_b_r(7, DE.HiByte, "D"); break;
				case 0xCB7B: _bit_b_r(7, DE.LoByte, "E"); break;
				case 0xCB7C: _bit_b_r(7, HL.HiByte, "H"); break;
				case 0xCB7D: _bit_b_r(7, HL.LoByte, "L"); break;
				case 0xCB7E: _bit_b_hl(7); break;
				case 0xCB7F: _bit_b_r(7, AF.HiByte, "A"); break;
				#endregion

				#region RES
				case 0xCB80: _res_b_r(0, ref BC.HiByte, "B"); break;
				case 0xCB81: _res_b_r(0, ref BC.LoByte, "C"); break;
				case 0xCB82: _res_b_r(0, ref DE.HiByte, "D"); break;
				case 0xCB83: _res_b_r(0, ref DE.LoByte, "E"); break;
				case 0xCB84: _res_b_r(0, ref HL.HiByte, "H"); break;
				case 0xCB85: _res_b_r(0, ref HL.LoByte, "L"); break;
				case 0xCB86: _res_b_hl(0); break;
				case 0xCB87: _res_b_r(0, ref AF.HiByte, "A"); break;

				case 0xCB88: _res_b_r(1, ref BC.HiByte, "B"); break;
				case 0xCB89: _res_b_r(1, ref BC.LoByte, "C"); break;
				case 0xCB8A: _res_b_r(1, ref DE.HiByte, "D"); break;
				case 0xCB8B: _res_b_r(1, ref DE.LoByte, "E"); break;
				case 0xCB8C: _res_b_r(1, ref HL.HiByte, "H"); break;
				case 0xCB8D: _res_b_r(1, ref HL.LoByte, "L"); break;
				case 0xCB8E: _res_b_hl(1); break;
				case 0xCB8F: _res_b_r(1, ref AF.HiByte, "A"); break;

				case 0xCB90: _res_b_r(2, ref BC.HiByte, "B"); break;
				case 0xCB91: _res_b_r(2, ref BC.LoByte, "C"); break;
				case 0xCB92: _res_b_r(2, ref DE.HiByte, "D"); break;
				case 0xCB93: _res_b_r(2, ref DE.LoByte, "E"); break;
				case 0xCB94: _res_b_r(2, ref HL.HiByte, "H"); break;
				case 0xCB95: _res_b_r(2, ref HL.LoByte, "L"); break;
				case 0xCB96: _res_b_hl(2); break;
				case 0xCB97: _res_b_r(2, ref AF.HiByte, "A"); break;

				case 0xCB98: _res_b_r(3, ref BC.HiByte, "B"); break;
				case 0xCB99: _res_b_r(3, ref BC.LoByte, "C"); break;
				case 0xCB9A: _res_b_r(3, ref DE.HiByte, "D"); break;
				case 0xCB9B: _res_b_r(3, ref DE.LoByte, "E"); break;
				case 0xCB9C: _res_b_r(3, ref HL.HiByte, "H"); break;
				case 0xCB9D: _res_b_r(3, ref HL.LoByte, "L"); break;
				case 0xCB9E: _res_b_hl(3); break;
				case 0xCB9F: _res_b_r(3, ref AF.HiByte, "A"); break;

				case 0xCBA0: _res_b_r(4, ref BC.HiByte, "B"); break;
				case 0xCBA1: _res_b_r(4, ref BC.LoByte, "C"); break;
				case 0xCBA2: _res_b_r(4, ref DE.HiByte, "D"); break;
				case 0xCBA3: _res_b_r(4, ref DE.LoByte, "E"); break;
				case 0xCBA4: _res_b_r(4, ref HL.HiByte, "H"); break;
				case 0xCBA5: _res_b_r(4, ref HL.LoByte, "L"); break;
				case 0xCBA6: _res_b_hl(4); break;
				case 0xCBA7: _res_b_r(4, ref AF.HiByte, "A"); break;

				case 0xCBA8: _res_b_r(5, ref BC.HiByte, "B"); break;
				case 0xCBA9: _res_b_r(5, ref BC.LoByte, "C"); break;
				case 0xCBAA: _res_b_r(5, ref DE.HiByte, "D"); break;
				case 0xCBAB: _res_b_r(5, ref DE.LoByte, "E"); break;
				case 0xCBAC: _res_b_r(5, ref HL.HiByte, "H"); break;
				case 0xCBAD: _res_b_r(5, ref HL.LoByte, "L"); break;
				case 0xCBAE: _res_b_hl(5); break;
				case 0xCBAF: _res_b_r(5, ref AF.HiByte, "A"); break;

				case 0xCBB0: _res_b_r(6, ref BC.HiByte, "B"); break;
				case 0xCBB1: _res_b_r(6, ref BC.LoByte, "C"); break;
				case 0xCBB2: _res_b_r(6, ref DE.HiByte, "D"); break;
				case 0xCBB3: _res_b_r(6, ref DE.LoByte, "E"); break;
				case 0xCBB4: _res_b_r(6, ref HL.HiByte, "H"); break;
				case 0xCBB5: _res_b_r(6, ref HL.LoByte, "L"); break;
				case 0xCBB6: _res_b_hl(6); break;
				case 0xCBB7: _res_b_r(6, ref AF.HiByte, "A"); break;

				case 0xCBB8: _res_b_r(7, ref BC.HiByte, "B"); break;
				case 0xCBB9: _res_b_r(7, ref BC.LoByte, "C"); break;
				case 0xCBBA: _res_b_r(7, ref DE.HiByte, "D"); break;
				case 0xCBBB: _res_b_r(7, ref DE.LoByte, "E"); break;
				case 0xCBBC: _res_b_r(7, ref HL.HiByte, "H"); break;
				case 0xCBBD: _res_b_r(7, ref HL.LoByte, "L"); break;
				case 0xCBBE: _res_b_hl(7); break;
				case 0xCBBF: _res_b_r(7, ref AF.HiByte, "A"); break;
				#endregion

				#region SET
				case 0xCBC0: _set_b_r(0, ref BC.HiByte, "B"); break;
				case 0xCBC1: _set_b_r(0, ref BC.LoByte, "C"); break;
				case 0xCBC2: _set_b_r(0, ref DE.HiByte, "D"); break;
				case 0xCBC3: _set_b_r(0, ref DE.LoByte, "E"); break;
				case 0xCBC4: _set_b_r(0, ref HL.HiByte, "H"); break;
				case 0xCBC5: _set_b_r(0, ref HL.LoByte, "L"); break;
				case 0xCBC6: _set_b_hl(0); break;
				case 0xCBC7: _set_b_r(0, ref AF.HiByte, "A"); break;

				case 0xCBC8: _set_b_r(1, ref BC.HiByte, "B"); break;
				case 0xCBC9: _set_b_r(1, ref BC.LoByte, "C"); break;
				case 0xCBCA: _set_b_r(1, ref DE.HiByte, "D"); break;
				case 0xCBCB: _set_b_r(1, ref DE.LoByte, "E"); break;
				case 0xCBCC: _set_b_r(1, ref HL.HiByte, "H"); break;
				case 0xCBCD: _set_b_r(1, ref HL.LoByte, "L"); break;
				case 0xCBCE: _set_b_hl(1); break;
				case 0xCBCF: _set_b_r(1, ref AF.HiByte, "A"); break;

				case 0xCBD0: _set_b_r(2, ref BC.HiByte, "B"); break;
				case 0xCBD1: _set_b_r(2, ref BC.LoByte, "C"); break;
				case 0xCBD2: _set_b_r(2, ref DE.HiByte, "D"); break;
				case 0xCBD3: _set_b_r(2, ref DE.LoByte, "E"); break;
				case 0xCBD4: _set_b_r(2, ref HL.HiByte, "H"); break;
				case 0xCBD5: _set_b_r(2, ref HL.LoByte, "L"); break;
				case 0xCBD6: _set_b_hl(2); break;
				case 0xCBD7: _set_b_r(2, ref AF.HiByte, "A"); break;

				case 0xCBD8: _set_b_r(3, ref BC.HiByte, "B"); break;
				case 0xCBD9: _set_b_r(3, ref BC.LoByte, "C"); break;
				case 0xCBDA: _set_b_r(3, ref DE.HiByte, "D"); break;
				case 0xCBDB: _set_b_r(3, ref DE.LoByte, "E"); break;
				case 0xCBDC: _set_b_r(3, ref HL.HiByte, "H"); break;
				case 0xCBDD: _set_b_r(3, ref HL.LoByte, "L"); break;
				case 0xCBDE: _set_b_hl(3); break;
				case 0xCBDF: _set_b_r(3, ref AF.HiByte, "A"); break;

				case 0xCBE0: _set_b_r(4, ref BC.HiByte, "B"); break;
				case 0xCBE1: _set_b_r(4, ref BC.LoByte, "C"); break;
				case 0xCBE2: _set_b_r(4, ref DE.HiByte, "D"); break;
				case 0xCBE3: _set_b_r(4, ref DE.LoByte, "E"); break;
				case 0xCBE4: _set_b_r(4, ref HL.HiByte, "H"); break;
				case 0xCBE5: _set_b_r(4, ref HL.LoByte, "L"); break;
				case 0xCBE6: _set_b_hl(4); break;
				case 0xCBE7: _set_b_r(4, ref AF.HiByte, "A"); break;

				case 0xCBE8: _set_b_r(5, ref BC.HiByte, "B"); break;
				case 0xCBE9: _set_b_r(5, ref BC.LoByte, "C"); break;
				case 0xCBEA: _set_b_r(5, ref DE.HiByte, "D"); break;
				case 0xCBEB: _set_b_r(5, ref DE.LoByte, "E"); break;
				case 0xCBEC: _set_b_r(5, ref HL.HiByte, "H"); break;
				case 0xCBED: _set_b_r(5, ref HL.LoByte, "L"); break;
				case 0xCBEE: _set_b_hl(5); break;
				case 0xCBEF: _set_b_r(5, ref AF.HiByte, "A"); break;

				case 0xCBF0: _set_b_r(6, ref BC.HiByte, "B"); break;
				case 0xCBF1: _set_b_r(6, ref BC.LoByte, "C"); break;
				case 0xCBF2: _set_b_r(6, ref DE.HiByte, "D"); break;
				case 0xCBF3: _set_b_r(6, ref DE.LoByte, "E"); break;
				case 0xCBF4: _set_b_r(6, ref HL.HiByte, "H"); break;
				case 0xCBF5: _set_b_r(6, ref HL.LoByte, "L"); break;
				case 0xCBF6: _set_b_hl(6); break;
				case 0xCBF7: _set_b_r(6, ref AF.HiByte, "A"); break;

				case 0xCBF8: _set_b_r(7, ref BC.HiByte, "B"); break;
				case 0xCBF9: _set_b_r(7, ref BC.LoByte, "C"); break;
				case 0xCBFA: _set_b_r(7, ref DE.HiByte, "D"); break;
				case 0xCBFB: _set_b_r(7, ref DE.LoByte, "E"); break;
				case 0xCBFC: _set_b_r(7, ref HL.HiByte, "H"); break;
				case 0xCBFD: _set_b_r(7, ref HL.LoByte, "L"); break;
				case 0xCBFE: _set_b_hl(7); break;
				case 0xCBFF: _set_b_r(7, ref AF.HiByte, "A"); break;
				#endregion
				#endregion

				#region Jump Group
				case 0x00C3: _jp_nn(); break;
				case 0x00C2:
				case 0x00CA:
				case 0x00D2:
				case 0x00DA:
				case 0x00E2:
				case 0x00EA:
				case 0x00F2:
				case 0x00FA: _jp_cc_nn(); break;
				case 0x0018: _jr_e(); break;
				case 0x0038: _jr_c_e(); break;
				case 0x0030: _jr_nc_e(); break;
				case 0x0028: _jr_z_e(); break;
				case 0x0020: _jr_nz_e(); break;
				case 0x00E9: _jp_hl(); break;
				case 0xDDE9: _jp_ix(); break;
				case 0xFDE9: _jp_iy(); break;
				case 0x0010: _djnz_e(); break;
				#endregion

				#region Call and Return Group
				case 0x00CD: _call_nn(); break;
				case 0x00DC:
				case 0x00D4:
				case 0x00FC:
				case 0x00F4:
				case 0x00CC:
				case 0x00C4:
				case 0x00EC:
				case 0x00E4: _call_cc_nn(); break;
				case 0x00C9: _ret(); break;
				case 0x00F8:
				case 0x00F0:
				case 0x00C8:
				case 0x00C0:
				case 0x00D8:
				case 0x00D0:
				case 0x00E8:
				case 0x00E0: _ret_cc(); break;
				case 0xED4D: _reti(); break;
				case 0xED45: _retn(); break;
				case 0x00C7:
				case 0x00CF:
				case 0x00D7:
				case 0x00DF:
				case 0x00E7:
				case 0x00EF:
				case 0x00F7:
				case 0x00FF: _rst_p(); break;			

				#endregion

				#region Input and Output Group
				case 0x00DB: _in_n(); break;
				case 0xED78: _in_r_c(ref AF.HiByte, "A"); break;
				case 0xEDA2: _ini(); break;
				case 0xEDB2: _inir(); break;
				case 0x00D3: _out_n(); break;
				case 0xED41: _out_c_r(BC.HiByte, "B"); break;
				case 0xED49: _out_c_r(BC.LoByte, "C"); break;
				case 0xED51: _out_c_r(DE.HiByte, "D"); break;
				case 0xED59: _out_c_r(DE.LoByte, "E"); break;
				case 0xED61: _out_c_r(HL.HiByte, "H"); break;
				case 0xED69: _out_c_r(HL.LoByte, "L"); break;
				case 0xED79: _out_c_r(AF.HiByte, "A"); break;
				case 0xED71: _out_c_r(0, "0"); break;
				case 0xEDA3: _outi(); break;
				case 0xEDB3: _otir(); break;
				case 0xEDAB: _outd(); break;
				#endregion

				#region DD/FD CB Prefix
				case 0xDDCB:
				case 0xFDCB:

				/*
				 * An opcode prefixed by DD/FD CB is 4 bytes long
				 * the 3rd byte is the displacement value whilst
				 * the 4th byte is the actual code of the instruction to 
				 * execute.
				 */

				sbyte d = (sbyte)Memory[PC.Word++];
				byte op = Memory[PC.Word++];

				int fullOp = (int)((opcode << 8) + op);

				switch (fullOp)
				{
					// RLC
					case 0xDDCB06: _rlc_ix_d(d); break;
					case 0xFDCB06: _rlc_iy_d(d); break;

					// RL
					case 0xDDCB16: _rl_ix_d(d); break;
					case 0xFDCB16: _rl_iy_d(d); break;

					// RRC
					case 0xDDCB0E: _rrc_ix_d(d); break;
					case 0xFDCB0E: _rrc_iy_d(d); break;

					// RR
					case 0xDDCB1E: _rr_ix_d(d); break;
					case 0xFDCB1E: _rr_iy_d(d); break;

					// SLA
					case 0xDDCB26: _sla_ix_d(d); break;
					case 0xFDCB26: _sla_iy_d(d); break;

					// SRA
					case 0xDDCB2E: _sra_ix_d(d); break;
					case 0xFDCB2E: _sra_iy_d(d); break;

					// SLL
					case 0xDDCB36: _sll_ix_d(d); break;
					case 0xFDCB36: _sll_iy_d(d); break;

					// SRL
					case 0xDDCB3E: _srl_ix_d(d); break;
					case 0xFDCB3E: _srl_iy_d(d); break;

					// BIT
					case 0xDDCB46: _bit_b_ixd(0, d); break;
					case 0xFDCB46: _bit_b_iyd(0, d); break;
					case 0xDDCB4E: _bit_b_ixd(1, d); break;
					case 0xFDCB4E: _bit_b_iyd(1, d); break;
					case 0xDDCB56: _bit_b_ixd(2, d); break;
					case 0xFDCB56: _bit_b_iyd(2, d); break;
					case 0xDDCB5E: _bit_b_ixd(3, d); break;
					case 0xFDCB5E: _bit_b_iyd(3, d); break;
					case 0xDDCB66: _bit_b_ixd(4, d); break;
					case 0xFDCB66: _bit_b_iyd(4, d); break;
					case 0xDDCB6E: _bit_b_ixd(5, d); break;
					case 0xFDCB6E: _bit_b_iyd(5, d); break;
					case 0xDDCB76: _bit_b_ixd(6, d); break;
					case 0xFDCB76: _bit_b_iyd(6, d); break;
					case 0xDDCB7E: _bit_b_ixd(7, d); break;
					case 0xFDCB7E: _bit_b_iyd(7, d); break;

					// RES
					case 0xDDCB86: _res_b_ix_d(0, d); break;
					case 0xFDCB86: _res_b_iy_d(0, d); break;
					case 0xDDCB8E: _res_b_ix_d(1, d); break;
					case 0xFDCB8E: _res_b_iy_d(1, d); break;
					case 0xDDCB96: _res_b_ix_d(2, d); break;
					case 0xFDCB96: _res_b_iy_d(2, d); break;
					case 0xDDCB9E: _res_b_ix_d(3, d); break;
					case 0xFDCB9E: _res_b_iy_d(3, d); break;
					case 0xDDCBA6: _res_b_ix_d(4, d); break;
					case 0xFDCBA6: _res_b_iy_d(4, d); break;
					case 0xDDCBAE: _res_b_ix_d(5, d); break;
					case 0xFDCBAE: _res_b_iy_d(5, d); break;
					case 0xDDCBB6: _res_b_ix_d(6, d); break;
					case 0xFDCBB6: _res_b_iy_d(6, d); break;
					case 0xDDCBBE: _res_b_ix_d(7, d); break;
					case 0xFDCBBE: _res_b_iy_d(7, d); break;

					// SET
					case 0xDDCBC6: _set_b_ixd(0, d); break;
					case 0xFDCBC6: _set_b_iyd(0, d); break;
					case 0xDDCBCE: _set_b_ixd(1, d); break;
					case 0xFDCBCE: _set_b_iyd(1, d); break;
					case 0xDDCBD6: _set_b_ixd(2, d); break;
					case 0xFDCBD6: _set_b_iyd(2, d); break;
					case 0xDDCBDE: _set_b_ixd(3, d); break;
					case 0xFDCBDE: _set_b_iyd(3, d); break;
					case 0xDDCBE6: _set_b_ixd(4, d); break;
					case 0xFDCBE6: _set_b_iyd(4, d); break;
					case 0xDDCBEE: _set_b_ixd(5, d); break;
					case 0xFDCBEE: _set_b_iyd(5, d); break;
					case 0xDDCBF6: _set_b_ixd(6, d); break;
					case 0xFDCBF6: _set_b_iyd(6, d); break;
					case 0xDDCBFE: _set_b_ixd(7, d); break;
					case 0xFDCBFE: _set_b_iyd(7, d); break;

					default: throw new Z80UnknownOpcode(opcode);
				}
				break;
				#endregion

				#region Undocumented Loads
				case 0xDD44: _ld_r_r_(ref BC.HiByte, IX.HiByte, "B, IXh"); break;
				case 0xDD45: _ld_r_r_(ref BC.HiByte, IX.LoByte, "B, IXl"); break;
				case 0xDD4C: _ld_r_r_(ref BC.LoByte, IX.HiByte, "C, IXh"); break;
				case 0xDD4D: _ld_r_r_(ref BC.LoByte, IX.LoByte, "C, IXl"); break;
				case 0xDD54: _ld_r_r_(ref DE.HiByte, IX.HiByte, "D, IXh"); break;
				case 0xDD55: _ld_r_r_(ref DE.HiByte, IX.LoByte, "D, IXl"); break;
				case 0xDD5C: _ld_r_r_(ref DE.LoByte, IX.HiByte, "E, IXh"); break;
				case 0xDD5D: _ld_r_r_(ref DE.LoByte, IX.LoByte, "E, IXl"); break;
				case 0xDD7C: _ld_r_r_(ref AF.HiByte, IX.HiByte, "A, IXh"); break;
				case 0xDD7D: _ld_r_r_(ref AF.HiByte, IX.LoByte, "A, IXl"); break;

				case 0xDD60: _ld_r_r_(ref IX.HiByte, BC.HiByte, "IXh, B"); break;
				case 0xDD61: _ld_r_r_(ref IX.HiByte, BC.LoByte, "IXh, C"); break;
				case 0xDD62: _ld_r_r_(ref IX.HiByte, DE.HiByte, "IXh, D"); break;
				case 0xDD63: _ld_r_r_(ref IX.HiByte, DE.LoByte, "IXh, E"); break;
				case 0xDD64: _ld_r_r_(ref IX.HiByte, IX.HiByte, "IXh, IXh"); break;
				case 0xDD65: _ld_r_r_(ref IX.HiByte, IX.LoByte, "IXh, IXl"); break;
				case 0xDD67: _ld_r_r_(ref IX.HiByte, AF.HiByte, "IXh, A"); break;

				case 0xDD68: _ld_r_r_(ref IX.LoByte, BC.HiByte, "IXl, B"); break;
				case 0xDD69: _ld_r_r_(ref IX.LoByte, BC.LoByte, "IXl, C"); break;
				case 0xDD6A: _ld_r_r_(ref IX.LoByte, DE.HiByte, "IXl, D"); break;
				case 0xDD6B: _ld_r_r_(ref IX.LoByte, DE.LoByte, "IXl, E"); break;
				case 0xDD6C: _ld_r_r_(ref IX.LoByte, IX.HiByte, "IXl, IXh"); break;
				case 0xDD6D: _ld_r_r_(ref IX.LoByte, IX.LoByte, "IXl, IXl"); break;
				case 0xDD6F: _ld_r_r_(ref IX.LoByte, AF.HiByte, "IXl, A"); break;

				case 0xFD44: _ld_r_r_(ref BC.HiByte, IY.HiByte, "B, IYh"); break;
				case 0xFD45: _ld_r_r_(ref BC.HiByte, IY.LoByte, "B, IYl"); break;
				case 0xFD4C: _ld_r_r_(ref BC.LoByte, IY.HiByte, "C, IYh"); break;
				case 0xFD4D: _ld_r_r_(ref BC.LoByte, IY.LoByte, "C, IYl"); break;
				case 0xFD54: _ld_r_r_(ref DE.HiByte, IY.HiByte, "D, IYh"); break;
				case 0xFD55: _ld_r_r_(ref DE.HiByte, IY.LoByte, "D, IYl"); break;
				case 0xFD5C: _ld_r_r_(ref DE.LoByte, IY.HiByte, "E, IYh"); break;
				case 0xFD5D: _ld_r_r_(ref DE.LoByte, IY.LoByte, "E, IYl"); break;
				case 0xFD7C: _ld_r_r_(ref AF.HiByte, IY.HiByte, "A, IYh"); break;
				case 0xFD7D: _ld_r_r_(ref AF.HiByte, IY.LoByte, "A, IYl"); break;

				case 0xFD60: _ld_r_r_(ref IY.HiByte, BC.HiByte, "IYh, B"); break;
				case 0xFD61: _ld_r_r_(ref IY.HiByte, BC.LoByte, "IYh, C"); break;
				case 0xFD62: _ld_r_r_(ref IY.HiByte, DE.HiByte, "IYh, D"); break;
				case 0xFD63: _ld_r_r_(ref IY.HiByte, DE.LoByte, "IYh, E"); break;
				case 0xFD64: _ld_r_r_(ref IY.HiByte, IY.HiByte, "IYh, IYh"); break;
				case 0xFD65: _ld_r_r_(ref IY.HiByte, IY.LoByte, "IYh, IYl"); break;
				case 0xFD67: _ld_r_r_(ref IY.HiByte, AF.HiByte, "IYh, A"); break;

				case 0xFD68: _ld_r_r_(ref IY.LoByte, BC.HiByte, "IYl, B"); break;
				case 0xFD69: _ld_r_r_(ref IY.LoByte, BC.LoByte, "IYl, C"); break;
				case 0xFD6A: _ld_r_r_(ref IY.LoByte, DE.HiByte, "IYl, D"); break;
				case 0xFD6B: _ld_r_r_(ref IY.LoByte, DE.LoByte, "IYl, E"); break;
				case 0xFD6C: _ld_r_r_(ref IY.LoByte, IY.HiByte, "IYl, IYh"); break;
				case 0xFD6D: _ld_r_r_(ref IY.LoByte, IY.LoByte, "IYl, IYl"); break;
				case 0xFD6F: _ld_r_r_(ref IY.LoByte, AF.HiByte, "IYl, A"); break;
				#endregion	

				#region Undocumented Adds
				case 0xDD84: _add_a_r(IX.HiByte, "IXh", false); break;
				case 0xDD85: _add_a_r(IX.LoByte, "IXl", false); break;
				case 0xDD8C: _add_a_r(IX.HiByte, "IXh", true); break;
				case 0xDD8D: _add_a_r(IX.LoByte, "IXl", true); break;
				case 0xDD94: _sub_a_r(IX.HiByte, "IXh", false); break;
				case 0xDD95: _sub_a_r(IX.LoByte, "IXl", false); break;
				case 0xDD9C: _sub_a_r(IX.HiByte, "IXh", true); break;
				case 0xDD9D: _sub_a_r(IX.LoByte, "IXl", true); break;
				case 0xDDA4: _and_r(IX.HiByte, "IXh"); break;
				case 0xDDA5: _and_r(IX.LoByte, "IXl"); break;
				case 0xDDAC: _xor_r(IX.HiByte, "IXh"); break;
				case 0xDDAD: _xor_r(IX.LoByte, "IXl"); break;
				case 0xDDB4: _or_r(IX.HiByte, "IXh"); break;
				case 0xDDB5: _or_r(IX.LoByte, "IXl"); break;
				case 0xDDBC: _cp_r(IX.HiByte, "IXh"); break;
				case 0xDDBD: _cp_r(IX.LoByte, "IXl"); break;

				case 0xFD84: _add_a_r(IY.HiByte, "IYh", false); break;
				case 0xFD85: _add_a_r(IY.LoByte, "IYl", false); break;
				case 0xFD8C: _add_a_r(IY.HiByte, "IYh", true); break;
				case 0xFD8D: _add_a_r(IY.LoByte, "IYl", true); break;
				case 0xFD94: _sub_a_r(IY.HiByte, "IYh", false); break;
				case 0xFD95: _sub_a_r(IY.LoByte, "IYl", false); break;
				case 0xFD9C: _sub_a_r(IY.HiByte, "IYh", true); break;
				case 0xFD9D: _sub_a_r(IY.LoByte, "IYl", true); break;
				case 0xFDA4: _and_r(IY.HiByte, "IYh"); break;
				case 0xFDA5: _and_r(IY.LoByte, "IYl"); break;
				case 0xFDAC: _xor_r(IY.HiByte, "IYh"); break;
				case 0xFDAD: _xor_r(IY.LoByte, "IYl"); break;
				case 0xFDB4: _or_r(IY.HiByte, "IYh"); break;
				case 0xFDB5: _or_r(IY.LoByte, "IYl"); break;
				case 0xFDBC: _cp_r(IY.HiByte, "IYh"); break;
				case 0xFDBD: _cp_r(IY.LoByte, "IYl"); break;
				#endregion

				#region dupes
				case 0xDD40:
				case 0xDD41:
				case 0xDD42:
				case 0xDD43:
				case 0xDD47:
				case 0xDD48:
				case 0xDD49:
				case 0xDD4A:
				case 0xDD4B:
				case 0xDD4F:
				case 0xDD50:
				case 0xDD51:
				case 0xDD52:
				case 0xDD53:
				case 0xDD57:
				case 0xDD58:
				case 0xDD59:
				case 0xDD5A:
				case 0xDD5B:
				case 0xDD5F:

				case 0xDD76:
				case 0xDD78:
				case 0xDD79:
				case 0xDD7A:
				case 0xDD7B:
				case 0xDD7F:
				case 0xDD80:
				case 0xDD81:
				case 0xDD82:
				case 0xDD83:
				case 0xDD87:
				case 0xDD88:
				case 0xDD89:
				case 0xDD8A:
				case 0xDD8B:
				case 0xDD8F:
				case 0xDD90:
				case 0xDD91:
				case 0xDD92:
				case 0xDD93:
				case 0xDD97:
				case 0xDD98:
				case 0xDD99:
				case 0xDD9A:
				case 0xDD9B:
				case 0xDD9F:
				case 0xDDA0:
				case 0xDDA1:
				case 0xDDA2:
				case 0xDDA3:
				case 0xDDA7:
				case 0xDDA8:
				case 0xDDA9:
				case 0xDDAA:
				case 0xDDAB:
				case 0xDDAF:
				case 0xDDB0:
				case 0xDDB1:
				case 0xDDB2:
				case 0xDDB3:
				case 0xDDB7:
				case 0xDDB8:
				case 0xDDB9:
				case 0xDDBA:
				case 0xDDBB:
				case 0xDDBF:

				case 0xFD40:
				case 0xFD41:
				case 0xFD42:
				case 0xFD43:
				case 0xFD47:
				case 0xFD48:
				case 0xFD49:
				case 0xFD4A:
				case 0xFD4B:
				case 0xFD4F:
				case 0xFD50:
				case 0xFD51:
				case 0xFD52:
				case 0xFD53:
				case 0xFD57:
				case 0xFD58:
				case 0xFD59:
				case 0xFD5A:
				case 0xFD5B:
				case 0xFD5F:

				case 0xFD76:
				case 0xFD78:
				case 0xFD79:
				case 0xFD7A:
				case 0xFD7B:
				case 0xFD7F:
				case 0xFD80:
				case 0xFD81:
				case 0xFD82:
				case 0xFD83:
				case 0xFD87:
				case 0xFD88:
				case 0xFD89:
				case 0xFD8A:
				case 0xFD8B:
				case 0xFD8F:
				case 0xFD90:
				case 0xFD91:
				case 0xFD92:
				case 0xFD93:
				case 0xFD97:
				case 0xFD98:
				case 0xFD99:
				case 0xFD9A:
				case 0xFD9B:
				case 0xFD9F:
				case 0xFDA0:
				case 0xFDA1:
				case 0xFDA2:
				case 0xFDA3:
				case 0xFDA7:
				case 0xFDA8:
				case 0xFDA9:
				case 0xFDAA:
				case 0xFDAB:
				case 0xFDAF:
				case 0xFDB0:
				case 0xFDB1:
				case 0xFDB2:
				case 0xFDB3:
				case 0xFDB7:
				case 0xFDB8:
				case 0xFDB9:
				case 0xFDBA:
				case 0xFDBB:
				case 0xFDBF: PC.Word--; return 0;
				#endregion

				default:
					throw new Z80UnknownOpcode(opcode);
			}	

			return (TStates - tstates);
		}

		private void DebugOut(string mnemonic)
		{
			DebugOut(mnemonic, null);
		}

		private void DebugOut(string mnemonic, string operands, params object[] args)
		{
			CurrentOpcode = string.Format(mnemonic + "\t" + operands, args);

			if (DebugStream != null)
			{
				DebugStream.WriteLine("@{0:X4}h :: Executing Opcode: {1:X4}", ProgramCounter, opcode);
				DebugStream.WriteLine(CurrentOpcode);
				DebugStream.WriteLine("AF: {0:X4} | BC: {1:X4} | DE: {2:X4} | HL: {3:X4}", AF.Word, BC.Word, DE.Word, HL.Word);
				DebugStream.WriteLine("af: {0:X4} | bc: {1:X4} | de: {2:X4} | hl: {3:X4}", AF_.Word, BC_.Word, DE_.Word, HL_.Word);
				DebugStream.WriteLine("SP: {0:X4} | PC: {1:X4} | IX: {2:X4} | IY: {3:X4}", SP.Word, PC.Word, IX.Word, IY.Word);
				DebugStream.WriteLine("FZ: {5} | FC: {0} | FHC: {1} | FPV: {2} | FN: {3} | FS: {4}", FCarry, FHalfCarry, FPV, FNegative, FSign, FZero);
				DebugStream.WriteLine();
			}
		}
	}
}
