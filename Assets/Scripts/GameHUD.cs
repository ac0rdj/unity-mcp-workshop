using UnityEngine;
using UnityEngine.SceneManagement;

// Minimal IMGUI HUD: timer, crosshair, blaster charges, finish screen. No UI packages needed.
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public Blaster blaster;

    float startTime;
    float finishTime = -1f;
    GUIStyle big, small;

    void Awake()
    {
        Instance = this;
        startTime = Time.time;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Finish()
    {
        if (finishTime < 0f) finishTime = Time.time - startTime;
    }

    void OnGUI()
    {
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        }

        float elapsed = finishTime >= 0f ? finishTime : Time.time - startTime;
        GUI.Label(new Rect(20, 15, 400, 30), $"Time  {elapsed:0.00}s", small);

        if (blaster)
        {
            string pips = new string('●', blaster.Charges) + new string('○', blaster.MaxCharges - blaster.Charges);
            GUI.Label(new Rect(20, 45, 400, 30), $"Blaster  {pips}", small);
        }
        GUI.Label(new Rect(20, Screen.height - 40, 900, 30), "WASD move · Space jump · Click blast (where you look) · R restart · Esc cursor", small);

        GUI.Label(new Rect(Screen.width / 2f - 10, Screen.height / 2f - 15, 20, 30), "+", small);

        if (finishTime >= 0f)
            GUI.Label(new Rect(0, Screen.height / 2f - 100, Screen.width, 80), $"FINISHED  {finishTime:0.00}s\nPress R to retry", big);
    }
}
