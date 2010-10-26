#region (c)2009 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

[assembly : AssemblyCompany("Lokad")]
[assembly : AssemblyProduct("Lokad.Cloud")]
[assembly : AssemblyCulture("")]
[assembly : ComVisible(false)]
[assembly : AllowPartiallyTrustedCallers]

///<summary>
/// Assembly information class that is shared between all projects
///</summary>
internal static class GlobalAssemblyInfo
{
	// copied from the Lokad.Client 'GlobalAssemblyInfo.cs'
	internal const string PublicKey =
		"00240000048000009400000006020000002400005253413100040000010001009df7" +
			"e75ec7a084a12820d571ea9184386b479eb6e8dbf365106519bda8fc437cbf8e" +
				"fb3ce06212ac89e61cd0caa534537575c638a189caa4ac7b831474ceca5a" +
					"cf5018f2d4b41499044ce90e4f67bb0e8da4121882399b13aabaa6ff" +
						"46b4c24d5ec6141104028e1b5199e2ba1e35ad95bd50c1cf6ec5" +
							"c4e7b97c1d29c976e793";
}