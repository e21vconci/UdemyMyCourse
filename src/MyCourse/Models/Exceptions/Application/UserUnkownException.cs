using System;

namespace MyCourse.Models.Exceptions.Application
{
    public class UserUnkownException : Exception
    {
        public UserUnkownException() : base($"A known user is required for this operation")
        {
        }        
    }
}