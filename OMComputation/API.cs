//using Mono.Cecil.Cil;
//using MonoMod.Cil;
//using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
//using Quintessential.Serialization;
//using Quintessential.Settings;
//using SDL2;
using System;
//using System.IO;
using System.Linq;
using System.Collections.Generic;
//using System.Reflection;

namespace OMComputation;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public static class API
{
	// public types and public data

	public delegate ComputationManagerBase ComputationManagerMaker(string puzzleID);
	public struct IOIndex
	{
		public readonly bool isInput;
		public readonly int ID;

		public IOIndex(bool isInput, int ID)
		{
			this.ID = ID;
			this.isInput = isInput;
		}
		public static IOIndex Input(int ID) => new IOIndex(true, ID);
		public static IOIndex Output(int ID) => new IOIndex(false, ID);
		public override string ToString()
		{
			return "(" + (this.isInput ? "input " : "output ") + this.ID + ")";
		}


	}
	public struct IOGlyph // defines the shape of a computation IO part
	{
		public readonly IOIndex ioIndex;
		public readonly HashSet<HexIndex> footprint;
		public readonly Molecule profile;

		public IOGlyph(IOIndex ioIndex, Molecule profile, HashSet<HexIndex> footprint)
		{
			this.ioIndex = ioIndex;
			this.footprint = footprint;
			this.profile = profile;
		}

		public IOGlyph(IOIndex ioIndex, Molecule profile)
		{
			this.ioIndex = ioIndex;
			this.profile = profile;
			this.footprint = GetFootprintFromMolecule(profile);
		}
	}

	







	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions

	/// <summary>
	/// A function that generates a ComputationManager.
	/// </summary>
	// <param name="part">The part to be displayed.</param>
	// <param name="position">The position of the part.</param>
	// <param name="editor">The solution editor that the part is being displayed in.</param>
	// <param name="helper">An object containing functions for rendering images, at different positions/rotations and lightmaps.</param>


	public static HashSet<HexIndex> GetFootprintFromMolecule(Molecule molecule) => GetFootprintFromMolecules(new List<Molecule>() { molecule });
	public static HashSet<HexIndex> GetFootprintFromMolecules(List<Molecule> molecules)
	{
		var ret = new HashSet<HexIndex>();
		foreach (var molecule in molecules) ret.UnionWith(molecule.method_1100().Keys);
		return ret;
	}
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

	public static void AddComputationPuzzleDefinition(string puzzleID, List<IOGlyph> ioGlyphs, ComputationManagerMaker managerMaker)
	{
		if (!computationPuzzleDefinitions.ContainsKey(puzzleID))
		{
			computationPuzzleDefinitions.Add(puzzleID, new ComputationPuzzleDefinition(puzzleID, ioGlyphs, managerMaker));
			Logger.Log("[OMComputation] Added ComputationPuzzleDefinition for '" + puzzleID + "'.");
			return;
		}

		Logger.Log("[OMComputation] AddComputationPuzzleDefinition: There is already a ComputationPuzzleDefinition for '" + puzzleID + "', ignoring.");
	}

	public static void AddSimpleComputationPuzzleDefinition(
		string puzzleID,
		Dictionary<IOIndex, List<Molecule>> initialMolecules,
		List<Dictionary<IOIndex, List<Molecule>>> rulebook,
		int seed = 0)
	{
		Dictionary<IOIndex, List<Molecule>> moleculeDict = new();
		foreach (var key in rulebook.First().Keys)
		{
			moleculeDict.Add(key, new List<Molecule>());
		}
		foreach (var kvp in initialMolecules)
		{
			moleculeDict[kvp.Key].AddRange(kvp.Value);
		}
		foreach (Dictionary<IOIndex, List<Molecule>> rule in rulebook)
		{
			foreach (var kvp in rule)
			{
				moleculeDict[kvp.Key].AddRange(rule[kvp.Key]);
			}
		}

		List<IOGlyph> ioGlyphs = new();
		foreach (var kvp in moleculeDict)
		{
			ioGlyphs.Add(new IOGlyph(kvp.Key, GetProfileFromMolecules(kvp.Value)));
		}

		AddComputationPuzzleDefinition(puzzleID, ioGlyphs, ComputationManagerSimple.makerConstructor(initialMolecules, rulebook, seed));
	}

	public static void AddSimpleComputationPuzzleDefinition(
		string puzzleID,
		List<Dictionary<IOIndex, List<Molecule>>> rulebook,
		int seed = 0)
	{
		AddSimpleComputationPuzzleDefinition(puzzleID, new Dictionary<IOIndex, List<Molecule>>(), rulebook, seed);
	}



























	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// "read-only" public functions
	public static bool PartIsInput(Part part) => part.method_1159().field_1541;
	public static bool PartIsOutput(Part part) => part.method_1159().field_1553;

	public static bool IOPartIsComputationIO(Solution solution, int index, bool isInput)
	{
		var puzzleID = solution.method_1934().field_2766;
		if (computationPuzzleDefinitions.ContainsKey(puzzleID)) return computationPuzzleDefinitions[puzzleID].IOPartIsComputationIO(new IOIndex(isInput, index));
		return false;
	}

	public static HashSet<HexIndex> GetFootprint(Solution solution, Part part) => GetFootprint(solution, part.method_1167(), PartIsInput(part));
	public static HashSet<HexIndex> GetFootprint(Solution solution, int index, bool isInput)
	{
		var puzzleID = solution.method_1934().field_2766;
		if (computationPuzzleDefinitions.ContainsKey(puzzleID)) return computationPuzzleDefinitions[puzzleID].GetFootprint(new IOIndex(isInput, index));
		throw new Exception(invalidPuzzleID(puzzleID));
	}

	public static Molecule GetProfile(Solution solution, Part part) => GetProfile(solution, part.method_1167(), PartIsInput(part));
	public static Molecule GetProfile(Solution solution, int index, bool isInput)
	{
		var puzzleID = solution.method_1934().field_2766;
		if (computationPuzzleDefinitions.ContainsKey(puzzleID)) return computationPuzzleDefinitions[puzzleID].GetProfile(new IOIndex(isInput, index));
		throw new Exception(invalidPuzzleID(puzzleID));
	}

	////////////////////////////////////////////////////////////
	public static Molecule GetCurrentMolecule(SolutionEditorBase seb, Part part) => GetCurrentMolecule(seb, part.method_1167(), PartIsInput(part));
	public static Molecule GetCurrentMolecule(SolutionEditorBase seb, int index, bool isInput)
	{
		Sim sim = getSimFromSeb(seb);
		if (sim != null) return GetCurrentMolecule(sim, index, isInput);

		var puzzleID = seb.method_502().method_1934().field_2766;
		var compDefinition = computationPuzzleDefinitions[puzzleID];
		var manager = compDefinition.createComputationManager();
		return manager.CurrentMolecule(new IOIndex(isInput, index));
	}
	public static Molecule GetCurrentMolecule(Sim sim, Part part) => GetCurrentMolecule(sim, part.method_1167(), PartIsInput(part));
	public static Molecule GetCurrentMolecule(Sim sim, int index, bool isInput)
	{
		var ioIndex = new IOIndex(isInput, index);
		var manager = fetchManagerFromSim(sim);
		return manager.CurrentMolecule(ioIndex);
	}
	public static Molecule GetPreviousMolecule(SolutionEditorBase seb, Part part)
	{
		Sim sim = getSimFromSeb(seb);
		return sim == null ? GetCurrentMolecule(seb, part) : GetPreviousMolecule(sim, part);
	}
	public static Molecule GetPreviousMolecule(Sim sim, Part part)
	{
		var ioIndex = new IOIndex(PartIsInput(part), part.method_1167());
		var manager = fetchManagerFromSim(sim);
		return manager.previousMolecule(ioIndex);
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions with side effects
	public static void NextMolecule(Sim sim, Part part) => NextMolecule(sim, part.method_1167(), PartIsInput(part));
	public static void NextMolecule(Sim sim, int index, bool isInput)
	{
		var ioIndex = new IOIndex(isInput, index);
		var manager = fetchManagerFromSim(sim);
		manager.GoToNextMolecule(ioIndex);
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public helper functions
	public static Sim getSimFromSeb(SolutionEditorBase seb)
	{
		if (seb is SolutionEditorScreen) return getSimFromSes((SolutionEditorScreen)seb);
		if (seb is class_194) return getSimFrom194((class_194)seb);

		Logger.Log("[OMComputation] getSimFromSeb: incompatible SolutionEditorBase encountered.");
		throw new Exception("Could not extract Sim from Seb.");
	}
	public static Sim getSimFromSes(SolutionEditorScreen ses)
	{
		var maybeSim = new DynamicData(ses).Get<Maybe<Sim>>("field_4022");
		return maybeSim.method_1085() ? maybeSim.method_1087() : null;
	}
	public static Sim getSimFrom194(class_194 class194) => class194.method_500();































	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// private data
	
	private static Dictionary<string, ComputationPuzzleDefinition> computationPuzzleDefinitions = new();
	const string Sim_ComputationManagerField = "OMComputation_Sim_ComputationManagerField";



	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// private functions

	private static string invalidPuzzleID(string puzzleID) => "[OMComputation] The ComputationPuzzleDefinition for the puzzle '" + puzzleID + "' is missing.";
	private static ComputationManagerBase fetchManagerFromSim(Sim sim)
	{
		if (sim == null) throw new Exception("[OMComputation] fetchManagerFromSim: cannot fetch a ComputationManager for a null Sim.");
		var puzzleID = sim.field_3818.method_502().method_1934().field_2766;
		if (!computationPuzzleDefinitions.ContainsKey(puzzleID)) throw new Exception(invalidPuzzleID(puzzleID));
		var sim_dyn = new DynamicData(sim);
		var manager = (ComputationManagerBase) sim_dyn.Get(Sim_ComputationManagerField);
		if (manager == null)
		{
			manager = computationPuzzleDefinitions[puzzleID].createComputationManager();
			sim_dyn.Set(Sim_ComputationManagerField, manager);
		}
		return manager;
	}

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
		public bool IOPartIsComputationIO(IOIndex ioIndex) => footprints.ContainsKey(ioIndex);
		public HashSet<HexIndex> GetFootprint(IOIndex ioIndex)
		{
			if (footprints.ContainsKey(ioIndex)) return footprints[ioIndex];
			throw new Exception(invalidIOIndexMsg(ioIndex));
		}
		public Molecule GetProfile(IOIndex ioIndex)
		{
			if (profiles.ContainsKey(ioIndex)) return profiles[ioIndex];
			throw new Exception(invalidIOIndexMsg(ioIndex));
		}

		// helpers
		public ComputationManagerBase createComputationManager()
		{
			var manager = this.managerMaker(this.puzzleID);
			manager.setPuzzleID(this.puzzleID);
			return manager;
		}
		private string invalidIOIndexMsg(IOIndex ioIndex) => "[OMComputation] Invalid ioIndex " + ioIndex.ToString() + " for puzzle '" + puzzleID + "'.";
}

	////////////////////////////////////////////////////////////
	public abstract class ComputationManagerBase
	{
		string puzzleID = "";
		Dictionary<IOIndex, Queue<Molecule>> moleculeQueues = new();
		Dictionary<IOIndex, Molecule> previousMolecules = new();

		// functions that need to be defined when deriving a ComputationManager subclass
		public abstract void AddMoleculesToQueues(IOIndex ioIndexThatNeedsMolecules);


		// default functions
		public void setPuzzleID(string puzzleID)
		{
			if (this.puzzleID == "") this.puzzleID = puzzleID;
		}
		private void ensureSafeDictionaryAccess(IOIndex ioIndex)
		{
			if (!moleculeQueues.ContainsKey(ioIndex)) moleculeQueues.Add(ioIndex, new Queue<Molecule>());
		}

		public void AddMoleculeToQueue(IOIndex ioIndex, Molecule molecule)
		{
			ensureSafeDictionaryAccess(ioIndex);
			if (!moleculeQueues.ContainsKey(ioIndex)) moleculeQueues.Add(ioIndex, new Queue<Molecule>());
			moleculeQueues[ioIndex].Enqueue(molecule);
		}
		public void GoToNextMolecule(IOIndex ioIndex)
		{
			if (!previousMolecules.ContainsKey(ioIndex)) previousMolecules.Add(ioIndex, null);
			ensureSafeDictionaryAccess(ioIndex);
			previousMolecules[ioIndex] = this.CurrentMolecule(ioIndex);
			moleculeQueues[ioIndex].Dequeue();
		}
		public Molecule previousMolecule(IOIndex ioIndex)
		{
			if (!previousMolecules.ContainsKey(ioIndex)) previousMolecules.Add(ioIndex, null);
			if (previousMolecules[ioIndex] == null) return CurrentMolecule(ioIndex);
			return previousMolecules[ioIndex];
		}
		public Molecule CurrentMolecule(IOIndex ioIndex)
		{
			ensureSafeDictionaryAccess(ioIndex);
			if (moleculeQueues[ioIndex].Count == 0) AddMoleculesToQueues(ioIndex);
			if (moleculeQueues[ioIndex].Count > 0) return moleculeQueues[ioIndex].Peek();
			throw new Exception("[OMComputation] The ComputationManager for puzzle '" + puzzleID + "' failed to generate more molecules for ioIndex " + ioIndex.ToString() + ".");
		}




	}

	private class ComputationManagerSimple : ComputationManagerBase
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
