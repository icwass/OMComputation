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
using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
using Texture = class_256;

public static class ComputationPart
{
	private static IDetour hook_SES_method_2131, hook_SEB_method_1994, hook_SEB_method_1996;


	private static PartType ComputationInputPart, ComputationOutputPart;

	private static Texture ioHexBaseFootprint;
	private static Texture[] ioInput;
	private static Texture[] ioOutput;




	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions

	public static void LoadPuzzleContent()
	{
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

		string path = "computation/textures/parts/";

		ioInput = new Texture[6]
		{
			class_235.method_615(path + "input_base"),
			class_238.field_1989.field_90.field_164, // bonder_shadow
			class_235.method_615(path + "input_ring"),
			class_238.field_1989.field_90.field_182, // input_ring_gloss_mask
			class_235.method_615(path + "input_gloss"),
			class_235.method_615(path + "input_bond"),
		};

		ioOutput = new Texture[6]
		{
			class_235.method_615(path + "output_base"),
			class_238.field_1989.field_90.field_193, // output_shadow
			class_235.method_615(path + "output_ring"),
			class_238.field_1989.field_90.field_192, // output_ring_gloss_mask
			class_235.method_615(path + "output_gloss"),
			class_235.method_615(path + "output_bond"),
		};

		ioHexBaseFootprint = ioInput[0];
		
		QApi.AddPartType(ComputationInputPart);
		QApi.AddPartType(ComputationOutputPart);
	}
	public static void LoadHooking()
	{
		hook_SES_method_2131 = new Hook(MainClass.PrivateMethod<SolutionEditorScreen>("method_2131"), SES_GetHexesForDrawingThePartHandle);

		hook_SEB_method_1994 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1994"), SEB_DrawPartMolecules);
		hook_SEB_method_1996 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1996"), SEB_DrawPartGlyph);

		On.Part.method_1187 += PartPlacementCollisions;

		On.SolutionEditorBase.method_1997 += PartGlowDrawing;
		On.SolutionEditorBase.method_1998 += PartStrokeDrawing;

		On.SolutionEditorPartsPanel.class_428.method_2047 += ConvertComputationIOInThePartsTray;


	}
	public static void UnloadHooking()
	{
		hook_SES_method_2131.Dispose();
		hook_SEB_method_1994.Dispose();
		hook_SEB_method_1996.Dispose();
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// helper functions
	private static bool PartIsComputationIO(Part part) => part.method_1159() == ComputationInputPart || part.method_1159() == ComputationOutputPart;


	private static Sim getSimFromSeb(SolutionEditorBase seb)
	{
		if (seb is SolutionEditorScreen) return getSimFromSes((SolutionEditorScreen)seb);
		if (seb is class_194) return getSimFrom194((class_194)seb);

		Logger.Log("[OMComputation] getSimFromSeb: incompatible SolutionEditorBase encountered.");
		throw new Exception("Could not extract Sim from Seb");
	}
	private static Sim getSimFromSes(SolutionEditorScreen ses)
	{
		var maybeSim = new DynamicData(ses).Get<Maybe<Sim>>("field_4022");
		return maybeSim.method_1085() ? maybeSim.method_1087() : null;
	}
	private static Sim getSimFrom194(class_194 class194) => class194.method_500();

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions

	////////////////////////////////////////////////////////////
	private delegate HashSet<HexIndex> orig_SolutionEditorScreen_method_2131(Solution param_5731, Part param_5732);
	private static HashSet<HexIndex> SES_GetHexesForDrawingThePartHandle(orig_SolutionEditorScreen_method_2131 orig, Solution solution, Part part)
	{
		return PartIsComputationIO(part) ? ComputationManager.GetFootprint(solution, part) : orig(solution, part);
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1994(SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState);
	private static void SEB_DrawPartMolecules(orig_SolutionEditorBase_method_1994 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState)
	{
		if (!PartIsComputationIO(part))
		{
			orig(seb_self, part, offset, flag, viewPreviousBoardState);
			return;
		}

		PartSimState partSimState = seb_self.method_507().method_481(part);
		Molecule molecule = ComputationManager.GetMolecule(getSimFromSeb(seb_self), part);

		class_236 class236 = seb_self.method_1989(part, offset);
		void method925(float x, float y, float z, bool flg) => Editor.method_925(molecule, class236.field_1984, new HexIndex(0, 0), class236.field_1985, x, y, z, flg, null);

		bool isInput = part.method_1159().field_1541;

		if (isInput)
		{
			bool drawDuringEditing = seb_self.method_503() == enum_128.Stopped && !viewPreviousBoardState;
			if (partSimState.field_2743) method925(1f, seb_self.method_504(), 1f, false);
			if (drawDuringEditing) method925(1f, 1f, 1f, false);
		}
		else // isOutput
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

	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1996(SolutionEditorBase seb_self, Part part, Vector2 offset);
	private static void SEB_DrawPartGlyph(orig_SolutionEditorBase_method_1996 orig, SolutionEditorBase seb_self, Part part, Vector2 offset)
	{
		if (!PartIsComputationIO(part))
		{
			orig(seb_self, part, offset);
			return;
		}

		bool isInput = part.method_1159().field_1541;
		var solution = seb_self.method_502();
		var footprint = ComputationManager.GetFootprint(solution, part);
		var profile = ComputationManager.GetProfile(solution, part);
		var atomHexes = profile.method_1100().Keys;

		// based on method_2000
		class_236 class236 = seb_self.method_1989(part, offset);
		Vector2 rendererOffset = Editor.method_922();
		class_195 renderer = new class_195(class236.field_1984, class236.field_1985, rendererOffset);

		Texture[] ioTextures = isInput ? ioInput : ioOutput;

		Texture ioHexBase = ioTextures[0];
		Texture ioHexShadow = ioTextures[1];
		Texture ioHexRing = ioTextures[2];
		Texture ioHexRingGlossMask = ioTextures[3];
		Texture ioHexRingGloss = ioTextures[4];
		Texture ioBond = ioTextures[5];

		foreach (HexIndex hex in footprint)
		{
			renderer.method_525(atomHexes.Contains(hex) ? ioHexBase : ioHexBaseFootprint, new Vector2(-1f, -1f), hex, 0.0f);
		}
		foreach (HexIndex hex in atomHexes)
		{
			renderer.method_530(ioHexShadow, hex, 4f);
		}
		foreach (HexIndex hex in atomHexes)
		{
			var method2001_result = 0.0001f * (renderer.field_1797 + class_187.field_1742.method_492(hex).Rotated(renderer.field_1798) - 0.5f * class_115.field_1433);

			renderer.method_528(ioHexRing, hex, new Vector2(0f, 0f));
			class_135.method_257().field_1692 = class_238.field_1995.field_1757; // MaskedGlossPS shader
			class_135.method_257().field_1693[1] = ioHexRingGloss;
			class_135.method_257().field_1694[1] = (enum_123)3;
			class_135.method_257().field_1695 = method2001_result;
			renderer.method_529(ioHexRingGlossMask, hex, new Vector2(0f, 0f));
			class_135.method_257().field_1692 = class_135.method_257().field_1696;
			class_135.method_257().field_1693[1] = class_238.field_1989.field_71; // single white pixel
			class_135.method_257().field_1694[1] = (enum_123)2;
		}
		foreach (Bond bond in (IEnumerable<Bond>)profile.method_1101())
		{
			float angle = class_187.field_1742.method_492(bond.field_2188 - bond.field_2187).Angle();
			renderer.method_526(ioBond, bond.field_2187, new Vector2(0f, 0f), new Vector2(-23f, 20f), angle);
		}
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

	////////////////////////////////////////////////////////////
	private static void PartStrokeDrawing(On.SolutionEditorBase.orig_method_1998 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, float alpha)
	{
		if (PartIsComputationIO(part)) return;

		orig(seb_self, part, offset, alpha);
	}

	////////////////////////////////////////////////////////////
	private static void ConvertComputationIOInThePartsTray(
		On.SolutionEditorPartsPanel.class_428.orig_method_2047 orig,
		SolutionEditorPartsPanel.class_428 class428_self,
		string trayName,
		List<PartTypeForToolbar> list)
	{
		orig(class428_self, trayName, list);

		var reagents = class_134.method_253("Reagents", string.Empty);
		var products = class_134.method_253("Products", string.Empty);
		if (trayName != reagents && trayName != products) return;


		var solutionEditorPartsPanel = class428_self.field_3972;
		var solutionEditorScreen = new DynamicData(solutionEditorPartsPanel).Get<SolutionEditorScreen>("field_2007");
		var solution = solutionEditorScreen.method_502();

		// check if any inputs are computation io, and convert if needed
		foreach (var partTypeForToolbar in list)
		{
			var partType = partTypeForToolbar.field_2745;
			bool isInput = partType.field_1541;
			bool isOutput = partType.field_1553; // only standard outputs!
			if (!isInput && !isOutput) continue;

			int index = partTypeForToolbar.field_2746;
			bool isComputation = ComputationManager.IOPartIsComputationIO(solution, index, isInput);
			if (!isComputation) continue;

			partTypeForToolbar.field_2745 = isInput ? ComputationInputPart : ComputationOutputPart;
			Vector2 vector2 = new Vector2(215f, 1000f - 200f);

			var molecule = ComputationManager.GetMolecule(getSimFromSes(solutionEditorScreen), index, isInput);

			//debug/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

			partTypeForToolbar.field_2750 = Editor.method_928(molecule, isInput, false, vector2, false, struct_18.field_1431);
			partTypeForToolbar.field_2751 = Editor.method_928(molecule, isInput, true, vector2, false, struct_18.field_1431);
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	

























}



public static class oldComputationPart
{
	private static IDetour hook_Sim_method_1843;
	//
	private static PartType ComputationInputPart;

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions

	public static void LoadHooking()
	{
		hook_Sim_method_1843 = new Hook(MainClass.PrivateMethod<Sim>("method_1843"), Sim_SpawnComputationInputs);

		//On.Solution.method_1959 += SolutionWriteExtension;
		//On.Solution.method_1960 += SolutionReadExtension;

		//On.SolutionEditorScreen.method_50 += EditorChangeMonomerExtension;
	}
	public static void UnloadHooking()
	{
		hook_Sim_method_1843.Dispose();
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions

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

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







}
