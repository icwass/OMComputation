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



		DebugLoadPuzzleContent();

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




	private class ComputationManagerTest1 : API.ComputationManagerBase
	{
		bool left = true;
		public override void AddMoleculesToQueues(API.IOIndex ioIndex)
		{
			AddMoleculeToQueue(ioIndex, left ? saltLeft : saltRight);
			left = !left;
		}
	}



	static Molecule cardinalEnergizer(AtomType cardinal, bool energized)
	{
		var ret = Molecule.method_1122(fire, cardinal);
		var hex0 = new HexIndex(1, 0);
		var hex1 = new HexIndex(1, -1);
		ret.method_1105(new Atom(cardinal), hex1);
		ret.method_1111(enum_126.Standard, new HexIndex(0, 0), hex1);
		if (energized)
		{
			ret.method_1111(enum_126.Prisma0, hex0, hex1);
			ret.method_1111(enum_126.Prisma1, hex0, hex1);
			ret.method_1111(enum_126.Prisma2, hex0, hex1);
		}
		return ret;
	}

	private class ComputationManagerTest5 : API.ComputationManagerBase
	{
		Random[] random = new Random[2] { new(1), new(1) };
		public override void AddMoleculesToQueues(API.IOIndex ioIndex)
		{
			bool isInput = ioIndex.isInput;
			int i = random[isInput ? 0 : 1].Next(3);
			AddMoleculeToQueue(ioIndex, cardinalEnergizer(new AtomType[3] { earth, water, air }[i], !isInput));
		}
	}

	static Molecule saltLeft = new Molecule();
	static Molecule saltRight = new Molecule();
	static AtomType salt => class_175.field_1675;
	static AtomType air => class_175.field_1676;
	static AtomType water => class_175.field_1679;
	static AtomType fire => class_175.field_1678;
	static AtomType earth => class_175.field_1677;

	static AtomType quicksilver => class_175.field_1680;
	static AtomType lead => class_175.field_1681;
	static AtomType tin => class_175.field_1683;
	static AtomType iron => class_175.field_1684;
	static AtomType copper => class_175.field_1682;
	static AtomType silver => class_175.field_1685;
	static AtomType gold => class_175.field_1686;

	public static void DebugLoadPuzzleContent()
	{
		var firstInput = API.IOIndex.Input(0);
		var secondInput = API.IOIndex.Input(1);
		var firstOutput = API.IOIndex.Output(0);
		var secondOutput = API.IOIndex.Output(1);




		saltLeft.method_1105(new Atom(salt), new HexIndex(0, 0));
		foreach (var hex in new HexIndex[3] { new HexIndex(-1, 0), new HexIndex(0, 1), new HexIndex(1, -1) })
		{
			saltLeft.method_1105(new Atom(salt), hex);
			saltLeft.method_1111((enum_126)1, new HexIndex(0, 0), hex);
		}
		saltRight = saltLeft.method_1115(new HexRotation(1));
		var saltMolecules = new List<Molecule>() { saltLeft, saltRight };

		var singleAtomMolecule = Molecule.method_1121(salt);

		var def1 = new API.IOGlyph(firstInput, internalAPI.GetProfileFromMolecules(saltMolecules));

		var def3 = new API.IOGlyph(firstOutput, singleAtomMolecule, internalAPI.GetFootprintFromMolecule(saltLeft));

		var def5A = new API.IOGlyph(firstInput, cardinalEnergizer(salt, false));
		var def5B = new API.IOGlyph(firstOutput, cardinalEnergizer(salt, true));


		internalAPI.AddComputationPuzzleDefinition("computation-example-1", new List<API.IOGlyph>() { def1 }, (_, _) => new ComputationManagerTest1());


		/*
		API.AddSimpleComputationPuzzleDefinition(
			"computation-example-2",
			new Dictionary<API.IOIndex, List<Molecule>>()
			{
				{ firstInput, new(){ Molecule.method_1121(lead), Molecule.method_1121(lead), Molecule.method_1121(lead), Molecule.method_1121(lead) } }
			},
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(lead), Molecule.method_1121(lead), Molecule.method_1121(tin) } }
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(lead), Molecule.method_1121(tin), Molecule.method_1121(lead) } }
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(tin), Molecule.method_1121(lead), Molecule.method_1121(lead) } }
				},
			}
		);
		*/




		internalAPI.AddSimpleComputationPuzzleDefinition(
			"computation-example-3",
			new Dictionary<API.IOIndex, List<Molecule>>(),
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstOutput, new(){ Molecule.method_1121(air), Molecule.method_1121(earth), Molecule.method_1121(water), Molecule.method_1121(fire) } }
				},
			}
		);


		internalAPI.AddSimpleComputationPuzzleDefinition(
			"computation-example-4",
			new Dictionary<API.IOIndex, List<Molecule>>(),
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(salt, salt), Molecule.method_1121(salt) } },
					{ firstOutput, new(){ Molecule.method_1121(salt)} },
					{ secondOutput, new(){ Molecule.method_1121(salt)} },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(salt, salt), Molecule.method_1121(fire) } },
					{ firstOutput, new(){ Molecule.method_1121(salt)} },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(salt, fire), Molecule.method_1121(salt) } },
					{ firstOutput, new(){ Molecule.method_1121(salt)} },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(salt, fire), Molecule.method_1121(fire) } },
					{ firstOutput, new(){ Molecule.method_1121(salt) } },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(fire, salt), Molecule.method_1121(salt) } },
					{ firstOutput, new(){ Molecule.method_1121(salt)} },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(fire, salt), Molecule.method_1121(fire) } },
					{ firstOutput, new(){ Molecule.method_1121(salt) } },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(fire, fire), Molecule.method_1121(salt) } },
					{ firstOutput, new(){ Molecule.method_1121(salt) } },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(fire, fire), Molecule.method_1121(fire) } },
					{ firstOutput, new(){ Molecule.method_1121(fire) } },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1122(fire, fire), Molecule.method_1121(fire) } },
					{ firstOutput, new(){ Molecule.method_1121(fire) } },
					{ secondOutput, new(){ Molecule.method_1121(fire) } },
				},
			},
			123456789 // seed for the random-number generator
		);

		/*
		internalAPI.AddSimpleComputationPuzzleDefinition(
			"computation-example-5",
			new Dictionary<API.IOIndex, List<Molecule>>()
			{
				{ firstInput, new() { cardinalEnergizer(earth, false), cardinalEnergizer(water, false), cardinalEnergizer(air, false), } },
				{ firstOutput, new() { cardinalEnergizer(earth, true), cardinalEnergizer(water, true), cardinalEnergizer(air, true), } }
			},
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ cardinalEnergizer(earth, false) } },
					{ firstOutput, new(){ cardinalEnergizer(earth, true) } }
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ cardinalEnergizer(water, false) } },
					{ firstOutput, new(){ cardinalEnergizer(water, true) } }
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ cardinalEnergizer(air, false) } },
					{ firstOutput, new(){ cardinalEnergizer(air, true) } }
				},
			},
			1 // seed
		);
		*/
		internalAPI.AddComputationPuzzleDefinition("computation-example-5", new List<API.IOGlyph>() { def5A, def5B }, (_, _) =>
		{
			var ret = new ComputationManagerTest5();

			ret.CurrentMolecule(firstInput);
			ret.CurrentMolecule(firstOutput);
			//ret.ChangeQueueAllowance(firstInput, 1);

			return ret;
		});

		
		internalAPI.AddSimpleComputationPuzzleDefinition(
			"computation-example-6",
			new Dictionary<API.IOIndex, List<Molecule>>(),
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(iron)} },
					{ firstOutput, new(){ Molecule.method_1121(copper)} },
					{ secondInput, new(){ Molecule.method_1121(tin)} },
					{ secondOutput, new(){ Molecule.method_1121(lead)} },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(copper) } },
					{ firstOutput, new(){ Molecule.method_1121(silver)} },
					{ secondInput, new(){ Molecule.method_1121(iron) } },
					{ secondOutput, new(){ Molecule.method_1121(tin) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(silver) } },
					{ firstOutput, new(){ Molecule.method_1121(gold)} },
					{ secondInput, new(){ Molecule.method_1121(copper) } },
					{ secondOutput, new(){ Molecule.method_1121(iron) } },
				},
			},
			1
		);

		
		internalAPI.AddSimpleComputationPuzzleDefinition(
			"rmc-computation",
			new Dictionary<API.IOIndex, List<Molecule>>()
			{
				{ firstInput, new(){ Molecule.method_1121(iron), Molecule.method_1121(silver), Molecule.method_1121(lead), Molecule.method_1121(copper), Molecule.method_1121(tin), Molecule.method_1121(gold) } },
				{ firstOutput, new(){ Molecule.method_1121(iron), Molecule.method_1121(silver), Molecule.method_1121(lead), Molecule.method_1121(copper), Molecule.method_1121(tin), Molecule.method_1121(gold) } },
				{ secondOutput, new(){ Molecule.method_1121(iron), Molecule.method_1121(silver), Molecule.method_1121(lead), Molecule.method_1121(copper), Molecule.method_1121(tin), Molecule.method_1121(gold) } },
				{ secondInput, new(){ Molecule.method_1121(quicksilver), Molecule.method_1121(quicksilver), Molecule.method_1121(quicksilver), Molecule.method_1121(quicksilver), Molecule.method_1121(quicksilver), Molecule.method_1121(quicksilver) } },
			},
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(lead) } },
					{ firstOutput, new(){ Molecule.method_1121(lead) } },
					{ secondOutput, new(){ Molecule.method_1121(lead) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(tin) } },
					{ firstOutput, new(){ Molecule.method_1121(tin) } },
					{ secondOutput, new(){ Molecule.method_1121(tin) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(iron) } },
					{ firstOutput, new(){ Molecule.method_1121(iron) } },
					{ secondOutput, new(){ Molecule.method_1121(iron) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(copper) } },
					{ firstOutput, new(){ Molecule.method_1121(copper) } },
					{ secondOutput, new(){ Molecule.method_1121(copper) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(silver) } },
					{ firstOutput, new(){ Molecule.method_1121(silver) } },
					{ secondOutput, new(){ Molecule.method_1121(silver) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstInput, new(){ Molecule.method_1121(gold) } },
					{ firstOutput, new(){ Molecule.method_1121(gold) } },
					{ secondOutput, new(){ Molecule.method_1121(gold) } },
					{ secondInput, new(){ Molecule.method_1121(quicksilver) } },
				},
			}
		);
		

	}
}
