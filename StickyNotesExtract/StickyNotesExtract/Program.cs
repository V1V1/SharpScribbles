using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace StickyNotesExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\r\n===== StickyNotesExtract: Read data from Windows Sticky Notes =====\r\n");

            // Find notes DB
            // From SharpStick (https://github.com/two06/SharpStick)
            string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    + @"\Packages\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe\LocalState\plum.sqlite";

            if (!File.Exists(dbPath))
            {
                Console.WriteLine("[X] Stick Notes DB not found.\n");
                return;
            }

            // Duplicate notes DB
            // Adapted from SharpChromium (https://github.com/djhohnstein/SharpChromium/blob/master/Utils.cs)
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string newFile = "";
            newFile = Path.GetRandomFileName();
            string tempDBFile = localAppData + "\\Temp\\" + newFile;
            File.Copy(dbPath, tempDBFile);

            Console.WriteLine("[!] Sticky Notes DB file: " + dbPath);
            Console.WriteLine("\n[i] Created temporary DB at: " + tempDBFile);

            // I couldn't figure out a safe way to open WAL enabled sqlite DBs (https://github.com/metacore/csharp-sqlite/issues/112)
            // So we'll "patch" our temporary DB file to disable WAL journaling
            // Patch idea from here (https://stackoverflow.com/a/5476850)
            // Warning - Don't use this patch on live DB files, always create temp ones first

            var offsets = new List<int> { 0x12, 0x13 };

            foreach (var n in offsets)

                using (var fs = new FileStream(tempDBFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Position = n;
                    fs.WriteByte(Convert.ToByte(0x1));
                }

            // Extract data from temp sqlite DB
            try
            {
                // DB connection
                SQLiteConnection database = null;
                database = new SQLiteConnection(tempDBFile, SQLiteOpenFlags.ReadOnly, false);

                // Notes table query
                Console.WriteLine("\n[*] Extracting Sticky Notes data:");
                
                string query = "SELECT Text, IsOpen, Theme, UpdatedAt FROM Note;";
                List<SQLiteQueryRow> results = database.Query2(query, false);

                if (results.Count == 0)
                {
                    Console.WriteLine("\n[X] No notes found in database.\n");
                    return;
                }

                // Print results
                int i = 1;
                foreach (SQLiteQueryRow row in results)
                {
                    // Notes metadata
                    Console.WriteLine($"\n##### Note {i++} #####\n");
                    Console.WriteLine("Color: " + row.column[2].Value);
                    Console.WriteLine("NoteIsOpen: " + row.column[1].Value);

                    // Notes text
                    Console.WriteLine("Note contents:\n");

                    var str = row.column[0].Value.ToString();
                    using (StringReader reader = new StringReader(str))
                    {
                        string line = string.Empty;
                        do
                        {
                            line = reader.ReadLine();
                            if (line != null)
                            {
                                // Remove first 40 characters from each line of notes text
                                string noteText = line.Substring(40, line.Length - 40);
                                Console.WriteLine(noteText);
                            }
                        } while (line != null);
                    }

                }

                // Get path to notes media attachments
                Console.WriteLine("\n[*] Notes media attachments:\n");
                string mediaPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    + @"\Packages\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe\LocalState\media\";

                string[] filePaths = Directory.GetFiles(mediaPath);
                if (filePaths != null)
                    foreach (var file in filePaths)
                    {
                        Console.WriteLine(file);
                    }
                else
                {
                    Console.WriteLine("[i] No media attachments.");
                }

                // Cleanup - Close sqlite connection and delete temp DB file
                database.Close();

                try
                {
                    File.Delete(tempDBFile);
                    Console.WriteLine("\n[i] Temporary DB at '{0}' has been deleted.", tempDBFile);
                }
                catch { Console.WriteLine("[X] Failed to delete temp notes database at '{0}'", tempDBFile); }

                Console.WriteLine("\n[*] Done.\n");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}