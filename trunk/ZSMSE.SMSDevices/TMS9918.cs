using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zoofware.ZZ80E;

namespace Zoofware.ZSMSE.Devices
{
	/// <summary>
	/// TMS9918a emulator.
	/// </summary>
	public abstract class TMS9918a
	{
		/// <summary>
		/// Video region to output in.
		/// </summary>
		public enum VideoRegions
		{
			PAL,
			NTSC,
		};

		/// <summary>
		/// SMS2 Specific TMS9918 display modes.
		/// </summary>
		public enum DisplayModes
		{
			Graphic_1,
			Text,
			Graphic_2,
			Mode_1_2,
			Multicolor,
			Mode_1_3,
			Mode_2_3,
			Mode_1_2_3,
			Mode_4,
			InvalidTextMode,
			Mode_4_224line,
			Mode_4_240line,
		};

		/// <summary>
		/// Sega specific TMS9918a versions.
		/// </summary>
		public enum Versions
		{
			/// <summary>
			/// Sega Mark III (Sega Master System). TMS9918a 315-5124
			/// </summary>
			SMS = 3155124,

			/// <summary>
			/// Sega Master System II. TMS9918a 315-5246
			/// </summary>
			SMS2 = 3155246,

			/// <summary>
			/// Sega Game Gear. TMS9918a 315-5378
			/// </summary>
			GG = 3155378,

			/// <summary>
			/// Sega Genesis / Sega Mega Drive. TMS9918a 315-5313
			/// </summary>
			MD = 3155313,
		};

		/// <summary>
		/// TMS9918a version to emulate.
		/// </summary>
		public Versions Version;

		/// <summary>
		/// Video region to emulate.
		/// </summary>
		public VideoRegions VideoRegion;

		/// <summary>
		/// Mode that the VDP is currently in.
		/// </summary>
		public DisplayModes DisplayMode
		{
			get
			{
				return DisplayModes.Mode_4;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private bool IsSecondControlWrite;

		/// <summary>
		/// Wraps around at 0x3FFF as this is the maximum VRAM size.
		/// </summary>
		private ushort AddressRegister
		{
			get { return _AddressRegister; }
			set
			{
				_AddressRegister = (ushort)(value & 0x3FFF);
			}
		}

		private ushort _AddressRegister;

		/// <summary>
		/// Determines the function for the VDP to carry out on the next IO write.
		/// </summary>
		private byte ControlCode;

		/// <summary>
		/// Buffer for the VDP to return on the next IO read.
		/// </summary>
		private byte ReadBuffer;

		/// <summary>
		/// Internal video ram.
		/// </summary>
		protected byte[] VRAM = new byte[0x4000];

		/// <summary>
		/// Colour palettes.
		/// </summary>
		protected byte[] CRAM = new byte[0x20];

		/// <summary>
		/// Internal registers.
		/// </summary>
		//protected byte[] Registers = new byte[0x10];
		protected byte[] Registers = Enumerable.Range(0, 0x10).Select(x => (byte)0xFF).ToArray();

		#region Register Helpers
		/// <summary>
		/// Disable vertical scrolling for columns 24-31.
		/// </summary>
		protected bool R0_D7 { get { return StaticHelpers.TestBit(Registers[0], 7); } }

		/// <summary>
		/// Disable horizonal scrolling for rows 0-1.
		/// </summary>
		protected bool R0_D6 { get { return StaticHelpers.TestBit(Registers[0], 6); } }

		/// <summary>
		/// Mask column 0 with overscan color for R7.
		/// </summary>
		protected bool R0_D5 { get { return StaticHelpers.TestBit(Registers[0], 5); } }

		/// <summary>
		/// Line interrupt enabled.
		/// </summary>
		protected bool R0_D4_IE1 { get { return StaticHelpers.TestBit(Registers[0], 4); } }

		/// <summary>
		/// Shift sprites left by 8 pixels.
		/// </summary>
		protected bool R0_D3_EC { get { return StaticHelpers.TestBit(Registers[0], 3); } }

		/// <summary>
		/// Mode 4 enabled.
		/// </summary>
		protected bool R0_D2_M4 { get { return StaticHelpers.TestBit(Registers[0], 2); } }

		/// <summary>
		/// Enable height selector mode 4.
		/// </summary>
		protected bool R0_D1_M2 { get { return StaticHelpers.TestBit(Registers[0], 1); } }

		/// <summary>
		/// No sync, monochrome display.
		/// </summary>
		protected bool R0_D0 { get { return StaticHelpers.TestBit(Registers[0], 0); } }

		/// <summary>
		/// Display active.
		/// </summary>
		protected bool R1_D6_BLK { get { return StaticHelpers.TestBit(Registers[1], 6); } }

		/// <summary>
		/// Frame interrupt enabled.
		/// </summary>
		protected bool R1_D5_IE0 { get { return StaticHelpers.TestBit(Registers[1], 5); } }

		/// <summary>
		/// Select 224 line screen for mode 4.
		/// </summary>
		protected bool R1_D4_M1 { get { return StaticHelpers.TestBit(Registers[1], 4); } }

		/// <summary>
		/// Select 240 line screen for mode 4.
		/// </summary>
		protected bool R1_D3_M3 { get { return StaticHelpers.TestBit(Registers[1], 3); } }

		/// <summary>
		/// If mode 4 then sprites are 8x16 if true, 8x8 if false.
		/// </summary>
		protected bool R1_D1 { get { return StaticHelpers.TestBit(Registers[1], 1); } }

		/// <summary>
		/// Double srites sizes.
		/// </summary>
		protected bool R1_D0 { get { return StaticHelpers.TestBit(Registers[1], 0); } }

		protected bool R6_D2 { get { return StaticHelpers.TestBit(Registers[6], 2); } }
		/// <summary>
		/// Overscan / backdrop colour.
		/// </summary>
		protected byte R7_BKCL { get { return (byte)(Registers[7] & 0xF); } }

		protected byte R8_StartingColumn { get { return (byte)((Registers[8] & 0xF8) >> 3); } }

		protected byte R8_FineScroll { get { return (byte)(Registers[8] & 7); } }
		/// <summary>
		/// Vertical scroll starting row
		/// </summary>
		protected byte R9_StartingRow { get { return (byte)((Registers[9] & 0xF8) >> 3); } }

		protected byte R9_FineScroll { get { return (byte)(Registers[9] & 7); } }

		#endregion

		/// <summary>
		/// If true, then the VDP has requested an interrupt.
		/// </summary>
		public bool InterruptPending;

		/// <summary>
		/// Status register.
		/// </summary>
		private byte Status;

		#region Abstracts
		public abstract void Tick(int cycles, bool drawScanline = true);
		#endregion

		#region Status Helpers
		/// <summary>
		/// Frame interrupt pending.
		/// </summary>
		protected bool INT
		{
			get { return StaticHelpers.TestBit(Status, 7); }
			set { Status = StaticHelpers.SetBit(Status, 7, value); }
		}

		/// <summary>
		/// Sprite Overflow.
		/// </summary>
		protected bool OVR
		{
			get { return StaticHelpers.TestBit(Status, 6); }
			set { Status = StaticHelpers.SetBit(Status, 6, value); }
		}

		/// <summary>
		/// Sprite Collision.
		/// </summary>
		protected bool COL
		{
			get { return StaticHelpers.TestBit(Status, 5); }
			set { Status = StaticHelpers.SetBit(Status, 5, value); }
		}
		#endregion

		/// <summary>
		/// Horizontal Counter.
		/// </summary>
		public byte HCounter
		{
			/*
			 * The HCount is really 9 bits as there are 342 pixels in a scanline,
			 * however the Z80 IO bus is 8 bits wide so just return the upper 8 bits.
			 */

			get { return (byte)(_HCounter >> 1); }
		}

		/// <summary>
		/// Vertical Counter.
		/// </summary>
		public byte VCounter
		{
			get { return (byte)_VCounter; }
		}

		protected uint _HCounter;
		protected byte _VCounter;

		/// <summary>
		/// Used by the VDP to count down the number of lines until
		/// the next line interrupt.
		/// </summary>
		protected byte LICounter;

		//protected ushort SATOffset = 0x3F00;
		//protected ushort NameTableOffset = 0x3800;

		protected ushort SATOffset
		{
			get
			{
				return (ushort)(0x3F00 & ((Registers[5] & 0x7F) << 8));
			}
		}

		protected ushort NameTableOffset
		{
			get
			{
				return (ushort)(0x3800 & ((Registers[2] & 0xE) << 10));
			}
		}

		protected int SpriteSize
		{
			get
			{
				if (R1_D1)
				{
					return 16;
				}
				else
				{
					return 8;
				}
			}
		}

		private void SetRegister(byte value, byte registerNumber)
		{
			/*
			 * The lower 4 bits of registerNumber denote which register
			 * to write to.
			 */

			byte register = (byte)(registerNumber & 0xF);

			/*
			 * There are 16 registers (0->0x10), anything higher is ignored.
			 */

			if (register > 0x10) return;

			Registers[register] = value;
		}

		#region Ports
		/// <summary>
		/// Writes data to the control port.
		/// </summary>
		/// <param name="highAddr">High 8 bits of Z80 IO bus</param>
		/// <param name="value">value to write</param>
		public void WriteToControlPort(byte highAddr, byte value)
		{
			if (IsSecondControlWrite)
			{
				/*
				 * The lower 6 bits of the written value are the upper
				 * 6 bits of the address register
				 */

				AddressRegister |= (ushort)((value & 0x3F) << 8);

				/*
				 * The VDP may carry out additional processing based on the upper 2 bits
				 * of the value
				 */

				ControlCode = (byte)((value & 0xC0) >> 6);

				switch (ControlCode)
				{
					case 0: ReadBuffer = VRAM[AddressRegister++]; break;
					case 1: break; // n/a for control port
					case 2: SetRegister((byte)(AddressRegister & 0xFF), value); break;
					case 3: break; // n/a for control port
					default: throw new TMS9918aException("Invalid control code");
				}

				IsSecondControlWrite = false;
			}
			else
			{
				/*
				 * When the first byte is written, the lower 8 bits of the address register are updated
				 */

				AddressRegister = value;

				IsSecondControlWrite = true;
			}
		}

		public byte ReadControlPort(byte highAddr)
		{
			byte status_ = Status;

			Status = 0;
			InterruptPending = false;
			IsSecondControlWrite = false;

			return status_;
		}

		public void WriteToDataPort(byte highAddr, byte value)
		{
			IsSecondControlWrite = false;

			switch (ControlCode)
			{
				case 0: VRAM[AddressRegister] = value; break;
				case 1: VRAM[AddressRegister] = value; break;
				case 2: VRAM[AddressRegister] = value; break;

				/*
				 * CRAM has a max addressable range of 0 -> 1F
				 * So discard any higher bits.
				 */

				case 3: CRAM[AddressRegister & 0x1F] = value; break;
				default: throw new TMS9918aException("Invalid control code");
			}

			ReadBuffer = value;
			AddressRegister++;
		}

		public byte ReadDataPort(byte highAddr)
		{
			byte buffer = ReadBuffer;

			ReadBuffer = VRAM[AddressRegister++]; ;

			IsSecondControlWrite = false;

			return buffer;
		}
		#endregion
	}

	class TMS9918aException : Exception
	{
		public TMS9918aException(string message)
			: base(message) { }
	}
}
