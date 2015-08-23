using System;
using System.Collections.Generic;
using System.Text;

namespace Zoofware.ZZ80E
{
	public class Z80UnknownOpcode : Exception
	{
		public ushort Opcode;

		public Z80UnknownOpcode(ushort opcode)
		{
			this.Opcode = opcode;
		}
	}

	public class Z80InvalidIOPort : Exception
	{
		public byte Port { get; set; }

		public override string Message
		{
			get
			{
				return string.Format("Port {0:X2} invalid", Port);
			}
		}

		public Z80InvalidIOPort(byte port)
		{
			this.Port = port;
		}
	}
}
