#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Threading;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Core
{
	/// <summary>Organize the executions of the services.</summary>
	public class ServiceScheduler
	{
		readonly object _sync = new object();

		readonly CloudService[] _services;
		readonly ILog _logger;

		bool _isStopRequested;
		bool _isStopped;

		public ServiceScheduler(CloudService[] services, ILog logger)
		{
			_services = services;
			_logger = logger;
		}

		public void Start()
		{
			while(!_isStopRequested)
			{
				// TODO: need to implement here a naive bandit pattern to run services

				throw new NotImplementedException();
			}

			lock(_sync)
			{
				_isStopped = true;
				Monitor.Pulse(_sync);
			}
		}

		public void Stop()
		{
			_isStopRequested = true;

			lock(_sync)
			{
				while(!_isStopped)
				{
					Monitor.Wait(_sync);
				}
			}
		}
	}
}
