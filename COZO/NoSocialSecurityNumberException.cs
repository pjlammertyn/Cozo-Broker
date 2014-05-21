using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COZO
{
    public class NoSocialSecurityNumberException : Exception
    {
        public NoSocialSecurityNumberException(string message) : base(message)
        {
        }
    }
}
