using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
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

	public override void Load()
	{
		//
	}
	public override void LoadPuzzleContent()
	{
		ComputationPart.LoadPuzzleContent();
		ComputationPart.LoadHooking();



		DebugLoadPuzzleContent();


	}
	public override void Unload()
	{
		ComputationPart.UnloadHooking();
	}
	public override void PostLoad()
	{
		//

		// read-in computation.yamls and add to dictionary
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

	static Molecule saltLeft = new Molecule();
	static Molecule saltRight = new Molecule();
	static AtomType salt => class_175.field_1675;
	static AtomType lead => class_175.field_1681;
	static AtomType tin => class_175.field_1683;
	static AtomType air => class_175.field_1676;
	static AtomType water => class_175.field_1679;
	static AtomType fire => class_175.field_1678;
	static AtomType earth => class_175.field_1677;

	public static void DebugLoadPuzzleContent()
	{
		var firstInput = new API.IOIndex(0, true);
		var firstOutput = new API.IOIndex(0, false);
		var secondOutput = new API.IOIndex(1, false);




		saltLeft.method_1105(new Atom(salt), new HexIndex(0, 0));
		foreach (var hex in new HexIndex[3] { new HexIndex(-1, 0), new HexIndex(0, 1), new HexIndex(1, -1) })
		{
			saltLeft.method_1105(new Atom(salt), hex);
			saltLeft.method_1111((enum_126)1, new HexIndex(0, 0), hex);
		}
		saltRight = saltLeft.method_1115(new HexRotation(1));
		var saltMolecules = new List<Molecule>() { saltLeft, saltRight };

		var singleAtomMolecule = Molecule.method_1121(salt);

		var def1 = new API.IOGlyph(firstInput, API.GetProfileFromMolecules(saltMolecules));

		var def3 = new API.IOGlyph(firstOutput, singleAtomMolecule, API.GetFootprintFromMolecule(saltLeft));


		API.AddComputationPuzzleDefinition("computation-example-1", new List<API.IOGlyph>() { def1 }, (_) => new ComputationManagerTest1());

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





		API.AddSimpleComputationPuzzleDefinition(
			"computation-example-3",
			new List<Dictionary<API.IOIndex, List<Molecule>>>
			{
				new Dictionary<API.IOIndex, List<Molecule>>()
				{
					{ firstOutput, new(){ Molecule.method_1121(air), Molecule.method_1121(earth), Molecule.method_1121(water), Molecule.method_1121(fire) } }
				},
			}
		);


		API.AddSimpleComputationPuzzleDefinition(
			"computation-example-4",
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
	}
}
