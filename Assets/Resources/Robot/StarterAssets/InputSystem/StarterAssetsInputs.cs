using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool attack;
		public bool dead;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		// 攻击持续时间
		public float attackDuration = 0.5f;
		
		// 当前正在执行的攻击协程
		private Coroutine attackCoroutine;

		private void Awake()
		{
			// 确保初始状态下攻击和死亡标志为false
			attack = false;
			dead = false;
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnAttack(InputValue value)
		{
			// 只在按下时触发攻击
			if (value.isPressed)
			{
				TriggerAttack();
			}
		}

		public void OnDead()
		{
			DeadInput(true);
		}
#endif

		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void DeadInput(bool newDeadState)
		{
			dead = newDeadState;
			
			// 只在设置为true时输出调试信息
			if (newDeadState)
			{
				Debug.Log("Dead设置为True", this);
			}
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

		private void Update()
		{
			// 检测按下1键激活死亡状态
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				DeadInput(true);
			}
			
			// 检测鼠标左键点击激活攻击
			if (Input.GetMouseButtonDown(0))
			{
				TriggerAttack();
			}
		}
		
		// 触发攻击行为
		public void TriggerAttack()
		{
			// 如果已经有攻击协程在运行，先停止它
			if (attackCoroutine != null)
			{
				StopCoroutine(attackCoroutine);
			}
			
			// 启动新的攻击协程
			attackCoroutine = StartCoroutine(AttackSequence());
		}
		
		// 攻击序列协程
		private IEnumerator AttackSequence()
		{
			// 设置攻击状态为true
			SetAttackState(true);
			
			// 等待指定的攻击持续时间
			yield return new WaitForSeconds(attackDuration);
			
			// 设置攻击状态为false
			SetAttackState(false);
			
			// 清除协程引用
			attackCoroutine = null;
		}
		
		// 设置攻击状态并输出日志
		private void SetAttackState(bool newState)
		{
			// 只有状态发生变化时才处理
			if (attack != newState)
			{
				attack = newState;
				
				if (newState)
				{
					Debug.Log("Attack设置为True", this);
				}
				else
				{
					Debug.Log("Attack设置为False", this);
				}
			}
		}
	}
}