# 開發須知 / Prompt for Future Work

以下規範請在每次進行開發或維護本模組時優先確認，避免重複踩雷：

1. **Harmony 補丁與版本限制**
   - **必須使用 Harmony 2.2.2 版本**：遊戲的 Unity Mono 只包含 `mscorlib.dll`，不支援 `System.Runtime.dll`。Harmony 2.3+ 會引用 `System.Runtime` 導致 typeref 解析失敗，遊戲載入 DLL 時直接崩潰。
   - 在 `csproj` 中指定：`<PackageReference Include="Lib.Harmony" Version="2.2.2" />`
   - 使用 `net48` 版本的 `0Harmony.dll`（約 910KB），從 NuGet 快取複製：`~/.nuget/packages/lib.harmony/2.2.2/lib/net48/0Harmony.dll`
   - 避免直接使用 `System.Reflection.MethodInfo`、`Assembly.GetExecutingAssembly()` 等 API，改用 Harmony 的 `Traverse` 或 `AccessTools`。
   - 使用 `CreateClassProcessor` 方式套用補丁，避免依賴 `Assembly` 型別。
   - 補丁掛載後務必記錄日誌，至少包含成功訊息與被 patch 的方法列表。
   - **驗證指標**：編譯後 `0Harmony.dll` 應為 ~910KB；若為 2.3MB 代表用錯版本。

2. **設定載入／持久化**
   - 任何資料結構（例如 `SlotConfigsList`）一律以 **手動序列化／反序列化** 實作，避免依賴 Unity `JsonUtility` 在 `Dictionary`、`List` 方面的限制。
   - 載入時一定要檢查資料筆數與欄位正確性，必要時重建字典並寫回檔案。

3. **功能調整流程**
   - **優先參考 `/workspace/decompiled/` 反編譯資料**：確認遊戲 API 簽名、私有欄位名稱、可用方法。
   - 實作前先調查官方或既有實作是否已有推薦做法，禁止「為了省時間」自行嘗試未驗證的 API。
   - 每次變更需完成：編譯、覆蓋 DLL、啟動遊戲驗證（含載入設定與實際穿脫裝備測試）、收集日誌。
   - 遇到 typeref 或 runtime 錯誤時，優先檢查依賴函式庫版本，而非程式碼本身。

4. **日誌與除錯**
   - 關鍵流程（載入設定、保存設定、Harmony patch、即時渲染）必須寫出清楚日誌並附 Slot/ID 等資訊，方便快速對照是否有進度。

請將以上內容視為「內建 prompt」，任何新指示都需要符合此規範後再執行。若遇到新坑，務必更新本文件。記得：**寧可多花 5 分鐘查文件，也不要亂試造成整體功能倒退。**

