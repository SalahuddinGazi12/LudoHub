using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageTxt;
    [SerializeField] private Button closeBtn; 


    public void ShowMessagePanel(string message)
    {
        messageTxt.text = message;
        gameObject.SetActive(true);
    }
    
    public void ShowMessagePanelWithCustomAction(string message, Action action)
    {
        messageTxt.text = message;
        gameObject.SetActive(true);
        AddCloseButtonAction(action);
    }

    public void CloseMessagePanel()
    {
        gameObject.SetActive(false);
    }

    private void AddCloseButtonAction(Action closeAction)
    {
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(()=> closeAction?.Invoke());
    }
}