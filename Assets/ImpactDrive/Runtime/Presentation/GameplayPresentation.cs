using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImpactDrive.Runtime
{
    /// <summary>Functional development HUD. Input is collected per rendered frame and sampled by vehicle physics.</summary>
    public sealed class GameplayPresentation : MonoBehaviour
    {
        private VehicleController vehicle;
        private VehicleCamera cameraDriver;
        private ProceduralEngineAudio engineAudio;
        private PlayerSettings settings;
        private RectTransform safeRoot;
        private Font font;
        private Sprite circle, whiteSquare;
        private VirtualControl steeringStick, steeringWheel, steeringLeft, steeringRight, throttle, brake, handbrake, look;
        private GameObject steeringButtons, tiltHint, pausePanel, telemetryPanel;
        private Text speedText, gearText, statusText, telemetryText, steeringModeText, assistText, transmissionText, cameraText;
        private Text fpsTargetText, qualityText, slowText, muteStatusText, steeringAssistText;
        private Image rpmFill;
        private bool paused, slowMotion, settingsDirty;
        private float saveAt, nextTelemetry, smoothedFrameTime = .016f;
        private Rect lastSafeArea;
        private int lastWidth, lastHeight;
        private string savePath;
        private string saveNotice = string.Empty;
        private string drivingNotice = string.Empty;
        private float drivingNoticeUntil;
        private readonly StringBuilder telemetry = new StringBuilder(700);
        private static readonly string[] WheelNames = { "FL", "FR", "RL", "RR" };
        private Canvas canvas;

        public static GameplayPresentation Create(VehicleController vehicle)
        {
            if (!vehicle) throw new ArgumentNullException(nameof(vehicle));
            var root = new GameObject("ImpactDrive presentation");
            var presentation = root.AddComponent<GameplayPresentation>();
            presentation.vehicle = vehicle;
            presentation.Initialize();
            return presentation;
        }

        private void Initialize()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circle = CreateCircle();
            whiteSquare = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f);
            savePath = Path.Combine(Application.persistentDataPath, "impactdrive-settings.save");
            LoadSettings();
            if (!EventSystem.current)
            {
                var events = new GameObject("Touch event system");
                events.transform.SetParent(transform, false);
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>();
            }
            cameraDriver = VehicleCamera.Create(vehicle);
            engineAudio = ProceduralEngineAudio.Create(vehicle);
            BuildCanvas();
            ApplySettings();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("Driving HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = .5f;
            safeRoot = Rect(canvasObject.transform, "Safe area", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
            UpdateSafeArea();

            Text label = Label(safeRoot, "IMPACTDRIVE  /  ENGINEERING BUILD", new Vector2(28, -26), new Vector2(560, 28), 22, TextAnchor.MiddleLeft);
            label.color = new Color(.38f, .87f, .92f);
            Label(safeRoot, "ASTER HATCHBACK  ·  PROVING GROUND", new Vector2(28, -58), new Vector2(540, 24), 17, TextAnchor.MiddleLeft);
            statusText = Label(safeRoot, "", new Vector2(28, -90), new Vector2(580, 70), 17, TextAnchor.UpperLeft);

            RectTransform speedPanel = Rect(safeRoot, "Live speed", new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(.5f, 1f), new Vector2(0, -22), new Vector2(280, 150));
            speedText = Label(speedPanel, "0", new Vector2(0, 0), new Vector2(200, 94), 76, TextAnchor.MiddleRight);
            Label(speedPanel, "km/h", new Vector2(208, -46), new Vector2(70, 35), 21, TextAnchor.MiddleLeft);
            gearText = Label(speedPanel, "D  1", new Vector2(0, -92), new Vector2(280, 30), 22, TextAnchor.MiddleCenter);
            var rpmTrack = Panel(speedPanel, "RPM scale", new Vector2(0, -136), new Vector2(280, 6), new Color(.1f, .15f, .19f, .85f));
            rpmFill = Panel(rpmTrack.transform, "RPM", Vector2.zero, new Vector2(280, 6), new Color(.22f, .84f, .85f)).GetComponent<Image>();
            rpmFill.type = Image.Type.Filled;
            rpmFill.fillMethod = Image.FillMethod.Horizontal;
            rpmFill.fillOrigin = 0;

            RectTransform actions = Rect(safeRoot, "Actions", Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-28, -24), new Vector2(450, 150));
            cameraText = Action(actions, "Camera", new Vector2(0, 0), new Vector2(142, 48), ChangeCamera);
            Action(actions, "Reset", new Vector2(154, 0), new Vector2(142, 48), ResetVehicle);
            Action(actions, "Pause", new Vector2(308, 0), new Vector2(142, 48), () => SetPaused(true));
            Action(actions, "Repair", new Vector2(0, -60), new Vector2(142, 48), () => vehicle.RepairVehicle());
            slowText = Action(actions, "Time ×1", new Vector2(154, -60), new Vector2(142, 48), ToggleSlowMotion);
            Action(actions, "Engine", new Vector2(308, -60), new Vector2(142, 48), () => vehicle.ToggleEngine());

            steeringStick = Control(safeRoot, "Steering stick", new Vector2(178, 172), new Vector2(260, 260),
                VirtualControl.ControlKind.Stick, "STEER", false);
            steeringStick.GetComponent<Image>().sprite = circle;
            RectTransform thumb = Rect(steeringStick.transform, "Thumb", Vector2.one * .5f, Vector2.one * .5f,
                Vector2.one * .5f, Vector2.zero, new Vector2(80, 80));
            Image thumbImage = thumb.gameObject.AddComponent<Image>();
            thumbImage.sprite = circle;
            thumbImage.color = new Color(.36f, .83f, .87f, .85f);
            thumbImage.raycastTarget = false;
            steeringStick.Thumb = thumb;

            steeringWheel = Control(safeRoot, "Steering wheel", new Vector2(178, 172), new Vector2(260, 260),
                VirtualControl.ControlKind.Wheel, "WHEEL", false);
            steeringWheel.GetComponent<Image>().sprite = circle;
            RectTransform spokes = Rect(steeringWheel.transform, "Spokes", Vector2.one * .5f, Vector2.one * .5f,
                Vector2.one * .5f, Vector2.zero, new Vector2(180, 180));
            Image spoke = Panel(spokes, "Crossbar", new Vector2(0, -80), new Vector2(180, 14), new Color(.4f, .8f, .85f, .8f)).GetComponent<Image>();
            spoke.raycastTarget = false;
            Image stem = Panel(spokes, "Stem", new Vector2(83, -80), new Vector2(14, 100), new Color(.4f, .8f, .85f, .8f)).GetComponent<Image>();
            stem.raycastTarget = false;
            steeringWheel.Thumb = spokes;

            RectTransform buttons = Rect(safeRoot, "Steering buttons", Vector2.zero, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(360, 300));
            steeringButtons = buttons.gameObject;
            steeringLeft = Control(buttons, "Left", new Vector2(90, 130), new Vector2(130, 160), VirtualControl.ControlKind.Button, "◀", false);
            steeringRight = Control(buttons, "Right", new Vector2(245, 130), new Vector2(130, 160), VirtualControl.ControlKind.Button, "▶", false);
            RectTransform tilt = Rect(safeRoot, "Tilt hint", Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(28, 40), new Vector2(310, 175));
            tiltHint = tilt.gameObject;
            Label(tilt, "TILT TO STEER\n\nHold the phone naturally.\nCalibrate in Pause → Controls.", Vector2.zero,
                new Vector2(310, 175), 23, TextAnchor.MiddleCenter);

            throttle = Control(safeRoot, "Throttle", new Vector2(-100, 160), new Vector2(145, 250), VirtualControl.ControlKind.Pedal, "THROTTLE", true);
            brake = Control(safeRoot, "Brake", new Vector2(-270, 160), new Vector2(145, 250), VirtualControl.ControlKind.Pedal, "BRAKE", true);
            handbrake = Control(safeRoot, "Handbrake", new Vector2(-435, 85), new Vector2(145, 95), VirtualControl.ControlKind.Button, "HANDBRAKE", true);
            RectTransform gears = Rect(safeRoot, "Gear buttons", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-362, 150), new Vector2(145, 135));
            Action(gears, "D / R", Vector2.zero, new Vector2(145, 42), ChangeDirection);
            Action(gears, "−", new Vector2(0, -50), new Vector2(67, 42), () => vehicle.ShiftDown());
            Action(gears, "+", new Vector2(78, -50), new Vector2(67, 42), () => vehicle.ShiftUp());
            Action(gears, "Neutral", new Vector2(0, -100), new Vector2(145, 35), () => vehicle.SetGear(0));

            look = Control(safeRoot, "Camera look", new Vector2(-110, 420), new Vector2(165, 120), VirtualControl.ControlKind.Stick, "LOOK", true);
            look.GetComponent<Image>().color = new Color(.05f, .11f, .15f, .23f);
            cameraDriver.LookControl = look;

            RectTransform telemetryRect = Rect(safeRoot, "Wheel telemetry", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f),
                new Vector2(28, 35), new Vector2(515, 220));
            telemetryPanel = telemetryRect.gameObject;
            Image telemetryBackground = telemetryRect.gameObject.AddComponent<Image>();
            telemetryBackground.color = new Color(.025f, .045f, .07f, .70f);
            telemetryBackground.raycastTarget = false;
            telemetryText = Label(telemetryRect, "", new Vector2(14, -10), new Vector2(490, 202), 17, TextAnchor.UpperLeft);
            Text controlsHelp = Label(safeRoot, "", new Vector2(450, -190), new Vector2(700, 45), 15, TextAnchor.MiddleCenter);
            controlsHelp.text = Application.isMobilePlatform ? "Pedals are analogue: drag upward for more input. Tap D / R when stopped."
                : "W / ↑ throttle  ·  S / ↓ brake  ·  A / D steer  ·  Space handbrake\nR reverse/drive  ·  Q / E shift  ·  C camera  ·  Backspace reset  ·  Esc pause";

            BuildPauseMenu();
        }

        private void BuildPauseMenu()
        {
            RectTransform overlay = Rect(safeRoot, "Pause and settings", Vector2.zero, Vector2.one, Vector2.one * .5f,
                Vector2.zero, Vector2.zero);
            pausePanel = overlay.gameObject;
            Image shade = overlay.gameObject.AddComponent<Image>();
            shade.color = new Color(.015f, .025f, .04f, .95f);
            RectTransform panel = Rect(overlay, "Settings", Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f,
                Vector2.zero, new Vector2(860, 765));
            Label(panel, "PAUSED", new Vector2(0, 0), new Vector2(860, 48), 38, TextAnchor.MiddleLeft);
            Label(panel, "Driving foundation · Work in progress · No device performance certification", new Vector2(0, -55),
                new Vector2(860, 30), 18, TextAnchor.MiddleLeft);
            steeringModeText = Action(panel, "Steering", new Vector2(0, -110), new Vector2(400, 48), CycleSteeringMode);
            Action(panel, "Calibrate tilt", new Vector2(420, -110), new Vector2(210, 48), () =>
            {
                settings.TiltCenter = Input.acceleration.x;
                MarkSettingsDirty();
            });
            Action(panel, "Telemetry", new Vector2(650, -110), new Vector2(210, 48), () =>
            {
                settings.ShowTelemetry = !settings.ShowTelemetry;
                ApplySettings();
                MarkSettingsDirty();
            });
            SliderRow(panel, "Steering sensitivity", -184, .4f, 1.8f, settings.SteeringSensitivity, value =>
            {
                settings.SteeringSensitivity = value;
                vehicle.SteeringSensitivity = value;
                MarkSettingsDirty();
            });
            SliderRow(panel, "Steering dead zone", -246, 0f, .25f, settings.SteeringDeadZone, value =>
            {
                settings.SteeringDeadZone = value;
                MarkSettingsDirty();
            });
            SliderRow(panel, "Engine sound", -308, 0f, 1f, settings.SoundVolume, value =>
            {
                settings.SoundVolume = value;
                engineAudio.Volume = value;
                MarkSettingsDirty();
            });
            fpsTargetText = Action(panel, "Frame target", new Vector2(0, -380), new Vector2(265, 48), () =>
            {
                settings.TargetFrameRate = settings.TargetFrameRate == 30 ? 60 : 30;
                ApplySettings(); MarkSettingsDirty();
            });
            qualityText = Action(panel, "Quality", new Vector2(285, -380), new Vector2(285, 48), () =>
            {
                settings.QualityLevel = (settings.QualityLevel + 1) % Mathf.Max(1, QualitySettings.names.Length);
                ApplySettings(); MarkSettingsDirty();
            });
            transmissionText = Action(panel, "Automatic", new Vector2(590, -380), new Vector2(270, 48), () =>
            {
                vehicle.ToggleTransmission(); RefreshSettingsLabels();
            });
            assistText = Action(panel, "Assists", new Vector2(0, -444), new Vector2(400, 48), () =>
            {
                // Four explicit configurations: both, ABS only, neither, traction only.
                if (settings.AbsEnabled && settings.TractionControlEnabled) settings.TractionControlEnabled = false;
                else if (settings.AbsEnabled) settings.AbsEnabled = false;
                else if (!settings.TractionControlEnabled) settings.TractionControlEnabled = true;
                else settings.AbsEnabled = true;
                ApplySettings(); MarkSettingsDirty();
            });
            Action(panel, "Repair and reset", new Vector2(420, -444), new Vector2(440, 48), () =>
            {
                vehicle.RepairVehicle(); ResetVehicle(); SetPaused(false);
            });
            steeringAssistText = Action(panel, "Steering assist", new Vector2(0, -508), new Vector2(400, 44), () =>
            {
                settings.SteeringAssistEnabled = !settings.SteeringAssistEnabled;
                ApplySettings(); MarkSettingsDirty();
            });
            Label(panel, "Steering assist limits rack angle at speed.", new Vector2(420, -508), new Vector2(440, 44), 18, TextAnchor.MiddleLeft);
            Label(panel, "Graphics settings do not change the physics timestep.\nTilt uses device gravity. Settings save offline with a recovery backup.",
                new Vector2(0, -570), new Vector2(860, 50), 19, TextAnchor.UpperLeft);
            muteStatusText = Label(panel, "", new Vector2(0, -626), new Vector2(860, 36), 17, TextAnchor.MiddleLeft);
            Action(panel, "RESUME DRIVING", new Vector2(0, -688), new Vector2(860, 64), () => SetPaused(false));
            pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (!vehicle) return;
            UpdateSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
            if (!paused)
            {
                if (Input.GetKeyDown(KeyCode.C)) ChangeCamera();
                if (Input.GetKeyDown(KeyCode.R)) ChangeDirection();
                if (Input.GetKeyDown(KeyCode.Q)) vehicle.ShiftDown();
                if (Input.GetKeyDown(KeyCode.E)) vehicle.ShiftUp();
                if (Input.GetKeyDown(KeyCode.Backspace)) ResetVehicle();
                ReadDrivingInput();
            }
            else vehicle.Input = default(DriverInput);
            smoothedFrameTime = Mathf.Lerp(smoothedFrameTime, Time.unscaledDeltaTime, .05f);
            if (Time.unscaledTime >= nextTelemetry)
            {
                nextTelemetry = Time.unscaledTime + .1f;
                RefreshTelemetry();
            }
            if (settingsDirty && Time.unscaledTime >= saveAt) SaveSettings();
        }

        private void ReadDrivingInput()
        {
            float steering;
            switch (settings.SteeringMode)
            {
                case SteeringControlMode.Wheel: steering = steeringWheel.Value.x; break;
                case SteeringControlMode.Buttons: steering = steeringRight.Value.x - steeringLeft.Value.x; break;
                case SteeringControlMode.Tilt: steering = Mathf.Clamp((Input.acceleration.x - settings.TiltCenter) * 2.7f, -1f, 1f); break;
                default: steering = steeringStick.Value.x; break;
            }
            float keyboard = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            if (keyboard != 0f) steering = keyboard;
            float magnitude = Mathf.Abs(steering);
            steering = magnitude <= settings.SteeringDeadZone ? 0f : Mathf.Sign(steering)
                * (magnitude - settings.SteeringDeadZone) / (1f - settings.SteeringDeadZone);
            vehicle.Input = new DriverInput
            {
                Steering = Mathf.Clamp(steering, -1f, 1f),
                Throttle = Mathf.Max(throttle.Value.y, Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f),
                Brake = Mathf.Max(brake.Value.y, Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f),
                Handbrake = Mathf.Max(handbrake.Value.y, Input.GetKey(KeyCode.Space) ? 1f : 0f)
            };
        }

        private void RefreshTelemetry()
        {
            speedText.text = Mathf.Abs(vehicle.SpeedKph).ToString("0");
            string gear = vehicle.CurrentGear < 0 ? "R" : vehicle.CurrentGear == 0 ? "N" : vehicle.CurrentGear.ToString();
            gearText.text = (vehicle.AutomaticTransmission ? "AUTO  " : "MANUAL  ") + gear + "   ·   " + vehicle.EngineRpm.ToString("0") + " rpm";
            rpmFill.fillAmount = Mathf.Clamp01(vehicle.EngineRpm / vehicle.Specification.RedlineRpm);
            bool anyAbs = false;
            if (vehicle.Wheels != null)
                foreach (WheelTelemetry wheel in vehicle.Wheels) anyAbs |= wheel.AbsActive;
            statusText.text = (vehicle.EngineRunning ? "ENGINE RUNNING" : "ENGINE STOPPED") + "   ·   HEALTH "
                + (vehicle.EngineHealth * 100f).ToString("0") + "%\n" + (vehicle.AbsEnabled ? anyAbs ? "ABS ACTIVE" : "ABS ON" : "ABS OFF")
                + "   ·   TC " + (vehicle.TractionControlEnabled ? "ON" : "OFF") + "   ·   "
                + (1f / Mathf.Max(.001f, smoothedFrameTime)).ToString("0") + " render fps";
            if (Time.unscaledTime < drivingNoticeUntil) statusText.text += "\n" + drivingNotice;
            if (!settings.ShowTelemetry || vehicle.Wheels == null) return;
            telemetry.Clear();
            telemetry.AppendLine("LIVE WHEEL TELEMETRY · SI UNITS");
            int count = Mathf.Min(4, vehicle.Wheels.Length);
            for (int i = 0; i < count; i++)
            {
                WheelTelemetry wheel = vehicle.Wheels[i];
                telemetry.Append(WheelNames[i]).Append(wheel.Grounded ? " contact  " : " airborne ")
                    .Append(wheel.NormalLoadN.ToString("0")).Append(" N  sag ")
                    .Append((wheel.CompressionM * 1000f).ToString("0")).Append(" mm\n")
                    .Append("     slip ").Append(wheel.SlipRatio.ToString("0.00"))
                    .Append("  angle ").Append((wheel.SlipAngleRad * Mathf.Rad2Deg).ToString("0.0")).Append("°")
                    .Append("  brake ").Append(wheel.BrakeTemperatureC.ToString("0")).Append("°C\n");
            }
            telemetryText.text = telemetry.ToString();
        }

        private void SetPaused(bool value)
        {
            paused = value;
            Time.timeScale = paused ? 0f : slowMotion ? .25f : 1f;
            if (pausePanel) pausePanel.SetActive(paused);
            if (cameraDriver) cameraDriver.InputEnabled = !paused;
            ReleaseControls();
            if (paused) { RefreshSettingsLabels(); SaveSettings(); }
        }

        private void ReleaseControls()
        {
            VirtualControl[] controls = { steeringStick, steeringWheel, steeringLeft, steeringRight, throttle, brake, handbrake, look };
            foreach (VirtualControl control in controls) if (control) control.Release();
            if (vehicle) vehicle.Input = default(DriverInput);
        }

        private void ChangeCamera()
        {
            cameraDriver.NextView();
            cameraText.text = cameraDriver.Mode.ToString();
        }

        private void ResetVehicle()
        {
            ReleaseControls();
            vehicle.ResetVehicle();
            cameraDriver.SnapToVehicle();
        }

        private void ChangeDirection()
        {
            if (Mathf.Abs(vehicle.SignedSpeedMps) > 1f)
            {
                drivingNotice = "Stop before selecting reverse or drive.";
                drivingNoticeUntil = Time.unscaledTime + 3f;
                return;
            }
            vehicle.SetGear(vehicle.CurrentGear < 0 ? 1 : -1);
        }

        private void ToggleSlowMotion()
        {
            slowMotion = !slowMotion;
            if (!paused) Time.timeScale = slowMotion ? .25f : 1f;
            slowText.text = slowMotion ? "Time ×0.25" : "Time ×1";
        }

        private void CycleSteeringMode()
        {
            settings.SteeringMode = (SteeringControlMode)(((int)settings.SteeringMode + 1) % 4);
            ReleaseControls();
            ApplySettings();
            MarkSettingsDirty();
        }

        private void ApplySettings()
        {
            settings.Validate();
            Application.targetFrameRate = settings.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            QualitySettings.SetQualityLevel(settings.QualityLevel, true);
            vehicle.AbsEnabled = settings.AbsEnabled;
            vehicle.TractionControlEnabled = settings.TractionControlEnabled;
            vehicle.SteeringAssistEnabled = settings.SteeringAssistEnabled;
            vehicle.SteeringSensitivity = settings.SteeringSensitivity;
            engineAudio.Volume = settings.SoundVolume;
            steeringStick.gameObject.SetActive(settings.SteeringMode == SteeringControlMode.Stick);
            steeringWheel.gameObject.SetActive(settings.SteeringMode == SteeringControlMode.Wheel);
            steeringButtons.SetActive(settings.SteeringMode == SteeringControlMode.Buttons);
            tiltHint.SetActive(settings.SteeringMode == SteeringControlMode.Tilt);
            telemetryPanel.SetActive(settings.ShowTelemetry);
            RefreshSettingsLabels();
        }

        private void RefreshSettingsLabels()
        {
            if (!steeringModeText) return;
            steeringModeText.text = "Steering: " + settings.SteeringMode;
            fpsTargetText.text = "Frame target: " + settings.TargetFrameRate;
            string[] qualities = QualitySettings.names;
            qualityText.text = "Quality: " + (qualities.Length > 0 ? qualities[settings.QualityLevel] : "Default");
            transmissionText.text = vehicle.AutomaticTransmission ? "Automatic transmission" : "Manual transmission";
            assistText.text = "ABS " + (settings.AbsEnabled ? "ON" : "OFF") + "   /   TC " + (settings.TractionControlEnabled ? "ON" : "OFF");
            steeringAssistText.text = "Steering assist: " + (settings.SteeringAssistEnabled ? "ON" : "OFF");
            muteStatusText.text = saveNotice;
        }

        private void MarkSettingsDirty() { settingsDirty = true; saveAt = Time.unscaledTime + .75f; }

        private void LoadSettings()
        {
            settings = new PlayerSettings();
            bool recovered;
            string payload = AtomicSaveFile.Read(savePath, out recovered);
            if (payload == null) return;
            try
            {
                PlayerSettings loaded = JsonUtility.FromJson<PlayerSettings>(payload);
                if (loaded == null) throw new ArgumentException("Settings were empty.");
                loaded.Validate();
                settings = loaded;
                saveNotice = recovered ? "Settings recovered from the last good backup." : string.Empty;
            }
            catch (ArgumentException)
            {
                saveNotice = "Settings could not be read. Safe defaults are active.";
            }
        }

        private void SaveSettings()
        {
            if (settings == null || string.IsNullOrEmpty(savePath)) return;
            try
            {
                settings.Validate();
                AtomicSaveFile.Write(savePath, JsonUtility.ToJson(settings));
                settingsDirty = false;
            }
            catch (IOException exception)
            {
                saveNotice = "Settings could not be saved. Check available storage.";
                settingsDirty = false;
                Debug.LogWarning("Settings save failed: " + exception.GetType().Name);
            }
            catch (UnauthorizedAccessException)
            {
                saveNotice = "Settings storage is unavailable.";
                settingsDirty = false;
            }
            if (muteStatusText) muteStatusText.text = saveNotice;
        }

        private void UpdateSafeArea()
        {
            if (!safeRoot || Screen.width == 0 || Screen.height == 0) return;
            Rect area = Screen.safeArea;
            if (area == lastSafeArea && lastWidth == Screen.width && lastHeight == Screen.height) return;
            safeRoot.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
            lastSafeArea = area; lastWidth = Screen.width; lastHeight = Screen.height;
        }

        private void OnApplicationPause(bool value) { if (value && settings != null) SetPaused(true); }
        private void OnApplicationFocus(bool value) { if (!value && settings != null) SetPaused(true); }
        private void OnDestroy()
        {
            SaveSettings();
            Time.timeScale = 1f;
            if (circle) { Texture2D texture = circle.texture; Destroy(circle); Destroy(texture); }
            if (whiteSquare) Destroy(whiteSquare);
            if (cameraDriver) Destroy(cameraDriver.gameObject);
        }

        private RectTransform Rect(Transform parent, string name, Vector2 minimum, Vector2 maximum, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var element = new GameObject(name, typeof(RectTransform));
            var rect = element.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = minimum; rect.anchorMax = maximum; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = whiteSquare;
            image.color = color;
            image.raycastTarget = false;
            return rect.gameObject;
        }

        private Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = Rect(parent, "Label", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.text = value;
            text.alignment = alignment; text.color = new Color(.9f, .95f, .98f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Text Action(Transform parent, string label, Vector2 position, Vector2 size, Action action)
        {
            GameObject buttonObject = Panel(parent, label, position, size, new Color(.09f, .18f, .23f, .94f));
            Image image = buttonObject.GetComponent<Image>(); image.raycastTarget = true;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.7f, .95f, 1f);
            colors.pressedColor = new Color(.3f, .7f, .8f); button.colors = colors;
            button.onClick.AddListener(() => action());
            return Label(buttonObject.transform, label, new Vector2(5, -3), size - new Vector2(10, 6), 20, TextAnchor.MiddleCenter);
        }

        private VirtualControl Control(Transform parent, string name, Vector2 position, Vector2 size, VirtualControl.ControlKind kind, string label, bool right)
        {
            Vector2 anchor = right ? new Vector2(1, 0) : Vector2.zero;
            RectTransform rect = Rect(parent, name, anchor, anchor, Vector2.one * .5f, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.05f, .11f, .15f, .65f);
            VirtualControl control = rect.gameObject.AddComponent<VirtualControl>(); control.Kind = kind;
            if (kind == VirtualControl.ControlKind.Pedal || kind == VirtualControl.ControlKind.Button)
            {
                Image fill = Panel(rect, "Input strength", Vector2.zero, size, new Color(.15f, .66f, .7f, .4f)).GetComponent<Image>();
                fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Vertical; fill.fillOrigin = 0; fill.fillAmount = 0f;
                control.Fill = fill;
            }
            Label(rect, label, Vector2.zero, size, kind == VirtualControl.ControlKind.Button && label.Length == 1 ? 42 : 18, TextAnchor.MiddleCenter);
            return control;
        }

        private void SliderRow(Transform parent, string label, float y, float minimum, float maximum, float value, Action<float> changed)
        {
            Label(parent, label, new Vector2(0, y), new Vector2(300, 40), 21, TextAnchor.MiddleLeft);
            RectTransform rect = Rect(parent, label, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(320, y), new Vector2(540, 40));
            Image hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            Slider slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = minimum; slider.maxValue = maximum;
            RectTransform track = Rect(rect, "Track", new Vector2(0, .35f), new Vector2(1, .65f), Vector2.one * .5f, Vector2.zero, Vector2.zero);
            Image trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = new Color(.1f, .2f, .25f); trackImage.raycastTarget = false;
            RectTransform fill = Rect(track, "Fill", Vector2.zero, Vector2.one, Vector2.one * .5f, Vector2.zero, Vector2.zero);
            Image fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = new Color(.2f, .7f, .75f); fillImage.raycastTarget = false;
            RectTransform handle = Rect(rect, "Handle", new Vector2(0, .5f), new Vector2(0, .5f), Vector2.one * .5f, Vector2.zero, new Vector2(32, 40));
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(.75f, .94f, .96f); handleImage.raycastTarget = false;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight; slider.value = value;
            Navigation navigation = slider.navigation; navigation.mode = Navigation.Mode.None; slider.navigation = navigation;
            slider.onValueChanged.AddListener(v => changed(v));
        }

        private Sprite CreateCircle()
        {
            const int side = 64;
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false);
            texture.name = "Original procedural touch circle";
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
            {
                float distance = new Vector2(x - 31.5f, y - 31.5f).magnitude;
                pixels[y * side + x] = new Color(1, 1, 1, Mathf.Clamp01(31.5f - distance));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, side, side), Vector2.one * .5f, side);
        }
    }
}
