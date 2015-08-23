using System;
using System.Collections.Generic;

namespace Zoofware.ZZ80E
{
	public partial class CPU
	{
		public delegate byte ReadIOPortDelegate(byte highAddr);
		public delegate void WriteIOPortDelegate(byte highAddr, byte value);

		private Dictionary<byte, ReadIOPortDelegate> InPorts = new Dictionary<byte,ReadIOPortDelegate>();
		private Dictionary<byte, WriteIOPortDelegate> OutPorts = new Dictionary<byte,WriteIOPortDelegate>();

		private byte ReadIOPort(byte highAddr, byte lowAddr)
		{
			if (!InPorts.ContainsKey(lowAddr))
			{
				throw new Z80InvalidIOPort(lowAddr);
			}

			return InPorts[lowAddr].Invoke(highAddr);
		}

		private void WriteIOPort(byte highAddr, byte lowAddr, byte value)
		{
			if (!OutPorts.ContainsKey(lowAddr))
			{
				throw new Z80InvalidIOPort(lowAddr);
			}

			OutPorts[lowAddr].Invoke(highAddr, value);
		}

		public void ConnectIOPort(byte lowAddr, ReadIOPortDelegate readCallback, WriteIOPortDelegate writeCallback)
		{
			InPorts[lowAddr] = readCallback;
			OutPorts[lowAddr] = writeCallback;
		}
	}
}
