#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

// [vermorel] This attribute should be applicable to simple properties, 
// to indicate that NDCS should be used instead.

namespace Lokad.Cloud
{
	/// <summary>
	/// A type marked with this attribute does not support versioning and the type contract
	/// is considered immutable. If the type contract changes, deserialization of previously
	/// serialized instances will fail.
	/// </summary>
	/// <remarks>This attribute is incompatible with <see cref="ContractVersionAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
	// Usage is the same as [DataContractAttribute]
	public class TransientAttribute : Attribute
	{
		/// <summary>Initializes a new instance of the <see cref="TransientAttribute"/> class.</summary>
		public TransientAttribute() {
			ThrowOnDeserializationError = false;
		}

		/// <summary>Initializes a new instance of the <see cref="TransientAttribute"/> class.</summary>
		/// <param name="throwOnDeserializationError"><c>true</c> causes the framework to throw an
		/// exception when a serialized instance cannot be deserialized,
		/// <c>false</c> will cause the instance to be deleted instead.</param>
		public TransientAttribute(bool throwOnDeserializationError)
		{
			ThrowOnDeserializationError = throwOnDeserializationError;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the framework throws an exception when a serialized
		/// instance cannot be deserialized (<c>true</c>),
		/// or if the instance is deleted instead (<c>false</c>, default).
		/// </summary>
		public bool ThrowOnDeserializationError { get; set; }
	}
}
