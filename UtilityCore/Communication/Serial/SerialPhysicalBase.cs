using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Alarm;

namespace UtilityCore.Communication.Serial
{
	public abstract class SerialPhysicalBase
	{
		internal event Action<byte[]> OnSendBytes;
		internal event Action<string> OnReceiveString;
		internal event Action<byte[]> OnReceiveBytes;
		internal event Action<ConnectionStates> OnConnectionStateChanged;
		internal event Action<AlarmBase> OnAlarm;

		internal SerialHelper Helper { get; private set; }

		protected object _accessLock = new object();

		private ConnectionStates _ConnectionState = ConnectionStates.Disconnected;
		public ConnectionStates ConnectionState
		{
			get
			{
				return _ConnectionState;
			}
			set
			{
				if (_ConnectionState != value)
				{
					_ConnectionState = value;
					RaiseEventOnConnectionStateChanged(_ConnectionState);
				}
			}
		}

		public SerialPhysicalBase()
		{
			Helper = new SerialHelper(Send);

			Helper.OnSendBytes += RaiseEventOnSendBytes;
			Helper.OnReceiveBytes += RaiseEventOnReceiveBytes;
			Helper.OnReceiveString += RaiseEventOnReceiveString;

			Helper.OnAlarm += RaiseEventOnAlarm;
		}

		public void Connect()
		{
			lock (_accessLock)
			{
				if (ConnectionState == ConnectionStates.Disconnecting || ConnectionState == ConnectionStates.Disconnected)
				{
					ConnectionState = ConnectionStates.Connecting;
				}
			}
			ConnectInternal();
		}
		public void Disconnect()
		{
			lock (_accessLock)
			{
				if (ConnectionState == ConnectionStates.Connected || ConnectionState == ConnectionStates.Connecting)
				{
					ConnectionState = ConnectionStates.Disconnecting;
				}
			}
			DisconnectInternal();
		}

		public abstract void ConnectInternal();

		public abstract void DisconnectInternal();

		public void SendString(string message)
		{
			Helper.SendString(message);
		}

		public async Task<string> SendStringAsync(string message, int timeoutMs)
		{
			return await Helper.SendStringAsync(message, timeoutMs);
		}

		public async Task SendStringAsyncNoResponse(string message, int completeTimeMs)
		{
			await Helper.SendStringAsyncNoResponse(message, completeTimeMs);
		}

		public void SendBytes(byte[] bytes)
		{
			Helper.SendBytes(bytes);
		}

		public async Task<byte[]> SendBytesAsync(byte[] bytes, int timeoutMs)
		{
			return await Helper.SendBytesAsync(bytes, timeoutMs);
		}

		public async Task SendBytesAsyncNoResponse(byte[] bytes, int completeTimeMs)
		{
			await Helper.SendBytesAsyncNoResponse(bytes, completeTimeMs);
		}

		protected abstract void Send(byte[] bytes);

		protected void OnReceiveBytesInternal(byte[] bytes)
		{
			Helper.OnReceiveBytesInternal(bytes, bytes.Length);
		}

		private void RaiseEventOnSendBytes(byte[] bytes)
		{
			if (OnSendBytes != null)
			{
				OnSendBytes(bytes);
			}
		}

		private void RaiseEventOnReceiveBytes(byte[] bytes)
		{
			if (OnReceiveBytes != null)
			{
				OnReceiveBytes(bytes);
			}
		}

		private void RaiseEventOnReceiveString(string message)
		{
			if (OnReceiveString != null)
			{
				OnReceiveString(message);
			}
		}

		protected void OnConnected()
		{
			ConnectionState = ConnectionStates.Connected;
		}

		protected void OnDisconnected()
		{
			ConnectionState = ConnectionStates.Disconnected;
		}

		protected void RaiseEventOnConnectionStateChanged(ConnectionStates connected)
		{
			if (OnConnectionStateChanged != null)
			{
				OnConnectionStateChanged(connected);
			}
		}

		protected void RaiseEventOnAlarm(AlarmBase alarm)
		{
			if (OnAlarm != null)
			{
				OnAlarm(alarm);
			}
		}
	}
}
