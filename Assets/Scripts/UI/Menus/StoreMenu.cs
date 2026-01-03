using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class StoreMenu : BasePanel
    {
        [Header("設定通用按鈕")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button closeButton;
        
        [Header("物品分類標籤頁")]
        [SerializeField] private Button equipmentTabButton; 
        [SerializeField] private Button consumableTabButton; 
        [SerializeField] private Button materialTabButton;
        [SerializeField] private Button keyItemTabButton;

        [Header("商店資訊")]
        [SerializeField] GameObject prefabSlotStoreItem;
        [SerializeField] GameObject scrollViewContentStoreItemList;
        [SerializeField] TMP_Text textMoneyVolue;

        [Header("當前選中物品資訊")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TMP_Text selectedItemName;
        [SerializeField] private TMP_Text selectedItemDescription;
        
        [Header("升級設定")]
        [SerializeField] private Button 升等車;
        [Tooltip("升級一次需要多少錢 (基礎費)")]
        [SerializeField] private int upgradeCost = 500; 
        
        // ★★★ 新增：顯示等級與費用的文字框 ★★★
        [Tooltip("請將顯示等級費用的 TextMeshPro 拉進來")]
        [SerializeField] private TMP_Text upgradeInfoText; 
        
        [Tooltip("請將顯示等級費用的 TextMeshPro 拉進來")]
        [SerializeField] private TMP_Text levle; 

        
        protected override void Awake()
        {
            base.Awake();

            // 設定通用按鈕
            InitializeCommonButtons();
            
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = true});  
            
            // 訂閱事件
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnStoreItemClickedEvent, OnStoreItemClickedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPurchaseItemClickedEvent, OnPurchaseItemClickedEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            GameManager.Instance.UIManager.OpenPanel<GameHUD>(UIType.GameHUD);
            
            // 取消訂閱事件
            GameManager.Instance.MainGameEvent.Unsubscribe<StoreItemClickedEvent>(OnStoreItemClickedEvent);
            GameManager.Instance.MainGameEvent.Unsubscribe<PurchaseItemClickedEvent>(OnPurchaseItemClickedEvent);
        }
        
        protected override void Start()
        {
            base.Start();
            Update_StoreItemDataInGrid();
            UpdateMoneyVolueText();
            
            // ★★★ 初始化時更新升級資訊文字 ★★★
            UpdateUpgradeInfoText();
        }
        
        #region 事件訂閱

        void OnStoreItemClickedEvent(StoreItemClickedEvent cmd)
        {
            UpdateClickItemInfo(cmd.StoreItemData);
        }

        void OnPurchaseItemClickedEvent(PurchaseItemClickedEvent cmd)
        {
            UpdateMoneyVolueText();
        }

        #endregion
        
        #region 註冊按鈕事件

        /// <summary>
        /// 初始化通用按鈕
        /// </summary>
        void InitializeCommonButtons()
        {
            if(useButton) useButton.onClick.AddListener(OnUseButtonClicked);
            if(closeButton) closeButton.onClick.AddListener(OnCloseButtonClicked);
            if(升等車) 升等車.onClick.AddListener(OnULButtonClicked);
        }

        /// <summary>
        /// 初始化設定類別按鈕
        /// </summary>
        void InitializeCategoryButtons()
        {
            equipmentTabButton.onClick.AddListener(OnEquipmentTabButtonClicked);
            consumableTabButton.onClick.AddListener(OnConsumableTabButtonClicked);
            materialTabButton.onClick.AddListener(OnMaterialTabButtonClicked);
            keyItemTabButton.onClick.AddListener(OnKeyItemTabButtonClicked);
        }

        // 設定通用按鈕
        void OnUseButtonClicked()
        {
            if (InventoryManager.Instance.curClickInventoryItemRuntimeData != null)
            {
                StoreManager.Instance.PurchaseItem(StoreManager.Instance.curClickStoreItemData, 1);
            }
        }

        void OnCloseButtonClicked()
        {
            Debug.Log("Click Btn_Cancel");
            RequestClose();
        }
        
        // ──────────────────────────────────────────────
        //  ★ 車子升級邏輯 (包含更新文字)
        // ──────────────────────────────────────────────
        void OnULButtonClicked()
        {
            Debug.Log("Click UL (嘗試升級車子)");

            if (GameManager.Instance.CarStatsManager == null)
            {
                Debug.LogError("錯誤：找不到 CarStatsManager！");
                return;
            }

            // 1. 檢查等級上限 (Max 10)
            if (GameManager.Instance.CarStatsManager.currentLevel >= 10)
            {
                Debug.Log($"<color=yellow>升級失敗：已達到最高等級</color>");
                return;
            }

            // 2. 準備數據
            var moneyData = InventoryManager.Instance.GetInventoryData(100);
            int currentMoney = moneyData.quantity;
            
            // 計算費用：基礎費 * 當前等級
            int currentLevel = GameManager.Instance.CarStatsManager.currentLevel;
            int upgradeCosts = upgradeCost * currentLevel;
            
            if (currentMoney >= upgradeCosts)
            {
                // A. 扣錢
                InventoryManager.Instance.RemoveItem(100, upgradeCosts);

                // B. 執行升級
                GameManager.Instance.CarStatsManager.LevelUp();

                Debug.Log($"<color=green>升級成功！</color> Lv.{GameManager.Instance.CarStatsManager.currentLevel} | 花費: {upgradeCosts}");

                // C. 更新介面 (金錢 + 升級資訊文字)
                UpdateMoneyVolueText();
                UpdateUpgradeInfoText(); // ★★★ 升級後更新文字 ★★★
            }
            else
            {
                Debug.Log($"<color=red>金錢不足！</color> 需要: {upgradeCosts}, 擁有: {currentMoney}");
            }
        }

        // ──────────────────────────────────────────────
        //  ★ 新增功能：更新升級資訊文字
        // ──────────────────────────────────────────────
        void UpdateUpgradeInfoText()
        {
            if (upgradeInfoText == null) return;
            if (GameManager.Instance.CarStatsManager == null) return;

            int curLvl = GameManager.Instance.CarStatsManager.currentLevel;

            // 判斷是否滿等
            if (curLvl >= 10)
            {
                //upgradeInfoText.text = $"等級: {curLvl} (已滿等)";
                if (升等車) 升等車.interactable = false; // 滿等就把按鈕鎖起來
                levle.text = curLvl.ToString();
            }
            else
            {
                // 顯示下個等級的費用
                int cost = upgradeCost * curLvl;
                //upgradeInfoText.text = $"等級: {curLvl} ▶ {curLvl + 1}\n費用: {cost:N0} G";
                if (升等車) 升等車.interactable = true;
                levle.text = curLvl.ToString();
                upgradeInfoText.text = cost.ToString();
            }
        }


        // 設定類別按鈕
        void OnEquipmentTabButtonClicked()
        {
            Debug.Log("Click OnEquipmentTabButtonClicked");
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Equipment});  
        }

        void OnConsumableTabButtonClicked()
        {
            Debug.Log("Click OnConsumableTabButtonClicked");
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Consumable});  
        }
        
        void OnMaterialTabButtonClicked()
        {
            Debug.Log("Click OnMaterialTabButtonClicked");
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Material});  
        }

        void OnKeyItemTabButtonClicked()
        {
            Debug.Log("Click OnKeyItemTabButtonClicked");
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.KeyItem});  
        }

        #endregion

        /// <summary>
        /// 添加商店物品數據至ScrollView_Content
        /// </summary>
        void Update_StoreItemDataInGrid()
        {
            // 清空現有物品
            foreach (Transform child in scrollViewContentStoreItemList.transform)
            {
                Destroy(child.gameObject);
            }

            if (StoreManager.Instance == null) return;

            var targetStore = StoreManager.Instance.AllStoresRuntimeData.stores
                .Find(s => s.storeId == StoreManager.Instance.CurrentStoreId);

            if (targetStore == null || 
                targetStore.categoryGroups.Count == 0 || 
                targetStore.categoryGroups[0].items == null)
            {
                return;
            }

            foreach (StoreItemRuntimeData storeItemData in targetStore.categoryGroups[0].items)
            {
                GameObject newSlot = Instantiate(prefabSlotStoreItem, scrollViewContentStoreItemList.transform);
                newSlot.GetComponent<SlotStoreItem>().Initialize(storeItemData, gameObject);
            }
        }

        /// <summary>
        /// 更新點擊的物品資訊
        /// </summary>
        void UpdateClickItemInfo(StoreItemRuntimeData itemData)
        {
            if (itemData == null || selectedItemIcon == null || selectedItemName == null || selectedItemDescription == null)
            {
                return;
            }

            selectedItemIcon.sprite = itemData.ItemBaseTemplete.ItemIconPath;
            selectedItemName.text = itemData.ItemBaseTemplete.Name;
            selectedItemDescription.text = itemData.ItemBaseTemplete.ItemDescription;
        }
        
        /// <summary>
        /// 更新金錢文字UI
        /// </summary>
        void UpdateMoneyVolueText()
        {
            if (InventoryManager.Instance != null && textMoneyVolue != null)
            {
                textMoneyVolue.text = InventoryManager.Instance.PlayerMoney.quantity.ToString();
            }
        }
    }
}