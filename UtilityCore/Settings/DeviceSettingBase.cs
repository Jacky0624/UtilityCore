using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Settings
{
	[Serializable]
	public abstract class DeviceSettingBase : ViewModelBase
	{
		public abstract void Initialize();
	}
}
