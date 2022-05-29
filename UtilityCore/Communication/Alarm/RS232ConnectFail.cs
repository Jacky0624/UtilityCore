using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Alarm
{
	public class RS232ConnectFail : AlarmBase
	{
		public RS232ConnectFail(string message, Exception exception = null) : base(message, exception)
		{
		}
	}
}
