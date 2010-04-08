#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	[Serializable]
	public class RoleConfigurationSettings : ICloudConfigurationSettings
	{
		public string DataConnectionString { get; set; }
		public string SelfManagementSubscriptionId { get; set; }
		public string SelfManagementCertificateThumbprint { get; set; }

		public static Maybe<ICloudConfigurationSettings> LoadFromRoleEnvironment()
		{
			if (!CloudEnvironment.IsAvailable)
			{
				return Maybe<ICloudConfigurationSettings>.Empty;
			}

			var setting = new RoleConfigurationSettings();
			ApplySettingFromRole("DataConnectionString", v => setting.DataConnectionString = v);
			ApplySettingFromRole("SelfManagementSubscriptionId", v => setting.SelfManagementSubscriptionId = v);
			ApplySettingFromRole("SelfManagementCertificateThumbprint", v => setting.SelfManagementCertificateThumbprint = v);
			return setting;
		}

		static void ApplySettingFromRole(string setting, Action<string> setter)
		{
			CloudEnvironment.GetConfigurationSetting(setting).Apply(setter);
		}
	}

	/// <summary>
	/// Settings used among others by the <see cref="Lokad.Cloud.Storage.Azure.StorageModule" />.
	/// </summary>
	public interface ICloudConfigurationSettings
	{
		/// <summary>
		/// Gets the data connection string.
		/// </summary>
		/// <value>The data connection string.</value>
		string DataConnectionString { get; }

		/// <summary>
		/// Gets the Azure subscription Id to be used for self management (optional, can be null).
		/// </summary>
		string SelfManagementSubscriptionId { get; }

		/// <summary>
		/// Gets the Azure certificate thumbpring to be used for self management (optional, can be null).
		/// </summary>
		string SelfManagementCertificateThumbprint { get; }
	}
}