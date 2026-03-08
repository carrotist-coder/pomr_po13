using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FileNodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshPro _name;
    
    [SerializeField] private BoxCollider2D _boxCollider;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Transform _transform;

    public void Setup(string name)
    {
        _name.text = name;
    }

    public void OnClick()
    {
        
    }
}
