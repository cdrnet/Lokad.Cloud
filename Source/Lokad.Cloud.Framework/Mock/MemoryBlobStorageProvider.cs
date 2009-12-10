#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Threading;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Blob Storage.</summary>
	/// <remarks>
	/// All the methods of <see cref="MemoryBlobStorageProvider"/> are thread-safe.
	/// </remarks>
	public class MemoryBlobStorageProvider : IBlobStorageProvider
	{
		/// <summary> Containers Property.</summary>
		Dictionary<string, MockContainer> Containers { get { return _containers;} }
		readonly Dictionary<string, MockContainer> _containers;
		
		/// <summary>naive global lock to make methods thread-safe.</summary>
		readonly object _syncRoot;

		public MemoryBlobStorageProvider()
		{
			_containers = new Dictionary<string, MockContainer>();
			_syncRoot = new object();
		}

		public bool CreateContainer(string containerName)
		{
			lock (_syncRoot)
			{
				if (Containers.Keys.Contains(containerName))
				{
					return false;
				}
				
				Containers.Add(containerName, new MockContainer());
				return true;
			}	
		}

		public bool DeleteContainer(string containerName)
		{
			lock (_syncRoot)
			{
				if (!Containers.Keys.Contains(containerName))
				{
					return false;
				}

				Containers.Remove(containerName);
				return true;
			}
		}

		public void PutBlob<T>(string containerName, string blobName, T item)
		{
			PutBlob<T>(containerName, blobName,item, true);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
		{
			string ignored;
			return PutBlob<T>(containerName, blobName, item, overwrite, out ignored);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag)
		{
			return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag);
		}

		public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag)
		{
			lock(_syncRoot)
			{
				etag = null;
				if(Containers.ContainsKey(containerName))
				{
					if(Containers[containerName].BlobNames.Contains(blobName))
					{
						if(!overwrite)
						{
							return false;
						}

						Containers[containerName].SetBlob(blobName, item);
						etag = Containers[containerName].BlobsEtag[blobName];
						return true;
					}

					Containers[containerName].AddBlob(blobName, item);
					etag = Containers[containerName].BlobsEtag[blobName];
					return true;
				}

				Containers.Add(containerName, new MockContainer());
				Containers[containerName].AddBlob(blobName, item);
				etag = Containers[containerName].BlobsEtag[blobName];
				return true;
			}
		}

		public Maybe<T> GetBlob<T>(string containerName, string blobName)
		{
			string ignoredEtag;
			return GetBlob<T>(containerName, blobName, out ignoredEtag);
		}

		public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag)
		{
			return GetBlob(containerName, blobName, typeof (T), out etag)
				.Convert(o => (T) o, Maybe<T>.Empty);
		}

		public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag)
		{
			lock(_syncRoot)
			{
				if(!Containers.ContainsKey(containerName) ||
					 !Containers[containerName].BlobNames.Contains(blobName))
				{
					etag = null;
					return Maybe<object>.Empty;
				}

				etag = Containers[containerName].BlobsEtag[blobName];
				return Containers[containerName].GetBlob(blobName);
			}
		}

		public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags)
		{
			// Copy-paste from BlobStorageProvider.cs

			var tempResult = blobNames.SelectInParallel(blobName =>
			{
				string etag;
				Maybe<T> blob = GetBlob<T>(containerName, blobName, out etag);
				return new Tuple<Maybe<T>, string>(blob, etag);
			}, blobNames.Length);

			etags = new string[blobNames.Length];
			var result = new Maybe<T>[blobNames.Length];

			for(int i = 0; i < tempResult.Length; i++)
			{
				result[i] = tempResult[i].Item1;
				etags[i] = tempResult[i].Item2;
			}

			return result;
		}

		public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag)
		{
			lock(_syncRoot)
			{
				string currentEtag = GetBlobEtag(containerName, blobName);

				if(currentEtag == oldEtag)
				{
					newEtag = null;
					return Maybe<T>.Empty;
				}
				
				newEtag = currentEtag;
				return GetBlob<T>(containerName, blobName).Value;
			}
		}

		public string GetBlobEtag(string containerName, string blobName)
		{
			lock (_syncRoot)
			{
				return (Containers.ContainsKey(containerName) && Containers[containerName].BlobNames.Contains(blobName))
					? Containers[containerName].BlobsEtag[blobName]
					: null;
			}
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			lock (_syncRoot)
			{
				Maybe<T> input;
				if (Containers.ContainsKey(containerName) )
				{
					if (Containers[containerName].BlobNames.Contains(blobName))
					{
						var blobData = Containers[containerName].GetBlob(blobName);
						input = blobData == null ? Maybe<T>.Empty : (T) blobData;
					}
					else
					{
						input = Maybe<T>.Empty;
					}
				}
				else
				{
					Containers.Add(containerName, new MockContainer());
					input = Maybe<T>.Empty;
				}

				// updating the item
				result = updater(input);

				if (!result.IsSuccess)
				{
					return false;
				}
				Containers[containerName].SetBlob(blobName, result.Value);
				return true;
			}
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater, out T result)
		{
			Result<T> rresult;
			var flag = UpdateIfNotModified(containerName, blobName, x => Result.CreateSuccess(updater(x)), out rresult);

			result = rresult.Value;
			return flag;
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater)
		{
			Result<T> ignored;
			return UpdateIfNotModified(containerName, blobName, updater, out ignored);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater)
		{
			return UpdateIfNotModified<T>(containerName, blobName, x => Result.CreateSuccess(updater(x)));
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			lock (_syncRoot)
			{
				if (!Containers.Keys.Contains(containerName) || !Containers[containerName].BlobNames.Contains(blobName))
				{
					return false;
				}

				Containers[containerName].RemoveBlob(blobName);
				return true;
			}
		}

		public IEnumerable<string> List(string containerName, string prefix)
		{
			lock (_syncRoot)
			{
				if (!Containers.Keys.Contains(containerName))
				{
					throw new InvalidOperationException("ContainerName : " + containerName + " does not exist.");
				}

				return Containers[containerName].BlobNames.Where(name => name.StartsWith(prefix));
			}
		}

		class MockContainer
		{
			readonly Dictionary<string, object> _blobSet;
			readonly Dictionary<string, string> _blobsEtag;

			public string[] BlobNames { get { return _blobSet.Keys.ToArray(); } }

			public Dictionary<string, string> BlobsEtag { get { return _blobsEtag; } }

			public MockContainer()
			{
				_blobSet = new Dictionary<string, object>();
				_blobsEtag = new Dictionary<string, string>();
			}

			public void SetBlob(string blobName, object item)
			{
				_blobSet[blobName] = item;
				_blobsEtag[blobName] = Guid.NewGuid().ToString();
			}

			public object GetBlob(string blobName)
			{
				return _blobSet[blobName];
			}

			public void AddBlob(string blobName, object item)
			{
				_blobSet.Add(blobName, item);
				_blobsEtag.Add(blobName, Guid.NewGuid().ToString());
			}

			public void RemoveBlob(string blobName)
			{
				_blobSet.Remove(blobName);
				_blobsEtag.Remove(blobName);
			}
		}
	}
}
