using System;
using System.ComponentModel;

namespace UtilityCore
{
	[Serializable]
	public class ViewModelBase : INotifyPropertyChanged //interface
	{
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
