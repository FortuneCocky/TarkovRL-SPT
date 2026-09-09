using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems
{
    /// <summary>
    /// ADS deadzone system. Works like the hip deadzone: the weapon tracks
    /// the mouse inside a cone while the camera stays locked. Only when the
    /// weapon reaches the cone edge does the camera start following. The
    /// weapon-vs-camera offset is exposed via WeaponOffset and applied to
    /// the WeaponRoot in Patch_CalculateCameraPosition.
    /// </summary>
    internal static class DeadzoneSystem
    {
        private static Quaternion _weaponRot = Quaternion.identity;
        private static Quaternion _cameraRot = Quaternion.identity;
        private static bool _initialized;

        private static Vector2 _offset = Vector2.zero;
        private static Vector2 _clampedOffset = Vector2.zero;
        private static Vector3 _impulse = Vector3.zero;

        // Cached weapon offset — computed once per Tick, read many times per frame
        private static Quaternion _cachedWeaponOffset = Quaternion.identity;

        public static Vector2 Offset => _clampedOffset;

        /// <summary>
        /// Weapon rotation relative to camera. Applied to WeaponRoot while
        /// aiming so the weapon visibly moves inside the deadzone cone.
        /// </summary>
        public static Quaternion WeaponOffset => _cachedWeaponOffset;

        public static void AddImpulse(float yaw, float pitch)
        {
            _impulse.y += yaw;
            _impulse.x += pitch;
            _impulse.z = 0f;
        }

        public static void Tick(Player player, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (!HandlingConfig.DeadzoneEnabled.Value || !HandlingState.HasWeapon
                || HandlingState.IsSprinting || !HandlingState.IsAiming)
            {
                Reset();
                return;
            }

            MovementContext movementContext = player.MovementContext;
            if (movementContext != null && (movementContext.IsInMountedState
                || (movementContext.CurrentState != null && movementContext.CurrentState.Name == EPlayerState.Stationary)))
            {
                Reset();
                return;
            }

            float dt = Mathf.Min(deltaTime, 0.05f);
            Vector3 desiredEuler = player.HeadRotation;
            Quaternion desired = Quaternion.Euler(desiredEuler);

            if (!_initialized)
            {
                _weaponRot = desired;
                _cameraRot = desired;
                _initialized = true;
            }

            float cone = Mathf.Max(0.1f, HandlingConfig.DeadzoneAds.Value);
            float weaponSmooth = Mathf.Max(0.1f, HandlingConfig.DeadzoneWeaponSmooth.Value);
            float cameraLag = Mathf.Max(0.1f, HandlingConfig.DeadzoneCameraLag.Value);
            // Fixed settle speed (was configurable as "ADS smoothness")
            float settleSpeed = 2f;

            // --- Weapon: tracks the mouse with weighted smoothing ---
            _weaponRot = Quaternion.Slerp(_weaponRot, desired, weaponSmooth * dt);

            // Procedural drift from movement/turning while aiming
            float motionInfluence = HandlingConfig.DeadzoneMotionInfluence.Value;
            if (motionInfluence > 0f)
            {
                float yawDrift = MotionTracker.YawRate * motionInfluence * 0.01f;
                float pitchDrift = MotionTracker.PitchRate * motionInfluence * 0.01f;
                Quaternion drift = Quaternion.Euler(pitchDrift, yawDrift, 0f);
                _weaponRot = drift * _weaponRot;
            }

            // --- Hard clamp: prevent the weapon from drifting too far from the
            // mouse target during fast turns. When the angle exceeds the catch-up
            // limit, snap the weapon toward the target instead of Slerping slowly.
            float catchUpAngle = 20f; // tighter in ADS for precision
            float weaponDrift = Quaternion.Angle(_weaponRot, desired);
            if (weaponDrift > catchUpAngle)
            {
                float snapT = (weaponDrift - catchUpAngle) / Mathf.Max(weaponDrift, 0.001f);
                _weaponRot = Quaternion.Slerp(_weaponRot, desired, snapT);
            }

            // --- Camera: locked inside the cone, follows only at edge ---
            float angle = Quaternion.Angle(_cameraRot, _weaponRot);

            if (angle > cone)
            {
                // Weapon hit the edge — camera catches up toward the mouse target
                _cameraRot = Quaternion.Slerp(_cameraRot, desired, cameraLag * dt);

                // Safety snap so the weapon never drifts too far off screen
                float maxAngle = cone * 1.5f;
                float postAngle = Quaternion.Angle(_cameraRot, _weaponRot);
                if (postAngle > maxAngle)
                {
                    float snapT = (postAngle - maxAngle) / Mathf.Max(postAngle, 0.001f);
                    _cameraRot = Quaternion.Slerp(_cameraRot, _weaponRot, snapT);
                }
            }
            else
            {
                // Inside the cone — gently settle camera toward desired so it
                // doesn't drift permanently, but much slower than the weapon.
                _cameraRot = Quaternion.Slerp(_cameraRot, desired, settleSpeed * dt * 0.1f);
            }

            // Shot impulse decay
            _impulse *= Mathf.Exp(-8f * dt);

            // Track offset for camera lag system
            Vector3 displayEuler = _cameraRot.eulerAngles + _impulse;
            _offset = new Vector2(
                Mathf.DeltaAngle(displayEuler.y, desiredEuler.y),
                Mathf.DeltaAngle(displayEuler.x, desiredEuler.x));

            float window = Mathf.Max(1f, cone * 1.5f);
            _clampedOffset = new Vector2(
                Mathf.Clamp(_offset.x, -window, window),
                Mathf.Clamp(_offset.y, -window, window));

            // Cache the weapon offset quaternion once per tick
            if (_initialized && HandlingConfig.DeadzoneEnabled.Value && HandlingState.IsAiming)
                _cachedWeaponOffset = _weaponRot * Quaternion.Inverse(_cameraRot);
            else
                _cachedWeaponOffset = Quaternion.identity;
        }

        /// <summary>
        /// Returns the locked camera rotation to use for the head while aiming.
        /// </summary>
        public static Vector3 Modify(Vector3 headRotation)
        {
            if (!HandlingConfig.DeadzoneEnabled.Value || !HandlingState.HasWeapon || !HandlingState.IsAiming)
            {
                return headRotation;
            }

            return (_cameraRot * Quaternion.Euler(_impulse)).eulerAngles;
        }

        public static void Reset()
        {
            _weaponRot = Quaternion.identity;
            _cameraRot = Quaternion.identity;
            _initialized = false;
            _offset = Vector2.zero;
            _clampedOffset = Vector2.zero;
            _impulse = Vector3.zero;
            _cachedWeaponOffset = Quaternion.identity;
        }
    }
}
