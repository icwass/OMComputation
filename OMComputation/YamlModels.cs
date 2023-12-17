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

public class ComputationProblemSet
{
	public List<ComputationIOPacketModel> Set;
}

public class ComputationPuzzleDefinitionModel
{
	public string PuzzleID;
	public int Seed = 0;
	public Dictionary<string,PuzzleModel.MoleculeM> MoleculeDictionary;

	public ComputationProblemSet InitialSet;
	public List<ComputationProblemSet> SetPool;

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
		Dictionary<API.IOIndex, List<Molecule>> actualInitialSet = new();
		List<Dictionary<API.IOIndex, List<Molecule>>> actualSetPool = new();
		foreach (var packet in InitialSet.Set)
		{
			var index = new API.IOIndex(packet.IsInput, packet.ID);
			if (actualInitialSet.ContainsKey(index)) throw new Exception("[OMComputation] The computation.yaml file for '" + PuzzleID + "' has duplicate keys in its initialSet: " + index.ToString());

			List<Molecule> molList = new();
			foreach (var molRef in packet.Molecules)
			{
				if (!molDict.ContainsKey(molRef)) throw new Exception("[OMComputation] The computation.yaml file for '" + PuzzleID + "' has an undefined molecule reference in its initialSet: " + molRef);
				molList.Add(molDict[molRef]);
			}

			actualInitialSet.Add(index, molList);
		}

		foreach (var problemSet in SetPool)
		{
			Dictionary<API.IOIndex, List<Molecule>> actualProblemSet = new();

			foreach (var packet in problemSet.Set)
			{
				var index = new API.IOIndex(packet.IsInput, packet.ID);
				if (actualProblemSet.ContainsKey(index)) throw new Exception("[OMComputation] The computation.yaml file for '" + PuzzleID + "' has duplicate keys in its setPool: " + index.ToString());

				List<Molecule> molList = new();
				foreach (var molRef in packet.Molecules)
				{
					if (!molDict.ContainsKey(molRef)) throw new Exception("[OMComputation] The computation.yaml file for '" + PuzzleID + "' has a set in setPool with an undefined molecule reference: " + molRef);
					molList.Add(molDict[molRef]);
				}

				actualProblemSet.Add(index, molList);
			}

			actualSetPool.Add(actualProblemSet);
		}

		internalAPI.AddSimpleComputationPuzzleDefinition(PuzzleID, actualInitialSet, actualSetPool, Seed);
	}
}