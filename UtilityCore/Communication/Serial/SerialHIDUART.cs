using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.HIDUART;

namespace UtilityCore.Communication.Serial
{
    public class SerialHIDUART : SerialPhysicalBase
    {
        private HIDInfo _targetDevice
        {
            get
            {
                return _deviceDic[Setting.TargetDeviceName];
            }
        }
        Dictionary<string, HIDInfo> _deviceDic = new Dictionary<string, HIDInfo>();
        public SerialHIDUSBSetting Setting { get; set; }
        private SLABHID _SLABHID;
        public SerialHIDUART(SerialHIDUSBSetting setting)
        {
            this.Setting = setting;
            setting.Initialize();
            _SLABHID = new SLABHID();
            List<string> deviceNameList = GetDeviceList();
            if (!_deviceDic.ContainsKey(Setting.TargetDeviceName))
            {
                Setting.TargetDeviceName = deviceNameList[0];
            }

            _SLABHID.OnConnected += OnConnected;
            _SLABHID.OnDisConnected += OnDisconnected;
            _SLABHID.OnReceieveBytes += OnReceiveBytesInternal;
        }
        public List<string> GetDeviceList()
        {
            List<HIDInfo> list = _SLABHID.GetHIDInfos();
            List<string> nameList = new List<string>();
            _deviceDic = new Dictionary<string, HIDInfo>();
            for (int i = 0; i < list.Count; i++)
            {
                int count = 2;
                string str = string.Format("PID:{0},VID:{1},SerNO:{2}", list[i].PID, list[i].VID, list[i].SerialNumber);
                while (_deviceDic.ContainsKey(str))
                {
                    str = string.Format("{0}({1})", str, count);
                    count++;
                }
                nameList.Add(str);
                _deviceDic.Add(str, list[i]);
            }
            //DeviceNameList = nameList;
            if (Setting.TargetDeviceName == null)
            {
                Setting.TargetDeviceName = nameList[0];
            }
            return nameList;
        }
        public override void ConnectInternal()
        {
            try
            {
                _SLABHID.Connect((ushort)(_deviceDic[Setting.TargetDeviceName].VID), (ushort)_deviceDic[Setting.TargetDeviceName].PID);
            }
            catch (Exception ex)
            {
                OnDisconnected();
                //throw ex;
            }
        }

        public override void DisconnectInternal()
        {
            try
            {
                _SLABHID.Close();
            }
            catch (Exception ex)
            {


            }
            OnDisconnected();
        }

        protected override void Send(byte[] bytes)
        {
            _SLABHID.Write(bytes);
        }
    }
}
