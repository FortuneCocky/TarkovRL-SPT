using BepInEx.Configuration;

namespace IRLWeaponHandling.Configuration;

internal static class HandlingConfig
{
	private const string GeneralSection = "1. General";

	private const string AimingSection = "2. Aiming";

	private const string SwaySection = "3. Sway & Inertia";

	private const string DeadzoneSection = "4. Aim Deadzone";

	private const string ShotSection = "5. Shot Misalignment";

	private const string CameraSection = "6. Camera Lag";

	private const string LeanSection = "7. Turn Lean";

	private const string MomentumSection = "8 - Momentum";

	private const string FluidSection = "10. Fluid Movement";

	private const string LegCrippleSection = "11. Leg Cripple";

	public static ConfigEntry<bool> Enabled { get; private set; }

	public static ConfigEntry<bool> VerboseLogging { get; private set; }

	public static ConfigEntry<bool> SprintCancelAds { get; private set; }

	public static ConfigEntry<bool> AimingEnabled { get; private set; }

	public static ConfigEntry<float> LightWeightKg { get; private set; }

	public static ConfigEntry<float> HeavyWeightKg { get; private set; }

	public static ConfigEntry<float> AdsTimeLight { get; private set; }

	public static ConfigEntry<float> AdsTimeHeavy { get; private set; }

	public static ConfigEntry<float> ErgonomicsInfluence { get; private set; }

	public static ConfigEntry<float> StaminaAdsInfluence { get; private set; }

	public static ConfigEntry<bool> SwayEnabled { get; private set; }

	public static ConfigEntry<float> SwayMultiplier { get; private set; }

	public static ConfigEntry<float> HipSwayMultiplier { get; private set; }

	public static ConfigEntry<float> AimSwayMultiplier { get; private set; }

	public static ConfigEntry<float> MotionInertiaMultiplier { get; private set; }

	public static ConfigEntry<float> StaminaSwayInfluence { get; private set; }

	public static ConfigEntry<float> TremorMultiplier { get; private set; }

	// === Enhanced sway layers ===
	public static ConfigEntry<float> SwayMuzzleAmplification { get; private set; }
	public static ConfigEntry<float> SwayInertiaOvershoot { get; private set; }
	public static ConfigEntry<float> SwayBreathingStrength { get; private set; }
	public static ConfigEntry<float> SwayBreathingSpeed { get; private set; }
	public static ConfigEntry<float> SwayWalkBobStrength { get; private set; }
	public static ConfigEntry<float> SwayWalkBobSpeed { get; private set; }
	public static ConfigEntry<float> SwayFatigueMicroSway { get; private set; }
	public static ConfigEntry<float> SwayTurnLeadStrength { get; private set; }

	public static ConfigEntry<bool> DeadzoneEnabled { get; private set; }

	public static ConfigEntry<float> DeadzoneAds { get; private set; }

	public static ConfigEntry<float> DeadzoneFollowSpeed { get; private set; }

	public static ConfigEntry<float> DeadzoneWeaponSmooth { get; private set; }

	public static ConfigEntry<float> DeadzoneCameraLag { get; private set; }

	public static ConfigEntry<float> DeadzoneMotionInfluence { get; private set; }

	public static ConfigEntry<bool> HipDeadzoneEnabled { get; private set; }

	public static ConfigEntry<bool> WeaponDeadzoneEnabled { get; private set; }

	public static ConfigEntry<float> WeaponDeadzoneMulti { get; private set; }

	public static ConfigEntry<float> WeaponDeadzoneFollowSpeed { get; private set; }

	public static ConfigEntry<float> HipDeadzoneCone { get; private set; }

	public static ConfigEntry<float> HipWeaponSmooth { get; private set; }

	public static ConfigEntry<float> HipCameraLag { get; private set; }

	public static ConfigEntry<float> HipDeadzoneInfluence { get; private set; }

	public static ConfigEntry<float> HipNaturalMotion { get; private set; }

	public static ConfigEntry<float> HipOrganicDrift { get; private set; }

	public static ConfigEntry<float> HipMouseWhip { get; private set; }

	public static ConfigEntry<float> HipDirectionalTilt { get; private set; }

	// Procedural arm/wrist motion layers for hip fire
	public static ConfigEntry<float> HipStrafeRoll { get; private set; }
	public static ConfigEntry<float> HipAccelSway { get; private set; }
	public static ConfigEntry<float> HipWristRoll { get; private set; }
	public static ConfigEntry<float> HipFloatMotion { get; private set; }

	public static ConfigEntry<float> SprintTransitionSpeed { get; private set; }

	public static ConfigEntry<bool> ReloadEfficiencyEnabled { get; private set; }

	public static ConfigEntry<float> ReloadEfficiencyInfluence { get; private set; }

	public static ConfigEntry<float> ReloadSprintPenalty { get; private set; }

	public static ConfigEntry<bool> ShotMisalignmentEnabled { get; private set; }

	public static ConfigEntry<float> ShotMisalignmentStrength { get; private set; }

	public static ConfigEntry<float> ShotMisalignmentMax { get; private set; }

	public static ConfigEntry<float> ShotBreathPenalty { get; private set; }

	public static ConfigEntry<float> StaminaShotInfluence { get; private set; }

	public static ConfigEntry<bool> CameraLagEnabled { get; private set; }

	public static ConfigEntry<float> CameraSwayFollow { get; private set; }

	public static ConfigEntry<float> CameraSwayFollowSpeed { get; private set; }

	public static ConfigEntry<float> CameraSettleSmoothing { get; private set; }

	public static ConfigEntry<bool> LeanEnabled { get; private set; }

	public static ConfigEntry<float> LeanCameraAngle { get; private set; }

	public static ConfigEntry<float> LeanWeaponAngle { get; private set; }

	public static ConfigEntry<float> LeanTurnRate { get; private set; }

	public static ConfigEntry<float> LeanMultiplier { get; private set; }

	// Procedural Q/E lean: extra roll from the vanilla lean input
	public static ConfigEntry<bool> ProceduralLeanEnabled { get; private set; }
	public static ConfigEntry<float> ProceduralLeanRoll { get; private set; }
	public static ConfigEntry<float> ProceduralLeanWeaponRoll { get; private set; }
	public static ConfigEntry<float> ProceduralLeanSmooth { get; private set; }

	public static ConfigEntry<bool> MomentumEnabled { get; private set; }

	public static ConfigEntry<float> MomentumAmount { get; private set; }

	public static ConfigEntry<float> MomentumWeight { get; private set; }

	public static ConfigEntry<float> MomentumTurn { get; private set; }

	public static ConfigEntry<float> MomentumStopDelay { get; private set; }

	public static ConfigEntry<float> MomentumWalkForwardLean { get; private set; }

	public static ConfigEntry<float> MomentumWalkSideLean { get; private set; }

	// Fluid Movement (removed — all bob features removed by user request)

	// Leg Cripple
	public static ConfigEntry<bool> LegCrippleEnabled { get; private set; }
	public static ConfigEntry<bool> LegCrippleEnablePlayer { get; private set; }
	public static ConfigEntry<bool> LegCrippleEnableBots { get; private set; }
	public static ConfigEntry<float> LegCrippleCrawlSpeed { get; private set; }
	public static ConfigEntry<float> LegCrippleCrouchHealthThreshold { get; private set; }

	public static void Init(ConfigFile config)
	{
		Enabled = config.Bind("1. General", "Enable mod", defaultValue: true, "Master switch. When off, all handling systems leave the vanilla values untouched.");
		VerboseLogging = config.Bind("1. General", "Verbose logging", defaultValue: true, "Log recalculated weapon handling values to the BepInEx console.");
		SprintCancelAds = config.Bind("1. General", "Cancel sprint to ADS on right click", defaultValue: true, "Pressing right mouse while sprinting cancels sprint and immediately aims down sights.");
		AimingEnabled = config.Bind("2. Aiming", "Enable aiming changes", defaultValue: true, "Scale ADS speed by weapon weight, ergonomics and hands stamina.");
		LightWeightKg = config.Bind("2. Aiming", "Light weapon weight (kg)", 2f, new ConfigDescription("Weight treated as a light weapon.", new AcceptableValueRange<float>(0.5f, 8f)));
		HeavyWeightKg = config.Bind("2. Aiming", "Heavy weapon weight (kg)", 10f, new ConfigDescription("Weight treated as a heavy weapon.", new AcceptableValueRange<float>(3f, 20f)));
		AdsTimeLight = config.Bind("2. Aiming", "ADS time, light weapon (s)", 1f, new ConfigDescription("Time to fully aim a light, high ergonomics weapon.", new AcceptableValueRange<float>(0.05f, 2f)));
		AdsTimeHeavy = config.Bind("2. Aiming", "ADS time, heavy weapon (s)", 1f, new ConfigDescription("Time to fully aim a heavy, high ergonomics weapon.", new AcceptableValueRange<float>(0.1f, 3f)));
		ErgonomicsInfluence = config.Bind("2. Aiming", "Ergonomics influence", 1f, new ConfigDescription("How much low ergonomics adds to the ADS time. 0 disables the ergonomics term.", new AcceptableValueRange<float>(0f, 2f)));
		StaminaAdsInfluence = config.Bind("2. Aiming", "Hands stamina influence", 1f, new ConfigDescription("How much drained hands stamina slows aiming down.", new AcceptableValueRange<float>(0f, 1f)));
		SwayEnabled = config.Bind("3. Sway & Inertia", "Enable sway changes", defaultValue: true, "Scale sway, weapon inertia and tremor by weight, ergonomics and stamina.");
		SwayMultiplier = config.Bind("3. Sway & Inertia", "Sway multiplier", 0.4f, new ConfigDescription("Global multiplier on the ergonomic weight that drives sway strength.", new AcceptableValueRange<float>(0.1f, 4f)));
		HipSwayMultiplier = config.Bind("3. Sway & Inertia", "Hip sway multiplier", 1.2f, new ConfigDescription("Extra sway while not aiming.", new AcceptableValueRange<float>(0.1f, 4f)));
		AimSwayMultiplier = config.Bind("3. Sway & Inertia", "Aim sway multiplier", 0.6295775f, new ConfigDescription("Extra sway while aiming.", new AcceptableValueRange<float>(0.1f, 4f)));
		MotionInertiaMultiplier = config.Bind("3. Sway & Inertia", "Weapon inertia multiplier", 1.3f, new ConfigDescription("How much the weapon lags behind player movement and mouse motion.", new AcceptableValueRange<float>(0.1f, 4f)));
		StaminaSwayInfluence = config.Bind("3. Sway & Inertia", "Stamina influence", 1f, new ConfigDescription("How much drained stamina and oxygen add to sway.", new AcceptableValueRange<float>(0f, 2f)));
		TremorMultiplier = config.Bind("3. Sway & Inertia", "Tremor multiplier", 0.81f, new ConfigDescription("Multiplier on hand shake / tremor intensity.", new AcceptableValueRange<float>(0.1f, 4f)));
		// === Enhanced sway layers ===
		SwayMuzzleAmplification = config.Bind("3.1 Sway Layers", "Muzzle tip amplification", 3f, new ConfigDescription("Amplifies the gun tip rotation from mouse-driven sway. 1 = vanilla, 1.5 = 50% more muzzle swing, 2 = double. Makes the gun tip rotate more dramatically.", new AcceptableValueRange<float>(0.5f, 4f)));
		SwayInertiaOvershoot = config.Bind("3.1 Sway Layers", "Inertia overshoot", 0.4976526f, new ConfigDescription("How much the weapon overshoots past center when the mouse stops. 0 = critically damped (no overshoot), 0.5 = slight bounce, 1 = pronounced oscillation. Adds natural springiness.", new AcceptableValueRange<float>(0f, 1f)));
		SwayBreathingStrength = config.Bind("3.1 Sway Layers", "Breathing sway", 0.6f, new ConfigDescription("Slow sinusoidal weapon pitch/yaw from breathing, independent of vanilla breath. Scales with fatigue. 0 = off, 1 = noticeable chest breathing moving the gun.", new AcceptableValueRange<float>(0f, 3f)));
		SwayBreathingSpeed = config.Bind("3.1 Sway Layers", "Breathing speed", 0.8f, new ConfigDescription("Speed of the breathing sway cycle. 0.5 = slow deep breaths, 1 = normal, 2 = fast panting. Faster when fatigued.", new AcceptableValueRange<float>(0.2f, 3f)));
		SwayWalkBobStrength = config.Bind("3.1 Sway Layers", "Walk bob rotation", 0.3f, new ConfigDescription("Weapon tip pitches and rolls in sync with footsteps while walking. 0 = off, 1 = natural bob, 2 = heavy marching bob. Scales with movement speed.", new AcceptableValueRange<float>(0f, 3f)));
		SwayWalkBobSpeed = config.Bind("3.1 Sway Layers", "Walk bob speed", 7f, new ConfigDescription("Cadence of the walk bob in cycles per second. 5 = slow heavy steps, 7 = normal walk, 10 = jogging.", new AcceptableValueRange<float>(2f, 15f)));
		SwayFatigueMicroSway = config.Bind("3.1 Sway Layers", "Fatigue micro-sway", 1.140845f, new ConfigDescription("High-frequency weapon tip tremor when stamina is low. Simulates trembling arms from exhaustion. 0 = off, 1 = subtle, 2 = heavy trembling.", new AcceptableValueRange<float>(0f, 3f)));
		SwayTurnLeadStrength = config.Bind("3.1 Sway Layers", "Turn muzzle lead", 2f, new ConfigDescription("The gun tip leads slightly in the turn direction, then settles back when you stop turning. Like actively pointing the weapon. 0 = off, 1 = natural, 2 = pronounced lead.", new AcceptableValueRange<float>(0f, 3f)));
		DeadzoneEnabled = config.Bind("4. Aim Deadzone", "Enable ADS deadzone", defaultValue: true, "The camera lags behind the gun while aiming down sights.");
		DeadzoneAds = config.Bind("4. Aim Deadzone", "ADS deadzone (deg)", 12f, new ConfigDescription("Size of the free-aim window while aiming down sights.", new AcceptableValueRange<float>(0f, 12f)));
		DeadzoneFollowSpeed = config.Bind("4. Aim Deadzone", "ADS follow speed", 25.50939f, new ConfigDescription("How fast the camera follows the gun once it leaves the ADS deadzone.", new AcceptableValueRange<float>(0.1f, 30f)));
		DeadzoneWeaponSmooth = config.Bind("4. Aim Deadzone", "ADS weapon smoothness", 4.311267f, new ConfigDescription("How smoothly the weapon tracks the mouse inside the ADS deadzone cone. Lower = heavier/more lag, higher = snappier.", new AcceptableValueRange<float>(0.1f, 30f)));
		DeadzoneCameraLag = config.Bind("4. Aim Deadzone", "ADS camera lag speed", 14f, new ConfigDescription("How fast the camera catches up once the weapon exits the ADS deadzone cone. Lower = more lag, higher = snappier.", new AcceptableValueRange<float>(0.1f, 30f)));
		DeadzoneMotionInfluence = config.Bind("4. Aim Deadzone", "ADS motion influence", 1.183098f, new ConfigDescription("How much player movement/turning adds procedural drift to the weapon while aiming. 0 = none, higher = more drift.", new AcceptableValueRange<float>(0f, 3f)));
		HipDeadzoneEnabled = config.Bind("4.1 Hip Deadzone", "Enable hip deadzone", defaultValue: true, "Procedural non-ADS deadzone. The arms/gun track the mouse while the camera stays detached. The camera only follows once the weapon reaches the cone edge.");
		WeaponDeadzoneEnabled = config.Bind("4.1 Hip Deadzone", "Enable weapon deadzone", defaultValue: true, "Direct camera cone-clamp deadzone ported from Tarkov Real Life. The camera stays locked inside a cone and only follows the mouse once it reaches the edge. Works in all states (hip and ADS).");
		WeaponDeadzoneMulti = config.Bind("4.1 Hip Deadzone", "Weapon deadzone multiplier", 3.3f, new ConfigDescription("Controls the deadzone cone size. The cone angle is this value x 10 degrees. 0.3 = 3 deg cone, 0.5 = 5 deg cone.", new AcceptableValueRange<float>(0f, 5f)));
		WeaponDeadzoneFollowSpeed = config.Bind("4.1 Hip Deadzone", "Weapon deadzone follow speed", 20f, new ConfigDescription("How fast the camera catches up once the mouse exits the deadzone cone. Higher = snappier, lower = more lag.", new AcceptableValueRange<float>(0f, 20f)));
		HipDeadzoneCone = config.Bind("4.1 Hip Deadzone", "Deadzone cone (deg)", 14.19953f, new ConfigDescription("How far the weapon can move from the camera before the camera starts following.", new AcceptableValueRange<float>(0f, 25f)));
		HipWeaponSmooth = config.Bind("4.1 Hip Deadzone", "Weapon smoothness", 4.2f, new ConfigDescription("How smoothly the weapon tracks the mouse. Lower = heavier/more lag, higher = snappier.", new AcceptableValueRange<float>(0.1f, 30f)));
		HipCameraLag = config.Bind("4.1 Hip Deadzone", "Camera lag speed", 14.23052f, new ConfigDescription("How fast the camera catches up once the weapon exits the cone. Lower = more lag, higher = snappier.", new AcceptableValueRange<float>(0.1f, 30f)));
		HipDeadzoneInfluence = config.Bind("4.1 Hip Deadzone", "Deadzone influence", 1.2f, new ConfigDescription("How much player movement/turning adds procedural drift to the weapon. 0 = none, higher = more sway.", new AcceptableValueRange<float>(0f, 3f)));
		HipNaturalMotion = config.Bind("4.1 Hip Deadzone", "Natural motion", 0.8f, new ConfigDescription("Overall strength of human-like weapon motion at hip. Adds organic drift, mouse whip and directional tilt for an immersive feel. 0 = off, 1 = natural, higher = more pronounced.", new AcceptableValueRange<float>(0f, 3f)));
		HipOrganicDrift = config.Bind("4.1 Hip Deadzone", "Organic drift", 1.2f, new ConfigDescription("Perlin-noise micro-drift simulating hands holding a weapon. 0 = robotic/still, 1 = subtle human tremor, 2 = heavy drift.", new AcceptableValueRange<float>(0f, 2f)));
		HipMouseWhip = config.Bind("4.1 Hip Deadzone", "Mouse whip", 2.408451f, new ConfigDescription("How much the weapon whips and lags behind fast mouse movements, then springs back. 0 = rigid, 1 = natural, 2 = heavy lag.", new AcceptableValueRange<float>(0f, 3f)));
		HipDirectionalTilt = config.Bind("4.1 Hip Deadzone", "Directional tilt", 1.5f, new ConfigDescription("Weapon roll/tilt in the direction of mouse motion, like wrists flexing. 0 = none, 1 = subtle, 2 = pronounced.", new AcceptableValueRange<float>(0f, 3f)));
		HipStrafeRoll = config.Bind("4.1 Hip Deadzone", "Strafe wrist roll", 0f, new ConfigDescription("Weapon rolls (wrist rotation) when strafing left/right, as if the arms swing with body momentum. 0 = off, 1 = natural, 2 = pronounced.", new AcceptableValueRange<float>(0f, 3f)));
		HipAccelSway = config.Bind("4.1 Hip Deadzone", "Acceleration sway", 1.614084f, new ConfigDescription("Weapon pitches forward/back when starting or stopping movement, simulating arm inertia. 0 = off, 1 = natural, 2 = heavy.", new AcceptableValueRange<float>(0f, 3f)));
		HipWristRoll = config.Bind("4.1 Hip Deadzone", "Idle wrist roll", 0.713615f, new ConfigDescription("Subtle random wrist roll while holding the weapon at hip, simulating micro-adjustments of the grip. 0 = off, 1 = noticeable.", new AcceptableValueRange<float>(0f, 2f)));
		HipFloatMotion = config.Bind("4.1 Hip Deadzone", "Shoulder pivot tracking", 3f, new ConfigDescription("Procedural tracking system for non-ADS: the gun stays grounded at the shoulder while the arms track mouse movement with a slight lag. The muzzle tip curves in the turn direction, tracing an arc from the stock. Tied to deadzone influence. 0 = off, 1 = natural, 2 = pronounced curve.", new AcceptableValueRange<float>(0f, 3f)));
		SprintTransitionSpeed = config.Bind("4.1 Hip Deadzone", "Sprint transition speed", 20f, new ConfigDescription("How smoothly the hip deadzone blends in/out when starting and stopping sprint. Lower = slower/smoother, higher = snappier.", new AcceptableValueRange<float>(0.5f, 20f)));
		ReloadEfficiencyEnabled = config.Bind("9. Reload Efficiency", "Enable reload efficiency", defaultValue: true, "Reload speed scales with the weapon's ergonomics (efficiency) stat. Higher ergonomics = faster reloads. Disabled by default.");
		ReloadEfficiencyInfluence = config.Bind("9. Reload Efficiency", "Efficiency influence", 1f, new ConfigDescription("How much ergonomics affects reload speed. 0 = no effect, 1 = full scaling (100 ergo = 2x reload speed).", new AcceptableValueRange<float>(0f, 2f)));
		ReloadSprintPenalty = config.Bind("9. Reload Efficiency", "Sprint reload penalty", 0.7511737f, new ConfigDescription("Reload speed reduction when reloading while sprinting. 0 = no penalty, 0.5 = 50% slower, 1 = cannot reload.", new AcceptableValueRange<float>(0f, 1f)));
		ShotMisalignmentEnabled = config.Bind("5. Shot Misalignment", "Enable shot misalignment", defaultValue: true, "Every shot nudges the weapon out of alignment, scaled by weight and ergonomics.");
		ShotMisalignmentStrength = config.Bind("5. Shot Misalignment", "Strength", 1f, new ConfigDescription("Impulse applied to the hands rotation spring per shot.", new AcceptableValueRange<float>(0f, 4f)));
		ShotMisalignmentMax = config.Bind("5. Shot Misalignment", "Maximum per shot (deg)", 1.5f, new ConfigDescription("Clamp on a single shot's misalignment impulse.", new AcceptableValueRange<float>(0.1f, 10f)));
		ShotBreathPenalty = config.Bind("5. Shot Misalignment", "Accuracy loss per shot", 2f, new ConfigDescription("Multiplier on the vanilla breath amplitude gain applied per shot.", new AcceptableValueRange<float>(0f, 3f)));
		StaminaShotInfluence = config.Bind("5. Shot Misalignment", "Stamina recoil influence", 3f, new ConfigDescription("How much drained hands stamina increases shot misalignment and recoil shake.", new AcceptableValueRange<float>(0f, 3f)));
		CameraLagEnabled = config.Bind("6. Camera Lag", "Enable camera lag", defaultValue: true, "The head/camera trails the weapon's sway instead of sitting rigidly on it.");
		CameraSwayFollow = config.Bind("6. Camera Lag", "Sway follow amount", 1f, new ConfigDescription("Fraction of the weapon sway the camera drifts after.", new AcceptableValueRange<float>(0f, 1f)));
		CameraSwayFollowSpeed = config.Bind("6. Camera Lag", "Sway follow speed", 8f, new ConfigDescription("How fast the camera catches up with the weapon sway. Lower lags further behind.", new AcceptableValueRange<float>(0.5f, 20f)));
		CameraSettleSmoothing = config.Bind("6. Camera Lag", "Hip camera smoothing", 1f, new ConfigDescription("Multiplier on the vanilla non-aiming camera settle speed. Below 1 is softer.", new AcceptableValueRange<float>(0.2f, 2f)));
		LeanEnabled = config.Bind("7. Turn Lean", "Enable turn lean", defaultValue: true, "Turning with the mouse leans the body into the turn, so the camera and weapon bank instead of rotating perfectly level.");
		LeanCameraAngle = config.Bind("8. Turn Lean", "Camera lean (deg)", 4f, new ConfigDescription("Maximum camera roll at the reference turn rate. Negative flips the direction of the lean.", new AcceptableValueRange<float>(-10f, 10f)));
		LeanWeaponAngle = config.Bind("8. Turn Lean", "Weapon lean (deg)", 14f, new ConfigDescription("Maximum extra weapon roll at the reference turn rate. Negative flips the direction of the lean.", new AcceptableValueRange<float>(-15f, 15f)));
		LeanTurnRate = config.Bind("8. Turn Lean", "Reference turn rate (deg/s)", 180f, new ConfigDescription("Turn rate that produces the full lean angle. Lower leans on gentler turns.", new AcceptableValueRange<float>(45f, 540f)));
		LeanMultiplier = config.Bind("8. Turn Lean", "Lean amount", 1.5f, new ConfigDescription("0 = no lean, 1 = natural, up to 3 for drastic lean.", new AcceptableValueRange<float>(0f, 3f)));
		// Procedural Q/E lean: adds extra camera and weapon roll when leaning with Q/E,
		// producing a more pronounced body-lean feel on top of the vanilla tilt.
		ProceduralLeanEnabled = config.Bind("8.2 Procedural Lean", "Enable procedural Q/E lean", defaultValue: true, "When leaning with Q/E, adds extra camera and weapon roll for a more pronounced body-lean feel.");
		ProceduralLeanRoll = config.Bind("8.2 Procedural Lean", "Camera roll (deg)", 3f, new ConfigDescription("Additional camera roll from Q/E lean on top of the vanilla tilt.", new AcceptableValueRange<float>(0f, 15f)));
		ProceduralLeanWeaponRoll = config.Bind("8.2 Procedural Lean", "Weapon roll (deg)", 6f, new ConfigDescription("Additional weapon roll from Q/E lean. The weapon banks further than the camera for a natural peeking feel.", new AcceptableValueRange<float>(0f, 20f)));
		ProceduralLeanSmooth = config.Bind("8.2 Procedural Lean", "Smooth speed", 8f, new ConfigDescription("How quickly the procedural lean transitions. Lower = slower/more deliberate, higher = snappier.", new AcceptableValueRange<float>(1f, 20f)));
		MomentumEnabled = config.Bind("8 - Momentum", "Enable player momentum", defaultValue: true, "Adds weight and inertia-driven camera/weapon sway for more realistic movement.");
		MomentumAmount = config.Bind("8 - Momentum", "Momentum amount", 1f, new ConfigDescription("Overall strength of the momentum effect. 0 is off, 1 is natural.", new AcceptableValueRange<float>(0f, 1f)));
		MomentumWeight = config.Bind("8 - Momentum", "Inventory weight influence", 1f, new ConfigDescription("How much inventory weight and inertia scale the effect. 0 is off, 1 is full.", new AcceptableValueRange<float>(0f, 1f)));
		MomentumTurn = config.Bind("8 - Momentum", "Turn momentum", 1f, new ConfigDescription("Extra roll from turning left/right, scaled by weight. 0 is off, 1 is natural.", new AcceptableValueRange<float>(0f, 1f)));
		MomentumStopDelay = config.Bind("8 - Momentum", "Stop delay", 2f, new ConfigDescription("How long the camera/weapon momentum lingers after the player stops moving. 0 is instant, 1 is heavy.", new AcceptableValueRange<float>(0f, 2f)));
		MomentumWalkForwardLean = config.Bind("8 - Momentum", "Walk forward/back lean", 2f, new ConfigDescription("Camera pitch lean from walking forward and backward. Positive leans the camera with the movement.", new AcceptableValueRange<float>(-2f, 2f)));
		MomentumWalkSideLean = config.Bind("8 - Momentum", "Walk side lean", 2f, new ConfigDescription("Camera roll lean from strafing left and right. Positive leans the camera into the strafe.", new AcceptableValueRange<float>(-2f, 2f)));
		// === Leg Cripple ===
		LegCrippleEnabled = config.Bind("11. Leg Cripple", "Enable leg cripple", defaultValue: true, "Master toggle for the leg cripple system.");
		LegCrippleEnablePlayer = config.Bind("11. Leg Cripple", "Enable for player", defaultValue: false, "Apply leg cripple effects to the local player.");
		LegCrippleEnableBots = config.Bind("11. Leg Cripple", "Enable for bots", defaultValue: true, "Apply leg cripple effects to AI bots (PMCs, scavs, etc.).");
		LegCrippleCrawlSpeed = config.Bind("11. Leg Cripple", "Crawl speed", 0.15f, new ConfigDescription("Movement speed multiplier when forced to crawl with both legs blacked. 0.15 = very slow crawl, 0.5 = faster crawl.", new AcceptableValueRange<float>(0.05f, 0.5f)));
		LegCrippleCrouchHealthThreshold = config.Bind("11. Leg Cripple", "Crouch health threshold", 0.2f, new ConfigDescription("When leg health drops below this fraction (0-1) of max, the player/bot is forced into crouch. 0.3 = crouch at 30% leg health.", new AcceptableValueRange<float>(0f, 0.9f)));
	}
}