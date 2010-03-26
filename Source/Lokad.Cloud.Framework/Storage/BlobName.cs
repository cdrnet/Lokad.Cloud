#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Lokad.Quality;

namespace Lokad.Cloud.Storage
{
	/// <summary>
	/// Base class for strong-typed hierarchical blob names.
	/// </summary>
	[Serializable, DataContract]
	public abstract class BlobName
	{
		class InheritanceComparer : IComparer<Type>
		{
			public int Compare(Type x, Type y)
			{
				if (x.Equals(y)) return 0;
				return x.IsSubclassOf(y) ? 1 : -1;
			}
		}

		/// <summary>Sortable pattern for date times.</summary>
		/// <remarks>Hyphens can be eventually used to refine further the iteration.</remarks>
		public const string DateFormatInBlobName = "yyyy-MM-dd-HH-mm-ss-ffff";

		static readonly Dictionary<Type, Func<string, object>> Parsers = new Dictionary<Type, Func<string, object>>();
		static readonly Dictionary<Type, Func<object, string>> Printers = new Dictionary<Type, Func<object, string>>();

		/// <summary>Name of the container (to be used as a short-hand while
		/// operating with the <see cref="IBlobStorageProvider"/>).</summary>
		/// <remarks>Do not introduce an extra field for the property as
		/// it would be incorporated in the blob name. Instead, just
		/// return a constant string.</remarks>
		public abstract string ContainerName { get; }

		static BlobName()
		{
			// adding overrides

			// Guid: does not have default converter
			Printers.Add(typeof(Guid), o => ((Guid)o).ToString("N"));
			Parsers.Add(typeof(Guid), s => new Guid(s));

			// DateTime: sortable ascending;
			// NOTE: not time zone safe, users have to deal with that temselves
			Printers.Add(typeof(DateTime),
				o => ((DateTime)o).ToString(DateFormatInBlobName, CultureInfo.InvariantCulture));
			Parsers.Add(typeof(DateTime),
				s => DateTime.ParseExact(s, DateFormatInBlobName, CultureInfo.InvariantCulture));

			// DateTimeOffset: sortable ascending;
			// time zone safe, but always returned with UTC/zero offset (comparisons can deal with that)
			Printers.Add(typeof(DateTimeOffset),
				o => ((DateTimeOffset)o).UtcDateTime.ToString(DateFormatInBlobName, CultureInfo.InvariantCulture));
			Parsers.Add(typeof(DateTimeOffset),
				s => new DateTimeOffset(DateTime.SpecifyKind(DateTime.ParseExact(s, DateFormatInBlobName, CultureInfo.InvariantCulture), DateTimeKind.Utc)));
		}

		public override string ToString()
		{
			// Invoke a Static Generic Method using Reflection
			var method = typeof(BlobName).GetMethod("Print", BindingFlags.Static | BindingFlags.Public);

			// Binding the method info to generic arguments
			method = method.MakeGenericMethod(new[] { GetType() });

			// Invoking the method and passing parameters
			// The null parameter is the object to call the method from. Since the method is static, pass null.
			return (string)method.Invoke(null, new object[] { this });
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
				Fields = typeof(T).GetFields()
					.Where(f => f.GetCustomAttributes(typeof(RankAttribute), true).Exists())
					// ordering always respect inheritance
					.GroupBy(f => f.DeclaringType)
					.OrderBy(g => g.Key, new InheritanceComparer())
					.Select(g =>
						g.OrderBy(f => ((RankAttribute)f.GetCustomAttributes(typeof(RankAttribute), true).First()).Index))
					.SelectMany(f => f)
					.ToArray();

				FirstCtor = typeof(T).GetConstructors(
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
			}

			public static string Print(T instance)
			{
				return PartialPrint(instance, Fields.Length);
			}

			public static string PartialPrint(T instance, int fieldCount)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < fieldCount; i++)
				{
					var info = Fields[i];
					var s = InternalPrint(info.GetValue(instance), info.FieldType);
					sb.Append(s);
					if (i < fieldCount - 1) sb.Append(Delimeter);
				}
				return sb.ToString();
			}

			public static T Parse(string value)
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException("value");
				}

				var split = value.Split(new[] { Delimeter }, StringSplitOptions.RemoveEmptyEntries);

				// In order to support parsing blob names also to blob name supper classes
				// in case of inheritance, we simply ignore supplement items in the name
				if (split.Length < Fields.Length)
				{
					throw new ArgumentException("Number of items in the string is invalid. Are you missing something?", "value");
				}

				var parameters = new object[Fields.Length];

				for (int i = 0; i < parameters.Length; i++)
				{
					parameters[i] = InternalParse(split[i], Fields[i].FieldType);
				}

				// Initialization through reflection (no assumption on constructors)
				var name = (T)FormatterServices.GetUninitializedObject(typeof (T));
				for (int i = 0; i < Fields.Length; i++)
				{
					Fields[i].SetValue(name, parameters[i]);
				}

				return name;
			}
		}

		/// <summary>Do not use directly, call <see cref="ToString"/> instead.</summary>
		public static string Print<T>(T instance) where T : BlobName
		{
			return ConverterTypeCache<T>.Print(instance);
		}

		public static string PartialPrint<T>(T instance, int fieldCount) where T : BlobName
		{
			return ConverterTypeCache<T>.PartialPrint(instance, fieldCount);
		}

		public static BlobNamePrefix<T> GetPrefix<T>(T instance, int fieldCount) where T : BlobName
		{
			return new BlobNamePrefix<T>(GetContainerName<T>(), PartialPrint(instance, fieldCount));
		}

		/// <summary>Parse a hierarchical blob name.</summary>
		public static T Parse<T>(string value) where T : BlobName
		{
			return ConverterTypeCache<T>.Parse(value);
		}

		/// <summary>Returns the <see cref="ContainerName"/> value without
		/// having an instance at hand.</summary>
		public static string GetContainerName<T>() where T : BlobName
		{
			// HACK: that's a heavy way of getting the thing done
			return ((T)FormatterServices.GetUninitializedObject(typeof(T))).ContainerName;
		}
	}
}