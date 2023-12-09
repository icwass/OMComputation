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
	private class ComputationManagerTest2 : API.ComputationManagerBase
	{
		Random random = new Random(1111);
		public override void AddMoleculesToQueues(API.IOIndex ioIndex)
		{
			AddMoleculeToQueue(ioIndex, Molecule.method_1121(random.Next(5) < 3 ? tin : lead));
		}
	}
	private class ComputationManagerTest3 : API.ComputationManagerBase
	{
		public override void AddMoleculesToQueues(API.IOIndex ioIndex)
		{
			AddMoleculeToQueue(ioIndex, Molecule.method_1121(air));
			AddMoleculeToQueue(ioIndex, Molecule.method_1121(earth));
			AddMoleculeToQueue(ioIndex, Molecule.method_1121(water));
			AddMoleculeToQueue(ioIndex, Molecule.method_1121(fire));
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
		saltLeft.method_1105(new Atom(salt), new HexIndex(0, 0));
		foreach (var hex in new HexIndex[3] { new HexIndex(-1, 0), new HexIndex(0, 1), new HexIndex(1, -1) })
		{
			saltLeft.method_1105(new Atom(salt), hex);
			saltLeft.method_1111((enum_126)1, new HexIndex(0, 0), hex);
		}
		saltRight = saltLeft.method_1115(new HexRotation(1));
		var saltMolecules = new List<Molecule>() { saltLeft, saltRight };

		var singleAtomMolecule = Molecule.method_1121(salt);

		var def1 = new API.IODefinition(new API.IOIndex(0, true), API.GetFootprintFromMolecules(saltMolecules), API.GetProfileFromMolecules(saltMolecules));

		var def2 = new API.IODefinition(new API.IOIndex(0, true), API.GetFootprintFromMolecule(singleAtomMolecule), singleAtomMolecule);

		var def3 = new API.IODefinition(new API.IOIndex(0, false), API.GetFootprintFromMolecule(saltLeft), singleAtomMolecule);


		API.AddComputationPuzzleDefinition("computation-example-1", new List<API.IODefinition>() { def1 }, (_) => new ComputationManagerTest1());
		API.AddComputationPuzzleDefinition("computation-example-2", new List<API.IODefinition>() { def2 }, (_) => new ComputationManagerTest2());
		API.AddComputationPuzzleDefinition("computation-example-3", new List<API.IODefinition>() { def3 }, (_) => new ComputationManagerTest3());
	}
}
