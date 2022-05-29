using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
	[Serializable]
	public class SerialRS232Setting : ViewModelBase
	{
		public string COM { get; set; }
		public int BaudRate { get; set; }

		private System.IO.Ports.Parity _parity = System.IO.Ports.Parity.None;
		public System.IO.Ports.Parity Parity
		{
			get
			{
				return _parity;
			}
			set
			{
				_parity = value;
			}
		}

		public int DataBits { get; set; }

		private System.IO.Ports.StopBits _stopBits = System.IO.Ports.StopBits.One;
		public System.IO.Ports.StopBits StopBits
		{
			get
			{
				return _stopBits;
			}
			set
			{
				_stopBits = value;
			}
		}

		private int _AutoReconnectInterval = 10000;
		public int AutoReconnectInterval
		{
			get
			{
				return _AutoReconnectInterval;
			}
			set
			{
				if (_AutoReconnectInterval != value)
				{
					if (value <= 0)
					{
						_AutoReconnectInterval = 10000;
					}
					else
					{
						_AutoReconnectInterval = value;

					}
					OnPropertyChanged("AutoReconnectInterval");
				}
			}
		}

		private bool _AutoReconnect = false;
		public bool AutoReconnect
		{
			get
			{
				return _AutoReconnect;
			}
			set
			{
				if (_AutoReconnect != value)
				{
					_AutoReconnect = value;
					OnPropertyChanged("AutoReconnect");
				}
			}
		}

		public void Initialize()
		{
			if (COM == null || COM == "")
			{
				COM = "COM1";
			}

			if (BaudRate <= 0)
			{
				BaudRate = 115200;
			}


			if (DataBits <= 0)
			{
				DataBits = 8;
			}

			if (AutoReconnectInterval <= 0)
			{
				AutoReconnectInterval = 3000;
			}

			//Parity = System.IO.Ports.Parity.None; //解決舊版沒有設定到的問題用

			//StopBits = System.IO.Ports.StopBits.One;
		}
	}
}
