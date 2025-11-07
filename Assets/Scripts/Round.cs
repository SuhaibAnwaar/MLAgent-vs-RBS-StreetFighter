using UnityEngine;
using TMPro;

public class Round : MonoBehaviour
{
    [HideInInspector]
    public TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateRound(int roundNumber)
    {
        if (text)
        {
            text.text = "Round " + roundNumber.ToString();
        }
    }

    public void GameFinished()
    {
        text.text = "Game Finished";
    }
}
