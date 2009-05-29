#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Core
{
	/// <summary>Convert types into identifier and vice-versa. The purpose of this 
	/// interface is to support implicit cloud storage names for processed items.</summary>
	public interface ITypeMapperProvider
	{
		/// <summary>Gets the identifier associated to the specifed type.</summary>
		string GetIdentifier(Type type);

		/// <summary>Gets the type based on the identifier.</summary>
		Type GetType(string identifier);
	}
}
