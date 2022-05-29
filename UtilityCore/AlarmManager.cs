using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCore.Communication.Alarm;
using UtilityCore.Log.Alarm;

namespace UtilityCore
{
	public class AlarmManager : ViewModelBase
	{
		public static readonly AlarmManager _instance = null;

		public static event Action OnAlarmReset;

		public event Action OnAlarm;

		//public StackLight StackLight { get; set; }

		public bool UseBuzzer { get; set; } = true;

		public bool AlarmExist
		{
			get
			{
				lock (_accessLock)
				{
					return _existingAlarms.Count > 0;
				}
			}
		}

		public string Language { get; set; }

		private List<AlarmBase> _existingAlarms = new List<AlarmBase>();

		private ObservableCollection<AlarmBase> _ExistingAlarmsUI;
		public ObservableCollection<AlarmBase> ExistingAlarmsUI
		{
			get
			{
				return _ExistingAlarmsUI;
			}
			set
			{
				if (_ExistingAlarmsUI != value)
				{
					_ExistingAlarmsUI = value;
					OnPropertyChanged("ExistingAlarmsUI");
				}
			}
		}

		private List<AlarmBase> _historyAlarms = new List<AlarmBase>();

		private ObservableCollection<AlarmBase> _HistoryAlarmsUI;
		public ObservableCollection<AlarmBase> HistoryAlarmsUI
		{
			get
			{
				return _HistoryAlarmsUI;
			}
			set
			{
				if (_HistoryAlarmsUI != value)
				{
					_HistoryAlarmsUI = value;
					OnPropertyChanged("HistoryAlarmsUI");
				}
			}
		}

		private AlarmBase _LastAlarm;
		public AlarmBase LastAlarm
		{
			get
			{
				return _LastAlarm;
			}
			set
			{
				if (_LastAlarm != value)
				{
					_LastAlarm = value;
					OnPropertyChanged("LastAlarm");
					_instance.OnPropertyChanged("AlarmExist");
				}
			}
		}

		private object _accessLock = new object();

		static AlarmManager()
		{
			_instance = new AlarmManager();
		}

		public static void Alarm(AlarmBase alarmCode, string message = "", bool activateBuzzer = true)
		{
			return;
			_instance.AlarmInternal(alarmCode, message, activateBuzzer);
		}

		public void AlarmInternal(AlarmBase alarm, string message = "", bool activateBuzzer = true)
		{

			List<AlarmBase> _existAlarmsCopy = new List<AlarmBase>();
			List<AlarmBase> _historyAlarmsCopy = new List<AlarmBase>();

			lock (_accessLock)
			{
				string newAlarmName = alarm.GetType().Name;

				bool exist = false;

				int i = 0;

				for (i = 0; i < _existingAlarms.Count; ++i)
				{
					AlarmBase whichAlarm = _existingAlarms[i];
					if (whichAlarm.GetType().Name == newAlarmName)
					{
						exist = true;
						_existingAlarms[i] = alarm;
						break;
					}
				}

				if (!exist)
				{
					_existingAlarms.Add(alarm);
				}

				_historyAlarms.Add(alarm);

				if (_historyAlarms.Count > 200)
				{
					_historyAlarms.RemoveAt(0);
				}

				_existAlarmsCopy = new List<AlarmBase>(_existingAlarms);
				_historyAlarmsCopy = new List<AlarmBase>(_historyAlarms);
			}

			//TaskHelper.RunOnUiThread(() =>
			//{
			//	ExistingAlarmsUI = new ObservableCollection<AlarmBase>(_existAlarmsCopy);
			//	HistoryAlarmsUI = new ObservableCollection<AlarmBase>(_historyAlarmsCopy);
			//});

			LastAlarm = alarm;


			Language = "";

			System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Language);

			//if (StackLight != null)
			//{
			//	StackLight.ChangeState(StackLight.States.Alarm);

			//	if (!activateBuzzer)
			//	{
			//		StackLight.StopBuzzer();
			//	}
			//}

			string allMessage = null;


			if (alarm.ContainMessage)
			{
				if (message == "")
				{
					allMessage = string.Format("{0}: {1}", alarm, alarm.Message);
				}
				else
				{
					allMessage = string.Format("{0}: {1}, {2}", alarm, alarm.Message, message);
				}
			}
			else
			{
				if (message == "")
				{
					allMessage = string.Format("{0}: {1}", alarm, message);
				}
				else
				{
					allMessage = string.Format("{0}", alarm);
				}
			}

			if (!(alarm is Warning))
			{
			}

			OnAlarm?.Invoke();

			Log.Log.WriteLine(allMessage);
		}

		internal static void Alarm(WriteLogFailAlarm writeLogFailAlarm)
		{
			throw new NotImplementedException();
		}

		public static void AlarmReset()
		{
			_instance.AlarmResetInternal();
		}

		internal void AlarmResetInternal()
		{
			lock (_accessLock)
			{
				_existingAlarms.Clear();

			}

			//TaskHelper.RunOnUiThread(() =>
			//{
			//	ExistingAlarmsUI = new ObservableCollection<AlarmBase>();
			//});

			//if (StackLight != null)
			//{
			//	StackLight.ChangeState(StackLight.States.Stopped);
			//}

			_instance.OnPropertyChanged("AlarmExist");

			try
			{
				OnAlarmReset?.Invoke();
			}
			catch (Exception)
			{
			}
		}
	}
}
