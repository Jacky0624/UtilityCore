using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.HIDUART
{
    public class HIDInfo : ViewModelBase
    {
        private ushort _vid;
        public ushort VID
        {
            get
            {
                return _vid;
            }
            set
            {
                if (_vid != value)
                {
                    _vid = value;
                    OnPropertyChanged("VID");
                }
            }
        }
        private ushort _pid;
        public ushort PID
        {
            get
            {
                return _pid;
            }
            set
            {
                if (_pid != value)
                {
                    _pid = value;
                    OnPropertyChanged("PID");
                }
            }
        }
        private string _serialNumber;
        public string SerialNumber
        {
            get
            {
                return _serialNumber;
            }
            set
            {
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    OnPropertyChanged("SerialNumber");
                }
            }
        }

        public HIDInfo(string serialNo, ushort vid, ushort pid)
        {
            this.VID = vid;
            this.PID = pid;
            this.SerialNumber = serialNo;
        }
    }
}
