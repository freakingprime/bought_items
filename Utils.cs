using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoughtItems
{
    public class Utils
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public Utils()
        {

        }

        public static string GetValidFolderPath(string path)
        {
            if (path == null)
            {
                return "";
            }
            while (path.Length > 0 && !Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return path;
        }
    }
}
