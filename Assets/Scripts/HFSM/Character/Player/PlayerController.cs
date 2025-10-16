using System;
using Gamemanager;
using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerController : MonoBehaviour
    {
        private GameObject _player;
        private Animator _animator;
        private Rigidbody _rigidbody;
        
        [SerializeField] private PlayerSO data;
        [SerializeField] private PlayerStateReusableData reusableData;
        [SerializeField] private PlayerAnimationData playerAnimationData;

        private Transform _mainCameraTransform;
        
        private PlayerRootHFSM _playerRootHFSM;
        private PlayerStateContext _playerStateContext;

        public PlayerStateContext PlayerStateContext => _playerStateContext;

        [Header("暫時測試用")]
        [SerializeField] private GroundTrigger groundTrigger;
        
        private void Awake()
        {
            playerAnimationData.Initialize();
            
            _player = gameObject;
            _animator = GetComponentInChildren<Animator>();
            _rigidbody = GetComponent<Rigidbody>();

            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            
            // 建構 context
            _playerStateContext = new PlayerStateContext(_player, _animator, _rigidbody, data, reusableData, playerAnimationData, _mainCameraTransform);
            _playerRootHFSM = new PlayerRootHFSM(_playerStateContext);

            // 事件訂閱（使用更清楚的命名）
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnMovementKeyPressedEvent,
                cmd => { reusableData.MovementInput = cmd.MoveInput; });
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnJumpPressedEvent,
                cmd => { reusableData.JumpInput = cmd.JumpPressed; });
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnWalkToggledEvent,
                cmd => { reusableData.IsPreferWalkMode = cmd.WalkToggled; });

            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnRunPressedEvent,
                cmd => { reusableData.IsSprinting = cmd.RunPressed; });
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnAttackPressedEvent,
                cmd => { reusableData.WantsToAttack = cmd.AttackPressed; });
        }
        
        private void Update()
        {
            _playerRootHFSM.Tick();
            reusableData.IsGrounded = groundTrigger.IsGrounded;
        }

        private void FixedUpdate()
        {
            //GameManager.Instance.MainGameEvent.Send(new FixedUpdateEvent());
            _playerRootHFSM.GetCurrentState().PhysicsUpdate();
        }
        
        public void OnMovementStateAnimationEnterEvent()
        {
            _playerRootHFSM.OnAnimationEnterEvent();
        }

        public void OnMovementStateAnimationExitEvent()
        {
            _playerRootHFSM.OnAnimationExitEvent();
        }

        public void OnMovementStateAnimationTransitionEvent()
        {
            _playerRootHFSM.OnAnimationTransitionEvent();
        }
        
        public void OnAttackWindupFinishedEvent() 
        {
            _playerRootHFSM.OnAttackWindupFinishedEvent();
        }

        public void OnAttackSwingFinishedEvent() 
        {
            _playerRootHFSM.OnAttackSwingFinishedEvent();
        }

        public void OnComboWindowOverEvent() 
        {
            _playerRootHFSM.OnComboWindowOverEvent();
        }
    }

    public class PlayerStateContext
    {
        public GameObject Player;
        
        public Animator Animator { get; }
        public Rigidbody Rigidbody { get; }
        public PlayerSO Data { get; }
        public PlayerStateReusableData ReusableData { get; }
        public PlayerAnimationData AnimationData { get; }
        
        public Transform MainCameraTransform { get; }

        public PlayerStateContext(
            GameObject player,
            Animator animator,
            Rigidbody rigidbody,
            PlayerSO data,
            PlayerStateReusableData reusableData,
            PlayerAnimationData animationData,
            Transform mainCameraTransform)
        {
            Player = player;
            Animator = animator;
            Rigidbody = rigidbody;
            Data = data;
            ReusableData = reusableData;
            AnimationData = animationData;
            MainCameraTransform  = mainCameraTransform;
        }
    }
}
