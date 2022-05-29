using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Serial;
using UtilityCore.Socket.Common;

namespace UtilityCore.Socket.Server
{
	public class ClientInfo : SocketCustom
	{
		public event Action<object> OnReceiveCommand;
		public event Action<string> OnReceiveString;
		public event Action<byte[]> OnReceiveBytes;
		public event Action OnClientDisconnected;
		public event Action<System.Exception> OnError;
		internal event Action OnAccountInitializeFinished;

		private object _accessLock2 = new object();

		private SerialHelper Helper { get; set; }

		public string Ip
		{
			get;
			private set;
		}

		public string Account
		{
			get;
			private set;
		}

		private bool InitializeComplete
		{
			get
			{
				return Account != null;
			}
		}

		internal ClientInfo(
			System.Net.Sockets.Socket socket,
			int sendBufferSize,
			int receiveBufferSize,
			string ip,
			Action<ClientInfo> onConnectedCallback,
			Action<ClientInfo> onClientDisconnectedCallback,
			bool customMode)
			: base(socket, sendBufferSize, receiveBufferSize)
		{
			Ip = ip;
			ConnectedReal = true;

			this.CustomMode = customMode;

			Helper = new SerialHelper(SendBytes);

			Helper.Setting = new SerialSetting(); //因為不是Serial Device, 這邊new

			Helper.OnReceiveBytes += RaiseEventOnReceiveBytes;
			Helper.OnReceiveString += RaiseEventOnReceiveString;

			StartReadLoop();

			if (CustomMode)
			{
				Connected = true;
				if (onConnectedCallback != null)
				{
					onConnectedCallback(this);
				}
			}
			else
			{
				OnAccountInitializeFinished += () =>
				{
					Connected = true;
					if (onConnectedCallback != null)
					{
						onConnectedCallback(this);
					}
				};
			}

			OnClientDisconnected += () =>
			{
				if (onClientDisconnectedCallback != null)
				{
					onClientDisconnectedCallback(this);
				}
			};
		}

		public void SetEventHandler(Action<object> onReceiveCommand, Action onClientDisconnected, Action<System.Exception> onError)
		{
			OnReceiveCommand += onReceiveCommand;
			OnClientDisconnected += onClientDisconnected;
			OnError += onError;
		}

		public void Disconnect()
		{
			try
			{
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Close();
				OnDisconnectedProtected();
				ConnectedReal = false;
				Connected = false;
			}
			catch (System.Exception)
			{
			}
		}

		//--

		public Encoding Encoding
		{
			get
			{
				return Helper.Setting.Encoding;
			}
			set
			{
				Helper.Setting.Encoding = value;
			}
		}

		public byte[] SendStringEndOfLine
		{
			get
			{
				return Helper.Setting.SendStringEndOfLine;
			}
			set
			{
				Helper.Setting.SendStringEndOfLine = value;
			}
		}

		public byte[] ReceiveStringEndOfLine
		{
			get
			{
				return Helper.Setting.ReceiveStringEndOfLine;
			}
			set
			{
				Helper.Setting.ReceiveStringEndOfLine = value;
			}
		}

		public CheckFullPacketModes CheckFullPacketMode
		{
			get
			{
				return Helper.Setting.CheckFullPacketMode;
			}
			set
			{
				Helper.Setting.CheckFullPacketMode = value;
			}
		}

		public int TimeoutMs
		{
			get
			{
				return Helper.Setting.TimeoutMs;
			}
			set
			{
				Helper.Setting.TimeoutMs = value;
			}
		}

		public void SendString(string message)
		{
			Helper.SendString(message);
		}

		public async Task<string> SendStringAsync(string message, int timeoutMs = 2000)
		{
			return await Helper.SendStringAsync(message, timeoutMs);
		}

		public async Task SendStringAsyncNoResponse(string message, int completeTimeMs = 500)
		{
			await Helper.SendStringAsyncNoResponse(message, completeTimeMs);
		}

		//public void SendBytes(byte[] bytes)
		//{
		//	Helper.SendBytes(bytes);
		//}

		public async Task<byte[]> SendBytesAsync(byte[] bytes, int timeoutMs = 2000)
		{
			return await Helper.SendBytesAsync(bytes, timeoutMs);
		}

		public async Task SendBytesAsyncNoResponse(byte[] bytes, int completeTimeMs = 500)
		{
			await Helper.SendBytesAsyncNoResponse(bytes, completeTimeMs);
		}
		//--

		protected override void OnReceiveCommandProtected(object data)
		{
			if (CustomMode)
			{
				if (data is byte[])
				{
					byte[] bytes = (byte[])data;
					Helper.OnReceiveBytesInternal(bytes, bytes.Length);
				}
			}
			else
			{
				if (!InitializeComplete && data is LoginCommand)
				{
					Account = (data as LoginCommand).Account;
					Send(new LoginEvent());
					if (ConnectedReal)
					{
						if (OnAccountInitializeFinished != null)
						{
							OnAccountInitializeFinished();
						}
					}
				}
				else
				{
					if (ConnectedReal && InitializeComplete)
					{
						if (OnReceiveCommand != null)
						{
							OnReceiveCommand(data);
						}
					}
				}
			}
		}

		protected void RaiseEventOnReceiveBytes(byte[] bytes)
		{
			if (OnReceiveBytes != null)
			{
				OnReceiveBytes(bytes);
			}
		}

		protected void RaiseEventOnReceiveString(string message)
		{
			if (OnReceiveString != null)
			{
				OnReceiveString(message);
			}
		}

		protected override void OnDisconnectedProtected()
		{
			if (ConnectedReal && InitializeComplete)
			{
				ConnectedReal = false;
				Connected = false;

				if (OnClientDisconnected != null)
				{
					OnClientDisconnected();
				}
			}
		}

		protected override void OnErrorProtected(System.Exception ex)
		{
			if (ConnectedReal && InitializeComplete)
			{

				if (OnError != null)
				{
					OnError(ex);
				}
			}
		}
	}
}
