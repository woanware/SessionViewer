
 SessionViewer
=============

SessionViewer is a PCAP TCP session reconstructor and viewer with the initial concept/code used from this [codeproject](http://www.codeproject.com/Articles/20501/TCP-Session-Reconstruction-Tool/) article. It has been rewrite numerous times to attempt to use a database as the storage, however, it has proven too slow, even with SQL Server, therefore multiple files are used that are generated with some pre-processing. The TCP reconstruction code has been updated to be in-line with the current version of the original source e.g. Wireshark follow.c.

To speed up the display of each session SessionViewer viewer produces a **large** amount of files, it uses NTFS Alternate Data Streams (ADS) to store three sets of data in one file, a binary HEX version, a colourised HTML version and info data such as extracted URLS. The reason being that it reduces the processing when moving from session to session and adds only a minor overhead whilst performing the initial parsing. It also makes colourising of the conversations easier as it both sides of the network traffic.

The SMTP processing will perform the SMTP MIME parsing, extract all attachments to the selected output directory, in the form “SRCIP.SRCPORT-DSTIP.DSTPORT”, it will then extract any zips, then MD5 all files, and produce a file called “Attachment.hashes.csv” within the output directory. I have also added an extra summary file (Attachment.Summary.txt) which will give you a list of recipients per attachment e.g. uniqued by MD5 hash. The output also shows all of the MIME “Subject”’s used for that attachment, along with a list of senders, and file names etc. The MD5 hashes can be copied out of the Then you can use that fill to feed into your VirusTotal checking.

The HTTP file extraction uses file signatures to extract files. The supported file signatures are zip, exe, gz, pdf, doc, xls, rar.

Note that this application is compiled for x64 only. The version of MimeKit slightly modified to be less strict when parsing SMTP MIME headers.

## Features ##

- Fast
- HEX/Ascii/Colourised Views
- Quick view facilities e.g Q key to move up a session, A key to move down a session
- Excel like grid filtering (right click on the session list column headers)
- Packet parsers (DNS)
- Session parsers (SMTP, HTTP)

## Third party libraries ##

- [Be.HexEditor](http://sourceforge.net/projects/hexbox/) : HEX view of packet data
- [HtmlParserSharp](https://github.com/jamietre/HtmlParserSharp): HTML link parsing
- [HttpKit](https://github.com/woanware/HttpKit): HTTP session parsing (woanware)
- [MaxMind GeoLite](http://www.maxmind.com): Geo IP locations, compiled as GeoIP
- [MimeKit](https://github.com/jstedfast/MimeKit): MIME parsing
- [MS SQL CE](http://www.microsoft.com/en-us/download/details.aspx?id=30709) : Access to MS SQL CE session database
- [Nlog](http://nlog-project.org/): Logging
- [NPoco](https://github.com/schotime/NPoco) : Easy access to SQL CE data
- [ObjectListView](http://objectlistview.sourceforge.net/cs/index.html) : Data viewing via lists
- [PCAPDotNet](http://pcapdotnet.codeplex.com/) : Parsing of PCAP files
- [this.Log](https://github.com/ferventcoder/this.Log): Logging extensions
- [Trinet.Core.IO.Ntfs](https://github.com/hubkey/Trinet.Core.IO.Ntfs): Alternate Data Stream (ADS) storage
- [Utility](http://www.woanware.co.uk) (woanware) : My helper library
- [Winpcap](http://www.winpcap.org/) : Parsing of PCAP files
  
## Requirements ##

Microsoft .NET Framework v4.5 on x64 Windows