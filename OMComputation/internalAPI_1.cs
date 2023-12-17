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
	// public API functions
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

	////////////////////////////////////////////////////////////
	// helper classes
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