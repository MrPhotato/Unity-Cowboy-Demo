using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyCowboy;

namespace MyCowboy.Demo
{
    // Base State class for the State Machine pattern
    public abstract class CharacterStateBase
    {
        protected BasicMotionsCharacterController controller;

        public CharacterStateBase(BasicMotionsCharacterController controller)
        {
            this.controller = controller;
        }

        public virtual void EnterState() { }
        public virtual void UpdateState() { }
        public virtual void FixedUpdateState() { }
        public virtual void ExitState() { }
        public virtual CharacterState GetStateEnum() { return CharacterState.Idle; }
    }

    // Concrete state implementations
    public class IdleState : CharacterStateBase
    {
        public IdleState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            // Reset movement parameters
            controller.moveSpeed = controller.runSpeed;
        }

        public override CharacterState GetStateEnum() { return CharacterState.Idle; }
    }

    public class WalkState : CharacterStateBase
    {
        public WalkState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            controller.moveSpeed = controller.walkSpeed;
        }

        public override CharacterState GetStateEnum() { return CharacterState.Walk; }
    }

    public class RunState : CharacterStateBase
    {
        public RunState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            controller.moveSpeed = controller.runSpeed;
        }

        public override CharacterState GetStateEnum() { return CharacterState.Run; }
    }

    public class SprintState : CharacterStateBase
    {
        public SprintState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            controller.moveSpeed = controller.sprintSpeed;
        }

        public override void UpdateState()
        {
            // Additional collision check to avoid tunneling when sprinting
            controller.CheckCollisions();
        }

        public override CharacterState GetStateEnum() { return CharacterState.Sprint; }
    }

    public class JumpState : CharacterStateBase
    {
        public JumpState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            // 使用更大的跳跃力
            controller.verticalVelocity = controller.jumpForce * 1.2f;
            controller.animator[0].SetBool("Jump", true);
            
            // 强制设置为非地面状态
            controller.animator[0].SetBool("Grounded", false);
            
            // 开始跳跃协程以避免地面检测
            if (controller.jumpCheckGroundAvoider != null)
            {
                controller.StopCoroutine(controller.jumpCheckGroundAvoider);
            }
            controller.jumpCheckGroundAvoider = controller.JumpCheckGroundAvoider();
            controller.StartCoroutine(controller.jumpCheckGroundAvoider);
            
            // 输出调试信息
            // Debug.Log("Jump state entered with velocity: " + controller.verticalVelocity);
        }

        public override CharacterState GetStateEnum() { return CharacterState.Jump; }
    }

    public class FallState : CharacterStateBase
    {
        public FallState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            // Set falling animation if needed
        }

        public override CharacterState GetStateEnum() { return CharacterState.Fall; }
    }

    public class CrouchState : CharacterStateBase
    {
        public CrouchState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            controller.moveSpeed = controller.crouchSpeed;
            controller.collisionBox.center = controller.crouchBoxCenter;
            controller.collisionBox.size = controller.crouchBoxSize;
        }

        public override void ExitState()
        {
            controller.collisionBox.center = controller.defaultBoxCenter;
            controller.collisionBox.size = controller.defaultBoxSize;
        }

        public override CharacterState GetStateEnum() { return CharacterState.Crouch; }
    }

    public class ShootState : CharacterStateBase
    {
        private float shootingTime = 0f;

        private Vector2 originalMovement; // 保存原始移动输入

        public ShootState(BasicMotionsCharacterController controller) : base(controller) { }

        public override void EnterState()
        {
            // 保存原始移动状态
            originalMovement = controller.inputs.movement;
            
            // 禁止移动
            controller.inputs.movement = Vector2.zero;
            
            // 播放射击动画
            controller.animator[0].SetBool("Shooting", true);
            
            // 重置射击时间
            shootingTime = 0f;
            
            // 调用射击逻辑
            controller.FireWeapon();
            
            // 输出调试信息
            // Debug.Log("进入射击状态 - 禁止移动" + controller.shootDuration + "秒");
        }
        
        public override void UpdateState()
        {
            // 保持角色不能移动
            controller.inputs.movement = Vector2.zero;
            
            // 计时，超过射击动画时间后返回前一个状态
            shootingTime += Time.deltaTime;
            if (shootingTime >= controller.shootDuration)
            {
                // 恢复原始输入
                controller.inputs.movement = originalMovement;
                
                // 如果在地面上，根据移动状态返回相应状态
                if (controller.animator[0].GetBool("Grounded"))
                {
                    if (controller.inputs.movement.magnitude > 0.1f)
                    {
                        if (controller.sprint)
                            controller.ChangeState(CharacterState.Sprint);
                        else if (controller.walk)
                            controller.ChangeState(CharacterState.Walk);
                        else
                            controller.ChangeState(CharacterState.Run);
                    }
                    else
                    {
                        controller.ChangeState(CharacterState.Idle);
                    }
                }
                // 如果在空中
                else
                {
                    if (controller.verticalVelocity > 0)
                        controller.ChangeState(CharacterState.Jump);
                    else
                        controller.ChangeState(CharacterState.Fall);
                }
            }
            
            // 如果持续按住鼠标左键并且冷却完成，可以再次射击
            if (Input.GetMouseButton(0) && shootingTime >= controller.shootDuration && controller.canShoot)
            {
                controller.FireWeapon();
                shootingTime = 0f; // 重置射击时间
            }
        }
        
        public override void ExitState()
        {
            // 退出射击状态时，关闭射击动画
            controller.animator[0].SetBool("Shooting", false);
            
            // 恢复原始输入（以防万一）
            controller.inputs.movement = originalMovement;
            
            // Debug.Log("退出射击状态 - 恢复移动能力");
        }

        public override CharacterState GetStateEnum() { return CharacterState.Shoot; }
    }

    ///MAIN CLASS//
    public class BasicMotionsCharacterController : MonoBehaviour
    {
        [Header("[CHARACTER STATE]")]
        public CharacterState characterState; // CURRENT STATE OF THE CHARACTER
        private CharacterStateBase currentState; // Current state in the state machine
        private Dictionary<CharacterState, CharacterStateBase> states; // Dictionary to store all states
        
        // Initialize the state machine
        private void InitializeStateMachine()
        {
            // Create state dictionary
            states = new Dictionary<CharacterState, CharacterStateBase>
            {
                { CharacterState.Idle, new IdleState(this) },
                { CharacterState.Walk, new WalkState(this) },
                { CharacterState.Run, new RunState(this) },
                { CharacterState.Sprint, new SprintState(this) },
                { CharacterState.Jump, new JumpState(this) },
                { CharacterState.Fall, new FallState(this) },
                { CharacterState.Crouch, new CrouchState(this) },
                { CharacterState.Shoot, new ShootState(this) }
            };
            
            // Set initial state
            ChangeState(CharacterState.Idle);
        }
        
        // Change to a new state
        public void ChangeState(CharacterState newState)
        {
            // Exit current state if it exists
            if (currentState != null)
            {
                currentState.ExitState();
            }
            
            // Set the new state
            characterState = newState;
            currentState = states[newState];
            
            // Enter the new state
            currentState.EnterState();
        }
        
        // Legacy method for backward compatibility
        public void ChangeStateAndCollision(CharacterState newState) // FUNCTION TO MODIFY CHARACTER STATE
        {
            // Handle special conditions to adjust state before applying
            switch (newState)
            {
                case CharacterState.Idle:
                case CharacterState.Walk:
                case CharacterState.Run:
                case CharacterState.Sprint:
                    if (crouchLayerWeight >= 0.5f) // THRESHOLD FOR TRIGGERING CROUCH STATE
                    {
                        newState = CharacterState.Crouch;
                    }
                    break;

                // For other states (Jump, Fall, Crouch), no special adjustment is needed
                default:
                    break;
            }
            
            // Change to the new state using the state machine
            ChangeState(newState);
        }


        [Header("[CAMERA]")]
        public Transform mainCamera; // 摄像机的 Transform

        [Header("[ANIMATOR]")]
        //ASSIGN HERE THE ANIMATOR FROM BOTH CHARACTERS
        //TO MAKE CHARACTER SWITCH POSSIBLE IN THE MIDDLE OF AN ANIMATION BOTH ANIMATORS ARE USED AT THE SAME TIME
        public Animator[] animator;

        //VARIABLES TO CONTROL ANIMATOR LAYERS
        public int walkLayer = 1;
        public float walkLayerWeight = 0f;
        public float walkTransitionSpeed = 10f;
        public int sprintLayer = 2;
        public float sprintLayerWeight = 0f;
        public float sprintTransitionSpeed = 6f;
        public int crouchLayer = 3;
        public float crouchLayerWeight = 0f;
        public float crouchTransitionSpeed = 10f;

        [Header("[MOVEMENT]")]
        public float moveSpeed;               //CURRENT CHARACTER SPEED
        public float runSpeed = 4.4f;         //SPEED WHEN RUNNING
        public float walkSpeed = 2f;          //SPEED WHEN WALKING

        public float crouchSpeed = 2f;        //SPEED WHEN CROUCHING
        public float sprintSpeed = 7.5f;      //SPEED WHEN SPRINTING
        public float turnSpeed = 1f;        //SPEED FOR TURNING THE CHARACTER
        private Vector3 moveDirection = Vector3.zero; //CURRENT CHARACTER MOVEMENT DIRECTION

        private bool jump; //JUMP CHECK
        public float jumpForce = 4f; //VERTICAL VELOCITY APPLIED WHEN JUMPING
        public float verticalVelocity = 0f; //CURRENT VERTICAL VELOCITY

        public bool walk = false; // 标记是否为走路
        public bool sprint = false; //标记是否冲刺

        //COROUTINE WHEN JUMPING (BYPASSES GROUND CHECK AT THE BEGINNING OF A JUMP)
        public IEnumerator jumpCheckGroundAvoider;
        public IEnumerator JumpCheckGroundAvoider()
        {
            jump = true;
            animator[0].SetBool("Grounded", false);
            
            // 给角色一个初始向上位移，帮助脱离地面
            transform.Translate(Vector3.up * 0.2f);
            
            // 等待少量帧让跳跃开始
            for (int i = 0; i < 10; i++)
            {
            yield return new WaitForFixedUpdate();
            }
            
            // 重置跳跃动画状态，但保持跳跃物理状态
            animator[0].SetBool("Jump", false);
            
            // 再等待一些帧确保不会过早检测地面
            for (int i = 0; i < 5; i++)
            {
            yield return new WaitForFixedUpdate();
            }
            
            // 允许地面检测恢复
            jump = false;
            Debug.Log("跳跃协程完成，可以开始检测地面");
        }

        //JUMP COYOTE TIME
        private bool canJump = false;
        private IEnumerator canJumpTimer;
        private IEnumerator CanJumpTimer()
        {
            // 增加土狼时间的持续时间以获得更好的跳跃体验
            canJump = true;
            Debug.Log("土狼时间开始: 允许短时间内跳跃");
            
            // 延长土狼时间窗口
            yield return new WaitForSeconds(0.2f);
            
            canJump = false;
            Debug.Log("土狼时间结束: 禁止跳跃");
            canJumpTimer = null;
        }


        [Header("[INPUTS]")]
        public InputSent inputs; // 改为public，让ShootState可以访问
        private bool blockControls = false; //USED FOR BLOCKING CONTROLS (FINISH LINE ANIMATION)
        private float movementInputSpeed = 6f; //SPEED FOR CHANGING MOVEMENT INPUTS (ANIMATOR PARAMETER)
        private float inputX = 0; //VARIABLE FOR X INPUT MOVEMENT FLOAT ANIMATOR PARAMATER
        private float inputY = 0; //VARIABLE FOR Y INPUT MOVEMENT FLOAT ANIMATOR PARAMATER
        private bool moving; //CHECK TO DETECT IF THERE IS MOVEMENT INPUT
        private float timeMoving; //AMOUNT OF TIME PLAYER MOVED

        private bool allowInputWhileJumping = false;
        public Vector2 lastMovementInputs = Vector2.zero;

        [Header("[PHYSICS]")]
        public BoxCollider collisionBox; //CHARACTER COLLIDER WITH DEFAULT VALUES (DEFAULT = STAND UP)
        public Vector3 defaultBoxCenter; //DEFAULT COLLIDER CENTER VALUES (LOADED FROM collisionBox AT Awake)
        public Vector3 defaultBoxSize; //DEFAULT COLLIDER SIZE VALUES (LOADED FROM collisionBox AT Awake)
        public Vector3 crouchBoxCenter; //CROUCH COLLIDER CENTER VALUES
        public Vector3 crouchBoxSize; //CROUCH COLLIDER SIZE VALUES

        // 将重复声明的slopeCheckDistance变量提升为类成员变量
        private float slopeCheckDistance;

        //COLLISIONS ROOT (ONLY OBJECTS CHILDREN OF THIS TRANSFORM WILL BE USED FOR COLLISIONS)
        public Transform collisionsRoot;

        //CUSTOM GRAVITY (NOT USING CURRENT UNITY PROJECT GRAVITY)
        public float gravity = -9.81f;

        //GROUND RAYS COLLISION DETECTION (FROM COLLIDER BOTTOM BASE TO SUPPOSED GROUND LOCATION)
        private float distanceToGround;
        private Vector3[] groundRayOrigin;
        private void LoadGroundRays(BoxCollider boxCollider)
        {
            Vector3 halfExtents = boxCollider.size * 0.5f;

            groundRayOrigin = new Vector3[16];

            //BOTTOM BASE CORNER ORIGIN POINTS
            groundRayOrigin[0] = new Vector3(-halfExtents.x, 0, -halfExtents.z);
            groundRayOrigin[1] = new Vector3(halfExtents.x, 0, -halfExtents.z);
            groundRayOrigin[2] = new Vector3(halfExtents.x, 0, halfExtents.z);
            groundRayOrigin[3] = new Vector3(-halfExtents.x, 0, halfExtents.z);

            //BOTTOM BASE SIDE ORIGIN POINTS
            groundRayOrigin[4] = new Vector3(0, 0, -halfExtents.z);
            groundRayOrigin[5] = new Vector3(halfExtents.x, 0, 0);
            groundRayOrigin[6] = new Vector3(0, 0, halfExtents.z);
            groundRayOrigin[7] = new Vector3(-halfExtents.x, 0, 0);

            //ORIGIN POINTS BETWEEN BASE CORNER AND BASE SIDE ORIGIN POINTS
            groundRayOrigin[8] = (groundRayOrigin[0] + groundRayOrigin[4]) * 0.5f;
            groundRayOrigin[9] = (groundRayOrigin[1] + groundRayOrigin[4]) * 0.5f;
            groundRayOrigin[10] = (groundRayOrigin[1] + groundRayOrigin[5]) * 0.5f;
            groundRayOrigin[11] = (groundRayOrigin[2] + groundRayOrigin[5]) * 0.5f;
            groundRayOrigin[12] = (groundRayOrigin[2] + groundRayOrigin[6]) * 0.5f;
            groundRayOrigin[13] = (groundRayOrigin[3] + groundRayOrigin[6]) * 0.5f;
            groundRayOrigin[14] = (groundRayOrigin[3] + groundRayOrigin[7]) * 0.5f;
            groundRayOrigin[15] = (groundRayOrigin[0] + groundRayOrigin[7]) * 0.5f;
        }

        [Header("[CHARACTER SWITCH]")]
        public GameObject[] characterMeshesRoot; //GAME OBJECT ROOT OF CHARACTER MESHES (NOT WHOLE CHARACTER ROOT)
        public GameObject characterChangeVFX; //PARTICLE EFFECT WHEN SWITCHING CHARACTERS
        private int currentCharacter = 0; //CURRENT CHARACTER, BY DEFAULT 0

        [Header("[UI]")]
        public GameObject controlsWindow;

        // 射击相关变量
        [Header("[SHOOTING]")]
        public float shootDuration = 0.2f; // 射击状态持续时间(禁止移动时长)，可在Inspector中调整
        public Transform firePoint; // 枪口位置
        public GameObject bulletPrefab; // 子弹预制体
        public float bulletForce = 20f; // 子弹力度
        public float fireCooldown = 0.2f; // 射击冷却时间
        private float nextFireTime = 0f; // 下一次可射击时间
        public bool canShoot = true; // 是否可以射击
        
        // 射击音效
        public AudioSource shootAudioSource;
        public AudioClip shootSound;
        
        // 火花特效
        public GameObject muzzleFlashPrefab;

        ///INITIALIZE VARIABLES
        private void Awake()
        {
            //SET FRAME RATE LIMIT TO 60
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            //INITIALIZE INPUTS
            inputs = new InputSent();

            //LOAD DEFAULT COLLIDER SIZE (NEEDED WHEN CHANGING TO CROUCH COLLIDER SIZE)
            defaultBoxSize = collisionBox.size;
            defaultBoxCenter = collisionBox.center;

            //LOAD RAYS FOR GROUND COLLISION DETECTION FROM COLLIDER BASE TO FLOOR
            //(COLLIDER DOES NOT TOUCH GROUND, THIS IS INTENDED)
            LoadGroundRays(collisionBox);
            Vector3 origin = transform.position + (Vector3.up * collisionBox.center.y) + (-Vector3.up * (collisionBox.size.y * 0.5f));
            distanceToGround = (origin.y - transform.position.y) * 1.01f; //1.01f = MARGIN TO MAKE SURE RAYS TOUCH GROUND

            //LOAD DEFAULT MOVE SPEED (CHARACTER BY DEFAULT WILL RUN)
            moveSpeed = runSpeed;
            
            // 初始化枪口位置（如果未在Inspector中指定）
            if (firePoint == null)
            {
                // 创建枪口位置
                GameObject firePointObj = new GameObject("FirePoint");
                firePoint = firePointObj.transform;
                firePoint.SetParent(transform);
                
                // 设置在角色前方适当位置
                firePoint.localPosition = new Vector3(0.3f, 1.5f, 0.5f); // 右肩前方位置
                firePoint.localRotation = Quaternion.identity;
            }
            
            // Initialize the state machine
            InitializeStateMachine();
            
            //RANDOM IDLE ANIMATION
            RandomIdle();

            //INITIALIZE CHARACTER, HIDE BOTH ENABLE DEFAULT CHARACTER
            ChangeCharacter(currentCharacter);
        }

        ///CHANGE IDLE ANIMATION RANDOMLY
        private void RandomIdle()
        {
            int randomness = 6; //INCREASE THIS VALUE TO MAKE VARIANT IDLE LESS LIKELY TO APPEAR
            //IF randomValue IS 1 OR 2, THE CHARACTER WILL USE IDLE VARIANT INSTEAD OF DEFAULT IDLE
            int randomValue = Random.Range(0, randomness);
            animator[0].SetInteger("Idle Variant", randomValue);

            //RECURSIVELY CALL THIS FUNCTION AFTER 1 SECOND
            Invoke("RandomIdle", 1f);
        }

        ///DETECT PLAYER INPUTS AND MOVE CHARACTER
        private void Update()
        {
            //GET INPUTS
            GetInputs();

            //MOVE CHARACTER
            ControlCharacter();
            
            // Update current state
            if (currentState != null)
            {
                currentState.UpdateState();
            }
        }

        ///INPUTS ARE READ DIRECTLY FROM KEYBOARD OR MOUSE (TO AVOID CONFLICTS WITH CURRENT PROJECT INPUT CONFIGURATION) 
        private void GetInputs()
        {
            //RESET INPUTS TO READ NEW ONES
            inputs.Clear();

            //AVOID NEW INPUTS IF blockControls IS ENABLED
            if (blockControls)
            {
                animator[0].SetBool("Moving", false);
                return;
            }

            // 射击输入 - 直接处理射击
            if (Input.GetMouseButtonDown(0) && canShoot && Time.time >= nextFireTime)
            {
                // 设置射击标志
                inputs.shoot = true;
                
                // 保存当前移动状态
                Vector2 originalMovement = inputs.movement;
                
                // 强制停止移动
                blockControls = true;
                inputs.movement = Vector2.zero;
                
                // 记录目标旋转方向（摄像机朝向）
                if (mainCamera != null)
                {
                    Vector3 cameraForward = mainCamera.transform.forward;
                    cameraForward.y = 0;
                    cameraForward.Normalize();
                    Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                    
                    // 启动协程平滑旋转到摄像机朝向并射击
                    StartCoroutine(RotateAndShoot(targetRotation, originalMovement));
                }
                else
                {
                    // 如果没有摄像机引用，直接射击
                    FireWeapon();
                    StartCoroutine(DelayedRestoreMovement(originalMovement, shootDuration));
                }
                
                // 直接返回，跳过其他输入处理
                return;
            }

            //MOVEMENT
            float targetInputX = 0f;
            float targetInputY = 0f;

            if (Input.GetKey(KeyCode.D))
            {
                targetInputX = 1f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                targetInputX = -1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                targetInputY = 1f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                targetInputY = -1f;
            }

            inputs.movement = new Vector2(targetInputX, targetInputY);

            animator[0].SetBool("Moving", targetInputX != 0 || targetInputY != 0);

            inputX = Mathf.MoveTowards(inputX, targetInputX, movementInputSpeed * Time.deltaTime);
            inputY = Mathf.MoveTowards(inputY, targetInputY, movementInputSpeed * Time.deltaTime);

            animator[0].SetFloat("InputX", inputX);
            animator[0].SetFloat("InputY", inputY);


            //JUMP
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                inputs.jump = true;
            }

            //WALK
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                walk = !walk; // 切换走路模式

            }

            //CROUCH
            if(Input.GetKey(KeyCode.LeftAlt))
            {
                inputs.crouch = true;
            }

            //SPRINT
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift))
            {
                sprint = !sprint;
            }

            //SHOW CONTROLS WINDOW
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                controlsWindow.SetActive(!controlsWindow.activeInHierarchy);
            }
        }
        
        // 平滑旋转到目标方向并射击
        private IEnumerator RotateAndShoot(Quaternion targetRotation, Vector2 originalMovement)
        {
            // 旋转速度加快，使旋转更迅速但仍然平滑
            float rotationSpeed = turnSpeed * 3f;
            float rotationTime = 0f;
            float rotationDuration = 0.15f; // 旋转持续时间
            
            // 记录初始旋转
            Quaternion startRotation = transform.rotation;
            
            // 平滑旋转到目标方向
            while (rotationTime < rotationDuration)
            {
                rotationTime += Time.deltaTime;
                float t = Mathf.Clamp01(rotationTime / rotationDuration);
                
                // 使用平滑插值旋转
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                yield return null;
            }
            
            // 确保完全旋转到目标方向
            transform.rotation = targetRotation;
            
            // 射击
            FireWeapon();
            
            // 延迟恢复移动
            yield return new WaitForSeconds(shootDuration);
            
            // 恢复移动能力
            blockControls = false;
            inputs.movement = originalMovement;
            inputs.shoot = false;
            
            // Debug.Log("射击完成，恢复移动能力");
        }
        
        // 延迟恢复移动的协程
        private IEnumerator DelayedRestoreMovement(Vector2 originalMovement, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // 恢复移动能力
            blockControls = false;
            inputs.movement = originalMovement;
            inputs.shoot = false;
            
            // Debug.Log("射击完成，恢复移动能力");
        }

        ///MOVE CHARACTER BASED ON INPUTS
        private void ControlCharacter()
        {
            // Determine the appropriate state based on inputs and conditions
            CharacterState newState = characterState; // Start with current state
            
            // Check if character is grounded
            bool isGrounded = animator[0].GetBool("Grounded");
            
            // Debug ground state
            // Debug.Log("Grounded: " + isGrounded + ", CanJump: " + canJump);
            
            // Handle state transitions based on inputs and conditions
            if (isGrounded)
            {
                // Ground movement states
                if (inputs.jump && canJump)
                {
                    Debug.Log("Jump triggered! canJump: " + canJump);
                    newState = CharacterState.Jump;
                    // 确保跳跃状态被正确设置
                    ChangeState(newState);
                    // 避免在本帧内被其他状态覆盖
                    return;
                }
                else if (inputs.crouch)
                {
                    newState = CharacterState.Crouch;
                }
                else if (sprint && animator[0].GetBool("Moving") && crouchLayerWeight < 0.5f)
                {
                    newState = CharacterState.Sprint;
                }
                else if (walk && animator[0].GetBool("Moving"))
                {
                    newState = CharacterState.Walk;
                }
                else if (animator[0].GetBool("Moving"))
                {
                    newState = CharacterState.Run;
                }
                else
                {
                    newState = CharacterState.Idle;
                }
            }
            else
            {
                // Air states
                if (verticalVelocity < 0)
                {
                    newState = CharacterState.Fall;
                }
                else if (verticalVelocity > 0)
                {
                    // 确保上升过程中保持Jump状态
                    newState = CharacterState.Jump;
                }
                // Keep Jump state if already jumping and velocity is positive
            }
            
            // Update animation layer weights
            
            //WALK INPUT PRESSED
            if (walk)
            {
                if (isGrounded)
                {
                    walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 1f, Time.deltaTime * walkTransitionSpeed);
                }
            }
            else
            {
                walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 0f, Time.deltaTime * walkTransitionSpeed);
            }
            animator[0].SetLayerWeight(walkLayer, walkLayerWeight);

            //CROUCH INPUT PRESSED
            if (inputs.crouch)
            {
                if (isGrounded)
                {
                    crouchLayerWeight = Mathf.MoveTowards(crouchLayerWeight, 1f, Time.deltaTime * crouchTransitionSpeed);
                }
            }
            else
            {
                if (!IsCeilingAbove())
                {
                    crouchLayerWeight = Mathf.MoveTowards(crouchLayerWeight, 0f, Time.deltaTime * crouchTransitionSpeed);
                }
            }
            animator[0].SetLayerWeight(crouchLayer, crouchLayerWeight);

            //SPRINT INPUT PRESSED
            if (sprint)
            {
                //MAKE SURE SLIDING IS POSSIBLE WHILE SPRINTING
                timeMoving = 1f;
                if (isGrounded && animator[0].GetBool("Moving") && characterState != CharacterState.Crouch)
                {
                    sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 1f, Time.deltaTime * sprintTransitionSpeed);
                }
            }
            else
            {
                sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 0f, Time.deltaTime * sprintTransitionSpeed);
            }
            animator[0].SetLayerWeight(sprintLayer, sprintLayerWeight);
            
            // Change state if needed
            if (newState != characterState)
            {
                ChangeState(newState);
            }

            // 获取摄像机的前向和右向
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            // 忽略摄像机的垂直分量 (Y轴)
            cameraForward.y = 0;
            cameraRight.y = 0;

            // 标准化方向向量
            cameraForward.Normalize();
            cameraRight.Normalize();

            if (!animator[0].GetBool("Grounded"))
            {
                // 允许在空中使用当前输入控制移动，不再使用lastMovementInputs
                moveDirection = (cameraForward * inputs.movement.y + cameraRight * inputs.movement.x).normalized;

                // 在空中移动速度稍微降低
                float airControlFactor = 0.8f;

                if (moveDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

                    // 计算并应用移动速度
                    moveDirection = moveDirection * moveSpeed * airControlFactor * Time.deltaTime;
                    transform.position += moveDirection;
                    return; // 避免再次应用地面移动
                }
            }
            else
            {
                // 地面上的移动：使用摄像机朝向控制前后左右
                moveDirection = (cameraForward * inputs.movement.y + cameraRight * inputs.movement.x).normalized;

                // 如果移动方向有效，旋转角色使其朝向移动方向
                if (moveDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            // 计算并应用移动速度
            moveDirection = moveDirection * moveSpeed * Time.deltaTime;
            transform.position += moveDirection;
            }

            //INCREASE AMOUNT OF TIME CHARACTER WAS MOVING FOR SLIDING
            if (characterState == CharacterState.Walk || characterState == CharacterState.Run || characterState == CharacterState.Sprint)
            {
                if (timeMoving < 10f) //LIMIT TO AVOID INFINITE VALUE GROW
                {
                    timeMoving += Time.deltaTime;
                }
            }

            //RESET AMOUNT OF TIME CHARACTER WAS MOVING WHEN CROUCHING OR NO MOVEMENT INPUTS DETECTED
            if (inputs.movement == Vector2.zero || characterState == CharacterState.Crouch)
            {
                timeMoving = 0f;
            }

            // 重新添加跳跃输入处理
            if (inputs.jump && canJump && animator[0].GetBool("Grounded"))
            {
                //STORE LAST INPUTS TO USE IF allowInputWhileJumping IS FALSE
                lastMovementInputs = inputs.movement;
                
                // 明确允许跳跃时控制方向
                allowInputWhileJumping = true;

                //AVOID DOUBLE JUMP
                canJump = false;

                //AVOID GROUND CHECK TO ALLOW CHARACTER MOVE UP
                if (jumpCheckGroundAvoider != null)
                {
                    StopCoroutine(jumpCheckGroundAvoider);
                }
                jumpCheckGroundAvoider = JumpCheckGroundAvoider();
                StartCoroutine(jumpCheckGroundAvoider);

                //FORCE CHARACTER OUT OF GROUND (BECAUSE WE DISABLED GROUND CHECK IN THIS FRAME)
                animator[0].SetBool("Grounded", false);

                //PLAY JUMP ANIMATION
                animator[0].SetBool("Jump", true);

                //MOVE CHARACTER UP AT ApplyGravity FUNCTION - 增加跳跃力度
                verticalVelocity = jumpForce * 1.2f;
            }

            // 处理射击状态
            if (inputs.shoot && canShoot)
            {
                newState = CharacterState.Shoot;
                ChangeState(newState);
                return; // 避免其他状态覆盖
            }
        }

        ///CHECK COLLISIONS AND PHYSICS
        private void FixedUpdate()
        {
            // 检查碰撞
            CheckCollisions();

            // 获取当前是否在地面
                bool wasGrounded = animator[0].GetBool("Grounded");
            
            // 检查地面碰撞 (除非正在跳跃)
            bool grounded = false;
            if (!jump)
            {
                grounded = CheckGround();
                animator[0].SetBool("Grounded", grounded);
            }
            
            // 处理地面和空中状态变化
            if (grounded)
            {
                // 刚刚着地
                if (!wasGrounded)
                {
                    Debug.Log("刚刚着地，重置垂直速度和跳跃状态");
                    Land(); // 调用Land函数重置状态
                }
                
                // 确保在地面上时可以跳跃
                canJump = true;
            }
            else
            {
                // 如果不在地面且未跳跃，应用重力
                ApplyGravity();
                
                // 如果之前在地面上，设置土狼时间允许短暂跳跃
                if (wasGrounded && canJumpTimer == null)
                {
                    canJumpTimer = CanJumpTimer();
                    StartCoroutine(canJumpTimer);
                }
            }
            
            // 更新当前状态的物理
            if (currentState != null)
            {
                currentState.FixedUpdateState();
            }
        }

        ///CHECK FOR DETECTING IF CHARACTER CAN STAND UP FROM CROUCH OR NOT
        private bool IsCeilingAbove()
        {
            bool obstacleDetected = false;
            RaycastHit[] hits = Physics.BoxCastAll(transform.position + defaultBoxCenter, defaultBoxSize * 0.45f, Vector3.up, transform.rotation, 0.01f);

            foreach (RaycastHit hit in hits)
            {
                //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                if (hit.collider.transform.parent != collisionsRoot)
                {
                    continue;
                }
                obstacleDetected = true;
            }

            return obstacleDetected;
        }

        ///CHECK COLLISIONS TOUCHING CHARACTER BOX COLLIDER 
        public void CheckCollisions()
        {
            Vector3 penetrationDirection;
            float penetrationDistance;

            Collider[] colliders = Physics.OverlapBox(transform.position + collisionBox.center, collisionBox.size * 0.5f, transform.rotation);

            foreach (Collider collider in colliders)
            {
                //检查是否为角色自身的碰撞体，避免自碰撞
                if (collider == collisionBox)
                {
                    continue;
                }
                
                //不再限制只检测collisionsRoot子物体，允许检测所有碰撞体

                bool insideCollision = Physics.ComputePenetration(collisionBox, collisionBox.transform.position, collisionBox.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out penetrationDirection, out penetrationDistance);

                if (insideCollision)
                {
                    float angleWithDown = Vector3.Angle(penetrationDirection, Vector3.down);
                    if (angleWithDown < 10f) //DETECT IF COLLISION IS CEILING AND NOT A WALL
                    {
                        if (!animator[0].GetBool("Grounded"))
                        {
                            moveSpeed = walkSpeed; //REDUCE SPEED TO AVOID "FLYING EFFECT" UNDER CEILING
                        }

                        if (verticalVelocity > 0)
                        {
                            //RESET JUMP TO SIMULATE HIT WITH CEILING
                            verticalVelocity = 0f;
                            jump = false;
                        }
                    }

                    //MOVE CHARACTER OUTSIDE THE DETECTED COLLIDER WALL
                    transform.Translate(penetrationDirection * penetrationDistance, Space.World);
                }
            }
        }

        ///CHECK COLLISIONS MADE BY RAYS FROM BASE COLLIDER TO SUPPOSED GROUND LOCATION
        private bool CheckGround()
        {
            Vector3 origin = transform.position + (Vector3.up * collisionBox.center.y) + (-Vector3.up * (collisionBox.size.y * 0.5f));
            float rayDistance = distanceToGround * 1.2f;
            
            // 如果正在跳跃或垂直速度明显向上，直接返回false
            if (jump || verticalVelocity > 0.1f)
            {
                return false;
            }
            
            // 绘制调试射线
            Debug.DrawRay(origin, Vector3.down * rayDistance, Color.red, 0.1f);
            
            // 中心射线检测
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance))
            {
                // 跳过自身碰撞体
                if (hit.collider == collisionBox)
                {
                    return false;
                }
                
                // 判断斜坡角度
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle <= 45f)
                {
                    // 调整角色位置以贴合地面
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                    // Debug.Log($"检测到地面: {hit.collider.name}, 角度: {slopeAngle:F1}");
                    return true;
                }
            }
            
            // 多点射线检测
            for (int i = 0; i < groundRayOrigin.Length; i++)
            {
                Vector3 localRayOrigin = groundRayOrigin[i];
                Vector3 rotatedRayOrigin = transform.rotation * localRayOrigin;
                Vector3 rayOrigin = origin + rotatedRayOrigin;
                
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance))
                {
                    if (hit.collider == collisionBox)
                {
                    continue;
                }
                
                    float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                    if (slopeAngle <= 45f)
                    {
                        transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                        // Debug.Log($"周边射线检测到地面: {hit.collider.name}");
                        return true;
                    }
                }
            }
            
            // 未检测到地面时确保重力生效
            if (verticalVelocity >= 0)
            {
                verticalVelocity = -0.1f; // 强制下落
            }
            
            return false;
        }

        ///APPLY CUSTOM GRAVITY BY MOVING CHARACTER DOWN
        private void ApplyGravity()
        {
            // 始终应用重力，确保角色在空中时下落
            verticalVelocity += gravity * Time.fixedDeltaTime * 1.2f; // 增加重力系数使下落更明显
            
            // 限制最大下落速度
            verticalVelocity = Mathf.Max(verticalVelocity, -20f);
            
            // 应用垂直运动
            transform.Translate(Vector3.up * verticalVelocity * Time.fixedDeltaTime);
            
            // 输出调试信息
            Debug.Log($"应用重力: 垂直速度={verticalVelocity:F2}");
        }

        ///FUNCTION CALLED WHEN CHARACTER IS GROUNDED
        private void Land()
        {
            // 重置垂直速度和跳跃状态
            verticalVelocity = 0f;
            
            // 停止所有跳跃相关协程
            if (jumpCheckGroundAvoider != null)
            {
                StopCoroutine(jumpCheckGroundAvoider);
                jumpCheckGroundAvoider = null;
            }
            
            // 允许跳跃和停止空中控制
            canJump = true;
            allowInputWhileJumping = false;
            
            // 重置动画状态
            animator[0].SetBool("Jump", false);
            
            // 停止土狼时间计时器
            if (canJumpTimer != null)
            {
                StopCoroutine(canJumpTimer);
                canJumpTimer = null;
            }
        }


        ///CHARACTER SWITCH///
        private void ChangeCharacter(int newCharacter)
        {
            currentCharacter = newCharacter;
            for (int i = 0; i < characterMeshesRoot.Length; i++)
            {
                characterMeshesRoot[i].SetActive(false);
            }

            characterMeshesRoot[currentCharacter].SetActive(true);
        }

        // 射击方法
        public void FireWeapon()
        {
            if (Time.time < nextFireTime)
                return;
            
            // 设置下一次可射击时间
            nextFireTime = Time.time + fireCooldown;
            
            // 播放射击音效
            if (shootAudioSource != null && shootSound != null)
            {
                shootAudioSource.PlayOneShot(shootSound);
            }
            
            // 显示枪口火花
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(muzzleFlash, 0.1f); // 短暂显示后销毁
            }
            
            // 射击检测和子弹生成
            if (mainCamera != null && firePoint != null)
            {
                // 从摄像机获取水平方向，但射线从firePoint发出
                Camera cam = mainCamera.GetComponent<Camera>();
                if (cam == null) {
                    Debug.LogError("摄像机没有Camera组件!");
                    return;
                }
                
                // 获取摄像机前方方向，但只取其水平分量
                Vector3 cameraDirection = mainCamera.forward;
                Vector3 horizontalDirection = new Vector3(cameraDirection.x, 0, cameraDirection.z).normalized;
                
                // 创建射线，起点为firePoint位置
                Ray ray = new Ray(firePoint.position, horizontalDirection);
                
                // 绘制水平射线
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 5f);
                Debug.Log("水平射击射线: 从高度" + firePoint.position.y + "发出，方向=" + ray.direction);
                
                // 获取场景中所有碰撞体信息
                RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
                
                // 按距离排序
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                
                bool hitFound = false;
                
                // 输出所有找到的物体，帮助调试
                if (hits.Length > 0) {
                    Debug.Log("射线检测到 " + hits.Length + " 个物体");
                } else {
                    Debug.Log("射线没有检测到任何物体!");
                }
                
                foreach (RaycastHit hit in hits)
                {
                    // 检查是否击中自己或摄像机
                    bool isPlayer = hit.transform.gameObject == gameObject || hit.transform.IsChildOf(transform);
                    bool isCamera = hit.transform.gameObject == mainCamera.gameObject || hit.transform.IsChildOf(mainCamera.transform);
                    
                    if (!isPlayer && !isCamera)
                    {
                        hitFound = true;
                        Debug.Log("击中有效物体: " + hit.transform.name + " 在坐标: " + hit.point);
                        
                        // 检查目标是否实现了IDamageable接口
                        IDamageable damageable = hit.transform.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            // 对目标造成伤害
                            damageable.TakeDamage(1); // 造成1点伤害
                            Debug.Log("对" + hit.transform.name + "造成伤害");
                        }
                        
                        // 创建击中特效
                        ShowHitEffect(hit);
                        
                        // 创建子弹飞向目标
                        if (bulletPrefab != null)
                        {
                            // 创建子弹并设置属性
                            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                            
                            // 禁用子弹上的所有碰撞体，避免自碰撞
                            Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>();
                            foreach (Collider col in bulletColliders)
                            {
                                col.enabled = false;
                            }
                            
                            Rigidbody rb = bullet.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                // 禁用重力
                                rb.useGravity = false;
                                
                                // 使子弹直接飞向目标点
                                Vector3 direction = (hit.point - firePoint.position).normalized;
                                float distance = Vector3.Distance(hit.point, firePoint.position);
                                
                                // 根据距离计算速度，确保子弹快速到达
                                float speed = Mathf.Max(bulletForce, distance * 2f);
                                rb.velocity = direction * speed;
                                
                                // 设置子弹朝向
                                bullet.transform.forward = direction;
                            }
                            
                            // 2秒后销毁子弹
                            Destroy(bullet, 2f);
                        }
                        
                        break; // 只处理第一个有效碰撞
                    }
                }
                
                // 如果没有击中任何有效物体
                if (!hitFound)
                {
                    Debug.Log("射线未击中任何有效物体，射向远处");
                    
                    // 创建一个远处的目标点进行可视化
                    Vector3 farPoint = ray.origin + ray.direction * 100f;
                    
                    // 在远点创建一个临时标记
                    GameObject farMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    farMarker.transform.position = farPoint;
                    farMarker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    farMarker.GetComponent<Renderer>().material.color = Color.blue;
                    Destroy(farMarker, 2f);
                    
                    // 创建子弹射向远处
                    if (bulletPrefab != null)
                    {
                        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                        
                        // 禁用子弹上的所有碰撞体
                        Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>();
                        foreach (Collider col in bulletColliders)
                        {
                            col.enabled = false;
                        }
                        
                        Rigidbody rb = bullet.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.useGravity = false;
                            rb.velocity = ray.direction * bulletForce;
                            bullet.transform.forward = ray.direction;
                        }
                        
                        Destroy(bullet, 2f);
                    }
                }
            }
            else
            {
                Debug.LogError("主摄像机或射击点未设置，无法进行射击检测");
            }
            
            Debug.Log("开火！");
        }
        
        // 显示击中特效
        private void ShowHitEffect(RaycastHit hit)
        {
            // 这里可以添加击中特效，例如火花、破碎、弹孔等
            Debug.Log("创建击中特效于: " + hit.point);
            
            // 创建特效父物体
            GameObject hitEffect = new GameObject("HitEffect");
            hitEffect.transform.position = hit.point;
            hitEffect.transform.rotation = Quaternion.LookRotation(hit.normal); // 让特效朝向表面法线
            
            // 添加一个明显的视觉指示器
            GameObject visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualIndicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            visualIndicator.transform.position = hit.point;
            visualIndicator.transform.parent = hitEffect.transform;
            
            // 创建一个材质并设置红色
            Renderer renderer = visualIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.red;
            }
            
            // 如果碰撞的是Rigidbody，添加力
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(mainCamera.transform.forward * 5f, hit.point, ForceMode.Impulse);
            }
            
            // 2秒后销毁击中特效
            Destroy(hitEffect, 2f);
        }

        void Start()
        {
            // 隐藏鼠标光标并锁定到游戏窗口中心
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 禁用输入法
            Input.imeCompositionMode = IMECompositionMode.Off;
        }
    }


    //DEFINITION OF CHARACTER POSSIBLE STATES
    public enum CharacterState
    {
        Idle,
        Walk,
        Run,
        Sprint,
        Jump,
        Fall,
        Crouch,
        Shoot,
    }

    //CLASS FOR HANDLING PLAYER INPUTS
    public class InputSent
    {
        public Vector2 movement;
        public float turn;
        public bool jump;
        public bool walk;
        public bool runSlide;
        public bool roll;
        public bool crouch;
        public bool sprint;
        public bool shoot;

        public void Clear()
        {
            movement = Vector2.zero;
            turn = 0f;
            jump = false;
            walk = false;
            runSlide = false;
            roll = false;
            crouch = false;
            sprint = false;
            shoot = false;
        }
    }

}