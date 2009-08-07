#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using Lokad.Cloud.Framework;

namespace PingPong
{
	/// <summary>Retrieving messages from 'ping' and put them in 'pong'.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = "ping")]
	public class PingPongService : QueueService<double>
	{
		protected override void Start(IEnumerable<double> messages)
		{
			foreach(var x in messages)
			{
				var y = x * x; // square operation
				Put(y, "pong");
				
				// Optionaly, we could manually delete incoming messages,
				// but here, we let the framework deal with that.
 
				// Delete(x);
			}
		}
	}
}
