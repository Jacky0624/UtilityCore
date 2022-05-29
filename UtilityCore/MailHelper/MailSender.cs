using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.MailHelper.Setting;

namespace UtilityCore.MailHelper
{
    public class MailSender : ViewModelBase
    {
        public MailSenderSetting Setting { get; set; }
        public MailSender(MailSenderSetting setting)
        {
            Setting = setting;
        }
        public async Task SendMailAsync(string body, List<string> ccItems, List<string> toItems)
        {
            System.Net.Mail.MailMessage MyMail = new System.Net.Mail.MailMessage();
            MyMail.From = new System.Net.Mail.MailAddress(Setting.Address, Setting.DisplayName);

            for (int i = 0; i < toItems.Count; i++)
            {
                MyMail.To.Add(toItems[i]);
            }

            for (int i = 0; i < ccItems.Count; i++)
            {
                MyMail.CC.Add(ccItems[i]);
            }
            MyMail.Subject = "AOI Statistic";
            MyMail.Body = body;
            MyMail.IsBodyHtml = true; //是否使用html格式
            System.Net.Mail.SmtpClient MySMTP = new System.Net.Mail.SmtpClient(Setting.SmtpHost, Setting.SmtpPort);
            MySMTP.Credentials = new System.Net.NetworkCredential(Setting.UserName, Setting.Password);
            //MySMTP.EnableSsl = true;
            MySMTP.Timeout = 200000;
            try
            {
                await MySMTP.SendMailAsync(MyMail);
                MyMail.Dispose(); //釋放資源
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void SendMail(string body, List<string> ccItems, List<string> toItems)
        {
            System.Net.Mail.MailMessage MyMail = new System.Net.Mail.MailMessage();
            MyMail.From = new System.Net.Mail.MailAddress(Setting.Address, Setting.DisplayName);

            for (int i = 0; i < toItems.Count; i++)
            {
                MyMail.To.Add(toItems[i]);
            }

            for (int i = 0; i < ccItems.Count; i++)
            {
                MyMail.CC.Add(ccItems[i]);
            }
            MyMail.Subject = "Beacon Alert";
            MyMail.Body = body;
            MyMail.IsBodyHtml = true; //是否使用html格式
            System.Net.Mail.SmtpClient MySMTP = new System.Net.Mail.SmtpClient(Setting.SmtpHost, Setting.SmtpPort);
            MySMTP.Credentials = new System.Net.NetworkCredential(Setting.UserName, Setting.Password);
            //MySMTP.EnableSsl = true;
            MySMTP.Timeout = 200000;
            try
            {
                MySMTP.Send(MyMail);
                MyMail.Dispose(); //釋放資源
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
