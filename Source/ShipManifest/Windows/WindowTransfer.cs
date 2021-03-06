using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ShipManifest.APIClients;
using ShipManifest.InternalObjects;
using ShipManifest.Modules;
using ShipManifest.Process;
using UnityEngine;

namespace ShipManifest.Windows
{
  internal static class WindowTransfer
  {
    #region Properties

    internal static string Title = "";
    internal static Rect Position = new Rect(0, 0, 0, 0);
    internal static bool ShowWindow;
    internal static bool ToolTipActive;
    internal static bool ShowToolTips = true;

    internal static string ToolTip = "";
    internal static string XferToolTip = ""; // value filled by SMConditions.CanCrewBeXferred
    internal static string EvaToolTip = ""; // value filled by SMConditions.CanCrewBeXferred

    // Switches for List Viewers
    internal static bool ShowSourceVessels;
    internal static bool ShowTargetVessels;
    internal static Rect SelectBox = new Rect(0, 0, 300, 100);
    internal static Rect DetailsBox = new Rect(0,0, 300, 120);

    // vessel mode crew selection vars.
    internal static bool SelectAllFrom;
    internal static bool SelectAllTo;
    internal static bool TouristsOnlyFrom;
    internal static bool TouristsOnlyTo;

    // Display mode
    internal static SMConditions.TransferMode DsiplayMode;

    // this list is for display use.  Transfers are executed against a separate list.  
    // These objects may be used to derive objects to be added to the transfer process queue.
    internal static List<TransferPump> DisplayPumps = new List<TransferPump>();

    private static Dictionary<PartModule, bool> _scienceModulesSource;
    internal static Dictionary<PartModule, bool> ScienceModulesSource
    {
      get
      {
        if (_scienceModulesSource != null) return _scienceModulesSource;
        _scienceModulesSource = new Dictionary<PartModule, bool>();
        if (SMAddon.SmVessel.SelectedPartsSource.Count <= 0) return _scienceModulesSource;
        List<Part>.Enumerator part = SMAddon.SmVessel.SelectedPartsSource.GetEnumerator();
        while (part.MoveNext())
        {
          if (part.Current == null) continue;
          List<IScienceDataContainer>.Enumerator module = part.Current.FindModulesImplementing<IScienceDataContainer>().GetEnumerator();
          while (module.MoveNext())
          {
            if (module.Current == null) continue;
            PartModule pm = (PartModule) module.Current;
            _scienceModulesSource.Add(pm, false);
          }
          module.Dispose();
        }
        part.Dispose();
        return _scienceModulesSource;
      }
    }

    private static Dictionary<PartModule, bool> _scienceModulesTarget;
    internal static Dictionary<PartModule, bool> ScienceModulesTarget
    {
      get
      {
        if (_scienceModulesTarget != null) return _scienceModulesTarget;
        _scienceModulesTarget = new Dictionary<PartModule, bool>();
        if (SMAddon.SmVessel.SelectedPartsSource.Count <= 0) return _scienceModulesTarget;
        List<Part>.Enumerator part = SMAddon.SmVessel.SelectedPartsSource.GetEnumerator();
        while (part.MoveNext())
        {
          if (part.Current == null) continue;
          List<IScienceDataContainer>.Enumerator module = part.Current.FindModulesImplementing<IScienceDataContainer>().GetEnumerator();
          while (module.MoveNext())
          {
            if (module.Current == null) continue;
            PartModule pm = (PartModule)module.Current;
            _scienceModulesTarget.Add(pm, false);
          }
          module.Dispose();
        }
        part.Dispose();
        return _scienceModulesTarget;
      }
    }
    #endregion


    #region TransferWindow (GUI Layout)

    // Resource Transfer Window
    // This window allows you some control over the selected resource on a selected source and target part
    // This window assumes that a resource has been selected on the Ship manifest window.
    internal static void Display(int windowId)
    {
      // set input locks when mouseover window...
      //_inputLocked = GuiUtils.PreventClickthrough(ShowWindow, Position, _inputLocked);

      string displayAmounts = SmUtils.DisplayVesselResourceTotals(SMAddon.SmVessel.SelectedResources[0]);
      Title = $"{SmUtils.SmTags["#smloc_transfer_000"]} - {SMAddon.SmVessel.Vessel.vesselName}{displayAmounts}"; // "Transfer"

      // Reset Tooltip active flag...
      ToolTipActive = false;
      SMHighlighter.IsMouseOver = false;

      //GUIContent label = new GUIContent("", "Close Window");
      GUIContent label = new GUIContent("", SmUtils.SmTags["#smloc_window_tt_001"]);
      if (SMConditions.IsTransferInProgress())
      {
        // label = new GUIContent("", "Action in progress.  Cannot close window");
        label = new GUIContent("", SmUtils.SmTags["#smloc_window_tt_002"]);
        GUI.enabled = false;
      }

      Rect rect = new Rect(Position.width - 20, 4, 16, 16);
      if (GUI.Button(rect, label))
      {
        ShowWindow = false;
        SMAddon.SmVessel.SelectedResources.Clear();
        SMAddon.SmVessel.SelectedPartsSource.Clear();
        SMAddon.SmVessel.SelectedPartsTarget.Clear();
        SMAddon.SmVessel.SelectedVesselsSource.Clear();
        SMAddon.SmVessel.SelectedVesselsTarget.Clear();
        ToolTip = "";
        SMHighlighter.Update_Highlighter();
        return;
      }
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);

      GUI.enabled = true;
      try
      {
        // This window assumes that a resource has been selected on the Ship manifest window.
        GUILayout.BeginHorizontal();
        //Left Column Begins
        GUILayout.BeginVertical();

        // Build source Transfer Viewer
        SourceTransferViewer();

        // Text above Source Details. (Between viewers)
        TextBetweenViewers(SMAddon.SmVessel.SelectedPartsSource, TransferPump.TypeXfer.SourceToTarget);

        // Build Details ScrollViewer
        SourceDetailsViewer();

        // Okay, we are done with the left column of the dialog...
        GUILayout.EndVertical();

        // Right Column Begins...
        GUILayout.BeginVertical();

        // Build Target Transfer Viewer
        TargetTransferViewer();

        // Text between viewers
        TextBetweenViewers(SMAddon.SmVessel.SelectedPartsTarget, TransferPump.TypeXfer.TargetToSource);

        // Build Target details Viewer
        TargetDetailsViewer();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Display MouseOverHighlighting, if any
        SMHighlighter.MouseOverHighlight();

        GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
        SMAddon.RepositionWindow(ref Position);
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in Ship Manifest Window.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SmUtils.LogType.Error, true);
      }
    }

    #region Source Viewers (GUI Layout)

    // Transfer Window components
    private static Vector2 _sourceTransferViewerScrollPosition = Vector2.zero;
    internal static void SourceTransferViewer()
    {
      try
      {
        // This is a scroll panel (we are using it to make button lists...)
        _sourceTransferViewerScrollPosition = GUILayout.BeginScrollView(_sourceTransferViewerScrollPosition,
          SMStyle.ScrollStyle, GUILayout.Height(SelectBox.height), GUILayout.Width(SelectBox.width));
        GUILayout.BeginVertical();

        if (ShowSourceVessels)
          VesselTransferViewer(SMAddon.SmVessel.SelectedResources, TransferPump.TypeXfer.SourceToTarget,
            _sourceTransferViewerScrollPosition);
        else
          PartsTransferViewer(SMAddon.SmVessel.SelectedResources, TransferPump.TypeXfer.SourceToTarget,
            _sourceTransferViewerScrollPosition);

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in Ship Manifest Window - SourceTransferViewer.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SmUtils.LogType.Error, true);
      }
    }

    private static Vector2 _sourceDetailsViewerScrollPosition = Vector2.zero;
    private static void SourceDetailsViewer()
    {
      try
      {
        // Source Part resource Details
        // this Scroll viewer is for the details of the part selected above.
        _sourceDetailsViewerScrollPosition = GUILayout.BeginScrollView(_sourceDetailsViewerScrollPosition,
          SMStyle.ScrollStyle, GUILayout.Height(DetailsBox.height), GUILayout.Width(DetailsBox.width));
        GUILayout.BeginVertical();

        if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
        {
          CrewDetails(SMAddon.SmVessel.SourceMembersSelected, SMAddon.SmVessel.SelectedPartsSource, SMAddon.SmVessel.SelectedPartsTarget, ShowSourceVessels, true);
        }
        else if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Science.ToString()))
        {
          ScienceDetailsSource(ShowSourceVessels);
        }
        else
        {
          // Other resources are left....
          ResourceDetailsViewer(TransferPump.TypeXfer.SourceToTarget);
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in WindowTransfer.SourceDetailsViewer.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}",
          SmUtils.LogType.Error, true);
      }
    }

    #endregion

    private static void TextBetweenViewers(IList<Part> selectedParts, TransferPump.TypeXfer xferType)
    {
      GUI.enabled = true;
      const float textWidth = 220;
      const float toggleWidth = 65; 
      string labelText = "";

      GUILayout.BeginHorizontal();
      if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
        labelText = selectedParts.Count > 0 ? $"{selectedParts[0].partInfo.title}" : $"{SmUtils.SmTags["#smloc_transfer_002"]}";
      else
      {
        if (selectedParts != null)
        {
          if (selectedParts.Count > 1)
            labelText = $"{SmUtils.SmTags["#smloc_transfer_001"]}"; // "Multiple Parts Selected");
          else if (selectedParts.Count == 1)
            labelText = $"{selectedParts[0].partInfo.title}";
          else
            labelText = $"{SmUtils.SmTags["#smloc_transfer_002"]}"; // "No Part Selected");
        }
      }
      GUILayout.Label(labelText, SMStyle.LabelStyleNoWrap, GUILayout.Width(textWidth));
      if (SMAddon.SmVessel.ModDockedVessels.Count > 0 && !SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Science.ToString()))
      {
        if (xferType == TransferPump.TypeXfer.SourceToTarget)
        {
          bool prevValue = ShowSourceVessels;
          ShowSourceVessels = GUILayout.Toggle(ShowSourceVessels, SmUtils.SmTags["#smloc_transfer_003"],
            GUILayout.Width(toggleWidth)); // "Vessels"
          if (!prevValue && ShowSourceVessels)
            WindowManifest.ResolveResourcePartSelections(SMAddon.SmVessel.SelectedResources);
        }
        else
        {
          if (SMAddon.SmVessel.ModDockedVessels.Count > 0)
          {
            bool prevValue = ShowSourceVessels;
            ShowTargetVessels = GUILayout.Toggle(ShowTargetVessels, SmUtils.SmTags["#smloc_transfer_003"],
              GUILayout.Width(toggleWidth)); // "Vessels"
            if (!prevValue && ShowSourceVessels)
              WindowManifest.ResolveResourcePartSelections(SMAddon.SmVessel.SelectedResources);
          }
        }
      }
      GUILayout.EndHorizontal();
    }

    #region Target Viewers (GUI Layout)

    private static Vector2 _targetTransferViewerScrollPosition = Vector2.zero;
    private static void TargetTransferViewer()
    {
      try
      {
        // Adjust target style colors for part selectors when using/not using CLS highlighting
        if (SMSettings.EnableClsHighlighting &&
            SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
          SMStyle.ButtonToggledTargetStyle.normal.textColor = SMSettings.Colors[SMSettings.TargetPartCrewColor];
        else
          SMStyle.ButtonToggledTargetStyle.normal.textColor = SMSettings.Colors[SMSettings.TargetPartColor];

        // This is a scroll panel (we are using it to make button lists...)
        _targetTransferViewerScrollPosition = GUILayout.BeginScrollView(_targetTransferViewerScrollPosition,
          SMStyle.ScrollStyle, GUILayout.Height(SelectBox.height), GUILayout.Width(SelectBox.width));
        GUILayout.BeginVertical();

        if (ShowTargetVessels)
          VesselTransferViewer(SMAddon.SmVessel.SelectedResources, TransferPump.TypeXfer.TargetToSource,
            _targetTransferViewerScrollPosition);
        else
          PartsTransferViewer(SMAddon.SmVessel.SelectedResources, TransferPump.TypeXfer.TargetToSource,
            _targetTransferViewerScrollPosition);

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in Ship Manifest Window - TargetTransferViewer.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SmUtils.LogType.Error, true);
      }
    }

    private static Vector2 _targetDetailsViewerScrollPosition = Vector2.zero;
    private static void TargetDetailsViewer()
    {
      try
      {
        // Target Part resource details
        _targetDetailsViewerScrollPosition = GUILayout.BeginScrollView(_targetDetailsViewerScrollPosition,
          SMStyle.ScrollStyle, GUILayout.Height(DetailsBox.height), GUILayout.Width(DetailsBox.width));
        GUILayout.BeginVertical();

        // --------------------------------------------------------------------------
        if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
        {
          CrewDetails(SMAddon.SmVessel.TargetMembersSelected, SMAddon.SmVessel.SelectedPartsTarget, SMAddon.SmVessel.SelectedPartsSource, ShowTargetVessels, false);
        }
        else if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Science.ToString()))
        {
          ScienceDetailsTarget();
        }
        else
        {
          ResourceDetailsViewer(TransferPump.TypeXfer.TargetToSource);
        }
        // --------------------------------------------------------------------------
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in WindowTransfer.TargetDetailsViewer.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}",
          SmUtils.LogType.Error, true);
      }
    }

    #endregion

    #region Viewer Details (GUI Layout)

    #region Part/Vessel Buttons Viewer
    private static void PartsTransferViewer(List<string> selectedResources, TransferPump.TypeXfer xferType,
      Vector2 viewerScrollPosition)
    {
      //float scrollX = Position.x + (pumpType == TransferPump.TypePump.SourceToTarget ? 20 : 320);
      //float scrollY = Position.y + 30 - viewerScrollPosition.y;
      float scrollX = (xferType == TransferPump.TypeXfer.SourceToTarget ? 20 : 320);
      float scrollY = viewerScrollPosition.y;
      string step = "begin";
      try
      {
        step = "begin button loop";
        List<Part>.Enumerator parts = SMAddon.SmVessel.SelectedResourcesParts.GetEnumerator();
        while (parts.MoveNext())
        {
          if (parts.Current == null) continue;
          // Build the part button title...
          step = "part button title";
          string strDescription = GetResourceDescription(selectedResources, parts.Current);

          // set the conditions for a button style change.
          int btnWidth = 273; // Start with full width button...
          if (SMConditions.AreSelectedResourcesTypeOther(selectedResources))
            btnWidth = !SMSettings.RealXfers || (SMSettings.EnablePfResources && SMConditions.IsInPreflight()) ? 173 : 223;
          else if (selectedResources.Contains(SMConditions.ResourceType.Crew.ToString()) && SMConditions.CanShowCrewFillDumpButtons())
            btnWidth = 173;

          // Set style based on viewer and toggled state.
          step = "Set style";
          GUIStyle style = GetPartButtonStyle(xferType, parts.Current);

          GUILayout.BeginHorizontal();

          // Now let's account for any target buttons already pressed. (sources and targets for resources cannot be the same)
          GUI.enabled = IsPartSelectable(selectedResources[0], xferType, parts.Current);

          step = "Render part Buttons";
          if (GUILayout.Button(strDescription, style, GUILayout.Width(btnWidth), GUILayout.Height(20)))
          {
            PartButtonToggled(xferType, parts.Current);
            SMHighlighter.Update_Highlighter();
          }
          Rect rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && rect.Contains(Event.current.mousePosition))
            SMHighlighter.SetMouseOverData(rect, scrollY, scrollX, SelectBox.height, parts.Current, Event.current.mousePosition);

          // Reset Button enabling.
          GUI.enabled = true;

          step = "Render dump/fill buttons";
          if (selectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
          {
            if (SMConditions.CanShowCrewFillDumpButtons())
            CrewFillDumpPartButtons(parts.Current);
          }
          if (SMConditions.AreSelectedResourcesTypeOther(selectedResources))
          {
            ResourceDumpFillButtons(selectedResources, xferType, parts.Current);
          }
          GUI.enabled = true;
          GUILayout.EndHorizontal();
        }
        parts.Dispose();
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SmUtils.LogMessage($"Error in Windowtransfer.PartsTransferViewer ({xferType}) at step:  {step}.  Error:  {ex}",
            SmUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    private static void VesselTransferViewer(List<string> selectedResources, TransferPump.TypeXfer xferType,
      Vector2 viewerScrollPosition)
    {
      //float scrollX = Position.x + (pumpType == TransferPump.TypePump.SourceToTarget ? 20 : 320);
      //float scrollY = Position.y + 30 - viewerScrollPosition.y;
      float scrollX = xferType == TransferPump.TypeXfer.SourceToTarget ? 20 : 320;
      float scrollY = viewerScrollPosition.y;
      string step = "begin";
      try
      {
        step = "begin button loop";
        List<ModDockedVessel>.Enumerator modDockedVessels = SMAddon.SmVessel.ModDockedVessels.GetEnumerator();
        while (modDockedVessels.MoveNext())
        {
          if (modDockedVessels.Current == null) continue;
          // Build the part button title...
          step = "vessel button title";
          string strDescription = GetResourceDescription(selectedResources, modDockedVessels.Current);

          // set the conditions for a button style change.
          int btnWidth = 273;
          if (!SMSettings.RealXfers) btnWidth = 180;

          // Set style based on viewer and toggled state.
          step = "Set style";
          GUIStyle style = GetVesselButtonStyle(xferType, modDockedVessels.Current);

          GUILayout.BeginHorizontal();

          // Now let's account for any target buttons already pressed. (sources and targets for resources cannot be the same)
          GUI.enabled = CanSelectVessel(xferType, modDockedVessels.Current);

          step = "Render vessel Buttons";
          if (GUILayout.Button($"{strDescription}", style, GUILayout.Width(btnWidth), GUILayout.Height(20)))
          {
            VesselButtonToggled(xferType, modDockedVessels.Current);
          }
          Rect rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && rect.Contains(Event.current.mousePosition))
            SMHighlighter.SetMouseOverData(rect, scrollY, scrollX, SelectBox.height, modDockedVessels.Current, Event.current.mousePosition);

          // Reset Button enabling.
          GUI.enabled = true;

          //step = "Render dump/fill buttons";
          // Crew
          if (selectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
          {
            if (SMConditions.CanShowCrewFillDumpButtons())
              CrewFillDumpVesselButtons(modDockedVessels.Current);
          }

          // Science

          // Resources
          else if (!SMSettings.RealXfers)
          {
            if (selectedResources.Count > 1)
              GUI.enabled = TransferPump.CalcRemainingResource(modDockedVessels.Current.VesselParts, selectedResources[0]) > 0 ||
                            TransferPump.CalcRemainingResource(modDockedVessels.Current.VesselParts, selectedResources[1]) > 0;
            else
              GUI.enabled = TransferPump.CalcRemainingResource(modDockedVessels.Current.VesselParts, selectedResources[0]) > 0;
            GUIStyle style1 = xferType == TransferPump.TypeXfer.SourceToTarget
              ? SMStyle.ButtonSourceStyle
              : SMStyle.ButtonTargetStyle;
            uint pumpId = TransferPump.GetPumpIdFromHash(string.Join("", selectedResources.ToArray()),
              modDockedVessels.Current.VesselParts.First(), modDockedVessels.Current.VesselParts.Last(), xferType,
              TransferPump.TriggerButton.Transfer);
            //GUIContent dumpContent = !TransferPump.IsPumpInProgress(pumpId)
            //  ? new GUIContent("Dump", "Dumps the selected resource in this Part")
            //  : new GUIContent("Stop", "Halts the dumping of the selected resource in this part");
            GUIContent dumpContent = !TransferPump.IsPumpInProgress(pumpId)
              ? new GUIContent(SmUtils.SmTags["#smloc_transfer_004"], SmUtils.SmTags["#smloc_transfer_tt_004"])
              : new GUIContent(SmUtils.SmTags["#smloc_transfer_005"], SmUtils.SmTags["#smloc_transfer_tt_005"]);
            if (GUILayout.Button(dumpContent, style1, GUILayout.Width(45), GUILayout.Height(20)))
            {
              SMPart.ToggleDumpResource(modDockedVessels.Current.VesselParts, selectedResources, pumpId);
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);

            GUIStyle style2 = xferType == TransferPump.TypeXfer.SourceToTarget
              ? SMStyle.ButtonSourceStyle
              : SMStyle.ButtonTargetStyle;
            if (selectedResources.Count > 1)
              GUI.enabled = TransferPump.CalcRemainingCapacity(modDockedVessels.Current.VesselParts, selectedResources[0]) > 0 ||
                            TransferPump.CalcRemainingCapacity(modDockedVessels.Current.VesselParts, selectedResources[0]) > 0;
            else
              GUI.enabled = TransferPump.CalcRemainingCapacity(modDockedVessels.Current.VesselParts, selectedResources[0]) > 0;
            //GUIContent fillContent = new GUIContent("Fill","Fills the Selected vessel with the selected resource(s)\r\n(Fill is from ground source, NOT from other parts in vessel)");
            GUIContent fillContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_006"], SmUtils.SmTags["#smloc_transfer_tt_006"]);
            if (GUILayout.Button(fillContent, style2, GUILayout.Width(30), GUILayout.Height(20)))
            {
              SMPart.FillResource(modDockedVessels.Current.VesselParts, selectedResources[0]);
              if (selectedResources.Count > 1)
                SMPart.FillResource(modDockedVessels.Current.VesselParts, selectedResources[1]);
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);
            GUI.enabled = true;
          }
          GUILayout.EndHorizontal();
        }
        modDockedVessels.Dispose();
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SmUtils.LogMessage($"Error in Windowtransfer.VesselTransferViewer ({xferType}) at step:  {step}.  Error:  {ex}",
            SmUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    private static void ResourceDumpFillButtons(List<string> selectedResources, TransferPump.TypeXfer xferType, Part part)
    {
      uint pumpId = TransferPump.GetPumpIdFromHash(string.Join("", selectedResources.ToArray()), part, part,
        xferType, TransferPump.TriggerButton.Transfer);
      //GUIContent dumpContent = !TransferPump.IsPumpInProgress(pumpId)
      //  ? new GUIContent("Dump", "Dumps the selected resource in this vessel")
      //  : new GUIContent("Stop", "Halts the dumping of the selected resource in this vessel");
      GUIContent dumpContent = !TransferPump.IsPumpInProgress(pumpId)
        ? new GUIContent(SmUtils.SmTags["#smloc_transfer_004"], SmUtils.SmTags["#smloc_transfer_tt_001"])
        : new GUIContent(SmUtils.SmTags["#smloc_transfer_005"], SmUtils.SmTags["#smloc_transfer_tt_002"]);
      GUIStyle style1 = xferType == TransferPump.TypeXfer.SourceToTarget
        ? SMStyle.ButtonSourceStyle
        : SMStyle.ButtonTargetStyle;
      GUI.enabled = CanDumpPart(part);

      if (GUILayout.Button(dumpContent, style1, GUILayout.Width(45), GUILayout.Height(20)))
      {
        SMPart.ToggleDumpResource(part, selectedResources, pumpId);
      }
      Rect rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);


      if (SMSettings.RealXfers || (!SMSettings.EnablePfResources || !SMConditions.IsInPreflight())) return;
      GUIStyle style2 = xferType == TransferPump.TypeXfer.SourceToTarget
        ? SMStyle.ButtonSourceStyle
        : SMStyle.ButtonTargetStyle;
      //GUIContent fillContent = new GUIContent("Fill", "Fills the Selected part with the selected resource(s)\r\n(Fill is from ground source, NOT from other parts in vessel)");
      GUIContent fillContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_006"], SmUtils.SmTags["#smloc_transfer_tt_003"]);
      // Fills should only be in Non Realism mode or if Preflight Resources are selected...
      if (selectedResources.Count > 1)
        GUI.enabled = part.Resources[selectedResources[0]].amount <
                      part.Resources[selectedResources[0]].maxAmount ||
                      part.Resources[selectedResources[1]].amount <
                      part.Resources[selectedResources[1]].maxAmount;
      else
        GUI.enabled = part.Resources[selectedResources[0]].amount <
                      part.Resources[selectedResources[0]].maxAmount;
      if (GUILayout.Button(fillContent, style2, GUILayout.Width(30), GUILayout.Height(20)))
      {
        SMPart.FillResource(part, selectedResources[0]);
        if (selectedResources.Count > 1)
          SMPart.FillResource(part, selectedResources[1]);
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);
    }

    private static void CrewFillDumpVesselButtons(ModDockedVessel vessel)
    {
      //GUIContent dumpContent = new GUIContent("Dump", "Removes any crew members in this vessel");
      //GUIContent fillContent = new GUIContent("Fill", "Fills this vessel with crew members");
      GUIContent dumpContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_004"], SmUtils.SmTags["#smloc_transfer_tt_007"]);
      GUIContent fillContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_006"], SmUtils.SmTags["#smloc_transfer_tt_008"]);
      int crewCapacity = SmUtils.GetPartsCrewCapacity(vessel.VesselParts);
      int crewCount = SmUtils.GetPartsCrewCount(vessel.VesselParts);
      GUI.enabled = SmUtils.GetPartsCrewCapacity(vessel.VesselParts) > 0;
      if (GUILayout.Button(dumpContent, GUILayout.Width(45), GUILayout.Height(20)))
      {
        List<Part>.Enumerator part = vessel.VesselParts.GetEnumerator();
        while (part.MoveNext())
        {
          if (part.Current == null) continue;
          SMPart.DumpCrew(part.Current);
        }
        part.Dispose();
      }
      Rect rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);

      GUI.enabled = crewCount < crewCapacity;
      if (GUILayout.Button(fillContent, GUILayout.Width(45), GUILayout.Height(20)))
      {
        List<Part>.Enumerator part = vessel.VesselParts.GetEnumerator();
        while (part.MoveNext())
        {
          if (part.Current == null) continue;
          SMPart.FillCrew(part.Current);
        }
        part.Dispose();
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);
    }

    private static void CrewFillDumpPartButtons(Part part)
    {
      //GUIContent dumpContent = new GUIContent("Dump", "Removes any crew members in this part");
      //GUIContent fillContent = new GUIContent("Fill", "Fills this part with crew members");
      GUIContent dumpContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_004"], SmUtils.SmTags["#smloc_transfer_tt_007"]);
      GUIContent fillContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_006"], SmUtils.SmTags["#smloc_transfer_tt_008"]);

      GUI.enabled = part.protoModuleCrew.Count > 0;
      if (GUILayout.Button(dumpContent, GUILayout.Width(45), GUILayout.Height(20)))
      {
        SMPart.DumpCrew(part);
      }
      Rect rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);

      GUI.enabled = part.protoModuleCrew.Count < part.CrewCapacity;
      if (GUILayout.Button(fillContent, GUILayout.Width(45), GUILayout.Height(20)))
      {
        SMPart.FillCrew(part);
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, 10);
    }
    #endregion

    #region Crew Details Viewer

    private static void CrewDetails(List<ProtoCrewMember> selectedCrewMembers, List<Part> selectedPartsFrom, List<Part> selectedPartsTo, bool isVesselMode, bool isSourceView)
    {
      float xOffset = 30;
      // TODO: these could be moved out of onGUI, and updated statically from events that cause changes.
      int sourceCrewCount = SmUtils.GetPartsCrewCount(selectedPartsFrom);
      int targetCapacity = GetAvailableCrewSpace(selectedPartsTo);
      //bool crewContainsTourists = (SmUtils.CrewContainsTourists(selectedPartsFrom));
      // end_todo

      // Vessel mode only code.
      bool touristsOnly = isSourceView ? TouristsOnlyFrom : TouristsOnlyTo;
      if (selectedPartsFrom.Count <= 0) return;
      if (isVesselMode)
      {
        // If vessel contains any crew, display the tourist filter option.
        if (sourceCrewCount > 0) touristsOnly = ShowOptionTouristsOnly(isSourceView, touristsOnly, xOffset);

        // check to see if crewmembers selected match criteria for a select all setting...
        bool selectAll = IsSelectAll(selectedCrewMembers, sourceCrewCount, isSourceView, targetCapacity);

        // Now display the selectAll option and Xfer button.
        ShowSelectAllOption(selectedCrewMembers, selectedPartsFrom, selectedPartsTo, isSourceView, sourceCrewCount, targetCapacity, selectAll, touristsOnly, xOffset);
        GUI.enabled = true;
      }

      List<ProtoCrewMember>.Enumerator crewMember = SMAddon.SmVessel.GetCrewFromParts(selectedPartsFrom).GetEnumerator();
      // ReSharper disable once ForCanBeConvertedToForeach
      while (crewMember.MoveNext() && GUI.enabled)
      {
        if (crewMember.Current == null) continue;
        if (isVesselMode && touristsOnly && crewMember.Current.type != ProtoCrewMember.KerbalType.Tourist) continue;

        CrewMemberDetails(selectedPartsFrom, selectedPartsTo, selectedCrewMembers, crewMember.Current, xOffset, isVesselMode, targetCapacity);
      }
      crewMember.Dispose();
      // Cater for DeepFreeze Continued... parts - list frozen kerbals
      if (!InstalledMods.IsDfApiReady) return;
      try
      {
        PartModule deepFreezer = (from PartModule pm in selectedPartsFrom[0].Modules
          where pm.moduleName == "DeepFreezer"
          select pm).SingleOrDefault();
        if (deepFreezer == null) return;
        DfWrapper.DeepFreezer sourcepartFrzr = new DfWrapper.DeepFreezer(deepFreezer);
        if (sourcepartFrzr.StoredCrewList.Count <= 0) return;
        //Dictionary<string, DFWrapper.KerbalInfo> frozenKerbals = DFWrapper.DeepFreezeAPI.FrozenKerbals;
        List<DfWrapper.FrznCrewMbr>.Enumerator frznCrew = sourcepartFrzr.StoredCrewList.GetEnumerator();
        while (frznCrew.MoveNext())
        {
          if (frznCrew.Current == null) continue;
          FrozenCrewMemberDetails(xOffset, frznCrew.Current);
        }
        frznCrew.Dispose();
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage(
          $" in WindowTransfer.CrewDetails.  Error attempting to check DeepFreeze for FrozenKerbals.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SmUtils.LogType.Error, true);
        //Debug.Log("Error attempting to check DeepFreeze for FrozenKerbals");
        //Debug.Log(ex.Message);
      }
    }

    private static void FrozenCrewMemberDetails(float xOffset, DfWrapper.FrznCrewMbr frznCrew)
    {
      GUILayout.BeginHorizontal();
      GUI.enabled = false;
      if (GUILayout.Button(new GUIContent("»", SmUtils.SmTags["#smloc_transfer_tt_009"]), SMStyle.ButtonStyle,
        GUILayout.Width(15), GUILayout.Height(20))) // "Move Kerbal to another seat within Part"
      {
        ToolTip = "";
      }
      Rect rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      string trait = "";
      ProtoCrewMember frozenKerbal = FindFrozenKerbal(frznCrew.CrewName);
      if (frozenKerbal != null) trait = frozenKerbal.trait;
      GUI.enabled = true;
      GUILayout.Label($"  {frznCrew.CrewName} ({trait})", SMStyle.LabelStyleCyan, GUILayout.Width(190),
        GUILayout.Height(20));
      //GUIContent thawContent = new GUIContent("Thaw", "This Kerbal is Frozen. Click to Revive kerbal");
      GUIContent thawContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_010"],
        SmUtils.SmTags["#smloc_transfer_tt_010"]);
      if (GUILayout.Button(thawContent, SMStyle.ButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
      {
        WindowRoster.ThawKerbal(frznCrew.CrewName);
        ToolTip = "";
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      GUILayout.EndHorizontal();
    }

    private static void ShowSelectAllOption(List<ProtoCrewMember> selectedCrewMembers, List<Part> selectedPartsFrom, List<Part> selectedPartsTo,
      bool isSourceView, int sourceCrewCount, int targetCapacity, bool selectAll, bool touristsOnly, float xOffset)
    {
      Rect rect;
      GUILayout.BeginHorizontal();
      GUI.enabled = selectedPartsFrom.Count > 0 && selectedPartsTo.Count > 0 && sourceCrewCount > 0 && targetCapacity > 0;
      GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_020"],
        SmUtils.SmTags[GUI.enabled ? "#smloc_transfer_tt_033" : "#smloc_transfer_tt_034"]);
      selectAll = GUILayout.Toggle(selectAll, content, GUILayout.Width(180));
      if (selectAll != (isSourceView ? SelectAllFrom : SelectAllTo))
      {
        if (selectAll)
        {
          List<ProtoCrewMember>.Enumerator member =
            SMAddon.SmVessel.GetCrewFromParts(selectedPartsFrom).GetEnumerator();
          while (member.MoveNext())
          {
            if (member.Current == null) continue;
            if (touristsOnly && member.Current.type != ProtoCrewMember.KerbalType.Tourist) continue;
            if (!selectedCrewMembers.Contains(member.Current) && targetCapacity > selectedCrewMembers.Count)
              selectedCrewMembers.Add(member.Current);
          }
          member.Dispose();
        }
        else
        {
          selectedCrewMembers.Clear();
        }
        if (isSourceView) SelectAllFrom = selectAll;
        else SelectAllTo = selectAll;
      }
      IsSelectAll(selectedCrewMembers, sourceCrewCount, isSourceView, targetCapacity);
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

      GUI.enabled = selectedCrewMembers.Count > 0 && SMConditions.CanKerbalsBeXferred(selectedPartsFrom, selectedPartsTo);
      XferToolTip = SmUtils.SmTags[selectedCrewMembers.Count <= 0
        ? "#smloc_conditions_tt_008"
        : "#smloc_conditions_tt_009"];
      CrewSelectedXferButton(selectedPartsFrom, selectedPartsTo, selectedCrewMembers, xOffset);
      GUILayout.EndHorizontal();
    }

    private static bool ShowOptionTouristsOnly(bool isSourceView, bool touristsOnly, float xOffset)
    {
      Rect rect;
      GUI.enabled = true;
      // check to see if crewmembers selected match criteria for a show Tourists only setting...
      GUIContent tContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_022"],
        SmUtils.SmTags["#smloc_transfer_tt_035"]); // Show tourists only
      touristsOnly = GUILayout.Toggle(touristsOnly, tContent, GUILayout.Width(180));
      if (touristsOnly != (isSourceView ? TouristsOnlyFrom : TouristsOnlyTo))
      {
        if (isSourceView) TouristsOnlyFrom = touristsOnly;
        else TouristsOnlyTo = touristsOnly;
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      return touristsOnly;
    }

    private static bool IsSelectAll(List<ProtoCrewMember> selectedCrewMembers, int sourceCrewCount, bool isSourceView, int targetCapacity)
    {
      bool selectAll = false;
      if (sourceCrewCount <= 0 || targetCapacity <= 0) return false;
      if (selectedCrewMembers.Count < sourceCrewCount && selectedCrewMembers.Count < targetCapacity)
      {
        if (isSourceView) SelectAllFrom = false;
        else SelectAllTo = false;
      }
      else if (selectedCrewMembers.Count == sourceCrewCount ||
               selectedCrewMembers.Count == targetCapacity)
      {
        selectAll = true;
        if (isSourceView) SelectAllFrom = true;
        else SelectAllTo = true;
      }
      return selectAll;
    }

    private static void CrewMemberDetails(List<Part> selectedPartsFrom, List<Part> selectedPartsTo, List<ProtoCrewMember> crewMembers, ProtoCrewMember crewMember, float xOffset, bool isVesselMode, int targetCapacity)
    {
      const float cmWidth = 180;
      const float cmMoveWidth = 25;

      Rect rect;
      GUILayout.BeginHorizontal();
      if (isVesselMode)
      {
        bool selected = crewMembers.Contains(crewMember);
        selected = GUILayout.Toggle(selected, $"{crewMember.name} ({crewMember.experienceTrait.Title})", GUILayout.Width(cmWidth), GUILayout.Height(20));
        if (selected && !crewMembers.Contains(crewMember))
        {
          if (crewMembers.Count < targetCapacity) crewMembers.Add(crewMember);
        }
        else if (!selected && crewMembers.Contains(crewMember))
        {
          crewMembers.Remove(crewMember);
        }
      }
      else
      {
        GUI.enabled = true;
        GUILayout.Label($"  {crewMember.name} ({crewMember.experienceTrait.Title })", GUILayout.Width(cmWidth), GUILayout.Height(20));
      }
      GUI.enabled = !SMConditions.IsTransferInProgress();
      // GUIContent moveContent = new GUIContent("»", "Move Kerbal to another seat within Part");
      GUIContent moveContent = new GUIContent("»", SmUtils.SmTags["#smloc_transfer_tt_009"]);
      if (GUILayout.Button(moveContent, SMStyle.ButtonStyle, GUILayout.Width(cmMoveWidth), GUILayout.Height(20)))
      {
        ToolTip = "";
        SMAddon.SmVessel.TransferCrewObj.CrewTransferBegin(crewMember, selectedPartsFrom[0], selectedPartsFrom[0]);
      }
      rect = GUILayoutUtility.GetLastRect();
      if (Event.current.type == EventType.Repaint && ShowToolTips)
        ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

      // Display the Transfer Button.
      CrewMemberXferButton(selectedPartsFrom, selectedPartsTo, crewMember, xOffset);
      GUILayout.EndHorizontal();
    }

    private static void CrewMemberXferButton(List<Part> selectedPartsFrom, List<Part> selectedPartsTo, ProtoCrewMember crewMember, float xOffset)
    {
      const float btnWidth = 60;
      Rect rect;
      GUI.enabled = SMConditions.CanKerbalsBeXferred(selectedPartsFrom, selectedPartsTo);
      if ((SMAddon.SmVessel.TransferCrewObj.FromCrewMember == crewMember ||
           SMAddon.SmVessel.TransferCrewObj.ToCrewMember == crewMember) && SMConditions.IsTransferInProgress())
      {
        GUI.enabled = true;
        //GUILayout.Label("Moving", GUILayout.Width(btnWidth), GUILayout.Height(20));
        GUILayout.Label(SmUtils.SmTags["#smloc_transfer_007"], GUILayout.Width(btnWidth), GUILayout.Height(20));
      }
      else if (!SMConditions.IsClsInSameSpace(selectedPartsFrom[0],
        selectedPartsTo.Count > 0 ? selectedPartsTo[0] : null))
      {
        GUI.enabled = crewMember.type != ProtoCrewMember.KerbalType.Tourist;
        //GUIContent evaContent = new GUIContent("EVA", EvaToolTip);
        GUIContent evaContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_008"], EvaToolTip);
        if (GUILayout.Button(evaContent, SMStyle.ButtonStyle, GUILayout.Width(btnWidth), GUILayout.Height(20)))
        {
          ToolTip = "";
          FlightEVA.SpawnEVA(crewMember.KerbalRef);
          GUI.enabled = false;
        }
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      }
      else
      {
        if (GUILayout.Button(new GUIContent(SmUtils.SmTags["#smloc_transfer_009"], XferToolTip),
          SMStyle.ButtonStyle, GUILayout.Width(btnWidth), GUILayout.Height(20))) // "Xfer"
        {
          SMAddon.SmVessel.TransferCrewObj.CrewTransferBegin(crewMember, selectedPartsFrom[0], selectedPartsTo[0]);
        }
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      }
    }

    private static void CrewSelectedXferButton(List<Part> selectedPartsFrom, List<Part> selectedPartsTo, List<ProtoCrewMember> crewMembers, float xOffset)
    {
      Rect rect;
      //if ((SMAddon.SmVessel.TransferCrewObj.FromCrewMembers == crewMembers ||
      //     SMAddon.SmVessel.TransferCrewObj.ToCrewMembers == crewMembers) && SMConditions.IsTransferInProgress())
      if (SMConditions.IsTransferInProgress())
      {
        GUI.enabled = true;
        //GUILayout.Label("Moving", GUILayout.Width(50), GUILayout.Height(20));
        GUILayout.Label(SmUtils.SmTags["#smloc_transfer_007"], GUILayout.Width(50), GUILayout.Height(20));
      }
      else
      {
        if (GUILayout.Button(new GUIContent($"{SmUtils.SmTags["#smloc_transfer_021"]} ({crewMembers.Count})", XferToolTip),
          SMStyle.ButtonStyle, GUILayout.Width(90),
          GUILayout.Height(20))) // "Xfer Crew"
        {
          SMAddon.SmVessel.TransferCrewObj.CrewTransfersBegin(crewMembers, selectedPartsFrom, selectedPartsTo);
        }
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
      }
    }
    #endregion

    #region Science Details Viewer

    private static void ScienceVesselDetails(Dictionary<PartModule, bool> sourceModules, Dictionary<PartModule, bool> targetModules, bool isVesselMode)
    {
      if (sourceModules.Count <= 0 || targetModules.Count <= 0) return;
      const float xOffset = 30;
      // Okay, for vessels we want summaries.  then a result list of modules to transfer based on those summaries.
      // - Collectable Science
      // - Processed Science (Labs)
      // - Uncollectable Science (eva only Experiments)
      // - Unprocessed science (Labs)

      Dictionary<PartModule, bool>.KeyCollection.Enumerator modules = sourceModules.Keys.GetEnumerator();
      while (modules.MoveNext())
      {
        if (modules.Current == null) continue;
        // experiments/Containers.
        int scienceCount = ((IScienceDataContainer)modules.Current).GetScienceCount();
        bool isCollectable = true;
        switch (modules.Current.moduleName)
        {
          case "ModuleScienceExperiment":
            isCollectable = ((ModuleScienceExperiment)modules.Current).dataIsCollectable;
            break;
          case "ModuleScienceContainer":
            isCollectable = ((ModuleScienceContainer)modules.Current).dataIsCollectable;
            break;
          case "ModuleScienceLab":
          case "ModuleScienceConverter":
            isCollectable = true;
            break;
        }

        GUILayout.BeginHorizontal();
        GUI.enabled = ((IScienceDataContainer)modules.Current).GetScienceCount() > 0;

        string label = "+";
        // string toolTip = string.Format("{0} {1}", "Expand/Collapse Science detail.", GUI.enabled? "" : "(Disabled, nothing to xfer)");
        string toolTip =
          $"{SmUtils.SmTags["#smloc_transfer_tt_011"]} {(GUI.enabled ? "" : SmUtils.SmTags["#smloc_transfer_tt_012"])}";
        GUIStyle expandStyle = ScienceModulesSource[modules.Current] ? SMStyle.ButtonToggledStyle : SMStyle.ButtonStyle;
        if (ScienceModulesSource[modules.Current]) label = "-";
        if (GUILayout.Button(new GUIContent(label, toolTip), expandStyle, GUILayout.Width(15), GUILayout.Height(20)))
        {
          ScienceModulesSource[modules.Current] = !ScienceModulesSource[modules.Current];
        }
        Rect rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

        GUI.enabled = true;
        GUILayout.Label($"{modules.Current.moduleName} - ({scienceCount})", GUILayout.Width(205),
          GUILayout.Height(20));

        // If we have target selected, it is not the same as the source, there is science to xfer.
        if (SMAddon.SmVessel.SelectedModuleTarget != null && scienceCount > 0)
        {
          if (SMSettings.RealXfers && !isCollectable)
          {
            GUI.enabled = false;
            //toolTip = "Realism Mode is preventing transfer.\r\nExperiment/data is marked not transferable";
            toolTip = SmUtils.SmTags["#smloc_transfer_tt_013"];
          }
          else
          {
            GUI.enabled = true;
            //toolTip = "Realism is off, or Experiment/data is transferable";
            toolTip = SmUtils.SmTags["#smloc_transfer_tt_014"];
          }
          //GUIContent xferContent = new GUIContent("Xfer", toolTip);
          GUIContent xferContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_tt_009"], toolTip);
          if (GUILayout.Button(xferContent, SMStyle.ButtonStyle, GUILayout.Width(40),
            GUILayout.Height(20)))
          {
            SMAddon.SmVessel.SelectedModuleSource = modules.Current;
            ProcessController.TransferScience(SMAddon.SmVessel.SelectedModuleSource,
              SMAddon.SmVessel.SelectedModuleTarget);
            SMAddon.SmVessel.SelectedModuleSource = null;
          }
          rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && ShowToolTips)
            ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

          if (GUI.enabled && SMAddon.SmVessel.Vessel.FindPartModulesImplementing<ModuleScienceLab>().Count > 0)
          {
            //GUIContent content = new GUIContent("Proc", "Transfer only science that was already processed");
            GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_011"], SmUtils.SmTags["#smloc_transfer_tt_014"]);
            if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(40), GUILayout.Height(20)))
            {
              SMAddon.SmVessel.SelectedModuleSource = modules.Current;
              ProcessController.TransferScienceLab(SMAddon.SmVessel.SelectedModuleSource,
                SMAddon.SmVessel.SelectedModuleTarget,
                ProcessController.Selection.OnlyProcessed);
              SMAddon.SmVessel.SelectedModuleSource = null;
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
            //content = new GUIContent("Unproc", "Transfer only science that was not processed yet";
            content = new GUIContent(SmUtils.SmTags["#smloc_transfer_012"], SmUtils.SmTags["#smloc_transfer_tt_015"]);
            if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
            {
              SMAddon.SmVessel.SelectedModuleSource = modules.Current;
              ProcessController.TransferScienceLab(SMAddon.SmVessel.SelectedModuleSource,
                SMAddon.SmVessel.SelectedModuleTarget,
                ProcessController.Selection.OnlyUnprocessed);
              SMAddon.SmVessel.SelectedModuleSource = null;
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
          }
        }
        GUILayout.EndHorizontal();
        if (ScienceModulesSource[modules.Current])
        {
          IEnumerator items = ((IScienceDataContainer)modules.Current).GetData().GetEnumerator();
          while (items.MoveNext())
          {
            if (items.Current == null) continue;
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(15), GUILayout.Height(20));

            // Get science data from experiment.
            string expId = ((ScienceData)items.Current).subjectID.Split('@')[0];
            string expKey = ((ScienceData)items.Current).subjectID.Split('@')[1];
            ScienceExperiment se = ResearchAndDevelopment.GetExperiment(expId);
            string key = (from k in se.Results.Keys where expKey.Contains(k) select k).FirstOrDefault();
            key = key ?? "default";
            string results = se.Results[key];

            // Build Tooltip
            toolTip = ((ScienceData)items.Current).title;
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_016"]}:  {results}";
            toolTip +=
              $"\n-{SmUtils.SmTags["#smloc_transfer_tt_017"]}:  {((ScienceData)items.Current).dataAmount} {SmUtils.SmTags["#smloc_transfer_tt_018"]}";
            toolTip +=
              $"\n-{SmUtils.SmTags["#smloc_transfer_tt_019"]}:  {((ScienceData)items.Current).baseTransmitValue}"; // was transmitValue;
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_020"]}:  {((ScienceData)items.Current).labValue}";
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_021"]}:  {((ScienceData)items.Current).transmitBonus}";  // Was labBoost
            //toolTip += "\r\n-Results:    " + results;
            //toolTip += "\r\n-Data Amt:   " + ((ScienceData)items.Current).dataAmount + " Mits";
            //toolTip += "\r\n-Xmit Value: " + ((ScienceData) items.Current).baseTransmitValue; // was transmitValue;
            //toolTip += "\r\n-Lab Value:  " + ((ScienceData)items.Current).labValue;
            //toolTip += "\r\n-Lab Boost:  " + ((ScienceData)items.Current).transmitBonus;  // Was labBoost

            GUILayout.Label(new GUIContent(se.experimentTitle, toolTip), SMStyle.LabelStyleNoWrap, GUILayout.Width(205), GUILayout.Height(20));
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

            if (SMSettings.RealXfers && !isCollectable)
            {
              GUI.enabled = false;
              //toolTip = "Realistic Transfers is preventing transfer.\r\nData is marked not transferable";
              toolTip = SmUtils.SmTags["#smloc_transfer_tt_022"];
            }
            else
            {
              //toolTip = "Realistic Transfers is off, or Data is transferable";
              toolTip = SmUtils.SmTags["#smloc_transfer_tt_023"];
              GUI.enabled = true;
            }
            if (SMAddon.SmVessel.SelectedModuleTarget != null && scienceCount > 0)
            {
              //GUIContent content = new GUIContent("Xfer", toolTip);
              GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_tt_009"], toolTip);
              if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(40), GUILayout.Height(20)))
              {
                if (((ModuleScienceContainer)SMAddon.SmVessel.SelectedModuleTarget).AddData(((ScienceData)items.Current)))
                  ((IScienceDataContainer)modules.Current).DumpData(((ScienceData)items.Current));
              }
              rect = GUILayoutUtility.GetLastRect();
              if (Event.current.type == EventType.Repaint && ShowToolTips)
                ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
            }
            GUILayout.EndHorizontal();
          }
        }
        GUI.enabled = true;
      }
      modules.Dispose();
    }

    private static void ScienceDetailsSource(bool isVesselMode)
    {
      if (SMAddon.SmVessel.SelectedPartsSource.Count <= 0) return;
      const float xOffset = 30;

      Dictionary<PartModule, bool>.KeyCollection.Enumerator modules = ScienceModulesSource.Keys.GetEnumerator();
      while (modules.MoveNext())
      {
        if (modules.Current == null) continue;
        // experiments/Containers.
        int scienceCount = ((IScienceDataContainer)modules.Current).GetScienceCount();
        bool isCollectable = true;
        switch (modules.Current.moduleName)
        {
          case "ModuleScienceExperiment":
            isCollectable = ((ModuleScienceExperiment)modules.Current).dataIsCollectable;
            break;
          case "ModuleScienceContainer":
            isCollectable = ((ModuleScienceContainer)modules.Current).dataIsCollectable;
            break;
        }

        GUILayout.BeginHorizontal();
        GUI.enabled = ((IScienceDataContainer)modules.Current).GetScienceCount() > 0;

        string label = "+";
        // string toolTip = string.Format("{0} {1}", "Expand/Collapse Science detail.", GUI.enabled? "" : "(Disabled, nothing to xfer)");
        string toolTip =
          $"{SmUtils.SmTags["#smloc_transfer_tt_011"]} {(GUI.enabled ? "" : SmUtils.SmTags["#smloc_transfer_tt_012"])}";
        GUIStyle expandStyle = ScienceModulesSource[modules.Current] ? SMStyle.ButtonToggledStyle : SMStyle.ButtonStyle;
        if (ScienceModulesSource[modules.Current]) label = "-";
        if (GUILayout.Button(new GUIContent(label, toolTip), expandStyle, GUILayout.Width(15), GUILayout.Height(20)))
        {
          ScienceModulesSource[modules.Current] = !ScienceModulesSource[modules.Current];
        }
        Rect rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

        GUI.enabled = true;
        GUILayout.Label($"{modules.Current.moduleName} - ({scienceCount})", GUILayout.Width(205),
          GUILayout.Height(20));

        // If we have target selected, it is not the same as the source, there is science to xfer.
        if (SMAddon.SmVessel.SelectedModuleTarget != null && scienceCount > 0)
        {
          if (SMSettings.RealXfers && !isCollectable)
          {
            GUI.enabled = false;
            //toolTip = "Realism Mode is preventing transfer.\r\nExperiment/data is marked not transferable";
            toolTip = SmUtils.SmTags["#smloc_transfer_tt_013"];
          }
          else
          {
            GUI.enabled = true;
            //toolTip = "Realism is off, or Experiment/data is transferable";
            toolTip = SmUtils.SmTags["#smloc_transfer_tt_014"];
          }
          //GUIContent xferContent = new GUIContent("Xfer", toolTip);
          GUIContent xferContent = new GUIContent(SmUtils.SmTags["#smloc_transfer_tt_009"], toolTip);
          if (GUILayout.Button(xferContent, SMStyle.ButtonStyle, GUILayout.Width(40),
            GUILayout.Height(20)))
          {
            SMAddon.SmVessel.SelectedModuleSource = modules.Current;
            ProcessController.TransferScience(SMAddon.SmVessel.SelectedModuleSource,
              SMAddon.SmVessel.SelectedModuleTarget);
            SMAddon.SmVessel.SelectedModuleSource = null;
          }
          rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && ShowToolTips)
            ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

          if (GUI.enabled && SMAddon.SmVessel.Vessel.FindPartModulesImplementing<ModuleScienceLab>().Count > 0)
          {
            //GUIContent content = new GUIContent("Proc", "Transfer only science that was already processed");
            GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_011"], SmUtils.SmTags["#smloc_transfer_tt_014"]);
            if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(40), GUILayout.Height(20)))
            {
              SMAddon.SmVessel.SelectedModuleSource = modules.Current;
              ProcessController.TransferScienceLab(SMAddon.SmVessel.SelectedModuleSource,
                                                   SMAddon.SmVessel.SelectedModuleTarget,
                                             ProcessController.Selection.OnlyProcessed);
              SMAddon.SmVessel.SelectedModuleSource = null;
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
            //content = new GUIContent("Unproc", "Transfer only science that was not processed yet";
            content = new GUIContent(SmUtils.SmTags["#smloc_transfer_012"], SmUtils.SmTags["#smloc_transfer_tt_015"]);
            if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
            {
              SMAddon.SmVessel.SelectedModuleSource = modules.Current;
              ProcessController.TransferScienceLab(SMAddon.SmVessel.SelectedModuleSource,
                                                   SMAddon.SmVessel.SelectedModuleTarget,
                                             ProcessController.Selection.OnlyUnprocessed);
              SMAddon.SmVessel.SelectedModuleSource = null;
            }
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
          }
        }
        GUILayout.EndHorizontal();
        if (ScienceModulesSource[modules.Current])
        {
          IEnumerator items = ((IScienceDataContainer) modules.Current).GetData().GetEnumerator();
          while (items.MoveNext())
          {
            if (items.Current == null) continue;
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(15), GUILayout.Height(20));

            // Get science data from experiment.
            string expId = ((ScienceData)items.Current).subjectID.Split('@')[0];
            string expKey = ((ScienceData)items.Current).subjectID.Split('@')[1];
            ScienceExperiment se = ResearchAndDevelopment.GetExperiment(expId);
            string key = (from k in se.Results.Keys where expKey.Contains(k) select k).FirstOrDefault();
            key = key ?? "default";
            string results = se.Results[key];

            // Build Tooltip
            toolTip = ((ScienceData)items.Current).title;
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_016"]}:  {results}";
            toolTip +=
              $"\n-{SmUtils.SmTags["#smloc_transfer_tt_017"]}:  {((ScienceData) items.Current).dataAmount} {SmUtils.SmTags["#smloc_transfer_tt_018"]}";
            toolTip +=
              $"\n-{SmUtils.SmTags["#smloc_transfer_tt_019"]}:  {((ScienceData) items.Current).baseTransmitValue}"; // was transmitValue;
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_020"]}:  {((ScienceData) items.Current).labValue}";
            toolTip += $"\n-{SmUtils.SmTags["#smloc_transfer_tt_021"]}:  {((ScienceData) items.Current).transmitBonus}";  // Was labBoost
            //toolTip += "\r\n-Results:    " + results;
            //toolTip += "\r\n-Data Amt:   " + ((ScienceData)items.Current).dataAmount + " Mits";
            //toolTip += "\r\n-Xmit Value: " + ((ScienceData) items.Current).baseTransmitValue; // was transmitValue;
            //toolTip += "\r\n-Lab Value:  " + ((ScienceData)items.Current).labValue;
            //toolTip += "\r\n-Lab Boost:  " + ((ScienceData)items.Current).transmitBonus;  // Was labBoost

            GUILayout.Label(new GUIContent(se.experimentTitle, toolTip), SMStyle.LabelStyleNoWrap, GUILayout.Width(205), GUILayout.Height(20));
            rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);

            if (SMSettings.RealXfers && !isCollectable)
            {
              GUI.enabled = false;
              //toolTip = "Realistic Transfers is preventing transfer.\r\nData is marked not transferable";
              toolTip = SmUtils.SmTags["#smloc_transfer_tt_022"];
            }
            else
            {
              //toolTip = "Realistic Transfers is off, or Data is transferable";
              toolTip = SmUtils.SmTags["#smloc_transfer_tt_023"];
              GUI.enabled = true;
            }
            if (SMAddon.SmVessel.SelectedModuleTarget != null && scienceCount > 0)
            {
              //GUIContent content = new GUIContent("Xfer", toolTip);
              GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_tt_009"], toolTip);
              if (GUILayout.Button(content, SMStyle.ButtonStyle, GUILayout.Width(40), GUILayout.Height(20)))
              {
                if (((ModuleScienceContainer)SMAddon.SmVessel.SelectedModuleTarget).AddData(((ScienceData)items.Current)))
                  ((IScienceDataContainer)modules.Current).DumpData(((ScienceData)items.Current));
              }
              rect = GUILayoutUtility.GetLastRect();
              if (Event.current.type == EventType.Repaint && ShowToolTips)
                ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
            }
            GUILayout.EndHorizontal();
          }
        }
        GUI.enabled = true;
      }
      modules.Dispose();
    }

    private static void ScienceDetailsTarget()
    {
      const float xOffset = 30;
      if (SMAddon.SmVessel.SelectedPartsTarget.Count <= 0) return;
      int count =
        SMAddon.SmVessel.SelectedPartsTarget[0].Modules.Cast<PartModule>()
          .Count(tpm => tpm is IScienceDataContainer && tpm.moduleName != "ModuleScienceExperiment");

      List<PartModule>.Enumerator modules = SMAddon.SmVessel.SelectedPartsTarget[0].Modules.GetEnumerator();
      while (modules.MoveNext())
      {
        if (modules.Current == null) continue;
        // Containers.
        if (!(modules.Current is IScienceDataContainer) || (modules.Current).moduleName == "ModuleScienceExperiment") continue;
        int scienceCount = ((IScienceDataContainer)modules.Current).GetScienceCount();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{modules.Current.moduleName} - ({scienceCount})", GUILayout.Width(220),
          GUILayout.Height(20));
        // set the conditions for a button style change.
        bool isReceiveToggled = false;
        if (modules.Current == SMAddon.SmVessel.SelectedModuleTarget)
          isReceiveToggled = true;
        else if (count == 1)
        {
          SMAddon.SmVessel.SelectedModuleTarget = modules.Current;
          isReceiveToggled = true;
        }
        //SelectedModuleTarget = pm;
        GUIStyle style = isReceiveToggled ? SMStyle.ButtonToggledTargetStyle : SMStyle.ButtonStyle;

        // Only containers can receive science data
        if (modules.Current.moduleName != "ModuleScienceExperiment")
        {
          //GUIContent content = new GUIContent("Recv", "Set this module as the receiving container");
          GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_013"], SmUtils.SmTags["#smloc_transfer_tt_024"]);
          if (GUILayout.Button(content, style, GUILayout.Width(40), GUILayout.Height(20)))
          {
            SMAddon.SmVessel.SelectedModuleTarget = modules.Current;
          }
          Rect rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && ShowToolTips)
            ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
        }
        GUILayout.EndHorizontal();
      }
      modules.Dispose();
    }
    #endregion

    #region Resources Details Viewer

    private static void ResourceDetailsViewer(TransferPump.TypeXfer pumpType)
    {
      Rect rect;
      // Let's get the pump objects we may use...
      List<string> selectedResources = SMAddon.SmVessel.SelectedResources;
      List<TransferPump> pumps = TransferPump.GetDisplayPumpsByType(pumpType);
      if (!(from pump in pumps where pump.FromParts.Count > 0 select pump).Any()) return;

      // This routine assumes that a resource has been selected on the Resource manifest window.
      // Set scrollX offsets for left and right viewers
      const int xOffset = 30;
      string toolTip;

      // Set pump ratios
      TransferPump activePump = TransferPump.GetRatioPump(pumps);
      TransferPump ratioPump = TransferPump.GetRatioPump(pumps, true);
      activePump.PumpRatio = 1;
      ratioPump.PumpRatio = TransferPump.CalcRatio(pumps);

      // Resource Flow control Display loop
      ResourceFlowButtons(pumpType, xOffset);

      // let's determine how much of a resource we can move to the target.
      double thisXferAmount = double.Parse(activePump.EditSliderAmount);
      double maxPumpAmount = TransferPump.CalcMaxPumpAmt(pumps[0].FromParts, pumps[0].ToParts, selectedResources);
      if (maxPumpAmount <= 0 && !TransferPump.PumpProcessOn) return;

      // Xfer Controls Display
      GUILayout.BeginHorizontal();
      string label;
      if (TransferPump.PumpProcessOn)
      {
        // We want to show this during transfer if the direction is correct...
        if (activePump.PumpType == pumpType)
        {
          //GUILayout.Label("Xfer Remaining:", GUILayout.Width(120));
          GUILayout.Label($"{SmUtils.SmTags["#smloc_transfer_014"]}:", GUILayout.Width(120));
          
          GUILayout.Label(activePump.PumpBalance.ToString("#######0.##"));
          if (SMAddon.SmVessel.SelectedResources.Count > 1)
            GUILayout.Label($" | {ratioPump.PumpBalance:#######0.##}");
        }
      }
      else
      {
        if (selectedResources.Count > 1)
        {
          //label = "Xfer Amts:";
          //toolTip = "Displays xfer amounts of both resourses selected.";
          //toolTip += "\r\nAllows editing of part's larger capacity resourse xfer value.";
          //toolTip += "\r\nIt then calculates the smaller xfer amount using a ratio";
          //toolTip += "\r\n of the smaller capacity resource to the larger.";
          label = $"{SmUtils.SmTags["#smloc_transfer_015"]}:";
          toolTip = SmUtils.SmTags["#smloc_transfer_tt_025"];
        }
        else
        {
          //label = "Xfer Amt:";
          //toolTip += "Displays the Amount of selected resource to xfer.";
          //toolTip += "\r\nAllows editing of the xfer value.";
          label = $"{SmUtils.SmTags["#smloc_transfer_016"]}:";
          toolTip = SmUtils.SmTags["#smloc_transfer_tt_026"];
        }
        GUILayout.Label(new GUIContent(label, toolTip), GUILayout.Width(65));
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
        activePump.EditSliderAmount = GUILayout.TextField(activePump.EditSliderAmount, 20, GUILayout.Width(95),
          GUILayout.Height(20));
        thisXferAmount = double.Parse(activePump.EditSliderAmount);
        double ratioXferAmt = thisXferAmount * ratioPump.PumpRatio > ratioPump.FromCapacity
          ? ratioPump.FromCapacity
          : thisXferAmount * ratioPump.PumpRatio;
        if (SMAddon.SmVessel.SelectedResources.Count > 1)
        {
          label = $" | {ratioXferAmt:#######0.##}";
          //toolTip = "Smaller Tank xfer amount.  Calculated at " + ratioPump.PumpRatio + ".\r\n(Note: A value of 0.818181 = 0.9/1.1)";
          toolTip = $"{SmUtils.SmTags["#smloc_transfer_tt_027"]}:  {ratioPump.PumpRatio}.\n{SmUtils.SmTags["#smloc_transfer_tt_028"]}";
          GUILayout.Label(new GUIContent(label, toolTip), GUILayout.Width(80));
          rect = GUILayoutUtility.GetLastRect();
          if (Event.current.type == EventType.Repaint && ShowToolTips)
            ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
        }
      }
      GUILayout.EndHorizontal();

      if (SMConditions.IsShipControllable() && (SMConditions.CanResourceBeXferred(pumpType, maxPumpAmount) || activePump.PumpType == pumpType && activePump.IsPumpOn))
      {
        GUILayout.BeginHorizontal();
        GUIStyle noPad = SMStyle.LabelStyleNoPad;
        //label = "Xfer:";
        //toolTip = "Xfer amount slider control.\r\nMove slider to select a different value.\r\nYou can use this instead of the text box above.";
        label = $"{SmUtils.SmTags["#smloc_transfer_009"]}:";
        toolTip = SmUtils.SmTags["#smloc_transfer_tt_029"];
        GUILayout.Label(new GUIContent(label, toolTip), noPad, GUILayout.Width(50), GUILayout.Height(20));
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
        thisXferAmount = GUILayout.HorizontalSlider((float)thisXferAmount, 0, (float)maxPumpAmount,
          GUILayout.Width(190));
        activePump.EditSliderAmount = thisXferAmount.ToString(CultureInfo.InvariantCulture);
        // set Xfer button style
        //GUIContent xferContent = !TransferPump.PumpProcessOn
        //  ? new GUIContent("Xfer", "Transfers the selected resource(s)\r\nto the selected Part(s)")
        //  : new GUIContent("Stop", "Halts the Transfer of the selected resource(s)\r\nto the selected Part(s)");
        GUIContent xferContent = !TransferPump.PumpProcessOn || activePump.PumpType == pumpType && !activePump.IsPumpOn
          ? new GUIContent(SmUtils.SmTags["#smloc_transfer_009"], SmUtils.SmTags["#smloc_transfer_tt_030"]) // Xfer
          : new GUIContent(SmUtils.SmTags["#smloc_transfer_005"], SmUtils.SmTags["#smloc_transfer_tt_031"]); // Stop
        GUI.enabled = !TransferPump.PumpProcessOn || activePump.PumpType == pumpType && activePump.IsPumpOn;
        if (GUILayout.Button(xferContent, GUILayout.Width(40), GUILayout.Height(18)))
        {
          uint pumpId = TransferPump.GetPumpIdFromHash(string.Join("", selectedResources.ToArray()),
            pumps[0].FromParts.First(), pumps[0].ToParts.Last(), pumpType, TransferPump.TriggerButton.Transfer);
          if (!TransferPump.PumpProcessOn)
          {
            //Calc amounts and update xfer modules
            TransferPump.AssignPumpAmounts(pumps, thisXferAmount, pumpId);
            ProcessController.TransferResources(pumps);
          }
          else TransferPump.AbortAllPumpsInProcess(pumpId);
        }
        rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint && ShowToolTips)
          ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, xOffset);
        GUILayout.EndHorizontal();
      }
    }

    private static void ResourceFlowButtons(TransferPump.TypeXfer pumpType, int scrollX)
    {
      string step = "";
      try
      {
        List<TransferPump>.Enumerator displayPumps = TransferPump.GetDisplayPumpsByType(pumpType).GetEnumerator();
        while (displayPumps.MoveNext())
        {
          if (displayPumps.Current == null) continue;
          // this var is used for button state change management
          bool flowState = displayPumps.Current.FromParts.Any(part => part.Resources[displayPumps.Current.Resource].flowState);
          //string flowtext = flowState ? "On" : "Off";
          string flowtext = flowState ? SmUtils.SmTags["#smloc_transfer_017"] : SmUtils.SmTags["#smloc_transfer_018"];

          // Flow control Display
          step = "resource quantities labels";

          GUILayout.BeginHorizontal();
          string label =
            $"{displayPumps.Current.Resource}: ({displayPumps.Current.FromRemaining:#######0.##}/{displayPumps.Current.FromCapacity:######0.##})";
          GUILayout.Label(label , SMStyle.LabelStyleNoWrap, GUILayout.Width(220),GUILayout.Height(18));
          GUILayout.Label(flowtext, GUILayout.Width(20), GUILayout.Height(18));
          if (SMAddon.SmVessel.Vessel.IsControllable)
          {
            step = "render flow button(s)";
            //GUIContent content = new GUIContent("Flow", "Enables/Disables flow of selected resource(s) from selected part(s).");
            GUIContent content = new GUIContent(SmUtils.SmTags["#smloc_transfer_019"], SmUtils.SmTags["#smloc_transfer_tt_032"]);
            if (GUILayout.Button(content, GUILayout.Width(40), GUILayout.Height(20)))
            {
              List<Part>.Enumerator parts = displayPumps.Current.FromParts.GetEnumerator();
              while (parts.MoveNext())
              {
                if (parts.Current == null) continue;
                parts.Current.Resources[displayPumps.Current.Resource].flowState = !flowState;
              }
              parts.Dispose();
            }
            Rect rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && ShowToolTips)
              ToolTip = SMToolTips.SetActiveToolTip(rect, GUI.tooltip, ref ToolTipActive, scrollX);
          }
          GUILayout.EndHorizontal();
        }
        displayPumps.Dispose();
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SmUtils.LogMessage(
            $" in WindowTransfer.ResourceFlowButtons at step:  {step}.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SmUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }
    #endregion

    #endregion

    #endregion

    #region Button Action Methods

    private static void PartButtonToggled(TransferPump.TypeXfer xferType, Part part)
    {
      string step = "Part Button Toggled";
      try
      {
        if (SMConditions.IsTransferInProgress()) return;
        if (xferType == TransferPump.TypeXfer.SourceToTarget)
        {
          // Now lets update the list...
          if (SMAddon.SmVessel.SelectedPartsSource.Contains(part))
          {
            SMAddon.SmVessel.SelectedPartsSource.Remove(part);
          }
          else
          {
            if (!SMConditions.AreSelectedResourcesTypeOther(SMAddon.SmVessel.SelectedResources))
              SMAddon.SmVessel.SelectedPartsSource.Clear();
            SMAddon.SmVessel.SelectedPartsSource.Add(part);
          }
          if (SMConditions.IsClsActive())
          {
            SMAddon.UpdateClsSpaces();
          }
          SMAddon.SmVessel.SelectedModuleSource = null;
          _scienceModulesSource = null;
        }
        else
        {
          if (SMAddon.SmVessel.SelectedPartsTarget.Contains(part))
          {
            SMAddon.SmVessel.SelectedPartsTarget.Remove(part);
          }
          else
          {
            if (!SMConditions.AreSelectedResourcesTypeOther(SMAddon.SmVessel.SelectedResources))
              SMAddon.SmVessel.SelectedPartsTarget.Clear();
            SMAddon.SmVessel.SelectedPartsTarget.Add(part);
          }
          SMAddon.SmVessel.SelectedModuleTarget = null;
        }
        step = "Set Xfer amounts?";
        if (!SMConditions.AreSelectedResourcesTypeOther(SMAddon.SmVessel.SelectedResources)) return;
        TransferPump.UpdateDisplayPumps();
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SmUtils.LogMessage(
            $"Error in WindowTransfer.PartButtonToggled ({xferType}) at step:  {step}.  Error:  {ex}",
            SmUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    private static void VesselButtonToggled(TransferPump.TypeXfer xferType, ModDockedVessel modVessel)
    {
      string step = "Vessel Button Toggled";
      try
      {
        if (xferType == TransferPump.TypeXfer.SourceToTarget)
        {
          // Now lets update the list...
          // Because IEquatable is not implemented by ModDockedVessel, we need to inspect the object for equality.
          // Additionally, we cannot use the object directly for a List<T>.Remove. Use the found object instead.
          // Since the objects are reconstructed during Refresh Lists, the objects are no longer reference objects, but value Objects.
          // I may play with IEquatable to see if it suits my needs at a later date, or alter the object to be consumed differently.
          ModDockedVessel modDockedVessel =
            SMAddon.SmVessel.SelectedVesselsSource.Find(v => v.VesselInfo.rootPartUId == modVessel.VesselInfo.rootPartUId);
          if (modDockedVessel != null)
            SMAddon.SmVessel.SelectedVesselsSource.Remove(modDockedVessel);
          else
            SMAddon.SmVessel.SelectedVesselsSource.Add(modVessel);
          SMAddon.SmVessel.SelectedPartsSource =
            SMAddon.SmVessel.GetVesselsPartsByResource(SMAddon.SmVessel.SelectedVesselsSource,
              SMAddon.SmVessel.SelectedResources);
        }
        else
        {
          ModDockedVessel modDockedVessel =
            SMAddon.SmVessel.SelectedVesselsTarget.Find(v => v.VesselInfo.rootPartUId == modVessel.VesselInfo.rootPartUId);
          if (modDockedVessel != null)
            SMAddon.SmVessel.SelectedVesselsTarget.Remove(modDockedVessel);
          else
            SMAddon.SmVessel.SelectedVesselsTarget.Add(modVessel);
          SMAddon.SmVessel.SelectedPartsTarget =
            SMAddon.SmVessel.GetVesselsPartsByResource(SMAddon.SmVessel.SelectedVesselsTarget,
              SMAddon.SmVessel.SelectedResources);
        }
        WindowManifest.ResolveResourcePartSelections(SMAddon.SmVessel.SelectedResources);
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SmUtils.LogMessage(
            $"Error in WindowTransfer.VesselButtonToggled ({xferType}) at step:  {step}.  Error:  {ex}",
            SmUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    #endregion

    #region Utilities

    internal static ProtoCrewMember FindFrozenKerbal(string crewName)
    {
      ProtoCrewMember retval = null;
      IEnumerator<ProtoCrewMember> crew = HighLogic.CurrentGame.CrewRoster.Unowned.GetEnumerator();
      while (crew.MoveNext())
      {
        if (crew.Current == null) continue;
        if (crew.Current.name != crewName) continue;
        retval = crew.Current;
        break;
      }
      crew.Dispose();
      return retval;
    }

    private static bool CanDumpPart(Part part)
    {
      bool isDumpable;
      if (SMAddon.SmVessel.SelectedResources.Count > 1)
        isDumpable = part.Resources[SMAddon.SmVessel.SelectedResources[0]].amount > 0 ||
                     part.Resources[SMAddon.SmVessel.SelectedResources[1]].amount > 0;
      else
        isDumpable = part.Resources[SMAddon.SmVessel.SelectedResources[0]].amount > 0;

      return isDumpable;
    }

    private static bool CanSelectVessel(TransferPump.TypeXfer xferType, ModDockedVessel modDockedVessel)
    {
      bool isSelectable = true;
      if (xferType == TransferPump.TypeXfer.SourceToTarget)
      {
        if (SMAddon.SmVessel.SelectedVesselsTarget.Find(v => v.VesselInfo.rootPartUId == modDockedVessel.VesselInfo.rootPartUId) != null)
          isSelectable = false;
      }
      else
      {
        if (SMAddon.SmVessel.SelectedVesselsSource.Find(v => v.VesselInfo.rootPartUId == modDockedVessel.VesselInfo.rootPartUId) != null)
          isSelectable = false;
      }
      return isSelectable;
    }

    private static GUIStyle GetPartButtonStyle(TransferPump.TypeXfer xferType, Part part)
    {
      GUIStyle style;
      if (xferType == TransferPump.TypeXfer.SourceToTarget)
      {
        style = SMAddon.SmVessel.SelectedPartsSource.Contains(part)
          ? SMStyle.ButtonToggledSourceStyle
          : SMStyle.ButtonSourceStyle;
      }
      else
      {
        style = SMAddon.SmVessel.SelectedPartsTarget.Contains(part)
          ? SMStyle.ButtonToggledTargetStyle
          : SMStyle.ButtonTargetStyle;
      }
      return style;
    }

    private static GUIStyle GetVesselButtonStyle(TransferPump.TypeXfer xferType, ModDockedVessel modDockedVessel)
    {
      GUIStyle style;
      if (xferType == TransferPump.TypeXfer.SourceToTarget)
      {
        style = SMAddon.SmVessel.SelectedVesselsSource.Find(v => v.VesselInfo.rootPartUId == modDockedVessel.VesselInfo.rootPartUId) != null
          ? SMStyle.ButtonToggledSourceStyle
          : SMStyle.ButtonSourceStyle;
      }
      else
      {
        style = SMAddon.SmVessel.SelectedVesselsTarget.Find(v => v.VesselInfo.rootPartUId == modDockedVessel.VesselInfo.rootPartUId) != null
          ? SMStyle.ButtonToggledTargetStyle
          : SMStyle.ButtonTargetStyle;
      }
      return style;
    }

    private static string GetResourceDescription(IList<string> selectedResources, Part part)
    {
      string strDescription;

      if (selectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
      {
        strDescription = $"{SmUtils.GetPartCrewCount(part)} - {part.partInfo.title}";
      }
      else if (selectedResources.Contains(SMConditions.ResourceType.Science.ToString()))
      {
        int cntScience = GetScienceCount(part);
        strDescription = $"{cntScience} - {part.partInfo.title}";
      }
      else
      {
        strDescription = $"{part.Resources[selectedResources[0]].amount:######0.##} - {part.partInfo.title}";
      }
      return strDescription;
    }

    private static string GetResourceDescription(List<string> selectedResources, ModDockedVessel modDockedVvessel)
    {
      return $"{GetVesselResourceTotals(modDockedVvessel, selectedResources)} - {modDockedVvessel.VesselName}";
    }

    private static int GetScienceCount(Part part)
    {
      try
      {
        return part.Modules.OfType<IScienceDataContainer>().Sum(pm => pm.GetScienceCount());
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage($" in GetScienceCount.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}",
          SmUtils.LogType.Error, true);
        return 0;
      }
    }

    private static int GetScienceCount(ModDockedVessel vessel)
    {
      try
      {
        int count = 0;
        List<Part>.Enumerator part = vessel.VesselParts.GetEnumerator();
        while (part.MoveNext())
        {
          if (part.Current == null) continue;
          count += part.Current.Modules.OfType<IScienceDataContainer>().Sum(pm => pm.GetScienceCount());
        }
        part.Dispose();
        return count;
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage($" in GetScienceCount.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}",
          SmUtils.LogType.Error, true);
        return 0;
      }
    }

    internal static string GetVesselResourceTotals(ModDockedVessel modDockedVessel, List<string> selectedResources)
    {
      double currAmount = 0;
      double totAmount = 0;
      try
      {
        List<ModDockedVessel> modDockedVessels = new List<ModDockedVessel> { modDockedVessel };
        if (selectedResources.Contains(SMConditions.ResourceType.Crew.ToString()))
        {
          currAmount = SmUtils.GetPartsCrewCount(modDockedVessel.VesselParts);
          totAmount = SmUtils.GetPartsCrewCapacity(modDockedVessel.VesselParts);
        }
        else if (selectedResources.Contains(SMConditions.ResourceType.Science.ToString()))
        {
          currAmount = GetScienceCount(modDockedVessel);
          totAmount = currAmount;
        }
        else
        {
          List<Part>.Enumerator parts = SMAddon.SmVessel.GetVesselsPartsByResource(modDockedVessels, selectedResources).GetEnumerator();
          while (parts.MoveNext())
          {
            if (parts.Current == null) continue;
            currAmount += parts.Current.Resources[selectedResources[0]].amount;
            totAmount += parts.Current.Resources[selectedResources[0]].maxAmount;
          }
          parts.Dispose();          
        }
      }
      catch (Exception ex)
      {
        SmUtils.LogMessage($" in DisplayVesselResourceTotals().  Error:  {ex}", SmUtils.LogType.Error, true);
      }
      string displayAmount = $"({currAmount:#######0}/{totAmount:######0})";

      return displayAmount;
    }

    private static bool IsPartSelectable(string selectedResource, TransferPump.TypeXfer xferType, Part part)
    {
      if (selectedResource == SMConditions.ResourceType.Crew.ToString() ||
          selectedResource == SMConditions.ResourceType.Science.ToString()) return true;
      bool isSelectable = true;
      if (xferType == TransferPump.TypeXfer.SourceToTarget)
      {
        if (SMAddon.SmVessel.SelectedPartsTarget.Contains(part))
          isSelectable = false;
      }
      else
      {
        if (SMAddon.SmVessel.SelectedPartsSource.Contains(part))
          isSelectable = false;
      }
      return isSelectable;
    }

    private static int GetAvailableCrewSpace(List<Part> parts)
    {
      int results = 0;
      List<Part>.Enumerator part = parts.GetEnumerator();
      while (part.MoveNext())
      {
        if (part.Current == null) continue;
        results += part.Current.CrewCapacity - part.Current.protoModuleCrew.Count;
      }
      part.Dispose();
      return results;
    }
    #endregion
  }
}
