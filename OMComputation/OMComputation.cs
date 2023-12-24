using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Serialization;
using Quintessential.Settings;
using SDL2;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace OMComputation;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class MainClass : QuintessentialMod
{
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public override Type SettingsType => typeof(MySettings);
	public static QuintessentialMod MainClassAsMod;
	public class MySettings
	{
		public static MySettings Instance => MainClassAsMod.Settings as MySettings;

		[SettingsLabel("Enable Computation Mode")]
		public bool enableComputationMode = false;
		[SettingsLabel("")]
		public DisplaySettings displayEditingSettings = new();
		public class DisplaySettings : SettingsGroup
		{
			public override bool Enabled => Instance.enableComputationMode;

			//[SettingsLabel("Show the origin on the navigation map.")]
			//public bool showCritelliOnMap = false;
			//[SettingsLabel("Open Map")]
			//public Keybinding KeyShowMap = new() { Key = "M" };
		}
	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		var SET = (MySettings)Settings;
		ComputationPart.enableComputationMode = SET.enableComputationMode;
	}

	public override void Load()
	{
		MainClassAsMod = this;
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		ComputationPart.LoadPuzzleContent();
		ComputationPart.LoadHooking();

		// read-in computation.yamls and add to dictionary
		foreach (var mod in QuintessentialLoader.Mods)
		{
			LoadComputationDefinitionsFromMod(mod);
		}
	}
	public override void Unload()
	{
		ComputationPart.UnloadHooking();
	}
	public override void PostLoad()
	{
		//
	}

	private void LoadComputationDefinitionsFromMod(ModMeta mod)
	{
		// based somewhat on LoadModCampaigns from Quintessential/QuintessentialLoader.cs
		if (mod == QuintessentialLoader.QuintessentialModMeta) return;
		var puzzles = Path.Combine(mod.PathDirectory, "Puzzles");
		if (!Directory.Exists(puzzles)) return;

		foreach (var item in Directory.GetFiles(puzzles))
		{
			string filename = Path.GetFileName(item);
			if (!filename.EndsWith(".computation.yaml")) continue;

			using StreamReader reader = new(item);
			var model = YamlHelper.Deserializer.Deserialize<ComputationPuzzleDefinitionModel>(reader);
			model.AddDefinitionFromModel(item);
		}
	}
}
