﻿using UnityEngine;

namespace ShipManifest.Windows
{
  internal static class GuiUtils
  {
    internal static string ColorSelector(out string toolTip)
    {
      const float guiWidth = 20;
      const float guiHeight = 20;
      string thisColor = "";
      toolTip = "";

      if (GUILayout.Button(new GUIContent("", "Blue"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "blue";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Black"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "black";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Clear"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "clear";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Cyan"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "cyan";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Gray"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "gray";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Green"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "green";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Magenta"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "magenta";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Red"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "red";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "White"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "white";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      if (GUILayout.Button(new GUIContent("", "Yellow"), SMStyle.ButtonStyle, GUILayout.Width(guiWidth), GUILayout.Height(guiHeight)))
        thisColor = "yellow";
      if (Event.current.type == EventType.Repaint)
      {
        toolTip = GUI.tooltip;
      }
      return thisColor;
    }

    //this block is derived from Kerbal Engineer source. Thanks cybutek!
    internal static void PreventEditorClickthrough(bool visible, Rect position, ref bool lockedInputs)
    {
      bool mouseOverWindow = MouseIsOverWindow(visible, position);
      if (!lockedInputs && mouseOverWindow)
      {
        EditorLogic.fetch.Lock(true, true, true, "SM_Window");
        lockedInputs = true;
      }
      if (!lockedInputs || mouseOverWindow) return;
      EditorLogic.fetch.Unlock("SM_Window");
      lockedInputs = false;
    }

    internal static bool PreventClickthrough(bool visible, Rect position, bool lockedInputs)
    {
      // Still in work.  Not behaving correctly in Flight.  Works fine in Editor and Space Center...
      bool mouseOverWindow = MouseIsOverWindow(visible, position);
      if (!lockedInputs && mouseOverWindow)
      {
        if(HighLogic.LoadedSceneIsFlight)
          InputLockManager.SetControlLock(ControlTypes.UI | ControlTypes.UI_DIALOGS | ControlTypes.GUI, "SM_Window");
        else
        {
          InputLockManager.SetControlLock(ControlTypes.All, "SM_Window");
        }
        lockedInputs = true;
      }
      if (!lockedInputs || mouseOverWindow) return lockedInputs;
      InputLockManager.RemoveControlLock("SM_Window");
      return false;
    }

    private static bool MouseIsOverWindow(bool visible, Rect position)
    {
      return visible
             && position.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    }

  }
}