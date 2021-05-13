using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Exceptions.Application
{
    public class SendException : Exception
    {
        public SendException() : base($"Couldn't send the message")
        {
        }
    }
}
