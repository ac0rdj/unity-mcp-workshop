📑 **Workshop slides:** [unity-mcp-workshop-deck.pptx](docs/unity-mcp-workshop-deck.pptx) (PowerPoint, 39 MB)

# Unity MCP Workshop: ParkourBase

A tiny first-person parkour game for learning **MCP**: you talk to an AI agent (Claude Code), and it works inside your running Unity editor for you.

![Art track example](docs/images/track-art.jpg)

Everything here is **free**. You don't need any API keys.

---

## Setup (about 10 minutes)

### 1. Install Unity 6
Install **Unity Hub**, then in Hub go to **Installs → Install Editor → Unity 6** (any `6000.x` version).
- Windows / Mac: https://unity.com/download
- Linux (Ubuntu): https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux

The project was made with `6000.0.47f1`, but any Unity 6 works. When Hub or Unity warns that the version is different, choose **Continue** / **Upgrade** (never Skip).

> **Free about 15 GB of disk first.** The editor download is about 7 GB, and Unity's first import needs a few GB more. A full disk mid-install leaves a broken editor.
>
> A different Unity 6 makes Unity update a few package files on the first open. That's normal. `6000.0.47f1` and `6000.6.5f1` both ran every demo in testing.

### 2. Download this project
- **Easiest:** green **Code** button → **Download ZIP**, then unzip it somewhere simple (e.g. `Documents`).
- **With git:**
  ```
  git clone https://github.com/ac0rdj/unity-mcp-workshop.git
  ```

### 3. Open it in Unity
Unity Hub → **Add** → **Add project from disk** → pick the folder you just downloaded → open it.
The first open takes a few minutes. Both MCP packages are already included, and the MCP server starts by itself.

### 4. Run the setup script
This installs anything missing (uv, Claude Code, and Git on Windows) and connects Claude Code to Unity, Blender and the Unity docs. It's safe to run again any time.

**Windows:** open the project folder, then open `setup` and double-click **`setup-windows.cmd`**.

**Mac / Linux:** open Terminal in the project folder and paste:
```
bash setup/setup-mac-linux.sh
```

Every line should show ✔ / `[ok]`. A `!` is a tip, not an error. If anything shows ✘ / `[x]`, fix what it says and run the script again.

**Mac / Linux: check everything.** After Unity has opened the project, run:
```
bash setup/doctor-mac-linux.sh
```
It prints one line per check (Unity version, disk space, internet, login, the four connections, Unity and Blender running). Every ✘ line tells you the exact fix. It changes nothing, so run it as often as you like, and again when you arrive.

> **Port already in use?** The script will tell you. Run it again with another port (`--port 8092` on Mac/Linux, `setup-windows.cmd -Port 8092` on Windows), then type the same URL (`http://127.0.0.1:8092`) into **Window → MCP for Unity**.

### 5. Start Claude Code
The connections are saved **per folder**. Claude Code started anywhere else can't see Unity, and it quietly falls back to reading scene files by hand. So always start it from the project:

**Mac / Linux** (open a **new** terminal; this also repairs a missing Unity connection):
```
bash setup/start-mac-linux.sh
```
**Windows:** open a **new** terminal *in the project folder*, then:
```
claude
```
The first time, it asks you to log in, then asks if you trust the folder. Pick **Yes, I trust this folder**.

![Trust prompt](docs/images/05a-claude-trust.png)

Type `/mcp`. You should see `UnityMCP`, `unity-official`, `blender` and `context7` with ✔:

![/mcp](docs/images/05-claude-mcp.png)

### 6. Unity's own MCP: approve it once
The first time Claude connects, Unity asks you to approve it: **Edit → Project Settings → AI → Unity MCP Server**, click **Accept** on the notice, then approve Claude under **Pending Connections**.

![Unity MCP Server settings](docs/images/03-official-mcp.png)

### 7. Say hi
Type (or say, with voice dictation):
```
List the root objects in the open Unity scene and tell me if there are any console errors.
```
If it reads your scene back, you're ready. 🎉

![Smoke test](docs/images/06-smoke-test.png)

---

## Optional: Blender (Art track)
1. Install Blender from https://www.blender.org/download/ (4.x or newer) and open it once.
2. Run the setup script again. It installs the Blender add-on.
3. **Mac / Linux, one command:** `bash setup/start-blender-mac-linux.sh` opens Blender with the add-on on, Poly Haven ticked and the server running. Done.

**By hand (Windows, or if the command fails):**
3. In Blender: **Edit → Preferences → Add-ons**, search `MCP`, tick **Interface: MCP for Blender**.
4. In the 3D view press **N** → **MCP for Blender** tab → tick **Poly Haven** → **Start MCP Server**.

![Blender panel](docs/images/04-blender-panel.png)

Poly Haven gives you free CC0 models, textures and skies with no account.

---

## Troubleshooting
| Problem | Fix |
|---|---|
| `claude` / `uv` "not found" right after setup | Close the terminal and open a new one, so it picks up the newly installed tools. |
| Claude asks "which scene?", or reads `.unity` files itself | You started `claude` in the wrong folder. Close it and run `bash setup/start-mac-linux.sh` (Windows: open the terminal in the project folder). |
| Not sure what's wrong | Mac / Linux: `bash setup/doctor-mac-linux.sh`. Every ✘ line says how to fix it. |
| Unity install or import fails halfway | Almost always a full disk. Free 15 GB, delete the half-installed editor folder in Unity Hub, install again. |
| Unity asks to upgrade or change the project's version | Normal when your Unity 6 differs from `6000.0.47f1`. Choose **Continue** / **Upgrade** (not Skip). |
| `UnityMCP` ✘ in `/mcp` | Keep Unity open. **Window → MCP for Unity** should say *Session Active*. If not, click **Start Server**. |
| `unity-official` missing | Open the project in Unity once (that installs Unity's relay), then run the setup script again. |
| `unity-official` ✘ | Approve Claude under **Project Settings → AI → Unity MCP Server → Pending Connections**. |
| `blender` ✘ | Blender must be open with **Start MCP Server** clicked (port 9876). |
| Windows: "running scripts is disabled" | Use `setup-windows.cmd`. It runs the script without changing your settings. |
| Mac: "permission denied" on the script | Run it with `bash setup/setup-mac-linux.sh` (don't use `./`). |

### Manual setup (if you'd rather not run a script)
Run these in the project folder after installing [uv](https://docs.astral.sh/uv/getting-started/installation/) and [Claude Code](https://code.claude.com/docs/en/setup):
```
claude mcp add --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp add --transport http context7 https://mcp.context7.com/mcp
claude mcp add blender -- uvx mcp-for-blender
```
Then add Unity's official relay with the line for your computer:
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
