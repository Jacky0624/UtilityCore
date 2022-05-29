using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Alarm
{
	public class RS232ReceiveFail : AlarmBase
	{
		public RS232ReceiveFail(string message, Exception exception = null) : base(message, exception)
		{
		}
	}
}
