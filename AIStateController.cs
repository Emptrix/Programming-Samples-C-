using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TunnelRaiderMonClass {closeQuater, midRange, longRange}
public enum TunnelRaiderPoolAction {register, unregister}
public enum TunnelRaiderPoolCat {active, inActive}

public enum TunnelMonFacing {Left, Right}
public enum TargetDirection {Left, Right}
public abstract class AIStateController : MonoBehaviour {

	[Header("Required")]
	[SerializeField]
	private AIState currentState;
	[SerializeField]
	private AIState remainState;

	[Header("Monitoring Purpose")]
	[SerializeField]
	private TunnelRaiderMonData monData;
	[SerializeField]
	private float currentHealth;

	private Vector3 currentTargetFloatPosition;

	private AutoLevelSkillWielder _skillWielder;
	public AutoLevelSkillWielder skillWielder {
		get {
			if (null == _skillWielder)
				_skillWielder = GetComponent<AutoLevelSkillWielder> ();
			return _skillWielder;
		}
	}
	private Collider2D _col2D;
	public Collider2D col2D {
		get {
			if (null == _col2D)
				_col2D = GetComponent<Collider2D> ();
			return _col2D;
		}
	}

	private Rigidbody2D _rb2D;
	public Rigidbody2D rb2D { 
		get {
			if (null == _rb2D)
				_rb2D = GetComponent<Rigidbody2D>();
			return _rb2D;
		}
	}
	private Animator _animator;
	public Animator animator {
		get {
			if (null == _animator)
				_animator = GetComponent<Animator> ();
			return _animator;
		}
	}
	private Transform _groundDetection;
	public Transform groundDetection { 
		get {
			if (null == _groundDetection)
				LoadAssets ();
			return _groundDetection;
		}
	}
	private Transform _aggroDetection;
	public Transform aggroDetection {
		get {
			if (null == _aggroDetection)
				LoadAssets ();
			return _aggroDetection;
		}
	}
	private ParticleSystem _pS;
	public ParticleSystem pS {
		get {
			if (null == _pS)
				_pS = GetComponentInChildren<ParticleSystem> ();
			return _pS;
		}
	}
	private AIStateLogger _stateLogger;
	private AIStateLogger stateLogger {
		get {
			if (null == _stateLogger)
				_stateLogger = GetComponent<AIStateLogger> ();
			return _stateLogger;
		}
	}
	private AutoLevelGameManager gameManager;

	private TunnelRaiderVFX _vFX;
	private TunnelRaiderVFX vFX {
		get {
			if (null == _vFX)
				_vFX = GetComponent<TunnelRaiderVFX> ();
			return _vFX;
		}
	}

	//private TunnelRaiderMonSpawner monSpawner;
	private AIStateControllerSpawner mobSpawner;

	private TunnelMonFacing currentlyFacing;

	public Transform target;
	private bool isTransitioning;
	public float idleDuration;
	[SerializeField]
	private bool isGoose;
	private SliderAsset _healthBar;
	private SliderAsset healthBar {
		get {
			if (null == _healthBar)
				_healthBar = GetComponentInChildren<SliderAsset> ();
			return _healthBar;
		}
	}

	private bool isAggro;
	private bool isHurt;
	private bool isDead;
	public float blinkDuration;
	//private bool isTakingDamage;
	private TunnelRaiderMonClass monClass;

	public void Init (AutoLevelGameManager gameManager, TunnelRaiderMonData monData, TunnelRaiderMonClass monClass, bool isGoose) {
		this.gameManager = gameManager;
		this.monData = monData;
		this.currentHealth = monData.health;
		this.isGoose = isGoose;
		skillWielder.SetCurrentSkill (monData.basicAttack);
		healthBar.InitSlider (currentHealth, currentHealth);
		isDead = false;
		this.monClass = monClass;

		int randomDirection = Random.Range (0, 1);

		if (randomDirection == 1)
			Rotate (TargetDirection.Left);
		else
			Rotate (TargetDirection.Right);
	}
	/*
	public void SetMonSpawner(TunnelRaiderMonSpawner monSpawner) {
		this.monSpawner = monSpawner;
	}
*/
	public void SetMobSpawner(AIStateControllerSpawner mobSpawner) {
		this.mobSpawner = mobSpawner;
	}

	void FixedUpdate() {
		if (!gameManager.GetGameStatus()) {
			currentState.CheckTransitions (this);
			currentState.DoActions (this);
			//currentState.UpdateState (this);
			skillWielder.Process ();
		}
		Animation ();
		healthBar.UpdateValue ();
		IsTakingDamage ();

		OnUpdate ();
	}

	public virtual void OnUpdate() {}

	public void LoadAssets() {
		foreach (TunnelRaiderAssetTag asset in GetComponentsInChildren<TunnelRaiderAssetTag>()) {
			if (asset.assetTag == AssetTag.groundDetection)
				_groundDetection = asset.transform;
			if (asset.assetTag == AssetTag.aggroDetection)
				_aggroDetection = asset.transform;
		}
	}
		
	public void LaunchBaseAttack() {
		GetMonData ().basicAttack.Skill(skillWielder);
		animator.speed = 1f;
	}

	public TunnelRaiderMonData GetMonData() {
		return monData;
	}

	public TunnelMonFacing GetCurrentlyFacing() {
		return currentlyFacing;
	}

	public void SetCurrentlyFacing(TunnelMonFacing facing) {
		currentlyFacing = facing;
	}

	public void TransitionToState(AIState nextState) {
		if (nextState != remainState) {
			currentState = nextState;
			//Debug.Log(currentState.name);
		}
	}

	public void SetTarget(Transform target) {
		if (null != skillWielder) {
			skillWielder.SetTarget (target);
			//Debug.Log (skillWielder.GetTarget ().name);
		}
		
		this.target = target;
	}

	public Transform GetTarget() {
		return target;
	}

	void OnCollisionEnter2D(Collision2D col) {
		if (col.collider.gameObject == this.gameObject)
			return;
		if (!gameManager.GetGameStatus ()) {
			AutoLevelProjectile projectile = col.gameObject.GetComponent<AutoLevelProjectile> ();
			if (null != projectile) {
				//if (!projectile.GetTunnelRaiderSkill ().isContinousAttack) {
					//if (projectile.CheckIfHit (col2D)) {
						if (!isGoose) {
							if (!skillWielder.GetIsAttacking ()) {
								SetIsHurt (true);
								animator.SetTrigger ("hurt");
							}					
							DamageMonsterBySkill (projectile.GetDamage (), projectile.GetTunnelRaiderSkill ());
							SetIsTakingDamage (true);
							SetAggroStatus (true, projectile.GetOwner ());
							//Sounds.Play ("Monster_Hit");
							Sounds.Play (projectile.GetTunnelRaiderSkill().soundEffects.disperseSFX);

						} else if (projectile.GetDamage () >= 4)
							DamageMonsterBySkill (projectile.GetDamage (), projectile.GetTunnelRaiderSkill ());
					//}
				//}

			}

			OnCustomCollisionEnterAction (col);
		}
	}

	public virtual void OnCustomCollisionEnterAction(Collision2D col) {
	}
	public virtual void OnCustomCollisionExitAction(Collision2D col) {
	}

	void OnTriggerEnter2D(Collider2D col) {
		if (col.gameObject == this.gameObject)
			return;

		if (!gameManager.GetGameStatus ()) {
			AutoLevelProjectile projectile = col.gameObject.GetComponent<AutoLevelProjectile> ();
			if (null != projectile)
				Sounds.Play (projectile.GetTunnelRaiderSkill ().soundEffects.disperseSFX);
		}
	}
	void OnTriggerStay2D(Collider2D col) {
		if (col.gameObject == this.gameObject)
			return;
		if (!gameManager.GetGameStatus ()) {
			AutoLevelProjectile projectile = col.gameObject.GetComponent<AutoLevelProjectile> ();
			if (null != projectile) {
				//if (projectile.GetTunnelRaiderSkill ().isContinousAttack) {
					//if (projectile.CheckIfHit (col2D)) {
						if (!isGoose) {
							if (!skillWielder.GetIsAttacking ()) {
								SetIsHurt (true);
								animator.SetTrigger ("hurt");
							}
							DamageMonsterBySkill (projectile.GetDamage (), projectile.GetTunnelRaiderSkill ());

							SetIsTakingDamage (true);
							SetAggroStatus (true, projectile.GetOwner ());

						} else if (projectile.GetDamage () >= 4)
							DamageMonsterBySkill (projectile.GetDamage (), projectile.GetTunnelRaiderSkill ());
					//}
				//}
			}
		}
	}

	void DamageMonsterBySkill(float damage, TunnelRaiderSkill incomingSkill) {
		this.currentHealth -= damage;
		healthBar.Damage (damage);
		if (null != gameManager.GetSkillHitVFXFactory())
		gameManager.GetSkillHitVFXFactory ().SpawnHitVFX (incomingSkill, transform);
			
		if (currentHealth <= 0 && !isDead) {
			isDead = true;
			if (!isGoose )
				Dead ();
			if (isGoose) {
				rb2D.isKinematic = false;
				gameManager.GameOver (true);
			}
		}
	}

	public void DamageMonsterByObstacle(EndlessPlatformerObstacleData incomingObstacleData) {
		this.currentHealth -= incomingObstacleData.damage;
		healthBar.Damage (incomingObstacleData.damage);

		if (null != gameManager.GetObstacleHitVFXFactory ())
			gameManager.GetObstacleHitVFXFactory ().SpawnHitVFX (incomingObstacleData, transform);

		if (currentHealth <= 0 && !isDead) {
			isDead = true;
			Dead ();
		}
	}

	public void Dead() {
		GetMobSpawner().RegisterMonsterToPool (monClass, this, TunnelRaiderPoolAction.unregister);
		GetMobSpawner().Despawn (this);

		OnDead ();
	}

	public virtual void OnDead () {}

	public Vector3 GetCurrentTargetFloatPosition() {
		return currentTargetFloatPosition;
	}

	public void SetCurrentTargetFloatPosition(Vector3 value) {
		currentTargetFloatPosition = value;
	}
	/*
	public TunnelRaiderMonSpawner GetMonSpawner() {
		return monSpawner;
	}
	*/
	public AIStateControllerSpawner GetMobSpawner() {
		return mobSpawner;
	}

	public abstract void GhostMode (bool active);

	public bool IsTransitioning() {
		return isTransitioning;
	}

	public void SetTransitioning(bool value) {
		isTransitioning = value;
	}

	private void Animation() {
		animator.SetFloat ("velocityX", Mathf.Abs(rb2D.velocity.x));
	}

	public bool AnimatorIsPlaying(){
		return animator.GetCurrentAnimatorStateInfo(0).length >
			animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
	}

	public bool GetAggroStatus() {
		return isAggro;
	}

	public void SetAggroStatus(bool value, Transform target) {
		if (null != target)
			SetTarget (target);
		
		isAggro = value;
	}

	public TunnelRaiderVFX GetVFX() {
		return vFX;
	}

	public bool GetIsHurt() {
		return isHurt;
	}

	public void SetIsHurt(bool value) {
		isHurt = value;
	}

	public SliderAsset GetHealthBar() {
		return healthBar;
	}

	public void Rotate(TargetDirection dir) {
		if (dir == TargetDirection.Left) {
			GetHealthBar ().transform.localEulerAngles = new Vector3 (0, -180, 0);
			transform.eulerAngles = new Vector3 (0, -180, 0);
			SetCurrentlyFacing (TunnelMonFacing.Left);
		} else if (dir == TargetDirection.Right) {
			GetHealthBar ().transform.localEulerAngles = new Vector3 (0, 0, 0);
			transform.eulerAngles = new Vector3 (0, 0, 0);
			SetCurrentlyFacing (TunnelMonFacing.Right);
		}
	}

	public AutoLevelGameManager GetGameManager() {
		return gameManager;
	}

	public abstract void IsTakingDamage ();

	public void SetIsTakingDamage(bool value) {
		//isTakingDamage = value;

		if (value)
			blinkDuration = 0.1F;
	}

	public float GetCurrentHealth() {
		return currentHealth;
	}

	public int GetDefaultHealth() {
		return monData.health;
	}

	public AIStateLogger GetStateLogger() {
		return stateLogger;
	}
}