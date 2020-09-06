using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ThunderFox;
using System.Globalization;

namespace ThunderFox.Utils
{
    class FileUtils
    {
        public static string CreateTempDuplicateFile(string filePath)
        {
            // Create temporary duplicate files
            // Adapted from SharpChromium (https://github.com/djhohnstein/SharpChromium/blob/master/Utils.cs)
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string newFile = "";
            newFile = Path.GetRandomFileName();
            string tempFileName = localAppData + "\\Temp\\" + newFile;
            File.Copy(filePath, tempFileName);
            return tempFileName;
        }
        public static string PatchWALDatabase(string tempDBFile)
        {
            // I couldn't figure out a safe way to open WAL enabled sqlite DBs (https://github.com/metacore/csharp-sqlite/issues/112)
            // So we'll "patch" temporary DB files we're reading to disable WAL journaling
            // Patch idea from here (https://stackoverflow.com/a/5476850)
            // WARNING - Don't use this patch on live/production sqlite DB files, always create temp duplicates first then patch the copy
            var offsets = new List<int> { 0x12, 0x13 };

            foreach (var n in offsets)

                using (var fs = new FileStream(tempDBFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Position = n;
                    fs.WriteByte(Convert.ToByte(0x1));
                }
            return tempDBFile;
        }
    }
    class MiscUtils
    {
        public static string UnixTimeToLocalTime(string unixTime)
        {
            // Convert Thunderbird/Firefox time (unix time) format to local time
            // https://stackoverflow.com/questions/2883576/how-do-you-convert-epoch-time-in-c
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double rawThunderTime = double.Parse(unixTime);
            var localTime = epoch.AddSeconds(rawThunderTime).ToLocalTime();
            return localTime.ToString("dd-MM-yyyy HH:mm:ss");
        }

        // ByteHelper.cs from firepwd.net (https://github.com/gourk/FirePwd.Net)
        // Convert string to byte array
        public static byte[] ByteHelper(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, 
                    "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        public static string CleanForJSON(string s)
        {
            // helper that cleans a string for JSON output.
            // (https://stackoverflow.com/questions/1242118/how-to-escape-json-string/17691629#17691629)

            if (s == null || s.Length == 0)
            {
                return "";
            }

            char c = '\0';
            int i;
            int len = s.Length;
            StringBuilder sb = new StringBuilder(len + 4);
            String t;

            for (i = 0; i < len; i += 1)
            {
                c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            t = "000" + String.Format("X", c);
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
