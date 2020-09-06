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
    class Thunderbird
    {
        public static void ExtractContacts(string contactsDBPath, string displayFormat = "csv")
        {
            if (!File.Exists(contactsDBPath))
            {
                return;
            }

            bool someResults = false;
            SQLiteConnection database = null;

            try
            {
                database = new SQLiteConnection(contactsDBPath, SQLiteOpenFlags.ReadOnly, false);
            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            string query = "SELECT contactID, kind, value FROM identities ORDER by contactID ASC;";
            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                if (displayFormat.Equals("csv"))
                {
                    if (!someResults)
                    {
                        Console.WriteLine("\r\n----- Thunderbird Contacts -----\r\n");
                        Console.WriteLine("Contact ID,Type,Contact");
                    }
                    someResults = true;
                    Console.WriteLine("{0},{1},{2}", row.column[0].Value, row.column[1].Value, row.column[2].Value);
                }
            }
            database.Close();

            Console.WriteLine("\n[*] Done.\n");
        }

        public static void ExtractMailList(string mailDBPath, string displayFormat = "csv", string bodyRegex = "")
        {
            if (!File.Exists(mailDBPath))
            {
                return;
            }

            bool someResults = false;
            SQLiteConnection database = null;

            if (!displayFormat.Equals("csv") && !displayFormat.Equals("table"))
            {
                Console.WriteLine("\r\n[X] Invalid output format: {0}\n", displayFormat);
                return;
            }

            try
            {
                database = new SQLiteConnection(mailDBPath, SQLiteOpenFlags.ReadOnly, false);
            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            string query = String.Join(
                    Environment.NewLine,
                    "SELECT docid, c0body, c1subject, c3author, c4recipients, c2attachmentNames, date / 1000000",
                    "FROM messagesText_content",
                    "INNER JOIN messages on messages.id = messagesText_content.docid",
                    "ORDER by docid ASC;");

            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                // check conditions that will determine whether we're displaying this cookie entry
                bool displayValue = false;
                if (String.IsNullOrEmpty(bodyRegex))
                {
                    displayValue = true;
                }
                else if (!String.IsNullOrEmpty(bodyRegex))
                {
                    Match match = Regex.Match(row.column[1].Value.ToString(), bodyRegex, RegexOptions.IgnoreCase);
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
                            Console.WriteLine("\r\n----- Thunderbird List Mail -----\r\n");
                            Console.WriteLine("Email ID;Subject;From;To;Attachments;Date");
                        }
                        someResults = true;
                        // Convert Thunderbird time (unix time) format to local time
                        var emailDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[6].Value.ToString());

                        Console.WriteLine("{0};{1};{2};{3};{4};{5}",
                            row.column[0].Value, row.column[2].Value,
                            row.column[3].Value, row.column[4].Value,
                            row.column[5].Value.ToString().Replace("\n", ","), emailDate);
                    }
                    else if (displayFormat.Equals("table"))
                    {
                        if (!someResults)
                        {
                            Console.WriteLine("\r\n----- Thunderbird List Mail -----\r\n");
                        }
                        someResults = true;
                        // Convert Thunderbird time (unix time) format to local time
                        var emailDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[6].Value.ToString());

                        Console.WriteLine("Email ID        : {0}", row.column[0].Value);
                        Console.WriteLine("Subject         : {0}", row.column[2].Value);
                        Console.WriteLine("Date            : {0}", emailDate);
                        Console.WriteLine("From            : {0}", row.column[3].Value);
                        Console.WriteLine("To              : {0}", row.column[4].Value);
                        Console.WriteLine("Attachments     : {0}\r\n\r\n", row.column[5].Value.ToString().Replace("\n", ","));
                    }

                }

            }
            database.Close();

            Console.WriteLine("\n[*] Done.\n");
        }

        public static void ExtractSingleMail(string mailDBPath, string displayFormat = "table", string emailID = "")
        {

            if (String.IsNullOrEmpty(emailID))
            {
                Console.WriteLine("\r\n[X] No email ID specified.\n");
                return;
            }

            // Ensure supplied email ID is a numerical value
            if (!emailID.All(char.IsDigit))
            {
                Console.WriteLine("\r\n[X] Email ID must be a numeric value.\n");
                return;
            }

            bool someResults = false;
            SQLiteConnection database = null;

            try
            {
                database = new SQLiteConnection(mailDBPath, SQLiteOpenFlags.ReadOnly, false);
            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            // Read email matching the user supplied emailID

            string query = String.Join(
                    Environment.NewLine,
                    "SELECT docid, c0body, c1subject, c3author, c4recipients, c2attachmentNames, date / 1000000",
                    "FROM messagesText_content",
                    "INNER JOIN messages on messages.id = messagesText_content.docid",
                    "WHERE messagesText_content.docid = " + emailID + ";");

            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                if (displayFormat.Equals("table"))
                {
                    if (!someResults)
                    {
                        Console.WriteLine("\r\n----- Thunderbird Read Mail -----\r\n");
                    }
                    someResults = true;

                    // Convert Thunderbird time (unix time) format to local time
                    var emailDate = Utils.MiscUtils.UnixTimeToLocalTime(row.column[6].Value.ToString());

                    Console.WriteLine("Email ID        : {0}", row.column[0].Value);
                    Console.WriteLine("Subject         : {0}", row.column[2].Value);
                    Console.WriteLine("Date            : {0}", emailDate);
                    Console.WriteLine("From            : {0}", row.column[3].Value);
                    Console.WriteLine("To              : {0}", row.column[4].Value);
                    Console.WriteLine("Attachments     : {0}\r\n", row.column[5].Value.ToString().Replace("\n", ","));
                    Console.WriteLine("Email Content   :\n\n{0}", row.column[1].Value);
                }

            }

            database.Close();

            Console.WriteLine("\n[*] Done.\n");

        }

    }
}