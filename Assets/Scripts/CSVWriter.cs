using UnityEngine;
using System.IO;

public class CSVWriter : MonoBehaviour
{
    //filename for when the player faces the ml agent
    public string filenameAgent = " ";
    //filename for when the player faces the static AI.
    public string filenameStaticAI = " ";

    public GameManager gameManager;

    //data to be written to csv file.
    public int Round;
    public int Wins;
    public int Losses;
    public int Punches;
    public int Kicks;
    public int Combo1;
    public int Combo2;
    public int Blocks;
    public int Jumps;
    public int Knockdown;
    public int Hit;
    public int Health;

    private string dataPath = "";

    // Start is called before the first frame update
    void Start()
    {
        if (gameManager)
        {
            if (ButtonPress.isMLAgent)
            {
                dataPath = Application.streamingAssetsPath + gameManager.folderPath + filenameAgent;
            }
            else
            {
                dataPath = Application.streamingAssetsPath + gameManager.folderPath + filenameStaticAI;
            }
        }
    }

    public void WriteHeadings()
    {
        if(dataPath.Length > 0)
        {
            TextWriter tw = new StreamWriter(dataPath, false);
            tw.WriteLine("Round, Wins, Losses, Punches, Kicks, Combo1, Combo2, Blocks, Jumps, Knockdown, Hit, Health");
            tw.Close();
        }
    }
    public void WriteCSV()
    {
        if (dataPath.Length > 0)
        {
            TextWriter tw = new StreamWriter(dataPath, true);
            tw.WriteLine(Round + "," + Wins + "," + Losses + "," + Punches + "," + Kicks + "," + Combo1 + "," + Combo2 + "," + Blocks + "," + Jumps + "," + Knockdown + "," + Hit + "," + Health);
            tw.Close();
        }
    }

    public void ResetVariables()
    {
        Punches = 0;
        Kicks = 0;
        Blocks = 0;
        Combo1 = 0;
        Combo2 = 0;
        Hit = 0;
        Knockdown = 0;
        Health = 0;
        Jumps = 0;
    }
}
