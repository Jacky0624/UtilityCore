using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
    [Serializable]
    public class SerialHIDUSBSetting : ViewModelBase
    {


        private string _targetDeviceName;
        public string TargetDeviceName
        {
            get
            {
                return _targetDeviceName;
            }
            set
            {
                if (_targetDeviceName != value)
                {
                    _targetDeviceName = value;
                    OnPropertyChanged("TargetDeviceName");
                }
            }
        }
        public void Initialize()
        {
            if (TargetDeviceName == null)
            {
                TargetDeviceName = "";
            }
        }
    }
}
