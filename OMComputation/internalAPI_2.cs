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
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - computation-data getters
	public static int GetNumberOfTestCases(string puzzleID) => 1;

	////////////////////////////////////////////////////////////
	public static HashSet<HexIndex> GetComputationFootprint(SolutionEditorBase seb, Part part) => GetComputationFootprint(vanillaAPI.getSolution(seb), new IOIndex(part));
	public static HashSet<HexIndex> GetComputationFootprint(Solution solution, Part part) => GetComputationFootprint(solution, new IOIndex(part));
	public static HashSet<HexIndex> GetComputationFootprint(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = vanillaAPI.getPuzzleID(solution);
		throwErrorOnInvalidPuzzleID(puzzleID);
		return fetchComputationPuzzleDefinition(puzzleID).GetFootprint(ioIndex);
	}
	////////////////////////////////////////////////////////////
	public static Molecule GetComputationProfile(Solution solution, Part part) => GetComputationProfile(solution, new IOIndex(part));
	public static Molecule GetComputationProfile(Solution solution, IOIndex ioIndex)
	{
		var puzzleID = vanillaAPI.getPuzzleID(solution);
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

		var compDefinition = fetchComputationPuzzleDefinition(seb);
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
		var ioIndex = new IOIndex(vanillaAPI.PartIsInput(part), vanillaAPI.PartIONumber(part));
		return fetchManagerFromSim(sim).previousMolecule(ioIndex);
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions with side effects
	public static void AdvanceToNextComputationMolecule(Sim sim, Part part) => AdvanceToNextComputationMolecule(sim, new IOIndex(part));
	public static void AdvanceToNextComputationMolecule(Sim sim, IOIndex ioIndex) => fetchManagerFromSim(sim).GoToNextMolecule(ioIndex);

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
	public static bool IOIndexIsPaused(Sim sim, Part part) => fetchManagerFromSim(sim).IOIndexIsPaused(new IOIndex(part));
	public static bool IOIndexIsPaused(SolutionEditorBase seb, Part part)
	{
		Sim sim = getSimFromSeb(seb);
		if (sim != null) return IOIndexIsPaused(sim, part);

		var compDefinition = fetchComputationPuzzleDefinition(seb);
		return compDefinition.createComputationManager().IOIndexIsPaused(new IOIndex(part));
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

	private static ComputationPuzzleDefinition fetchComputationPuzzleDefinition(SolutionEditorBase seb) => fetchComputationPuzzleDefinition(vanillaAPI.getPuzzleID(seb));
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
			int testCase = 0;// testcases are not implemented yet //////////////////////////////////////////////////////////////////////////////////////////////
			var manager = this.managerMaker(this.puzzleID, testCase);
			manager.puzzleID = this.puzzleID;
			return manager;
		}

		private Exception invalidIOIndex(IOIndex ioIndex) => new Exception("[OMComputation] Invalid ioIndex " + ioIndex.ToString() + " for puzzle '" + puzzleID + "'.");
	}
}