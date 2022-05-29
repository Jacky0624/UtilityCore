using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Socket.Exception
{
	public class ConnectException : System.Exception
	{
		public ConnectException(string message, System.Exception innerException) : base(message, innerException)
		{
		}
	}
}
