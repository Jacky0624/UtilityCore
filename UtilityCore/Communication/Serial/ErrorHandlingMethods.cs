﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityCore.Communication.Serial
{
	public enum ErrorHandlingMethods
	{
		RaiseOnAlarmEvent,
		AlarmManager,
		//ThrowException,
		DoNothing
	}
}
