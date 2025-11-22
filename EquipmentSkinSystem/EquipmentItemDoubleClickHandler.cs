using UnityEngine;
using UnityEngine.EventSystems;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 裝備項目雙擊檢測處理器
    /// </summary>
    public class EquipmentItemDoubleClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private int _typeID;
        private SkinManagerUI? _uiManager;
        private float _lastClickTime = 0f;
        private const float DOUBLE_CLICK_TIME = 0.3f; // 雙擊時間間隔（秒）

        public void Initialize(int typeID, SkinManagerUI uiManager)
        {
            _typeID = typeID;
            _uiManager = uiManager;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_uiManager == null) return;

            float currentTime = Time.time;
            
            // 檢查是否為雙擊（在時間間隔內點擊兩次）
            if (currentTime - _lastClickTime < DOUBLE_CLICK_TIME)
            {
                // 雙擊檢測成功
                _uiManager.OnEquipmentItemDoubleClicked(_typeID);
                _lastClickTime = 0f; // 重置，避免三擊觸發
            }
            else
            {
                // 記錄第一次點擊時間
                _lastClickTime = currentTime;
            }
        }
    }
}

