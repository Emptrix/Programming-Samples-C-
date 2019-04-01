using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SkillAnimationType {SingleProjectile, Explosive, ChargeBeam, OneHandBlast}
public abstract class AutoLevelSkillWielder : MonoBehaviour {

	[Header("Required")]
	[SerializeField]
	EndlessRunnerGUI mainGUI;
	[SerializeField]
	private TunnelRaiderSkill defaultSkill;
	private int singleProjectileAnimationIndex;
	[SerializeField]
	private Transform projectileLaunchNode1;
	[SerializeField]
	private Transform projectileLaunchNode2;
	private bool isAttacking;
	private float attackInterval;
	private bool isConjuring;
	private bool isSkillAnimationPlaying;
	private bool isContinuouslyAttacking;
	private float skillAnimationDuration;
	private bool cancellable;
	[HideInInspector]
	public int ammunitionAmount;
	[HideInInspector]
	public int maxAmmunitionAmount;
	private TunnelRaiderSkill currentSkill;
	[SerializeField]
	private List<LaunchSkillVFXS> launchSkillVFXS = new List<LaunchSkillVFXS>();
	private List<ContinuousSkillVFXS> continuousSkillVFXS = new List<ContinuousSkillVFXS>();

	//Use this for beam attack a.i control
	private bool continuousAttackTrigger = false;

	//Used for skills with aim assist or trajectory
	private Transform target;

	private Transform _projectileEmitter;
	public Transform projectileEmitter {
		get {
			if (null == _projectileEmitter)
				LoadAssets ();
			return _projectileEmitter;
		}
	}

	private Animator _animator;
	private Animator animator {
		get {
			if (null == _animator)
				_animator = GetComponent<Animator> ();
			return _animator;
		}
	}

	public void LoadAssets() {
		foreach (TunnelRaiderAssetTag asset in GetComponentsInChildren<TunnelRaiderAssetTag>()) {
			if (asset.assetTag == AssetTag.projectileEmitter)
				_projectileEmitter = asset.transform;
		}
	}

	void Start() {
		OnStart ();
	}

	public abstract void OnStart ();

	//Passively process the variables
	public void Process() {
		if (isAttacking) {
			attackInterval -= Time.deltaTime;
			if (attackInterval <= 0) {
				SetIsAttacking (false);
			}
		}
		if (isSkillAnimationPlaying) {
			skillAnimationDuration -= Time.deltaTime;
			if (skillAnimationDuration <= 0) {
				isSkillAnimationPlaying = false;
			}
		}
		OnProcess ();
	}

	public virtual void OnProcess () {
	}
		
	public abstract void LaunchSkill();

	public abstract void SkillSwapConditions();

	public virtual void OnSwapSkill() {
	}

	public void SetCurrentSkill (TunnelRaiderSkill skill) {
		if (null != skill) {
			if (currentSkill != skill)
				ammunitionAmount = 0;

			OnSwapSkill ();
			StopContinuousSkillVFX ();
			AttackCanceller ();

			currentSkill = skill;
			ammunitionAmount += currentSkill.ammunitionAmount;
			maxAmmunitionAmount = ammunitionAmount;

			/*
			if (null != mainGUI)
			if (null != mainGUI.GetAmmunitionSlider ()) {
				mainGUI.GetAmmunitionSlider ().InitSlider (maxAmmunitionAmount, maxAmmunitionAmount);
				mainGUI.SetAmmunition (currentSkill.skillName, ammunitionAmount, maxAmmunitionAmount);
			}
			*/

			if (null != GetMainGUI ()) {
				if (null != GetMainGUI ().GetSkillBar () && !GetMainGUI().GetSkillBar().IsCustomUsage()) {
					GetMainGUI ().GetSkillBar ().Init (GetCurrentSkill ().icon, GetCurrentSkill ().barColor, maxAmmunitionAmount);
					GetMainGUI ().GetSkillBar ().SetSkillBarValue (ammunitionAmount);
					GetMainGUI ().GetSkillBar ().PlaySwapEffect ();
				}
                /*
				if (null != GetMainGUI ().GetAttackButton ())
					if (currentSkill.isContinousAttack)
						GetMainGUI ().GetGlowPad ().SetAttackPadText ("HOLD ATTACK");
					else
						GetMainGUI ().GetGlowPad ().SetAttackPadText ("ATTACK");
                        */
			}
		}
	}

	public void AttackCanceller() {
		if (GetIsCancellable ()) {
			StopContinuousSkillVFX ();
			animator.speed = 1f;
			SetIsCancellable (false);
			SetSkillAnimationDuration (0);
			SetAttackInterval (0);
			animator.SetTrigger ("attackCanceller");
		}
	}

	public void SetIsAttacking (bool value) {
		isAttacking = value;
	}

	public void SetIsConjuring (bool value) {
		isConjuring = value;
	}

	public bool GetIsAttacking () {
		return isAttacking;
	}

	public bool GetIsConjuring () {
		return isConjuring;
	}

	public void SetAttackInterval(float delay) {
		attackInterval = delay;
	}

	public bool IsSkillAnimationPlaying() {
		return isSkillAnimationPlaying;
	}

	public void SetIsSkillAnimationPlaying (bool value) {
		isSkillAnimationPlaying = value;
	}

	public void SetSkillAnimationDuration(float value) {
		skillAnimationDuration = value;
	}
		
	public bool GetIsCancellable() {
		return cancellable;
	}

	public void SetIsCancellable(bool value) {
		cancellable = value;
	}

	public Animator GetAnimator() {
		return animator;
	}

	public void AddAttackInterval(float duration) {
		attackInterval += duration;
	}

	public void AddSkillAnimationDuration(float duration) {
		skillAnimationDuration += duration;
	}

	public TunnelRaiderSkill GetCurrentSkill() {
		return currentSkill;
	}

	public EndlessRunnerGUI GetMainGUI() {
		return mainGUI;
	}

	public float GetAttackInterval() {
		return attackInterval;
	}

	public void SetIsContinuouslyAttacking(bool value) {
		isContinuouslyAttacking = value;
	}

	public bool GetIsContinuouslyAttacking() {
		return isContinuouslyAttacking;
	}

	public virtual void ConsumeAmmunition () {
	}

	public TunnelRaiderSkill GetDefaultSkill() {
		return defaultSkill;
	}

	public virtual void PlaySingleProjectileAnimation(TunnelRaiderSkill skill) {
		if (singleProjectileAnimationIndex == 2)
			singleProjectileAnimationIndex = 0;

		if (singleProjectileAnimationIndex == 0)
			GetAnimator ().SetTrigger ("attack");
		else
			GetAnimator ().SetTrigger ("attack_01");

		//PlayLaunchVFX (skill);
		singleProjectileAnimationIndex++;
	}

	public virtual void PlayExplosiveProjectileAnimation(TunnelRaiderSkill skill) {
		animator.SetTrigger ("attack_02");
		//PlayLaunchVFX (skill);
	}

	public virtual void PlayOneHandBlastProjectileAnimation() {
		animator.SetTrigger ("attack_03");
	}

	public List<LaunchSkillVFXS> GetLaunchSkillVFXS() {
		return launchSkillVFXS;
	}

	public List<ContinuousSkillVFXS> GetContinuousSkillVFXS() {
		return continuousSkillVFXS;
	}

	public void PlayLaunchVFX() {
		if (null != currentSkill.launchVFX) {
			if (GetLaunchSkillVFXS ().Count <= currentSkill.iD) {
				//Fill up list first with empty items
				int currentSlotsAmount = GetLaunchSkillVFXS ().Count;

				for (int index = currentSkill.iD; index >= currentSlotsAmount; index--)
					GetLaunchSkillVFXS ().Add (new LaunchSkillVFXS());
			}

			if (GetLaunchSkillVFXS ()[currentSkill.iD].launchVFXSData.Count <= 0) {
				foreach (LaunchSkillVFXData data in currentSkill.launchVFX.launchVFXSData) {
					Transform designatedNode = null;
					if (data.designatedNode == DesignatedLaunchParticleNode.Node1)
						designatedNode = projectileLaunchNode1;
					else if (data.designatedNode == DesignatedLaunchParticleNode.Node2)
						designatedNode = projectileLaunchNode2;

					ParticleSystem currentPs = Instantiate (data.ps.gameObject, Vector3.zero, transform.rotation, designatedNode).GetComponent<ParticleSystem> ();
					LaunchSkillVFXData currentData = new LaunchSkillVFXData ();
					currentData.designatedNode = data.designatedNode;
					currentData.ps = currentPs;

					Debug.Log ("projectile Node: " + currentData.designatedNode);

					GetLaunchSkillVFXS () [currentSkill.iD].launchVFXSData.Add (currentData);

					currentData.ps.transform.localPosition = Vector3.zero;
				}
			}

			foreach (LaunchSkillVFXData data in GetLaunchSkillVFXS()[currentSkill.iD].launchVFXSData)
				data.ps.Play ();

			PlayChargeSkillSFX ();
		}
	}

	public void PlayContinuousSkillVFX() {
		if (null != currentSkill.continuousVFX) {
			if (GetContinuousSkillVFXS ().Count <= currentSkill.iD) {
				//Fill up list first with empty items
				int currentSlotsAmount = GetContinuousSkillVFXS ().Count;

				for (int index = currentSkill.iD; index >= currentSlotsAmount; index--)
					GetContinuousSkillVFXS ().Add (new ContinuousSkillVFXS());
			}

			if (GetContinuousSkillVFXS ()[currentSkill.iD].continuousVFXSData.Count <= 0) {
				foreach (ContinuousSkillVFXData data in currentSkill.continuousVFX.continuousVFXSData) {
					Transform designatedNode = null;
					if (data.designatedNode == DesignatedLaunchParticleNode.Node1)
						designatedNode = projectileLaunchNode1;
					else if (data.designatedNode == DesignatedLaunchParticleNode.Node2)
						designatedNode = projectileLaunchNode2;

					ParticleSystem currentPs = Instantiate (data.ps.gameObject, Vector3.zero, transform.rotation, designatedNode).GetComponent<ParticleSystem> ();
					ContinuousSkillVFXData currentData = new ContinuousSkillVFXData ();
					currentData.designatedNode = data.designatedNode;
					currentData.ps = currentPs;

					GetContinuousSkillVFXS () [currentSkill.iD].continuousVFXSData.Add (currentData);
					currentData.ps.transform.localPosition = Vector3.zero;
				}
			}

			foreach (ContinuousSkillVFXData data in GetContinuousSkillVFXS()[currentSkill.iD].continuousVFXSData)
				foreach (ParticleSystem ps in data.ps.GetComponentsInChildren<ParticleSystem>())
					ps.Play ();
		}
	}

	public void StopContinuousSkillVFX() {
		if (null != currentSkill && GetContinuousSkillVFXS().Count > 0)
			foreach (ContinuousSkillVFXData data in GetContinuousSkillVFXS()[currentSkill.iD].continuousVFXSData)
				foreach (ParticleSystem ps in data.ps.GetComponentsInChildren<ParticleSystem>())
					ps.Stop ();
	}

	public void PlayChargeSkillSFX() {
        if (null != GetCurrentSkill().soundEffects.chargeSFX)
		Sounds.Play (GetCurrentSkill().soundEffects.chargeSFX.name);
	}

	//Used by continuous attack skills
	public bool IsAttackButtonDown() {
		Debug.Log ("isAttackbuttondown");
		if (null != mainGUI)
			if ((Input.GetKey (KeyCode.Z) || mainGUI.GetAttackButton ().GetPointerDown () || continuousAttackTrigger) && ammunitionAmount > 0)
				return true;
			else
				return false;

		if (continuousAttackTrigger)
			return true;
		
		return false;
	}

	public void SetTarget(Transform target) {
		this.target = target;
	}

	public Transform GetTarget() {
		return target;
	}
}
