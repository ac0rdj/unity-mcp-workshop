using UnityEditor;

// Turns on MCP for Unity's auto-start the first time this project opens, so the MCP server comes up with
// the editor (no clicks, and auto-start skips the one-time "Start Local HTTP Server" dialog).
// Leaves the server URL alone: default is http://127.0.0.1:8080, change it in Window > MCP for Unity if needed.
[InitializeOnLoad]
public static class WorkshopPrefs
{
    const string AppliedKey = "ParkourWorkshop.McpPrefsApplied";

    static WorkshopPrefs()
    {
        if (EditorPrefs.GetBool(AppliedKey, false)) return;
        EnableMcpAutoStart();
        EditorPrefs.SetBool(AppliedKey, true);
    }

    [MenuItem("Parkour/Enable MCP Auto-Start")]
    public static void EnableMcpAutoStart()
    {
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
    }
}
