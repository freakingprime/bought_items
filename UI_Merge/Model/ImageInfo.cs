using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems.UI_Merge.Model
{
    public class ImageInfo : IEquatable<ImageInfo>, IComparable<ImageInfo>, IEqualityComparer<ImageInfo>
    {
        public ImageInfo()
        {

        }

        public string URL = string.Empty;
        public string LocalImagePath = string.Empty;

        public bool Equals(ImageInfo other)
        {
            return URL.Equals(other.URL) && LocalImagePath.Equals(other.LocalImagePath);
        }

        public int CompareTo(ImageInfo other)
        {
            int t = URL.CompareTo(other.URL);
            if (t == 0)
            {
                t = LocalImagePath.CompareTo(other.LocalImagePath);
            }
            return t;
        }

        public bool Equals(ImageInfo x, ImageInfo y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(ImageInfo obj)
        {
            return LocalImagePath.GetHashCode();
        }

        public override string ToString()
        {
            return URL + "|" + LocalImagePath;
        }

        public static ImageInfo FromString(string s)
        {
            ImageInfo result = null;
            string[] split = s.Split('|');
            if (split.Length == 2)
            {
                result = new ImageInfo
                {
                    URL = split[0].Trim(),
                    LocalImagePath = split[1].Trim()
                };
            }
            return result;
        }
    }
}
