#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;

namespace Lokad.Cloud.Web.Api10
{
	public class SoapApiKeyVerifier : UserNamePasswordValidator
	{
		readonly Maybe<string> _apiKey;

		public SoapApiKeyVerifier()
		{
			_apiKey = CloudEnvironment.GetConfigurationSetting("ManagementApiKey");
		}

		public override void Validate(string userName, string password)
		{
			if (!_apiKey.HasValue || (userName != _apiKey.Value))
			{
				throw new SecurityTokenException("Unknown Api Key");
			}
		}
	}
}
