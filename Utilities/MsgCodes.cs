using System;
using System.Text;

namespace Utilities
{
    public class MsgCodes
    {
        public static byte[] WrongAuth { get => BitConverter.GetBytes(401); }
        public static byte[] AuthOk { get => BitConverter.GetBytes(202); }
        public static byte[] Seperator { get => BitConverter.GetBytes(31); }
    }
}
