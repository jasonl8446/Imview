/*
BSD 3-Clause License

Copyright (c) 2024, Jooty

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.
*/

using System.Xml.Serialization;
using System.Collections.Generic;

namespace Imview.Core.Models;

[XmlRoot("LatestFileList")]
public class LatestFileList {
    [XmlElement("RECORD")]
    public List<FileRecord> Records { get; set; } = new();
}

public class FileRecord {
    [XmlElement("SrcFileName")]
    public string SourceFileName { get; set; } = string.Empty;

    [XmlElement("TarFileName")]
    public string TargetFileName { get; set; } = string.Empty;

    [XmlElement("FileType")]
    public int FileType { get; set; }

    [XmlElement("Size")]
    public long Size { get; set; }

    [XmlElement("HeaderSize")]
    public long HeaderSize { get; set; }

    [XmlElement("CompressedHeaderSize")]
    public long CompressedHeaderSize { get; set; }

    [XmlElement("CRC")]
    public string CRC { get; set; } = string.Empty;

    [XmlElement("HeaderCRC")]
    public string HeaderCRC { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(SourceFileName) 
        ? "Unknown File" 
        : System.IO.Path.GetFileNameWithoutExtension(SourceFileName);

    public string SizeFormatted => Size switch {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024.0):F1} MB",
        _ => $"{Size / (1024.0 * 1024.0 * 1024.0):F1} GB"
    };
}

public class DownloadProgress {
    public string FileName { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
    public string Status { get; set; } = "Pending";
}

public enum DownloadStatus {
    Pending,
    InProgress,
    Completed,
    Failed,
    Cached
}