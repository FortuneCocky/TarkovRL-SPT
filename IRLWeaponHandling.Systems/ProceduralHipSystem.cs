using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

/// <summary>
/// Procedural hip deadzone. Think of the deadzone cone as a red box on screen:
/// the arms/weapon move freely inside it while the camera stays completely
/// locked. Only when the weapon reaches the edge of the box does the camera
/// start following. A larger cone = more arm movement before the camera moves.
/// Sprinting smoothly blends the effect out and back in for natural transitions.
///
/// Natural motion layers (controlled by HipNaturalMotion):
///  - Organic Perlin drift: subtle micro-tremors simulating hands holding a gun
///  - Mouse whip: weapon lags behind fast mouse flicks then springs back
///  - Directional tilt: weapon rolls/tilts in the direction of mouse motion
/// </summary>
internal static class ProceduralHipSystem
{
    private static Quaternion _weaponRot = Quaternion.identity;
    private static Quaternion _cameraRot = Quaternion.identity;
    private static bool _initialized;

    // Sprint blend: 0 = full hip deadzone, 1 = full sprint (no deadzone).
    private static float _sprintBlend;
    private static float _sprintVelocity;

    // --- Natural motion state ---
    // Organic Perlin drift (micro-tremors / breathing while holding at hip)
    private static float _driftTime;
    private static Vector2 _organicOffset;

    // Mouse whip (weapon lags behind fast flicks, springs back)
    private static Vector2 _whipOffset;
    private static Vector2 _whipVelocity;
    private static Vector2 _lastMouseDelta;

    // Directional tilt (weapon rolls in direction of mouse motion)
    private static float _tiltCurrent;
    private static float _tiltVelocity;

    // --- Procedural arm/wrist motion state ---

    // Strafe wrist roll: weapon rolls when strafing left/right
    private static float _strafeRoll;
    private static float _strafeRollVel;

    // Acceleration sway: weapon pitches forward/back from movement inertia
    private static float _accelPitch;
    private static float _accelPitchVel;
    private static float _lastSpeed;

    // Idle wrist roll: subtle random wrist micro-adjustments
    private static float _wristRollTime;
    private static float _wristRollCurrent;
    private static float _wristRollVel;

    // --- Shoulder-pivot tracking state ---
    // The gun is grounded at the shoulder (stock doesn't move). When the
    // player moves the mouse, the arms track with a slight lag — like a
    // tracking system where the arms try to follow. Because the gun pivots
    // from the shoulder, the TIP curves in the turn direction, tracing an arc.
    //
    // This is a rotational offset (not position). The pivot point is the
    // WeaponRoot origin (near the shoulder), so rotating it makes the muzzle
    // tip swing while the stock stays put.
    private static float _trackYaw;      // current tracking yaw offset (deg)
    private static float _trackYawVel;   // tracking yaw spring velocity
    private static float _trackPitch;    // current tracking pitch offset (deg)
    private static float _trackPitchVel; // tracking pitch spring velocity
    // Smoothed mouse rate for the tracking input
    private static float _smoothYawRate;
    private static float _smoothPitchRate;

    // Cached weapon offset — computed once per Tick, read many times per frame
    private static Quaternion _cachedWeaponOffset = Quaternion.identity;

    /// <summary>Where the camera should look (locked until weapon hits edge).</summary>
    public static Vector3 CameraEuler => _cameraRot.eulerAngles;

    /// <summary>Relative weapon offset applied to WeaponRoot (weapon vs camera).</summary>
    public static Quaternion WeaponOffset => _cachedWeaponOffset;

    public static void Tick(Player player, float deltaTime)
    {
        if (deltaTime <= 0f || player == null)
        {
            return;
        }

        if (!HandlingConfig.HipDeadzoneEnabled.Value || !HandlingState.HasWeapon)
        {
            Reset();
            return;
        }

        MovementContext mc = player.MovementContext;
        if (mc != null && (mc.IsInMountedState || (mc.CurrentState != null && mc.CurrentState.Name == EPlayerState.Stationary)))
        {
            Reset();
            return;
        }

        // Smoothly blend sprint state instead of hard reset.
        float sprintTarget = HandlingState.IsSprinting ? 1f : 0f;
        float dt = Mathf.Min(deltaTime, 0.05f);
        SpringSprint(ref _sprintBlend, ref _sprintVelocity, sprintTarget, HandlingConfig.SprintTransitionSpeed.Value, dt);

        Quaternion target = Quaternion.Euler(player.HeadRotation);
        if (!_initialized)
        {
            _weaponRot = target;
            _cameraRot = target;
            _initialized = true;
        }

        // --- Weapon: tracks the mouse target with weighted smoothing ---
        float weaponSmooth = Mathf.Max(0.1f, HandlingConfig.HipWeaponSmooth.Value);
        float sprintMul = 1f + _sprintBlend * 2f;
        _weaponRot = Quaternion.Slerp(_weaponRot, target, weaponSmooth * sprintMul * dt);

        // Procedural deadzone drift — reduced during sprint.
        float deadzoneInfluence = HandlingConfig.HipDeadzoneInfluence.Value * (1f - _sprintBlend * 0.7f);
        if (deadzoneInfluence > 0f)
        {
            float yawDrift = MotionTracker.YawRate * deadzoneInfluence * 0.01f;
            float pitchDrift = MotionTracker.PitchRate * deadzoneInfluence * 0.01f;
            Quaternion drift = Quaternion.Euler(pitchDrift, yawDrift, 0f);
            _weaponRot = drift * _weaponRot;
        }

        // --- Hard clamp: prevent the weapon from drifting too far from the
        // mouse target during fast turns. When the angle exceeds the catch-up
        // limit, snap the weapon toward the target instead of Slerping slowly.
        // This fixes arms failing to catch up during hard turns.
        float catchUpAngle = 30f + _sprintBlend * 20f; // 30° hip, up to 50° sprint
        float weaponDrift = Quaternion.Angle(_weaponRot, target);
        if (weaponDrift > catchUpAngle)
        {
            float snapT = (weaponDrift - catchUpAngle) / Mathf.Max(weaponDrift, 0.001f);
            _weaponRot = Quaternion.Slerp(_weaponRot, target, snapT);
        }

        // --- Camera: locked inside the cone, follows only at edge ---
        float cone = Mathf.Max(0.1f, HandlingConfig.HipDeadzoneCone.Value);
        float angle = Quaternion.Angle(_cameraRot, _weaponRot);

        if (angle > cone)
        {
            // Weapon hit the edge — camera centers itself with the mouse target.
            float smoothness = Mathf.Max(0.1f, HandlingConfig.HipCameraLag.Value);
            smoothness *= (1f + _sprintBlend * 2f);
            _cameraRot = Quaternion.Slerp(_cameraRot, target, smoothness * dt);

            // Safety: if the weapon is moving fast and the camera can't keep up,
            // snap the camera so the arms never disappear off screen.
            float maxAngle = cone * 1.5f;
            float postAngle = Quaternion.Angle(_cameraRot, _weaponRot);
            if (postAngle > maxAngle)
            {
                float snapT = (postAngle - maxAngle) / Mathf.Max(postAngle, 0.001f);
                _cameraRot = Quaternion.Slerp(_cameraRot, _weaponRot, snapT);
            }
        }

        // --- Natural motion layers (blended out during sprint) ---
        UpdateNaturalMotion(dt, sprintTarget);
        UpdateProceduralArmMotion(player, dt);
        UpdateShoulderPivotTracking(dt);

        // --- Cache the weapon offset quaternion (computed once per tick) ---
        CacheWeaponOffset();
    }

    /// <summary>
    /// Computes and caches the weapon offset quaternion. Called once per Tick
    /// so the WeaponOffset getter is a cheap field read instead of doing
    /// quaternion math on every access (which can be many times per frame).
    /// </summary>
    private static void CacheWeaponOffset()
    {
        if (!_initialized)
        {
            _cachedWeaponOffset = Quaternion.identity;
            return;
        }

        Quaternion baseOffset = _weaponRot * Quaternion.Inverse(_cameraRot);

        float naturalStrength = HandlingConfig.HipNaturalMotion.Value;
        if (naturalStrength > 0.001f)
        {
            Vector2 drift = _organicOffset * naturalStrength;
            Vector2 whip = _whipOffset * naturalStrength;
            float tilt = _tiltCurrent * naturalStrength;

            Vector3 naturalEuler = new Vector3(
                drift.y + whip.y,
                drift.x + whip.x,
                tilt);

            baseOffset = Quaternion.Euler(naturalEuler) * baseOffset;
        }

        // Procedural arm/wrist motion layers (independent of HipNaturalMotion)
        Vector3 armEuler = Vector3.zero;
        // Acceleration sway: pitch from starting/stopping
        armEuler.x += _accelPitch;
        // Strafe wrist roll: roll from lateral movement
        armEuler.z += _strafeRoll;
        // Idle wrist roll: subtle random roll
        armEuler.z += _wristRollCurrent;
        if (armEuler != Vector3.zero)
            baseOffset = Quaternion.Euler(armEuler) * baseOffset;

        // Shoulder-pivot tracking: the gun tip curves when turning (non-ADS).
        // Applied as a rotation from the WeaponRoot (shoulder pivot point),
        // so the muzzle traces an arc while the stock stays grounded.
        if (HandlingConfig.HipFloatMotion.Value > 0.001f)
        {
            Vector3 trackEuler = new Vector3(_trackPitch, _trackYaw, 0f);
            if (trackEuler != Vector3.zero)
                baseOffset = Quaternion.Euler(trackEuler) * baseOffset;
        }

        float blend = 1f - _sprintBlend;
        _cachedWeaponOffset = Quaternion.Slerp(Quaternion.identity, baseOffset, blend);
    }

    /// <summary>
    /// Shoulder-pivot tracking system.
    ///
    /// The gun is grounded at the shoulder — the stock stays fixed. When the
    /// player moves the mouse, the arms act like a tracking system: they try
    /// to follow but lag slightly behind. Because the gun pivots from the
    /// shoulder (WeaponRoot origin), the TIP curves in the turn direction,
    /// tracing an arc. When the mouse stops, the arms spring back to neutral.
    ///
    /// This is purely rotational — no position offset. The pivot point is the
    /// WeaponRoot, which sits near the shoulder, so rotating it makes the
    /// muzzle swing while the stock stays planted.
    /// </summary>
    private static void UpdateShoulderPivotTracking(float dt)
    {
        float trackStrength = HandlingConfig.HipFloatMotion.Value;
        if (trackStrength <= 0.001f)
        {
            _trackYaw = 0f; _trackYawVel = 0f;
            _trackPitch = 0f; _trackPitchVel = 0f;
            _smoothYawRate = 0f;
            _smoothPitchRate = 0f;
            return;
        }

        float sprintFade = 1f - _sprintBlend * 0.85f;
        WeaponStats stats = HandlingState.Stats;
        // Heavier weapons track slower (more arm inertia)
        float weightTrack = 1f - 0.3f * stats.WeightProgress;
        // Tie to deadzone influence
        float influence = HandlingConfig.HipDeadzoneInfluence.Value;
        float influenceScale = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(influence / 3f));
        float effective = trackStrength * sprintFade * influenceScale;

        // Smooth the mouse rate — this is the "tracking target" that the arms follow
        // The smoothing creates the lag: arms don't react instantly, they track
        float smoothSpeed = Mathf.Lerp(12f, 6f, stats.WeightProgress); // heavier = slower tracking
        _smoothYawRate = Mathf.Lerp(_smoothYawRate, MotionTracker.YawRate, dt * smoothSpeed);
        _smoothPitchRate = Mathf.Lerp(_smoothPitchRate, MotionTracker.PitchRate, dt * smoothSpeed);

        // The tracking target: the arms want to align with where the mouse is going,
        // but they lag. The offset is proportional to the difference between the
        // raw mouse rate and the smoothed rate — this is the "tracking error"
        float yawError = MotionTracker.YawRate - _smoothYawRate;
        float pitchError = MotionTracker.PitchRate - _smoothPitchRate;

        // Target rotation: the gun tip curves in the direction of mouse motion
        // Positive yaw rate (turning right) → muzzle curves right (positive yaw offset)
        // The scaling makes the curve subtle but noticeable
        float yawTarget = Mathf.Clamp(yawError * 0.02f * effective * weightTrack, -3f, 3f);
        float pitchTarget = Mathf.Clamp(pitchError * 0.015f * effective * weightTrack, -2f, 2f);

        // Spring the tracking rotation — arms try to catch up to the mouse
        // Heavier weapons use a slower spring (arms have more inertia)
        float trackSpringFreq = Mathf.Lerp(7f, 3.5f, stats.WeightProgress);
        SpringFloat(ref _trackYaw, ref _trackYawVel, yawTarget, trackSpringFreq, dt);
        SpringFloat(ref _trackPitch, ref _trackPitchVel, pitchTarget, trackSpringFreq * 0.8f, dt);

        // When the mouse stops, the smoothed rate catches up to zero, the error
        // goes to zero, and the spring pulls the tracking offset back to neutral.
        // The gun returns to its grounded shoulder position.
    }

    /// <summary>
    /// Updates all natural motion layers: organic drift, mouse whip, and
    /// directional tilt. All are blended out during sprint.
    /// </summary>
    private static void UpdateNaturalMotion(float dt, float sprintBlend)
    {
        float naturalStrength = HandlingConfig.HipNaturalMotion.Value;
        if (naturalStrength <= 0.001f)
        {
            _organicOffset = Vector2.zero;
            _whipOffset = Vector2.zero;
            _whipVelocity = Vector2.zero;
            _tiltCurrent = 0f;
            _tiltVelocity = 0f;
            return;
        }

        float sprintFade = 1f - _sprintBlend * 0.85f;

        // --- Organic Perlin drift ---
        // Simulates micro-tremors and breathing while holding a weapon at hip.
        // Uses two Perlin noise channels at different frequencies for yaw/pitch.
        float driftStrength = HandlingConfig.HipOrganicDrift.Value * sprintFade;
        if (driftStrength > 0.001f)
        {
            _driftTime += dt * 0.8f;
            // Weapon weight makes the drift slower and wider
            WeaponStats stats = HandlingState.Stats;
            float weightScale = 1f + 0.3f * stats.WeightProgress;
            float driftAmp = 0.4f * driftStrength * weightScale;
            // Two octaves for more organic feel
            float yawNoise = (Mathf.PerlinNoise(_driftTime * 0.7f, 1.3f) - 0.5f) * 2f;
            float pitchNoise = (Mathf.PerlinNoise(2.1f, _driftTime * 0.7f) - 0.5f) * 2f;
            // Add a slower secondary layer
            yawNoise += (Mathf.PerlinNoise(_driftTime * 0.25f, 5.7f) - 0.5f) * 0.5f;
            pitchNoise += (Mathf.PerlinNoise(8.3f, _driftTime * 0.25f) - 0.5f) * 0.5f;
            _organicOffset = new Vector2(yawNoise * driftAmp, pitchNoise * driftAmp * 0.7f);
        }
        else
        {
            _organicOffset = Vector2.zero;
        }

        // --- Mouse whip ---
        // When the player flicks the mouse, the weapon lags behind then springs
        // back. This creates a natural whip effect. The whip is driven by the
        // raw mouse delta (yaw/pitch rate), not the smoothed weapon rotation.
        float whipStrength = HandlingConfig.HipMouseWhip.Value * sprintFade;
        if (whipStrength > 0.001f)
        {
            // Raw mouse delta this frame (degrees)
            float yawDelta = MotionTracker.YawRate * dt;
            float pitchDelta = MotionTracker.PitchRate * dt;
            Vector2 mouseDelta = new Vector2(yawDelta, pitchDelta);

            // The whip target is opposite to the mouse motion (weapon lags behind)
            // Scaled by whip strength and weapon weight (heavier = more lag)
            WeaponStats stats = HandlingState.Stats;
            float weightLag = 1f + 0.4f * stats.WeightProgress;
            Vector2 whipTarget = -mouseDelta * whipStrength * 0.3f * weightLag;

            // Clamp the whip so it doesn't go crazy on huge flicks
            whipTarget.x = Mathf.Clamp(whipTarget.x, -3f, 3f);
            whipTarget.y = Mathf.Clamp(whipTarget.y, -2f, 2f);

            // Spring the whip offset toward the target, then it settles back to 0
            // Use a critically-damped spring for natural feel
            float whipFreq = Mathf.Lerp(6f, 3f, stats.WeightProgress); // heavier = slower spring
            SpringVector2(ref _whipOffset, ref _whipVelocity, whipTarget, whipFreq, dt);

            // Decay the whip target back to zero when mouse stops
            // (the spring naturally pulls _whipOffset back to the target, and
            //  when mouse stops, whipTarget → 0, so the weapon settles back)
            _lastMouseDelta = mouseDelta;
        }
        else
        {
            _whipOffset = Vector2.zero;
            _whipVelocity = Vector2.zero;
        }

        // --- Directional tilt ---
        // The weapon rolls/tilts in the direction of mouse yaw motion, like
        // a person's wrists flexing when swinging the gun sideways.
        float tiltStrength = HandlingConfig.HipDirectionalTilt.Value * sprintFade;
        if (tiltStrength > 0.001f)
        {
            // Use the slow yaw rate for smoother tilt
            float yawRate = MotionTracker.YawRate;
            // Target tilt is proportional to yaw rate (clamped)
            float tiltTarget = Mathf.Clamp(yawRate / 180f, -1f, 1f) * tiltStrength * 2.5f;
            // Weapon weight reduces tilt (heavier guns are harder to tilt)
            WeaponStats stats = HandlingState.Stats;
            tiltTarget *= (1f - 0.2f * stats.WeightProgress);
            // Spring the tilt for smooth transitions
            SpringFloat(ref _tiltCurrent, ref _tiltVelocity, tiltTarget, 5f, dt);
        }
        else
        {
            _tiltCurrent = 0f;
            _tiltVelocity = 0f;
        }
    }

    /// <summary>
    /// Updates procedural arm/wrist motion layers: walk sway, strafe roll,
    /// acceleration sway, and idle wrist roll. These simulate realistic
    /// weapon rotation from body movement and grip micro-adjustments.
    /// </summary>
    private static void UpdateProceduralArmMotion(Player player, float dt)
    {
        float sprintFade = 1f - _sprintBlend * 0.85f;
        WeaponStats stats = HandlingState.Stats;
        // Heavier weapons sway more, roll less (harder to rotate wrists)
        float weightSway = 1f + 0.3f * stats.WeightProgress;
        float weightRoll = 1f - 0.25f * stats.WeightProgress;

        // --- Strafe wrist roll ---
        // When strafing left/right, the weapon rolls in the direction of
        // body momentum, like wrists rotating with the swing.
        float strafeStrength = HandlingConfig.HipStrafeRoll.Value * sprintFade;
        if (strafeStrength > 0.001f)
        {
            // Use lateral input direction for strafe detection
            float strafeInput = MotionTracker.InputDirection.x;
            // Also factor in actual lateral velocity for smoothness
            float yaw = player.Rotation.x * Mathf.Deg2Rad;
            float sin = Mathf.Sin(yaw);
            float cos = Mathf.Cos(yaw);
            float localRight = player.Velocity.x * cos - player.Velocity.z * sin;
            float lateralVel = Mathf.Clamp(localRight * 0.05f, -1.5f, 1.5f);
            // Combine input and velocity for a natural target
            float rollTarget = (strafeInput * 0.6f + lateralVel * 0.4f) * strafeStrength * 2.5f * weightRoll;
            rollTarget = Mathf.Clamp(rollTarget, -8f, 8f);
            SpringFloat(ref _strafeRoll, ref _strafeRollVel, rollTarget, 4f, dt);
        }
        else
        {
            _strafeRoll = Mathf.Lerp(_strafeRoll, 0f, dt * 8f);
            _strafeRollVel = 0f;
        }

        // --- Acceleration sway ---
        // When starting/stopping, the weapon pitches forward/back from arm inertia.
        float accelStrength = HandlingConfig.HipAccelSway.Value * sprintFade;
        if (accelStrength > 0.001f)
        {
            float currentSpeed = MotionTracker.NormalizedSpeed;
            float speedDelta = currentSpeed - _lastSpeed;
            _lastSpeed = currentSpeed;
            // Positive delta = accelerating = weapon lags back (positive pitch)
            // Negative delta = decelerating = weapon tips forward (negative pitch)
            float pitchTarget = Mathf.Clamp(speedDelta * 15f, -5f, 5f) * accelStrength * weightSway;
            // Spring it so it bounces back naturally
            SpringFloat(ref _accelPitch, ref _accelPitchVel, pitchTarget, 3f, dt);
        }
        else
        {
            _accelPitch = Mathf.Lerp(_accelPitch, 0f, dt * 8f);
            _accelPitchVel = 0f;
            _lastSpeed = MotionTracker.NormalizedSpeed;
        }

        // --- Idle wrist roll ---
        // Subtle random wrist micro-adjustments using Perlin noise.
        // Simulates the grip shifting slightly while holding the weapon.
        float wristStrength = HandlingConfig.HipWristRoll.Value * sprintFade;
        if (wristStrength > 0.001f && MotionTracker.NormalizedSpeed < 0.15f)
        {
            _wristRollTime += dt * 0.5f;
            // Slow Perlin noise for organic micro-roll
            float noise = (Mathf.PerlinNoise(_wristRollTime, 11.3f) - 0.5f) * 2f;
            float rollTarget = noise * wristStrength * 0.8f * weightRoll;
            SpringFloat(ref _wristRollCurrent, ref _wristRollVel, rollTarget, 2.5f, dt);
        }
        else
        {
            _wristRollCurrent = Mathf.Lerp(_wristRollCurrent, 0f, dt * 5f);
            _wristRollVel = 0f;
        }
    }

    /// <summary>Returns the camera rotation to use for the head, or the input if inactive.</summary>
    public static Vector3 Modify(Vector3 headRotation)
    {
        if (!_initialized || !HandlingConfig.HipDeadzoneEnabled.Value)
        {
            return headRotation;
        }
        // During full sprint, don't override the camera — let vanilla handle it.
        if (_sprintBlend > 0.95f)
        {
            return headRotation;
        }
        // Blend between our camera and vanilla as sprint increases.
        // Only blend yaw and pitch, never roll — prevents camera roll bug.
        Vector3 ourCam = _cameraRot.eulerAngles;
        Vector3 result = headRotation;
        result.x = Mathf.LerpAngle(ourCam.x, headRotation.x, _sprintBlend);
        result.y = Mathf.LerpAngle(ourCam.y, headRotation.y, _sprintBlend);
        return result;
    }

    public static void Reset()
    {
        _weaponRot = Quaternion.identity;
        _cameraRot = Quaternion.identity;
        _initialized = false;
        _sprintBlend = 0f;
        _sprintVelocity = 0f;
        _driftTime = 0f;
        _organicOffset = Vector2.zero;
        _whipOffset = Vector2.zero;
        _whipVelocity = Vector2.zero;
        _lastMouseDelta = Vector2.zero;
        _tiltCurrent = 0f;
        _tiltVelocity = 0f;
        _strafeRoll = 0f;
        _strafeRollVel = 0f;
        _accelPitch = 0f;
        _accelPitchVel = 0f;
        _lastSpeed = 0f;
        _wristRollTime = 0f;
        _wristRollCurrent = 0f;
        _wristRollVel = 0f;
        _trackYaw = 0f; _trackYawVel = 0f;
        _trackPitch = 0f; _trackPitchVel = 0f;
        _smoothYawRate = 0f;
        _smoothPitchRate = 0f;
        _cachedWeaponOffset = Quaternion.identity;
    }

    private static void SpringSprint(ref float current, ref float velocity, float target, float speed, float dt)
    {
        float freq = Mathf.Max(0.5f, speed);
        float k = freq * freq;
        float d = 1.4f * freq;
        velocity += ((target - current) * k - velocity * d) * dt;
        current += velocity * dt;
        current = Mathf.Clamp01(current);
    }

    private static void SpringVector2(ref Vector2 current, ref Vector2 velocity, Vector2 target, float frequency, float dt)
    {
        float k = frequency * frequency;
        float d = 1.4f * frequency;
        velocity += ((target - current) * k - velocity * d) * dt;
        current += velocity * dt;
    }

    private static void SpringFloat(ref float current, ref float velocity, float target, float frequency, float dt)
    {
        float k = frequency * frequency;
        float d = 1.4f * frequency;
        velocity += ((target - current) * k - velocity * d) * dt;
        current += velocity * dt;
    }
}
