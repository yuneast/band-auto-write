using System;
using System.Collections.Specialized;
using System.Configuration;

namespace BandProgram
{
	public class AppConfiguration
	{
		public AppConfiguration()
		{
		}

		public static void AddAppConfig(string key, string value)
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			configuration.AppSettings.Settings.Add(key, value);
			configuration.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
		}

		public static string GetAppConfig(string key)
		{
			return ConfigurationManager.AppSettings[key];
		}

		public static void RemoveAppConfig(string key)
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;
			try
			{
				settings.Remove(key);
				configuration.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
			}
			catch
			{
			}
		}

		public static void SetAppConfig(string key, string value)
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;
			settings.Remove(key);
			settings.Add(key, value);
			configuration.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
		}
	}
}