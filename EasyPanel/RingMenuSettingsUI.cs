using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板設置UI
	/// 讓玩家可以自定義按鍵和其他設置
	/// </summary>
	public class RingMenuSettingsUI : MonoBehaviour
	{
		[Header("UI 組件")]
		[SerializeField]
		[Tooltip("顯示當前按鍵的文字")]
		private Text currentKeyText;

		[SerializeField]
		[Tooltip("更改按鍵的按鈕")]
		private Button changeKeyButton;

		[SerializeField]
		[Tooltip("重置按鈕")]
		private Button resetButton;

		[SerializeField]
		[Tooltip("按住顯示的切換開關")]
		private Toggle holdToShowToggle;

		[SerializeField]
		[Tooltip("滑鼠確認的切換開關")]
		private Toggle mouseConfirmToggle;

		[SerializeField]
		[Tooltip("等待按鍵輸入的提示文字")]
		private Text waitingForKeyText;

		[SerializeField]
		[Tooltip("狀態提示文字")]
		private Text statusText;

		private RingMenuInputHandler inputHandler;
		private bool isWaitingForKey = false;

		private void Start()
		{
			// 查找 RingMenuInputHandler
			inputHandler = FindObjectOfType<RingMenuInputHandler>();
			if (inputHandler == null)
			{
				Debug.LogError("RingMenuSettingsUI: 找不到 RingMenuInputHandler");
				return;
			}

			// 綁定按鈕事件
			if (changeKeyButton != null)
			{
				changeKeyButton.onClick.AddListener(OnChangeKeyButtonClicked);
			}

			if (resetButton != null)
			{
				resetButton.onClick.AddListener(OnResetButtonClicked);
			}

			// 綁定切換開關事件
			if (holdToShowToggle != null)
			{
				holdToShowToggle.onValueChanged.AddListener(OnHoldToShowChanged);
			}

			if (mouseConfirmToggle != null)
			{
				mouseConfirmToggle.onValueChanged.AddListener(OnMouseConfirmChanged);
			}

			// 隱藏等待提示
			if (waitingForKeyText != null)
			{
				waitingForKeyText.gameObject.SetActive(false);
			}

			// 載入當前設置
			LoadCurrentSettings();
		}

		private void Update()
		{
			// 如果正在等待按鍵輸入
			if (isWaitingForKey)
			{
				HandleKeyInput();
			}
		}

		/// <summary>
		/// 載入當前設置並顯示
		/// </summary>
		private void LoadCurrentSettings()
		{
			if (inputHandler == null) return;

			RingMenuSettings settings = inputHandler.GetSettings();

			// 顯示當前按鍵
			if (currentKeyText != null)
			{
				currentKeyText.text = settings.OpenMenuKeyName.ToUpper();
			}

			// 設置切換開關
			if (holdToShowToggle != null)
			{
				holdToShowToggle.isOn = settings.HoldToShow;
			}

			if (mouseConfirmToggle != null)
			{
				mouseConfirmToggle.isOn = settings.UseMouseConfirm;
			}
		}

		/// <summary>
		/// 更改按鍵按鈕點擊
		/// </summary>
		private void OnChangeKeyButtonClicked()
		{
			isWaitingForKey = true;

			if (waitingForKeyText != null)
			{
				waitingForKeyText.gameObject.SetActive(true);
			}

			if (changeKeyButton != null)
			{
				changeKeyButton.interactable = false;
			}

			ShowStatus("請按下任意按鍵...", Color.yellow);
		}

		/// <summary>
		/// 處理按鍵輸入
		/// </summary>
		private void HandleKeyInput()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null) return;

			// 檢查所有按鍵
			foreach (Key key in System.Enum.GetValues(typeof(Key)))
			{
				if (keyboard[key].wasPressedThisFrame)
				{
					// 跳過一些不適合的按鍵
					if (IsValidKey(key))
					{
						SetNewKey(key);
					}
					else
					{
						ShowStatus($"按鍵 {key} 不可用，請選擇其他按鍵", Color.red);
					}
					break;
				}
			}
		}

		/// <summary>
		/// 設置新按鍵
		/// </summary>
		private void SetNewKey(Key key)
		{
			string keyName = key.ToString().ToLower();
			inputHandler.SetOpenMenuKey(keyName);

			// 更新顯示
			if (currentKeyText != null)
			{
				currentKeyText.text = keyName.ToUpper();
			}

			// 結束等待
			isWaitingForKey = false;

			if (waitingForKeyText != null)
			{
				waitingForKeyText.gameObject.SetActive(false);
			}

			if (changeKeyButton != null)
			{
				changeKeyButton.interactable = true;
			}

			ShowStatus($"快捷鍵已更改為: {keyName.ToUpper()}", Color.green);
		}

		/// <summary>
		/// 檢查按鍵是否有效
		/// </summary>
		private bool IsValidKey(Key key)
		{
			// 排除一些不適合的按鍵
			return key != Key.None &&
				   key != Key.Escape &&
				   key != Key.Enter &&
				   key != Key.NumpadEnter &&
				   key != Key.LeftCtrl &&
				   key != Key.RightCtrl &&
				   key != Key.LeftShift &&
				   key != Key.RightShift &&
				   key != Key.LeftAlt &&
				   key != Key.RightAlt;
		}

		/// <summary>
		/// 重置按鈕點擊
		/// </summary>
		private void OnResetButtonClicked()
		{
			RingMenuSettings settings = inputHandler.GetSettings();
			settings.ResetToDefault();

			LoadCurrentSettings();
			ShowStatus("已重置為默認設置 (Q 鍵)", Color.green);
		}

		/// <summary>
		/// 按住顯示設置改變
		/// </summary>
		private void OnHoldToShowChanged(bool value)
		{
			if (inputHandler != null)
			{
				inputHandler.SetHoldToShow(value);
				string mode = value ? "按住顯示" : "切換顯示";
				ShowStatus($"模式已更改為: {mode}", Color.green);
			}
		}

		/// <summary>
		/// 滑鼠確認設置改變
		/// </summary>
		private void OnMouseConfirmChanged(bool value)
		{
			if (inputHandler != null)
			{
				inputHandler.SetUseMouseConfirm(value);
				string status = value ? "啟用" : "禁用";
				ShowStatus($"滑鼠確認已{status}", Color.green);
			}
		}

		/// <summary>
		/// 顯示狀態訊息
		/// </summary>
		private void ShowStatus(string message, Color color)
		{
			if (statusText != null)
			{
				statusText.text = message;
				statusText.color = color;

				// 3秒後清除訊息
				CancelInvoke(nameof(ClearStatus));
				Invoke(nameof(ClearStatus), 3f);
			}
		}

		/// <summary>
		/// 清除狀態訊息
		/// </summary>
		private void ClearStatus()
		{
			if (statusText != null)
			{
				statusText.text = "";
			}
		}

		private void OnDestroy()
		{
			// 清理事件監聽
			if (changeKeyButton != null)
			{
				changeKeyButton.onClick.RemoveListener(OnChangeKeyButtonClicked);
			}

			if (resetButton != null)
			{
				resetButton.onClick.RemoveListener(OnResetButtonClicked);
			}

			if (holdToShowToggle != null)
			{
				holdToShowToggle.onValueChanged.RemoveListener(OnHoldToShowChanged);
			}

			if (mouseConfirmToggle != null)
			{
				mouseConfirmToggle.onValueChanged.RemoveListener(OnMouseConfirmChanged);
			}
		}
	}
}
