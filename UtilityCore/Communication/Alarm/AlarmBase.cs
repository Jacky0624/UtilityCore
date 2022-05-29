using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Alarm
{
	public class AlarmBase
	{
		public DateTime Time { get; private set; }

		public string Message { get; set; }

		public AlarmBase InnerAlarm { get; private set; }

		public Exception Exception { get; private set; }

		public bool ContainMessage
		{
			get
			{
				return Message != null && Message.Length > 0;
			}
		}

		public AlarmBase(string message, Exception exception = null)
		{
			Time = DateTime.Now;
			Message = message;
			Exception = exception;
		}

		public AlarmBase(string message, AlarmBase innerAlarm)
		{
			StringBuilder sb = new StringBuilder();
			Time = DateTime.Now;
			InnerAlarm = innerAlarm;
			sb.AppendFormat("{0}, {1}", message, innerAlarm.Message.TrimEnd());

			//if (Exception != null)
			//{
			//	sb.AppendFormat(", {0}", innerAlarm.Exception.Message);
			//	if (Exception.InnerException != null)
			//	{
			//		sb.AppendFormat(", {0}", innerAlarm.Exception.InnerException.Message);
			//	}
			//}

			if (innerAlarm.Exception != null)
			{
				sb.AppendFormat(", {0}", innerAlarm.Exception.Message);
				if (innerAlarm.Exception.InnerException != null)
				{
					sb.AppendFormat(", {0}", innerAlarm.Exception.InnerException.Message);
				}
			}

			Message = sb.ToString();

			Exception = InnerAlarm.Exception;
		}
	}

	public abstract class Warning : AlarmBase
	{
		public Warning(string message) : base(message)
		{
		}
	}
}
