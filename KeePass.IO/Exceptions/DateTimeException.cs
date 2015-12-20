using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePass.IO.Exceptions
{
    public class DateTimeFormatException : Exception
    {
        public DateTimeFormatException(string massage = null, Exception ex = null) : base(massage, ex) { }
    }
}
