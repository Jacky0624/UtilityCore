using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilityCore.Socket.Server
{
	public class Server
	{
		private class AsyncAcceptData
		{
			public System.Net.Sockets.Socket WorkSocket = null;
			public ManualResetEvent Done = new ManualResetEvent(false);
		}

		public event Action<ClientInfo> OnClientConnected;
		public event Action<ClientInfo> OnClientDisconnected;
		public event Action<System.Exception> OnError;

		private System.Net.Sockets.Socket _listener = null;
		private CancellationTokenSource _listenerCancel = null;
		private List<ClientInfo> _clients = new List<ClientInfo>();
		public List<ClientInfo> Clients
		{
			get
			{
				return new List<ClientInfo>(_clients);
			}
		}

		private object _accessLock = new object();

		public string Ip { get; set; }
		public int Port { get; set; }
		public int SendBufferSize { get; set; }
		public int ReceiveBufferSize { get; set; }
		public int StartServerRetryIntervalMs { get; set; }

		public bool CustomMode { get; set; }

		public bool Running
		{
			get
			{
				return !_listenerCancel.IsCancellationRequested;
			}
		}

		public Server()
		{
			Ip = "127.0.0.1";
			Port = 11000;
			SendBufferSize = 100000;
			ReceiveBufferSize = 100000;
			StartServerRetryIntervalMs = 10000;

			_listenerCancel = new CancellationTokenSource();
			_listenerCancel.Cancel(); // make Running == false
		}

		public void Start()
		{
			lock (_accessLock)
			{
				if (!Running)
				{
					_listenerCancel = new CancellationTokenSource();
					Task.Factory.StartNew(() =>
					{
						while (!_listenerCancel.IsCancellationRequested)
						{
							IPAddress ipAddress = IPAddress.Parse(Ip);
							IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);
							_listener = new System.Net.Sockets.Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
							_listener.Bind(localEndPoint);
							_listener.Listen(100);

							while (!_listenerCancel.IsCancellationRequested)
							{
								AsyncAcceptData acceptData = new AsyncAcceptData();
								acceptData.WorkSocket = _listener;

								try
								{
									_listener.BeginAccept(new AsyncCallback(AcceptCallback), acceptData);
									acceptData.Done.WaitOne();
									acceptData.Done.Reset();
								}
								catch (System.Exception ex) //??
								{
									OnErrorHandle(ex);
									break;
								}
							}

							Thread.Sleep(StartServerRetryIntervalMs);
						}
					}, TaskCreationOptions.LongRunning);
				}
			}
		}

		public void Stop()
		{
			lock (_accessLock)
			{
				if (Running)
				{
					_listenerCancel.Cancel();
					try
					{
						_listener.Close();
						_listener = null;

						List<ClientInfo> clientsCopy = new List<ClientInfo>(_clients);
						Task.Factory.StartNew(new Action(() =>
						{
							foreach (ClientInfo client in clientsCopy)
							{
								client.Disconnect();
							}
						}));
						_clients.Clear();
					}
					catch (System.Exception ex)
					{
						if (OnError != null)
						{
							OnError(ex);
						}
					}
				}
			}
		}

		private void AcceptCallback(IAsyncResult asyncResult)
		{
			AsyncAcceptData acceptData = (AsyncAcceptData)asyncResult.AsyncState;

			try
			{
				System.Net.Sockets.Socket handler = acceptData.WorkSocket.EndAccept(asyncResult);
				handler.NoDelay = true;
				lock (_accessLock)
				{
					if (Running)
					{
						IPEndPoint ipEdPoint = handler.RemoteEndPoint as IPEndPoint;
						ClientInfo clientInfo = new ClientInfo(
							handler,
							SendBufferSize,
							ReceiveBufferSize,
							ipEdPoint.Address.ToString(),
							OnClientConnected,
							OnClientDisconnected,
							CustomMode);

						_clients.Add(clientInfo);
					}
					else
					{
						handler.Shutdown(SocketShutdown.Both);
						handler.Close();
					}
				}
				acceptData.Done.Set();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (System.Exception ex) //??
			{
				OnErrorHandle(ex);
			}
		}

		private void OnErrorHandle(System.Exception ex)
		{
			Stop();
			Task.Factory.StartNew(() =>
			{
				if (OnError != null)
				{
					OnError(ex);
				}
			});
		}
	}
}
