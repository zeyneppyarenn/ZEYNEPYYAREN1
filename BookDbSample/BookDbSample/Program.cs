// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace BookStore
{


    public class BookStoreContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Publisher> Publishers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BookDb;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>()
                .HasOne(p => p.Publisher)
                .WithMany(b => b.Books)
                .HasForeignKey(p => p.PublisherId);
        }
    }

    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int PublisherId { get; set; }
        public Publisher Publisher { get; set; }
    }

    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Book> Books { get; set; }
    }

    public class BookStore
    {
        private readonly BookStoreContext _context;
        private static BookStore _instance;

        private BookStore()
        {
            _context = new BookStoreContext();
        }

        public static BookStore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new BookStore();
            }

            return _instance;
        }

        public void AddBook(string title, string author, string publisherName)
        {
            var publisher = _context.Publishers.FirstOrDefault(p => p.Name == publisherName);

            if (publisher == null)
            {
                publisher = new Publisher { Name = publisherName };
            }

            var book = new Book { Title = title, Author = author, Publisher = publisher };

            _context.Books.Add(book);
            _context.SaveChanges();
        }

        public void UpdateBook(int bookId, string title, string author, string publisherName)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);

            if (book == null)
            {
                Console.WriteLine($"Book with Id {bookId} not found.");
                return;
            }

            var publisher = _context.Publishers.FirstOrDefault(p => p.Name == publisherName);

            if (publisher == null)
            {
                publisher = new Publisher { Name = publisherName };
            }

            book.Title = title;
            book.Author = author;
            book.Publisher = publisher;

            _context.SaveChanges();
        }

        public Book GetBookById(int bookId)
        {
            return _context.Books.Include(b => b.Publisher).FirstOrDefault(b => b.Id == bookId);
        }

        public List<Book> GetAllBooks()
        {
            return _context.Books.Include(b => b.Publisher).ToList();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Book Store Application\n");

            using (var db = new BookStoreContext())
            {
                // Seed data
                if (!db.Publishers.Any())
                {
                    db.Publishers.Add(new Publisher { Name = "Publisher 1" });
                    db.Publishers.Add(new Publisher { Name = "Publisher 2" });
                    db.SaveChanges();
                }

                if (!db.Books.Any())
                {
                    var publisher1 = db.Publishers.First();
                    var publisher2 = db.Publishers.Skip(1).First();

                    db.Books.Add(new Book { Title = "Book 1", Author="Author 1",PublisherId = publisher1.Id });
                    db.Books.Add(new Book { Title = "Book 2", Author = "Author 1",PublisherId = publisher1.Id });
                    db.Books.Add(new Book { Title = "Book 3", Author = "Author 2",PublisherId = publisher2.Id });
                    db.SaveChanges();
                }

                while (true)
                {
                    Console.WriteLine("\nPlease select an action:");
                    Console.WriteLine("1- Insert Book");
                    Console.WriteLine("2- Update Book");
                    Console.WriteLine("3- Get Book by Id");
                    Console.WriteLine("4- Get All Books");
                    Console.WriteLine("5- Insert Publisher");
                    Console.WriteLine("0- Exit");

                    var choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("Please enter book title:");
                            var bookTitle = Console.ReadLine();

                            Console.WriteLine("Please enter publisher name:");
                            var publisherName = Console.ReadLine();

                            var publisher = db.Publishers.FirstOrDefault(p => p.Name == publisherName);

                            if (publisher == null)
                            {
                                publisher = new Publisher { Name = publisherName };
                                db.Publishers.Add(publisher);
                                db.SaveChanges();
                            }

                            var book = new Book { Title = bookTitle, PublisherId = publisher.Id };
                            db.Books.Add(book);
                            db.SaveChanges();

                            Console.WriteLine($"Book {book.Title} has been added with Publisher {publisher.Name}");
                            break;

                        case "2":
                            Console.WriteLine("Please enter book id:");
                            var bookIdStr = Console.ReadLine();
                            if (!int.TryParse(bookIdStr, out var bookId))
                            {
                                Console.WriteLine("Invalid book id");
                                break;
                            }

                            var bookToUpdate = db.Books.Find(bookId);
                            if (bookToUpdate == null)
                            {
                                Console.WriteLine("Book not found");
                                break;
                            }

                            Console.WriteLine("Please enter book title:");
                            bookTitle = Console.ReadLine();
                            bookToUpdate.Title = bookTitle;

                            Console.WriteLine("Please enter publisher name:");
                            publisherName = Console.ReadLine();

                            publisher = db.Publishers.FirstOrDefault(p => p.Name == publisherName);

                            if (publisher == null)
                            {
                                publisher = new Publisher { Name = publisherName };
                                db.Publishers.Add(publisher);
                                db.SaveChanges();
                            }

                            bookToUpdate.PublisherId = publisher.Id;
                            db.SaveChanges();

                            Console.WriteLine($"Book {bookToUpdate.Title} has been updated with Publisher {publisher.Name}");
                            break;

                        case "3": // Get book by Id
                            Console.Write("Enter book Id: ");
                             bookId = Convert.ToInt32(Console.ReadLine());

                            book = db.Books.Find(bookId);

                            if (book != null)
                            {
                                Console.WriteLine($"Title: {book.Title}\nAuthor: {book.Author}\nPublisher: {book.Publisher.Name}\n");
                            }
                            else
                            {
                                Console.WriteLine("Book not found");
                            }
                            break;

                        case "4": // Get all books
                            var books = db.Books.Include(b => b.Publisher).ToList();

                            if (books.Any())
                            {
                                foreach (var bk in books)
                                {
                                    Console.WriteLine($"Id: {bk.Id}\nTitle: {bk.Title}\nAuthor: {bk.Author}\nPublisher: {bk.Publisher.Name}\n");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No books found");
                            }
                            break;
                    }
                }
            }

        }
    }
    }
