using System;

using SharpTox.Core;
using SQLite;

namespace Toxy.Tables
{
    public class ToxMessage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(ToxConstants.PublicKeySize * 2)]
        public string PublicKey { get; set; }

        public string Name { get; set; }

        [MaxLength(ToxConstants.MaxMessageLength)]
        public string Message { get; set; }

        public bool IsAction { get; set; }

        public bool IsSelf { get; set; }

        public DateTime Timestamp { get; set; }

        [MaxLength(ToxConstants.PublicKeySize * 2)]
        public string ProfilePublicKey { get; set; }
    }
}
