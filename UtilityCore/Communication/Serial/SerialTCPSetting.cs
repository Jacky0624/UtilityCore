using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
	[Serializable]
	public class SerialTCPSetting : ViewModelBase
	{
		private string _Ip;
		public string Ip
		{
			get
			{
				return _Ip;
			}
			set
			{
				if (_Ip != value)
				{
					_Ip = value;
					OnPropertyChanged("Ip");
				}
			}
		}

		private int _Port;
		public int Port
		{
			get
			{
				return _Port;
			}
			set
			{
				if (_Port != value)
				{
					_Port = value;
					OnPropertyChanged("Port");
				}
			}
		}

		public void Initialize()
		{
			if (Ip == null)
			{
				Ip = "192.168.100.8";
			}

			if (Port <= 0)
			{
				Port = 9004;
			}
		}
	}
}
