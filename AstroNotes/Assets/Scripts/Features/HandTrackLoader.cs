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
            // Определяем путь к проекту в зависимости от компьютера
            string projectPath = GetProjectPath();

            if (string.IsNullOrEmpty(projectPath))
            {
                UnityEngine.Debug.LogError("Project path not found!");
                return;
            }

            // Проверяем существование пути
            if (!Directory.Exists(projectPath))
            {
                UnityEngine.Debug.LogError($"Directory not found: {projectPath}");
                return;
            }

            // Получаем полный путь к main.py
            string pythonScriptPath = Path.Combine(projectPath, "main.py");

            if (!File.Exists(pythonScriptPath))
            {
                UnityEngine.Debug.LogError($"Python script not found: {pythonScriptPath}");
                return;
            }

            // Ищем Python
            string pythonPath = FindPythonPath();

            if (string.IsNullOrEmpty(pythonPath))
            {
                UnityEngine.Debug.LogError("Python not found in system!");
                return;
            }

            UnityEngine.Debug.Log($"Python path: {pythonPath}");
            UnityEngine.Debug.Log($"Script path: {pythonScriptPath}");

            // Запускаем Python скрипт напрямую
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = pythonPath;
            start.Arguments = $"\"{pythonScriptPath}\"";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.WorkingDirectory = projectPath;

            // Настройка кодировки для вывода
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;

            pythonProcess = Process.Start(start);

            // Опционально: читаем вывод для отладки
            pythonProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.Log($"Python: {args.Data}");
            };

            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log("Python script started successfully!");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Launch error: {e.Message}");
        }
    }

    string GetProjectPath()
    {
        // Способ 1: Проверяем оба возможных пути
        string[] possiblePaths = {
            @"C:\Users\Женя\pomr_po13\cam",
            @"E:\kurs3\pomr_13\pomr_po13\cam"
        };

        foreach (string path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                UnityEngine.Debug.Log($"Found project at: {path}");
                return path;
            }
        }

        // Способ 2: Можно также попробовать получить путь из настроек Unity
        string configPath = Application.dataPath + "/../python_path.txt";
        if (File.Exists(configPath))
        {
            string customPath = File.ReadAllText(configPath).Trim();
            if (Directory.Exists(customPath))
            {
                UnityEngine.Debug.Log($"Using custom path from config: {customPath}");
                return customPath;
            }
        }

        // Способ 3: Используем относительный путь от папки проекта
        string relativePath = Path.Combine(Application.dataPath, "..", "python_scripts", "cam");
        relativePath = Path.GetFullPath(relativePath);

        if (Directory.Exists(relativePath))
        {
            UnityEngine.Debug.Log($"Using relative path: {relativePath}");
            return relativePath;
        }

        return null;
    }

    string FindPythonPath()
    {
        // Возможные пути к Python
        string[] possiblePythonPaths = {
            "python",                           // Если Python в PATH
            "python3",                          // Python 3 в PATH
            @"C:\Python39\python.exe",          // Python 3.9
            @"C:\Python310\python.exe",         // Python 3.10
            @"C:\Python311\python.exe",         // Python 3.11
            @"C:\Users\Женя\AppData\Local\Programs\Python\Python39\python.exe",
            @"C:\Users\Женя\AppData\Local\Programs\Python\Python310\python.exe",
            @"C:\Users\Женя\AppData\Local\Programs\Python\Python311\python.exe",
            @"C:\Program Files\Python39\python.exe",
            @"C:\Program Files\Python310\python.exe",
            @"C:\Program Files\Python311\python.exe"
        };

        foreach (string pythonPath in possiblePythonPaths)
        {
            try
            {
                // Проверяем, существует ли Python по этому пути
                if (File.Exists(pythonPath) || pythonPath == "python" || pythonPath == "python3")
                {
                    // Для "python" и "python3" проверяем, доступны ли они
                    if (pythonPath == "python" || pythonPath == "python3")
                    {
                        ProcessStartInfo check = new ProcessStartInfo();
                        check.FileName = pythonPath;
                        check.Arguments = "--version";
                        check.UseShellExecute = false;
                        check.CreateNoWindow = true;
                        check.RedirectStandardOutput = true;

                        using (Process proc = Process.Start(check))
                        {
                            proc.WaitForExit(1000);
                            if (proc.ExitCode == 0)
                            {
                                UnityEngine.Debug.Log($"Python found in PATH: {pythonPath}");
                                return pythonPath;
                            }
                        }
                    }
                    else if (File.Exists(pythonPath))
                    {
                        UnityEngine.Debug.Log($"Python found at: {pythonPath}");
                        return pythonPath;
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"Error checking Python at {pythonPath}: {e.Message}");
            }
        }

        return null;
    }

    void OnDestroy()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.WaitForExit(5000);
                pythonProcess.Dispose();
                UnityEngine.Debug.Log("Python process terminated");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error terminating Python process: {e.Message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        // Дополнительная защита при закрытии приложения
        OnDestroy();
    }
}