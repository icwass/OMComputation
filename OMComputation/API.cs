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
using System.Reflection;

namespace OMComputation;

using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;

public static class API
{// public types and public data

	public delegate ComputationManagerBase ComputationManagerMaker(string puzzleID);
	public struct IOIndex
	{
		public readonly bool isInput;
		public readonly int ID;

		// constructors
		public IOIndex(bool isInput, int ID)
		{
			this.ID = ID;
			this.isInput = isInput;
		}
		public IOIndex(Part part) : this(PartIsInput(part), PartIONumber(part)) { }
		public static IOIndex Input(int ID) => new IOIndex(true, ID);
		public static IOIndex Output(int ID) => new IOIndex(false, ID);

		// helpers
		public override string ToString() => "(" + (this.isInput ? "input " : "output ") + this.ID + ")";
	}
	public struct IOGlyph // defines the shape of a computation IO part
	{
		public readonly IOIndex ioIndex;
		public readonly HashSet<HexIndex> footprint;
		public readonly Molecule profile;

		// constructors
		public IOGlyph(IOIndex ioIndex, Molecule profile, HashSet<HexIndex> footprint)
		{
			this.ioIndex = ioIndex;
			this.footprint = footprint;
			this.profile = profile;
		}
		public IOGlyph(IOIndex ioIndex, Molecule profile) : this(ioIndex, profile, GetFootprintFromMolecule(profile)) { }
	}



	/// <summary>
	/// A function that generates a ComputationManager.
	/// </summary>
	// <param name="part">The part to be displayed.</param>
	// <param name="position">The position of the part.</param>
	// <param name="editor">The solution editor that the part is being displayed in.</param>
	// <param name="helper">An object containing functions for rendering images, at different positions/rotations and lightmaps.</param>////////////////////////////////////////////////////////////

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - vanilla helpers
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public static void overwriteField(object obj, string field, object value) => new DynamicData(obj).Set(field, value);

	// access tree: main trunk
	public static SolutionEditorBase getSEB(Sim sim) => sim.field_3818;
	public static Solution getSolution(SolutionEditorBase seb) => seb.method_502();
	public static List<Part> getPartList(Solution solution) => solution.field_3919;
	public static Puzzle getPuzzle(Solution solution) => solution.method_1934();
	public static string getPuzzleID(Puzzle puzzle) => puzzle.field_2766;

	// access tree: shortcuts
	public static Solution getSolution(Sim sim) => getSolution(getSEB(sim));
	public static List<Part> getPartList(Sim sim) => getPartList(getSolution(getSEB(sim)));
	public static Puzzle getPuzzle(Sim sim) => getPuzzle(getSolution(getSEB(sim)));
	public static string getPuzzleID(Sim sim) => getPuzzleID(getPuzzle(getSolution(getSEB(sim))));

	//public static Puzzle getPuzzle(SolutionEditorBase seb) => getPuzzle(getSolution(seb));
	public static string getPuzzleID(SolutionEditorBase seb) => getPuzzleID(getPuzzle(getSolution(seb)));

	public static string getPuzzleID(Solution solution) => getPuzzleID(getPuzzle(solution));






	public static PartType getPartType(Part part) => part.method_1159();
	public static bool PartTypeIsInput(PartType partType) => partType.field_1541;
	public static bool PartTypeIsOutput(PartType partType) => partType.field_1553; // note: this only cares about STANDARD outputs - we don't computationalize polymer-outputs!
	public static bool PartTypeStandardIO(PartType partType) => PartTypeIsInput(partType) || PartTypeIsOutput(partType);
	public static int PartIONumber(Part part) => part.method_1167();
	public static bool PartIsInput(Part part) => PartTypeIsInput(getPartType(part));
	public static bool PartIsOutput(Part part) => PartTypeIsOutput(getPartType(part));
	public static bool PartIsStandardIO(Part part) => PartTypeStandardIO(getPartType(part));


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - computation helpers
	public static bool PartIsComputationIO(Sim sim, Part part) => PartIsComputationIO(getSolution(sim), part);
	public static bool PartIsComputationIO(SolutionEditorBase seb, Part part) => PartIsComputationIO(getSolution(seb), part);
	public static bool PartIsComputationIO(Solution solution, Part part) => PartIsStandardIO(part) && IOIndexIsComputationIO(solution, new IOIndex(part));
	public static bool IOIndexIsComputationIO(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = getPuzzleID(solution);
		if (PuzzleIsComputation(puzzleID)) return fetchComputationPuzzleDefinition(puzzleID).IOIndexIsComputationIO(ioIndex);
		return false;
	}

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


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - computation-data getters
	public static HashSet<HexIndex> GetComputationFootprint(SolutionEditorBase seb, Part part) => GetComputationFootprint(getSolution(seb), new IOIndex(part));
	public static HashSet<HexIndex> GetComputationFootprint(Solution solution, Part part) => GetComputationFootprint(solution, new IOIndex(part));
	public static HashSet<HexIndex> GetComputationFootprint(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = getPuzzleID(solution);
		throwErrorOnInvalidPuzzleID(puzzleID);
		return fetchComputationPuzzleDefinition(puzzleID).GetFootprint(ioIndex);
	}

	////////////////////////////////////////////////////////////
	public static Molecule GetComputationProfile(Solution solution, Part part) => GetComputationProfile(solution, new IOIndex(part));
	public static Molecule GetComputationProfile(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = getPuzzleID(solution);
		throwErrorOnInvalidPuzzleID(puzzleID);
		return fetchComputationPuzzleDefinition(puzzleID).GetProfile(ioIndex);
	}

	////////////////////////////////////////////////////////////
	public static Molecule GetComputationMolecule_Current(Sim sim, Part part) => GetComputationMolecule_Current(sim, new IOIndex(part));
	public static Molecule GetComputationMolecule_Current(Sim sim, IOIndex ioIndex) => fetchManagerFromSim(sim).CurrentMolecule(ioIndex);
	public static Molecule GetComputationMolecule_Current(SolutionEditorBase seb, Part part) => GetComputationMolecule_Current(seb, new IOIndex(part));
	public static Molecule GetComputationMolecule_Current(SolutionEditorBase seb, IOIndex ioIndex)
	{
		Sim sim = getSimFromSeb(seb);
		if (sim != null) return GetComputationMolecule_Current(sim, ioIndex);

		var compDefinition = fetchComputationPuzzleDefinition(getPuzzleID(seb));
		return compDefinition.createComputationManager().CurrentMolecule(ioIndex);
	}

	////////////////////////////////////////////////////////////
	public static Molecule GetComputationMolecule_Previous(SolutionEditorBase seb, Part part)
	{
		Sim sim = getSimFromSeb(seb);
		return sim == null ? GetComputationMolecule_Current(seb, part) : GetComputationMolecule_Previous(sim, part);
	}
	public static Molecule GetComputationMolecule_Previous(Sim sim, Part part)
	{
		var ioIndex = new IOIndex(PartIsInput(part), PartIONumber(part));
		return fetchManagerFromSim(sim).previousMolecule(ioIndex);
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions with side effects
	public static void AdvanceToNextComputationMolecule(Sim sim, Part part) => AdvanceToNextComputationMolecule(sim, new IOIndex(part));
	public static void AdvanceToNextComputationMolecule(Sim sim, IOIndex ioIndex) => fetchManagerFromSim(sim).GoToNextMolecule(ioIndex);


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - the big boys
	public static void AddComputationPuzzleDefinition(string puzzleID, List<IOGlyph> ioGlyphs, ComputationManagerMaker managerMaker)
	{
		if (!PuzzleIsComputation(puzzleID))
		{
			computationPuzzleDefinitions.Add(puzzleID, new ComputationPuzzleDefinition(puzzleID, ioGlyphs, managerMaker));
			Logger.Log("[OMComputation] Added ComputationPuzzleDefinition for '" + puzzleID + "'.");
			return;
		}
		else
		{
			Logger.Log("[OMComputation] AddComputationPuzzleDefinition: There is already a ComputationPuzzleDefinition for '" + puzzleID + "', ignoring.");
		}
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
		var puzzleID = getPuzzleID(sim);
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

	////////////////////////////////////////////////////////////
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