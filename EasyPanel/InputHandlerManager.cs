using UnityEngine;

namespace EasyPanel
{
	/// <summary>
	/// 輸入處理器管理器
	/// 負責監控玩家角色變化，並在角色重新生成時自動附加輸入處理器
	/// </summary>
	public class InputHandlerManager : MonoBehaviour
	{
		private CharacterMainControl lastPlayerReference;
		private RingMenuInputHandler currentHandler;

		private void Update()
		{
			// 持續檢查玩家角色是否變化（場景切換或重生）
			CheckAndAttachInputHandler();
		}

		/// <summary>
		/// 檢查並附加輸入處理器
		/// </summary>
		private void CheckAndAttachInputHandler()
		{
			// 獲取當前的主角控制器
			CharacterMainControl currentPlayer = CharacterMainControl.Main;

			// 如果沒有玩家，清理舊的引用
			if (currentPlayer == null)
			{
				if (lastPlayerReference != null)
				{
					Debug.Log("[EasyPanel] 玩家角色已消失，清理引用");
					lastPlayerReference = null;
					currentHandler = null;
				}
				return;
			}

			// 如果是新的玩家實例（場景切換或重生）
			if (currentPlayer != lastPlayerReference)
			{
				Debug.Log("[EasyPanel] 偵測到新的玩家角色，準備附加輸入處理器");

				// 移除舊的處理器（如果存在）
				if (lastPlayerReference != null && currentHandler != null)
				{
					Destroy(currentHandler);
					Debug.Log("[EasyPanel] 已移除舊的輸入處理器");
				}

				// 在新的玩家角色上添加輸入處理器
				GameObject playerObject = currentPlayer.gameObject;

				// 檢查是否已經有處理器（避免重複添加）
				currentHandler = playerObject.GetComponent<RingMenuInputHandler>();
				if (currentHandler == null)
				{
					currentHandler = playerObject.AddComponent<RingMenuInputHandler>();
					Debug.Log("[EasyPanel] 輸入處理器已附加到新的玩家角色");
				}
				else
				{
					Debug.Log("[EasyPanel] 玩家角色已有輸入處理器");
				}

				// 更新引用
				lastPlayerReference = currentPlayer;
			}
		}

		private void OnDestroy()
		{
			// 清理時移除處理器
			if (currentHandler != null)
			{
				Destroy(currentHandler);
				Debug.Log("[EasyPanel] InputHandlerManager 被銷毀，已移除輸入處理器");
			}
		}
	}
}
