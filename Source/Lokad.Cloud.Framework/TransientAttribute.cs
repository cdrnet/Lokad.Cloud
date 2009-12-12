#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

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

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// Code copied from Aaron Skonnard blog
	/// http://www.pluralsight.com/community/blogs/aaron/archive/2006/04/21/22284.aspx
	/// </remarks>
	public class NetDataContractFormat : Attribute, IOperationBehavior
	{
		public void AddBindingParameters(OperationDescription description, BindingParameterCollection parameters)
		{
		}

		public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
		{
			ReplaceDataContractSerializerOperationBehavior(description);
		}

		public void ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
		{
			ReplaceDataContractSerializerOperationBehavior(description);
		}

		public void Validate(OperationDescription description)
		{
		}

		private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description)
		{
			var dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();

			if (dcsOperationBehavior != null)
			{
				description.Behaviors.Remove(dcsOperationBehavior);
				description.Behaviors.Add(new NetDataContractSerializerOperationBehavior(description));
			}
		}

		public class NetDataContractSerializerOperationBehavior : DataContractSerializerOperationBehavior
		{
			public NetDataContractSerializerOperationBehavior(OperationDescription operationDescription) :
				base(operationDescription) { }

			public override XmlObjectSerializer CreateSerializer(
				Type type, string name, string ns, IList<Type> knownTypes)
			{
				return new NetDataContractSerializer();
			}

			public override XmlObjectSerializer CreateSerializer(Type type,
				XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
			{
				return new NetDataContractSerializer();
			}
		}
	}
}
