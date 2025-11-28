using System;
using HarmonyLib;
using UnityEngine;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 角色預覽系統
    /// 使用額外相機拍攝角色正面並顯示在 UI 中
    /// </summary>
    public class CharacterPreview : MonoBehaviour
    {
        private Camera? _previewCamera;
        private RenderTexture? _renderTexture;
        private GameObject? _previewCharacter;
        private CharacterMainControl? _originalCharacter;
        private CharacterEquipmentController? _previewEquipmentController;

        // 預覽相機設置
        private const int PREVIEW_WIDTH = 512;
        private const int PREVIEW_HEIGHT = 512;
        private const float DEFAULT_CAMERA_DISTANCE = 3.0f;
        private const float DEFAULT_CAMERA_HEIGHT = 0.6f; // 降低預設高度（讓預設值在範圍中間偏下）
        private const float CAMERA_FOV = 45f;

        // 相機高度控制（通過拖曳條）
        private float _cameraHeight = DEFAULT_CAMERA_HEIGHT;
        private const float MIN_CAMERA_HEIGHT = 0.2f; // 最小高度（降低）
        private const float MAX_CAMERA_HEIGHT = 1.5f; // 最大高度（降低，讓範圍更合理）

        // 相機距離控制（縮放，通過滾輪）- 使用離散等級
        private enum CameraDistanceLevel
        {
            Far = 0,    // 遠
            Medium = 1, // 中
            Near = 2    // 近
        }

        private CameraDistanceLevel _cameraDistanceLevel = CameraDistanceLevel.Medium;
        private const float CAMERA_DISTANCE_FAR = 5.0f;    // 遠距離
        private const float CAMERA_DISTANCE_MEDIUM = 3.0f; // 中距離（預設）
        private const float CAMERA_DISTANCE_NEAR = 1.8f;   // 近距離

        private float _cameraDistance = CAMERA_DISTANCE_MEDIUM;

        // 相機旋轉控制
        private float _cameraRotationY = 0f; // 水平旋轉（繞Y軸）
        private float _cameraRotationX = 0f; // 垂直旋轉（繞X軸）
        private const float ROTATION_SENSITIVITY = 0.5f; // 旋轉靈敏度（降低以減慢旋轉速度）
        private const float MIN_VERTICAL_ANGLE = -60f; // 最小垂直角度
        private const float MAX_VERTICAL_ANGLE = 60f; // 最大垂直角度

        /// <summary>
        /// 預覽 RenderTexture（供 UI 使用）
        /// </summary>
        public RenderTexture? PreviewTexture => _renderTexture;

        /// <summary>
        /// 初始化預覽系統
        /// </summary>
        public void Initialize()
        {
            try
            {
                // 創建 RenderTexture
                _renderTexture = new RenderTexture(PREVIEW_WIDTH, PREVIEW_HEIGHT, 24, RenderTextureFormat.ARGB32);
                _renderTexture.name = "CharacterPreviewRT";
                _renderTexture.antiAliasing = 2; // 2x MSAA
                _renderTexture.filterMode = FilterMode.Bilinear;

                // 創建預覽相機
                CreatePreviewCamera();

                Logger.Info("Character preview system initialized");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize character preview", e);
            }
        }

        /// <summary>
        /// 創建預覽相機
        /// </summary>
        private void CreatePreviewCamera()
        {
            try
            {
                // 創建相機 GameObject
                GameObject cameraObj = new GameObject("CharacterPreviewCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;

                // 設置相機組件
                _previewCamera = cameraObj.AddComponent<Camera>();
                _previewCamera.targetTexture = _renderTexture;
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f); // 深藍灰色背景
                _previewCamera.fieldOfView = CAMERA_FOV;
                _previewCamera.nearClipPlane = 0.1f;
                _previewCamera.farClipPlane = 10f;
                _previewCamera.cullingMask = 0; // 初始不渲染任何層
                _previewCamera.enabled = false; // 預設關閉，需要時才啟用

                // 設置相機位置（面向角色正面）
                // 相機會在角色前方，稍微向上傾斜
                cameraObj.transform.position = new Vector3(0, DEFAULT_CAMERA_HEIGHT, -CAMERA_DISTANCE_MEDIUM);
                cameraObj.transform.LookAt(new Vector3(0, DEFAULT_CAMERA_HEIGHT, 0));

                Logger.Debug("Preview camera created");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create preview camera", e);
            }
        }

        /// <summary>
        /// 設置要預覽的角色
        /// </summary>
        /// <param name="characterType">角色類型</param>
        /// <param name="resetCamera">是否重置相機（切換角色時為true，改裝備時為false）</param>
        public void SetPreviewCharacter(CharacterType characterType, bool resetCamera = true)
        {
            try
            {
                Logger.Debug($"SetPreviewCharacter called with type: {characterType}, resetCamera: {resetCamera}");

                // 取得目標角色
                CharacterMainControl? targetCharacter = null;
                if (characterType == CharacterType.Player)
                {
                    targetCharacter = LevelManager.Instance?.MainCharacter;
                    Logger.Debug($"Looking for Player character, found: {targetCharacter != null}");
                }
                else if (characterType == CharacterType.Pet)
                {
                    targetCharacter = LevelManager.Instance?.PetCharacter;
                    Logger.Debug($"Looking for Pet character, found: {targetCharacter != null}");
                }

                if (targetCharacter == null)
                {
                    Logger.Error($"Cannot find {characterType} character for preview - this should not happen!");
                    DisablePreview();
                    return;
                }

                // 檢查是否切換了角色（需要重置相機）
                bool characterChanged = _originalCharacter != targetCharacter;

                // 如果切換了角色，清理舊的預覽角色
                if (characterChanged)
                {
                    CleanupPreviewCharacter();
                }

                _originalCharacter = targetCharacter;

                // 創建預覽角色的複製（或使用原角色）
                // 注意：這裡我們直接使用原角色，但將相機設置為只渲染特定層
                SetupPreviewForCharacter(targetCharacter, resetCamera && characterChanged);

                Logger.Debug($"Preview set for {characterType} character successfully");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set preview character", e);
            }
        }

        /// <summary>
        /// 為角色設置預覽
        /// </summary>
        /// <param name="character">角色</param>
        /// <param name="resetCamera">是否重置相機</param>
        private void SetupPreviewForCharacter(CharacterMainControl character, bool resetCamera = true)
        {
            try
            {
                if (_previewCamera == null)
                {
                    Logger.Warning("Preview camera is null");
                    return;
                }

                // 取得角色的裝備控制器
                _previewEquipmentController = character.GetComponent<CharacterEquipmentController>();
                if (_previewEquipmentController == null)
                {
                    Logger.Warning("Character has no CharacterEquipmentController");
                    return;
                }

                // 如果需要重置相機，重置旋轉角度、高度和距離
                if (resetCamera)
                {
                    _cameraRotationY = 0f;
                    _cameraRotationX = 0f;
                    _cameraHeight = DEFAULT_CAMERA_HEIGHT;
                    _cameraDistanceLevel = CameraDistanceLevel.Medium; // 重置為中距離
                    _cameraDistance = CAMERA_DISTANCE_MEDIUM;
                }

                // 設置相機位置（即使不重置，也要更新位置以跟隨角色）
                UpdateCameraPosition();

                // 設置相機渲染層（渲染所有層，但我們可以過濾）
                // 注意：Unity 的 Layer 系統，我們需要確保角色在可渲染的層上
                _previewCamera.cullingMask = -1; // 渲染所有層

                // 啟用相機
                _previewCamera.enabled = true;

                Logger.Debug("Preview camera positioned and enabled");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to setup preview for character", e);
            }
        }

        /// <summary>
        /// 更新預覽（每幀調用）
        /// </summary>
        public void UpdatePreview()
        {
            try
            {
                if (_previewCamera == null || !_previewCamera.enabled)
                    return;

                // 如果角色存在，更新相機位置
                if (_originalCharacter != null)
                {
                    UpdateCameraPosition();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error updating preview", e);
            }
        }

        /// <summary>
        /// 根據旋轉角度更新相機位置
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (_previewCamera == null || _originalCharacter == null)
                return;

            Vector3 characterPosition = _originalCharacter.transform.position;
            Quaternion characterRotation = _originalCharacter.transform.rotation;

            // 計算相機的球面座標位置
            // 使用球面座標系統：距離、水平角、垂直角
            float radY = _cameraRotationY * Mathf.Deg2Rad;
            float radX = _cameraRotationX * Mathf.Deg2Rad;

            // 計算相機在世界空間中的方向
            Vector3 forward = characterRotation * Vector3.forward;
            Vector3 right = characterRotation * Vector3.right;
            Vector3 up = characterRotation * Vector3.up;

            // 計算相機偏移（使用球面座標）
            float cosX = Mathf.Cos(radX);
            float sinX = Mathf.Sin(radX);
            float cosY = Mathf.Cos(radY);
            float sinY = Mathf.Sin(radY);

            // 相機位置：圍繞角色旋轉
            Vector3 cameraOffset =
                forward * (cosX * cosY * -_cameraDistance) +
                right * (cosX * sinY * _cameraDistance) +
                up * (sinX * _cameraDistance + _cameraHeight);

            Vector3 cameraPosition = characterPosition + cameraOffset;
            _previewCamera.transform.position = cameraPosition;

            // 相機看向角色中心（使用當前高度）
            Vector3 lookTarget = characterPosition + up * _cameraHeight;
            _previewCamera.transform.LookAt(lookTarget);
        }

        /// <summary>
        /// 旋轉相機（由滑鼠拖曳調用）
        /// </summary>
        public void RotateCamera(float deltaX, float deltaY)
        {
            // deltaX: 水平拖曳（左右）
            // deltaY: 垂直拖曳（上下）

            // 水平旋轉（繞角色的Y軸）
            _cameraRotationY += deltaX * ROTATION_SENSITIVITY;

            // 垂直旋轉（限制角度）
            _cameraRotationX += deltaY * ROTATION_SENSITIVITY;
            _cameraRotationX = Mathf.Clamp(_cameraRotationX, MIN_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);

            // 立即更新相機位置
            UpdateCameraPosition();
        }

        /// <summary>
        /// 設置相機高度（由拖曳條調用）
        /// </summary>
        public void SetCameraHeight(float height)
        {
            _cameraHeight = Mathf.Clamp(height, MIN_CAMERA_HEIGHT, MAX_CAMERA_HEIGHT);
            UpdateCameraPosition();
        }

        /// <summary>
        /// 獲取當前相機高度（用於拖曳條）
        /// </summary>
        public float GetCameraHeight()
        {
            return _cameraHeight;
        }

        /// <summary>
        /// 獲取高度範圍（用於拖曳條）
        /// </summary>
        public void GetCameraHeightRange(out float min, out float max)
        {
            min = MIN_CAMERA_HEIGHT;
            max = MAX_CAMERA_HEIGHT;
        }

        /// <summary>
        /// 調整相機距離（縮放，由滾輪調用）- 使用離散等級
        /// </summary>
        public void AdjustCameraDistance(float delta)
        {
            // delta > 0: 滾輪向上（拉遠，從近→中→遠）
            // delta < 0: 滾輪向下（拉近，從遠→中→近）

            if (delta > 0)
            {
                // 滾輪向上：拉遠（等級減少：近→中→遠）
                if (_cameraDistanceLevel > CameraDistanceLevel.Far)
                {
                    _cameraDistanceLevel--;
                }
            }
            else if (delta < 0)
            {
                // 滾輪向下：拉近（等級增加：遠→中→近）
                if (_cameraDistanceLevel < CameraDistanceLevel.Near)
                {
                    _cameraDistanceLevel++;
                }
            }

            // 根據等級設置距離
            switch (_cameraDistanceLevel)
            {
                case CameraDistanceLevel.Far:
                    _cameraDistance = CAMERA_DISTANCE_FAR;
                    break;
                case CameraDistanceLevel.Medium:
                    _cameraDistance = CAMERA_DISTANCE_MEDIUM;
                    break;
                case CameraDistanceLevel.Near:
                    _cameraDistance = CAMERA_DISTANCE_NEAR;
                    break;
            }

            Logger.Debug($"Camera zoom level: {_cameraDistanceLevel} (distance: {_cameraDistance:F2})");

            UpdateCameraPosition();
        }

        /// <summary>
        /// 重置相機距離到預設值（中距離）
        /// </summary>
        public void ResetCameraDistance()
        {
            _cameraDistanceLevel = CameraDistanceLevel.Medium;
            _cameraDistance = CAMERA_DISTANCE_MEDIUM;
            UpdateCameraPosition();
        }

        /// <summary>
        /// 重置相機旋轉、高度和距離到預設位置
        /// </summary>
        public void ResetCameraRotation()
        {
            _cameraRotationY = 0f;
            _cameraRotationX = 0f;
            _cameraHeight = DEFAULT_CAMERA_HEIGHT;
            _cameraDistanceLevel = CameraDistanceLevel.Medium;
            _cameraDistance = CAMERA_DISTANCE_MEDIUM;
            UpdateCameraPosition();
        }

        /// <summary>
        /// 啟用預覽
        /// </summary>
        public void EnablePreview()
        {
            if (_previewCamera != null)
            {
                _previewCamera.enabled = true;
                Logger.Debug("Preview enabled");
            }
        }

        /// <summary>
        /// 停用預覽
        /// </summary>
        public void DisablePreview()
        {
            if (_previewCamera != null)
            {
                _previewCamera.enabled = false;
                Logger.Debug("Preview disabled");
            }
        }

        /// <summary>
        /// 清理預覽角色
        /// </summary>
        private void CleanupPreviewCharacter()
        {
            if (_previewCharacter != null)
            {
                Destroy(_previewCharacter);
                _previewCharacter = null;
            }

            _originalCharacter = null;
            _previewEquipmentController = null;
        }

        /// <summary>
        /// 清理資源
        /// </summary>
        private void OnDestroy()
        {
            CleanupPreviewCharacter();

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_previewCamera != null)
            {
                Destroy(_previewCamera.gameObject);
                _previewCamera = null;
            }

            Logger.Debug("Character preview cleaned up");
        }

        void Update()
        {
            // 每幀更新預覽（如果需要）
            if (_previewCamera != null && _previewCamera.enabled)
            {
                UpdatePreview();
            }
        }
    }
}
