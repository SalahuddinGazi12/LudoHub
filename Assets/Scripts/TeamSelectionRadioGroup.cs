using UnityEngine;
using UnityEngine.UI;

public class TeamSelectionRadioGroup : MonoBehaviour
{
    public delegate void OnPlayerCountSelected(int count);
    public event OnPlayerCountSelected onPlayerCountSelected;

    [SerializeField] private Toggle redTeamToggle;
    [SerializeField] private Toggle blueTeamToggle;
    [SerializeField] private Toggle yellowTeamToggle;
    [SerializeField] private Toggle greenTeamToggle;

    void Start()
    {
        redTeamToggle.onValueChanged.AddListener((bool b) => {
            CallPlayerCountSelected(0);
        });

        blueTeamToggle.onValueChanged.AddListener((bool b) => {
            CallPlayerCountSelected(1);
        });

        yellowTeamToggle.onValueChanged.AddListener((bool b) => {
            CallPlayerCountSelected(2);
        });

        greenTeamToggle.onValueChanged.AddListener((bool b) => {
            CallPlayerCountSelected(3);
        });
    }

    void CallPlayerCountSelected(int count)
    {
        onPlayerCountSelected?.Invoke(count);
    }
}
