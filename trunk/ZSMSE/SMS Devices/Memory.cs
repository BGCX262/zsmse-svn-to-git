using System;
using Zoofware.ZZ80E;

namespace Zoofware.ZSMSE
{
	/// <summary>
	/// Emulates the Sega Master System II memory manager.
	/// </summary>
	class SMS2Memory : Memory 
	{
		/// <summary>
		/// Internal read only memory. 8KiB.
		/// </summary>
		private byte[] ROM = new byte[8192];

		/// <summary>
		/// Internal RAM
		/// </summary>
		private byte[] RAM = new byte[8192];

		/// <summary>
		/// 2 banks of random access memory. 16KiB each.
		/// </summary>
		private byte[,] RAMBanks = new byte[2, 0x4000];

		/// <summary>
		/// Cartridge ROM.
		/// </summary>
		private byte[] Cartridge = new byte[0x100000];

		/// <summary>
		/// Contains a map of each slot to it's page number.
		/// </summary>
		private int[] SlotPages = new int[] { 0, 1, 2 };

		private int RAMBankSelect;
		private bool RAMEnableSlot2;

		public bool CartLoaded;

		public override byte this[int index]
		{
			/*
			 * RAM is mirrored in 2 places, 
			 * 0xC000-0xDFFF and 0xE000-0xFFFF
			 * 
			 * So if the index is greater than 0xDFFF then
			 * take away 0x2000 to get the actual address
			 */

			get
			{
				//if (index >= 0xDFEC && index <= 0xDFF0) System.Diagnostics.Debugger.Break();

				if (index > 0xDFFF)
				{
					index -= 0x2000;
				}

				/*
				 * Should always point to the same address in the cartridge
				 * as this is where interrupt handlers are.
				 */

				if (index < 0x400)
				{
					return Cartridge[index];
				}

				/*
				 * Cartridge ROM slot 0
				 */

				if (index < 0x4000)
				{
					return Cartridge[(SlotPages[0] * 0x4000) + index];
				}
				
				/*
				 * Cartridge ROM slot 1
				 */

				if (index < 0x8000)
				{
					index -= 0x4000;
					return Cartridge[(SlotPages[1] * 0x4000) + index];
				}

				/*
				 * Cartridge / RAM slot 2
				 * 
				 */

				if (index < 0xC000)
				{
					index -= 0x8000;

					if (RAMEnableSlot2)
					{
						return RAMBanks[RAMBankSelect, index];
					}

					else
					{
						return Cartridge[(SlotPages[2] * 0x4000) + index];
					}
				}

				/*
				 * Internal RAM
				 */

				if (index < 0xE000)
				{
					index -= 0xC000;
					return RAM[index];
				}

				return 0;
			}
			set
			{
				/*
				 * Deal with the upper 8 bytes of RAM
				 * which control pages and so on...
				 */

				if (index >= 0xFFF8 && index <= 0xFFFF)
				{
					/*
					 * Memory registers
					 */

					if (index == 0xFFFC)
					{
						/*
						 * Bit 2 is RAM bank select
						 * Bit 3 RAM enable ($8000-$bfff)
						 */

						RAMBankSelect = Convert.ToInt32(StaticHelpers.TestBit(value, 2));
						RAMEnableSlot2 = StaticHelpers.TestBit(value, 3);
					}

					/*
					 * Slot control
					 */

					if (index >= 0xFFFD && index <= 0xFFFF)
					{
						int slot = index - 0xFFFD;
						SlotPages[slot] = value;
						return;
					}


					return;
				}

				/*
				 * Mirror RAM
				 */

				if (index > 0xDFFF)
				{
					index -= 0x2000;
				}			

				/*
				 * Shouldn't be able to write to ROM
				 */

				if (index < 0x8000)
				{
					return;
				}

				/*
				 * Slot 2 ROM / RAM Banks
				 * Only writable if set to RAM
				 */

				if (index < 0xC000)
				{
					index -= 0x8000;

					if (RAMEnableSlot2)
					{
						RAMBanks[RAMBankSelect, index] = value;
					}

					return;
				}

				/*
				 * Internal RAM
				 */

				if (index < 0xE000)
				{
					index -= 0xC000;
					RAM[index] = value;
				}
			}
		}
	
		/// <summary>
		/// Loads a ROM / Cartridge
		/// </summary>
		/// <param name="src"></param>
		/// <param name="destination"></param>
		public override void Load(byte[] src)
		{
			CartLoaded = true;
			Buffer.BlockCopy(src, 0, Cartridge, 0, src.Length);
		}
	}

	class SMSMemoryException : ApplicationException
	{
		public SMSMemoryException(string message)
			: base(message)
		{ }
	}
}
