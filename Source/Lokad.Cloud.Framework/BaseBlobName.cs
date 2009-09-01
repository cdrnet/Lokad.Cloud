#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Lokad.Cloud.Core;
using Lokad.Quality;

namespace Lokad.Cloud.Framework
{
	/// <summary>Base class to strong-type hierarchical blob names.</summary>
	[Serializable]
	public abstract class BaseBlobName
	{
		/// <summary>Sortable pattern for date times.</summary>
		/// <remarks>Hyphens can be eventually used to refine further the iteration.</remarks>
		public const string DateFormatInBlobName = "yyyy-MM-dd-HH-mm-ss";

		static readonly Dictionary<Type, Func<string, object>> Parsers = new Dictionary<Type, Func<string, object>>();
		static readonly Dictionary<Type, Func<object, string>> Printers = new Dictionary<Type, Func<object, string>>();

		/// <summary>Name of the container (to be used as a short-hand while
		/// operating with the <see cref="IBlobStorageProvider"/>).</summary>
		/// <remarks>Do not introduce an extra field for the property as
		/// it would be incorporated in the blob name. Instead, just
		/// return a constant string.</remarks>
		public abstract string ContainerName { get; }

		static BaseBlobName()
		{
			// adding overrides

			// GUID does not have default converter
			Parsers.Add(typeof(Guid), s => new Guid(s));

			Parsers.Add(typeof(DateTime), s => 
				DateTime.ParseExact(s, DateFormatInBlobName, CultureInfo.InvariantCulture));

			Printers.Add(typeof(DateTime), 
				o => ((DateTime)o).ToString(DateFormatInBlobName, CultureInfo.InvariantCulture));

			Printers.Add(typeof(Guid), o => ((Guid)o).ToString("N"));
		}

		public override string ToString()
		{
			// Invoke a Static Generic Method using Reflection
			var method = typeof (BaseBlobName).GetMethod("Print", BindingFlags.Static | BindingFlags.Public);

			// Binding the method info to generic arguments
			method = method.MakeGenericMethod(new[] { GetType() });

			// Invoking the method and passing parameters
			// The null parameter is the object to call the method from. Since the method is static, pass null.
			return (string) method.Invoke(null, new object[] { this });
		}

		static object InternalParse(string value, Type type)
		{
			var func = Parsers.GetValue(type, s => Convert.ChangeType(s, type));
			return func(value);
		}


		static string InternalPrint(object value, Type type)
		{
			var func = Printers.GetValue(type, o => o.ToString());
			return func(value);
		}

		[UsedImplicitly]
		class ConverterTypeCache<T>
		{
			static readonly FieldInfo[] Fields;
			const string Delimeter = "/";

			static readonly ConstructorInfo FirstCtor;

			static ConverterTypeCache()
			{
				// HACK: optimize this to IL code, if needed
				// NB: this approach could be used to generate F# style objects!
				Fields = typeof(T).GetFields();
				FirstCtor = typeof(T).GetConstructors().First();
			}

			public static string Print(T instance)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < Fields.Length; i++)
				{
					var info = Fields[i];
					var s = InternalPrint(info.GetValue(instance), info.FieldType);
					sb.Append(s);
					if(i < Fields.Length - 1) sb.Append(Delimeter);
				}
				return sb.ToString();
			}

			public static T Parse(string value)
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException("value");

				var split = value.Split(new[] { Delimeter }, StringSplitOptions.RemoveEmptyEntries);

				if (split.Length != Fields.Length)
					throw new ArgumentException("Number of items in the string is invalid. Are you missing something?", "value");

				var parameters = new object[Fields.Length];

				for (int i = 0; i < parameters.Length; i++)
				{
					parameters[i] = InternalParse(split[i], Fields[i].FieldType);
				}

				return (T)FirstCtor.Invoke(parameters);
			}
		}

		/// <summary>Do not use directly, call <see cref="ToString"/> instead.</summary>
		public static string Print<T>(T instance)
		{
			return ConverterTypeCache<T>.Print(instance);
		}

		/// <summary>Parse a hierarchical blob name.</summary>
		public static T Parse<T>(string value)
		{
			return ConverterTypeCache<T>.Parse(value);
		}
	}

}
