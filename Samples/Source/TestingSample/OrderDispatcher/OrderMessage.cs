#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestingSample
{
	/// <summary>Contains order data.</summary>
	[Serializable] // Please note the SerializableAttribute
	public class OrderMessage
	{
		/// <summary>The date/time the order was placed on.</summary>
		public DateTimeOffset OrderPlacedOn { get; set; }

		/// <summary>The supplier ID.</summary>
		public string SupplierId { get; set; }

		/// <summary>The IDs of the parts to order.</summary>
		public IList<string> Parts { get; set; }
	}
}
