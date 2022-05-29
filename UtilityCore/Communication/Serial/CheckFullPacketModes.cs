using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
	public enum CheckFullPacketModes
	{
		[Description("EOF")]
		EOF,

		[Description("Timeout")]
		Timeout,

		[Description("自訂")]
		Custom,
		[Description("URC")]
		URC
	}
}
