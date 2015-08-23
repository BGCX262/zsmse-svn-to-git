using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

using Zoofware.ZZ80E;
using Zoofware.ZSMSE.Devices;

namespace Zoofware.ZSMSE
{
	public class TMS9918aBitmap : TMS9918a
	{
		/// <summary>
		/// Output bitmap.
		/// </summary>
		public Bitmap OutputSurface
		{
			get
			{
				return _OutputSurface;
			}
		}

		/// <summary>
		/// If V has folded back to an earlier value, then true.
		/// </summary>
		private bool HasVJumped;

		/// <summary>
		/// Internal output bitmap.
		/// </summary>
		private Bitmap _OutputSurface;

		/// <summary>
		/// Pointers used by the DrawSprite and DrawBackground methods
		/// respectively. They are seperate for clarity.
		/// </summary>
		unsafe private uint* spritePtr, backgroundPtr;

		/// <summary>
		/// Always points to the first byte of the output bitmap.
		/// </summary>
		unsafe private uint* basePtr;

		private BitmapData OutputData;
		
		/// <summary>
		/// Width and height of the output bitmap.
		/// </summary>
		private int Width, Height;

		/// <summary>
		/// Background colour of the output bitmap.
		/// This MUST be a colour where each channel is not a multiple of 85. (something which cannot be set by the VDP, which uses 2 bit colour channels)
		/// </summary>
		private uint BackgroundColour = 0xFF010101;

		public TMS9918aBitmap()
		{
			Width = 256;
			Height = 192;

			_OutputSurface = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
			
			OutputData = _OutputSurface.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, _OutputSurface.PixelFormat);

			unsafe
			{
				basePtr = spritePtr = backgroundPtr = (uint*)(void*)OutputData.Scan0;
			}
		}

		public override void Tick(int cycles, bool drawScanline = true)
		{
			/*
			 * Increment the H counter according to the number of cycles
			 */

			_HCounter += (uint)cycles;

			/*
			 * Move onto a new scanline?
			 * Each scanline is 342 pixels wide (of which 256 are active)
			 */

			if (_HCounter >= 342)
			{
				/*
				 * Wrap H counter back round
				 */

				_HCounter %= 342;

				_VCounter++;

				/*
				 * Because the V counter is a byte and the number of scanlines 
				 * is > 255; the counter wraps at a certain value.
				 * 
				 * This changes depending on the vdp mode.
				 * 
				 * For NTSC 256x192 this is 00-DA, D5-FF
				 */

				if (_VCounter == 0xDB && !HasVJumped)
				{
					_VCounter = 0xD5;
					HasVJumped = true;
				}

				/*
				 * Has the v counter wrapped back to 0 
				 */

				else if (_VCounter == 0)
				{
					HasVJumped = false;
				}
			}
			else
			{
				return;
			}

			/*
			 * If the current scanline is inside the active range
			 * then the internal interrupt counter needs to be decremented.
			 * 
			 * When this counter reaches -1, a line interrupt is triggered.
			 * 
			 * Sprites and background tiles also need to be drawn.
			 */

			if (_VCounter >= 0 && _VCounter < Height)
			{
				if (R1_D6_BLK && drawScanline)
				{
					/*
					 * Blank this line
					 */

					unsafe
					{
						backgroundPtr = basePtr + (_VCounter * Width);
						for (int i = 0; i < Width; i++)
						{
							*(backgroundPtr + i) = BackgroundColour;
						}
					}

					DrawSprites4();
					DrawBackground4();					
				}

				if (--LICounter == 0xFF && R0_D4_IE1)
				{					
					InterruptPending = true;
					LICounter = Registers[0xA];
				}
			}
			else
			{
				LICounter = Registers[0xA];
			}

			/*
			 * Depending on the height of the active area, the VDP
			 * will also generate a frame interrupt.
			 * 
			 * 192: C1
			 * 224: E1
			 * 240: F1
			 */

			if (_VCounter == 0xC1)
			{
				INT = InterruptPending = true;

				unsafe
				{
					spritePtr = backgroundPtr = basePtr;
				}
			}
		}

		private void DrawBackground4()
		{
			byte bitPlane0, bitPlane1, bitPlane2, bitPlane3;
			ushort data, n;
			bool priority, useSpritePalette, verticalFlip, horizontalFlip;
			int pixelRow, colour, palette, xPos;

			/*
			 * First, calculate the actual physical current row number.
			 */

			int row = (_VCounter / 8) + (R9_StartingRow);

			if ((_VCounter % 8) + R9_FineScroll > 7)
			{
				row++;
			}

			if (row >= 28)
			{
				row %= 28;
			}

			/*
			 * 32 columns in each row
			 */

			for (int column = 0; column < 32; column++)
			{
				/*
				 * Get the columns meta data
				 */

				data = VRAM[(row * 64) + (column * 2) + NameTableOffset];
				data |= (ushort)(VRAM[(row * 64) + (column * 2) + NameTableOffset + 1] << 8);

				priority = StaticHelpers.TestBit(data, 12);
				useSpritePalette = StaticHelpers.TestBit(data, 11);
				verticalFlip = StaticHelpers.TestBit(data, 10);
				horizontalFlip = StaticHelpers.TestBit(data, 9);
				n = (ushort)(data & 0x1FF);

				n *= 32;

				/*
				 * Now get the required row of the pattern
				 */

				pixelRow = _VCounter + Registers[9];
				
				pixelRow %= 8;

				if (verticalFlip)
				{
					pixelRow = -pixelRow;
					pixelRow += 7;
				}

				pixelRow <<= 2;

				bitPlane0 = VRAM[n + pixelRow];
				bitPlane1 = VRAM[n + pixelRow + 1];
				bitPlane2 = VRAM[n + pixelRow + 2];
				bitPlane3 = VRAM[n + pixelRow + 3];

				/*
				 * Set the starting x co-ordinate position of this column
				 */

				xPos = (column * 8) + Registers[8];

				/*
				 * Set the bitmap pointer to the current row
				 */

				unsafe
				{
					backgroundPtr = basePtr + (_VCounter * Width);
				}

				for (int pixel = 0; pixel < 8; pixel++, xPos++)
				{
					if (horizontalFlip)
					{
						palette = (((bitPlane3 & 1)) << 3);
						palette |= (((bitPlane2 & 1)) << 2);
						palette |= (((bitPlane1 & 1)) << 1);
						palette |= ((bitPlane0 & 1));

						bitPlane0 >>= 1;
						bitPlane1 >>= 1;
						bitPlane2 >>= 1;
						bitPlane3 >>= 1;
					}
					else
					{
						palette = (((bitPlane3 & 0x80) >> 7) << 3);
						palette |= (((bitPlane2 & 0x80) >> 7) << 2);
						palette |= (((bitPlane1 & 0x80) >> 7) << 1);
						palette |= ((bitPlane0 & 0x80) >> 7);

						bitPlane0 <<= 1;
						bitPlane1 <<= 1;
						bitPlane2 <<= 1;
						bitPlane3 <<= 1;
					}

					if (useSpritePalette)
					{
						palette += 16;
					}

					colour = CRAM[palette];

					/*
					 * Wrap the x pos back to 0 if its greater than the
					 * output width
					 */

					if (xPos >= Width)
					{
						xPos %= Width;
					}

					/*
					 * If the pixel falls in the first 8 pixels
					 * of the display and masking is set, then
					 * fill it with black
					 * 
					 * TODO: Fill mask with colour from R7
					 */

					if (xPos < 8 && R0_D5)
					{
						unsafe
						{
							*(backgroundPtr + xPos) = 0;
						}

						continue;
					}

					unsafe
					{						
						/*
						 * Check theres no sprite here already
						 */

						if(*(backgroundPtr + xPos) == BackgroundColour || (priority && palette != 0 && palette != 16))
						{
							*(backgroundPtr + xPos) = (uint)(((colour & 3) * 85) << 16);
							colour >>= 2;
							*(backgroundPtr + xPos) |= (uint)(((colour & 3) * 85) << 8);
							colour >>= 2;
							*(backgroundPtr + xPos) |= (uint)(((colour & 3) * 85) << 0);							
						}
					}
				}
			}
		}

		private void DrawSprites4()
		{
			int spriteCount = 0;
			int y, x, n, colour, palette;
			int bitPlane0, bitPlane1, bitPlane2, bitPlane3; 

			for (int s = 0; s < 62; s++)
			{
				if (spriteCount > 8)
				{
					OVR = true;
					break;
				}

				y = VRAM[SATOffset + s];

				if (y == 0xD0 && DisplayMode == DisplayModes.Mode_4)
				{
					return;
				}

				y++;

				if(_VCounter >= y && _VCounter < y + ((R1_D1) ? 16 : 8) && _VCounter < Height)
				{
					spriteCount++;

					x = VRAM[SATOffset + (s * 2) + 128];
					n = VRAM[SATOffset + (s * 2) + 129];

					if (R0_D3_EC)
					{
						x -= 8;						
					}
					
					unsafe
					{					
						spritePtr = basePtr + (_VCounter * Width) + x;					
					}

					if (StaticHelpers.TestBit(Registers[6], 2))
					{
						n = StaticHelpers.SetBit((ushort)n, 8, true);
					}

					n <<= 5; // n *= 32;
					n += (_VCounter - y) << 2; // * 4

					bitPlane0 = VRAM[n++];
					bitPlane1 = VRAM[n++];
					bitPlane2 = VRAM[n++];
					bitPlane3 = VRAM[n];

					/*
				 	 * Now draw each pixel
				 	 */

					for (int pixel = 0; pixel < 8; pixel++)
					{
						/*
						 * Dont draw the pixel if its off screen
						 */

						if (x + pixel >= Width || x + pixel < 0)
						{
							unsafe
							{
								spritePtr++;
							}

							continue;
						}

						/*
						 * Don;t draw the pixel if its in the first column
						 * and masking is enabled
						 */

						if (x + pixel < 8 && R0_D5)
						{
							unsafe
							{ 
								spritePtr++; 
							}

							continue;
						}
						
						palette = 0;

						palette |= (((bitPlane3 & 0x80) >> 7) << 3);
						palette |= (((bitPlane2 & 0x80) >> 7) << 2);
						palette |= (((bitPlane1 & 0x80) >> 7) << 1);
						palette |= ((bitPlane0 & 0x80) >> 7);

						bitPlane0 <<= 1;
						bitPlane1 <<= 1;
						bitPlane2 <<= 1;
						bitPlane3 <<= 1;											

						palette += 16;

						if (palette == 16)
						{
							unsafe
							{
								spritePtr++;
							}
							continue;
						}

						colour = CRAM[palette];

						unsafe
						{						
							/*
							 * Check for a collision
							 */

							if (*spritePtr != BackgroundColour)
							{
								COL = true;
							}
							
							*spritePtr = (uint)(((colour & 3) * 85) << 16);
							colour >>= 2;
							*spritePtr |= (uint)(((colour & 3) * 85) << 8);
							colour >>= 2;
							*spritePtr |= (uint)((colour & 3) * 85);
							spritePtr++;							
						}
					}
				}
			}
		}
	}
}
