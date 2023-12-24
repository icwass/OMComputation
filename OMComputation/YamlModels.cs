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

public class ComputationIOPacketModel
{
	public bool IsInput;
	public int ID;
	public List<string> Molecules;
}

public class ComputationProblemSetModel
{
	public List<ComputationIOPacketModel> Set;

	public Dictionary<API.IOIndex, List<Molecule>> FromModel(Dictionary<string, Molecule> molDict, string puzzleID)
	{
		Dictionary<API.IOIndex, List<Molecule>> actualSet = new();

		foreach (var packet in Set)
		{
			var index = new API.IOIndex(packet.IsInput, packet.ID);
			if (actualSet.ContainsKey(index)) throw new Exception("[OMComputation] Duplicate key " + index.ToString() + " encountered in computation.yaml file for '" + puzzleID + "'");

			List<Molecule> molList = new();
			foreach (var molRef in packet.Molecules)
			{
				if (!molDict.ContainsKey(molRef)) throw new Exception("[OMComputation] Undefined molecule reference '" + molRef + "' used in computation.yaml file for '" + puzzleID + "'");
				molList.Add(molDict[molRef]);
			}

			actualSet.Add(index, molList);
		}

		return actualSet;
	}
}

public class ComputationTestCaseModel
{
	public int Seed = 0;
	public ComputationProblemSetModel InitialSet;
	public List<ComputationProblemSetModel> SetPool;

	public Tuple<int, Dictionary<API.IOIndex, List<Molecule>>, List<Dictionary<API.IOIndex, List<Molecule>>>> FromModel(
		Dictionary<string, Molecule> molDict,
		string puzzleID
	)
	{
		Dictionary<API.IOIndex, List<Molecule>> actualInitialSet = new();
		if (InitialSet != null) InitialSet.FromModel(molDict, puzzleID);
		List<Dictionary<API.IOIndex, List<Molecule>>> actualSetPool = new();

		if (SetPool == null) throw new Exception("[OMComputation] Undefined SetPool in computation.yaml file for '" + puzzleID + "'");

		foreach (var problemSet in SetPool)
		{
			actualSetPool.Add(problemSet.FromModel(molDict, puzzleID));
		}

		return Tuple.Create(Seed, actualInitialSet, actualSetPool);
	}
}

public class ComputationPuzzleDefinitionModel
{
	public string PuzzleID;
	public Dictionary<string,PuzzleModel.MoleculeM> MoleculeDictionary;

	public List<ComputationTestCaseModel> TestCases;

	public void AddDefinitionFromModel(string filePath)
	{
		if (string.IsNullOrEmpty(PuzzleID))
		{
			Logger.Log("[OMComputation] '" + filePath + "' has an invalid puzzleID, ignoring.");
			return;
		}
		Dictionary<string, Molecule> molDict = new();
		foreach (var kvp in MoleculeDictionary)
		{
			molDict.Add(kvp.Key, kvp.Value.FromModel());
		}

		List<Tuple<int, Dictionary<API.IOIndex, List<Molecule>>, List<Dictionary<API.IOIndex, List<Molecule>>>>> actualTestCases = new();
		foreach (var testCase in TestCases)
		{
			actualTestCases.Add(testCase.FromModel(molDict, PuzzleID));
		}

		//debug - need to actually implement test cases
		var TestCase = actualTestCases.First();
		internalAPI.AddSimpleComputationPuzzleDefinition(PuzzleID, TestCase.Item2, TestCase.Item3, TestCase.Item1);
	}
}