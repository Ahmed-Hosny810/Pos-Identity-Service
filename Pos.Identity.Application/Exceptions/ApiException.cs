using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pos.Identity.Application.Exceptions
{
    public class ApiException : Exception
    {
        public ApiException() { }

        public ApiException(string message) : base(message) { }

        public ApiException(string message, params object[] args) :
            base(String.Format(CultureInfo.CurrentCulture, message, args))
        { }
    }
}
