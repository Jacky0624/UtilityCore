using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
	[Serializable]
	public class SerialSetting : ViewModelBase
	{
		private SerialModes _mode;
		public SerialModes Mode
		{
			get
			{
				return _mode;
			}
			set
			{
				if (_mode != value)
				{
					_mode = value;
					OnPropertyChanged("Mode");
				}
			}
		}

		//private byte[] _SendStringEndOfLine = Encoding.UTF8.GetBytes("\r");
		private byte[] _SendStringEndOfLine = new byte[0];
		public byte[] SendStringEndOfLine
		{
			get
			{
				return _SendStringEndOfLine;
			}
			set
			{
				if (_SendStringEndOfLine != value)
				{
					_SendStringEndOfLine = value;

					if (_SendStringEndOfLine == null)
					{
						_SendStringEndOfLine = new byte[0];
					}

					//if (_helper != null)
					//{
					//	_helper.SendStringEndOfLine = _SendStringEndOfLine;
					//}
				}
			}
		}

		private byte[] _ReceiveStringEndOfLine = Encoding.UTF8.GetBytes("\r\n");
		public byte[] ReceiveStringEndOfLine
		{
			get
			{
				return _ReceiveStringEndOfLine;
			}
			set
			{
				if (_ReceiveStringEndOfLine != value)
				{
					_ReceiveStringEndOfLine = value;

					if (_ReceiveStringEndOfLine == null)
					{
						_ReceiveStringEndOfLine = new byte[0];
					}

					//if (_helper != null)
					//{
					//	_helper.ReceiveStringEndOfLine = _ReceiveStringEndOfLine;
					//}

				}
			}
		}

		private Encoding _Encoding = Encoding.UTF8;
		[JsonIgnore]
		public Encoding Encoding
		{
			get
			{
				return _Encoding;
			}
			set
			{
				if (_Encoding != value)
				{
					_Encoding = value;

					OnPropertyChanged("Encoding");
				}
			}
		}

		private CheckFullPacketModes _CheckFullPacketMode = CheckFullPacketModes.EOF;
		public CheckFullPacketModes CheckFullPacketMode
		{
			get
			{
				return _CheckFullPacketMode;
			}
			set
			{
				if (_CheckFullPacketMode != value)
				{
					_CheckFullPacketMode = value;

					//if (_helper != null)
					//{
					//	_helper.CheckFullPacketMode = _CheckFullPacketMode;
					//}

					OnPropertyChanged("CheckFullPacketMode");
				}
			}
		}

		private int _TimeoutMs;
		public int TimeoutMs
		{
			get
			{
				return _TimeoutMs;
			}
			set
			{
				if (_TimeoutMs != value)
				{
					_TimeoutMs = value;
					OnPropertyChanged("TimeoutMs");
				}
			}
		}

		public SerialTCPSetting TCPSetting { get; set; }
		public SerialRS232Setting RS232Setting { get; set; }
		public SerialHIDUSBSetting HIDSetting { get; set; }
		private ErrorHandlingMethods _ErrorHandlingMethod = ErrorHandlingMethods.RaiseOnAlarmEvent;
		public ErrorHandlingMethods ErrorHandlingMethod
		{
			get
			{
				return _ErrorHandlingMethod;
			}
			set
			{
				if (_ErrorHandlingMethod != value)
				{
					_ErrorHandlingMethod = value;
					OnPropertyChanged("ErrorHandlingMethod");
				}
			}
		}


		//UI Setting
		public bool UseAngleBracket { get; set; }

		public SerialSetting()
		{
			Mode = SerialModes.TCP;

			Initialize();
		}


		public void Initialize()
		{
			if (SendStringEndOfLine == null)
			{
				SendStringEndOfLine = Encoding.UTF8.GetBytes("\r");
			}

			if (ReceiveStringEndOfLine == null)
			{
				ReceiveStringEndOfLine = Encoding.UTF8.GetBytes("\r");
			}

			if (Encoding == null)
			{
				Encoding = Encoding.UTF8;
			}

			if (TimeoutMs <= 0)
			{
				TimeoutMs = 300;
			}

			if (TCPSetting == null)
			{
				TCPSetting = new SerialTCPSetting();
			}
			TCPSetting.Initialize();

			if (RS232Setting == null)
			{
				RS232Setting = new SerialRS232Setting();
			}
			RS232Setting.Initialize();
			if (HIDSetting == null)
			{
				HIDSetting = new SerialHIDUSBSetting();
			}
			HIDSetting.Initialize();
		}
	}
}
