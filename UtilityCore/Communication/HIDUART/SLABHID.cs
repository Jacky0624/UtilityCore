using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilityCore.Communication.HIDUART
{
    public class SLABHID
    {
        private IntPtr _device;
        private uint _deviceNum = 0;
        private int _numByteToRead = 64;
        private List<byte[]> _writeDatas;
        public event Action OnConnected;
        public event Action OnDisConnected;
        public event Action<object> OnReceieveEvent;
        public event Action<byte[]> OnReceieveBytes;
        public event Action<string> OnReceieveString;
        public SLABHID()
        {
            if (_writeDatas == null)
            {
                _writeDatas = new List<byte[]>();
            }
        }
        public void Connect(ushort vid, ushort pid)
        {
            if (SLABHIDtoUART.HidUart_Open(ref _device, _deviceNum, vid, pid) == 0)
            {
                if (SLABHIDtoUART.HidUart_SetUartEnable(_device, 1) == 0)
                {
                    CallOnConnected();
                    StartReadLoop();
                    StartWriteLoop();
                }
            }
        }

        private void CallOnConnected()
        {
            Task.Factory.StartNew(() =>
            {
                OnConnected?.Invoke();
            });
        }
        private void CallOnDisConnected()
        {
            Task.Factory.StartNew(() =>
            {
                OnDisConnected?.Invoke();
            });
        }
        public bool CustomMode { get; set; }
        protected void OnReceiveCommandProtected(object data)
        {
            Task.Factory.StartNew(() =>
            {
                if (CustomMode)
                {
                    OnReceieveBytes((byte[])data);
                }
                else
                {
                    OnReceieveEvent?.Invoke(data);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public async void Close()
        {
            if (_device != null)
            {
                _writeCancel.Cancel();
                _readCancel.Cancel();
                await Task.Delay(2000);

                if (SLABHIDtoUART.HidUart_Close(_device) == 0)
                {

                    CallOnDisConnected();
                }
            }
        }
        public void ClearWriteBuffer()
        {
            lock (_accessLock)
            {
                if (_writeDatas != null)
                {
                    _writeDatas.Clear();
                }

            }

        }
        private CancellationTokenSource _readCancel = new CancellationTokenSource();
        public void StartReadLoop()
        {
            _readCancel.Cancel();
            _readCancel = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {

                while (!_readCancel.IsCancellationRequested)
                {
                    byte[] buffer = new byte[64];
                    try
                    {
                        uint num = 0;
                        var result = SLABHIDtoUART.HidUart_Read(_device, buffer, _numByteToRead, ref num);
                        //if (result == 0)
                        {
                            if (num > 0)
                            {
                                byte[] bytes = new byte[num];
                                Array.Copy(buffer, 0, bytes, 0, num);
                                OnReceieveBytes?.Invoke(bytes);
                            }

                        }
                    }
                    catch (Exception ex)
                    {


                    }

                    await Task.Delay(13, _readCancel.Token);
                }

            }, _readCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public List<HIDInfo> GetHIDInfos()
        {
            uint nums = 0;
            ushort vid = 0;
            ushort pid = 0;
            ushort releaseNumber = 0;
            List<HIDInfo> hidList = new List<HIDInfo>();
            var result = SLABHIDtoUART.HidUart_GetNumDevices(ref nums, 0, 0);
            if (result == 0)
            {
                for (uint i = 0; i < nums; i++)
                {
                    result = SLABHIDtoUART.HidUart_GetAttributes(i, 0, 0, ref vid, ref pid, ref releaseNumber);
                    if (result == 0)
                    {
                        StringBuilder sb = new StringBuilder(512);
                        result = SLABHIDtoUART.HidUart_GetString(i, 0, 0, sb, 0x04);
                        if (result == 0)
                        {
                            hidList.Add(new HIDInfo(sb.ToString(), vid, pid));
                        }
                    }
                }
            }
            return hidList;
        }

        public void Write(byte[] bytes)
        {
            lock (_accessLock)
            {
                _writeDatas.Add(bytes);
            }
        }
        private object _accessLock = new object();
        private CancellationTokenSource _writeCancel = new CancellationTokenSource();
        public void StartWriteLoop()
        {
            _writeCancel.Cancel();
            _writeCancel = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (!_writeCancel.IsCancellationRequested)
                {
                    lock (_accessLock)
                    {
                        if (_writeDatas.Count > 0)
                        {
                            for (int i = 0; i < _writeDatas.Count; i++)
                            {
                                int num = 0;
                                try
                                {
                                    if (SLABHIDtoUART.HidUart_Write(_device, _writeDatas[i], _writeDatas[i].Length, ref num) == 0)
                                    {

                                    }
                                }
                                catch (Exception ex)
                                {

                                    throw;
                                }

                            }
                            _writeDatas.Clear();
                        }
                    }
                    await Task.Delay(13, _writeCancel.Token);
                }

            }, _writeCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        }
    }
}
