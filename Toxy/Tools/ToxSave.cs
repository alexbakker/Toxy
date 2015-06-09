using SharpTox.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Toxy.Tools
{
    public class ToxSave
    {
        public ToxId Id { get; set; }
        public string Name { get; set; }
        public string StatusMessage { get; set; }
        public ToxUserStatus Status { get; set; }

        private ToxSave() { }

        public static ToxSave FromDisk(string filename)
        {
            try
            {
                ToxSave save = new ToxSave();

                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    stream.Position += sizeof(uint);

                    using (var reader = new BinaryReader(stream))
                    {
                        uint cookie = reader.ReadUInt32();
                        if (cookie != Constants.Cookie)
                            throw new Exception("Invalid cookie, this doesn't look like a tox profile");

                        for (; ; )
                        {
                            long left = reader.BaseStream.Length - reader.BaseStream.Position;
                            if (left < sizeof(uint))
                                break;

                            uint length = reader.ReadUInt32();
                            left = reader.BaseStream.Length - reader.BaseStream.Position;
                            if (left < length)
                                break;

                            var type = ReadStateType(reader);

                            switch (type)
                            {
                                case StateType.NospamKeys:
                                    uint nospam = reader.ReadUInt32();
                                    byte[] publicKey = reader.ReadBytes(ToxConstants.PublicKeySize);

                                    save.Id = new ToxId(publicKey, nospam);
                                    stream.Position += ToxConstants.SecretKeySize; //skip the private key, we don't want to show that here
                                    break;
                                case StateType.Dht:
                                    stream.Position += length; //skip this for now
                                    break;
                                case StateType.Friends:
                                    stream.Position += length;
                                    break;
                                case StateType.Name:
                                    save.Name = Encoding.UTF8.GetString(reader.ReadBytes((int)length));
                                    break;
                                case StateType.StatusMessage:
                                    save.StatusMessage = Encoding.UTF8.GetString(reader.ReadBytes((int)length));
                                    break;
                                case StateType.Status:
                                    save.Status = (ToxUserStatus)reader.ReadByte();
                                    break;
                                case StateType.TcpRelay:
                                    stream.Position += length; //skip this for now
                                    break;
                                case StateType.PathNode:
                                    stream.Position += length; //skip this for now
                                    break;
                                case StateType.Corrupt:
                                    throw new Exception("This Tox save file is probably corrupt. The displayed information may be incomplete/incorrect");
                                default:
                                    break;
                            }
                        }
                    }
                }
                return save;
            }
            catch { return null; }
        }

        private static StateType ReadStateType(BinaryReader reader)
        {
            uint type = reader.ReadUInt32();
            if (type >> 16 != Constants.CookieInner)
                return StateType.Corrupt;

            return (StateType)type;
        }

        private enum StateType : ushort
        {
            NospamKeys = 1,
            Dht = 2,
            Friends = 3,
            Name = 4,
            StatusMessage = 5,
            Status = 6,
            TcpRelay = 10,
            PathNode = 11,
            Corrupt = 50
        }

        private class Constants
        {
            public const uint Cookie = 0x15ed1b1f;
            public const uint CookieInner = 0x01ce;
        }
    }
}
