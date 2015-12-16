using System;

namespace MyAnimeList.API
{
    public class ServiceException : Exception
    {
        public int HttpStatusCode { get; set; }

        public ServiceException() { }
        public ServiceException(string message) : base(message) { }
        public ServiceException(string message, Exception inner) : base(message, inner) { }

    }
}