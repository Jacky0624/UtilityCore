using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UtilityCore.Communication.Alarm;

namespace UtilityCore.Communication.Serial
{
	public class SerialRS232 : SerialPhysicalBase
	{
		private SerialPort _serialPort = new SerialPort();

		private byte[] _buffer = new byte[10000];

		public SerialRS232Setting Setting { get; set; }

		private bool _wantToConnect = false;

		private System.Timers.Timer _reconnectTimer = new System.Timers.Timer();

		private bool _reconnectTimerRunning = false;

		internal SerialRS232(SerialRS232Setting setting)
		{
			this.Setting = setting;

			this.Setting.PropertyChanged += Setting_PropertyChanged;
		}

		private void Setting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "AutoReconnect")
			{
				lock (_accessLock)
				{
					if (Setting.AutoReconnect)
					{
						if (!_reconnectTimerRunning)
						{
							StartReconnectTimer();
						}
					}
					else
					{
						StopReconnectTimer();
					}
				}
			}
		}

		private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			byte[] bytesCopy = null;
			int lengthThisTime = 0;
			SerialPort whichSerialPort = sender as SerialPort;
			if (_serialPort == whichSerialPort)
			{
				lengthThisTime = whichSerialPort.Read(_buffer, 0, _buffer.Length);

				bytesCopy = new byte[lengthThisTime];
				Array.Copy(_buffer, bytesCopy, lengthThisTime);

				OnReceiveBytesInternal(bytesCopy);
			}
		}

		protected override void Send(byte[] bytes)
		{
			_serialPort.Write(bytes, 0, bytes.Length);
		}

		public override void ConnectInternal()
		{
			try
			{
				_wantToConnect = true;

				_serialPort = new SerialPort();

				_serialPort.PortName = Setting.COM;
				_serialPort.BaudRate = Setting.BaudRate;
				_serialPort.Parity = Setting.Parity;
				_serialPort.DataBits = Setting.DataBits;
				_serialPort.StopBits = Setting.StopBits;
				_serialPort.ReadTimeout = 500;
				_serialPort.WriteTimeout = 500;

				_serialPort.DataReceived -= _serialPort_DataReceived;
				_serialPort.DataReceived += _serialPort_DataReceived;

				_serialPort.Open();
				StopReconnectTimer();
				base.OnConnected();
			}
			catch (Exception ex)
			{
				base.OnDisconnected();
				if (Setting.AutoReconnect)
				{
					StartReconnectTimer();
				}
				RaiseEventOnAlarm(new RS232ConnectFail(ex.Message));
			}
		}

		public override void DisconnectInternal()
		{
			try
			{
				_wantToConnect = false;

				_serialPort.DataReceived -= _serialPort_DataReceived;
				_serialPort.Close();
				_serialPort.Dispose();
				base.OnDisconnected();
			}
			catch (Exception ex)
			{
				base.OnDisconnected();
			}
		}

		private void StartReconnectTimer()
		{
			lock (_accessLock)
			{
				if (_reconnectTimer != null)
				{
					_reconnectTimer.Stop();
					_reconnectTimer.Dispose();
					_reconnectTimer = null;
				}

				_reconnectTimer = new System.Timers.Timer();
				_reconnectTimer.AutoReset = false;
				_reconnectTimer.Interval = Setting.AutoReconnectInterval;
				_reconnectTimer.Elapsed += (object sender, ElapsedEventArgs el) =>
				{
					if (_wantToConnect)
					{
						try
						{
							Connect();
						}
						catch (Exception)
						{
						}
					}
				};
				_reconnectTimer.Start();
				_reconnectTimerRunning = true;
			}
		}

		private void StopReconnectTimer()
		{
			lock (_accessLock)
			{
				if (_reconnectTimer != null)
				{
					_reconnectTimer.Stop();
					_reconnectTimer.Dispose();
					_reconnectTimer = null;
				}
				_reconnectTimerRunning = false;
			}
		}
	}
}
