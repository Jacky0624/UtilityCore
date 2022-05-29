using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UtilityCore.Communication.Serial;

namespace UtilityCore.Settings
{
	[Serializable]
	public abstract class AppSettingBase : INotifyPropertyChanged
	{
		public static string SettingsFilePath
		{
			get
			{
				return string.Format("{0}/Settings/settings.bin", "C:/");
			}
		}

		public static AppSettingBase Current { get; private set; }

		private Dictionary<string, SerialSetting> _serialSettings = new Dictionary<string, SerialSetting>();
		public Dictionary<string, SerialSetting> SerialSettings
		{
			get
			{
				return _serialSettings;
			}
		}

		public string Language { get; set; }

		public abstract void Initialize();

		public static void Save()
		{
			try
			{
				DateTime now = DateTime.Now;

				CreateDirectoryIfNotExist(SettingsFilePath);

				string tempFilePath = SettingsFilePath + ".temp";
				string newOriginalFilePath = SettingsFilePath + ".original";

				BinaryFormatter formatter = new BinaryFormatter();
				using (Stream file = File.Open(tempFilePath, FileMode.OpenOrCreate))
				{
					formatter.Serialize(file, Current);
				}

				string BackupPath = string.Format("{0}/Settings/History/{1}/{2}/{3}/{4}.bin", "C:/", now.Year, now.Month, now.Day, now.ToString("HH_mm_ss_fff"));
				CreateDirectoryIfNotExist(BackupPath);

				if (File.Exists(SettingsFilePath))
				{
					File.Copy(SettingsFilePath, BackupPath);
					File.Move(SettingsFilePath, newOriginalFilePath);
					File.Move(tempFilePath, SettingsFilePath);
					File.Delete(newOriginalFilePath);
				}
				else
				{
					File.Move(tempFilePath, SettingsFilePath);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("{0}, {1}", "Setting write fail", ex.Message));
			}
		}

		public static AppSettingBase Load(AppSettingBase defaultSettings)
		{
			AppSettingBase answer = null;
			try
			{
				bool success = CreateDirectoryIfNotExist(SettingsFilePath);

				if (success)
				{
					if (!File.Exists(SettingsFilePath))
					{
						throw new Exception("Settings file not exist, use default settings");
						answer = defaultSettings;
					}
					else
					{
						BinaryFormatter formatter = new BinaryFormatter();
						using (Stream file = File.Open(SettingsFilePath, FileMode.Open))
						{
							answer = (AppSettingBase)formatter.Deserialize(file);

							if (answer._serialSettings == null)
							{
								answer._serialSettings = new Dictionary<string, SerialSetting>();
							}

							answer.Initialize();

							if (answer.Language == null)
							{
								answer.Language = "";
							}

							System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(answer.Language);
						}
					}
				}
				else
				{
					Environment.Exit(0);
					//System.Windows.Application.Current.Shutdown();
				}
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("{0}, {1}", "setting read fail", ex.Message));
				//Environment.Exit(0);
				//System.Windows.Application.Current.Shutdown();
			}

			Current = answer;

			return answer;
		}

		private static bool CreateDirectoryIfNotExist(string filePath)
		{
			bool success = false;
			string directoryPath = Path.GetDirectoryName(filePath);
			if (!File.Exists(directoryPath))
			{
				try
				{
					Directory.CreateDirectory(directoryPath);
					success = true;
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Create Folder Fail: {0}, {1}", directoryPath, ex.Message));
					Environment.Exit(0);
					//System.Windows.Application.Current.Shutdown();
				}
			}
			return success;
		}


		#region INotifyPropertyChanged
		[field: NonSerialized()]
		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyChanged)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyChanged));
			}
		}
		#endregion


	}
}
