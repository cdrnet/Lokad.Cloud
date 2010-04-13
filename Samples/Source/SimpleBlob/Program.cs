#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Runtime.Serialization;
using Lokad.Cloud;
using Lokad.Cloud.Storage;

namespace SimpleBlob
{
    [DataContract]
    class Book
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Author { get; set; }
    }

    class BookName : BlobReference<Book>
    {
        public override string ContainerName
        {
            get { return "books"; } // default container for 'Book' entities
        }

        [Rank(0)] public string Publisher { get; set;}

        [Rank(1)] public int BookId { get; set;}
    }

    class Program
    {
        static void Main(string[] args)
        {            
            // TODO: change your connection string here
            var providers = Standalone.CreateProviders("DefaultEndpointsProtocol=https;AccountName=;AccountKey=");
            var blobStorage = providers.BlobStorage;

            var potterBook = new Book { Author = "J. K. Rowling", Title = "Harry Potter" };
            // Resulting blob name is: Bloomsbury Publishing/1
            var potterRef = new BookName {Publisher = "Bloomsbury Publishing", BookId = 1};

            var poemsBook = new Book { Author = "John Keats", Title = "Complete Poems" };
            // Resulting blob name is: Harvard University Press/1
            var poemsRef = new BookName {Publisher = "Harvard University Press", BookId = 2};
            

            // writing entities to the storage
            blobStorage.PutBlob(potterRef, potterBook);
            blobStorage.PutBlob(poemsRef, poemsBook);

            // retriving all entities from 'Bloomsbury Publishing'

            //foreach(var book in blobStorage.List("Bloomsbury Publishing"))
            {
                // TODO: to be completed
            }

        }
    }
}
