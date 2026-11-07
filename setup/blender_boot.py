# Run by setup/start-blender-mac-linux.sh. Turns on the MCP for Blender add-on and Poly Haven.
import bpy, addon_utils

try:
    addon_utils.enable("blender_mcp", default_set=True, persistent=True)
    bpy.ops.wm.save_userpref()
    print("workshop: MCP for Blender add-on enabled")
except Exception as e:
    print("workshop: could not enable the add-on:", e)


def _tick_polyhaven():
    scene = bpy.context.scene
    try:
        scene.blendermcp_use_polyhaven = True       # Poly Haven box
        scene.blendermcp_auto_start_server = True   # server starts by itself (port 9876)
        print("workshop: Poly Haven on, port", scene.blendermcp_port)
    except Exception as e:
        print("workshop: waiting for the add-on:", e)
        return 1.0  # try again in a second
    return None


bpy.app.timers.register(_tick_polyhaven, first_interval=2.0)
