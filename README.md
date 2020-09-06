# SharpScribbles
This is a collection of projects I'm working on as I learn C#. Expect terrible code that sometimes works.

| Project | Info | .NET Version | Tested on |
| :------ | :---------- | :-----------  | :----------- |
| **StickyNotesExtract** | Extracts data from the Windows Sticky Notes database. Works on Windows 10 Build 1607 and higher. This project doesn't rely on any external dependencies. | 4.6 | Windows >= Build 1607 |
| **ThunderFox** | Retrieves data (contacts, emails, history, cookies and credentials) from Thunderbird and Firefox. More details available in [this post](https://bohops.com/2018/06/28/abusing-com-registry-structure-clsid-localserver32-inprocserver32/). | 4.6 | Thunderbird 78.2.1 and Firefox 80.0.1 |

## StickyNotesExtract usage:
```
$ .\StickyNotesExtract.exe
```
![alt tag](https://github.com/V1V1/SharpScribbles/raw/master/Images/StickyNotesExtract.png)

## ThunderFox usage:
```
$ .\ThunderFox.exe
     _____ _                     _          ______
    |_   _| |                   | |         |  ___|
      | | | |__  _   _ _ __   __| | ___ _ __| |_ _____  __
      | | | '_ \| | | | '_ \ / _` |/ _ \ '__|  _/ _ \ \/ /
      | | | | | | |_| | | | | (_| |  __/ |  | || (_) >  <
      \_/ |_| |_|\__,_|_| |_|\__,_|\___|_|  \_| \___/_/\_\


Usage:
    .\ThunderFox.exe command [/arg:X]

  Global commands:
    creds      -   Retrieve saved credentials from Thunderbird and Firefox.

  Global arguments:
    /target:"DIRECTORY"  -   Target a specific Thunderbird/Firefox profile directory.
                             Do not enumerate default install location.
    /pass:"PASS"         -   Master password used to decrypt credentials.
                             Used with 'creds' command.

  Thunderbird commands:
    contacts   -   Retrieve a list of the user's Thunderbird contacts.
    listmail   -   Retrieve a detailed list of all emails in Thunderbird.
    readmail   -   Read a specific email by supplying its email ID.

    Thunderbird command arguments:
      /search:"REGEX"    -   Used with 'listmail' command.
                             Only return emails where part of the email body contains the supplied regex.
      /format:X          -   Used with 'listmail' command. Either 'csv' (default) or 'table' format.
      /id:X              -   Used with 'readmail' command.
                             Read the email with the supplied email ID. Use 'listmail' to discover email IDs.

  Firefox commands:
    history    -   Retrieve user's Firefox history with a count of each time every URL was visited.
    cookies    -   Retrieve user's Firefox cookies.

    Firefox command arguments:
      /url:"REGEX"       -   Used with 'cookies' command.
                             Only return cookies where the URL contains the supplied regex.
      /format:X          -   Used with 'cookies' command. Either 'csv' (default) or 'json' format.
                             json format can be imported into the Cookie Quick Manager addon.

```
![alt tag](https://github.com/V1V1/SharpScribbles/raw/master/Images/thunderfox.png)
