#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace Lokad.Cloud.Web
{
	/// <summary>Allows to inspect assemblies in a separate AppDomain.</summary>
	/// <remarks>Use a <c>using</c> block so that <see cref="M:Dispose"/> is called.</remarks>
	public class AssemblyInspector : IDisposable
	{
		AppDomain _sandbox;
		bool _disposed = false;
		AssemblyWrapper _asmWrapper = null;

		/// <summary>Initializes a new instance of the <see cref="T:AssemblyInspector"/> class.</summary>
		/// <param name="assemblyBytes">The assembly bytes.</param>
		public AssemblyInspector(byte[] assemblyBytes)
		{
			_sandbox = AppDomain.CreateDomain("AsmInspector", null, AppDomain.CurrentDomain.SetupInformation);
			_asmWrapper = _sandbox.CreateInstanceAndUnwrap(
				Assembly.GetExecutingAssembly().FullName,
				typeof(AssemblyWrapper).FullName,
				false, BindingFlags.CreateInstance, null,
				new object[] { assemblyBytes },
				null, new object[0], null) as AssemblyWrapper;
		}

		/// <summary>Gets the assembly version.</summary>
		public string AssemblyVersion
		{
			get
			{
				if(_disposed) throw new ObjectDisposedException("AssemblyInspector");
				return _asmWrapper.Version;
			}
		}

		/// <summary>Disposes of the object and the wrapped <see cref="AppDomain"/>.</summary>
		public void Dispose()
		{
			if(!_disposed)
			{
				AppDomain.Unload(_sandbox);
				_disposed = true;
			}
		}

	}

	/// <summary>Wraps an assembly (to be used from within a secondary AppDomain).</summary>
	public class AssemblyWrapper : MarshalByRefObject
	{
		Assembly _wrappedAssembly = null;

		/// <summary>Initializes a new instance of the <see cref="T:AssemblyWrapper"/> class.</summary>
		/// <param name="assemblyBytes">The assembly bytes.</param>
		public AssemblyWrapper(byte[] assemblyBytes)
		{
			_wrappedAssembly = Assembly.Load(assemblyBytes);
		}

		/// <summary>Gets the assembly version.</summary>
		public string Version
		{
			get { return _wrappedAssembly.GetName().Version.ToString(); }
		}
	}

}
