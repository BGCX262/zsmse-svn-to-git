using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Thread = System.Threading.Thread;
using System.Timers;
using Zoofware.ZZ80E;
using System.ComponentModel;
using Zoofware.ZSMSE;
using Zoofware.ZSMSE.Devices;

namespace Zoofware.ZSMSE
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Timer UIUpdateTimer;
		private BackgroundWorker CPUWorker;
	
		private CPU CPU;
		private MemoryMapper Memory;
		private TMS9918aBitmap VDP;
		private Input Input;

		private double CPUFrequency;
		private double TargetCPUFrequency = DefaultFrequency;

		private DateTime CPUStartTime;
		private TimeSpan TotalTime;

		private int VDPRefreshInterval = DefaultVDPRefresh;
		private int VDPTickInterval = 1;
		private const int DefaultVDPRefresh = 59167;
		private const double DefaultFrequency = 3550000;

		private System.IO.FileStream DebugOut;

		public MainWindow()
		{
			InitializeComponent();

			UIUpdateTimer = new Timer(1000);
			UIUpdateTimer.Elapsed += new ElapsedEventHandler(UIUpdateTimer_Elapsed);
						
			CPUWorker = new BackgroundWorker()
			{
				WorkerSupportsCancellation = true,
				WorkerReportsProgress = true,
			};

			CPUWorker.DoWork += new DoWorkEventHandler(CPUWorker_DoWork);
			CPUWorker.ProgressChanged += new ProgressChangedEventHandler(CPUWorker_ProgressChanged);

			Init();
		}

		void CPUWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			/*
			 * Refresh the frequency.
			 */

			TotalTime = DateTime.Now - CPUStartTime;
			CPUFrequency = CPU.TStates / TotalTime.TotalSeconds;
		}

		public void Init()
		{
			Memory = new MemoryMapper();
			
			CPU = new CPU(Memory)
			{
				DebugMode = (bool) IsDebugModeEnabled.IsChecked,
				StackPointer = 0xDFF8,
			};

			VDP = new TMS9918aBitmap();
			Input = new Input();

			/*
			 * Add CPU ports
			 */

			/*
			 * Range 0x00 to 0x3F
			 * 
			 * Even writes: memory control register
			 * Odd writes: i/o control register
			 * Reads: return the last byte of the instruction which read the port (??)
			 */

			for (byte i = 0; i <= 0x3F; i++)
			{
				if (i % 2 == 0)
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(delegate { return 0; }),
						new CPU.WriteIOPortDelegate(delegate { }));
				}
				else
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(delegate { return 0; }),
						new CPU.WriteIOPortDelegate(delegate { }));
				}
			}

			/*
			 * Range 0x80 to 0xBF
			 * 
			 * Even: vdp data port
			 * Odd: vdp ctrl port
			 */

			for (byte i = 0x80; i <= 0xBF; i++)
			{
				if (i % 2 == 0)
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(VDP.ReadDataPort),
						new CPU.WriteIOPortDelegate(VDP.WriteToDataPort));
				}
				else
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(VDP.ReadControlPort),
						new CPU.WriteIOPortDelegate(VDP.WriteToControlPort));
				}
			}

			/*
			 * 0xC0 -> 0xFF
			 * Writes have no effect
			 * 
			 * Even reads return controller port A/B register
			 * Odd reads return controller port B/misc register
			 */

			for (byte i = 0xC0; i < 0xFF; i++)
			{
				if (i % 2 == 0)
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(delegate { return Input.RegisterAB; }),
						new CPU.WriteIOPortDelegate(delegate { return; }));
				}
				else
				{
					CPU.ConnectIOPort(i,
						new CPU.ReadIOPortDelegate(delegate { return Input.RegisterBMisc; }),
						new CPU.WriteIOPortDelegate(delegate { return; }));
				}
			}

			CPU.ConnectIOPort(0x7E,
				new CPU.ReadIOPortDelegate(delegate { return VDP.VCounter; }),
				new CPU.WriteIOPortDelegate(delegate {  }));

			CPU.ConnectIOPort(0x7F,
				new CPU.ReadIOPortDelegate(delegate { return VDP.HCounter; }),
				new CPU.WriteIOPortDelegate(delegate {  }));

			UIUpdateTimer.Start();
		}

		#region Events
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.Pause: CPU.NMIPending = true; break;
				case Key.A: Input.P1FireA = true; break;
				case Key.S: Input.P1FireB = true; break;
				case Key.Left: Input.P1Left = true; break;
				case Key.Right: Input.P1Right = true; break;
				case Key.Up: Input.P1Up = true; break;
				case Key.Down: Input.P1Down = true; break;
				case Key.Escape: Input.Reset = true; break;
			}
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.A		: Input.P1FireA = false; break;
				case Key.S		: Input.P1FireB = false; break;
				case Key.Left	: Input.P1Left = false; break;
				case Key.Right	: Input.P1Right = false; break;
				case Key.Up		: Input.P1Up = false; break;
				case Key.Down	: Input.P1Down = false; break;
			}
		}

		private void ShowFileDialog(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
			{
				CheckFileExists = true,
				CheckPathExists = true,
				Multiselect = false,
				Title = "Select SMS ROM",
			};

			ofd.InitialDirectory = Environment.CurrentDirectory + "\\Test ROMs";
			ofd.ShowDialog();

			if (ofd.FileName.Length > 0)
			{
				System.IO.BinaryReader br = new System.IO.BinaryReader(ofd.OpenFile());
				byte[] data = br.ReadBytes((int)ofd.OpenFile().Length);

				StopCPU();

				Memory.Load(data);
			}
		}

		private void UIUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
			{
				CPUFreqLabel.Content = CPUFrequency / 1000000;
				FPSLabel.Content = CPUFrequency / DefaultVDPRefresh;
				return null;
			}), null);

			/*
			 * Reset the total time every 10 seconds
			 */

			if ((DateTime.Now - CPUStartTime).TotalSeconds > 10)
			{
				CPUStartTime = DateTime.Now;
				CPU.TStates = 0;
			}
		}

		private void StartCPU(object sender, RoutedEventArgs e)
		{			
			if (!Memory.CartLoaded)
			{
				MessageBox.Show(this, "Before starting the CPU, please make sure a valid Z80/SMS ROM has been loaded", "ZSMSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			try
			{
				CPUWorker.RunWorkerAsync();
			}
			catch (InvalidOperationException)
			{
				MessageBox.Show(this, "The CPU is already running.", "ZSMSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			
		}

		private void StopCPU(object sender, RoutedEventArgs e)
		{
			StopCPU();
		}

		private void ToggleDebugMode(object sender, RoutedEventArgs e)
		{
			CPUStartTime = DateTime.Now;
			CPU.TStates = 0;

			if ((bool)IsDebugModeEnabled.IsChecked)
			{
				IsDebugModeToFileEnabled.IsEnabled = false;

				if((bool)IsDebugModeToFileEnabled.IsChecked)
				{
					DebugOut = new System.IO.FileStream(Environment.CurrentDirectory + "\\debugout.txt", System.IO.FileMode.Create);
					Console.SetOut(new System.IO.StreamWriter(DebugOut));
				}
				else
				{
					AllocConsole();
				}

				CPU.DebugMode = true;
			}
			else
			{
				CPU.DebugMode = false;

				/*
				 * Pause the thread just incase the CPU is mid cycle.
				 * The change to the debugmode needs to propagate before
				 * the console is freed.
				 */

				Thread.Sleep(100);

				IsDebugModeToFileEnabled.IsEnabled = true;

				if ((bool)IsDebugModeToFileEnabled.IsChecked)
				{
					DebugOut.Close();
				}
				else
				{
					FreeConsole();
				}
			}
		}

		private void EmulationSpeedHandler(object sender, RoutedEventArgs e)
		{
			if ((bool)EmulationSpeed_default.IsChecked)
			{
				TargetCPUFrequency = DefaultFrequency;
			}
			else if ((bool)EmulationSpeed_half.IsChecked)
			{
				TargetCPUFrequency = DefaultFrequency / 2;
			}
			else
			{
				TargetCPUFrequency = double.PositiveInfinity;
			}

			CPUStartTime = DateTime.Now;
			CPU.TStates = 0;
		}

		private void FrameSkipHandler(object sender, RoutedEventArgs e)
		{
			/*
			* Currently only emulating NTSC
			* 
			* 60FPS on a 3.55MHz CPU = 59167 TStates per frame
			*/

			if ((bool) FrameSkip_off.IsChecked)
			{
				VDPTickInterval = 1;
				VDPRefreshInterval = DefaultVDPRefresh;
			}
			else if ((bool) FrameSkip_x2.IsChecked)
			{
				VDPTickInterval = 2;
				VDPRefreshInterval = DefaultVDPRefresh * 2;
			}
			else if ((bool) FrameSkip_x3.IsChecked)
			{
				VDPTickInterval = 3;
				VDPRefreshInterval = DefaultVDPRefresh * 3;
			}
			else if ((bool)FrameSkip_x10.IsChecked)
			{
				VDPTickInterval = 10;
				VDPRefreshInterval = DefaultVDPRefresh * 10;
			}
			else if ((bool)FrameSkip_x100.IsChecked)
			{
				VDPTickInterval = 100;
				VDPRefreshInterval = DefaultVDPRefresh * 100;
			}
		}

		private void CPUWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			StartCPU();
		}

		private void SaveState(object sender, RoutedEventArgs e)
		{

		}
		#endregion

		private void StartCPU()
		{
			uint TStatesSinceLastRefresh = 0;
			uint InterationsSinceVDPTick = 0;
			uint TStates;

			CPUFrequency = 0;

			CPUStartTime = DateTime.Now;

			try
			{
				while (true)
				{
					/*
					 * Limit the CPU emulation speed 
					 */

					if (CPUFrequency > TargetCPUFrequency)
					{
						TotalTime = DateTime.Now - CPUStartTime;
						CPUFrequency = CPU.TStates / TotalTime.TotalSeconds;

						continue;
					}

					/*
					 * Request any interrupts...
					 */

					CPU.INTPending = VDP.InterruptPending;
					VDP.InterruptPending = false;
					TStates = CPU.Step();

					/*
					 * The VDP is 1.5x faster than the CPU
					 */

					InterationsSinceVDPTick++;

					if (InterationsSinceVDPTick >= VDPTickInterval)
					{
						InterationsSinceVDPTick = 0;
						VDP.Tick((int)(TStates * 1.5), true);
					}
					else
					{
						VDP.Tick((int)(TStates * 1.5), false);
					}

					TStatesSinceLastRefresh += TStates;

					if ((TStatesSinceLastRefresh >= VDPRefreshInterval))
					{
						Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
						{
							IntPtr hbitmap = VDP.OutputSurface.GetHbitmap();
							VDPHost.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, null);
							DeleteObject(hbitmap);
							
							return null;
						}), null);

						TStatesSinceLastRefresh = 0;
						
						CPUWorker.ReportProgress(0);
					}

					if (CPUWorker.CancellationPending)
					{
						return;
					}
				}
			}
			catch(Z80UnknownOpcode e)
			{
				Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new DispatcherOperationCallback(delegate
				{
					MessageBox.Show(this, "Unknown opcode " + e.Opcode.ToString("X4") + " @ " + CPU.ProgramCounter.ToString("X4") + ". Execution aborted.", "ZSMSE Error", MessageBoxButton.OK, MessageBoxImage.Error);
					Init();
					return null;
				}), null);

				return;
			}
		}

		private void StopCPU()
		{
			CPUWorker.CancelAsync();

			Init();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			UIUpdateTimer.Stop();
		}

		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		static extern bool DeleteObject(IntPtr hObject);

		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AllocConsole();

		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		static extern bool FreeConsole();
	}
}
