using DevhubWebApp.Models.Tools.Mime;

namespace DevhubWebApp.Constants;

/// <summary>
/// The media-type catalogue, held as data so the explorer's UI is a plain list rendering and
/// adding a type is a one-line change here and nowhere else.
/// </summary>
public static class MimeCatalog
{
    private static readonly IReadOnlyList<MimeEntry> Catalogue =
    [
        // ---- Image ----
        Entry("image/png", ["png"], MimeCategory.Image, "Lossless raster image with alpha transparency.", ["Any browser", "Photoshop", "GIMP"]),
        Entry("image/jpeg", ["jpg", "jpeg", "jfif"], MimeCategory.Image, "Lossy photographic image.", ["Any browser", "Photoshop", "Preview"]),
        Entry("image/gif", ["gif"], MimeCategory.Image, "Indexed-colour image with simple animation.", ["Any browser"]),
        Entry("image/webp", ["webp"], MimeCategory.Image, "Modern image format with lossy and lossless modes.", ["Any modern browser"]),
        Entry("image/avif", ["avif"], MimeCategory.Image, "AV1-based image format with very high compression.", ["Modern browsers"]),
        Entry("image/svg+xml", ["svg"], MimeCategory.Image, "Scalable vector graphics, written in XML.", ["Any browser", "Illustrator", "Inkscape"]),
        Entry("image/x-icon", ["ico"], MimeCategory.Image, "Windows icon, used for favicons.", ["Browsers", "Windows Explorer"]),
        Entry("image/tiff", ["tif", "tiff"], MimeCategory.Image, "High-fidelity raster image used in print and scanning.", ["Photoshop", "Preview"]),
        Entry("image/bmp", ["bmp"], MimeCategory.Image, "Uncompressed Windows bitmap.", ["Paint", "Preview"]),
        Entry("image/heic", ["heic"], MimeCategory.Image, "High-efficiency image used by Apple devices.", ["Photos", "Preview"]),

        // ---- Audio ----
        Entry("audio/mpeg", ["mp3"], MimeCategory.Audio, "Lossy audio, the ubiquitous music format.", ["Any player"]),
        Entry("audio/wav", ["wav"], MimeCategory.Audio, "Uncompressed PCM audio.", ["Audacity", "Any player"]),
        Entry("audio/ogg", ["ogg", "oga"], MimeCategory.Audio, "Open container, usually holding Vorbis or Opus audio.", ["VLC", "Firefox"]),
        Entry("audio/flac", ["flac"], MimeCategory.Audio, "Lossless compressed audio.", ["VLC", "foobar2000"]),
        Entry("audio/aac", ["aac", "m4a"], MimeCategory.Audio, "Lossy audio used by Apple and streaming services.", ["iTunes", "VLC"]),
        Entry("audio/webm", ["weba"], MimeCategory.Audio, "WebM container carrying audio only.", ["Browsers", "VLC"]),
        Entry("audio/midi", ["mid", "midi"], MimeCategory.Audio, "Note and instrument events rather than recorded sound.", ["DAWs", "Media players"]),

        // ---- Video ----
        Entry("video/mp4", ["mp4", "m4v"], MimeCategory.Video, "The default web and mobile video container.", ["Any player", "Browsers"]),
        Entry("video/webm", ["webm"], MimeCategory.Video, "Open web video container, VP8/VP9 or AV1.", ["Browsers", "VLC"]),
        Entry("video/quicktime", ["mov"], MimeCategory.Video, "Apple QuickTime container.", ["QuickTime", "VLC"]),
        Entry("video/x-msvideo", ["avi"], MimeCategory.Video, "Legacy Windows video container.", ["VLC", "Media Player"]),
        Entry("video/x-matroska", ["mkv"], MimeCategory.Video, "Flexible container that carries almost any codec.", ["VLC", "MPV"]),
        Entry("video/mpeg", ["mpeg", "mpg"], MimeCategory.Video, "MPEG-1 and MPEG-2 video.", ["VLC"]),

        // ---- Document ----
        Entry("application/pdf", ["pdf"], MimeCategory.Document, "Fixed-layout document.", ["Acrobat", "Browsers", "Preview"]),
        Entry("text/plain", ["txt", "log", "text"], MimeCategory.Document, "Unformatted text.", ["Any editor"]),
        Entry("text/markdown", ["md", "markdown"], MimeCategory.Document, "Lightweight markup that renders as HTML.", ["VS Code", "Obsidian", "GitHub"]),
        Entry("text/csv", ["csv"], MimeCategory.Document, "Comma-separated tabular data.", ["Excel", "Numbers", "Any editor"]),
        Entry("application/rtf", ["rtf"], MimeCategory.Document, "Rich text with basic formatting.", ["Word", "TextEdit"]),
        Entry("application/msword", ["doc"], MimeCategory.Document, "Legacy binary Word document.", ["Word", "LibreOffice"]),
        Entry("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ["docx"], MimeCategory.Document,
            "Word document in the Office Open XML format.", ["Word", "Google Docs", "LibreOffice"]),
        Entry("application/vnd.ms-excel", ["xls"], MimeCategory.Document, "Legacy binary Excel workbook.", ["Excel", "LibreOffice"]),
        Entry("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ["xlsx"], MimeCategory.Document,
            "Excel workbook in the Office Open XML format.", ["Excel", "Google Sheets", "LibreOffice"]),
        Entry("application/vnd.openxmlformats-officedocument.presentationml.presentation", ["pptx"], MimeCategory.Document,
            "PowerPoint deck in the Office Open XML format.", ["PowerPoint", "Google Slides", "Keynote"]),
        Entry("application/epub+zip", ["epub"], MimeCategory.Document, "Reflowable e-book.", ["Apple Books", "Calibre"]),

        // ---- Archive ----
        Entry("application/zip", ["zip"], MimeCategory.Archive, "The most widely supported archive format.", ["Explorer", "Finder", "7-Zip"]),
        Entry("application/gzip", ["gz"], MimeCategory.Archive, "Single-file compression, usually wrapping a tar.", ["tar", "7-Zip"]),
        Entry("application/x-tar", ["tar"], MimeCategory.Archive, "Uncompressed archive of files and metadata.", ["tar", "7-Zip"]),
        Entry("application/x-7z-compressed", ["7z"], MimeCategory.Archive, "High-ratio archive format.", ["7-Zip", "Keka"]),
        Entry("application/vnd.rar", ["rar"], MimeCategory.Archive, "Proprietary archive format.", ["WinRAR", "7-Zip"]),
        Entry("application/x-bzip2", ["bz2"], MimeCategory.Archive, "Block-sorting compression, often paired with tar.", ["tar", "7-Zip"]),

        // ---- Font ----
        Entry("font/woff2", ["woff2"], MimeCategory.Font, "Compressed web font, the current standard.", ["Browsers"]),
        Entry("font/woff", ["woff"], MimeCategory.Font, "Older compressed web font.", ["Browsers"]),
        Entry("font/ttf", ["ttf"], MimeCategory.Font, "TrueType outline font.", ["Operating systems", "Design tools"]),
        Entry("font/otf", ["otf"], MimeCategory.Font, "OpenType font with advanced typography.", ["Operating systems", "Design tools"]),

        // ---- Code ----
        Entry("text/html", ["html", "htm"], MimeCategory.Code, "Hypertext markup rendered by browsers.", ["Browsers", "Any editor"]),
        Entry("text/css", ["css"], MimeCategory.Code, "Style sheet for HTML documents.", ["Browsers", "Any editor"]),
        Entry("text/javascript", ["js", "mjs"], MimeCategory.Code, "JavaScript source.", ["Browsers", "Node.js"]),
        Entry("application/json", ["json"], MimeCategory.Code, "Structured data interchange format.", ["Any editor", "APIs"]),
        Entry("application/xml", ["xml"], MimeCategory.Code, "Extensible markup for structured documents.", ["Any editor"]),
        Entry("application/yaml", ["yaml", "yml"], MimeCategory.Code, "Human-friendly configuration and data format.", ["Any editor", "Kubernetes"]),
        Entry("application/wasm", ["wasm"], MimeCategory.Code, "WebAssembly binary module.", ["Browsers"]),
        Entry("text/x-csharp", ["cs"], MimeCategory.Code, "C# source file.", ["Visual Studio", "Rider", "VS Code"]),
        Entry("text/x-python", ["py"], MimeCategory.Code, "Python source file.", ["VS Code", "PyCharm"]),
        Entry("text/x-sql", ["sql"], MimeCategory.Code, "SQL script.", ["SSMS", "DataGrip", "Any editor"]),

        // ---- Executable ----
        Entry("application/vnd.microsoft.portable-executable", ["exe", "dll"], MimeCategory.Executable,
            "Windows executable or library.", ["Windows"]),
        Entry("application/x-msdownload", ["msi"], MimeCategory.Executable, "Windows installer package.", ["Windows Installer"]),
        Entry("application/x-apple-diskimage", ["dmg"], MimeCategory.Executable, "macOS disk image, usually an installer.", ["macOS"]),
        Entry("application/vnd.debian.binary-package", ["deb"], MimeCategory.Executable, "Debian and Ubuntu package.", ["apt", "dpkg"]),
        Entry("application/vnd.android.package-archive", ["apk"], MimeCategory.Executable, "Android application package.", ["Android"]),

        // ---- Data ----
        Entry("application/octet-stream", ["bin", "dat"], MimeCategory.Data, "Arbitrary binary data; the fallback type.", ["Depends on content"]),
        Entry("application/x-sqlite3", ["sqlite", "db"], MimeCategory.Data, "SQLite database file.", ["DB Browser", "sqlite3"]),
        Entry("application/x-ndjson", ["ndjson", "jsonl"], MimeCategory.Data, "One JSON document per line, for streaming.", ["Any editor", "jq"]),
        Entry("text/tab-separated-values", ["tsv"], MimeCategory.Data, "Tab-separated tabular data.", ["Excel", "Any editor"])
    ];

    /// <summary>All known media types, ordered by type name.</summary>
    public static IReadOnlyList<MimeEntry> All { get; } =
        Catalogue.OrderBy(entry => entry.MimeType, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Categories that contain at least one entry, in declaration order.</summary>
    public static IReadOnlyList<MimeCategory> Categories { get; } =
        Enum.GetValues<MimeCategory>().Where(category => All.Any(entry => entry.Category == category)).ToList();

    private static MimeEntry Entry(string mimeType, string[] extensions, MimeCategory category,
        string description, string[]? applications = null) => new()
        {
            MimeType = mimeType,
            Extensions = extensions,
            Category = category,
            Description = description,
            Applications = applications ?? []
        };
}
