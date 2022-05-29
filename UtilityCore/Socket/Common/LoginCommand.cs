using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Socket.Common
{
	[Serializable]
	public class LoginCommand
	{
		public string Account;
		public LoginCommand(string account)
		{
			Account = account;
		}
	}
}
