#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Tables;

namespace SimpleTable
{
    [DataContract]
    class Book
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Author { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TODO: change your connection string here
            var tableStorage = CloudStorage
                .ForAzureConnectionString("DefaultEndpointsProtocol=https;AccountName=;AccountKey=")
                .BuildTableStorage();
            
            // 'books' is the name of the table
            var books = new CloudTable<Book>(tableStorage, "books");

            var potterBook = new Book { Author = "J. K. Rowling", Title = "Harry Potter" };
            var poemsBook = new Book { Author = "John Keats", Title = "Complete Poems" };

            // inserting (or updating record in Table Storage)
            books.Upsert(new[]
                {
                    new CloudEntity<Book> {PartitionKey = "UK", RowKey = "potter", Value = potterBook},
                    new CloudEntity<Book> {PartitionKey = "UK", RowKey = "poems", Value = poemsBook}
                });

            // reading from table
            foreach(var entity in books.Get())
            {
                Console.WriteLine("{0} by {1} in partition '{2}' and rowkey '{3}'",
                    entity.Value.Title, entity.Value.Author, entity.PartitionKey, entity.RowKey);
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }
    }
}
