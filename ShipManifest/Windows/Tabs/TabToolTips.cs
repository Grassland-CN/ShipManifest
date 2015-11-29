﻿using System;
using System.Globalization;
using ShipManifest.APIClients;
using ShipManifest.Modules;
using UnityEngine;

namespace ShipManifest.Windows.Tabs
{
  internal static class TabToolTips
  {
    internal static string StrFlowCost = "0";

    // GUI tooltip and label support
    private static string _toolTip = "";
    private static Rect _rect;
    private static string _label = "";
    private static GUIContent _guiLabel;

    internal static string ToolTip = "";
    internal static bool ToolTipActive;
    internal static bool ShowToolTips = true;
    internal static Rect Position = WindowSettings.Position;

    internal static void Display(Vector2 displayViewerPosition)
    {
      // Reset Tooltip active flag...
      ToolTipActive = false;

      Position = WindowSettings.Position;
      var scrollX = Position.x + 20;
      var scrollY = Position.y + 50 - displayViewerPosition.y;

      // Enable Tool Tips
      GUI.enabled = true;
      GUILayout.Label("ToolTips", SMStyle.LabelTabHeader);
      GUILayout.Label("____________________________________________________________________________________________", SMStyle.LabelStyleHardRule, GUILayout.Height(10), GUILayout.Width(350));

      _label = "Enable Tool Tips";
      _toolTip = "Turns tooltips On or Off.";
      _toolTip += "\r\nThis is a global setting for all windows/tabs";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.ShowToolTips = GUILayout.Toggle(SMSettings.ShowToolTips, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUI.enabled = SMSettings.ShowToolTips;

      GUILayout.BeginHorizontal();
      _label = "Debugger Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Debugger Window only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      WindowDebugger.ShowToolTips = GUILayout.Toggle(WindowDebugger.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Manifest Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Manifest Window only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      WindowManifest.ShowToolTips = GUILayout.Toggle(WindowManifest.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Transfer Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Manifest Window only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      WindowTransfer.ShowToolTips = GUILayout.Toggle(WindowTransfer.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Settings Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      ShowToolTips = GUILayout.Toggle(ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUI.enabled = SMSettings.ShowToolTips && WindowControl.ShowToolTips;

      GUILayout.BeginHorizontal();
      _label = "Realism Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's Realism Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabRealism.ShowToolTips = GUILayout.Toggle(TabRealism.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Highlghting Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's Highlighting Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabHighlight.ShowToolTips = GUILayout.Toggle(TabHighlight.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "ToolTips Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's ToolTips Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      ShowToolTips = GUILayout.Toggle(ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Sounds Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's Sounds Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabLight.ShowToolTips = GUILayout.Toggle(TabLight.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Config Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's Config Tab only.";
      _toolTip += "Requires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      TabConfig.ShowToolTips = GUILayout.Toggle(TabConfig.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.BeginHorizontal();
      _label = "Installed Mods Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Settings Window's Installed Mods Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Settings Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      TabInstalledMods.ShowToolTips = GUILayout.Toggle(TabInstalledMods.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUI.enabled = true;
      GUILayout.BeginHorizontal();
      _label = "Roster Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Roster Window only.";
      _toolTip += "Requires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      WindowRoster.ShowToolTips = GUILayout.Toggle(WindowRoster.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.BeginHorizontal();
      _label = "Control Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Control Window only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      WindowControl.ShowToolTips = GUILayout.Toggle(WindowControl.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUI.enabled = SMSettings.ShowToolTips && WindowControl.ShowToolTips;

      GUILayout.BeginHorizontal();
      _label = "Hatch Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Control Window's Hatch Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Control Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabHatch.ShowToolTips = GUILayout.Toggle(TabHatch.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.BeginHorizontal();
      _label = "Solar Tab Window Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Control Window's Solar Panels Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Control Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabSolarPanel.ShowToolTips = GUILayout.Toggle(TabSolarPanel.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.BeginHorizontal();
      _label = "Antenna Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Control Window's Antennas Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Control Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabAntenna.ShowToolTips = GUILayout.Toggle(TabAntenna.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.BeginHorizontal();
      _label = "Light Tab Tool Tips";
      _toolTip = "Turns tooltips On or Off for the Control Window's Lights Tab only.";
      _toolTip += "\r\nRequires global ToolTips setting to be enabled.";
      _toolTip += "\r\nAlso requires Control Window tooltips to be enabled.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(40);
      TabLight.ShowToolTips = GUILayout.Toggle(TabLight.ShowToolTips, _guiLabel, GUILayout.Width(300));
      GUILayout.EndHorizontal();
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUI.enabled = true;
    }
  }
}
