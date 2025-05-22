using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    public event Action<string> OnConnectClicked;

    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Button _connectButton;

    private void Start()
    {
        _connectButton.onClick.AddListener(() => {
            OnConnectClicked?.Invoke(_nameInput.text);
        });
    }
}