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

    private static void LogStorageError(string prefix, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            File.AppendAllText(
                Path.Combine(FolderPath, "plugin.log"),
                DateTime.UtcNow.ToString("O") + " [" + prefix + "] " + ex + Environment.NewLine);
        }
        catch
        {
        }
    }

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
        catch (Exception ex)
        {
            LogStorageError("AUTH_STORAGE_READ", ex);
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
        catch (Exception ex)
        {
            LogStorageError("AUTH_STORAGE_WRITE", ex);
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
        catch (Exception ex)
        {
            LogStorageError("AUTH_STORAGE_CLEAR", ex);
        }
    }
}
