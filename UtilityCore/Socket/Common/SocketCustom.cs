using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Socket.Common
{
	public abstract class SocketCustom
	{
		protected System.Net.Sockets.Socket _socket = null;

		protected object _accessLock = new object();

		private List<byte[]> _sendQueue = new List<byte[]>();
		private bool _currentSendFinished = true;

		private const int _defaultWriteBufferSize = 100000;
		private byte[] _sendBuffer = null;
		private int _sendTotalSize = 0;
		private int _sendIndexNow = 0;

		private const int _defaultReadBufferSize = 100000;
		private byte[] _receiveBuffer = null;
		private byte[] _answerBuffer = null;
		private int _receiveBufferBytesNow;

		public bool ConnectedReal { get; protected set; }
		public bool Connected { get; protected set; }

		public bool CustomMode { get; set; }

		public int SendBufferSize
		{
			get
			{
				return _sendBuffer.Length;
			}
			set
			{
				if (value > 0 && _sendBuffer.Length != value)
				{
					_sendBuffer = new byte[value];
				}
			}
		}

		public int ReceiveBufferSize
		{
			get
			{
				return _receiveBuffer.Length;
			}
			set
			{
				if (value > 0 && (_receiveBuffer == null || _receiveBuffer.Length != value))
				{
					_receiveBuffer = new byte[value];
					_answerBuffer = new byte[value + EofHelper.EofLength];
				}
			}
		}

		public SocketCustom()
		{
			InitializeBuffer(_defaultWriteBufferSize, _defaultReadBufferSize);
		}

		public SocketCustom(System.Net.Sockets.Socket socket, int sendBufferSize, int receiveBufferSize)
		{
			_socket = socket;

			InitializeBuffer(sendBufferSize, receiveBufferSize);
		}

		public virtual void Send(object data)
		{
			Task.Factory.StartNew(() =>
			{
				lock (_accessLock)
				{
					if (ConnectedReal)
					{
						if (_currentSendFinished)
						{
							_currentSendFinished = false;

							MemoryStream _sendStream = new MemoryStream(_sendBuffer, 0, _sendBuffer.Length);
							_sendStream.Position = 0;
							BinaryFormatter _formater = new BinaryFormatter();
							_formater.Serialize(_sendStream, data);

							_sendIndexNow = 0;
							int serializeLength = (int)_sendStream.Position;
							_sendTotalSize = EofHelper.AppendEof(_sendBuffer, serializeLength);
							try
							{
								_socket.BeginSend(_sendBuffer, 0, _sendTotalSize, SocketFlags.None, new AsyncCallback(SendCallback), _socket);
							}
							catch (System.Exception ex) //斷線
							{
								OnDisconnectedPrivate();
								OnErrorProtected(ex);
							}
						}
						else
						{
							MemoryStream stream = new MemoryStream();
							BinaryFormatter _formater = new BinaryFormatter();
							_formater.Serialize(stream, data);
							lock (_sendQueue)
							{
								_sendQueue.Add(stream.ToArray());
							}
						}
					}
				}
			}, TaskCreationOptions.LongRunning);
		}

		public virtual void SendBytes(byte[] bytes)
		{
			Task.Factory.StartNew(() =>
			{
				lock (_accessLock)
				{
					if (ConnectedReal)
					{
						if (_currentSendFinished)
						{
							_currentSendFinished = false;

							Array.Copy(bytes, _sendBuffer, bytes.Length);
							_sendTotalSize = bytes.Length;

							try
							{
								_socket.BeginSend(_sendBuffer, 0, _sendTotalSize, SocketFlags.None, new AsyncCallback(SendCallback), _socket);
							}
							catch (System.Exception ex) //斷線
							{
								OnDisconnectedPrivate();
								OnErrorProtected(ex);
							}
						}
						else
						{

							lock (_sendQueue)
							{
								_sendQueue.Add((byte[])bytes.Clone());
							}
						}
					}
				}
			}, TaskCreationOptions.LongRunning);
		}

		protected void StartReadLoop()
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					lock (_accessLock)
					{
						_socket.BeginReceive(
									_receiveBuffer,
									0,
									_receiveBuffer.Length,
									SocketFlags.None,
									new AsyncCallback(ReadCallback), _socket);
					}
				}
				catch (System.Exception ex)
				{
					OnDisconnectedPrivate();
					OnErrorProtected(ex);
				}
			}, TaskCreationOptions.LongRunning);
		}

		protected abstract void OnReceiveCommandProtected(object data);

		protected abstract void OnDisconnectedProtected();

		protected abstract void OnErrorProtected(System.Exception ex);

		private void InitializeBuffer(int sendBufferSize, int receiveBufferSize)
		{
			_sendBuffer = new byte[sendBufferSize];

			ReceiveBufferSize = receiveBufferSize;
		}

		private void ClearAllBuffer()
		{
			lock (_accessLock)
			{
				_sendQueue.Clear();
				_currentSendFinished = true;

				_sendTotalSize = 0;
				_sendIndexNow = 0;

				_receiveBufferBytesNow = 0;
			}
		}

		private void OnDisconnectedPrivate()
		{
			lock (_accessLock)
			{
				ClearAllBuffer();

				try
				{
					_socket.Shutdown(SocketShutdown.Both);
					_socket.Close();
					_socket = null;
				}
				catch (System.Exception ex)
				{
					OnErrorProtected(ex);
				}
				finally
				{
					OnDisconnectedProtected();
					ConnectedReal = false;
					Connected = false;
				}

			}
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			try
			{
				lock (_accessLock)
				{
					System.Net.Sockets.Socket handler = (System.Net.Sockets.Socket)asyncResult.AsyncState;
					int bytesSent = handler.EndSend(asyncResult);
					_sendIndexNow += bytesSent;
					if (_sendIndexNow < _sendTotalSize)
					{
						try
						{
							_socket.BeginSend(_sendBuffer, _sendIndexNow, _sendTotalSize - _sendIndexNow, SocketFlags.None, new AsyncCallback(SendCallback), _socket);
						}
						catch (System.Exception ex) //斷線
						{
							OnDisconnectedPrivate();
							OnErrorProtected(ex);
						}
						//OnErrorProtected(new Exception("SNF"));
					}
					else
					{
						_currentSendFinished = true;
						_sendIndexNow = 0;

						lock (_sendQueue)
						{
							if (_sendQueue.Count > 0)
							{
								_currentSendFinished = false;
								Array.Copy(_sendQueue[0], _sendBuffer, _sendQueue[0].Length);
								//_sendTotalSize = EofHelper.AppendEof(_sendBuffer, _sendQueue[0].Length); //客製化模式不該加這個
								_sendQueue.RemoveAt(0);
							}
						}

						if (!_currentSendFinished)
						{
							try
							{
								_socket.BeginSend(_sendBuffer, 0, _sendTotalSize, SocketFlags.None, new AsyncCallback(SendCallback), _socket);
							}
							catch (System.Exception ex) //斷線
							{
								OnDisconnectedPrivate();
								OnErrorProtected(ex);
							}
						}

					}
				}
			}
			catch (System.Exception ex) //斷線
			{
				OnDisconnectedPrivate();
				OnErrorProtected(ex);
			}
		}

		private void ReadCallback(IAsyncResult asyncResult)
		{
			System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)asyncResult.AsyncState;
			try
			{
				lock (_accessLock)
				{
					int bytesRead = 0;
					try
					{
						bytesRead = socket.EndReceive(asyncResult);
					}
					catch (System.Exception ex) //斷線
					{
						OnDisconnectedPrivate();
						OnErrorProtected(ex);
						return;
					}

					if (bytesRead > 0)
					{
						if (CustomMode)
						{
							try
							{
								byte[] answerCopy = new byte[bytesRead];
								Array.Copy(_receiveBuffer, answerCopy, bytesRead);
								OnReceiveCommandProtected(answerCopy);
							}
							catch (System.Exception ex)
							{
								OnErrorProtected(ex);
							}
						}
						else
						{
							_receiveBufferBytesNow += bytesRead;
							int indexNow = 0;
							int bytesRemainNow = _receiveBufferBytesNow;
							int answerLength = 0;
							bool didGetFullPacket = false;
							while (indexNow < _receiveBuffer.Length && indexNow < _receiveBufferBytesNow)
							{
								int eofIndex = -1;
								eofIndex = EofHelper.FindFullPacket(_receiveBuffer, indexNow, bytesRemainNow, _answerBuffer, ref answerLength);

								if (eofIndex > 0)
								{
									didGetFullPacket = true;

									try
									{
										MemoryStream bytesStream = new MemoryStream(_receiveBuffer, indexNow, answerLength);
										BinaryFormatter _formater = new BinaryFormatter();
										object obj = _formater.Deserialize(bytesStream);
										OnReceiveCommandProtected(obj);
									}
									catch (System.Exception ex)
									{
										OnDisconnectedPrivate();
										OnErrorProtected(ex);
									}

									indexNow = eofIndex + EofHelper.EofLength;
									bytesRemainNow = _receiveBufferBytesNow - indexNow;
								}
								else
								{
									//OnErrorProtected(new Exception("RNF"));
									break;
								}
							}

							//將剩餘不完整封包的資料移到最前面
							if (didGetFullPacket)
							{
								if (indexNow > 0 && bytesRemainNow > 0)
								{
									for (int whichIndex = indexNow; whichIndex < _receiveBufferBytesNow; ++whichIndex)
									{
										_receiveBuffer[whichIndex - indexNow] = _receiveBuffer[whichIndex];
									}
								}
							}

							_receiveBufferBytesNow = bytesRemainNow;
						}

						try
						{
							socket.BeginReceive(
								_receiveBuffer,
								_receiveBufferBytesNow,
								_receiveBuffer.Length - _receiveBufferBytesNow,
								SocketFlags.None,
								new AsyncCallback(ReadCallback), socket);
						}
						catch (System.Exception ex)
						{
							OnDisconnectedPrivate();
							OnErrorProtected(ex);
							return;
						}
					}
					else //斷線
					{
						OnDisconnectedPrivate();
					}
				}
			}
			catch (System.Exception ex)
			{
				OnDisconnectedPrivate();
				OnErrorProtected(ex);
			}
		}
	}
}
