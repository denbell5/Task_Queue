using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data.Constants;

namespace Task_Queue.InternalServices
{
	public class RegistryService
	{
		public static object GetParameterValue(string path, string parameterName)
		{
			var hklm = RegistryKey.OpenBaseKey(
				RegistryHive.LocalMachine,
				RegistryView.Registry64);

			var parametersKey = hklm.OpenSubKey(path);
			var parameterValue = parametersKey.GetValue(parameterName);

			return parameterValue;
		}
	}
}
