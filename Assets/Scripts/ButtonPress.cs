using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonPress : MonoBehaviour
{
    //static variable allows other scripts to access and change its current state.
    public static bool isMLAgent;

    //player will face the Ml Agent.
    public void MLAgentButton()
    {
        isMLAgent = true;
        SceneManager.LoadScene("SampleScene");
    }

    //player will face the static AI.
    public void StaticAIButton()
    {
        isMLAgent = false;
        SceneManager.LoadScene("SampleScene");
    }

    //Exit game
    public void ExitButton()
    {
        //quits works when the game is built.
        Application.Quit();
    }
}
