using UnityEngine;

// Counts collected coins and draws "Coins x / total" under the existing HUD lines.
public class CoinCounter : MonoBehaviour
{
    public static CoinCounter Instance { get; private set; }

    public int Collected { get; private set; }
    public int Total { get; private set; }

    GUIStyle style;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Total = FindObjectsOfType<Coin>().Length;
    }

    public void Collect()
    {
        Collected++;
    }

    void OnGUI()
    {
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontSize = 22 };

        GUI.Label(new Rect(20, 75, 400, 30), $"Coins  {Collected} / {Total}", style);
    }
}
