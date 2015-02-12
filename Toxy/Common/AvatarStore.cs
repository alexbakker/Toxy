using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using SharpTox.Core;
using Toxy.Extenstions;

namespace Toxy.Common
{
    public class AvatarStore
    {
        private string _dir;
        public string Dir
        {
            get { return Path.Combine(_dir, "avatars"); }
        }

        public AvatarStore(string dir)
        {
            _dir = dir;

            if (!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);
        }

        public bool Save(byte[] img, ToxKey publicKey)
        {
            try
            {
                File.WriteAllBytes(Path.Combine(Dir, publicKey.GetString() + ".png"), img);
                return true;
            }
            catch { return false; }
        }

        private byte[] LoadBytes(ToxKey publicKey)
        {
            try
            {
                string avatarFilename = Path.Combine(Dir, publicKey.GetString() + ".png");
                if (File.Exists(avatarFilename))
                {
                    byte[] bytes = File.ReadAllBytes(avatarFilename);
                    if (bytes.Length > 0)
                        return bytes;
                }

                return null;
            }
            catch { return null; }
        }

        public BitmapImage Load(ToxKey publicKey, out byte[] result)
        {
            byte[] bytes = LoadBytes(publicKey);
            if (bytes == null)
            {
                result = null;
                return null;
            }

            MemoryStream stream = new MemoryStream(bytes);

            using (Bitmap bmp = new Bitmap(stream))
            {
                result = bytes;
                return bmp.ToBitmapImage(ImageFormat.Png);
            }
        }

        public bool Delete(ToxKey publicKey)
        {
            string avatarFilename = Path.Combine(Dir, publicKey.GetString() + ".png");
            if (File.Exists(avatarFilename))
            {
                try { File.Delete(avatarFilename); }
                catch { return false; }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Contains(ToxKey publicKey)
        {
            return File.Exists(Path.Combine(Dir, publicKey.GetString() + ".png"));
        }
    }
}
