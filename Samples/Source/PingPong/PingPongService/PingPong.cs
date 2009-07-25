#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using Lokad.Cloud.Framework;

namespace PingPong
{
	[QueueServiceSettings(AutoStart = true, QueueName = "ping")]
	public class PingPongService : QueueService<double>
	{
		public PingPongService(ProvidersForCloudStorage providers) : base(providers)
		{
		}

		protected override void Start(IEnumerable<double> messages)
		{
			foreach(var x in messages)
			{
				var y = x * x; // square operation
				Put(new[]{y}, "pong");
			}
		}
	}
}
