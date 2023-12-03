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
using BondType = enum_126;
//using BondSite = class_222;
using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
using Texture = class_256;

public static class ComputationPart
{
	//
	public static void LoadPuzzleContent()
	{
		//


	}
	public static void LoadHooking()
	{
		//


	}
	public static void UnloadHooking()
	{
		//



	}



}



public static class oldComputationPart
{
	private static IDetour hook_SEB_method_1994, hook_SEB_method_1996, hook_SES_method_2131, hook_Sim_method_1843;
	//
	private static PartType ComputationInputPart, ComputationOutputPart;

	private const string ComputationPartTypeField = "OMComputation_ComputationPartType";

	private const bool debugReplaceInputWithComputation = true; ///////////////////////////////////////////////////////////////////////////////////////////////////////////
	private const bool debugReplaceOutputWithComputation = true; ///////////////////////////////////////////////////////////////////////////////////////////////////////////

	private static Texture ioHexBaseInput, ioHexBaseOutput;
	private static Texture ioHexShadowInput, ioHexShadowOutput;
	private static Texture ioHexRingInput, ioHexRingOutput;
	private static Texture ioHexRingGlossMaskInput, ioHexRingGlossMaskOutput;
	private static Texture ioHexRingGlossInput, ioHexRingGlossOutput;
	private static Texture ioBondInput, ioBondOutput;

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

		string path = "computation/textures/parts/";

		ioHexBaseInput = class_235.method_615(path + "input_base");
		ioHexBaseOutput = class_235.method_615(path + "output_base");

		ioHexShadowInput = class_238.field_1989.field_90.field_164; // bonder_shadow
		ioHexShadowOutput = class_238.field_1989.field_90.field_193; // output_shadow

		ioHexRingInput = class_235.method_615(path + "input_ring");
		ioHexRingOutput = class_235.method_615(path + "output_ring");

		ioHexRingGlossMaskInput = class_238.field_1989.field_90.field_182; // input_ring_gloss_mask
		ioHexRingGlossMaskOutput = class_238.field_1989.field_90.field_192; // output_ring_gloss_mask

		ioHexRingGlossInput = class_235.method_615(path + "input_gloss");
		ioHexRingGlossOutput = class_235.method_615(path + "output_gloss");

		ioBondInput = class_235.method_615(path + "input_bond");
		ioBondOutput = class_235.method_615(path + "output_bond");

		QApi.AddPartType(ComputationInputPart, (_, _, _, _) => { });/////////////////////////////////////////////////////////////////
		QApi.AddPartType(ComputationOutputPart, (_, _, _, _) => { });/////////////////////////////////////////////////////////////////
	}

	public static void LoadHooking()
	{
		hook_SEB_method_1994 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1994"), SEB_DrawPartMolecules);
		hook_SEB_method_1996 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1996"), SEB_DrawPartGlyph);
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



	private static class_195 getPartRenderer(SolutionEditorBase seb, Part part, Vector2 offset)
	{
		class_236 class236 = seb.method_1989(part, offset);
		return new class_195(class236.field_1984, class236.field_1985, Editor.method_922());
	}

	private static Sim getSimFromSeb(SolutionEditorBase seb)
	{
		if (seb is SolutionEditorScreen) return getSimFromSes((SolutionEditorScreen)seb);
		if (seb is class_194) return getSimFrom194((class_194)seb);

		Logger.Log("[OMComputation] getSimFromSeb: incompatible SolutionEditorBase encountered.");
		throw new Exception("Could not extract Sim from Seb");
	}
	private static Sim getSimFromSes(SolutionEditorScreen ses) => new DynamicData(ses).Get<Maybe<Sim>>("field_4022").method_1087();
	private static Sim getSimFrom194(class_194 class194) => class194.method_500();

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions


	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1994(SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState);
	private static void SEB_DrawPartMolecules(orig_SolutionEditorBase_method_1994 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState)
	{

		class_236 class236 = seb_self.method_1989(part, offset);

		if (PartIsComputationIO(part))
		{
			PartSimState partSimState = seb_self.method_507().method_481(part);
			Molecule molecule = part.method_1185(seb_self.method_502());////////////////////////////////////////////////////////////////////////////////////////////////////////
			//do we swap-out the puzzle's io molecules, then put them back?
			//or do we draw stuff manually?
			molecule = ComputationManager.GetMolecule(getSimFromSeb(seb_self), part);

			void method925(float x, float y, float z, bool flg) => Editor.method_925(molecule, class236.field_1984, new HexIndex(0, 0), class236.field_1985, x, y, z, flg, null);

			if (PartIsInput(part))
			{
				if (partSimState.field_2743)
				{
					method925(1f, seb_self.method_504(), 1f, false);
				}
				if (seb_self.method_503() == enum_128.Stopped && !viewPreviousBoardState)
				{
					method925(1f, 1f, 1f, false);
				}
			}
			else if (PartIsOutput(part))
			{
				if (partSimState.field_2731.method_1085() && seb_self.method_510() >= partSimState.field_2731.method_1087() + 0.25f)
				{
					partSimState.field_2731 = (Maybe<float>)struct_18.field_1431;
				}
				if (partSimState.field_2743)
				{
					method925(1f, class_162.method_416(seb_self.method_504(), 0f, 1f, 1f, 0f), 1f, false);
					method925(0.05f, 1f, 0f, true);
				}
				else if (partSimState.field_2731.method_1085())
				{
					float num = class_162.method_406(seb_self.method_510() - partSimState.field_2731.method_1087());
					method925(class_162.method_416(num, 0f, 0.25f, 0.05f, 0.4f), 1f, 0f, true);
				}
				else
				{
					method925(0.4f, 1f, 0f, true);
				}
			}
		}
		else
		{
			orig(seb_self, part, offset, flag, viewPreviousBoardState);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1996(SolutionEditorBase seb_self, Part part, Vector2 offset);
	private static void SEB_DrawPartGlyph(orig_SolutionEditorBase_method_1996 orig, SolutionEditorBase seb_self, Part part, Vector2 offset)
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

			method_2000(seb_self.method_1989(part, offset), Editor.method_922(), ComputationManager.GetProfile(seb_self.method_502(), part), PartIsInput(part));

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

	public static void method_2000(
	class_236 class236,
	Vector2 rendererOffset,
	Molecule footprint,
	bool isInput)
	{
		var atomHexes = footprint.method_1100().Keys;
		class_195 renderer = new class_195(class236.field_1984, class236.field_1985, rendererOffset);

		Texture ioHexBase = isInput ? ioHexBaseInput : ioHexBaseOutput;
		Texture ioHexShadow = isInput ? ioHexShadowInput : ioHexShadowOutput;
		Texture ioHexRing = isInput ? ioHexRingInput : ioHexRingOutput;
		Texture ioHexRingGlossMask = isInput ? ioHexRingGlossMaskInput : ioHexRingGlossMaskOutput;
		Texture ioHexRingGloss = isInput ? ioHexRingGlossInput : ioHexRingGlossOutput;
		Texture ioBond = isInput ? ioBondInput : ioBondOutput;

		foreach (HexIndex hex in atomHexes)
		{
			renderer.method_525(ioHexBase, new Vector2(-1f, -1f), hex, 0.0f);
		}
		foreach (HexIndex hex in atomHexes)
		{
			renderer.method_530(ioHexShadow, hex, 4f);
		}
		foreach (HexIndex hex in atomHexes)
		{
			renderer.method_528(ioHexRing, hex, new Vector2(0f, 0f));
			class_135.method_257().field_1692 = class_238.field_1995.field_1757; // MaskedGlossPS shader
			class_135.method_257().field_1693[1] = ioHexRingGloss;
			class_135.method_257().field_1694[1] = (enum_123)3;
			class_135.method_257().field_1695 = method_2001(renderer, hex);
			renderer.method_529(ioHexRingGlossMask, hex, new Vector2(0f, 0f));
			class_135.method_257().field_1692 = class_135.method_257().field_1696;
			class_135.method_257().field_1693[1] = class_238.field_1989.field_71; // single white pixel
			class_135.method_257().field_1694[1] = (enum_123)2;
		}
		foreach (Bond bond in (IEnumerable<Bond>)footprint.method_1101())
		{
			float angle = class_187.field_1742.method_492(bond.field_2188 - bond.field_2187).Angle();
			renderer.method_526(ioBond, bond.field_2187, new Vector2(0f, 0f), new Vector2(-23f, 20f), angle);
		}
	}

	private static Vector2 method_2001(class_195 renderer, HexIndex hex) => 0.0001f * (renderer.field_1797 + class_187.field_1742.method_492(hex).Rotated(renderer.field_1798) - 0.5f * class_115.field_1433);


	////////////////////////////////////////////////////////////
	private delegate HashSet<HexIndex> orig_SolutionEditorScreen_method_2131(Solution param_5731, Part param_5732);
	private static HashSet<HexIndex> SES_GetHexesForDrawingThePartHandle(orig_SolutionEditorScreen_method_2131 orig, Solution solution, Part part)
	{
		if (PartIsComputationIO(part))
		{
			return ComputationManager.GetFootprint(solution, part);
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
				bool isComputationInput = debugReplaceInputWithComputation; // debug//////////////////////////////////////////////////////////////////////////////////
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
				bool isComputationOutput = debugReplaceOutputWithComputation; // debug//////////////////////////////////////////////////////////////////////////////////
				if (isComputationOutput) partTypeForToolbar.field_2745 = ComputationOutputPart;
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
			foreach (HexIndex hexIndex in ComputationManager.GetFootprint(solution, part_self))
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
			MainClass.PrivateMethod<SolutionEditorBase>("method_2017").Invoke(seb_self, new object[] { class236, ComputationManager.GetFootprint(seb_self.method_502(), part), color });
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
