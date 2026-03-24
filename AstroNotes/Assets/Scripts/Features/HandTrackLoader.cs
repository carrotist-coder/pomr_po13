using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

public class HandTrackLoader : MonoBehaviour
{
    private Process pythonProcess;
    
    void Start()
    {
        LaunchPythonScript();
    }
    
    void LaunchPythonScript()
    {
        try
        {
            // Temporary BAT file with rigth encoding
            string batContent = @"@echo off
chcp 1251 > nul
set PYTHONIOENCODING=cp1251
C:
cd \Users\Женя\pomr_po13\cam
python main.py
pause";
            
            string batPath = Path.Combine(Path.GetTempPath(), "run_hand.bat");
            File.WriteAllText(batPath, batContent, Encoding.GetEncoding(1251));
            
            UnityEngine.Debug.Log($"BAT file created: {batPath}");
            
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = batPath;
            start.UseShellExecute = true;
            start.CreateNoWindow = false;
            start.WindowStyle = ProcessWindowStyle.Normal;
            
            pythonProcess = Process.Start(start);
            
            UnityEngine.Debug.Log("Python script running from BAT");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Launch error: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
        }
    }
}