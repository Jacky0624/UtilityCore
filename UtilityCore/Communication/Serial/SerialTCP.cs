using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Alarm;
using UtilityCore.Socket.Client;

namespace UtilityCore.Communication.Serial
{
	public class SerialTCP : SerialPhysicalBase
	{
		private Client _client = null;

		public SerialTCPSetting Setting { get; set; }

		public string LocalIp
		{
			get
			{
				return _client.LocalIp;
			}
		}
		public int LocalPort
		{
			get
			{
				return _client.LocalPort;
			}
		}

		internal SerialTCP(SerialTCPSetting setting)
		{
			this.Setting = setting;

			_client = new Client();
			_client.OnReceiveBytes += OnReceivePacketBytes;

			_client.OnConnected += OnConnected;
			_client.OnDisconnectedCallback += OnDisconnected;
			_client.OnError += _client_OnError;

			_client.CustomMode = true;

			_client.ServerIp = Setting.Ip;
			_client.ServerPort = Setting.Port;
		}

		private void _client_OnError(Exception ex)
		{
			if (ex is Socket.Exception.ConnectException)
			{
				RaiseEventOnAlarm(new TCPConnectFail("Serial TCP Connect Fail", ex));
			}
		}

		private void OnReceivePacketBytes(byte[] bytes)
		{
			OnReceiveBytesInternal(bytes);
		}

		protected override void Send(byte[] bytes)
		{
			_client.SendBytes(bytes);
		}

		public override void ConnectInternal()
		{
			try
			{
				_client.ServerIp = Setting.Ip;
				_client.ServerPort = Setting.Port;
				_client.Connect();
			}
			catch (Exception ex)
			{
				RaiseEventOnAlarm(new TCPConnectFail("Serial TCP Connect Fail", ex));
			}
		}

		public override void DisconnectInternal()
		{
			_client.Disconnect();
		}
	}
}
