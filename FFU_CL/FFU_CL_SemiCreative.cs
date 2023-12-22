#pragma warning disable CS0108
#pragma warning disable CS0162
#pragma warning disable CS0414
#pragma warning disable CS0618
#pragma warning disable CS0626
#pragma warning disable CS0649
#pragma warning disable IDE1006
#pragma warning disable IDE0019
#pragma warning disable IDE0002

using MonoMod;
using UnityEngine;
using System.Text;
using System;

namespace FFU_Connection_Lost {
	public class Data {
		public const bool isSemiCreative = true;
	}
}

public class patch_PlayerAction_Plant : PlayerAction_Plant {
    public bool DetermineActive() {
        if (player.inhandItemId > 0) return false;
        if (!(FFU_Connection_Lost.Data.isSemiCreative || GameMain.sandboxToolsEnabled) || !controller.cmd.onPlanet || 
			controller.cmd.raycast == null || controller.gameData.guideRunning) return false;
        if (controller.cmd.type == ECommand.Plant && VFInput.readyToPlant) {
            if (!plantMode) return removalMode;
            return true;
        }
        return false;
    }
}

public class patch_CargoTraffic : CargoTraffic {
    public void MonitorGameTick() {
        AnimData[] entityAnimPool = factory.entityAnimPool;
        SpeakerComponent[] speakerPool = factory.digitalSystem.speakerPool;
        EntityData[] entityPool = factory.entityPool;
        bool sandboxToolsEnabled = GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative;
        PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
        for (int i = 1; i < monitorCursor; i++) {
            if (monitorPool[i].id == i) 
                monitorPool[i].InternalUpdate(this, sandboxToolsEnabled, 
					entityPool, speakerPool, entityAnimPool);
        }
        PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
    }
}

public class patch_UIFunctionPanel : UIFunctionPanel {
	[MonoModIgnore] private float pos;
	[MonoModIgnore] private float gpos;
	[MonoModIgnore] private float width;
	[MonoModIgnore] private float posWanted;
	[MonoModIgnore] private float gposWanted;
	[MonoModIgnore] private float widthWanted;
	[MonoModIgnore] private Player mainPlayer;
	[MonoModIgnore] private Color bgNormalColor;
	[MonoModIgnore] private PlayerController playerController;
    protected override void _OnUpdate() {
        if (openedTime == 0f) {
            menuTweener.normalizedTime = 0f;
        }
        if (!(GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative)) {
            showSandBoxMenu = false;
        }
        bool guideComplete = (base.data as GameData).guideComplete;
        openedTime += Time.deltaTime;
        Color color = bgNormalColor;
        if (guideComplete) {
            if (VFInput._upgradeModeKey && !VFInput.inputing && !buildMenu.active && VFInput.inScreen && VFInput.readyToBuild) {
                showSandBoxMenu = false;
                sandboxMenu._Close();
                buildMenu._Open();
            }
            if (buildMenu.active) {
                if (buildMenu.currentCategory >= 1 && buildMenu.currentCategory <= 9) {
                    posWanted = 0f;
                    widthWanted = 780f;
                } else {
                    posWanted = -75f;
                    widthWanted = 810f;
                }
                if (buildMenu.isDismantleMode) {
                    color = bgDismantleColor;
                    posWanted = 0f;
                    widthWanted = 780f;
                }
                if (buildMenu.isUpgradeMode) {
                    color = ((GameMain.mainPlayer.controller.actionBuild.upgradeTool.upgradeLevel >= 0) ? bgUpgradeColor : bgDngradeColor);
                    posWanted = 0f;
                    widthWanted = 780f;
                }
            } else if (sandboxMenu.active) {
                if (sandboxMenu.childGroup.activeSelf) {
                    posWanted = 0f;
                    widthWanted = 780f;
                } else {
                    posWanted = -75f;
                    widthWanted = 810f;
                }
                if (sandboxMenu.isRemovalMode) {
                    color = bgDismantleColor;
                    posWanted = 0f;
                    widthWanted = 780f;
                }
            } else if (mainPlayer.controller.actionBuild.blueprintMode > EBlueprintMode.None) {
                posWanted = -133f;
                widthWanted = 820f;
            } else {
                posWanted = -135f;
                widthWanted = 820f;
            }
        } else {
            posWanted = -135f;
            widthWanted = 820f;
        }
        gposWanted = ((GameMain.data.guideRunning || !GameMain.mainPlayer.isAlive) ? (-200f) : 0f);
        bgImage.color = color;
        pos = Lerp.Tween(pos, posWanted, 18f);
        width = Lerp.Tween(width, widthWanted, 18f);
        gpos = Lerp.Tween(gpos, gposWanted, 18f);
        bool flag = GameMain.history.TechUnlocked(1001);
        bool flag2 = !(GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative) 
			&& !flag && !GameMain.mainPlayer.sailing && !showSandBoxMenu;
        sandboxTitleObject.SetActive(pos > -90f);
        sandboxIconObject.SetActive(pos <= -90f);
        rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x, gpos);
        bgTrans.anchoredPosition = new Vector2(bgTrans.anchoredPosition.x, pos);
        bgTrans.sizeDelta = new Vector2(width, bgTrans.sizeDelta.y);
        if (needResearchTipGroup.activeSelf != flag2) {
            needResearchTipGroup.SetActive(flag2);
        }
        bool flag3 = openedTime > 1f && guideComplete;
        UIGame uiGame = UIRoot.instance.uiGame;
        if (UIGame.viewMode == EViewMode.Starmap || !GameMain.mainPlayer.isAlive) {
            flag3 = false;
        }
        if (flag3 && menuTweener.normalizedTime < 1f) {
            menuTweener.Play0To1Continuing();
        } else if (!flag3 && menuTweener.normalizedTime > 0f) {
            menuTweener.Play1To0Continuing();
        }
        if (menuTweener.normalizedTime == 0f) {
            uiGame.planetGlobe._Close();
            uiGame.gameMenu._Close();
        } else {
            uiGame.planetGlobe._Open();
            uiGame.gameMenu._Open();
        }
        sandboxRect.anchoredPosition = new Vector2(Mathf.Clamp((0f - pos) * 2f - 200f, -50f, 10f), 0f);
        sandboxButton.gameObject.SetActive(GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative);
        sandboxButton.highlighted = showSandBoxMenu;
        PlanetData localPlanet = GameMain.localPlanet;
        bool fastTravelling = GameMain.mainPlayer.fastTravelling;
        bool flag4 = localPlanet != null && localPlanet.type == EPlanetType.Gas && mainPlayer.movementState <= EMovementState.Fly && mainPlayer.isAlive;
        extractButton.gameObject.SetActive(flag4);
        extractButton.highlighted = playerController.actionMine.autoExtractGas && !fastTravelling;
        extractProgressImage.fillAmount = ((fastTravelling || !mainPlayer.isAlive) ? 0f : playerController.actionMine.extractGasProgress[0]);
        if (VFInput._openBlueprintLibrary && VFInput.inScreen && !VFInput.inputing && !UIGridSplit.opened && !VFInput.inCombatScreenGUI && buildMenu.blueprintButton.button.interactable) {
            VFAudio.Create("ui-click-0", null, Vector3.zero, play: true, 0, -1, -1L);
            buildMenu.OnBlueprintButtonClick();
        }
    }
}

public class patch_UIMonitorWindow : UIMonitorWindow {
	[MonoModIgnore] private bool event_lock;
	[MonoModIgnore] private long updateTimestamp;
	[MonoModIgnore] private int[] cargoBytesArray;
	[MonoModIgnore] private Sprite tagNotSelectedSprite;
	[MonoModIgnore] private RectTransform passColorRect;
	[MonoModIgnore] private RectTransform failColorRect;
	[MonoModIgnore] private StringBuilder powerServedSB;
	[MonoModIgnore] private RectTransform periodSliderRect;
	[MonoModIgnore] private ComputeBuffer cargoBytesBuffer;
	[MonoModIgnore] private ComputeBuffer periodCargoBytesBuffer;
	[MonoModIgnore] private void LanguageAdapt() { }
	[MonoModIgnore] private void OnOperatorChange() { }
	[MonoModIgnore] private void TryOpenSpeakerPanel() { }
	[MonoModIgnore] private void OnSystemWarningChange() { }
	[MonoModIgnore] private void OnSpeakerWarningChange() { }
	[MonoModIgnore] private void OnPeriodValueChange(float _value) { }
	[MonoModIgnore] private void OnPassColorButtonClick(int _value) { }
	[MonoModIgnore] private void OnFailColorButtonClick(int _value) { }
	[MonoModIgnore] private void OnCargoFlowValueChange(float _value) { }
	[MonoModIgnore] private void OnCargoFlowInputFieldChange(string _value) { }
	[MonoModIgnore] private void OnCargoFlowInputFieldEndEdit(string _value) { }
    private void RefreshMonitorWindow() {
        event_lock = true;
        cargoFlowInputFieldErrorTips.SetActive(value: false);
        float num = 60f / periodSlider.maxValue;
        float num2 = (float)cargoTraffic.monitorPool[monitorId].curPeriodTickCount / 60f;
        float num3 = num2 * 120f / (cargoFlowSlider.maxValue - cargoFlowSlider.minValue);
        float num4 = (float)cargoTraffic.monitorPool[monitorId].targetCargoBytes / 10f;
        cargoFlowInputField.text = num4.ToString("0.0");
        cargoFlowSlider.value = Mathf.RoundToInt(num4 / num3);
        periodSlider.value = num2 / num;
        periodText.text = num2.ToString("0.00");
        operatorCombo.itemIndex = cargoTraffic.monitorPool[monitorId].passOperator;
        speakerWarningCombo.itemIndex = speakerWarningCombo.ItemsData.IndexOf(cargoTraffic.monitorPool[monitorId].alarmMode);
        systemWarningCombo.itemIndex = systemWarningCombo.ItemsData.IndexOf(cargoTraffic.monitorPool[monitorId].systemWarningMode);
        OnCargoFilterPickerReturn(LDB.items.Select(cargoTraffic.monitorPool[monitorId].cargoFilter));
        int systemWarningSignalId = cargoTraffic.monitorPool[monitorId].systemWarningSignalId;
        Sprite sprite = LDB.signals.IconSprite(systemWarningSignalId);
        if (sprite != null) {
            iconTagImage.sprite = sprite;
        } else {
            iconTagImage.sprite = tagNotSelectedSprite;
        }
        iconTagGo.SetActive(cargoTraffic.monitorPool[monitorId].systemWarningMode != 0);
        if (GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative) {
            spawnGroup.SetActive(value: true);
            if (cargoTraffic.monitorPool[monitorId].spawnItemOperator > 0) {
                spawnToggle.isOn = true;
                spawnSwitch.gameObject.SetActive(value: true);
                spawnSwitch.SetToggleNoEvent(cargoTraffic.monitorPool[monitorId].spawnItemOperator == 1);
                spawnText.text = ((cargoTraffic.monitorPool[monitorId].spawnItemOperator == 1) ? "流速器生成物品".Translate() : "流速器移除物品".Translate());
            } else {
                spawnToggle.isOn = false;
                spawnSwitch.gameObject.SetActive(value: false);
                spawnText.text = "流速器生成物品标题".Translate();
            }
        } else {
            spawnGroup.SetActive(value: false);
        }
        event_lock = false;
    }
    protected override void _OnOpen() {
        if (GameMain.localPlanet != null && GameMain.localPlanet.factory != null) {
            factory = GameMain.localPlanet.factory;
            cargoTraffic = factory.cargoTraffic;
            player = GameMain.mainPlayer;
            updateTimestamp = 0L;
            periodSliderRect = periodSlider.GetComponent<RectTransform>();
            passColorRect = passColorImage.GetComponent<RectTransform>();
            failColorRect = failColorImage.GetComponent<RectTransform>();
            periodSlider.onValueChanged.AddListener(OnPeriodValueChange);
            cargoFlowSlider.onValueChanged.AddListener(OnCargoFlowValueChange);
            cargoFlowInputField.onValueChanged.AddListener(OnCargoFlowInputFieldChange);
            cargoFlowInputField.onEndEdit.AddListener(OnCargoFlowInputFieldEndEdit);
            operatorCombo.onItemIndexChange.AddListener(OnOperatorChange);
            passColorBtn.onClick += OnPassColorButtonClick;
            failColorBtn.onClick += OnFailColorButtonClick;
            speakerWarningCombo.onItemIndexChange.AddListener(OnSpeakerWarningChange);
            systemWarningCombo.onItemIndexChange.AddListener(OnSystemWarningChange);
            cargoFilterButton.onClick += OnCargoFilterSelectButtonClick;
            cargoFilterButton.onRightClick += OnCargoFilterSelectButtonRightClick;
            iconTagButton.onClick += OnTagSelectButtonClick;
            iconTagButton.onRightClick += OnTagSelectButtonRightClick;
            RefreshMonitorWindow();
            TryOpenSpeakerPanel();
            UIItemPicker.showAll = GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative;
        } else {
            _Close();
        }
        base.transform.SetAsLastSibling();
    }
    protected override void _OnUpdate() {
        LanguageAdapt();
        if (!monitorAvailable || monitorId == 0 || factory == null) {
            return;
        }
        int curPeriodTickCount = cargoTraffic.monitorPool[monitorId].curPeriodTickCount;
        float num = (float)cargoTraffic.monitorPool[monitorId].cargoFlow * 0.1f / ((float)curPeriodTickCount / 3600f);
        int num2 = (int)(num * 10f);
        if (curPeriodTickCount <= 0) {
            cargoFlowImageNumText.text = "0.0";
        } else {
            cargoFlowImageNumText.text = num.ToString("0.0");
        }
        if (GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative) {
            byte b = (byte)(spawnToggle.isOn ? (spawnSwitch.isOn ? 1 : 2) : 0);
            cargoTraffic.monitorPool[monitorId].SetSpawnOperator(b);
            spawnText.text = b switch {
                1 => "流速器生成物品".Translate(),
                0 => "流速器生成物品标题".Translate(),
                _ => "流速器移除物品".Translate(),
            };
            Color target = b switch {
                1 => GenTextColor,
                0 => new Color(0.588f, 0.588f, 0.588f),
                _ => RemoveTextColor,
            };
            spawnText.color = Lerp.Tween(spawnText.color, target, 20f);
        }
        if (updateTimestamp == 0L) {
            updateTimestamp = GameMain.gameTick;
            for (int i = 0; i < curPeriodTickCount; i++) {
                cargoBytesArray[i] = cargoTraffic.monitorPool[monitorId].cargoBytesArray[i];
            }
            cargoBytesBuffer.SetData(cargoBytesArray);
        } else {
            long num3 = GameMain.gameTick - updateTimestamp;
            num3 = ((num3 > curPeriodTickCount) ? curPeriodTickCount : num3);
            if (num3 > 0) {
                Array.Copy(cargoBytesArray, num3, cargoBytesArray, 0L, cargoBytesArray.Length - num3);
                for (int j = 1; j <= num3; j++) {
                    int num4 = curPeriodTickCount - j;
                    if (num4 >= 0 && num4 < curPeriodTickCount) {
                        cargoBytesArray[curPeriodTickCount - j] = cargoTraffic.monitorPool[monitorId].cargoBytesArray[curPeriodTickCount - j];
                    }
                }
                updateTimestamp = GameMain.gameTick;
                cargoBytesBuffer.SetData(cargoBytesArray);
            }
        }
        int num5 = 0;
        int num6 = int.MaxValue;
        int[] periodCargoBytesArray = cargoTraffic.monitorPool[monitorId].periodCargoBytesArray;
        for (int k = 0; k < curPeriodTickCount; k++) {
            num5 = ((periodCargoBytesArray[k] > num5) ? periodCargoBytesArray[k] : num5);
            num6 = ((periodCargoBytesArray[k] < num6) ? periodCargoBytesArray[k] : num6);
        }
        float num7 = 300f;
        if (cargoTraffic.monitorPool[monitorId].targetBeltId > 0) {
            int speed = cargoTraffic.beltPool[cargoTraffic.monitorPool[monitorId].targetBeltId].speed;
            int num8 = Mathf.Max(Mathf.Clamp(num2 / curPeriodTickCount / speed + 1, 1, 4), Mathf.Clamp(cargoTraffic.monitorPool[monitorId].targetCargoBytes / curPeriodTickCount / speed + 1, 1, 4));
            num7 = (float)speed * 60f * (float)num8;
        }
        cargoFlowImageMaxText.text = (num7 * 0.1f * 60f).ToString("0.0");
        periodCargoBytesBuffer.SetData(cargoTraffic.monitorPool[monitorId].periodCargoBytesArray);
        cargoFlowImage.material.SetBuffer("_PeriodCargoBytesBuffer", periodCargoBytesBuffer);
        cargoFlowImage.material.SetBuffer("_CargoBytesBuffer", cargoBytesBuffer);
        cargoFlowImage.material.SetFloat("_MaxBufferCount", curPeriodTickCount);
        cargoFlowImage.material.SetFloat("_BeltMaxSpeed", num7);
        cargoFlowImage.material.SetFloat("_PassOperator", (int)cargoTraffic.monitorPool[monitorId].passOperator);
        cargoFlowImage.material.SetFloat("_TargetCargoBytesFlow", cargoTraffic.monitorPool[monitorId].targetCargoBytes);
        cargoFlowImage.material.SetFloat("_PrewarmSampleTick", (int)cargoTraffic.monitorPool[monitorId].prewarmSampleTick);
        int passColorId = cargoTraffic.monitorPool[monitorId].passColorId;
        int failColorId = cargoTraffic.monitorPool[monitorId].failColorId;
        Color color = Configs.builtin.colorPalette256[passColorId];
        Color color2 = Configs.builtin.colorPalette256[failColorId];
        cargoFlowImage.material.SetColor("_PassColor", color);
        cargoFlowImage.material.SetColor("_FailColor", color2);
        cargoFlowImage.material.SetColor("_PrewarmColor", idleColor);
        Color color3 = color;
        Color color4 = color2;
        if (RectTransformUtility.RectangleContainsScreenPoint(passColorRect, Input.mousePosition, UIRoot.instance.overlayCanvas.worldCamera)) {
            color3 *= 1.2f;
        }
        if (RectTransformUtility.RectangleContainsScreenPoint(failColorRect, Input.mousePosition, UIRoot.instance.overlayCanvas.worldCamera)) {
            color4 *= 1.2f;
        }
        color3.a = 0.4f;
        color4.a = 0.4f;
        passColorImage.color = color3;
        failColorImage.color = color4;
        MonitorComponent.ELogicState logicState = cargoTraffic.monitorPool[monitorId].GetLogicState();
        if (logicState == MonitorComponent.ELogicState.None) {
            cargoFlowImageNumText.color = idleColor;
            cargoFlowImageText.color = idleColor;
        } else {
            Color color5 = ((logicState == MonitorComponent.ELogicState.Pass) ? color : color2);
            float num9 = color5.r * 0.299f + color5.g * 0.587f + color5.b * 0.114f;
            color5 = Color.Lerp(new Color(num9, num9, num9, num9), color5, 0.6f);
            color5.a = 0.8f;
            cargoFlowImageNumText.color = color5;
            cargoFlowImageText.color = color5;
        }
        PowerConsumerComponent powerConsumerComponent = factory.powerSystem.consumerPool[cargoTraffic.monitorPool[monitorId].pcId];
        int networkId = powerConsumerComponent.networkId;
        PowerNetwork powerNetwork = factory.powerSystem.netPool[networkId];
        float num10 = ((powerNetwork != null && networkId > 0) ? ((float)powerNetwork.consumerRatio) : 0f);
        long valuel = (long)((double)(powerConsumerComponent.requiredEnergy * 60) * (double)num10);
        StringBuilderUtility.WriteKMG(powerServedSB, 8, valuel);
        if (num10 == 1f) {
            powerText.text = powerServedSB.ToString();
            powerIconImage.color = powerNormalIconColor;
            powerText.color = powerNormalColor;
        } else if (num10 > 0.1f) {
            powerText.text = powerServedSB.ToString();
            powerIconImage.color = powerLowIconColor;
            powerText.color = powerLowColor;
        } else {
            powerText.text = "未供电".Translate();
            powerIconImage.color = Color.clear;
            powerText.color = powerOffColor;
        }
    }
}

public class patch_UIGame : UIGame {
	[MonoModIgnore] private bool willClose;
	[MonoModIgnore] private bool gameRunningVisual;
	[MonoModIgnore] private void DetermineViewMode(Player mainPlayer) { }
    protected override void _OnUpdate() {
        if (willClose) {
            willClose = false;
            _Close();
            return;
        }
        bool flag = canvasGroup.alpha > 0.5f;
        if (GameMain.isPaused) {
            if (!escMenu.active) {
                escMenu._Open();
            }
        } else if (escMenu.active) {
            escMenu._Close();
        }
        escMenu._Update();
        advisorTip.ForceUpdate();
        if (GameMain.isPaused || !GameMain.isRunning) {
            return;
        }
        if (VFInput.escape && UIMessageBox.CloseTopMessage()) {
            VFInput.UseEscape();
        }
        if (UIStarmap.isChangingToMilkyWay) {
            ShutStarmapChangingConflictWindows();
            SetZScreenDetail(null);
            SetPlanetDetail(null);
            SetStarDetail(null);
            starmap._Update();
            return;
        }
        Player mainPlayer = GameMain.mainPlayer;
        bool flag2 = mainPlayer.controller.actionBuild.blueprintMode != EBlueprintMode.None;
        DetermineViewMode(mainPlayer);
        if (!inZScreen) {
            if (functionPanel.showSandBoxMenu && (GameMain.sandboxToolsEnabled || FFU_Connection_Lost.Data.isSemiCreative)) {
                buildMenu._Close();
                if (!gameRunningVisual || !VFInput.readyToPlant) {
                    sandboxMenu._Close();
                } else {
                    sandboxMenu._Open();
                }
            } else {
                sandboxMenu._Close();
                if (!gameRunningVisual || !VFInput.readyToBuild || flag2) {
                    buildMenu._Close();
                } else {
                    buildMenu._Open();
                }
            }
            if (functionPanel.active) {
                energyBar._Open();
            }
            if (lootFilter.active) {
                lootFilter._Close();
            }
        } else {
            buildMenu._Close();
            energyBar._Close();
            sandboxMenu._Close();
        }
        if (viewMode == EViewMode.Sail && mainPlayer.isAlive && !mainPlayer.combatState) {
            sailPanel._Open();
        } else {
            sailPanel._Close();
        }
        mechaLab.DetermineVisible();
        DetermineViewMode(mainPlayer);
        researchQueue.DetermineVisible();
        sailIndicator.DetermineVisible();
        spaceCommandGizmo.DetermineVisible();
        trashPanel.DetermineVisible();
        shieldDetail.DetermineVisible();
        slotPicker.Determine();
        advisorTip.Determine();
        tutorialWindow.Determine();
        tutorialTip.Determine();
        variousPopupGroup.Determine();
        warningWindow.Determine(!dysonEditor.active && !techTree.active && !starmap.active);
        abnormalityTip.Determine();
        deathPanel.Determine();
        communicatorWindow.Determine();
        dfMonitor.Determine();
        if (gameData.gameDesc.isCombatMode && GameMain.gameTick > 0) {
            dfMonitor.OrganizeTargetList();
        }
        dfAssaultTip.Determine();
        gameMenu._Update();
        sailPanel._Update();
        functionPanel._Update();
        planetGlobe._Update();
        buildMenu._Update();
        sandboxMenu._Update();
        trashPanel._Update();
        inventoryWindow._Update();
        mechaWindow._Update();
        replicator._Update();
        statWindow._Update();
        handTip._Update();
        gridSplit._Update();
        inserterBuildTip._Update();
        beltBuildTip._Update();
        slotPicker._Update();
        sandUpTip._Update();
        abnormalityTip._Update();
        itemupTips._Update();
        energyBar._Update();
        deathPanel._Update();
        storageWindow._Update();
        tankWindow._Update();
        minerWindow._Update();
        assemblerWindow._Update();
        fractionatorWindow._Update();
        ejectorWindow._Update();
        siloWindow._Update();
        labWindow._Update();
        inserterWindow._Update();
        splitterWindow._Update();
        beltWindow._Update();
        accumulatorWindow._Update();
        nodeWindow._Update();
        generatorWindow._Update();
        exchangerWindow._Update();
        stationWindow._Update();
        dispenserWindow._Update();
        tutorialWindow._Update();
        blueprintBrowser._Update();
        blueprintCopyInspector._Update();
        blueprintPasteInspector._Update();
        monitorWindow._Update();
        spraycoaterWindow._Update();
        turretWindow._Update();
        fieldGeneratorWindow._Update();
        battleBaseWindow._Update();
        communicatorWindow._Update();
        globemap._Update();
        starmap._Update();
        dysonEditor._Update();
        zScreen._Update();
        techTree._Update();
        mechaLab._Update();
        veinDetail._Update();
        shieldDetail._Update();
        planetDetail._Update();
        starDetail._Update();
        recipePicker._Update();
        itemPicker._Update();
        signalPicker._Update();
        color256Picker._Update();
        lootFilter._Update();
        foreach (UIStorageGrid openedStorage in UIStorageGrid.openedStorages) {
            openedStorage._Update();
        }
        generalTips._Update();
        researchResultTip._Update();
        keyTips._Update();
        if (resourceTip.willShow) {
            resourceTip._Open();
        } else {
            resourceTip._Close();
        }
        resourceTip._Update();
        removeBasePitBtn._Update();
        pointerMask._Update();
        zoomWatcher._Update();
        advisorTip._Update();
        tutorialTip._Update();
        variousPopupGroup._Update();
        warningWindow._Update();
        dfMonitor._Update();
        dfAssaultTip._Update();
        powerGizmo._Update();
        defenseGizmo._Update();
        if (VFInput.escape && colorPalettePanel.gameObject.activeInHierarchy) {
            colorPalettePanel.Close();
            VFInput.UseEscape();
        }
        if (researchResultTip.ready && VFInput.escape) {
            VFInput.UseEscape();
            researchResultTip.FadeOut();
        }
        if ((recipePicker.active || itemPicker.active || signalPicker.active || color256Picker.active || lootFilter.active) && VFInput.escape) {
            VFInput.UseEscape();
            if (recipePicker.active) {
                recipePicker._Close();
            }
            if (itemPicker.active) {
                itemPicker._Close();
            }
            if (signalPicker.active) {
                signalPicker._Close();
            }
            if (color256Picker.active) {
                color256Picker._Close();
            }
            if (lootFilter.active) {
                lootFilter._Close();
            }
        }
        if (inventoryWindow.active && VFInput.escape) {
            VFInput.UseEscape();
            ShutPlayerInventory();
        }
        if (isAnyFunctionWindowActive && VFInput.escape) {
            VFInput.UseEscape();
            ShutAllFunctionWindow();
        }
        if (dysonEditor.active && VFInput.escape) {
            VFInput.UseEscape();
            TryShutDysonEditor();
        }
        if (zScreen.active && VFInput.escape) {
            VFInput.UseEscape();
            mainPlayer.combatState = false;
        }
        if (VFInput.inFullscreenGUI && VFInput.escape) {
            VFInput.UseEscape();
            ShutAllFullScreens();
        }
        if (sandboxMenu.showGeneralGroup && VFInput.escape) {
            VFInput.UseEscape();
            sandboxMenu.OnCategoryButtonClick(0);
        }
        if (guideComplete) {
            if (Input.GetKeyDown(KeyCode.E) && !VFInput.inputing && flag) {
                if (mechaWindow.active && colorPalettePanel.gameObject.activeInHierarchy) {
                    colorPalettePanel.Close();
                } else if (researchResultTip.ready) {
                    researchResultTip.FadeOut();
                } else if (sandboxMenu.showGeneralGroup) {
                    sandboxMenu.OnCategoryButtonClick(0);
                } else if (recipePicker.active || itemPicker.active || signalPicker.active || color256Picker.active || lootFilter.active) {
                    if (recipePicker.active) {
                        recipePicker._Close();
                    }
                    if (itemPicker.active) {
                        itemPicker._Close();
                    }
                    if (signalPicker.active) {
                        signalPicker._Close();
                    }
                    if (color256Picker.active) {
                        color256Picker._Close();
                    }
                    if (lootFilter.active) {
                        lootFilter._Close();
                    }
                } else if (tutorialWindow.active) {
                    CloseTutorialWindow();
                } else if (mechaWindow.active) {
                    if (inventoryWindow.active) {
                        ShutMechaWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (replicator.active) {
                    if (inventoryWindow.active) {
                        ShutReplicatorWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (statWindow.active) {
                    ShutProductionWindow();
                } else if (storageWindow.active) {
                    if (inventoryWindow.active) {
                        ShutStorageWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (tankWindow.active) {
                    if (inventoryWindow.active) {
                        ShutTankWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (minerWindow.active) {
                    ShutMinerWindow();
                } else if (assemblerWindow.active) {
                    ShutAssemblerWindow();
                } else if (fractionatorWindow.active) {
                    ShutFractionatorWindow();
                } else if (ejectorWindow.active) {
                    if (inventoryWindow.active) {
                        ShutEjectorWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (siloWindow.active) {
                    if (inventoryWindow.active) {
                        ShutSiloWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (labWindow.active) {
                    if (inventoryWindow.active) {
                        ShutLabWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (inserterWindow.active) {
                    ShutInserterWindow();
                } else if (splitterWindow.active) {
                    ShutSplitterWindow();
                } else if (beltWindow.active) {
                    if (inventoryWindow.active) {
                        ShutBeltWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (accumulatorWindow.active) {
                    ShutAccumulatorWindow();
                } else if (nodeWindow.active) {
                    ShutNodeWindow();
                } else if (generatorWindow.active) {
                    if (generatorWindow.needInventory) {
                        if (inventoryWindow.active) {
                            ShutGeneratorWindow();
                            ShutPlayerInventory();
                        } else {
                            On_E_Switch();
                        }
                    } else {
                        ShutGeneratorWindow();
                    }
                } else if (exchangerWindow.active) {
                    if (inventoryWindow.active) {
                        ShutExchangerWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (stationWindow.active) {
                    if (inventoryWindow.active) {
                        ShutStationWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (dispenserWindow.active) {
                    if (inventoryWindow.active) {
                        ShutDispenserWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (blueprintBrowser.active) {
                    ShutBlueprintBrowser();
                } else if (monitorWindow.active) {
                    ShutMonitorWindow();
                } else if (spraycoaterWindow.active) {
                    if (inventoryWindow.active) {
                        ShutSpraycoaterWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (turretWindow.active) {
                    if (inventoryWindow.active) {
                        ShutTurretWindow();
                        ShutPlayerInventory();
                    } else {
                        On_E_Switch();
                    }
                } else if (fieldGeneratorWindow.active) {
                    ShutFieldGeneratorWindow();
                } else if (battleBaseWindow.active) {
                    ShutBattleBaseWindow();
                } else if (communicatorWindow.active) {
                    ShutCommunicatorWindow();
                } else if (techTree.active) {
                    On_E_Switch();
                } else if (starmap.active) {
                    On_E_Switch();
                } else if (dysonEditor.active) {
                    On_E_Switch();
                } else if (globemap.active) {
                    On_E_Switch();
                } else {
                    On_E_Switch();
                }
            } else if (VFInput._openMechaPanel && !VFInput.inputing && flag && !VFInput.copyBuildingAndMechPanelConflict && !VFInput.copyRecipeAndMechPanelConflict) {
                On_C_Switch();
            } else if (VFInput._openReplicatorPanel && !VFInput.inputing && flag) {
                On_F_Switch();
            } else if (VFInput._openProductionPanel && !VFInput.inputing && flag) {
                On_P_Switch();
            } else if (VFInput._openDetailFunc && !VFInput.inputing && flag) {
                On_H_Switch();
            } else if (VFInput._zScreenKey && !VFInput.inputing && !VFInput.inFullscreenGUI && flag) {
                On_Z_Switch();
            } else if (VFInput._openTechTree && !VFInput.inputing && flag) {
                On_T_Switch();
            } else if (VFInput._openStarmap && !VFInput.inputing && flag && !VFInput.pasteRecipeAndStarMapConflict) {
                On_V_Switch();
            } else if (VFInput._openPlanetView && !VFInput.inputing) {
                if (gameData.localPlanet != null && !starmap.active && !VFInput.inFullscreenGUI) {
                    On_M_Switch();
                } else if ((gameData.localPlanet == null || starmap.active) && flag) {
                    On_V_Switch();
                }
            } else if (VFInput._openDysonEditor && !VFInput.inputing && flag && GameMain.history.dysonSphereSystemUnlocked) {
                On_Y_Switch();
            } else if (VFInput._openMechLight && !VFInput.inputing && !inZScreen) {
                gameMenu.OnDfLightButtonClick();
            }
        }
        DetermineViewMode(mainPlayer);
        if (dysonEditor.active) {
            DysonSphere.renderPlace = ERenderPlace.Dysonmap;
        } else if (starmap.isFullOpened) {
            DysonSphere.renderPlace = ERenderPlace.Starmap;
        } else {
            DysonSphere.renderPlace = ERenderPlace.Universe;
        }
        PlanetData planetData = (dfVeinOnFinal ? gameData.localPlanet : null);
        if (veinDetail.inspectPlanet != planetData) {
            veinDetail.SetInspectPlanet(planetData);
        }
        PlanetData planetData2 = null;
        if (inZScreen) {
            planetData2 = gameData.localPlanet;
            SetZScreenDetail(planetData2);
        }
        planetData2 = null;
        if (starmap.isFullOpened) {
            planetData2 = ((starmap.focusPlanet == null) ? null : starmap.focusPlanet.planet);
            if (planetData2 == null) {
                planetData2 = starmap.viewPlanet;
            }
        } else if (viewMode == EViewMode.Globe) {
            planetData2 = gameData.localPlanet;
        }
        SetPlanetDetail(planetData2);
        StarData starData = null;
        if (planetData2 == null && gameData.guideComplete) {
            if (starmap.isFullOpened) {
                starData = ((starmap.focusStar == null) ? null : starmap.focusStar.star);
                if (starData == null) {
                    starData = starmap.viewStar;
                }
            } else if (gameData.localPlanet == null && gameData.localStar != null && (mainPlayer.uPosition - gameData.localStar.uPosition).magnitude < (double)gameData.localStar.physicsRadius) {
                starData = gameData.localStar;
            }
        }
        SetStarDetail(starData);
        bool flag3 = !hideAllUI0 && ((globemap.modeReady && globemap.active) || flag2);
        if (flag3 != polarMark.activeSelf) {
            polarMark.SetActive(flag3);
        }
        PlanetData localPlanet = gameData.localPlanet;
        removeBasePitBtn.SetLocalFactory((localPlanet == null) ? null : (localPlanet.factoryLoaded ? localPlanet.factory : null));
        if (DSPGame.IsMenuDemo) {
            disableLockCursor = true;
        }
        if (mainPlayer.sailing) {
            if (VFInput._sailLockCursor) {
                disableLockCursor = !disableLockCursor;
            }
            if (!disableLockCursor && !isCursorNeeded) {
                UICursor.LockCursor();
            }
        } else {
            disableLockCursor = false;
        }
        if (willClose) {
            willClose = false;
            _Close();
        }
    }
}