using EFT.Animations;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class SwaySystem
{
	private const float VanillaMotionIntensity = 0.45f;

	private static float _appliedTremor = -1f;

	// --- Procedural mouse-tracking sway state (non-ADS) ---
	// The weapon acts like a tracking system: it follows the mouse with a
	// spring lag. When the mouse moves, the weapon swings in that direction
	// proportional to the mouse rate, then springs back to center when the
	// mouse stops. This replaces static sway with dynamic, mouse-driven sway.
	private static float _trackYaw;
	private static float _trackYawVel;
	private static float _trackPitch;
	private static float _trackPitchVel;
	// Smoothed mouse rate — the "target" the weapon tracks
	private static float _smoothYaw;
	private static float _smoothPitch;

	// --- Breathing sway state ---
	// Slow sinusoidal weapon pitch/yaw from breathing, independent of vanilla breath.
	// Scales with fatigue — more tired = heavier breathing = more gun movement.
	private static float _breathTime;

	// --- Walk bob weapon rotation state ---
	// Weapon tip pitches and rolls in sync with footsteps while walking.
	// Uses a phase accumulator synced to movement speed.
	private static float _walkBobPhase;
	private static float _walkBobPitch;
	private static float _walkBobPitchVel;
	private static float _walkBobRoll;
	private static float _walkBobRollVel;

	// --- Fatigue micro-sway state ---
	// High-frequency Perlin-noise tremor when stamina is low.
	// Simulates trembling arms from exhaustion.
	private static float _fatigueTime;

	// --- Turn muzzle lead state ---
	// The gun tip leads slightly in the turn direction, then settles back.
	// Like actively pointing the weapon where you're turning.
	private static float _turnLeadYaw;
	private static float _turnLeadYawVel;
	private static float _smoothTurnRate;

	/// <summary>
	/// Procedural sway offset (rotation in degrees) driven by mouse motion.
	/// Applied to the weapon root in the camera position patch.
	/// Only active in non-ADS.
	/// </summary>
	public static Quaternion ProceduralSwayOffset { get; private set; } = Quaternion.identity;

	public static void Apply()
	{
		if (HandlingConfig.SwayEnabled.Value && HandlingState.HasWeapon)
		{
			ProceduralWeaponAnimation animation = HandlingState.Animation;
			bool isAiming = HandlingState.IsAiming;
			float num = Fatigue();
			float weightProgress = HandlingState.Stats.WeightProgress;
			float num2 = (isAiming ? HandlingConfig.AimSwayMultiplier.Value : HandlingConfig.HipSwayMultiplier.Value);
			float num3 = HandlingConfig.SwayMultiplier.Value * num2 * num;
			if (animation.Breath != null)
			{
				animation.Breath.Intensity = animation.IntensityByPoseLevel * animation.IntensityByAiming * num3;
				animation.Breath.HipPenalty = HandlingConfig.HipSwayMultiplier.Value * (1f + 0.5f * weightProgress);
			}
			if (animation.MotionReact != null)
			{
				animation.MotionReact.Intensity = 0.45f * HandlingConfig.MotionInertiaMultiplier.Value * (1f + 0.35f * weightProgress);
			}
			ApplyTremor(animation, num);
		}
	}

	/// <summary>
	/// Updates the procedural mouse-tracking sway plus all enhanced sway layers.
	/// Called every frame from HandlingManager. Only active in non-ADS.
	///
	/// Layers:
	///  1. Mouse-rate spring sway (existing, enhanced with amplification + overshoot)
	///  2. Breathing sway (slow sinusoidal, fatigue-scaled)
	///  3. Walk bob rotation (footstep-synced pitch/roll)
	///  4. Fatigue micro-sway (high-frequency tremor when tired)
	///  5. Turn muzzle lead (gun tip leads into turns, then settles)
	/// </summary>
	public static void TickProceduralSway(float dt)
	{
		if (!HandlingConfig.SwayEnabled.Value || !HandlingState.HasWeapon || HandlingState.IsAiming)
		{
			// Decay to neutral when ADS or disabled
			if (_trackYaw != 0f || _trackPitch != 0f || _walkBobPitch != 0f || _turnLeadYaw != 0f)
			{
				SpringFloat(ref _trackYaw, ref _trackYawVel, 0f, 8f, dt);
				SpringFloat(ref _trackPitch, ref _trackPitchVel, 0f, 8f, dt);
				SpringFloat(ref _walkBobPitch, ref _walkBobPitchVel, 0f, 6f, dt);
				SpringFloat(ref _walkBobRoll, ref _walkBobRollVel, 0f, 6f, dt);
				SpringFloat(ref _turnLeadYaw, ref _turnLeadYawVel, 0f, 6f, dt);
				ProceduralSwayOffset = Quaternion.Euler(_trackPitch, _trackYaw, 0f);
			}
			else
			{
				ProceduralSwayOffset = Quaternion.identity;
			}
			_smoothYaw = 0f;
			_smoothPitch = 0f;
			_smoothTurnRate = 0f;
			return;
		}

		float hipMul = HandlingConfig.HipSwayMultiplier.Value;
		if (hipMul <= 0.01f)
		{
			ProceduralSwayOffset = Quaternion.identity;
			return;
		}

		float fatigue = Fatigue();
		float weightProgress = HandlingState.Stats.WeightProgress;
		// Heavier weapons sway more from mouse motion (more inertia to overcome)
		float weightScale = 1f + 0.4f * weightProgress;
		// Fatigued arms track worse (more sway lag)
		float fatigueScale = Mathf.Lerp(0.8f, 1.6f, Mathf.Clamp01((fatigue - 1f) / 2f));

		// === Layer 1: Mouse-rate spring sway (enhanced) ===
		// Smooth the mouse rate — this is what the weapon is "tracking"
		// Slower smoothing = more lag = more sway
		float trackSpeed = Mathf.Lerp(10f, 4f, weightProgress) / fatigueScale;
		_smoothYaw = Mathf.Lerp(_smoothYaw, MotionTracker.YawRate, dt * trackSpeed);
		_smoothPitch = Mathf.Lerp(_smoothPitch, MotionTracker.PitchRate, dt * trackSpeed);

		// The sway target is proportional to the smoothed mouse rate.
		// Amplified by the muzzle amplification config for more gun tip rotation.
		float muzzleAmp = HandlingConfig.SwayMuzzleAmplification.Value;
		float yawTarget = Mathf.Clamp(_smoothYaw * 0.015f * hipMul * weightScale * muzzleAmp, -6f, 6f);
		float pitchTarget = Mathf.Clamp(_smoothPitch * 0.012f * hipMul * weightScale * muzzleAmp, -4f, 4f);

		// Spring the weapon toward the tracking target.
		// The overshoot config reduces damping so the weapon oscillates
		// naturally when the mouse stops — like a real spring with mass.
		float springFreq = Mathf.Lerp(6f, 2.5f, weightProgress) / fatigueScale;
		float overshoot = HandlingConfig.SwayInertiaOvershoot.Value;
		SpringFloatOvershoot(ref _trackYaw, ref _trackYawVel, yawTarget, springFreq, dt, overshoot);
		SpringFloatOvershoot(ref _trackPitch, ref _trackPitchVel, pitchTarget, springFreq * 0.85f, dt, overshoot);

		// === Layer 2: Breathing sway ===
		// Slow sinusoidal weapon pitch/yaw from breathing.
		// More fatigue = faster, deeper breathing = more gun movement.
		float breathStrength = HandlingConfig.SwayBreathingStrength.Value;
		float breathPitch = 0f;
		float breathYaw = 0f;
		if (breathStrength > 0.001f)
		{
			float breathSpeed = HandlingConfig.SwayBreathingSpeed.Value * Mathf.Lerp(1f, 1.8f, Mathf.Clamp01((fatigue - 1f) / 2f));
			_breathTime += dt * breathSpeed;
			// Pitch oscillates (chest rising/falling moves the gun up/down)
			breathPitch = Mathf.Sin(_breathTime * Mathf.PI * 2f) * 0.5f * breathStrength * fatigue;
			// Slight yaw oscillation (body sway from breathing)
			breathYaw = Mathf.Sin(_breathTime * Mathf.PI * 2f * 0.5f + 1.3f) * 0.2f * breathStrength * fatigue;
			// Heavier weapons breathe less (more mass to move)
			breathPitch *= (1f - 0.3f * weightProgress);
			breathYaw *= (1f - 0.3f * weightProgress);
		}

		// === Layer 3: Walk bob weapon rotation ===
		// Weapon tip pitches and rolls in sync with footsteps.
		// Scales with movement speed — standing still = no bob.
		float walkBobStrength = HandlingConfig.SwayWalkBobStrength.Value;
		float walkPitch = 0f;
		float walkRoll = 0f;
		if (walkBobStrength > 0.001f)
		{
			float moveSpeed = MotionTracker.NormalizedSpeed;
			if (moveSpeed > 0.05f)
			{
				float bobSpeed = HandlingConfig.SwayWalkBobSpeed.Value * Mathf.Lerp(0.8f, 1.3f, moveSpeed);
				_walkBobPhase += dt * bobSpeed * Mathf.PI * 2f;
				// Pitch: gun tips forward/back with each step
				float pitchAmp = 0.8f * walkBobStrength * moveSpeed * (1f + 0.3f * weightProgress);
				// Roll: gun rolls slightly with body weight transfer
				float rollAmp = 0.5f * walkBobStrength * moveSpeed * (1f - 0.2f * weightProgress);
				float targetPitch = Mathf.Sin(_walkBobPhase) * pitchAmp;
				float targetRoll = Mathf.Sin(_walkBobPhase * 0.5f) * rollAmp;
				// Spring for smooth transitions when starting/stopping
				SpringFloat(ref _walkBobPitch, ref _walkBobPitchVel, targetPitch, 8f, dt);
				SpringFloat(ref _walkBobRoll, ref _walkBobRollVel, targetRoll, 6f, dt);
				walkPitch = _walkBobPitch;
				walkRoll = _walkBobRoll;
			}
			else
			{
				// Decay to zero when standing still
				SpringFloat(ref _walkBobPitch, ref _walkBobPitchVel, 0f, 6f, dt);
				SpringFloat(ref _walkBobRoll, ref _walkBobRollVel, 0f, 6f, dt);
				walkPitch = _walkBobPitch;
				walkRoll = _walkBobRoll;
			}
		}

		// === Layer 4: Fatigue micro-sway ===
		// High-frequency Perlin-noise tremor when stamina is low.
		// Simulates trembling arms from exhaustion.
		float fatigueMicroStrength = HandlingConfig.SwayFatigueMicroSway.Value;
		float microPitch = 0f;
		float microYaw = 0f;
		if (fatigueMicroStrength > 0.001f)
		{
			// Only active when meaningfully fatigued
			float fatigueAmount = Mathf.Clamp01((fatigue - 1f) / 2f);
			if (fatigueAmount > 0.1f)
			{
				_fatigueTime += dt * 12f; // high frequency tremor
				float tremorAmp = fatigueMicroStrength * fatigueAmount * 0.4f;
				// Two Perlin channels for pitch and yaw
				microPitch = (Mathf.PerlinNoise(_fatigueTime, 3.7f) - 0.5f) * 2f * tremorAmp;
				microYaw = (Mathf.PerlinNoise(7.1f, _fatigueTime * 1.1f) - 0.5f) * 2f * tremorAmp * 0.7f;
			}
		}

		// === Layer 5: Turn muzzle lead ===
		// The gun tip leads slightly in the turn direction, then settles back.
		// Like actively pointing the weapon where you're turning.
		float turnLeadStrength = HandlingConfig.SwayTurnLeadStrength.Value;
		float turnLead = 0f;
		if (turnLeadStrength > 0.001f)
		{
			// Smooth the turn rate for the lead target
			_smoothTurnRate = Mathf.Lerp(_smoothTurnRate, MotionTracker.YawRate, dt * 8f);
			// The lead target is proportional to turn rate — gun points ahead
			float leadTarget = Mathf.Clamp(_smoothTurnRate * 0.008f * turnLeadStrength * weightScale, -3f, 3f);
			// Spring it — the lead settles back when turning stops
			SpringFloat(ref _turnLeadYaw, ref _turnLeadYawVel, leadTarget, 4f, dt);
			turnLead = _turnLeadYaw;
		}

		// === Combine all layers ===
		// Each layer contributes rotation in degrees.
		// The spring sway is the base, other layers add on top.
		float finalPitch = _trackPitch + breathPitch + walkPitch + microPitch;
		float finalYaw = _trackYaw + breathYaw + turnLead + microYaw;
		float finalRoll = walkRoll;

		ProceduralSwayOffset = Quaternion.Euler(finalPitch, finalYaw, finalRoll);
	}

	private static void ApplyTremor(ProceduralWeaponAnimation animation, float fatigue)
	{
		if (animation.HandShakeEffector != null)
		{
			float num = 3f * HandlingConfig.TremorMultiplier.Value * fatigue;
			if (!(Mathf.Abs(num - _appliedTremor) < 0.01f))
			{
				_appliedTremor = num;
				animation.HandShakeEffector.Setup(num, 1f, 1f);
			}
		}
	}

	public static float Fatigue()
	{
		float num = Mathf.Max(1f - HandlingState.HandsStamina01, 1f - HandlingState.Oxygen01);
		float num2 = 0.5f * HandlingState.Stats.WeightProgress * (1f - HandlingState.Stamina01);
		return 1f + HandlingConfig.StaminaSwayInfluence.Value * (num + num2);
	}

	public static void Reset()
	{
		_appliedTremor = -1f;
		_trackYaw = 0f; _trackYawVel = 0f;
		_trackPitch = 0f; _trackPitchVel = 0f;
		_smoothYaw = 0f;
		_smoothPitch = 0f;
		_breathTime = 0f;
		_walkBobPhase = 0f;
		_walkBobPitch = 0f; _walkBobPitchVel = 0f;
		_walkBobRoll = 0f; _walkBobRollVel = 0f;
		_fatigueTime = 0f;
		_turnLeadYaw = 0f; _turnLeadYawVel = 0f;
		_smoothTurnRate = 0f;
		ProceduralSwayOffset = Quaternion.identity;
	}

	/// <summary>
	/// Standard critically-damped spring. Used for layers that should
	/// settle smoothly without oscillation.
	/// </summary>
	private static void SpringFloat(ref float current, ref float velocity, float target, float frequency, float dt)
	{
		float k = frequency * frequency;
		float d = 1.4f * frequency;
		velocity += ((target - current) * k - velocity * d) * dt;
		current += velocity * dt;
	}

	/// <summary>
	/// Under-damped spring with configurable overshoot.
	/// When overshoot > 0, the damping is reduced below critical so the
	/// weapon oscillates past the target before settling — like a real
	/// spring with mass. This makes the mouse-driven sway feel more organic.
	/// </summary>
	private static void SpringFloatOvershoot(ref float current, ref float velocity, float target, float frequency, float dt, float overshoot)
	{
		float k = frequency * frequency;
		// Critical damping = 2 * sqrt(k). Reduce it by the overshoot factor.
		// overshoot=0 → critically damped (d = 2*freq), overshoot=1 → very under-damped (d = 0.3*freq)
		float criticalD = 2f * frequency;
		float d = Mathf.Lerp(criticalD, criticalD * 0.15f, Mathf.Clamp01(overshoot));
		velocity += ((target - current) * k - velocity * d) * dt;
		current += velocity * dt;
	}
}
