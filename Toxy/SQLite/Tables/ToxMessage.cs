using System;

using SharpTox.Core;
using SQLite;

namespace Toxy.Tables
{
    public class ToxMessage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(ToxConstants.ClientIdSize * 2)]
        public string PublicKey { get; set; }

        [MaxLength(ToxConstants.MaxMessageLength)]
        public string Message { get; set; }

        public bool IsAction { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
