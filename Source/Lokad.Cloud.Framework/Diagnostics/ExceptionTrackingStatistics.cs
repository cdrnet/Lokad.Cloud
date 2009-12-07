#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Diagnostics.Persist;

namespace Lokad.Cloud.Diagnostics
{
	[Serializable]
	[DataContract]
	public class ExceptionTrackingStatistics
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public ExceptionData[] Statistics { get; set; }
	}
}
