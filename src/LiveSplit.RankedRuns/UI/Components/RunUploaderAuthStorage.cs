using System;
using System.IO;
using System.Xml;

namespace LiveSplit.UI.Components;

public static class RunUploaderAuthStorage
{
    private static string FolderPath
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RankedRuns",
            "LiveSplit");

    private static string FilePath
        => Path.Combine(FolderPath, "auth.dat");

    public static string ReadRefreshTokenProtected()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return "";
            }

            var doc = new XmlDocument();
            doc.Load(FilePath);

            return doc.DocumentElement?["RefreshTokenProtected"]?.InnerText ?? "";
        }
        catch
        {
            return "";
        }
    }

    public static void WriteRefreshTokenProtected(string protectedToken)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);

            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("RankedRunsLiveSplitAuth");
            doc.AppendChild(root);

            XmlElement token = doc.CreateElement("RefreshTokenProtected");
            token.InnerText = protectedToken ?? "";
            root.AppendChild(token);

            doc.Save(FilePath);
        }
        catch
        {
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        catch
        {
        }
    }
}
