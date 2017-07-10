﻿using ShipManifest.APIClients;
using ShipManifest.InternalObjects;
using UnityEngine;

namespace ShipManifest.Windows.Tabs.Settings
{
  internal static class TabConfig
  {
    internal static string TxtSaveInterval = SMSettings.SaveIntervalSec.ToString();

    // GUI tooltip and label support
    private static string _toolTip = "";
    private static Rect _rect;
    private static string _label = "";
    private static GUIContent _guiLabel;
    private const float guiRuleWidth = 350;
    private const float guiMaintoggleWidth = 300;

    internal static string ToolTip = "";
    internal static bool ToolTipActive;
    internal static bool ShowToolTips = true;
    private static bool _canShowToolTips = true;

    internal static Rect Position = WindowSettings.Position;

    internal static void Display(Vector2 displayViewerPosition)
    {
      // Reset Tooltip active flag...
      ToolTipActive = false;
      _canShowToolTips = WindowSettings.ShowToolTips && ShowToolTips;

      Position = WindowSettings.Position;
      int scrollX = 20;

      //GUILayout.Label("Configuraton", SMStyle.LabelTabHeader);
      GUILayout.Label(SmUtils.Localize("#smloc_settings_config_000"), SMStyle.LabelTabHeader);
      GUILayout.Label("____________________________________________________________________________________________",
        SMStyle.LabelStyleHardRule, GUILayout.Height(10), GUILayout.Width(guiRuleWidth));

      if (!ToolbarManager.ToolbarAvailable)
      {
        if (SMSettings.EnableBlizzyToolbar)
          SMSettings.EnableBlizzyToolbar = false;
        GUI.enabled = false;
      }
      else
        GUI.enabled = true;

      //_label = "Enable Blizzy Toolbar (Replaces Stock Toolbar)";
      //_toolTip = "Switches the toolbar Icons over to Blizzy's toolbar, if installed.";
      //_toolTip += "\r\nIf Blizzy's toolbar is not installed, option is not selectable.";
      _label = SmUtils.Localize("#smloc_settings_config_001");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_001");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableBlizzyToolbar = GUILayout.Toggle(SMSettings.EnableBlizzyToolbar, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      GUI.enabled = true;
      // UnityStyle Mode
      //_label = "Enable Unity Style GUI Interface";
      //_toolTip = "Changes all window appearances to Unity's Default look (like Mech Jeb).";
      //_toolTip += "\r\nWhen Off, all windows look like KSP style windows.";
      _label = SmUtils.Localize("#smloc_settings_config_002");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_002");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.UseUnityStyle = GUILayout.Toggle(SMSettings.UseUnityStyle, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);
      if (SMSettings.UseUnityStyle != SMSettings.PrevUseUnityStyle)
        SMStyle.WindowStyle = null;

      //_label = "Enable Debug Window";
      //_toolTip = "Turns on or off the SM Debug window.";
      //_toolTip += "\r\nAllows viewing log entries / errors generated by SM.";
      _label = SmUtils.Localize("#smloc_settings_config_003");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_003");
      _guiLabel = new GUIContent(_label, _toolTip);
      WindowDebugger.ShowWindow = GUILayout.Toggle(WindowDebugger.ShowWindow, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      //_label = "Enable Verbose Logging";
      //_toolTip = "Turns on or off Expanded logging in the Debug Window.";
      //_toolTip += "\r\nAids in troubleshooting issues in SM";
      _label = SmUtils.Localize("#smloc_settings_config_004");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_004");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.VerboseLogging = GUILayout.Toggle(SMSettings.VerboseLogging, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      //_label = "Enable SM Debug Window On Error";
      //_toolTip = "When On, Ship Manifest automatically displays the SM Debug window on an error in SM.";
      //_toolTip += "\r\nThis is a troubleshooting aid.";
      _label = SmUtils.Localize("#smloc_settings_config_005");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_005");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.AutoDebug = GUILayout.Toggle(SMSettings.AutoDebug, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      //_label = "Save Error log on Exit";
      //_toolTip = "When On, Ship Manifest automatically saves the SM debug log on game exit.";
      //_toolTip += "\r\nThis is a troubleshooting aid.";
      _label = SmUtils.Localize("#smloc_settings_config_006");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_006");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.SaveLogOnExit = GUILayout.Toggle(SMSettings.SaveLogOnExit, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      // create Limit Error Log Length slider;
      GUILayout.BeginHorizontal();
      //_label = "Error Log Length: ";
      //_toolTip = "Sets the maximum number of error entries stored in the log.";
      //_toolTip += "\r\nAdditional entries will cause first entries to be removed from the log (rolling).";
      //_toolTip += "\r\nSetting this value to '0' will allow unlimited entries.";
      _label = $"{SmUtils.Localize("#smloc_settings_config_007")}:";
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_007");
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(110));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);
      SMSettings.ErrorLogLength = GUILayout.TextField(SMSettings.ErrorLogLength, GUILayout.Width(40));
      GUILayout.Label("(lines)", GUILayout.Width(50));
      GUILayout.EndHorizontal();

      //_label = "Enable AutoSave Settings";
      //_toolTip = "When On, SM automatically saves changes made to settings on a regular interval.";
      _label = SmUtils.Localize("#smloc_settings_config_008");
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_008");
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.AutoSave = GUILayout.Toggle(SMSettings.AutoSave, _guiLabel, GUILayout.Width(guiMaintoggleWidth));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);

      GUILayout.BeginHorizontal();
      //_label = "Save Interval: ";
      //_toolTip = "Sets the time (in seconds) between automatic saves.";
      //_toolTip += "\r\nAutosave Settings must be enabled.";
      _label = $"{SmUtils.Localize("#smloc_settings_config_009")}:";
      _toolTip = SmUtils.Localize("#smloc_settings_config_tt_009");
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(110));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && _canShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, GUI.tooltip, ref ToolTipActive, scrollX);
      TxtSaveInterval = GUILayout.TextField(TxtSaveInterval, GUILayout.Width(40));
      //GUILayout.Label("(sec)", GUILayout.Width(40));
      GUILayout.Label(SmUtils.Localize("#smloc_settings_config_010"), GUILayout.Width(40));
      GUILayout.EndHorizontal();
    }
  }
}