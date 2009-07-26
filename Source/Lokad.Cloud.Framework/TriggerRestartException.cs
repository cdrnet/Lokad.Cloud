#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

// IDEA: the message of the exception could be logged in to the cloud logs.
// (issue: how to avoid N identical messages to be logged through all workers)

namespace Lokad.Cloud.Framework
{
	///<summary>Throw this exception in order to force a worker restart.</summary>
	public class TriggerRestartException : ApplicationException
	{
	}
}
