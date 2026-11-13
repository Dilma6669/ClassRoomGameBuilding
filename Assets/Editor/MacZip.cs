using System.IO;
using System.IO.Compression;
using UnityEngine;

/// <summary>
/// Zips a Mac .app built on Windows so it still runs on a Mac.
/// Windows' own zip loses the "this file is a program" (executable) flag that macOS needs, and the app then
/// won't start. This zip writes Unix permissions: the program files in Contents/MacOS are executable.
/// It also adds a short 'HOW TO OPEN ON A MAC' note (the app isn't signed by Apple).
/// </summary>
public static class MacZip
{
    private const int Executable = 0x81ED; // regular file, rwxr-xr-x (0100755)
    private const int Normal = 0x81A4;     // regular file, rw-r--r-- (0100644)

    public const string HowToOpen =
        "HOW TO OPEN THIS GAME ON A MAC\r\n" +
        "==============================\r\n\r\n" +
        "The Mac blocks games that weren't bought from the App Store or paid for by a registered Apple developer.\r\n" +
        "This class game is safe, so you just tell the Mac it's OK, once. Pick ONE of the ways below.\r\n\r\n" +
        "1. Double-click the zip to unzip it. Drag the game into your Applications folder.\r\n\r\n" +
        "EASIEST WAY (works on every Mac): the Terminal\r\n" +
        "   a. Open the Terminal app (press Command + Space, type Terminal, press Return).\r\n" +
        "   b. Copy this line, paste it into Terminal and press Return:\r\n\r\n" +
        "        xattr -cr \"/Applications/{APP}\"\r\n\r\n" +
        "   c. Double-click the game. Done! It opens normally from now on.\r\n" +
        "   (If the game is NOT in Applications: type  xattr -cr  and a space, drag the game onto the Terminal\r\n" +
        "    window, then press Return.)\r\n\r\n" +
        "OTHER WAY (no Terminal): System Settings\r\n" +
        "   a. Double-click the game. The Mac says it can't be opened: click Done (or OK).\r\n" +
        "   b. Open System Settings > Privacy & Security and scroll right down.\r\n" +
        "   c. Next to '{NAME} was blocked', click 'Open Anyway', then type your Mac password.\r\n" +
        "   d. Double-click the game again and click 'Open Anyway'.\r\n" +
        "   (On older Macs, RIGHT-click the game, choose Open, then click Open.)\r\n\r\n" +
        "STILL WON'T OPEN, or it says \"damaged\"?\r\n" +
        "   Do the Terminal way above, then also paste this line and press Return:\r\n\r\n" +
        "        codesign --force --deep --sign - \"/Applications/{APP}\"\r\n\r\n" +
        "   Then double-click the game.\r\n";

    /// <summary>Zips appPath (a .app folder) into zipPath, with the right permissions. Returns zipPath.</summary>
    public static string ZipApp(string appPath, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        string appName = Path.GetFileName(appPath);
        string root = Path.GetDirectoryName(appPath);

        using (FileStream stream = new FileStream(zipPath, FileMode.Create))
        using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            foreach (string file in Directory.GetFiles(appPath, "*", SearchOption.AllDirectories))
            {
                string inZip = Path.Combine(appName, Path.GetRelativePath(appPath, file)).Replace('\\', '/');
                ZipArchiveEntry entry = zip.CreateEntry(inZip, System.IO.Compression.CompressionLevel.Optimal);
                bool program = inZip.Contains("/Contents/MacOS/") || inZip.EndsWith(".dylib") || inZip.Contains("/Contents/Frameworks/");
                entry.ExternalAttributes = (program ? Executable : Normal) << 16;
                using (Stream to = entry.Open())
                using (FileStream from = File.OpenRead(file))
                {
                    from.CopyTo(to);
                }
            }

            ZipArchiveEntry note = zip.CreateEntry("HOW TO OPEN ON A MAC.txt");
            note.ExternalAttributes = Normal << 16;
            using (StreamWriter writer = new StreamWriter(note.Open()))
            {
                writer.Write(HowToOpen.Replace("{APP}", appName).Replace("{NAME}", Path.GetFileNameWithoutExtension(appName)));
            }
        }
        Debug.Log("Mac zip ready: " + zipPath + " (" + (new FileInfo(zipPath).Length / (1024 * 1024)) + " MB)");
        return zipPath;
    }
}
