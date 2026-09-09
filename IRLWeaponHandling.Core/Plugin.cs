using BepInEx;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Patches;
using IRLWeaponHandling.Utilities;

namespace IRLWeaponHandling.Core;

[BepInPlugin("com.tarkovrl", "Tarkov Real Life", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
	public const string GUID = "com.tarkovrl";

	public const string Name = "Tarkov Real Life";

	public const string Version = "1.0.0";

	private void Awake()
	{
		Log.Init(base.Logger);
		HandlingConfig.Init(base.Config);
		PatchRegistry.EnableAll();
		Log.Info("Tarkov Real Life 1.0.0 loaded");
	}
}
