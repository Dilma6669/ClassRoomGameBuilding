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
        "1. Double-click the zip to unzip it, then drag the game into your Applications folder (or anywhere).\r\n" +
        "2. The first time: RIGHT-click (or Control-click) the game and choose Open, then click Open again.\r\n" +
        "   (A normal double-click says it can't be opened, because the game isn't signed by Apple.)\r\n" +
        "   After that, a normal double-click works.\r\n\r\n" +
        "If the Mac says the game \"is damaged and can't be opened\":\r\n" +
        "   Open the Terminal app, type   xattr -cr   (with a space after it), drag the game onto the Terminal window,\r\n" +
        "   press Return, then try step 2 again.\r\n";

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
                writer.Write(HowToOpen);
            }
        }
        Debug.Log("Mac zip ready: " + zipPath + " (" + (new FileInfo(zipPath).Length / (1024 * 1024)) + " MB)");
        return zipPath;
    }
}
