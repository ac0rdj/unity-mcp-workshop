📑 **Workshop slides:** [unity-mcp-workshop-deck.pptx](docs/unity-mcp-workshop-deck.pptx) (PowerPoint, 39 MB)

# Unity MCP Workshop: ParkourBase

A tiny first-person parkour game for learning **MCP**. You talk to an AI agent (Claude Code), and it works inside your running Unity editor.

![Art track example](docs/images/track-art.jpg)

Everything here is **free**. You don't need any API keys.

---

## Setup (about 10 minutes)

Do the steps in order. Each step says what you should see.

### 1. Install Unity 6
Install **Unity Hub**. In Hub, go to **Installs → Install Editor** and pick **Unity 6** (any `6000.x` version).
- Windows / Mac: https://unity.com/download
- Linux (Ubuntu): https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux

Do this before the workshop, because the download is large.

Your version doesn't need to match. The project was made with `6000.0.47f1`. When Unity asks about a different version, choose **Continue** or **Upgrade**. Never choose Skip.

### 2. Download this project
- **Easiest:** click the green **Code** button, choose **Download ZIP**, and unzip it somewhere simple (for example `Documents`).
- **With git:**
  ```
  git clone https://github.com/ac0rdj/unity-mcp-workshop.git
  ```

### 3. Open it in Unity
In Unity Hub, click **Add → Add project from disk**, pick the folder, and open it.

The first open takes a few minutes. The MCP packages are already included, and the MCP server starts by itself. You're done when the **Console** at the bottom of Unity shows no red errors.

### 4. Run the setup
This installs anything missing and connects Claude Code to Unity, Blender and the Unity docs. It's safe to run again.

- **Windows:** open the `setup` folder and double-click **`setup-windows.cmd`**.
- **Mac / Linux:** open Terminal in the project folder and paste:
  ```
  bash setup/setup-mac-linux.sh
  ```

Every line should show ✔ or `[ok]`. A `!` is a tip, not an error. If a line shows ✘ or `[x]`, do what it says, then run the setup again.

> **Antivirus blocked it?** You can skip the scripts. See [Antivirus blocks the scripts](#antivirus-blocks-the-scripts) below.

### 5. Check everything
- **Windows:** double-click **`setup\doctor-windows.cmd`**.
- **Mac / Linux:**
  ```
  bash setup/doctor-mac-linux.sh
  ```

It prints one line per check. Green means ready. Each red line tells you the exact fix. It changes nothing, so run it as often as you like.

### 6. Start Claude Code
Always start Claude from the project, never from another folder. The connections belong to the project folder, and Claude can't see Unity from anywhere else.

- **Windows:** double-click **`setup\start-windows.cmd`**.
- **Mac / Linux:** open a **new** terminal in the project folder and paste:
  ```
  bash setup/start-mac-linux.sh
  ```

The first time, Claude asks you to log in. Then it asks if you trust the folder. Pick **Yes, I trust this folder**.

![Trust prompt](docs/images/05a-claude-trust.png)

Type `/mcp`. You should see `UnityMCP`, `unity-official`, `blender` and `context7` with ✔:

![/mcp](docs/images/05-claude-mcp.png)

### 7. Approve Unity's own MCP (once)
The first time Claude connects, Unity asks you to approve it. Go to **Edit → Project Settings → AI → Unity MCP Server**. Click **Accept** on the notice. Then approve Claude under **Pending Connections**.

![Unity MCP Server settings](docs/images/03-official-mcp.png)

### 8. Say hi
Type, or say with voice dictation:
```
List the root objects in the open Unity scene and tell me if there are any console errors.
```
If Claude reads your scene back, you're ready. 🎉

![Smoke test](docs/images/06-smoke-test.png)

---

## Optional: Blender (Art track)
1. Install Blender 4.x or newer from https://www.blender.org/download/ and open it once.
2. Run the setup again (step 4). It installs the Blender add-on.
3. Start Blender with everything switched on:
   - **Windows:** double-click **`setup\start-blender-windows.cmd`**.
   - **Mac / Linux:** `bash setup/start-blender-mac-linux.sh`

Blender opens with the add-on on, Poly Haven ticked and the server running.

**If that doesn't work, do it by hand:**
1. In Blender, go to **Edit → Preferences → Add-ons**, search `MCP`, and tick **Interface: MCP for Blender**.
2. In the 3D view, press **N**, open the **MCP for Blender** tab, tick **Poly Haven**, and click **Start MCP Server**.

![Blender panel](docs/images/04-blender-panel.png)

Poly Haven gives you free CC0 models, textures and skies. You don't need an account.

---

## Something not working?
First, run the check from step 5. Each red line says how to fix it.

| Problem | Fix |
|---|---|
| Claude asks "which scene?" or reads `.unity` files itself | You started Claude in the wrong folder. Close it and use the start script from step 6. |
| `claude` or `uv` "not found" right after setup | Close the terminal and open a new one. |
| Unity asks about the project's version | Normal. Choose **Continue** or **Upgrade**, never Skip. |
| The Unity install or first open stalls | Your disk may be full. Empty the Trash or Recycle Bin, then install again from Unity Hub. |
| `UnityMCP` ✘ in `/mcp` | Keep Unity open. **Window → MCP for Unity** should say *Session Active*. If not, click **Start Server**. |
| `unity-official` missing | Open the project in Unity once, then run the setup again. |
| `unity-official` ✘ | Approve Claude under **Project Settings → AI → Unity MCP Server → Pending Connections**. |
| `blender` ✘ | Start Blender with the Blender step above (port 9876). |
| Port 8080 is already in use | Run the setup again with another port: `--port 8092` on Mac / Linux, `setup-windows.cmd -Port 8092` on Windows. Then type `http://127.0.0.1:8092` into **Window → MCP for Unity**. |
| Windows: "running scripts is disabled" | Use `setup-windows.cmd`. It runs the script without changing your settings. |
| Mac: "permission denied" on a script | Run it with `bash`, for example `bash setup/setup-mac-linux.sh`. Don't use `./`. |

### Antivirus blocks the scripts
Antivirus and SmartScreen often block `.cmd` and PowerShell files. Don't turn your antivirus off. Skip the scripts instead. Nothing here needs them.

1. **Install the tools.** Open Windows Terminal or PowerShell and paste one line at a time:
   ```
   winget install --id Git.Git -e
   winget install --id astral-sh.uv -e
   winget install --id Anthropic.ClaudeCode -e
   ```
   Then close the terminal and open a new one.
2. **Open a terminal in the project folder.** In File Explorer, right-click the folder and choose **Open in Terminal**.
3. **Connect the tools.** Paste the `claude mcp add` lines from *Manual setup* below.
4. **Start Claude** in that same terminal: `claude`.

If Windows says "Windows protected your PC", click **More info → Run anyway**. If your antivirus keeps removing the `setup` folder, add the project folder as an exclusion.

### Manual setup
Run these in the project folder. Install [uv](https://docs.astral.sh/uv/getting-started/installation/) and [Claude Code](https://code.claude.com/docs/en/setup) first.
```
claude mcp add --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp add --transport http context7 https://mcp.context7.com/mcp
claude mcp add blender -- uvx mcp-for-blender
```
Then add Unity's official relay, using the line for your computer:
```
# Windows (PowerShell)
claude mcp add unity-official -- "$env:USERPROFILE\.unity\relay\relay_win.exe" --mcp
# Mac (Apple Silicon)
claude mcp add unity-official -- ~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp
# Mac (Intel)
claude mcp add unity-official -- ~/.unity/relay/relay_mac_x64.app/Contents/MacOS/relay_mac_x64 --mcp
# Linux
claude mcp add unity-official -- ~/.unity/relay/relay_linux --mcp
```

---

## The game
Open `Assets/Scenes/Parkour.unity` and press Play.

| Input | Action |
|---|---|
| WASD | Move |
| Space | Jump |
| Left click | **Blast** yourself in the direction you're looking (1 charge, recharges on landing) |
| R | Restart |
| Esc | Free the cursor (click to recapture it) |

**Course:** warm-up jumps → checkpoint → long gap (jump, then blast forward and slightly up) → wall (jump, then blast upward) → checkpoint → final gap to the flag.

### Project map (useful when prompting an agent)
| Thing | Where |
|---|---|
| Movement | `Assets/Scripts/PlayerMotor.cs` (keeps momentum in the air) |
| Blast ability | `Assets/Scripts/Blaster.cs` (`blastSpeed`, `upwardBias`, `maxCharges`) |
| Timer / HUD | `Assets/Scripts/GameHUD.cs` (IMGUI, no UI packages) |
| Checkpoints / finish | `Checkpoint.cs`, `Goal.cs` |
| Level layout | Scene objects under `Level`. You can regenerate it from code with **Parkour › Rebuild Parkour Scene** (`Assets/Editor/LevelBuilder.cs`). ⚠️ Rebuilding overwrites manual edits. |
| Empty slots for you | `Props` (scenery) and `Collectibles` (coins etc.) |
| MCP demo scene | `Assets/Scenes/RockDemo.unity`: a grass plane and an empty `Rocks` object |
| Track examples | `Assets/Scenes/Examples/`: `Track_Art` (sky islands + waterfall), `Track_Mechanics`, `Track_Polish` |

### Challenge ideas
- 🎨 **Art:** fill `Props` with scenery from Poly Haven (through Blender MCP), add a skybox, lighting and mood.
- ⚙️ **Mechanics:** coins in `Collectibles` with a score on the HUD, a double blast (`maxCharges = 2`), moving platforms, a new course section.
- 🧹 **Polish:** tune the jump and blast feel, add blast sound and particles, add a best-time save.

Starter prompts: [DEMO-PROMPTS.md](DEMO-PROMPTS.md).

## Credits
3D models, textures and the sky are CC0 from [Poly Haven](https://polyhaven.com). MCP servers: [MCP for Unity](https://github.com/CoplayDev/unity-mcp), Unity AI Assistant (official MCP), [MCP for Blender](https://pypi.org/project/mcp-for-blender/), [Context7](https://context7.com).
