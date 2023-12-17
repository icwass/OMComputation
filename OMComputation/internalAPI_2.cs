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


using IOIndex = API.IOIndex;
using IOGlyph = API.IOGlyph;
using ComputationManagerBase = API.ComputationManagerBase;
using ComputationManagerMaker = API.ComputationManagerMaker;

public static partial class internalAPI
{
	// public functions in 
	//
	//
	//
	//
	//
	//
	//
	//

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - computation helpers
	public static bool PartIsComputationIO(Sim sim, Part part) => PartIsComputationIO(vanillaAPI.getSolution(sim), part);
	public static bool PartIsComputationIO(SolutionEditorBase seb, Part part) => PartIsComputationIO(vanillaAPI.getSolution(seb), part);
	public static bool PartIsComputationIO(Solution solution, Part part) => vanillaAPI.PartIsStandardIO(part) && IOIndexIsComputationIO(solution, new IOIndex(part));
	public static bool IOIndexIsComputationIO(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = vanillaAPI.getPuzzleID(solution);
		return PuzzleIsComputation(puzzleID) && fetchComputationPuzzleDefinition(puzzleID).IOIndexIsComputationIO(ioIndex);
	}
	////////////////////////////////////////////////////////////
	public static HashSet<HexIndex> GetFootprintFromMolecule(Molecule molecule) => GetFootprintFromMolecules(new List<Molecule>() { molecule });
	public static HashSet<HexIndex> GetFootprintFromMolecules(List<Molecule> molecules)
	{
		var ret = new HashSet<HexIndex>();
		foreach (var molecule in molecules) ret.UnionWith(molecule.method_1100().Keys);
		return ret;
	}
	////////////////////////////////////////////////////////////
	public static Molecule GetProfileFromMolecule(Molecule molecule) => GetProfileFromMolecules(new List<Molecule>() { molecule });
	public static Molecule GetProfileFromMolecules(List<Molecule> molecules)
	{
		var ret = new Molecule();
		foreach (var molecule in molecules)
		{
			foreach (var atom in molecule.method_1100())
			{
				if (!ret.method_1100().ContainsKey(atom.Key)) ret.method_1105(atom.Value, atom.Key);
			}
			foreach (var bond in molecule.method_1101())
			{
				ret.method_1111(bond.field_2186, bond.field_2187, bond.field_2188);
			}
		}
		return ret.method_1104();
	}

	// private data
	private static Dictionary<string, ComputationPuzzleDefinition> computationPuzzleDefinitions = new();
	const string Sim_ComputationManagerField = "OMComputation_Sim_ComputationManagerField";

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// private helper functions
	private static bool PuzzleIsComputation(string puzzleID) => computationPuzzleDefinitions.ContainsKey(puzzleID);
	private static void throwErrorOnInvalidPuzzleID(string puzzleID)
	{
		if (PuzzleIsComputation(puzzleID)) return;
		throw new Exception("[OMComputation] The ComputationPuzzleDefinition for the puzzle '" + puzzleID + "' is missing.");
	}

	private static ComputationPuzzleDefinition fetchComputationPuzzleDefinition(string puzzleID) => computationPuzzleDefinitions[puzzleID];

	private static ComputationManagerBase fetchManagerFromSim(Sim sim)
	{
		if (sim == null) throw new Exception("[OMComputation] fetchManagerFromSim: cannot fetch a ComputationManager for a null Sim.");
		var puzzleID = vanillaAPI.getPuzzleID(sim);
		throwErrorOnInvalidPuzzleID(puzzleID);
		var sim_dyn = new DynamicData(sim);
		var manager = (ComputationManagerBase)sim_dyn.Get(Sim_ComputationManagerField);
		if (manager == null)
		{
			manager = fetchComputationPuzzleDefinition(puzzleID).createComputationManager();
			sim_dyn.Set(Sim_ComputationManagerField, manager);
		}
		return manager;
	}
	private static Sim getSimFromSeb(SolutionEditorBase seb)
	{
		if (seb is SolutionEditorScreen) return getSimFromSes((SolutionEditorScreen)seb);
		if (seb is class_194) return getSimFrom194((class_194)seb);

		Logger.Log("[OMComputation] getSimFromSeb: incompatible SolutionEditorBase encountered.");
		throw new Exception("Could not extract Sim from SolutionEditorBase.");
	}
	private static Sim getSimFromSes(SolutionEditorScreen ses)
	{
		var maybeSim = new DynamicData(ses).Get<Maybe<Sim>>("field_4022");
		return maybeSim.method_1085() ? maybeSim.method_1087() : null;
	}
	private static Sim getSimFrom194(class_194 class194) => class194.method_500();

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// helper classes
	private class ComputationPuzzleDefinition
	{
		string puzzleID = "";
		Dictionary<IOIndex, HashSet<HexIndex>> footprints = new Dictionary<IOIndex, HashSet<HexIndex>>();
		Dictionary<IOIndex, Molecule> profiles = new Dictionary<IOIndex, Molecule>();
		ComputationManagerMaker managerMaker;

		// constructors
		public ComputationPuzzleDefinition(
			string puzzleID,
			List<IOGlyph> ioDefs,
			ComputationManagerMaker managerMaker)
		{
			this.puzzleID = puzzleID;
			foreach (var ioDef in ioDefs)
			{
				footprints.Add(ioDef.ioIndex, ioDef.footprint);
				profiles.Add(ioDef.ioIndex, ioDef.profile);
			}
			this.managerMaker = managerMaker;
		}

		// getters
		public bool IOIndexIsComputationIO(IOIndex ioIndex) => footprints.ContainsKey(ioIndex);
		public HashSet<HexIndex> GetFootprint(IOIndex ioIndex)
		{
			if (footprints.ContainsKey(ioIndex)) return footprints[ioIndex];
			throw invalidIOIndex(ioIndex);
		}
		public Molecule GetProfile(IOIndex ioIndex)
		{
			if (profiles.ContainsKey(ioIndex)) return profiles[ioIndex];
			throw invalidIOIndex(ioIndex);
		}

		// helpers
		public ComputationManagerBase createComputationManager()
		{
			var manager = this.managerMaker(this.puzzleID);
			manager.setPuzzleID(this.puzzleID);
			return manager;
		}

		private Exception invalidIOIndex(IOIndex ioIndex) => new Exception("[OMComputation] Invalid ioIndex " + ioIndex.ToString() + " for puzzle '" + puzzleID + "'.");
	}

	////////////////////////////////////////////////////////////
	public class ComputationManagerSimple : ComputationManagerBase
	{
		List<Dictionary<IOIndex, List<Molecule>>> rulebook;
		Random random;

		public override void AddMoleculesToQueues(IOIndex _)
		{
			int r = random.Next(rulebook.Count);
			Dictionary<IOIndex, List<Molecule>> rule = rulebook[r];
			addMoleculesFromRule(rule);
		}

		private void addMoleculesFromRule(Dictionary<IOIndex, List<Molecule>> rule)
		{
			foreach (var kvp in rule)
			{
				foreach (var molecule in kvp.Value)
				{
					AddMoleculeToQueue(kvp.Key, molecule);
				}
			}
		}

		private ComputationManagerSimple(
			Dictionary<IOIndex, List<Molecule>> initialMolecules,
			List<Dictionary<IOIndex, List<Molecule>>> rulebook,
			int seed)
		{
			this.rulebook = rulebook;
			this.random = new(seed);
			addMoleculesFromRule(initialMolecules);
		}

		public static ComputationManagerMaker makerConstructor(
			Dictionary<IOIndex, List<Molecule>> initialMolecules,
			List<Dictionary<IOIndex, List<Molecule>>> rulebook,
			int seed)
		{
			return (_) => new ComputationManagerSimple(initialMolecules, rulebook, seed);
		}
	}
}