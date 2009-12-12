#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Specialized;

// TODO: [Vermorel] This class will be discarded in favor of an operation-behavior override of DCS toward NDCS.

namespace Lokad.Cloud
{
	/// <summary>Contains information about a type.</summary>
	internal class TypeInformation
	{
		const string IsTransientKey = "IsTransient";
		const string ThrownOnDeserializationErrorKey = "ThrowOnDeserializationError";

		/// <summary>Gets a value indicating whether the type is transient.</summary>
		public bool IsTransient { get; private set; }

		/// <summary>If <see cref="IsTransient"/> is <c>true</c>, gets the behavior on deserialization error.</summary>
		public bool? ThrowOnDeserializationError { get; private set; }

		/// <summary>
		/// Gets the type information for a type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The type information.</returns>
		public static TypeInformation GetInformation(Type type)
		{
			var result = new TypeInformation { ThrowOnDeserializationError = null };

			var myType = type;

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				myType = type.GetGenericArguments()[0];
			}

			var transient = myType.GetAttribute<TransientAttribute>(false);

			if (transient != null)
			{
				result.IsTransient = true;
				result.ThrowOnDeserializationError = transient.ThrowOnDeserializationError;
			}

			return result;
		}

		/// <summary>
		/// Saves the type information in blob metadata.
		/// </summary>
		/// <param name="metadata">The metadata to act on.</param>
		public void SaveInBlobMetadata(NameValueCollection metadata)
		{
			metadata.Remove(ThrownOnDeserializationErrorKey);
			metadata[IsTransientKey] = IsTransient.ToString();

			if(IsTransient)
			{
				metadata[ThrownOnDeserializationErrorKey] = ThrowOnDeserializationError.Value.ToString();
			}
		}

		/// <summary>
		/// Loads the type information from blob metadata.
		/// </summary>
		/// <param name="metadata">The metadata to act on.</param>
		/// <returns>The type information, if available, <c>null</c> otherwise.</returns>
		public static TypeInformation LoadFromBlobMetadata(NameValueCollection metadata)
		{
			var transientString = metadata[IsTransientKey];
			if(string.IsNullOrEmpty(transientString)) return null;

			var isTransient = bool.Parse(transientString);
			var result = new TypeInformation { IsTransient = isTransient };

			if(isTransient)
			{
				var throwSetting = metadata[ThrownOnDeserializationErrorKey];
				if(string.IsNullOrEmpty(throwSetting)) return null;
				result.ThrowOnDeserializationError = bool.Parse(metadata[ThrownOnDeserializationErrorKey]);
			}

			return result;
		}

		/// <summary>Determines whether the specified <see cref="T:System.Object"/>
		/// is equal to the current <see cref="T:System.Object"/>.</summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.</returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
		public override bool Equals(object obj)
		{
			if(ReferenceEquals(obj, null)) throw new NullReferenceException();

			var realType = obj as TypeInformation;

			return !ReferenceEquals(realType, null) && Equals(realType);
		}

		/// <summary>
		/// Equalses the specified info.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <returns><c>true</c> if <paramref name="info"/> is equal to the current instance, <c>false</c> otherwise.</returns>
		public bool Equals(TypeInformation info)
		{
			if(ReferenceEquals(info, null)) throw new NullReferenceException();

			return
				info.IsTransient == IsTransient &&
				info.ThrowOnDeserializationError == ThrowOnDeserializationError;
		}

		/// <summary>Serves as a hash function for a particular type.</summary>
		/// <returns>A hash code for the current <see cref="T:System.Object"/>.</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
