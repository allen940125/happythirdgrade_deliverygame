using Gamemanager;
using System;
using UniRx;

namespace Gamemanager
{
    public class MainGameEventPack : GameEventPack
    {
        // ======================
        // Common Event Streams
        // ======================
        public IObservable<GameInitializedEvent> OnGameInitializedEvent => getSubject<GameInitializedEvent>();

        // ======================
        // Inventory Event Streams
        // ======================

        public IObservable<PlayerBagRefreshedEvent> OnPlayerBagRefreshedEvent => getSubject<PlayerBagRefreshedEvent>();
        public IObservable<InventoryItemClickedEvent> OnInventoryItemClickedEvent => getSubject<InventoryItemClickedEvent>();
        public IObservable<ItemAddedToBagEvent> OnItemAddedToBagEvent => getSubject<ItemAddedToBagEvent>();

        // ======================
        // Store Event Streams
        // ======================

        public IObservable<StoreItemsRefreshedEvent> OnStoreItemsRefreshedEvent => getSubject<StoreItemsRefreshedEvent>();
        public IObservable<StoreItemClickedEvent> OnStoreItemClickedEvent => getSubject<StoreItemClickedEvent>();
        public IObservable<PurchaseItemClickedEvent> OnPurchaseItemClickedEvent => getSubject<PurchaseItemClickedEvent>();

        // ======================
        // Quest Event Streams
        // ======================
        
        public IObservable<MoneyChangedEvent> OnMoneyChangedEvent => getSubject<MoneyChangedEvent>();
        public IObservable<DeliverySuccessfulEvent> OnDeliverySuccessfulEvent => getSubject<DeliverySuccessfulEvent>();
        public IObservable<GameQuestCompletedEvent> OnGameQuestCompletedEvent => getSubject<GameQuestCompletedEvent>();
        public IObservable<AchievementUnlockedEvent> OnAchievementUnlockedEvent => getSubject<AchievementUnlockedEvent>();
        
        // ======================
        // Scene Event Streams
        // ======================

        public IObservable<SceneTransitionStartedEvent> OnSceneTransitionStartedEvent => getSubject<SceneTransitionStartedEvent>();
        public IObservable<SceneLoadedEvent> OnSceneLoadedEvent => getSubject<SceneLoadedEvent>();

        // ======================
        // Input Player Event Streams
        // ======================
        
        public IObservable<FixedUpdateEvent> OnFixedUpdateEvent => getSubject<FixedUpdateEvent>();
        
        public IObservable<MovementKeyPressedEvent> OnMovementKeyPressedEvent => getSubject<MovementKeyPressedEvent>();
        public IObservable<CursorToggledEvent> OnCursorToggledEvent => getSubject<CursorToggledEvent>();
        public IObservable<WalkToggledEvent> OnWalkToggledEvent => getSubject<WalkToggledEvent>();
        public IObservable<RunPressedEvent> OnRunPressedEvent => getSubject<RunPressedEvent>();
        public IObservable<JumpPressedEvent> OnJumpPressedEvent => getSubject<JumpPressedEvent>();
        public IObservable<AttackPressedEvent> OnAttackPressedEvent => getSubject<AttackPressedEvent>();
        
        // ======================
        // Input UI Event Streams
        // ======================
        
        public IObservable<EscapeKeyPressedEvent> OnEscapeKeyPressedEvent => getSubject<EscapeKeyPressedEvent>();
        public IObservable<OpenBackpackKeyPressedEvent> OnOpenBackpackKeyPressedEvent => getSubject<OpenBackpackKeyPressedEvent>();
        
        // ======================
        // Player Event Streams
        // ======================
        
        public IObservable<PlayerHurtPressedEvent> OnPlayerHurtPressedEvent => getSubject<PlayerHurtPressedEvent>();
        
        // ======================
        // Game State Event Streams
        // ======================

        public IObservable<GameOverEvent> OnGameOverEvent => getSubject<GameOverEvent>();
    }
}
