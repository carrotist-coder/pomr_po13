using UnityEngine;
using TMPro;
using System.IO;

public class MarkdownEditorView : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _fileNameText;

    private FileNode _activeNode;
    private IFileService _fileService;

    public bool IsActive => _panel.activeSelf;

    public void Initialize(IFileService fileService)
    {
        _fileService = fileService;
        _panel.SetActive(false);
    }

    public void OpenFile(FileNode node)
    {
        if (node.IsDirectory) return;

        _activeNode = node;
        _fileNameText.text = node.Name;
        
        if (File.Exists(node.FullPath))
        {
            _inputField.text = File.ReadAllText(node.FullPath);
        }

        _panel.SetActive(true);
    }

    public void CloseAndSave()
    {
        if (_activeNode != null && !string.IsNullOrEmpty(_activeNode.FullPath))
        {
            File.WriteAllText(_activeNode.FullPath, _inputField.text);
            Debug.Log($"Saved: {_activeNode.Name}");
        }

        _activeNode = null;
        _panel.SetActive(false);
    }
}