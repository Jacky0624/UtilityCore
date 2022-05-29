using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UtilityCore.Socket.Common;
using UtilityCore.Socket.Exception;

namespace UtilityCore.Socket.Client
{
	public class Client : SocketCustom
	{
		public event Action OnConnected;
		public event Action OnDisconnectedCallback;
		public event Action<object> OnReceiveEvent;
		public event Action<byte[]> OnReceiveBytes;
		public event Action<System.Exception> OnError;

		private bool _wantToConnect = false;

		private bool _connecting = false;

		private bool _didCallDisconnectedCallback = false;

		private object _accessLock2 = new object();

		private System.Timers.Timer _reconnectTimer = new System.Timers.Timer();

		public string ServerIp { get; set; }
		public int ServerPort { get; set; }
		public string Account { get; set; }

		public string LocalIp
		{
			get
			{
				try
				{
					if (ConnectedReal)
					{
						return ((IPEndPoint)_socket.LocalEndPoint).Address.ToString();
					}
				}
				catch (System.Exception) { }
				return null;
			}
		}
		public int LocalPort
		{
			get
			{
				try
				{
					if (ConnectedReal)
					{
						return ((IPEndPoint)_socket.LocalEndPoint).Port;
					}
				}
				catch (System.Exception) { }
				return 0;
			}
		}

		private int _autoReconnectInterval = 3000;
		public int AutoReconnectInterval
		{
			get
			{
				return _autoReconnectInterval;
			}
			set
			{
				if (value <= 0)
				{
					_autoReconnectInterval = 1;
				}
				else
				{
					_autoReconnectInterval = value;
				}
			}
		}

		public Client()
		{
			Initialize("Default");
		}

		public Client(string account)
		{
			Initialize(account);
		}

		public void Connect()
		{
			lock (_accessLock2)
			{
				_wantToConnect = true;

				_didCallDisconnectedCallback = false;

				if (_reconnectTimer != null)
				{
					_reconnectTimer.Stop();
					_reconnectTimer.Dispose();
					_reconnectTimer = null;
				}

				if (!ConnectedReal && !_connecting)
				{
					_connecting = true;
					IPAddress ipAddress = null;
					IPEndPoint remoteEP = null;

					try
					{
						ipAddress = IPAddress.Parse(ServerIp);
					}
					catch (System.Exception)
					{
						throw new System.Exception(string.Format("Invalid Ip: {0}", ServerIp));
					}

					try
					{
						remoteEP = new IPEndPoint(ipAddress, ServerPort);
					}
					catch (System.Exception)
					{
						throw new System.Exception(string.Format("Invalid Port: {0}", ServerPort));
					}

					if (ipAddress != null && remoteEP != null)
					{
						_socket = new System.Net.Sockets.Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						_socket.NoDelay = true;

						_socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), _socket);
					}
					else
					{
						_connecting = false;
						Disconnect();
					}
				}
			}
		}

		public void Disconnect()
		{
			lock (_accessLock2)
			{
				_wantToConnect = false;

				_connecting = false;

				StopReconnectTimer();

				{
					ConnectedReal = false;
					Connected = false;
					try
					{
						_socket.Shutdown(SocketShutdown.Both);
						_socket.Close();
					}
					catch (System.Exception) { }

					CallOnDisconnected();
				}
			}
		}

		public override void Send(object data)
		{
			if (Connected)
			{
				base.Send(data);
			}
			else
			{
				throw new InvalidOperationException("Client不可以在還沒連線前就呼叫Send");
			}
		}

		public override void SendBytes(byte[] bytes)
		{
			if (Connected)
			{
				base.SendBytes(bytes);
			}
			else
			{
				throw new InvalidOperationException("Client不可以在還沒連線前就呼叫SendBytes");
			}
		}

		private void SendPrivate(object data)
		{
			base.Send(data);
		}

		protected override void OnDisconnectedProtected()
		{
			StartReconnectTimer();

			CallOnDisconnected();
		}

		private void CallOnDisconnected()
		{

			lock (_accessLock2)
			{
				if (OnDisconnectedCallback != null && !_didCallDisconnectedCallback)
				{
					_didCallDisconnectedCallback = true;
					Task.Factory.StartNew(() =>
					{
						OnDisconnectedCallback();
					}, TaskCreationOptions.LongRunning);

				}
			}

		}

		protected override void OnReceiveCommandProtected(object data)
		{
			if (data is LoginEvent)
			{
				CallOnConnectedCallback();
			}
			else
			{
				Task.Factory.StartNew(() =>
				{
					if (CustomMode)
					{
						if (OnReceiveBytes != null)
						{
							OnReceiveBytes((byte[])data);
						}
					}
					else
					{
						if (OnReceiveEvent != null)
						{
							OnReceiveEvent(data);
						}
					}
				}, TaskCreationOptions.LongRunning);

			}
		}

		private void CallOnConnectedCallback()
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					if (OnConnected != null)
					{
						OnConnected();
					}
				}
				catch (System.Exception)
				{
				}

			}, TaskCreationOptions.LongRunning);

		}

		protected override void OnErrorProtected(System.Exception ex)
		{
			lock (_accessLock2)
			{
				if (!_didCallDisconnectedCallback && OnError != null)
				{
					Task.Factory.StartNew(() =>
					{
						OnError(ex);
					}, TaskCreationOptions.LongRunning);

				}
			}
		}

		private void ConnectCallback(IAsyncResult asyncResult)
		{
			try
			{
				lock (_accessLock2)
				{
					StopReconnectTimer();

					_connecting = false;

					System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)asyncResult.AsyncState;

					socket.EndConnect(asyncResult);

					ConnectedReal = true;

					if (CustomMode)
					{
						Connected = true;
					}

					StartReadLoop();

					if (CustomMode)
					{
						Task.Factory.StartNew(() =>
						{
							CallOnConnectedCallback();
						}, TaskCreationOptions.LongRunning);

					}
					else
					{
						SendPrivate(new LoginCommand(Account));
					}
				}
			}
			catch (System.Exception ex)
			{
				OnDisconnectedCallback?.Invoke();
				Task.Factory.StartNew(() =>
				{
					if (OnError != null)
					{
						OnError(new ConnectException("Client Connect Fail", ex));
					}
				}, TaskCreationOptions.LongRunning);

				StartReconnectTimer();
			}
		}

		private void Initialize(string account)
		{
			ServerIp = "127.0.0.1";
			ServerPort = 11000;
			Account = account;
		}

		private void StartReconnectTimer()
		{
			lock (_accessLock2)
			{
				_reconnectTimer = new System.Timers.Timer();
				_reconnectTimer.AutoReset = false;
				_reconnectTimer.Interval = AutoReconnectInterval;
				_reconnectTimer.Elapsed += (object sender, ElapsedEventArgs el) =>
				{
					lock (_accessLock2)
					{
						if (_wantToConnect)
						{
							Connect();
						}
					}
				};
				_reconnectTimer.Start();
			}
		}

		private void StopReconnectTimer()
		{
			if (_reconnectTimer != null)
			{
				_reconnectTimer.Stop();
				_reconnectTimer.Dispose();
				_reconnectTimer = null;
			}
		}
	}
}
