using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Settings;

namespace UtilityCore.MailHelper.Setting
{
    [Serializable]
    public class MailSenderSetting : DeviceSettingBase
    {
        private string _address;
        public string Address
        {
            get
            {
                return _address;
            }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged("Address");
                }
            }
        }
        private string _displayName;
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }
        private string _smtpHost;
        public string SmtpHost
        {
            get
            {
                return _smtpHost;
            }
            set
            {
                if (_smtpHost != value)
                {
                    _smtpHost = value;
                    OnPropertyChanged("SmtpHost");
                }
            }
        }
        private int _smtpPort;
        public int SmtpPort
        {
            get
            {
                return _smtpPort;
            }
            set
            {
                if (_smtpPort != value)
                {
                    _smtpPort = value;
                    OnPropertyChanged("SmtpPort");
                }
            }
        }
        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }
        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged("Password");
                }
            }
        }
        public override void Initialize()
        {
            if (Address == null)
            {
                Address = "xiao.yu@yeontech.com";
            }
            if (DisplayName == null)
            {
                DisplayName = "AOI Statistic";
            }
            if (SmtpHost == null)
            {
                SmtpHost = "mail.yfy.com";
            }
            if (SmtpPort == 0)
            {
                SmtpPort = 25;
            }
            if (UserName == null)
            {
                UserName = "xiao.yu";
            }
            if (Password == null)
            {
                Password = "a82859348";
            }
        }
    }
}
