﻿using System;
using System.Globalization;
using ShipManifest.APIClients;
using ShipManifest.Modules;
using UnityEngine;

namespace ShipManifest.Windows.Tabs
{
  internal static class TabRealism
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
      var scrollX = 20;
      var scrollY = 50;

      GUI.enabled = true;
      GUILayout.Label(
        !SMSettings.LockSettings
          ? "Realism Settings / Options"
          : "Realism Settings / Options  (Locked.  To unlock, edit SMSettings.dat file)", SMStyle.LabelTabHeader);
      GUILayout.Label("____________________________________________________________________________________________", SMStyle.LabelStyleHardRule, GUILayout.Height(10), GUILayout.Width(350));

      var isEnabled = !SMSettings.LockSettings;
      // Realism Mode
      GUI.enabled = isEnabled;
      _label = "Enable Realism Mode";
      _toolTip = "Turns on/off Realism Mode.";
      _toolTip += "\r\nWhen ON, causes changes in the interface and limits";
      _toolTip += "\r\nyour freedom to do things that would not be 'Realistic'.";
      _toolTip += "\r\nWhen Off, Allows Fills, Dumps, Repeating Science,";
      _toolTip += "\r\ninstantaneous Xfers, Crew Xfers anywwhere, etc.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.RealismMode = GUILayout.Toggle(SMSettings.RealismMode, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      // EnableCrew Mode
      GUI.enabled = isEnabled;
      GUILayout.BeginHorizontal();
      _label = "Enable Crew Xfers";
      _toolTip = "Turns on/off Crew transfers.";
      _toolTip += "\r\nWhen ON, The Crew option will appear in your resource list.";
      _toolTip += "\r\nWhen Off, Crew transfers are not possible.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableCrew = GUILayout.Toggle(SMSettings.EnableCrew, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.EndHorizontal();

      if (!SMSettings.EnableCrew && HighLogic.LoadedSceneIsFlight)
      {
        if (SMAddon.SmVessel.SelectedResources.Contains("Crew"))
        {
          // Clear Resource selection.
          SMHighlighter.ClearResourceHighlighting(SMAddon.SmVessel.SelectedResourcesParts);
          SMAddon.SmVessel.SelectedResources.Clear();
          WindowTransfer.ShowWindow = false;
        }
      }

      // Enable stock Crew Xfer Override
      GUI.enabled = isEnabled;
      GUILayout.BeginHorizontal();
      _label = "Override Stock Crew Xfers";
      _toolTip = "Turns on/off Overriding the stock Crew Transfer mechanism with the SM style.";
      _toolTip += "\r\nWhen ON (requires Realism Mode On), stock crew transfers will behave like SM style transfers.";
      _toolTip += "\r\nWhen Off (or Realism is off), Stock Crew transfers behave normally.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Space(20);
      SMSettings.OverrideStockCrewXfer = GUILayout.Toggle(SMSettings.OverrideStockCrewXfer, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      GUILayout.EndHorizontal();

      // EnableCLS Mode
      GUILayout.BeginHorizontal();
      GUILayout.Space(20);
      if (!SMSettings.EnableCrew || !SMSettings.ClsInstalled)
        GUI.enabled = false;
      else
        GUI.enabled = isEnabled;
      _label = "Enable CLS  (Connected Living Spaces)";
      _toolTip = "Turns on/off Connected Living space support.";
      _toolTip += "\r\nWhen ON, Crew can only be xfered to a part in the same 'Living Space'.";
      _toolTip += "\r\nWhen Off, Crew transfers are possible to any part that can hold a kerbal.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableCls = GUILayout.Toggle(SMSettings.EnableCls, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      if (SMSettings.EnableCls != SMSettings.PrevEnableCls && HighLogic.LoadedSceneIsFlight)
      {
        if (!SMSettings.EnableCls)
          SMHighlighter.HighlightClsVessel(false, true);
        else if (SMAddon.SmVessel.SelectedResources.Contains("Crew"))
        {
          // Update spaces and reassign the resource to observe new settings.
          SMAddon.UpdateClsSpaces();
          SMAddon.SmVessel.SelectedResources.Clear();
          SMAddon.SmVessel.SelectedResources.Add("Crew");
        }
      }

      // EnableScience Mode
      GUILayout.BeginHorizontal();
      GUI.enabled = isEnabled;
      _label = "Enable Science Xfers";
      _toolTip = "Turns on/off Science Xfers.";
      _toolTip += "\r\nWhen ON, Science transfers are possible and show up in the Resource list.";
      _toolTip += "\r\nWhen Off, Science transfers will not appear in the resource list.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableScience = GUILayout.Toggle(SMSettings.EnableScience, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      if (!SMSettings.EnableScience && HighLogic.LoadedSceneIsFlight)
      {
        // Clear Resource selection.
        if (SMAddon.SmVessel.SelectedResources.Contains("Science"))
          SMAddon.SmVessel.SelectedResources.Clear();
      }

      // EnableResources Mode
      GUILayout.BeginHorizontal();
      GUI.enabled = isEnabled;
      _label = "Enable Resource Xfers";
      _toolTip = "Turns on/off Resource Xfers.";
      _toolTip += "\r\nWhen ON, Resource transfers are possible and display in Manifest Window.";
      _toolTip += "\r\nWhen Off, Resources will not appear in the resource list.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableResources = GUILayout.Toggle(SMSettings.EnableResources, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      if (!SMSettings.EnableResources && HighLogic.LoadedSceneIsFlight)
      {
        // Clear Resource selection.
        if (SMAddon.SmVessel.SelectedResources.Count > 0 && !SMAddon.SmVessel.SelectedResources.Contains("Crew") && !SMAddon.SmVessel.SelectedResources.Contains("Science"))
          SMAddon.SmVessel.SelectedResources.Clear();
      }

      // Set Gui.enabled for child settings to resources...
      GUI.enabled = SMSettings.EnableResources && isEnabled;

      // EnablePFResources Mode
      GUILayout.BeginHorizontal();
      GUILayout.Space(20);
      _label = "Enable Resources in Pre-Flight";
      _toolTip = "Turns on/off Fill and Empty Resources when in preflight.";
      _toolTip += "\r\nWhen ON, Fill & Dump resources vessel wide are possible (shows in the Resource list).";
      _toolTip += "\r\nWhen Off, Fill and Dump Resources vessel wide will not appear in the resource list.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnablePfResources = GUILayout.Toggle(SMSettings.EnablePfResources, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      // EnableXferCost Mode
      GUILayout.BeginHorizontal();
      GUILayout.Space(20);
      _label = "Enable Resource Xfer Costs";
      _toolTip = "Turns on/off ElectricCharge cost forResource Xfers.";
      _toolTip += "\r\nWhen ON, Resource transfers will cost ElectricCharge.";
      _toolTip += "\r\nWhen Off, Resources Xfers are Free (original SM behavior).";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.EnableXferCost = GUILayout.Toggle(SMSettings.EnableXferCost, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      // Resource Xfer EC cost
      float newCost;
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      _label = "Resource Flow Cost:";
      _toolTip = "Sets the Electrical cost of resource Xfers when Realism Mode is on.";
      _toolTip += "\r\nThe higher the number the more ElectricCharge used.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(130), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);

      // Lets parse the string to allow decimal points.
      StrFlowCost = SMSettings.FlowCost.ToString(CultureInfo.InvariantCulture);
      // add the decimal point if it was typed.
      StrFlowCost = Utilities.GetStringDecimal(StrFlowCost);
      // add the zero if it was typed.
      StrFlowCost = Utilities.GetStringZero(StrFlowCost);

      StrFlowCost = GUILayout.TextField(StrFlowCost, 20, GUILayout.Height(20), GUILayout.Width(80));
      _label = "EC/Unit";
      _toolTip = "Sets the Electrical cost of resource Xfers when Realism Mode is on.";
      _toolTip += "\r\nThe higher the number the more ElectricCharge used.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(80), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      // update decimal bool 
      Utilities.SetStringDecimal(StrFlowCost);
      //update zero bool 
      Utilities.SetStringZero(StrFlowCost);

      if (float.TryParse(StrFlowCost, out newCost))
        SMSettings.FlowCost = newCost;

      // create xfer Flow Rate slider;
      // Lets parse the string to allow decimal points.
      var strFlowRate = SMSettings.FlowRate.ToString(CultureInfo.InvariantCulture);
      var strMinFlowRate = SMSettings.MinFlowRate.ToString(CultureInfo.InvariantCulture);
      var strMaxFlowRate = SMSettings.MaxFlowRate.ToString(CultureInfo.InvariantCulture);
      var strMaxFlowTime = SMSettings.MaxFlowTimeSec.ToString();

      float newRate;

      // Resource Flow Rate
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      _label = "Resource Flow Rate:";
      _toolTip = "Sets the rate that resources Xfer when Realism Mode is on.";
      _toolTip += "\r\nThe higher the number the faster resources move.";
      _toolTip += "\r\nYou can also use the slider below to change this value.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(130), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      strFlowRate = GUILayout.TextField(strFlowRate, 20, GUILayout.Height(20), GUILayout.Width(80));
      _label = "Units/Sec";
      _toolTip = "Sets the rate that resources Xfer when Realism Mode is on.";
      _toolTip += "\r\nThe higher the number the faster resources move.";
      _toolTip += "\r\nYou can also use the slider below to change this value.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(80), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();
      if (float.TryParse(strFlowRate, out newRate))
        SMSettings.FlowRate = (int)newRate;

      // Resource Flow rate Slider
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      GUILayout.Label(SMSettings.MinFlowRate.ToString(CultureInfo.InvariantCulture), GUILayout.Width(10), GUILayout.Height(20));
      SMSettings.FlowRate = GUILayout.HorizontalSlider((float)SMSettings.FlowRate, (float)SMSettings.MinFlowRate, (float)SMSettings.MaxFlowRate, GUILayout.Width(240), GUILayout.Height(20));
      _label = SMSettings.MaxFlowRate.ToString(CultureInfo.InvariantCulture);
      _toolTip = "Slide control to change the Resource Flow Rate shown above.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(40), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();

      // Min Flow Rate for Slider
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      _label = " - Min Flow Rate:";
      _toolTip = "Sets the lowest range (left side) on the Flow rate Slider control.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(130), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      strMinFlowRate = GUILayout.TextField(strMinFlowRate, 20, GUILayout.Height(20), GUILayout.Width(80));
      _label = "Units/Sec";
      _toolTip = "Sets the lowest range (left side) on the Flow rate Slider control.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(80), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();
      if (float.TryParse(strMinFlowRate, out newRate))
        SMSettings.MinFlowRate = (int)newRate;

      // Max Flow Rate for Slider
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      _label = " - Max Flow Rate:";
      _toolTip = "Sets the highest range (right side) on the Flow rate Slider control.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(130), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      strMaxFlowRate = GUILayout.TextField(strMaxFlowRate, 20, GUILayout.Height(20), GUILayout.Width(80));
      _label = "Units/Sec";
      _toolTip = "Sets the highest range (right side) on the Flow rate Slider control.";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(80), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();
      if (float.TryParse(strMaxFlowRate, out newRate))
        SMSettings.MaxFlowRate = (int)newRate;

      // Max Flow Time 
      GUILayout.BeginHorizontal();
      GUILayout.Space(30);
      _label = " - Max Flow Time:";
      _toolTip = "Sets the maximum duration (in sec) of a resource transfer.";
      _toolTip += "\r\nWorks in conjunction with the Flow rate.  if time it would take";
      _toolTip += "\r\n to move a resource exceeds this number, this number will be used";
      _toolTip += "\r\n to calculate an adjusted flow rate.";
      _toolTip += "\r\n(protects your from 20 minute Xfers)";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(130), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      strMaxFlowTime = GUILayout.TextField(strMaxFlowTime, 20, GUILayout.Height(20), GUILayout.Width(80));
      _label = "Sec";
      _toolTip = "Sets the maximum duration (in sec) of a resource transfer.";
      _toolTip += "\r\nWorks in conjunction with the Flow rate.  if time it would take";
      _toolTip += "\r\n to move a resource exceeds this number, this number will be used";
      _toolTip += "\r\n to calculate an adjusted flow rate.";
      _toolTip += "\r\n(protects your from 20 minute Xfers)";
      _guiLabel = new GUIContent(_label, _toolTip);
      GUILayout.Label(_guiLabel, GUILayout.Width(80), GUILayout.Height(20));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
      GUILayout.EndHorizontal();
      if (float.TryParse(strMaxFlowTime, out newRate))
        SMSettings.MaxFlowTimeSec = (int)newRate;

      // reset gui.enabled to default
      GUI.enabled = isEnabled;

      // LockSettings Mode
      _label = "Lock Settings  (If set ON, disable in config file)";
      _toolTip = "Locks the settings in this section so they cannot be altered in game.";
      _toolTip += "\r\nTo turn off Locking you MUST edit the Config.xml file.";
      _guiLabel = new GUIContent(_label, _toolTip);
      SMSettings.LockSettings = GUILayout.Toggle(SMSettings.LockSettings, _guiLabel, GUILayout.Width(300));
      _rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(_rect, Position, GUI.tooltip, ref ToolTipActive, scrollX, scrollY - displayViewerPosition.y);
    }

    private static Rect GetRect(Rect  _rect, float scrollX, float scrollY)
    {
      var new_rect = new Rect(scrollX + _rect.x, scrollY + _rect.y, _rect.width, _rect.height);
      return new_rect;
    }
  }
}
