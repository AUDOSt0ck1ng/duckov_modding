using UnityEngine;
using Duckov.Modding;

namespace EasyPanel
{
	/// <summary>
	/// EasyPanel Mod 入口點
	/// 遊戲載入 Mod 時會創建此類的實例
	/// </summary>
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		private GameObject ringMenuPanelObject;
		private GameObject inputHandlerManagerObject;

		protected override void OnAfterSetup()
		{
			base.OnAfterSetup();
			Debug.Log("[EasyPanel] Mod 已載入");

			// 初始化環形面板系統
			InitializeRingMenu();
		}

		protected override void OnBeforeDeactivate()
		{
			base.OnBeforeDeactivate();
			Debug.Log("[EasyPanel] Mod 正在卸載");

			// 清理資源
			CleanupRingMenu();
		}

		/// <summary>
		/// 初始化環形面板
		/// </summary>
		private void InitializeRingMenu()
		{
			// 創建 Canvas
			ringMenuPanelObject = new GameObject("RingMenuCanvas");
			DontDestroyOnLoad(ringMenuPanelObject);

			// 添加 Canvas 組件
			var canvas = ringMenuPanelObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 1000; // 確保在最上層

			// 添加 CanvasScaler
			var scaler = ringMenuPanelObject.AddComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);

			// 添加 GraphicRaycaster
			ringMenuPanelObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

			// 創建面板容器
			GameObject panelContainer = new GameObject("Panel");
			panelContainer.transform.SetParent(ringMenuPanelObject.transform, false);

			var rectTransform = panelContainer.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.sizeDelta = Vector2.zero;

			// 添加 RingMenuPanel 組件
			var panel = panelContainer.AddComponent<RingMenuPanel>();
			panel.InitializeUI();

			Debug.Log("[EasyPanel] 環形面板已創建");

			// 創建輸入處理器管理物件（不會被場景切換銷毀）
			inputHandlerManagerObject = new GameObject("EasyPanelInputManager");
			DontDestroyOnLoad(inputHandlerManagerObject);
			inputHandlerManagerObject.AddComponent<InputHandlerManager>();

			Debug.Log("[EasyPanel] 輸入處理器管理器已創建");
		}

		/// <summary>
		/// 清理環形面板資源
		/// </summary>
		private void CleanupRingMenu()
		{
			if (ringMenuPanelObject != null)
			{
				Destroy(ringMenuPanelObject);
				Debug.Log("[EasyPanel] 環形面板已清理");
			}

			if (inputHandlerManagerObject != null)
			{
				Destroy(inputHandlerManagerObject);
				Debug.Log("[EasyPanel] 輸入處理器管理器已清理");
			}
		}
	}
}
