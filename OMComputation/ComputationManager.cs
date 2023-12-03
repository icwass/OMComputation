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
using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class ComputationManager
{
	//


	private static Molecule debugMoleculeA, debugMoleculeB; ///////////////////////////////////////////////////////////////////////////////////////////////////////////



	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions
	public static void LoadPuzzleContent()
	{
		//


		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		debugMoleculeA = Molecule.method_1122(class_175.field_1678, class_175.field_1675);/////////////////////////////////////////////////////////////////
		debugMoleculeB = Molecule.method_1122(class_175.field_1675, class_175.field_1678);/////////////////////////////////////////////////////////////////
	}

	public static bool IOPartIsComputationIO(Solution solution, int index, bool isInput)
	{
		// use the Solution to search the ComputationManager dictionary to find the desired info
		var puzzleID = solution.method_1934().field_2766;

		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		return puzzleID == "P030b" && index == 0;  // Life Sensing Potion
	}




	public static HashSet<HexIndex> GetFootprint(Solution solution, Part part)
	{
		// use the Solution to search the ComputationManager dictionary to find the desired info
		// use the Part to get the Footprint
		var puzzleID = solution.method_1934().field_2766;
		bool isInput = PartIsInput(part);
		int ioIndex = part.method_1167();

		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		return new HashSet<HexIndex>
		{
			new HexIndex(0,0),
			new HexIndex(1,0),
			new HexIndex(0,1),
			new HexIndex(1,-1),
		};
	}

	public static Molecule GetProfile(Solution solution, Part part)
	{
		// use the Solution to search the ComputationManager dictionary to find the desired info
		// use the Part to get the Profile
		var puzzleID = solution.method_1934().field_2766;
		bool isInput = PartIsInput(part);
		int ioIndex = part.method_1167();

		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		var ret = debugMoleculeA.method_1104();
		ret.method_1105(new Atom(class_175.field_1675), new HexIndex(0, 1));
		ret.method_1111((BondType)1, new HexIndex(0, 0), new HexIndex(0, 1));
		return ret;
	}

	public static Molecule GetMolecule(Sim sim, Part part) // => GetMolecule(sim, part.method_1167(), PartIsInput(part));
	{
		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		if (sim == null) return GetMolecule(sim, part.method_1167(), PartIsInput(part));
		var partSimState = sim.field_3821[part];
		var moleculeCounter = partSimState.field_2730;
		return moleculeCounter % 2 == 1 ^ PartIsInput(part) ? debugMoleculeA : debugMoleculeB;
	}
	public static Molecule GetMolecule(Sim sim, int ioIndex, bool isInput)
	{
		//ComputationManager manager;////////////////////////////////////////////////////////////////////////////
		if (sim == null)
		{
			// generate a throw-away manager
		}
		else
		{
			// use the Sim to get the Manager (generate it necessary)
		}

		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		return isInput ? debugMoleculeA : debugMoleculeB;
	}


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// helper functions
	private static bool PartIsInput(Part part) => part.method_1159().field_1541;

	private static ComputationManager GetComputationManager(Sim sim)
	{
		// use the Sim to get the Computation Manager (with state)
		// if the Manager doesn't exist yet, then generate it





		//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// NOT IMPLEMENTED YET
		return new ComputationManager();
	}



	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions


	////////////////////////////////////////////////////////////






	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







}
