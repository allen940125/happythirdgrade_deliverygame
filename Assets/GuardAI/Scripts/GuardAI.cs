using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityHFSM;

public class GuardAI : MonoBehaviour
{
    // 狀態機物件
    private StateMachine _fsm;
    
    [Header("觸發搜尋與攻擊的範圍")]
    public float searchSpotRange = 10;
    public float attackRange = 3;
    
    [Header("搜尋持續時間")]
    public float searchTime = 20;
    
    [Header("巡邏、追逐、攻擊的移動速度")]
    public float patrolSpeed = 2;
    public float chaseSpeed = 4;
    public float attackSpeed = 2;
    
    [Header("巡邏點座標陣列")]
    public Vector3[] patrolPoints;

    // 動畫、狀態顯示、巡邏方向、最後看到玩家的位置
    private Animator _animator;
    private TMP_Text _stateDisplayText;
    private int _patrolDirection = 1;
    private Vector3 _lastSeenPlayerPosition;

    // 玩家當前位置與與守衛之間的距離
    private Vector3 PlayerPosition => GameManager.Instance.Player.transform.position;
    private float DistanceToPlayer => Vector3.Distance(PlayerPosition, transform.position);

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _stateDisplayText = GetComponentInChildren<TMP_Text>();

        _fsm = new StateMachine();

        // ---- 戰鬥子狀態機 (Fight) ----
        var fightFsm = new HybridStateMachine(
            beforeOnLogic: state => MoveTowards(PlayerPosition, attackSpeed, minDistance: 1),
            needsExitTime: true
        );

        fightFsm.AddState("Wait",new State(
            onEnter:   s => { /* idle，由 Speed=0+IsFighting=true 切換 */ },
            onLogic: s =>{Debug.Log("Wait");}
        ));
        fightFsm.AddState("Telegraph",
            onEnter:   s => _animator.SetTrigger("Telegraph")
        );
        fightFsm.AddState("Hit",
            onEnter:   s => _animator.SetTrigger("Attack")
        );

        fightFsm.AddExitTransition("Wait");
        fightFsm.AddTransition(new TransitionAfter("Wait", "Telegraph", 0.5f));
        fightFsm.AddTransition(new TransitionAfter("Telegraph", "Hit", 0.42f));
        fightFsm.AddTransition(new TransitionAfter("Hit", "Wait", 0.5f));

        // 設定 fight 子狀態機
        fightFsm.SetStartState("Wait");
        fightFsm.Init();

        
        // ---- 主狀態機 ----
        _fsm.AddState("Patrol", new CoState(this, Patrol, loop: false,
            onEnter: s => {
                _animator.SetBool("IsFighting", false);
                _animator.SetFloat("Speed", patrolSpeed);
            },
            onExit: s => {
                _animator.SetFloat("Speed", 0f);
            }
        ));

        _fsm.AddState("Chase", new State(
            onEnter:   s => {
                _animator.SetBool("IsFighting", false);
                _animator.SetFloat("Speed", chaseSpeed);
            },
            onLogic:   s => MoveTowards(PlayerPosition, chaseSpeed),
            onExit:    s => _animator.SetFloat("Speed", 0f)
        ));
        _fsm.AddState("Fight", new State(
            onEnter:   s => _animator.SetBool("IsFighting", true),
            onLogic:   s => fightFsm.OnLogic(),
            onExit:    s => _animator.SetBool("IsFighting", false)
        ));
        _fsm.AddState("Search", new CoState(this, Search, loop: false));

        _fsm.SetStartState("Patrol");

        _fsm.AddTriggerTransition("PlayerSpotted", "Patrol", "Chase");
        _fsm.AddTwoWayTransition("Chase", "Fight", t => DistanceToPlayer <= attackRange);
        _fsm.AddTransition("Chase", "Search",
            t => DistanceToPlayer > searchSpotRange,
            onTransition: t => _lastSeenPlayerPosition = PlayerPosition);
        _fsm.AddTransition("Search", "Chase", t => DistanceToPlayer <= searchSpotRange);
        _fsm.AddTransition(new TransitionAfter("Search", "Patrol", searchTime));

        _fsm.Init();
    }

    void Update()
    {
        //Debug.Log("與玩家的距離" + DistanceToPlayer);
        _fsm.OnLogic(); // 每幀更新狀態機
        _stateDisplayText.text = _fsm.GetActiveHierarchyPath(); // 顯示當前狀態
    }

    // 碰到玩家時觸發狀態機事件
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _fsm.Trigger("PlayerSpotted");
        }
    }

    // 朝目標前進，直到接近最小距離
    private void MoveTowards(Vector3 target, float speed, float minDistance = 0f)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0; // 忽略Y軸移動
        float distance = direction.magnitude;

        if (distance > minDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    // 協程：移動到指定位置，直到接近容差距離
    private IEnumerator MoveToPosition(Vector3 target, float speed, float tolerance = 0.1f)
    {
        while (Vector3.Distance(transform.position, target) > tolerance)
        {
            MoveTowards(target, speed);
            yield return null;
        }
    }

    // 協程：巡邏行為
    private IEnumerator Patrol()
    {
        int currentPointIndex = FindClosestPatrolPoint();

        while (true)
        {
            yield return MoveToPosition(patrolPoints[currentPointIndex], patrolSpeed); // 前往巡邏點
            yield return new WaitForSeconds(3); // 停留時間

            currentPointIndex += _patrolDirection;

            // 到頭就轉向
            if (currentPointIndex >= patrolPoints.Length || currentPointIndex < 0)
            {
                currentPointIndex = Mathf.Clamp(currentPointIndex, 0, patrolPoints.Length - 1);
                _patrolDirection *= -1;
            }
        }
    }

    // 找出離守衛最近的巡邏點
    private int FindClosestPatrolPoint()
    {
        float minDistance = Vector3.Distance(transform.position, patrolPoints[0]);
        int minIndex = 0;

        for (int i = 1; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                minIndex = i;
            }
        }

        return minIndex;
    }

    // 協程：搜尋玩家最後出現的位置，並隨機移動幾次模擬搜尋
    private IEnumerator Search()
    {
        yield return MoveToPosition(_lastSeenPlayerPosition, chaseSpeed); // 前往最後看到玩家的位置

        while (true)
        {
            yield return new WaitForSeconds(2); // 停頓觀察

            Vector3 offset = Random.insideUnitSphere * 10f; // 隨機位置
            offset.y = 0; // 保持在XZ平面

            Vector3 target = transform.position + offset;
            yield return MoveToPosition(target, patrolSpeed); // 移動到該位置
        }
    }
}