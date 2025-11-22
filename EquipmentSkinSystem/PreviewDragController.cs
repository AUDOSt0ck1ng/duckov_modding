using UnityEngine;
using UnityEngine.EventSystems;

namespace EquipmentSkinSystem
{
    /// <summary>
    /// 預覽視窗拖曳控制器
    /// 處理滑鼠拖曳來旋轉預覽相機，以及滾輪調整高度
    /// </summary>
    public class PreviewDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
    {
        private CharacterPreview? _characterPreview;
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;
        
        /// <summary>
        /// 設置預覽系統引用
        /// </summary>
        public void SetCharacterPreview(CharacterPreview preview)
        {
            _characterPreview = preview;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _lastMousePosition = eventData.position;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _characterPreview == null)
                return;
            
            // 計算滑鼠移動距離
            Vector2 deltaPosition = eventData.position - _lastMousePosition;
            
            // 轉換為相機旋轉
            // 注意：Y軸反轉（因為UI座標系Y向下，但我們希望向上拖曳時相機向上）
            // X軸反轉：對調左右拖曳方向
            float deltaX = -deltaPosition.x; // 水平拖曳：左右旋轉（反轉以對調方向）
            float deltaY = -deltaPosition.y; // 垂直拖曳：上下旋轉（反轉）
            
            // 應用旋轉
            _characterPreview.RotateCamera(deltaX, deltaY);
            
            // 更新最後位置
            _lastMousePosition = eventData.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }
        
        /// <summary>
        /// 處理滑鼠滾輪事件（調整相機距離/縮放）
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            if (_characterPreview == null)
                return;
            
            // Unity 的 scrollDelta.y 通常是 0.1 或 -0.1（每格滾動）
            // 但可能因系統而異，我們需要處理各種情況
            float scrollDelta = eventData.scrollDelta.y;
            
            // 滾輪向上（scrollDelta > 0）：拉近（距離減小）
            // 滾輪向下（scrollDelta < 0）：拉遠（距離增大）
            // 反轉符號，因為向上滾動應該拉近（距離減小）
            _characterPreview.AdjustCameraDistance(-scrollDelta);
        }
    }
}

