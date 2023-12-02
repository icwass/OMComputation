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

using PartType = class_139;
using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public static class ComputationPart
{
	private static IDetour hook_SEB_method_1994, hook_SEB_method_1996, hook_SES_method_2131, hook_Sim_method_1843;
	//
	private static PartType ComputationInputPart, ComputationOutputPart;

	private const string ComputationPartTypeField = "OMComputation_ComputationPartType";

	private static Molecule debugMolecule; ///////////////////////////////////////////////////////////////////////////////////////////////////////////
	private static HashSet<HexIndex> debugFootprint; ///////////////////////////////////////////////////////////////////////////////////////////////////////////

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions

	public static void LoadPuzzleContent()
	{
		//
		ComputationInputPart = new PartType()
		{
			/*ID*/field_1528 = "input-comp",
			/*Name*/field_1529 = class_134.method_253("Reagent", string.Empty),
			/*Desc*/field_1530 = class_134.method_253("A computation input for this alchemical machine.", string.Empty),
			/*Is a Glyph?*/field_1539 = true,//default=false
			/*(?)Is Input?*/field_1541 = true,//default=false
			/*Permissions*/field_1551 = Permissions.None,
		};
		ComputationOutputPart = new PartType()
		{
			/*ID*/field_1528 = "out-comp",
			/*Name*/field_1529 = class_134.method_253("Product", string.Empty),
			/*Desc*/field_1530 = class_134.method_253("A computation output of this alchemical machine.", string.Empty),
			/*Is a Glyph?*/field_1539 = true,//default=false
			/*Is Standard Output?*/field_1553 = true,//default=false
			/*Permissions*/field_1551 = Permissions.None
		};
		new DynamicData(ComputationInputPart).Set(ComputationPartTypeField, ComputationInputPart);
		new DynamicData(ComputationOutputPart).Set(ComputationPartTypeField, ComputationInputPart);

		//string path = "textures/";

		QApi.AddPartType(ComputationInputPart, (_, _, _, _) => { });/////////////////////////////////////////////////////////////////
		QApi.AddPartType(ComputationOutputPart, (_, _, _, _) => { });/////////////////////////////////////////////////////////////////

		debugMolecule = Molecule.method_1122(class_175.field_1678, class_175.field_1675);/////////////////////////////////////////////////////////////////
		debugFootprint = new()
		{
			new HexIndex(0,0),
			new HexIndex(1,0),
			new HexIndex(0,1)
		};
	}

	public static void LoadHooking()
	{
		hook_SEB_method_1994 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1994"), SEB_DrawFirstInput);
		hook_SEB_method_1996 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1996"), SEB_DrawMainPart);
		hook_SES_method_2131 = new Hook(MainClass.PrivateMethod<SolutionEditorScreen>("method_2131"), SES_GetHexesForDrawingThePartHandle);
		hook_Sim_method_1843 = new Hook(MainClass.PrivateMethod<Sim>("method_1843"), Sim_SpawnComputationInputs);

		On.SolutionEditorPartsPanel.class_428.method_2047 += ConvertComputationIOInThePartsTray;

		On.Part.method_1187 += PartPlacementCollisions;

		//On.Solution.method_1959 += SolutionWriteExtension;
		//On.Solution.method_1960 += SolutionReadExtension;

		On.SolutionEditorBase.method_1997 += PartGlowDrawing;
		//On.SolutionEditorScreen.method_50 += EditorChangeMonomerExtension;

		// NEED A HOOK TO DRAW COMPUTATION IO IN THE PARTS TRAY ////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
	public static void UnloadHooking()
	{
		hook_SEB_method_1994.Dispose();
		hook_SEB_method_1996.Dispose();
		hook_SES_method_2131.Dispose();
		hook_Sim_method_1843.Dispose();
	}


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// helper functions
	
	private static bool PartIsComputationIO(Part part) => new DynamicData(part.method_1159()).Get<PartType>(ComputationPartTypeField) != null;
	private static bool PartIsInput(Part part) => part.method_1159().field_1541;
	private static bool PartIsOutput(Part part) => part.method_1159().field_1553;

	private static HashSet<HexIndex> GetComputationPartFootprint(Part part) => debugFootprint;

	private static class_195 getPartRenderer(SolutionEditorBase seb, Part part, Vector2 offset)
	{
		class_236 class236 = seb.method_1989(part, offset);
		return new class_195(class236.field_1984, class236.field_1985, Editor.method_922());
	}


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions


	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1994(SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState);
	private static void SEB_DrawFirstInput(orig_SolutionEditorBase_method_1994 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState)
	{
		if (PartIsInput(part) && PartIsComputationIO(part))
		{
			//fetch the first molecule for this computation input
			var firstMolecule = debugMolecule;

			//then draw it
			class_236 class236 = seb_self.method_1989(part, offset);
			if (seb_self.method_503() == enum_128.Stopped && !viewPreviousBoardState)
			{
				Editor.method_925(firstMolecule, class236.field_1984, new HexIndex(0, 0), class236.field_1985, 1f, 1f, 1f, false, null);
			}
		}
		else
		{
			orig(seb_self, part, offset, flag, viewPreviousBoardState);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1996(SolutionEditorBase seb_self, Part part, Vector2 offset);
	private static void SEB_DrawMainPart(orig_SolutionEditorBase_method_1996 orig, SolutionEditorBase seb_self, Part part, Vector2 offset)
	{
		if (PartIsComputationIO(part))
		{
			if (PartIsInput(part))
			{
				//


			}
			if (PartIsOutput(part))
			{
				//


			}

			/*
			class_195 renderer = getPartRenderer(seb_self, part, offset);////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			var molecule = part.method_1185(seb_self.method_502());
			foreach (HexIndex key in molecule.method_1100().Keys)
			{
				Texture part_base = class_238.field_1989.field_90.field_176;
				renderer.method_525(part_base, new Vector2(-1f, -1f), key, 0.0f);
				Texture atom_ring = class_238.field_1989.field_90.field_181;
				renderer.method_528(atom_ring, key, new Vector2(0.0f, 0.0f));
			}
			foreach (Bond class277 in (IEnumerable<Bond>)molecule.method_1101())
			{
				//Texture part_bond = class_238.field_1989.field_90.field_177;
				//float num = class_187.field_1742.method_492(class277.field_2188 - class277.field_2187).Angle();
				//renderer.method_526(part_bond, class277.field_2187, new Vector2(0.0f, 0.0f), new Vector2(-23f, 20f), num);
			}
			*/

		}
		else 
		{
			orig(seb_self, part, offset);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate HashSet<HexIndex> orig_SolutionEditorScreen_method_2131(Solution param_5731, Part param_5732);
	private static HashSet<HexIndex> SES_GetHexesForDrawingThePartHandle(orig_SolutionEditorScreen_method_2131 orig, Solution solution, Part part)
	{
		if (PartIsComputationIO(part))
		{
			return GetComputationPartFootprint(part);
		}
		else
		{
			return orig(solution, part);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_Sim_method_1843(Sim sim_self);
	private static void Sim_SpawnComputationInputs(orig_Sim_method_1843 orig, Sim sim_self)////////////////////////////////////////////////////////////////////////////////////
	{
		// inputs spawn molecules if there is room on the board for it
		// for now, do nothing

		ComputationInputPart.field_1541 = false;
		orig(sim_self);
		ComputationInputPart.field_1541 = true;
	}



	////////////////////////////////////////////////////////////
	private static void ConvertComputationIOInThePartsTray(
		On.SolutionEditorPartsPanel.class_428.orig_method_2047 orig,
		SolutionEditorPartsPanel.class_428 class428_self,
		string trayName,
		List<PartTypeForToolbar> list)
	{
		var solutionEditorPartsPanel = class428_self.field_3972;
		var solutionEditorScreen = new DynamicData(solutionEditorPartsPanel).Get<SolutionEditorScreen>("field_2007");
		var solution = solutionEditorScreen.method_502();
		var puzzle = solution.method_1934();
		var puzzleInputs = puzzle.field_2770;
		var puzzleOutputs = puzzle.field_2771;

		if (trayName == class_134.method_253("Reagents", string.Empty)) ////////////////////////////////////////////////////////////////////////////////////////////////////
		{
			// check if any inputs are computation io, and convert if needed
			
			foreach (var partTypeForToolbar in list)
			{
				int index = partTypeForToolbar.field_2746;
				//var molecule = puzzleInputs[index].field_2813;
				bool isComputationInput = false; // debug
				if (isComputationInput) partTypeForToolbar.field_2745 = ComputationInputPart;
			}
		}
		else if (trayName == class_134.method_253("Products", string.Empty))
		{
			// check if any outputs are computation io, and convert if needed

			foreach (var partTypeForToolbar in list)
			{
				int index = partTypeForToolbar.field_2746;
				//var molecule = puzzleInputs[index].field_2813;
				bool isComputationOutput = false; // debug
				if (isComputationOutput) partTypeForToolbar.field_2745 = ComputationInputPart;
			}
		}

		orig(class428_self, trayName, list);
	}

	////////////////////////////////////////////////////////////
	private static HashSet<HexIndex> PartPlacementCollisions(
		On.Part.orig_method_1187 orig,
		Part part_self,
		Solution solution,
		enum_137 enum137,
		HexIndex shift,
		HexRotation rotate)
	{
		if (PartIsComputationIO(part_self))
		{
			HashSet<HexIndex> ret = new HashSet<HexIndex>();
			foreach (HexIndex hexIndex in GetComputationPartFootprint(part_self))
			{
				ret.Add(hexIndex.Rotated(rotate) + shift);
			}
			return ret;
		}
		else
		{
			return orig(part_self, solution, enum137, shift, rotate);
		}
	}

	////////////////////////////////////////////////////////////
	private static void PartGlowDrawing(On.SolutionEditorBase.orig_method_1997 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, float alpha)
	{
		if (PartIsComputationIO(part))
		{
			if (alpha == 0f) return;
			Color color = Color.White.WithAlpha(alpha);
			class_236 class236 = seb_self.method_1989(part, offset);
			MainClass.PrivateMethod<SolutionEditorBase>("method_2017").Invoke(seb_self, new object[] { class236, GetComputationPartFootprint(part), color });
		}
		else
		{
			orig(seb_self, part, offset, alpha);
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







}
