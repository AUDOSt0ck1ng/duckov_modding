using UnityEngine;

namespace EasyPanel
{
	/// <summary>
	/// 環形面板配置
	/// 可以創建為 ScriptableObject 資源文件來調整設置
	/// </summary>
	[CreateAssetMenu(fileName = "RingMenuConfig", menuName = "EasyPanel/Ring Menu Config")]
	public class RingMenuConfig : ScriptableObject
	{
		[Header("環形佈局")]
		[Tooltip("環的半徑（像素）")]
		public float ringRadius = 200f;

		[Tooltip("最大顯示的道具數量")]
		[Range(4, 12)]
		public int maxItems = 8;

		[Tooltip("起始角度（度數，0 = 右側，90 = 上方）")]
		public float startAngle = 90f;

		[Tooltip("是否順時針排列")]
		public bool clockwise = false;

		[Header("視覺效果")]
		[Tooltip("選中時的縮放比例")]
		[Range(1.0f, 2.0f)]
		public float selectedScale = 1.2f;

		[Tooltip("選中時的顏色")]
		public Color selectedColor = new Color(1f, 0.9f, 0.3f, 1f);

		[Tooltip("未選中時的顏色")]
		public Color normalColor = Color.white;

		[Tooltip("背景透明度")]
		[Range(0f, 1f)]
		public float backgroundAlpha = 0.8f;

		[Header("動畫")]
		[Tooltip("打開動畫時間（秒）")]
		public float openAnimationDuration = 0.2f;

		[Tooltip("關閉動畫時間（秒）")]
		public float closeAnimationDuration = 0.15f;

		[Tooltip("選項淡入延遲（秒）")]
		public float itemFadeDelay = 0.05f;

		[Header("輸入設置")]
		[Tooltip("是否需要按住才顯示")]
		public bool holdToShow = true;

		[Tooltip("使用滑鼠左鍵確認選擇")]
		public bool useMouseConfirm = true;

		[Tooltip("選擇後自動關閉")]
		public bool autoCloseOnSelect = true;

		[Header("音效")]
		[Tooltip("打開面板音效")]
		public AudioClip openSound;

		[Tooltip("選擇項目音效")]
		public AudioClip selectSound;

		[Tooltip("確認選擇音效")]
		public AudioClip confirmSound;

		[Tooltip("關閉面板音效")]
		public AudioClip closeSound;
	}
}
