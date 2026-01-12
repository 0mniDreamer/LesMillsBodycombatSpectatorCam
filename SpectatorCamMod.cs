using MelonLoader;
using UnityEngine;

namespace BodycombatSpectatorCam
{
    public class SpectatorCamMod : MelonMod
    {
        // ============================================
        // PREFERENCES - All settings stored in UserData/SpectatorCam.cfg
        // ============================================
        private static MelonPreferences_Category _prefCategory;
        
        // Position Settings
        private static MelonPreferences_Entry<float> _cameraDistance;
        private static MelonPreferences_Entry<float> _cameraHeight;
        private static MelonPreferences_Entry<float> _horizontalOffset;
        private static MelonPreferences_Entry<float> _lookAtHeightOffset;
        
        // Camera Settings
        private static MelonPreferences_Entry<float> _fieldOfView;
        private static MelonPreferences_Entry<float> _nearClipPlane;
        private static MelonPreferences_Entry<float> _farClipPlane;
        
        // Smoothing Settings
        private static MelonPreferences_Entry<float> _positionSmoothing;
        private static MelonPreferences_Entry<float> _rotationSmoothing;
        
        // Behavior Settings
        private static MelonPreferences_Entry<bool> _enabledByDefault;
        private static MelonPreferences_Entry<bool> _followBodyRotation;
        private static MelonPreferences_Entry<bool> _lockVerticalRotation;
        
        // Keybinds
        private static MelonPreferences_Entry<string> _toggleKey;
        private static MelonPreferences_Entry<string> _reloadConfigKey;
        
        // Advanced
        private static MelonPreferences_Entry<int> _targetDisplay;
        private static MelonPreferences_Entry<float> _cameraDepth;

        // Runtime state
        private Camera _spectatorCamera;
        private GameObject _spectatorCameraObj;
        private Transform _playerHead;
        private Transform _playerBody;
        private bool _isEnabled = false;
        private bool _initialized = false;

        // Smoothing
        private Vector3 _currentCameraPosition;
        private Quaternion _currentCameraRotation;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Spectator Camera Mod initializing...");
            
            SetupPreferences();
            
            LoggerInstance.Msg("=========================================");
            LoggerInstance.Msg("Spectator Camera Mod loaded!");
            LoggerInstance.Msg($"Toggle Key: {_toggleKey.Value}");
            LoggerInstance.Msg($"Reload Config Key: {_reloadConfigKey.Value}");
            LoggerInstance.Msg("Config file: UserData/SpectatorCam.cfg");
            LoggerInstance.Msg("=========================================");
        }

        private void SetupPreferences()
        {
            // Create category with its own file
            _prefCategory = MelonPreferences.CreateCategory("SpectatorCam", "Spectator Camera Settings");
            _prefCategory.SetFilePath("UserData/SpectatorCam.cfg");

            // ============ POSITION SETTINGS ============
            _cameraDistance = _prefCategory.CreateEntry(
                "Distance", 
                2.5f, 
                "Camera Distance",
                "How far behind the player the camera sits (in meters)");
            
            _cameraHeight = _prefCategory.CreateEntry(
                "Height", 
                0.5f, 
                "Camera Height Offset",
                "Height offset above the player's head (in meters). Positive = higher, Negative = lower");
            
            _horizontalOffset = _prefCategory.CreateEntry(
                "HorizontalOffset", 
                0.3f, 
                "Horizontal Offset",
                "Offset to the right (positive) or left (negative) in meters");
            
            _lookAtHeightOffset = _prefCategory.CreateEntry(
                "LookAtHeightOffset", 
                0.1f, 
                "Look At Height Offset",
                "How far above the head the camera looks at (in meters)");

            // ============ CAMERA SETTINGS ============
            _fieldOfView = _prefCategory.CreateEntry(
                "FieldOfView", 
                75f, 
                "Field of View",
                "Camera field of view in degrees (default: 75, wider = more visible)");
            
            _nearClipPlane = _prefCategory.CreateEntry(
                "NearClipPlane", 
                0.01f, 
                "Near Clip Plane",
                "Minimum render distance (lower = can see closer objects)");
            
            _farClipPlane = _prefCategory.CreateEntry(
                "FarClipPlane", 
                1000f, 
                "Far Clip Plane",
                "Maximum render distance");

            // ============ SMOOTHING SETTINGS ============
            _positionSmoothing = _prefCategory.CreateEntry(
                "PositionSmoothing", 
                8f, 
                "Position Smoothing",
                "How smoothly the camera follows position (higher = snappier, lower = smoother). Range: 1-20");
            
            _rotationSmoothing = _prefCategory.CreateEntry(
                "RotationSmoothing", 
                8f, 
                "Rotation Smoothing",
                "How smoothly the camera rotates (higher = snappier, lower = smoother). Range: 1-20");

            // ============ BEHAVIOR SETTINGS ============
            _enabledByDefault = _prefCategory.CreateEntry(
                "EnabledByDefault", 
                true, 
                "Enabled By Default",
                "Whether the spectator camera is enabled when the game starts");
            
            _followBodyRotation = _prefCategory.CreateEntry(
                "FollowBodyRotation", 
                true, 
                "Follow Body Rotation",
                "If true, camera orbits based on body/playspace rotation. If false, follows head rotation");
            
            _lockVerticalRotation = _prefCategory.CreateEntry(
                "LockVerticalRotation", 
                true, 
                "Lock Vertical Rotation",
                "If true, camera stays level and doesn't tilt up/down with the player's head");

            // ============ KEYBINDS ============
            _toggleKey = _prefCategory.CreateEntry(
                "ToggleKey", 
                "F8", 
                "Toggle Key",
                "Key to toggle spectator camera on/off (e.g., F8, F5, Home, End)");
            
            _reloadConfigKey = _prefCategory.CreateEntry(
                "ReloadConfigKey", 
                "F9", 
                "Reload Config Key",
                "Key to reload config from file without restarting");

            // ============ ADVANCED ============
            _targetDisplay = _prefCategory.CreateEntry(
                "TargetDisplay", 
                0, 
                "Target Display",
                "Which display to render to (0 = primary/desktop window)");
            
            _cameraDepth = _prefCategory.CreateEntry(
                "CameraDepth", 
                100f, 
                "Camera Depth",
                "Render priority (higher = renders on top of other cameras)");

            // Save defaults if file doesn't exist
            MelonPreferences.Save();
        }

        private KeyCode GetKeyCode(string keyName)
        {
            if (System.Enum.TryParse<KeyCode>(keyName, true, out KeyCode result))
            {
                return result;
            }
            LoggerInstance.Warning($"Invalid key '{keyName}', defaulting to F8");
            return KeyCode.F8;
        }

        private void ReloadConfig()
        {
            MelonPreferences.Load();
            LoggerInstance.Msg("Config reloaded from file!");
            
            // Apply changes to camera if it exists
            if (_spectatorCamera != null)
            {
                ApplyCameraSettings();
            }
            
            LoggerInstance.Msg($"  Distance: {_cameraDistance.Value}");
            LoggerInstance.Msg($"  Height: {_cameraHeight.Value}");
            LoggerInstance.Msg($"  FOV: {_fieldOfView.Value}");
            LoggerInstance.Msg($"  Position Smoothing: {_positionSmoothing.Value}");
        }

        private void ApplyCameraSettings()
        {
            if (_spectatorCamera == null) return;
            
            _spectatorCamera.fieldOfView = _fieldOfView.Value;
            _spectatorCamera.nearClipPlane = _nearClipPlane.Value;
            _spectatorCamera.farClipPlane = _farClipPlane.Value;
            _spectatorCamera.depth = _cameraDepth.Value;
            _spectatorCamera.targetDisplay = _targetDisplay.Value;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");
            
            // Reset state on scene change
            _initialized = false;
            _spectatorCamera = null;
            _spectatorCameraObj = null;
            _playerHead = null;
            _playerBody = null;
        }

        public override void OnUpdate()
        {
            // Toggle spectator camera
            if (Input.GetKeyDown(GetKeyCode(_toggleKey.Value)))
            {
                _isEnabled = !_isEnabled;
                LoggerInstance.Msg($"Spectator Camera: {(_isEnabled ? "ENABLED" : "DISABLED")}");
                
                if (_spectatorCameraObj != null)
                {
                    _spectatorCameraObj.SetActive(_isEnabled);
                }
            }
            
            // Reload config
            if (Input.GetKeyDown(GetKeyCode(_reloadConfigKey.Value)))
            {
                ReloadConfig();
            }
        }

        public override void OnLateUpdate()
        {
            if (!_initialized)
            {
                TryInitialize();
                return;
            }

            if (!_isEnabled || _spectatorCamera == null || _playerHead == null)
                return;

            UpdateSpectatorCamera();
        }

        private void TryInitialize()
        {
            // Find the VR camera / player head
            _playerHead = FindPlayerHead();
            if (_playerHead == null)
                return;

            // Find player body/root for rotation reference
            _playerBody = FindPlayerBody();

            // Create spectator camera
            CreateSpectatorCamera();

            _isEnabled = _enabledByDefault.Value;
            if (_spectatorCameraObj != null)
            {
                _spectatorCameraObj.SetActive(_isEnabled);
            }

            _initialized = true;
            LoggerInstance.Msg("Spectator Camera initialized successfully!");
            LoggerInstance.Msg($"  Player Head: {_playerHead.name}");
            LoggerInstance.Msg($"  Player Body: {(_playerBody != null ? _playerBody.name : "Using head")}");
        }

        private Transform FindPlayerHead()
        {
            // Try common VR camera names
            string[] headNames = new string[]
            {
                "CenterEyeAnchor",
                "Camera",
                "Main Camera",
                "VRCamera",
                "Head",
                "PlayerHead",
                "Eye",
                "CameraRig",
                "TrackingSpace"
            };

            // First, try to find by Camera component with specific tags
            Camera[] allCameras = Object.FindObjectsOfType<Camera>();
            foreach (var cam in allCameras)
            {
                if (cam.CompareTag("MainCamera") || cam.gameObject.name.ToLower().Contains("eye") || 
                    cam.gameObject.name.ToLower().Contains("head") || cam.gameObject.name.ToLower().Contains("vr"))
                {
                    LoggerInstance.Msg($"Found VR camera: {cam.gameObject.name}");
                    return cam.transform;
                }
            }

            // Try by name
            foreach (string name in headNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    LoggerInstance.Msg($"Found player head by name: {name}");
                    return obj.transform;
                }
            }

            // Last resort - find any active camera
            foreach (var cam in allCameras)
            {
                if (cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    LoggerInstance.Msg($"Using fallback camera: {cam.gameObject.name}");
                    return cam.transform;
                }
            }

            return null;
        }

        private Transform FindPlayerBody()
        {
            // Try to find XR rig or player root
            string[] bodyNames = new string[]
            {
                "OVRCameraRig",
                "XR Rig",
                "XRRig",
                "[CameraRig]",
                "Player",
                "PlayerController",
                "TrackingSpace"
            };

            foreach (string name in bodyNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    return obj.transform;
                }
            }

            // If no body found, we'll use head's parent or head itself
            if (_playerHead != null && _playerHead.parent != null)
            {
                return _playerHead.parent;
            }

            return _playerHead;
        }

        private void CreateSpectatorCamera()
        {
            // Create a new camera for the desktop mirror
            _spectatorCameraObj = new GameObject("SpectatorCamera");
            Object.DontDestroyOnLoad(_spectatorCameraObj);

            _spectatorCamera = _spectatorCameraObj.AddComponent<Camera>();
            
            // Copy settings from main camera if possible
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _spectatorCamera.clearFlags = mainCam.clearFlags;
                _spectatorCamera.backgroundColor = mainCam.backgroundColor;
                _spectatorCamera.cullingMask = mainCam.cullingMask;
            }

            // Apply config settings
            ApplyCameraSettings();
            
            // Set this camera to render to the desktop display
            _spectatorCamera.stereoTargetEye = StereoTargetEyeMask.None; // Not VR!

            // Initialize position
            if (_playerHead != null)
            {
                _currentCameraPosition = CalculateTargetPosition();
                _currentCameraRotation = CalculateTargetRotation();
                _spectatorCameraObj.transform.position = _currentCameraPosition;
                _spectatorCameraObj.transform.rotation = _currentCameraRotation;
            }

            LoggerInstance.Msg("Spectator camera created");
        }

        private void UpdateSpectatorCamera()
        {
            if (_playerHead == null || _spectatorCamera == null)
                return;

            Vector3 targetPosition = CalculateTargetPosition();
            Quaternion targetRotation = CalculateTargetRotation();

            // Smooth follow with separate position and rotation smoothing
            float posSmoothFactor = _positionSmoothing.Value * Time.deltaTime;
            float rotSmoothFactor = _rotationSmoothing.Value * Time.deltaTime;
            
            _currentCameraPosition = Vector3.Lerp(_currentCameraPosition, targetPosition, posSmoothFactor);
            _currentCameraRotation = Quaternion.Slerp(_currentCameraRotation, targetRotation, rotSmoothFactor);

            _spectatorCameraObj.transform.position = _currentCameraPosition;
            _spectatorCameraObj.transform.rotation = _currentCameraRotation;
        }

        private Vector3 CalculateTargetPosition()
        {
            // Choose reference based on config
            Transform reference;
            if (_followBodyRotation.Value && _playerBody != null)
            {
                reference = _playerBody;
            }
            else
            {
                reference = _playerHead;
            }
            
            // Get the forward direction
            Vector3 forward = reference.forward;
            
            // Lock vertical rotation if enabled
            if (_lockVerticalRotation.Value)
            {
                forward.y = 0;
                forward.Normalize();
            }
            
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // Position behind and above the player
            Vector3 headPos = _playerHead.position;
            Vector3 targetPos = headPos 
                - forward * _cameraDistance.Value 
                + Vector3.up * _cameraHeight.Value
                + right * _horizontalOffset.Value;

            return targetPos;
        }

        private Quaternion CalculateTargetRotation()
        {
            // Look at the player's head with configurable offset
            Vector3 lookTarget = _playerHead.position + Vector3.up * _lookAtHeightOffset.Value;
            Vector3 direction = lookTarget - _currentCameraPosition;
            
            if (direction.sqrMagnitude > 0.001f)
            {
                return Quaternion.LookRotation(direction);
            }
            
            return _spectatorCameraObj.transform.rotation;
        }

        public override void OnApplicationQuit()
        {
            // Save preferences
            MelonPreferences.Save();
        }
    }
}
