using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ConnectedLivingSpace;
using DF;  // DeepFreeze
using ShipManifest.APIClients;
using ShipManifest.Process;
using ShipManifest.Windows;


namespace ShipManifest
{
  [KSPAddon(KSPAddon.Startup.EveryScene, false)]
  // ReSharper disable once InconsistentNaming
  internal class SMAddon : MonoBehaviour
  {
    ///
    /// Object Scope:  Current Unity/KSP Scene.  Object will be destroyed and recreated when scene changes!
    ///

    #region Static Properties

    // Game object that keeps us running
    // internal static GameObject SmInstance;

    // current vessel's controller instance
    internal static SMVessel SmVessel;
    internal static ICLSAddon ClsAddon;

    internal static string TextureFolder = "ShipManifest/Textures/";
    internal static string SaveMessage = string.Empty;

    // DeepFreeze Frozen Crew interface
    internal static Dictionary<string, KerbalInfo> FrozenKerbals = new Dictionary<string, KerbalInfo>();

    // Resource transfer vars
    internal static AudioSource Source1;
    internal static AudioSource Source2;
    internal static AudioSource Source3;

    internal static AudioClip Sound1;
    internal static AudioClip Sound2;
    internal static AudioClip Sound3;

    [KSPField(isPersistant = true)]
    internal static double Elapsed;

    // Resource xfer vars
    internal static XferDirection XferMode = XferDirection.SourceToTarget;

    // Toolbar Integration.
    private static IButton _smButtonBlizzy;
    private static IButton _smSettingsBlizzy;
    private static IButton _smRosterBlizzy;
    private static ApplicationLauncherButton _smButtonStock;
    private static ApplicationLauncherButton _smSettingsStock;
    private static ApplicationLauncherButton _smRosterStock;

    // Repeating error latch
    internal static bool FrameErrTripped;

    // SM UI toggle
    internal static bool ShowUi = true;

    #endregion

    #region Event handlers

    void DummyHandler() { }

    // Addon state event handlers
    internal void Awake()
    {
      try
      {
        if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
          DontDestroyOnLoad(this);
          SMSettings.LoadSettings();
          Utilities.LogMessage("SmAddon.Awake Active...", "info", SMSettings.VerboseLogging);

          if (SMSettings.AutoSave)
            InvokeRepeating("RunSave", SMSettings.SaveIntervalSec, SMSettings.SaveIntervalSec);

          if (SMSettings.EnableBlizzyToolbar)
          {
            // Let't try to use Blizzy's toolbar
            Utilities.LogMessage("SmAddon.Awake - Blizzy Toolbar Selected.", "Info", SMSettings.VerboseLogging);
            if (!ActivateBlizzyToolBar())
            {
              // We failed to activate the toolbar, so revert to stock
              GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
              GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
              Utilities.LogMessage("SmAddon.Awake - Stock Toolbar Selected.", "Info", SMSettings.VerboseLogging);
            }
          }
          else
          {
            // Use stock Toolbar
            Utilities.LogMessage("SmAddon.Awake - Stock Toolbar Selected.", "Info", SMSettings.VerboseLogging);
            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
          }
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.Awake.  Error:  " + ex, "Error", true);
      }
    }
    internal void Start()
    {
      Utilities.LogMessage("SmAddon.Start.", "Info", SMSettings.VerboseLogging);
      try
      {
        // Reset frame error latch if set
        if (FrameErrTripped)
          FrameErrTripped = false;

        if (WindowRoster.ResetRosterSize)
          WindowRoster.Position.height = SMSettings.UseUnityStyle ? 330 : 350;

        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
          if (GetClsAddon())
          {
            SMSettings.ClsInstalled = true;
          }
          else
          {
            SMSettings.EnableCls = false;
            SMSettings.ClsInstalled = false;
          }
          // reset any hacked kerbal names in game save from old version of SM/KSP
          if (SMSettings.RenameWithProfession)
            WindowRoster.ResetKerbalNames();

          SMSettings.SaveSettings();
          //RunSave();
        }

        if (HighLogic.LoadedScene == GameScenes.FLIGHT)
        {
          // Instantiate Event handlers
          GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
          GameEvents.onVesselChange.Add(OnVesselChange);
          GameEvents.onPartDie.Add(OnPartDie);
          GameEvents.onPartExplode.Add(OnPartExplode);
          GameEvents.onPartUndock.Add(OnPartUndock);
          GameEvents.onStageSeparation.Add(OnStageSeparation);
          GameEvents.onUndock.Add(OnUndock);
          GameEvents.onVesselCreate.Add(OnVesselCreate);
          GameEvents.onVesselDestroy.Add(OnVesselDestroy);
          GameEvents.onVesselWasModified.Add(OnVesselWasModified);
          GameEvents.onVesselChange.Add(OnVesselChange);
          GameEvents.onVesselLoaded.Add(OnVesselLoaded);
          GameEvents.onVesselTerminated.Add(OnVesselTerminated);
          GameEvents.onFlightReady.Add(OnFlightReady);
          GameEvents.onCrewTransferred.Add(OnCrewTransferred);
          GameEvents.onShowUI.Add(OnShowUi);
          GameEvents.onHideUI.Add(OnHideUi);


          // get the current Vessel data
          SmVessel = SMVessel.GetInstance(FlightGlobals.ActiveVessel);

          // Is CLS installed and enabled?
          if (GetClsAddon())
          {
            SMSettings.ClsInstalled = true;
            SMSettings.SaveSettings();
            UpdateClsSpaces();
          }
          else
          {
            Utilities.LogMessage("Start - CLS is not installed.", "Info", SMSettings.VerboseLogging);
            SMSettings.EnableCls = false;
            SMSettings.ClsInstalled = false;
            SMSettings.SaveSettings();
          }
          Utilities.LogMessage("CLS Installed?  " + SMSettings.ClsInstalled, "Info", SMSettings.VerboseLogging);
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.Start.  " + ex, "Error", true);
      }
    }
    internal void OnDestroy()
    {
      //Debug.Log("[ShipManifest]:  SmAddon.OnDestroy");
      try
      {
        if (HighLogic.LoadedSceneIsFlight)
          WindowControl.ShowWindow = WindowManifest.ShowWindow = WindowTransfer.ShowWindow = WindowRoster.ShowWindow = WindowSettings.ShowWindow = false;
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
          WindowRoster.ShowWindow = WindowSettings.ShowWindow = false;

        if (SMSettings.Loaded)
          SMSettings.SaveSettings();

        GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
        GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        GameEvents.onVesselChange.Remove(OnVesselChange);
        GameEvents.onPartDie.Remove(OnPartDie);
        GameEvents.onPartExplode.Remove(OnPartExplode);
        GameEvents.onPartUndock.Remove(OnPartUndock);
        GameEvents.onStageSeparation.Remove(OnStageSeparation);
        GameEvents.onUndock.Remove(OnUndock);
        GameEvents.onVesselCreate.Remove(OnVesselCreate);
        GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        GameEvents.onVesselChange.Remove(OnVesselChange);
        GameEvents.onVesselTerminated.Remove(OnVesselTerminated);
        GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
        GameEvents.onFlightReady.Remove(OnFlightReady);
        GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
        GameEvents.onHideUI.Remove(OnHideUi);
        GameEvents.onShowUI.Remove(OnShowUi);

        CancelInvoke("RunSave");

        // Handle Toolbars
        if (_smRosterBlizzy == null && _smSettingsBlizzy == null && _smButtonBlizzy == null)
        {
          if (_smButtonStock != null)
          {
            ApplicationLauncher.Instance.RemoveModApplication(_smButtonStock);
            _smButtonStock = null;
          }
          if (_smSettingsStock != null)
          {
            ApplicationLauncher.Instance.RemoveModApplication(_smSettingsStock);
            _smSettingsStock = null;
          }
          if (_smRosterStock != null)
          {
            ApplicationLauncher.Instance.RemoveModApplication(_smRosterStock);
            _smRosterStock = null;
          }
          if (_smButtonStock == null && _smSettingsStock == null && _smRosterStock == null)
          {
            // Remove the stock toolbar button launcher handler
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
          }
        }
        else
        {
          if (_smButtonBlizzy != null)
            _smButtonBlizzy.Destroy();
          if (_smRosterBlizzy != null)
            _smRosterBlizzy.Destroy();
          if (_smSettingsBlizzy != null)
            _smSettingsBlizzy.Destroy();
        }
        //Reset Roster Window data
        WindowRoster.OnCreate = false;
        WindowRoster.SelectedKerbal = null;
        WindowRoster.ToolTip = "";
        //WindowRoster.ShowWindow = false;

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  SmAddon.OnDestroy.  " + ex, "Error", true);
      }
    }
    // ReSharper disable once InconsistentNaming
    internal void OnGUI()
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnGUI");
      try
      {
        GUI.skin = SMSettings.UseUnityStyle ? null : HighLogic.Skin;

        SMStyle.SetupGuiStyles();
        Display();

        SMToolTips.ShowToolTips();

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnGUI.  " + ex, "Error", true);
      }
    }
    internal void Update()
    {
      try
      {
        CheckForToolbarTypeToggle();

        if (HighLogic.LoadedScene == GameScenes.FLIGHT)
        {
          if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null)
          {
            //Instantiate the controller for the active vessel.
            SmVessel = SMVessel.GetInstance(FlightGlobals.ActiveVessel);
            SMHighlighter.Update_Highlighter();

            // Realism Mode Resource transfer operation (real time)
            // XferOn is flagged in the Resource Controller
            if (TransferResource.ResourceXferActive)
            {
              TransferResource.ResourceTransferProcess();
            }

            // Realism Mode Crew transfer operation (real time)
            if (SmVessel.TransferCrewObj.CrewXferActive)
              SmVessel.TransferCrewObj.CrewTransferProcess();
            else if (SmVessel.TransferCrewObj.IsStockXfer)
            {
              TransferCrew.RevertCrewTransfer(SmVessel.TransferCrewObj.FromCrewMember, SmVessel.TransferCrewObj.FromPart, SmVessel.TransferCrewObj.ToPart);
              SmVessel.TransferCrewObj.CrewTransferBegin(SmVessel.TransferCrewObj.FromCrewMember, SmVessel.TransferCrewObj.FromPart, SmVessel.TransferCrewObj.ToPart);
            }

            if (SMSettings.EnableOnCrewTransferEvent && TransferCrew.FireSourceXferEvent)
            {
              // Now let's deal with third party mod support...
              TransferCrew.FireSourceXferEvent = false;
              GameEvents.onCrewTransferred.Fire(TransferCrew.SourceAction);

              //If a swap, we need to handle that too...
              if (TransferCrew.FireTargetXferEvent)
              {
                TransferCrew.FireTargetXferEvent = false;
                GameEvents.onCrewTransferred.Fire(TransferCrew.TargetAction);
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        if (!FrameErrTripped)
        {
          Utilities.LogMessage(string.Format(" in Update (repeating error).  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
          FrameErrTripped = true;
        }
      }
    }

    // save settings on scene changes
    private void OnGameSceneLoadRequested(GameScenes requestedScene)
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnGameSceneLoadRequested");
      SMSettings.SaveSettings();
      //RunSave();
      //if (SMSettings.Loaded)
      //{
      //    RunSave();
      //    SMSettings.SaveSettings();
      //}
    }

    // SM UI toggle handlers
    private void OnShowUi()
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnShowUI");
      ShowUi = true;
    }
    private void OnHideUi()
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnHideUI");
      ShowUi = false;
    }

    // Crew Event handlers
    internal void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> action)
    {
      if ((action.host == TransferCrew.SourceAction.host && action.from == TransferCrew.SourceAction.from && action.to == TransferCrew.SourceAction.to)
          || action.host == TransferCrew.TargetAction.host && action.from == TransferCrew.TargetAction.from && action.to == TransferCrew.TargetAction.to)
      {
        // We are performing a mod notification. Ignore the event.
        return;
      }
      if (!SmVessel.TransferCrewObj.CrewXferActive && (!SMSettings.OverrideStockCrewXfer ||
          action.to.Modules.Cast<PartModule>().Any(x => x is KerbalEVA) ||
          action.from.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)))
      {
        // no SM crew Xfers in progress, so Non-override stock Xfers and EVAs require no action
        return;
      }

      if (SmVessel.TransferCrewObj.CrewXferActive)
      {
        // Remove the transfer message that stock displayed. 
        var failMessage = string.Format("<color=orange>{0} is unable to xfer to {1}.  An SM Crew Xfer is in progress</color>", action.host.name, action.to.partInfo.title);
        DisplayScreenMsg(failMessage);
        TransferCrew.RevertCrewTransfer(action.host, action.from, action.to);
      }
      else
      {
        //Check for DeepFreezer full. if full, abort handling Xfer.
        if (DFInterface.IsDFInstalled && action.to.Modules.Contains("DeepFreezer"))
          // ReSharper disable once SuspiciousTypeConversion.Global
          if (((IDeepFreezer)action.to.Modules["DeepFreezer"]).DFIFreezerSpace == 0)
            return;

        // If we are here, then we want to override the Stock Xfer...
        RemoveScreenMsg();

        // store data from event.
        SmVessel.TransferCrewObj.FromPart = action.from;
        SmVessel.TransferCrewObj.ToPart = action.to;
        SmVessel.TransferCrewObj.FromCrewMember = action.host;
        if (SmVessel.TransferCrewObj.FromPart != null && SmVessel.TransferCrewObj.ToPart != null)
          SmVessel.TransferCrewObj.IsStockXfer = true;
      }
    }

    internal static void DisplayScreenMsg(string strMessage)
    {
      var smessage = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.LOWER_CENTER);
      var smessages = FindObjectOfType<ScreenMessages>();
      if (smessages != null)
      {
        var smessagesToRemove = smessages.activeMessages.Where(x => Math.Abs(x.startTime - smessage.startTime) < SMSettings.Tolerance && x.style == ScreenMessageStyle.LOWER_CENTER).ToList();
        foreach (var m in smessagesToRemove)
          ScreenMessages.RemoveMessage(m);
        var failmessage = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.UPPER_CENTER);
        ScreenMessages.PostScreenMessage(strMessage, failmessage, true);
      }
    }

    internal static void RemoveScreenMsg()
    {
      var smessage = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.LOWER_CENTER);
      var smessages = FindObjectOfType<ScreenMessages>();
      if (smessages != null)
      {
        var smessagesToRemove = smessages.activeMessages.Where(x => Math.Abs(x.startTime - smessage.startTime) < SMSettings.Tolerance && x.style == ScreenMessageStyle.LOWER_CENTER).ToList();
        foreach (var m in smessagesToRemove)
          ScreenMessages.RemoveMessage(m);
      }
    }

    //Vessel state handlers
    internal void OnVesselWasModified(Vessel modVessel)
    {
      Utilities.LogMessage("SmAddon.OnVesselWasModified.", "Info", SMSettings.VerboseLogging);
      try
      {
        SMHighlighter.ClearResourceHighlighting(SmVessel.SelectedResourcesParts);
        UpdateSMcontroller(modVessel);
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnVesselWasModified.  " + ex, "Error", true);
      }
    }
    internal void OnVesselChange(Vessel newVessel)
    {
      //Utilities.LogMessage("SmAddon.OnVesselChange active...", "Info", true);
      Utilities.LogMessage("SmAddon.OnVesselChange active...", "Info", SMSettings.VerboseLogging);
      try
      {
        SMHighlighter.ClearResourceHighlighting(SmVessel.SelectedResourcesParts);
        UpdateSMcontroller(newVessel);
      }
      catch (Exception ex)
      {
        Utilities.LogMessage(string.Format(" in SmAddon.OnVesselChange.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
      }
    }
    private void OnFlightReady()
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnFlightReady");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnFlightReady.  " + ex, "Error", true);
      }
    }
    private void OnVesselLoaded(Vessel data)
    {
      Utilities.LogMessage("SmAddon.OnVesselLoaded active...", "Info", SMSettings.VerboseLogging);
      try
      {
        if (data.Equals(FlightGlobals.ActiveVessel) && data != SmVessel.Vessel)
        {
          SMHighlighter.ClearResourceHighlighting(SmVessel.SelectedResourcesParts);
          UpdateSMcontroller(data);
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  SmAddon.OnVesselLoaded.  " + ex, "Error", true);
      }
    }
    private void OnVesselTerminated(ProtoVessel data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnVesselTerminated");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnVesselTerminated.  " + ex, "Error", true);
      }
    }
    private void OnPartDie(Part data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnPartDie");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnPartDie.  " + ex, "Error", true);
      }
    }
    private void OnPartExplode(GameEvents.ExplosionReaction data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnPartExplode");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnPartExplode.  " + ex, "Error", true);
      }
    }
    private void OnPartUndock(Part data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnPartUndock");
      try
      {
        Utilities.LogMessage("OnPartUnDock:  Active. - Part name:  " + data.partInfo.name, "Info", SMSettings.VerboseLogging);
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnPartUndock.  " + ex, "Error", true);
      }
    }
    private void OnStageSeparation(EventReport eventReport)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnStageSeparation");
      try
      {
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnStageSeparation.  " + ex, "Error", true);
      }
    }
    private void OnUndock(EventReport eventReport)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnUndock");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnUndock.  " + ex, "Error", true);
      }
    }
    private void OnVesselDestroy(Vessel data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnVesselDestroy");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnVesselDestroy.  " + ex, "Error", true);
      }
    }
    private void OnVesselCreate(Vessel data)
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnVesselCreate");
      try
      {

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnVesselCreate.  " + ex, "Error", true);
      }
    }

    // Stock vs Blizzy Toolbar switch handler
    private void CheckForToolbarTypeToggle()
    {
      if (SMSettings.EnableBlizzyToolbar && !SMSettings.PrevEnableBlizzyToolbar)
      {
        // Let't try to use Blizzy's toolbar
        Utilities.LogMessage("CheckForToolbarToggle - Blizzy Toolbar Selected.", "Info", SMSettings.VerboseLogging);
        if (!ActivateBlizzyToolBar())
        {
          // We failed to activate the toolbar, so revert to stock
          GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);

          Utilities.LogMessage("SmAddon.Awake - Stock Toolbar Selected.", "Info", SMSettings.VerboseLogging);
          SMSettings.EnableBlizzyToolbar = SMSettings.PrevEnableBlizzyToolbar;
        }
        else
        {
          OnGuiAppLauncherDestroyed();
          GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGuiAppLauncherDestroyed);
          SMSettings.PrevEnableBlizzyToolbar = SMSettings.EnableBlizzyToolbar;
          if (HighLogic.LoadedSceneIsFlight)
            _smButtonBlizzy.Visible = true;
          if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
          {
            _smRosterBlizzy.Visible = true;
            _smSettingsBlizzy.Visible = true;
          }
        }

      }
      else if (!SMSettings.EnableBlizzyToolbar && SMSettings.PrevEnableBlizzyToolbar)
      {
        // Use stock Toolbar
        Utilities.LogMessage("SmAddon.Awake - Stock Toolbar Selected.", "Info", SMSettings.VerboseLogging);
        if (HighLogic.LoadedSceneIsFlight)
          _smButtonBlizzy.Visible = false;
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
          _smRosterBlizzy.Visible = false;
          _smSettingsBlizzy.Visible = false;
        }
        GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);
        OnGuiAppLauncherReady();
        SMSettings.PrevEnableBlizzyToolbar = SMSettings.EnableBlizzyToolbar;
      }
    }

    // Stock Toolbar Startup and cleanup
    private void OnGuiAppLauncherReady()
    {
      Utilities.LogMessage("SmAddon.OnGUIAppLauncherReady active...", "Info", SMSettings.VerboseLogging);
      try
      {
        // Setup SM WIndow button
        if (HighLogic.LoadedSceneIsFlight && _smButtonStock == null && !SMSettings.EnableBlizzyToolbar)
        {
          var iconfile = "IconOff_38";
          _smButtonStock = ApplicationLauncher.Instance.AddModApplication(
              OnSmButtonToggle,
              OnSmButtonToggle,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
              GameDatabase.Instance.GetTexture(TextureFolder + iconfile, false));

          if (WindowManifest.ShowWindow)
            _smButtonStock.SetTexture(GameDatabase.Instance.GetTexture(WindowManifest.ShowWindow ? TextureFolder + "IconOn_38" : TextureFolder + "IconOff_38", false));
        }

        // Setup Settings Button
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER && _smSettingsStock == null && !SMSettings.EnableBlizzyToolbar)
        {
          var iconfile = "IconS_Off_38";
          _smSettingsStock = ApplicationLauncher.Instance.AddModApplication(
              OnSmSettingsToggle,
              OnSmSettingsToggle,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              ApplicationLauncher.AppScenes.SPACECENTER,
              GameDatabase.Instance.GetTexture(TextureFolder + iconfile, false));

          if (WindowSettings.ShowWindow)
            _smSettingsStock.SetTexture(GameDatabase.Instance.GetTexture(WindowSettings.ShowWindow ? TextureFolder + "IconS_On_38" : TextureFolder + "IconS_Off_38", false));
        }

        // Setup Roster Button
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER && _smRosterStock == null && !SMSettings.EnableBlizzyToolbar)
        {
          var iconfile = "IconR_Off_38";
          _smRosterStock = ApplicationLauncher.Instance.AddModApplication(
              OnSmRosterToggle,
              OnSmRosterToggle,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              DummyHandler,
              ApplicationLauncher.AppScenes.SPACECENTER,
              GameDatabase.Instance.GetTexture(TextureFolder + iconfile, false));

          if (WindowRoster.ShowWindow)
            _smRosterStock.SetTexture(GameDatabase.Instance.GetTexture(WindowRoster.ShowWindow ? TextureFolder + "IconR_On_38" : TextureFolder + "IconR_Off_38", false));
        }

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnGUIAppLauncherReady.  " + ex, "Error", true);
      }
    }
    private void OnGuiAppLauncherDestroyed()
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnGUIAppLauncherDestroyed");
      try
      {
        if (_smButtonStock != null)
        {
          ApplicationLauncher.Instance.RemoveModApplication(_smButtonStock);
          _smButtonStock = null;
        }
        if (_smRosterStock != null)
        {
          ApplicationLauncher.Instance.RemoveModApplication(_smRosterStock);
          _smRosterStock = null;
        }
        if (_smSettingsStock != null)
        {
          ApplicationLauncher.Instance.RemoveModApplication(_smSettingsStock);
          _smSettingsStock = null;
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnGUIAppLauncherDestroyed.  " + ex, "Error", true);
      }
    }

    //Toolbar button click handlers
    internal static void OnSmButtonToggle()
    {
      //Debug.Log("[ShipManifest]:  ShipManifestAddon.OnSMButtonToggle");
      try
      {
        if (WindowManifest.ShowWindow)
        {
          // SM is showing.  Turn off.
          if (SmVessel.TransferCrewObj.CrewXferActive || TransferResource.ResourceXferActive)
            return;

          SMHighlighter.ClearResourceHighlighting(SmVessel.SelectedResourcesParts);
          SmVessel.SelectedResources.Clear();
          SmVessel.SelectedPartsSource.Clear();
          SmVessel.SelectedPartsTarget.Clear();
          WindowManifest.ShowWindow = !WindowManifest.ShowWindow;
        }
        else
        {
          // SM is not showing. turn on if we can.
          if (CanShowShipManifest(true))
            WindowManifest.ShowWindow = !WindowManifest.ShowWindow;
          else
            return;
        }

        if (SMSettings.EnableBlizzyToolbar)
          _smButtonBlizzy.TexturePath = WindowManifest.ShowWindow ? TextureFolder + "IconOn_24" : TextureFolder + "IconOff_24";
        else
          _smButtonStock.SetTexture(GameDatabase.Instance.GetTexture(WindowManifest.ShowWindow ? TextureFolder + "IconOn_38" : TextureFolder + "IconOff_38", false));

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnSMButtonToggle.  " + ex, "Error", true);
      }
    }
    internal static void OnSmRosterToggle()
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnSMRosterToggle");
      try
      {
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
          WindowRoster.ShowWindow = !WindowRoster.ShowWindow;
          if (SMSettings.EnableBlizzyToolbar)
            _smRosterBlizzy.TexturePath = WindowRoster.ShowWindow ? TextureFolder + "IconR_On_24" : TextureFolder + "IconR_Off_24";
          else
            _smRosterStock.SetTexture(GameDatabase.Instance.GetTexture(WindowRoster.ShowWindow ? TextureFolder + "IconR_On_38" : TextureFolder + "IconR_Off_38", false));

          FrozenKerbals = WindowRoster.GetFrozenKerbals();
        }

      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnSMRosterToggle.  " + ex, "Error", true);
      }
    }
    internal static void OnSmSettingsToggle()
    {
      Debug.Log("[ShipManifest]:  ShipManifestAddon.OnSMRosterToggle. Val:  " + WindowSettings.ShowWindow);
      try
      {
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        {
          WindowSettings.ShowWindow = !WindowSettings.ShowWindow;
          SMSettings.MemStoreTempSettings();
          if (SMSettings.EnableBlizzyToolbar)
            _smSettingsBlizzy.TexturePath = WindowSettings.ShowWindow ? TextureFolder + "IconS_On_24" : TextureFolder + "IconS_Off_24";
          else
            _smSettingsStock.SetTexture(GameDatabase.Instance.GetTexture(WindowSettings.ShowWindow ? TextureFolder + "IconS_On_38" : TextureFolder + "IconS_Off_38", false));
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.OnSMSettingsToggle.  " + ex, "Error", true);
      }
    }

    #endregion

    #region Logic Methods

    internal static bool CanKerbalsBeXferred(List<Part> selectedPartsSource, List<Part> selectedPartsTarget)
    {
      var results = false;
      try
      {
        if (SmVessel.TransferCrewObj.CrewXferActive || TransferResource.ResourceXferActive)
        {
          WindowTransfer.XferToolTip = "Transfer in progress.  Xfers disabled.";
          return false;
        }
        if (selectedPartsSource.Count == 0 || selectedPartsTarget.Count == 0)
        {
          WindowTransfer.XferToolTip = "Source or Target Part is not selected.\r\nPlease Select a Source AND a Target part.";
          return false;
        }
        if (selectedPartsSource[0] == selectedPartsTarget[0])
        {
          WindowTransfer.XferToolTip = "Source and Target Part are the same.\r\nUse Move Kerbal (>>) instead.";
          return false;
        }
        // If one of the parts is a DeepFreeze part and no crew are showing in protoModuleCrew, check it isn't full of frozen Kerbals. 
        // This is to prevent SM from Transferring crew into a DeepFreeze part that is full of frozen kerbals.
        // If there is just one spare seat or seat taken by a Thawed Kerbal that is OK because SM will just transfer them into the empty
        // seat or swap them with a thawed Kerbal.
        var sourcepartFrzr = selectedPartsSource[0].FindModuleImplementing<IDeepFreezer>();
        var targetpartFrzr = selectedPartsTarget[0].FindModuleImplementing<IDeepFreezer>();
        if (sourcepartFrzr != null)
        {
          if (sourcepartFrzr.DFIFreezerSpace == 0)
          {
            WindowTransfer.XferToolTip = "DeepFreeze Part is full of frozen kerbals.\r\nCannot Xfer until some are thawed.";
            return false;
          }
        }
        if (targetpartFrzr != null)
        {
          if (targetpartFrzr.DFIFreezerSpace == 0)
          {
            WindowTransfer.XferToolTip = "DeepFreeze Part is full of frozen kerbals.\r\nCannot Xfer until some are thawed.";
            return false;
          }
        }

        // Are there kerbals to move?
        if (selectedPartsSource[0].protoModuleCrew.Count == 0)
        {
          WindowTransfer.XferToolTip = "No Kerbals to Move.";
          return false;
        }
        // now if realism mode, are the parts connected to each other in the same living space?
        results = IsClsInSameSpace();
      }
      catch (Exception ex)
      {
        Utilities.LogMessage(string.Format(" in CanBeXferred.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
      }
      if (WindowTransfer.XferToolTip == "")
        WindowTransfer.XferToolTip = "Source and target Part are the same.  Use Move Kerbal instead.";
      return results;
    }

    private static bool IsClsInSameSpace()
    {
      var results = false;
      try
      {
        if (SMSettings.EnableCls && SMSettings.RealismMode)
        {
          if (ClsAddon.Vessel != null)
          {
            if (SmVessel.ClsSpaceSource == null || SmVessel.ClsSpaceTarget == null)
              UpdateClsSpaces();
            if (SmVessel.ClsSpaceSource != null && SmVessel.ClsSpaceTarget != null)
            {
              if (SmVessel.ClsSpaceSource == SmVessel.ClsSpaceTarget)
              {
                WindowTransfer.XferToolTip = "Source & Target Part are in the same space.\r\nInternal Xfers are allowed.";
                results = true;
              }
              else
                WindowTransfer.XferToolTip = "Source and Target parts are not in the same Living Space.\r\nKerbals will have to go EVA.";
            }
            else
              WindowTransfer.XferToolTip = "You should NOT be seeing this, as Source or Target Space is missing.\r\nPlease reselect source or target part.";
          }
          else
            WindowTransfer.XferToolTip = "You should NOT be seeing this, as CLS is not behaving correctly.\r\nPlease check your CLS installation.";
        }
        else
        {
          WindowTransfer.XferToolTip = "Realism and/or CLS disabled.\r\nXfers anywhere are allowed.";
          results = true;
        }
      }
      catch (Exception ex)
      {
        if (!FrameErrTripped)
        {
          Utilities.LogMessage(string.Format(" in IsInCLS (repeating error).  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
          FrameErrTripped = true;
        }
      }
      //Utilities.LogMessage("IsInCLS() - results = " + results.ToString() , "info", Settings.VerboseLogging);
      return results;
    }

    internal static bool CanShowShipManifest(bool ignoreShowSm = false)
    {
      try
      {
        var canShow = false;
        if (ShowUi
            && HighLogic.LoadedScene == GameScenes.FLIGHT
          //&& !MapView.MapIsEnabled
            && !IsPauseMenuOpen()
            && !IsFlightDialogDisplaying()
            && FlightGlobals.fetch != null
            && FlightGlobals.ActiveVessel != null
            && !FlightGlobals.ActiveVessel.isEVA
            && FlightGlobals.ActiveVessel.vesselType != VesselType.Flag
            && FlightGlobals.ActiveVessel.vesselType != VesselType.Debris
            && FlightGlobals.ActiveVessel.vesselType != VesselType.Unknown
            && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA
            )
          canShow = ignoreShowSm || WindowManifest.ShowWindow;
        return canShow;
      }
      catch (Exception ex)
      {
        if (!FrameErrTripped)
        {
          var values = "SmAddon.ShowUI = " + ShowUi + "\r\n";
          values += "HighLogic.LoadedScene = " + HighLogic.LoadedScene + "\r\n";
          //values += "!MapView.MapIsEnabled = " +MapView.MapIsEnabled.ToString() + "\r\n";
          values += "PauseMenu.isOpen = " + IsPauseMenuOpen() + "\r\n";
          values += "FlightResultsDialog.isDisplaying = " + IsFlightDialogDisplaying() + "\r\n";
          values += "FlightGlobals.fetch != null = " + (FlightGlobals.fetch != null) + "\r\n";
          values += "FlightGlobals.ActiveVessel != null = " + (FlightGlobals.ActiveVessel != null) + "\r\n";
          values += "!FlightGlobals.ActiveVessel.isEVA = " + (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA) + "\r\n";
          if (FlightGlobals.ActiveVessel != null)
            values += "FlightGlobals.ActiveVessel.vesselType = " + FlightGlobals.ActiveVessel.vesselType + "\r\n";
          values += "CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA = " + (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA);

          Utilities.LogMessage(string.Format(" in CanShowShipManifest (repeating error).  Error:  {0} \r\n\r\n{1}\r\n\r\nValues:  {2}", ex.Message, ex.StackTrace, values), "Error", true);
          FrameErrTripped = true;
        }
        return false;
      }
    }

    internal static bool IsFlightDialogDisplaying()
    {
      try
      {
        return FlightResultsDialog.isDisplaying;
      }
      catch
      {
        return false;
      }
    }
    internal static bool IsPauseMenuOpen()
    {
      try
      {
        return PauseMenu.isOpen;
      }
      catch
      {
        return false;
      }
    }
    #endregion

    #region Action Methods

    internal void UpdateSMcontroller(Vessel newVessel)
    {
      try
      {
        SMHighlighter.ClearResourceHighlighting(SmVessel.SelectedResourcesParts);
        if (SmVessel.Vessel != newVessel)
        {
          if (SmVessel.TransferCrewObj.CrewXferActive && !SmVessel.TransferCrewObj.IvaDelayActive)
            SmVessel.TransferCrewObj.CrewTransferAbort();
          if (TransferResource.ResourceXferActive && SMSettings.RealismMode)
            TransferResource.ResourceTransferAbort();
        }

        if (SmVessel.Vessel != null && CanShowShipManifest())
        {
          if (newVessel.isEVA && !SmVessel.Vessel.isEVA)
          {
            if (WindowManifest.ShowWindow)
              OnSmButtonToggle();

            // kill selected resource and its associated highlighting.
            SmVessel.SelectedResources.Clear();
            Utilities.LogMessage("New Vessel is a Kerbal on EVA.  ", "Info", SMSettings.VerboseLogging);
          }
        }

        // Now let's update the current vessel view...
        SmVessel = SMVessel.GetInstance(newVessel);
        SmVessel.RefreshLists();
      }
      catch (Exception ex)
      {
        Utilities.LogMessage("Error in:  ShipManifestAddon.UpdateSMcontroller.  " + ex, "Error", true);
      }
    }

    internal static void UpdateClsSpaces()
    {
      if (GetClsVessel())
      {
        try
        {
          SmVessel.ClsPartSource = null;
          SmVessel.ClsSpaceSource = null;
          SmVessel.ClsPartTarget = null;
          SmVessel.ClsSpaceTarget = null;
          foreach (var sSpace in ClsAddon.Vessel.Spaces)
          {
            foreach (var sPart in sSpace.Parts)
            {
              if (SmVessel.SelectedPartsSource.Contains(sPart.Part) && SmVessel.ClsPartSource == null)
              {
                SmVessel.ClsPartSource = sPart;
                SmVessel.ClsSpaceSource = sSpace;
                Utilities.LogMessage("UpdateCLSSpaces - clsPartSource found;", "info", SMSettings.VerboseLogging);
              }
              if (SmVessel.SelectedPartsTarget.Contains(sPart.Part) && SmVessel.ClsPartTarget == null)
              {
                SmVessel.ClsPartTarget = sPart;
                SmVessel.ClsSpaceTarget = sSpace;
                Utilities.LogMessage("UpdateCLSSpaces - clsPartTarget found;", "info", SMSettings.VerboseLogging);
              }
              if (SmVessel.ClsPartSource != null && SmVessel.ClsPartTarget != null)
                break;
            }
            if (SmVessel.ClsSpaceSource != null && SmVessel.ClsSpaceTarget != null)
              break;
          }
        }
        catch (Exception ex)
        {
          Utilities.LogMessage(string.Format(" in UpdateCLSSpaces.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
        }
      }
      else
        Utilities.LogMessage("UpdateCLSSpaces - clsVessel is null... done.", "info", SMSettings.VerboseLogging);
    }

    internal static bool GetClsAddon()
    {
      ClsAddon = ClsClient.GetCls();
      if (ClsAddon == null)
      {
        Utilities.LogMessage("GetCLSVessel - ClsAddon is null.", "Info", SMSettings.VerboseLogging);
        return false;
      }
      return true;
    }

    internal static bool GetClsVessel()
    {
      try
      {
        Utilities.LogMessage("GetCLSVessel - Active.", "Info", SMSettings.VerboseLogging);

        if (ClsAddon.Vessel != null)
        {
          return true;
        }
        else
        {
          Utilities.LogMessage("GetCLSVessel - clsVessel is null.", "Info", SMSettings.VerboseLogging);
          return false;
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage(string.Format(" in GetCLSVessel.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
        return false;
      }
    }

    internal static bool ActivateBlizzyToolBar()
    {
      if (SMSettings.EnableBlizzyToolbar)
      {
        try
        {
          if (ToolbarManager.ToolbarAvailable)
          {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
              _smButtonBlizzy = ToolbarManager.Instance.add("ShipManifest", "Manifest");
              _smButtonBlizzy.TexturePath = WindowManifest.ShowWindow ? TextureFolder + "IconOn_24" : TextureFolder + "IconOff_24";
              _smButtonBlizzy.ToolTip = "Ship Manifest";
              _smButtonBlizzy.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
              _smButtonBlizzy.Visible = true;
              _smButtonBlizzy.OnClick += e =>
              {
                OnSmButtonToggle();
              };
            }

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {

              _smSettingsBlizzy = ToolbarManager.Instance.add("ShipManifest", "Settings");
              _smSettingsBlizzy.TexturePath = WindowSettings.ShowWindow ? TextureFolder + "IconS_On_24" : TextureFolder + "IconS_Off_24";
              _smSettingsBlizzy.ToolTip = "Ship Manifest Settings Window";
              _smSettingsBlizzy.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER);
              _smSettingsBlizzy.Visible = true;
              _smSettingsBlizzy.OnClick += e =>
              {
                OnSmSettingsToggle();
              };

              _smRosterBlizzy = ToolbarManager.Instance.add("ShipManifest", "Roster");
              _smRosterBlizzy.TexturePath = WindowRoster.ShowWindow ? TextureFolder + "IconR_On_24" : TextureFolder + "IconR_Off_24";
              _smRosterBlizzy.ToolTip = "Ship Manifest Roster Window";
              _smRosterBlizzy.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER);
              _smRosterBlizzy.Visible = true;
              _smRosterBlizzy.OnClick += e =>
              {
                OnSmRosterToggle();
              };
            }
            Utilities.LogMessage("Blizzy Toolbar available!", "Info", SMSettings.VerboseLogging);
            return true;
          }
          else
          {
            Utilities.LogMessage("Blizzy Toolbar not available!", "Info", SMSettings.VerboseLogging);
            return false;
          }
        }
        catch (Exception ex)
        {
          // Blizzy Toolbar instantiation error.
          Utilities.LogMessage("Error in EnableBlizzyToolbar... Error:  " + ex, "Error", true);
          return false;
        }
      }
      else
      {
        // No Blizzy Toolbar
        Utilities.LogMessage("Blizzy Toolbar not Enabled...", "Info", SMSettings.VerboseLogging);
        return false;
      }
    }

    internal void Display()
    {
      var step = "";
      try
      {
        step = "0 - Start";
        if (WindowDebugger.ShowWindow)
          WindowDebugger.Position = GUILayout.Window(398643, WindowDebugger.Position, WindowDebugger.Display, WindowDebugger.Title, GUILayout.MinHeight(20));

        if ((HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER) && ShowUi)
        {
          if (WindowSettings.ShowWindow)
          {
            step = "4 - Show Settings";
            WindowSettings.Position = GUILayout.Window(398546, WindowSettings.Position, WindowSettings.Display, WindowSettings.Title, GUILayout.MinHeight(20));
          }

          if (WindowRoster.ShowWindow)
          {
            step = "6 - Show Roster";
            if (WindowRoster.ResetRosterSize)
              WindowRoster.Position.height = SMSettings.UseUnityStyle ? 330 : 350;
            WindowRoster.Position = GUILayout.Window(398547, WindowRoster.Position, WindowRoster.Display, WindowRoster.Title, GUILayout.MinHeight(20));
          }
        }
        if (HighLogic.LoadedScene == GameScenes.FLIGHT && (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel != SmVessel.Vessel))
        {
          step = "0a - Vessel Change";
          SmVessel.SelectedPartsSource.Clear();
          SmVessel.SelectedPartsTarget.Clear();
          SmVessel.SelectedResources.Clear();
          return;
        }

        step = "1 - Show Interface(s)";
        // Is the scene one we want to be visible in?
        if (CanShowShipManifest())
        {
          // What windows do we want to show?
          step = "2 - Can Show Manifest - true";
          WindowManifest.Position = GUILayout.Window(398544, WindowManifest.Position, WindowManifest.Display, WindowManifest.Title, GUILayout.MinHeight(20));

          if (WindowTransfer.ShowWindow && SmVessel.SelectedResources.Count > 0)
          {
            step = "3 - Show Transfer";
            // Lets build the running totals for each resource for display in title...
            WindowTransfer.Position = GUILayout.Window(398545, WindowTransfer.Position, WindowTransfer.Display, WindowTransfer.Title, GUILayout.MinHeight(20));
          }

          if (WindowManifest.ShowWindow && WindowControl.ShowWindow)
          {
            step = "7 - Show Control";
            WindowControl.Position = GUILayout.Window(398548, WindowControl.Position, WindowControl.Display, WindowControl.Title, GUILayout.MinWidth(350), GUILayout.MinHeight(20));
          }
        }
        else
        {
          step = "2 - Can Show Manifest = false";
          if (SMSettings.EnableCls && SmVessel != null)
            if (SmVessel.SelectedResources.Contains("Crew"))
              SMHighlighter.HighlightClsVessel(false, true);
        }
      }
      catch (Exception ex)
      {
        if (!FrameErrTripped)
        {
          Utilities.LogMessage(string.Format(" in Display at or near step:  " + step + ".  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
          FrameErrTripped = true;
        }
      }
    }

    internal static void RepositionWindows()
    {
      RepositionWindow(ref WindowManifest.Position);
      RepositionWindow(ref WindowTransfer.Position);
      RepositionWindow(ref WindowDebugger.Position);
      RepositionWindow(ref WindowSettings.Position);
      RepositionWindow(ref WindowControl.Position);
      RepositionWindow(ref WindowRoster.Position);
    }

    internal static void RepositionWindow(ref Rect windowPosition)
    {
      if (windowPosition.x < 0)
        windowPosition.x = 0;
      if (windowPosition.y < 0)
        windowPosition.y = 0;
      if (windowPosition.xMax > Screen.currentResolution.width)
        windowPosition.x = Screen.currentResolution.width - windowPosition.width;
      if (windowPosition.yMax > Screen.currentResolution.height)
        windowPosition.y = Screen.currentResolution.height - windowPosition.height;
    }

    internal static void LoadSounds(string soundType, string path1, string path2, string path3, double dblVol)
    {
      try
      {
        Elapsed = 0;
        Utilities.LogMessage("Loading " + soundType + " sounds...", "Info", SMSettings.VerboseLogging);

        var go = new GameObject("Audio");

        Source1 = go.AddComponent<AudioSource>();
        Source2 = go.AddComponent<AudioSource>();
        Source3 = go.AddComponent<AudioSource>();

        if (GameDatabase.Instance.ExistsAudioClip(path1) && GameDatabase.Instance.ExistsAudioClip(path2) && GameDatabase.Instance.ExistsAudioClip(path3))
        {
          Sound1 = GameDatabase.Instance.GetAudioClip(path1);
          Sound2 = GameDatabase.Instance.GetAudioClip(path2);
          Sound3 = GameDatabase.Instance.GetAudioClip(path3);
          Utilities.LogMessage(soundType + " sounds loaded...", "Info", SMSettings.VerboseLogging);

          // configure sources
          Source1.clip = Sound1; // Start sound
          Source1.volume = (float)dblVol;
          Source1.pitch = 1f;

          Source2.clip = Sound2; // Run sound
          Source2.loop = true;
          Source2.volume = (float)dblVol;
          Source2.pitch = 1f;

          Source3.clip = Sound3; // Stop Sound
          Source3.volume = (float)dblVol;
          Source3.pitch = 1f;

          // now let's play the Pump start sound.
          Source1.Play();
          Utilities.LogMessage("Play " + soundType + " sound (start)...", "Info", SMSettings.VerboseLogging);
        }
        else
        {
          Utilities.LogMessage(soundType + " sound failed to load...", "Info", SMSettings.VerboseLogging);
        }
      }
      catch (Exception ex)
      {
        Utilities.LogMessage(string.Format(" in LoadSounds.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
        // ReSharper disable once PossibleIntendedRethrow
        throw ex;
      }
    }

    // This method is used for autosave...
    internal void RunSave()
    {
      try
      {
        Utilities.LogMessage("RunSave in progress...", "info", SMSettings.VerboseLogging);
        SMSettings.SaveSettings();
        Utilities.LogMessage("RunSave complete.", "info", SMSettings.VerboseLogging);
      }
      catch (Exception ex)
      {
        Utilities.LogMessage(string.Format(" in RunSave.  Error:  {0} \r\n\r\n{1}", ex.Message, ex.StackTrace), "Error", true);
      }
    }

    internal static void FireEventTriggers()
    {
      // Per suggestion by shaw (http://forum.kerbalspaceprogram.com/threads/62270?p=1033866&viewfull=1#post1033866)
      // and instructions for using CLS API by codepoet.
      Utilities.LogMessage("FireEventTriggers:  Active.", "info", SMSettings.VerboseLogging);
      GameEvents.onVesselChange.Fire(SmVessel.Vessel);
    }

    #endregion

    internal enum XferDirection
    {
      SourceToTarget,
      TargetToSource
    }

  }

  internal class ShipManifestModule : PartModule
  {
    [KSPEvent(guiActive = true, guiName = "Destroy Part", active = true)]
    internal void DestoryPart()
    {
      if (part != null)
        part.temperature = 5000;
    }

    public override void OnUpdate()
    {
      base.OnUpdate();

      if (part != null && part.name == "ShipManifest")
        Events["DestoryPart"].active = true;
      else
        Events["DestoryPart"].active = false;
    }
  }

}