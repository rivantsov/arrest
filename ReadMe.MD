# ARREST - .NET REST client

**Arrest** is a client-side component for communicating with REST APIs and services. It is a wrapper around HttpClient with a number of convenient methods for automatically serializing/deserializing typed data objects. 

Examples: 
```c#
  var client = new RestClient("http://www.bookstore.com");
  var someBooks = await client.GetAsync<IList<Book>>("api/books?take={0}", 10);
  var theBook = client.Get<Book>("api/books/{0}", someBookId); //sync version
  var myReview = new BookReview() { ... };
  var myReviewBack = await client.PostAsync<BookReview, BookReview>(myReview, "api/user/reviews");
  
```
The client handles HTTP status codes returned by the service (throws exceptions on BadRequest and Server errors).
Supports Json and Xml content serialization. Supports sync and async operations. 
See RestCallTests.cs class for examples of use. 


