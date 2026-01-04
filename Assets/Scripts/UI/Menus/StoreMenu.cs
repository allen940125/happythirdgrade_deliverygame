using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public class StoreMenu : BasePanel
    {
        #region === 1. 面板與分頁導航 ===
        [Header("=== 1. 面板與分頁導航 ===")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        
        [Tooltip("請依照順序放入面板")]
        [SerializeField] private GameObject[] subPanels;
        private int _currentPanelIndex = 0; 
        #endregion

        #region === 2. 車輛升級控制 ===
        [Header("=== 2. 車輛升級控制 ===")]
        [SerializeField] private Button btn_UpgradeCar_Gold;     
        [SerializeField] private Button btn_UpgradeCar_Voucher;  
        [SerializeField] private int upgradeCost_GoldBase = 500; 
        [SerializeField] private int upgradeCost_VoucherFixed = 1;
        [SerializeField] private int voucherItemId = 999;
        [SerializeField] private TMP_Text levelText; 
        [SerializeField] private TMP_Text upgradeInfoText; 
        [SerializeField] private TMP_Text upgradeIEXnfoText; 
        #endregion

        #region === 3. 商店列表與雜項 ===
        [Header("=== 3. 商店列表與雜項 ===")]
        [SerializeField] TMP_Text textMoneyValue;
        [SerializeField] TMP_Text textVoucherValue;
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TMP_Text selectedItemName;
        [SerializeField] private TMP_Text selectedItemDescription;
        [SerializeField] GameObject prefabSlotStoreItem;
        [SerializeField] GameObject scrollViewContentStoreItemList;
        [SerializeField] private Button equipmentTabButton; 
        [SerializeField] private Button consumableTabButton; 
        [SerializeField] private Button materialTabButton; 
        [SerializeField] private Button keyItemTabButton;
        #endregion

        #region === 4. 點卷購買 (模擬儲值) ===
        [Header("=== 4. 點卷購買 (作業用) ===")]
        [SerializeField] private Button[] buyVoucherButtons; 
        [SerializeField] private int[] buyVoucherAmounts;
        #endregion

        #region === 5. 角色購買與切換 (新功能) ===
        [Header("=== 5. 角色系統 ===")]
        [Header("角色 1 設定 (預設擁有)")]
        [SerializeField] private Button btn_Char1;         // 角色1 按鈕
        [SerializeField] private TMP_Text txt_Char1_Info;  // 角色1 按鈕文字

        [Header("角色 2 設定 (需購買)")]
        [SerializeField] private Button btn_Char2;         // 角色2 按鈕
        [SerializeField] private TMP_Text txt_Char2_Info;  // 角色2 按鈕文字
        [SerializeField] private int char2_Price = 10000;  // 角色2 價格 (金幣)
        
        [Header("角色 3 設定 (需購買)")]
        [SerializeField] private Button btn_Char3;         // 角色2 按鈕
        [SerializeField] private TMP_Text txt_Char3_Info;  // 角色2 按鈕文字
        [SerializeField] private int char3_EXPrice = 5;  // 角色2 價格 (金幣)
        
        #endregion

        protected override void Awake()
        {
            base.Awake();
            InitializeButtons();
            
            //GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = true});  
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnStoreItemClickedEvent, OnStoreItemClickedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPurchaseItemClickedEvent, OnPurchaseItemClickedEvent);
        }

        protected override void Start()
        {
            base.Start();
            UpdateCurrencyUI();
            UpdateUpgradeInfoText();
            UpdateSubPanelState();   
            Update_StoreItemDataInGrid();
            
            // ★ 啟動時刷新角色按鈕狀態
            UpdateCharacterUI(); 
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameManager.Instance.MainGameEvent.Unsubscribe<StoreItemClickedEvent>(OnStoreItemClickedEvent);
            GameManager.Instance.MainGameEvent.Unsubscribe<PurchaseItemClickedEvent>(OnPurchaseItemClickedEvent);
            EventSystem.current.SetSelectedGameObject(null);
        }

        #region === 初始化按鈕 ===

        void InitializeButtons()
        {
            if(closeButton) closeButton.onClick.AddListener(OnCloseButtonClicked);
            if(btn_UpgradeCar_Gold)    btn_UpgradeCar_Gold.onClick.AddListener(On_UpgradeCar_Gold_Clicked);
            if(btn_UpgradeCar_Voucher) btn_UpgradeCar_Voucher.onClick.AddListener(On_UpgradeCar_Voucher_Clicked);

            if(leftArrowButton)  leftArrowButton.onClick.AddListener(() => ChangePanel(-1));
            if(rightArrowButton) rightArrowButton.onClick.AddListener(() => ChangePanel(1));
            
            if(equipmentTabButton) equipmentTabButton.onClick.AddListener(() => SwitchCategory(ItemControllerType.Equipment));
            if(consumableTabButton) consumableTabButton.onClick.AddListener(() => SwitchCategory(ItemControllerType.Consumable));
            if(materialTabButton)   materialTabButton.onClick.AddListener(() => SwitchCategory(ItemControllerType.Material));
            if(keyItemTabButton)    keyItemTabButton.onClick.AddListener(() => SwitchCategory(ItemControllerType.KeyItem));

            // 點卷按鈕
            if (buyVoucherButtons != null)
            {
                for (int i = 0; i < buyVoucherButtons.Length; i++)
                {
                    int index = i;
                    if (buyVoucherButtons[i] != null) buyVoucherButtons[i].onClick.AddListener(() => OnBuyVoucherClicked(index));
                }
            }

            // ★ 角色按鈕綁定
            if (btn_Char1) btn_Char1.onClick.AddListener(OnChar1Clicked);
            if (btn_Char2) btn_Char2.onClick.AddListener(OnChar2Clicked);
            if (btn_Char3) btn_Char3.onClick.AddListener(OnChar3Clicked);
        }

        #endregion

        #region === ★★★ 角色系統邏輯 (新增) ★★★ ===

        void OnChar1Clicked()
        {
            // 角色1 預設擁有，直接切換
            GameManager.Instance.CurrentCharID = 1;
            Debug.Log("已切換為：角色 1");
            UpdateCharacterUI();
        }

        void OnChar2Clicked()
        {
            // 判斷是否擁有
            if (GameManager.Instance.OwnsChar2)
            {
                // 已擁有 -> 執行切換
                GameManager.Instance.CurrentCharID = 2;
                Debug.Log("已切換為：角色 2");
            }
            else
            {
                // 未擁有 -> 執行購買 (扣金幣 100 ID)
                var moneyData = InventoryManager.Instance.GetInventoryData(100);
                if (moneyData.quantity >= char2_Price)
                {
                    // 扣錢
                    InventoryManager.Instance.RemoveItem(100, char2_Price);
                    // 標記為已購買
                    GameManager.Instance.OwnsChar2 = true;
                    // 自動裝備
                    GameManager.Instance.CurrentCharID = 2;

                    Debug.Log($"購買成功！花費 {char2_Price} 金幣");
                    UpdateCurrencyUI(); // 刷新錢包顯示
                }
                else
                {
                    Debug.Log("金幣不足，無法購買角色 2");
                }
            }
            UpdateCharacterUI();
        }

        void OnChar3Clicked()
        {
            // 判斷是否擁有
            if (GameManager.Instance.OwnsChar3)
            {
                // 已擁有 -> 執行切換
                GameManager.Instance.CurrentCharID = 3;
                Debug.Log("已切換為：角色 3");
            }
            else
            {
                // 未擁有 -> 執行購買 (扣金幣 100 ID)
                var moneyData = InventoryManager.Instance.GetInventoryData(999);
                if (moneyData.quantity >= char3_EXPrice)
                {
                    // 扣錢
                    InventoryManager.Instance.RemoveItem(999, char3_EXPrice);
                    // 標記為已購買
                    GameManager.Instance.OwnsChar3 = true;
                    // 自動裝備
                    GameManager.Instance.CurrentCharID = 3;

                    Debug.Log($"購買成功！花費 {char3_EXPrice} 金幣");
                    UpdateCurrencyUI(); // 刷新錢包顯示
                }
                else
                {
                    Debug.Log("金幣不足，無法購買角色 3");
                }
            }
            UpdateCharacterUI();
        }
        
        // ★★★ 核心 UI 狀態更新 ★★★
        void UpdateCharacterUI()
        {
            int currentID = GameManager.Instance.CurrentCharID;
            bool hasChar2 = GameManager.Instance.OwnsChar2;
            // ★ 1. 記得獲取角色 3 的擁有權 (前提是 GameManager 裡要有 OwnsChar3 這個變數)
            bool hasChar3 = GameManager.Instance.OwnsChar3; 

            // --- 設定 角色 1 UI ---
            if (currentID == 1)
            {
                if (txt_Char1_Info) txt_Char1_Info.text = "使用中";
                if (btn_Char1) btn_Char1.interactable = false; 
            }
            else
            {
                if (txt_Char1_Info) txt_Char1_Info.text = "切換";
                if (btn_Char1) btn_Char1.interactable = true;
            }

            // --- 設定 角色 2 UI ---
            if (currentID == 2)
            {
                if (txt_Char2_Info) txt_Char2_Info.text = "使用中";
                if (btn_Char2) btn_Char2.interactable = false;
            }
            else
            {
                if (hasChar2)
                {
                    if (txt_Char2_Info) txt_Char2_Info.text = "切換";
                }
                else
                {
                    if (txt_Char2_Info) txt_Char2_Info.text = $"${char2_Price}";
                }
                if (btn_Char2) btn_Char2.interactable = true;
            }

            // --- ★★★ 2. 補上 角色 3 UI 設定 ★★★ ---
            if (currentID == 3)
            {
                // 正在使用角色 3
                if (txt_Char3_Info) txt_Char3_Info.text = "使用中";
                if (btn_Char3) btn_Char3.interactable = false;
            }
            else
            {
                if (hasChar3)
                {
                    // 買過了，但沒在用 -> 顯示切換
                    if (txt_Char3_Info) txt_Char3_Info.text = "切換";
                }
                else
                {
                    // 還沒買 -> 顯示價格 (注意：你的角色3是用點卷買的)
                    if (txt_Char3_Info) txt_Char3_Info.text = $"{char3_EXPrice} 點卷";
                }
                // 只要不是正在用，就可以點
                if (btn_Char3) btn_Char3.interactable = true;
            }
        }

        #endregion

        #region === 點卷/升級/其他邏輯 (保留原樣) ===

        void OnBuyVoucherClicked(int buttonIndex)
        {
            if (buyVoucherAmounts == null || buttonIndex >= buyVoucherAmounts.Length) return;
            int amountToGive = buyVoucherAmounts[buttonIndex];
            InventoryManager.Instance.AddItem(voucherItemId, amountToGive);
            Debug.Log($"購買成功，獲得 {amountToGive} 張點卷。");
            UpdateCurrencyUI();
            UpdateUpgradeInfoText();
        }

        void On_UpgradeCar_Gold_Clicked()
        {
            if (GameManager.Instance.CarStatsManager == null) return;
            int curLevel = GameManager.Instance.currentLevel;
            if (curLevel >= 10) return;
            
            int cost = upgradeCost_GoldBase * curLevel;
            var moneyData = InventoryManager.Instance.GetInventoryData(100);
            if (moneyData.quantity >= cost)
            {
                InventoryManager.Instance.RemoveItem(100, cost);
                PerformLevelUp();
            }
        }

        void On_UpgradeCar_Voucher_Clicked()
        {
            if (GameManager.Instance.CarStatsManager == null) return;
            if (GameManager.Instance.currentLevel >= 10) return;
            
            var voucherData = InventoryManager.Instance.GetInventoryData(voucherItemId);
            if (voucherData.quantity >= upgradeCost_VoucherFixed)
            {
                InventoryManager.Instance.RemoveItem(voucherItemId, upgradeCost_VoucherFixed);
                PerformLevelUp();
            }
        }

        void PerformLevelUp()
        {
            GameManager.Instance.CarStatsManager.LevelUp();
            UpdateCurrencyUI();
            UpdateUpgradeInfoText();
        }

        void UpdateUpgradeInfoText()
        {
            if (GameManager.Instance.CarStatsManager == null) return;
            int curLvl = GameManager.Instance.currentLevel;
            if (levelText) levelText.text = curLvl.ToString();

            if (curLvl >= 10)
            {
                if (btn_UpgradeCar_Gold) btn_UpgradeCar_Gold.interactable = false;
                if (btn_UpgradeCar_Voucher) btn_UpgradeCar_Voucher.interactable = false;
                if (upgradeInfoText) upgradeInfoText.text = "MAX";
                if (upgradeIEXnfoText) upgradeIEXnfoText.text = "MAX";
            }
            else
            {
                if (btn_UpgradeCar_Gold) btn_UpgradeCar_Gold.interactable = true;
                if (btn_UpgradeCar_Voucher) btn_UpgradeCar_Voucher.interactable = true;
                if (upgradeInfoText) upgradeInfoText.text = (upgradeCost_GoldBase * curLvl).ToString(); 
                if (upgradeIEXnfoText) upgradeIEXnfoText.text = upgradeCost_VoucherFixed.ToString(); 
            }
        }

        private void ChangePanel(int direction)
        {
            if (subPanels == null || subPanels.Length == 0) return;
            int newIndex = _currentPanelIndex + direction;
            if (newIndex >= 0 && newIndex < subPanels.Length)
            {
                _currentPanelIndex = newIndex;
                UpdateSubPanelState();
            }
        }

        private void UpdateSubPanelState()
        {
            if (subPanels == null) return;
            for (int i = 0; i < subPanels.Length; i++)
                if(subPanels[i]) subPanels[i].SetActive(i == _currentPanelIndex);
            
            if(leftArrowButton) leftArrowButton.interactable = (_currentPanelIndex > 0);
            if(rightArrowButton) rightArrowButton.interactable = (_currentPanelIndex < subPanels.Length - 1);
        }

        void OnCloseButtonClicked() { RequestClose(); }

        void SwitchCategory(ItemControllerType type)
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = type});  
        }

        void OnStoreItemClickedEvent(StoreItemClickedEvent cmd)
        {
            if (cmd.StoreItemData == null) return;
            if (selectedItemIcon) selectedItemIcon.sprite = cmd.StoreItemData.ItemBaseTemplete.ItemIconPath;
            if (selectedItemName) selectedItemName.text = cmd.StoreItemData.ItemBaseTemplete.Name;
            if (selectedItemDescription) selectedItemDescription.text = cmd.StoreItemData.ItemBaseTemplete.ItemDescription;
        }

        void OnPurchaseItemClickedEvent(PurchaseItemClickedEvent cmd) { UpdateCurrencyUI(); }

        void UpdateCurrencyUI()
        {
            if (InventoryManager.Instance != null)
            {
                if (textMoneyValue) textMoneyValue.text = InventoryManager.Instance.GetInventoryData(100).quantity.ToString();
                if (textVoucherValue) textVoucherValue.text = InventoryManager.Instance.GetInventoryData(voucherItemId).quantity.ToString();
            }
        }
        
        void Update_StoreItemDataInGrid()
        {
            if(scrollViewContentStoreItemList == null) return;
            foreach (Transform child in scrollViewContentStoreItemList.transform) Destroy(child.gameObject);
            if (StoreManager.Instance == null) return;
            var targetStore = StoreManager.Instance.AllStoresRuntimeData.stores.Find(s => s.storeId == StoreManager.Instance.CurrentStoreId);
            if (targetStore == null || targetStore.categoryGroups.Count == 0 || targetStore.categoryGroups[0].items == null) return;
            foreach (StoreItemRuntimeData storeItemData in targetStore.categoryGroups[0].items)
            {
                GameObject newSlot = Instantiate(prefabSlotStoreItem, scrollViewContentStoreItemList.transform);
                newSlot.GetComponent<SlotStoreItem>().Initialize(storeItemData, gameObject);
            }
        }
        #endregion
    }
}