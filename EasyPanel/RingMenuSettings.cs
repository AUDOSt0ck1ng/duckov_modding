using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板設置
	/// 負責保存和載入用戶自定義的按鍵設置
	/// </summary>
	[System.Serializable]
	public class RingMenuSettings
	{
		private const string SETTINGS_KEY = "EasyPanel_RingMenuSettings";
		private const string DEFAULT_KEY = "q";

		[SerializeField]
		private string openMenuKeyName = DEFAULT_KEY;

		[SerializeField]
		private bool holdToShow = true;

		[SerializeField]
		private bool useMouseConfirm = true;

		[SerializeField]
		private float selectionRadius = 200f; // 選取半徑

		/// <summary>
		/// 打開面板的按鍵名稱
		/// </summary>
		public string OpenMenuKeyName
		{
			get => openMenuKeyName;
			set
			{
				openMenuKeyName = value;
				Save();
			}
		}

		/// <summary>
		/// 是否需要按住才顯示
		/// </summary>
		public bool HoldToShow
		{
			get => holdToShow;
			set
			{
				holdToShow = value;
				Save();
			}
		}

		/// <summary>
		/// 使用滑鼠左鍵確認選擇
		/// </summary>
		public bool UseMouseConfirm
		{
			get => useMouseConfirm;
			set
			{
				useMouseConfirm = value;
				Save();
			}
		}

		/// <summary>
		/// 選取半徑
		/// </summary>
		public float SelectionRadius
		{
			get => selectionRadius;
			set
			{
				selectionRadius = Mathf.Clamp(value, 100f, 400f); // 限制在 100-400 之間
				Save();
			}
		}

		/// <summary>
		/// 獲取當前的按鍵（轉換為 Key 枚舉）
		/// </summary>
		public Key GetOpenMenuKey()
		{
			if (System.Enum.TryParse<Key>(openMenuKeyName, true, out Key key))
			{
				return key;
			}
			return Key.Q; // 默認 Q 鍵
		}

		/// <summary>
		/// 保存設置到 PlayerPrefs
		/// </summary>
		public void Save()
		{
			string json = UnityEngine.JsonUtility.ToJson(this);
			PlayerPrefs.SetString(SETTINGS_KEY, json);
			PlayerPrefs.Save();
			Debug.Log($"環形面板設置已保存: {openMenuKeyName}");
		}

		/// <summary>
		/// 從 PlayerPrefs 載入設置
		/// </summary>
		public static RingMenuSettings Load()
		{
			if (PlayerPrefs.HasKey(SETTINGS_KEY))
			{
				string json = PlayerPrefs.GetString(SETTINGS_KEY);
				try
				{
					RingMenuSettings settings = UnityEngine.JsonUtility.FromJson<RingMenuSettings>(json);
					Debug.Log($"環形面板設置已載入: {settings.openMenuKeyName}");
					return settings;
				}
				catch (System.Exception e)
				{
					Debug.LogWarning($"載入環形面板設置失敗: {e.Message}，使用默認設置");
					return CreateDefault();
				}
			}
			return CreateDefault();
		}

		/// <summary>
		/// 創建默認設置
		/// </summary>
		public static RingMenuSettings CreateDefault()
		{
			RingMenuSettings settings = new RingMenuSettings
			{
				openMenuKeyName = DEFAULT_KEY,
				holdToShow = true,
				useMouseConfirm = true,
				selectionRadius = 200f
			};
			settings.Save();
			return settings;
		}

		/// <summary>
		/// 重置為默認設置
		/// </summary>
		public void ResetToDefault()
		{
			openMenuKeyName = DEFAULT_KEY;
			holdToShow = true;
			useMouseConfirm = true;
			selectionRadius = 200f;
			Save();
		}

		/// <summary>
		/// 驗證按鍵名稱是否有效
		/// </summary>
		public static bool IsValidKeyName(string keyName)
		{
			return System.Enum.TryParse<Key>(keyName, true, out _);
		}
	}
}
