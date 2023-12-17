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

		var compDefinition = fetchComputationPuzzleDefinition(vanillaAPI.getPuzzleID(seb));
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
}