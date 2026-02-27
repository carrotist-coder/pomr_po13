using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileManager : IFileService
{
    public string NoteFoulder { get; private set; }

    public FileManager()
    {
        string roamingPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        NoteFoulder = Path.Combine(roamingPath, "AstroNotes");
        
        if (!Directory.Exists(NoteFoulder))
            Directory.CreateDirectory(NoteFoulder);
    }
}
