using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Storage.Documents
{
	public interface IDocumentName<TDocumentType> : IDocumentName
	{
	}

	public interface IDocumentName
	{
		string PartitionName { get; }
		string DocumentName { get; }
	}
}
