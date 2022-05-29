using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Alarm;
using UtilityCore.Log;
using UtilityCore.Settings;

namespace UtilityCore.Communication.Serial
{
    public abstract class SerialDevice : ViewModelBase
    {
        public event Action<ConnectionStates> OnConnectionStateChangedEvent;

        public event Action<byte[]> OnSendBytes;

        public event Action<string> OnReceiveStringEvent;
        public event Action<byte[]> OnReceiveBytesEvent;

        public event Action<string> OnLog;

        public event Action<AlarmBase> OnAlarm;

        private static List<string> _existSerialNames = new List<string>();
        private static List<SerialDevice> _existSerials = new List<SerialDevice>();

        private LogWorker _LogWorker;
        public LogWorker LogWorker
        {
            get
            {
                return _LogWorker;
            }
            set
            {
                if (_LogWorker != value)
                {
                    _LogWorker = value;
                    OnPropertyChanged("LogWorker");
                }
            }
        }

        private string _name = null;

        public bool Connected
        {
            get
            {
                return ConnectionState == ConnectionStates.Connected;
            }
        }

        private ConnectionStates _connectionState = ConnectionStates.Disconnected;
        public ConnectionStates ConnectionState
        {
            get
            {
                return _connectionState;
            }
            set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    OnPropertyChanged("ConnectionState");
                    OnPropertyChanged("Connected");
                }
            }
        }

        public SerialModes Mode
        {
            get
            {
                return SerialSetting.Mode;
            }
            set
            {
                if (value != SerialSetting.Mode)
                {
                    DoChangeMode(value);
                }
            }
        }

        public Func<byte[], IsFullPacketResult> CheckIsFullPacket
        {
            get
            {
                return SerialInstance.Helper.CheckIsFullPacket;
            }
            set
            {
                SerialInstance.Helper.CheckIsFullPacket = value;
            }

        }

        private void DoChangeMode(SerialModes value)
        {
            if (SerialInstance != null)
            {
                SerialInstance.OnAlarm -= Alarm;
                SerialInstance.OnSendBytes -= CallOnSendBytes;
                SerialInstance.OnReceiveString -= CallOnReceiveString;
                SerialInstance.OnReceiveBytes -= CallOnReceiveBytes;
                SerialInstance.OnConnectionStateChanged -= CallOnConnectionStateChanged;

                SerialInstance.Helper.CheckIsFullPacket = null;
            }

            SerialSetting.Mode = value;
            if (SerialSetting.Mode == SerialModes.RS232)
            {
                SerialInstance = new SerialRS232(SerialSetting.RS232Setting);
            }
            else if (SerialSetting.Mode == SerialModes.TCP)
            {
                SerialInstance = new SerialTCP(SerialSetting.TCPSetting);
            }
            else if (SerialSetting.Mode == SerialModes.HID)
            {
                SerialInstance = new SerialHIDUART(SerialSetting.HIDSetting);
            }
            if (SerialInstance != null)
            {
                SerialInstance.OnAlarm += Alarm;
                SerialInstance.OnSendBytes += CallOnSendBytes;
                SerialInstance.OnReceiveString += CallOnReceiveString;
                SerialInstance.OnReceiveBytes += CallOnReceiveBytes;
                SerialInstance.OnConnectionStateChanged += CallOnConnectionStateChanged;

                SerialInstance.Helper.Setting = SerialSetting;
                SerialInstance.Helper.CheckIsFullPacket = CheckIsFullPacket;
            }

        }

        public SerialSetting SerialSetting { get; private set; }

        private SerialPhysicalBase _SerialInstance;
        public SerialPhysicalBase SerialInstance
        {
            get
            {
                return _SerialInstance;
            }
            set
            {
                if (_SerialInstance != value)
                {
                    _SerialInstance = value;
                    OnPropertyChanged("SerialInstance");
                }
            }
        }

        public string LocalIp
        {
            get
            {
                if (SerialSetting.Mode == SerialModes.TCP)
                {
                    return (SerialInstance as SerialTCP).LocalIp;
                }
                else
                {
                    return null;
                }
            }
        }

        public int LocalPort
        {
            get
            {
                if (SerialSetting.Mode == SerialModes.TCP)
                {
                    return (SerialInstance as SerialTCP).LocalPort;
                }
                else
                {
                    return 0;
                }
            }
        }

        public SerialDevice(string name, ConnectMethods connectMethod = ConnectMethods.Immediately)
        {
            try
            {
                if (_existSerialNames.Contains(name))
                {
                    throw new Exception(string.Format("不可以創造兩個同名的Serial: {0}", name));
                }
                else
                {
                    _name = name;

                    LogWorker = new LogWorker();

                    _existSerialNames.Add(_name);
                    _existSerials.Add(this);

                    if (AppSettingBase.Current.SerialSettings.ContainsKey(_name))
                    {

                    }
                    else
                    {
                        AppSettingBase.Current.SerialSettings[name] = new SerialSetting();
                    }

                    SerialSetting = AppSettingBase.Current.SerialSettings[name];
                    SerialSetting.Initialize();

                    DoChangeMode(SerialSetting.Mode);
                    if (SerialInstance != null)
                    {
                        SerialInstance.Helper.Setting = SerialSetting;
                    }


                    if (connectMethod == ConnectMethods.Immediately)
                    {
                        Connect();
                    }
                }
            }
            catch (Exception)
            {
                if (SerialSetting == null)
                {
                    SerialSetting = new SerialSetting();
                }
                SerialSetting.Initialize();
                DoChangeMode(SerialSetting.Mode);
                SerialInstance.Helper.Setting = SerialSetting;
                LogWorker = new LogWorker();
            }



        }
        public void Remove(string name)
        {
            _existSerialNames.Remove(name);
            AppSettingBase.Current.SerialSettings.Remove(name);
        }
        public void Connect()
        {
            if (SerialInstance != null)
            {
                SerialInstance.Connect();
            }
        }

        public void Disconnect()
        {
            if (SerialInstance != null)
            {
                SerialInstance.Disconnect();
            }
        }

        public async Task<bool> Reconnect()
        {
            bool success = false;
            try
            {
                Disconnect();
            }
            catch (Exception) { }

            Stopwatch timeoutWatch = new Stopwatch();
            timeoutWatch.Restart();

            while (ConnectionState != ConnectionStates.Disconnected)
            {
                if (timeoutWatch.ElapsedMilliseconds > 1000)
                {
                    success = false;
                    return success;
                }
                else
                {
                    await Task.Delay(100);
                }
            }

            Connect();

            timeoutWatch.Restart();
            while (ConnectionState != ConnectionStates.Connected)
            {
                if (timeoutWatch.ElapsedMilliseconds > 1000)
                {
                    success = false;
                    return success;
                }
                else
                {
                    await Task.Delay(100);
                }
            }

            success = true;

            return success;
        }

        protected abstract void OnReceiveString(string message);

        protected abstract void OnReceiveBytes(byte[] bytes);

        private readonly object _accessLock = new object();

        protected abstract void OnConnectionStateChanged(ConnectionStates connected);

        //送出字串
        public void SendString(string message)
        {
            SerialInstance.SendString(message);
        }

        public async Task<string> SendStringAsync(string message, int timeoutMs = -1)
        {
            return await SerialInstance.SendStringAsync(message, timeoutMs);
        }

        public async Task SendStringAsyncNoResponse(string message, int completeTimeMs = 500)
        {
            await SerialInstance.SendStringAsyncNoResponse(message, completeTimeMs);
        }

        //送出byte array
        public void SendBytes(byte[] bytes)
        {
            try
            {
                SerialInstance.SendBytes(bytes);
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<byte[]> SendBytesAsync(byte[] bytes, int timeoutMs = -1)
        {

            return await SerialInstance.SendBytesAsync(bytes, timeoutMs);
        }


        public async Task SendBytesAsyncNoResponse(byte[] bytes, int completeTimeMs = 500)
        {
            await SerialInstance.SendBytesAsyncNoResponse(bytes, completeTimeMs);
        }

        public static async Task DisconnectAllSerialDevice()
        {
            List<Task> list = new List<Task>();
            foreach (SerialDevice s in _existSerials)
            {
                SerialDevice sCopy = s;
                list.Add(Task.Factory.StartNew(() =>
                {
                    sCopy.Disconnect();
                }, TaskCreationOptions.LongRunning));
            }

            await Task.WhenAll(list);
        }

        protected byte ComputeCheckSum(byte[] bytes, int start, int length)
        {
            byte answer = 0;
            for (int i = 0; i < length; ++i)
            {
                answer ^= bytes[start + i];
            }

            return answer;
        }

        private void CallOnSendBytes(byte[] bytes)
        {
            LogWorker.WriteLine("Send bytes: {0}", BytesToString(bytes));
            if (OnSendBytes != null)
            {
                OnSendBytes(bytes);
            }
        }

        protected void Alarm(AlarmBase alarm)
        {
            LogWorker.WriteLine("Alarm: {0}", alarm.Message);

            if (SerialSetting.ErrorHandlingMethod == ErrorHandlingMethods.RaiseOnAlarmEvent)
            {
                OnAlarm?.Invoke(alarm);
            }
            else if (SerialSetting.ErrorHandlingMethod == ErrorHandlingMethods.AlarmManager)
            {
                AlarmManager.Alarm(alarm);
            }
            //else if (SerialSetting.ErrorHandlingMethod == ErrorHandlingMethods.ThrowException) //TODO
            //{
            //	if (alarm.Exception != null)
            //	{
            //		throw alarm.Exception;
            //	}
            //	else
            //	{
            //		throw new Exception(alarm.Message);
            //	}
            //}
            else if (SerialSetting.ErrorHandlingMethod == ErrorHandlingMethods.DoNothing)
            {

            }
        }

        private void CallOnConnectionStateChanged(ConnectionStates newConnectionState)
        {
            LogWorker.WriteLine("Connection State: {0}", newConnectionState);

            ConnectionState = newConnectionState;

            OnConnectionStateChanged(newConnectionState);

            if (OnConnectionStateChangedEvent != null)
            {
                OnConnectionStateChangedEvent(newConnectionState);
            }
        }

        private void CallOnReceiveString(string message)
        {
            LogWorker.WriteLine("Receive String: {0}", message);

            OnReceiveString(message);

            if (OnReceiveStringEvent != null)
            {
                OnReceiveStringEvent(message);
            }
        }

        private void CallOnReceiveBytes(byte[] bytes)
        {
            LogWorker.WriteLine("Receive bytes: {0}", BytesToString(bytes));

            OnReceiveBytes(bytes);

            if (OnReceiveBytesEvent != null)
            {
                OnReceiveBytesEvent(bytes);
            }
        }

        protected string BytesToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            if (bytes != null)
            {
                for (int whichByte = 0; whichByte < bytes.Length; ++whichByte)
                {
                    sb.AppendFormat("{0:X2}", bytes[whichByte]);
                    if (whichByte < bytes.Length - 1)
                    {
                        sb.Append(" ");
                    }
                }
            }
            return sb.ToString();
        }

        protected bool BytesEqual(byte[] bytes1, byte[] bytes2)
        {
            bool answer = false;
            if (bytes1 != null && bytes2 != null)
            {
                if (bytes1.Length == bytes2.Length)
                {
                    for (int i = 0; i < bytes1.Length; ++i)
                    {
                        if (bytes1[i] != bytes2[i])
                        {
                            break;
                        }
                        else if (i >= bytes1.Length - 1)
                        {
                            answer = true;
                        }
                    }
                }
            }

            return answer;
        }

        protected void WriteLog(string format, params object[] args)
        {
            LogWorker.WriteLine(format, args);

            string message = null;
            if (args == null || args.Length <= 0)
            {
                message = format;
            }
            else
            {
                message = string.Format(format, args);
            }


            OnLog?.Invoke(message);
        }
    }
}
