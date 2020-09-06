using System;
using System.Management;
using System.Data;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SQLite;
using Microsoft.Win32;
using System.Linq;

namespace ThunderFox
{
    class Firefox
    {
        public static void ExtractHistory(string historyDBPath, string displayFormat = "csv")
        {
            if (!File.Exists(historyDBPath))
            {
                return;
            }

            // Create temp DB file & patch it to disable WAL journaling so we can open it
            string tempDBPath = Utils.FileUtils.CreateTempDuplicateFile(historyDBPath);
            // Patch the DB
            string patchedhistoryDB = Utils.FileUtils.PatchWALDatabase(tempDBPath);
            Console.WriteLine("[i] Created temporary DB at: '{0}", patchedhistoryDB);
            
            bool someResults = false;
            SQLiteConnection database = null;

            try
            {
                database = new SQLiteConnection(patchedhistoryDB, SQLiteOpenFlags.ReadWrite, false);

                // Drop moz_meta table before trying to run queries
                // No idea why, but if this table is in the DB when I try to read it I get a'malformed database schema' error
                string toDrop = "moz_meta";
                int d = database.DropTable<TableMapping>(toDrop);
            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            string query = "SELECT url, visit_count FROM moz_places ORDER by visit_count DESC;";
            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                if (displayFormat.Equals("csv"))
                {
                    if (!someResults)
                    {
                        Console.WriteLine("\r\n----- Firefox History -----\r\n");
                        Console.WriteLine("URL,Visit Count");
                    }
                    someResults = true;
                    Console.WriteLine("{0},{1}", row.column[0].Value, row.column[1].Value);
                }
            }
            database.Close();

            try
            {
                File.Delete(patchedhistoryDB);
                Console.WriteLine("\r\n\r\n[i] Temporary DB at '{0}' has been deleted.", patchedhistoryDB);
            }
            catch { Console.WriteLine("[X] Failed to delete temp notes database at '{0}'", patchedhistoryDB); }

            Console.WriteLine("\n[*] Done.\n");
        }

        public static void ExtractCookies(string cookiesDBPath, string displayFormat = "csv", string urlRegex = "")
        {
            if (!File.Exists(cookiesDBPath))
            {
                return;
            }

            // Create temp DB file & patch it to disable WAL journaling so we can open it
            string tempDBPath = Utils.FileUtils.CreateTempDuplicateFile(cookiesDBPath);
            // Patch the DB
            string patchedcookiesDB = Utils.FileUtils.PatchWALDatabase(tempDBPath);
            Console.WriteLine("[i] Created temporary DB at: '{0}", patchedcookiesDB);

            bool someResults = false;
            SQLiteConnection database = null;

            if (!displayFormat.Equals("csv") && !displayFormat.Equals("table") && !displayFormat.Equals("json"))
            {
                Console.WriteLine("\r\n[X] Invalid output format: {0}\n", displayFormat);
                return;
            }

            try
            {
                database = new SQLiteConnection(patchedcookiesDB, SQLiteOpenFlags.ReadOnly, false);
            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            // Cookies query
            string query = String.Join(
                Environment.NewLine,
                "SELECT host, name, path, value, expiry, isSecure, isHttpOnly, sameSite",
                "FROM moz_cookies",
                "ORDER by host;");

            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                bool displayValue = false;
                if (String.IsNullOrEmpty(urlRegex))
                {
                    displayValue = true;
                }
                else if (!String.IsNullOrEmpty(urlRegex))
                {
                    Match match = Regex.Match(row.column[0].Value.ToString(), urlRegex, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        displayValue = true;
                    }
                }
                if (displayValue)
                {
                    if (displayFormat.Equals("csv"))
                    {
                        if (!someResults)
                        {
                            Console.WriteLine("\r\n----- Firefox Cookies -----\r\n");
                            Console.WriteLine("Host;Name;Path;Value;Expiry;IsSecure;IsHttpOnly;SameSite");
                        }
                        someResults = true;
                        // Convert Firefox time (unix time) format to local time
                        var expiryDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[4].Value.ToString());

                        Console.WriteLine("{0};{1};{2};{3};{4};{5};{4};{5}",
                            row.column[0].Value, row.column[1].Value,
                            row.column[2].Value, row.column[3].Value,
                            expiryDate, row.column[5].Value,
                            row.column[6].Value, row.column[7].Value);
                    }
                    else if (displayFormat.Equals("table"))
                    {
                        if (!someResults)
                        {
                            Console.WriteLine("\r\n----- Firefox Cookies -----\r\n");
                        }
                        someResults = true;
                        // Convert Firefox time (unix time) format to local time
                        var expiryDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[4].Value.ToString());

                        Console.WriteLine("Host        : {0}", row.column[0].Value);
                        Console.WriteLine("Name        : {0}", row.column[1].Value);
                        Console.WriteLine("Path        : {0}", row.column[2].Value);
                        Console.WriteLine("Value       : {0}", row.column[3].Value);
                        Console.WriteLine("Expiry      : {0}", expiryDate);
                        Console.WriteLine("IsSecure    : {0}", row.column[5].Value);
                        Console.WriteLine("IsHTTPOnly  : {0}", row.column[6].Value);
                        Console.WriteLine("SameStie    : {0}\n", row.column[7].Value);
                    }
                    else if (displayFormat.Equals("json"))
                    {
                        if (!someResults)
                        {
                            Console.WriteLine("\r\n----- Firefox Cookies -----\r\n");
                            Console.WriteLine("[*] Cookies Quick Manager JSON import:\r\n\r\n[{\r");
                        }
                        else
                        {
                            Console.WriteLine("},\r\n{\r");
                        }
                        someResults = true;

                        // Convert Firefox time (unix time) format to local time
                        var expiryDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[4].Value.ToString());

                        Console.WriteLine("    \"Host raw\": \"https://{0}\",", row.column[0].Value.ToString());
                        Console.WriteLine("    \"Name raw\": \"{0}\",", Utils.MiscUtils.CleanForJSON(String.Format("{0}", row.column[1].Value)));
                        Console.WriteLine("    \"Path raw\": \"{0}\",", String.Format("{0}", row.column[2].Value));
                        Console.WriteLine("    \"Content raw\": \"{0}\",", Utils.MiscUtils.CleanForJSON(row.column[3].Value.ToString()));
                        Console.WriteLine("    \"Expires\": \"{0}\",", expiryDate);
                        Console.WriteLine("    \"Expires raw\": \"{0}\",", row.column[4].Value.ToString());
                        Console.WriteLine("    \"Send for\": \"Any type of connection\",");
                        Console.WriteLine("    \"Send for raw\": \"false\",");
                        Console.WriteLine("    \"HTTP only raw\": \"false\",");
                        Console.WriteLine("    \"SameSite raw\": \"no_restriction\",");
                        Console.WriteLine("    \"This domain only\": \"Valid for subdomains\",");
                        Console.WriteLine("    \"This domain only raw\": \"false\",");
                        Console.WriteLine("    \"Store raw\": \"firefox-default\",");
                        Console.WriteLine("    \"First Party Domain\": \"\"");

                    }

                }

            }

            if (displayFormat.Equals("json") && someResults)
            {
                Console.WriteLine("}]\r\n");
            }

            database.Close();

            try
            {
                File.Delete(patchedcookiesDB);
                Console.WriteLine("\n[i] Temporary DB at '{0}' has been deleted.", patchedcookiesDB);
            }
            catch { Console.WriteLine("[X] Failed to delete temp notes database at '{0}'", patchedcookiesDB); }

            Console.WriteLine("\n[*] Done.\n");

        }
    }
}