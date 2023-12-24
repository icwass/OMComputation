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
	private static IDetour hook_SES_method_2131, hook_Sim_method_1843, hook_Sim_method_1836, hook_Sim_method_1832, hook_SEB_method_1994, hook_SEB_method_1996;

	private static Texture ioHexBaseFootprint;
	private static Texture[] ioInput;
	private static Texture[] ioOutput;

	private static PartType internal_computationIO;

	public static bool enableComputationMode = true;

	//
	//
	// TO-DO (?): change "field_1530" display for inputs/outputs: class_134.method_253("A computation input for this alchemical machine.", string.Empty)
	//
	//

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions
	public static void LoadPuzzleContent()
	{
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

		internal_computationIO = new PartType();

		ioHexBaseFootprint = ioInput[0];
	}
	public static void LoadHooking()
	{
		hook_SES_method_2131 = new Hook(MainClass.PrivateMethod<SolutionEditorScreen>("method_2131"), SES_GetHexesForDrawingThePartHandle);

		hook_Sim_method_1843 = new Hook(MainClass.PrivateMethod<Sim>("method_1843"), Sim_SpawnComputationInputs);
		hook_Sim_method_1836 = new Hook(MainClass.PrivateMethod<Sim>("method_1836"), Sim_DrawInputSpawns);
		hook_Sim_method_1832 = new Hook(MainClass.PrivateMethod<Sim>("method_1832"), Sim_AcceptComputationOutputs);

		hook_SEB_method_1994 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1994"), SEB_DrawPartMolecules);
		hook_SEB_method_1996 = new Hook(MainClass.PrivateMethod<SolutionEditorBase>("method_1996"), SEB_DrawPartGlyph);

		On.Part.method_1187 += PartPlacementCollisions;

		On.SolutionEditorBase.method_1997 += PartGlowDrawing;
		On.SolutionEditorBase.method_1998 += PartStrokeDrawing;

		On.SolutionEditorPartsPanel.class_428.method_2047 += ConvertComputationIOInThePartsTray;
		On.SolutionEditorPartsPanel.method_221 += FixDrawingOfComputationIOInThePartsTray;
	}
	public static void UnloadHooking()
	{
		hook_SES_method_2131.Dispose();
		hook_Sim_method_1843.Dispose();
		hook_Sim_method_1836.Dispose();
		hook_Sim_method_1832.Dispose();
		hook_SEB_method_1994.Dispose();
		hook_SEB_method_1996.Dispose();
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// helper functions
	private static void computationMethod2000(class_236 class236, HashSet<HexIndex> footprint, Molecule profile, Vector2 rendererOffset, bool isInput)
	{
		// draws the glyph only, NOT the input/output molecule
		var atomHexes = profile.method_1100().Keys;

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

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// hooking functions

	////////////////////////////////////////////////////////////
	private delegate HashSet<HexIndex> orig_SolutionEditorScreen_method_2131(Solution param_5731, Part param_5732);
	private static HashSet<HexIndex> SES_GetHexesForDrawingThePartHandle(orig_SolutionEditorScreen_method_2131 orig, Solution solution, Part part)
	{
		return (enableComputationMode && internalAPI.PartIsComputationIO(solution, part)) ? internalAPI.GetComputationFootprint(solution, part) : orig(solution, part);
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_Sim_method_1843(Sim sim_self);
	private static void Sim_SpawnComputationInputs(orig_Sim_method_1843 orig, Sim sim_self)
	{
		Sim_InputsWrapper((sim) => orig(sim), sim_self, (sim, input, reagent) =>
		{
			var molecules = sim.field_3823;
			molecules.Add(reagent);
			internalAPI.AdvanceToNextComputationMolecule(sim, input);
		});
	}

	private delegate void orig_Sim_method_1836(Sim sim_self);
	private static void Sim_DrawInputSpawns(orig_Sim_method_1836 orig, Sim sim_self)
	{
		Sim_InputsWrapper((sim) => orig(sim), sim_self, (sim, input, _) =>
		{
			sim.field_3821[input].field_2743 = true;
		});
	}

	private static void Sim_InputsWrapper(Action<Sim> orig, Sim sim, Action<Sim, Part, Molecule> action)
	{
		if (!enableComputationMode)
		{
			orig(sim);
			return;
		}

		Dictionary<Part, PartType> computationInputs = new();
		var partList = vanillaAPI.getPartList(sim);

		foreach (var input in partList.Where(x => vanillaAPI.PartIsInput(x) && internalAPI.PartIsComputationIO(sim, x)))
		{
			computationInputs.Add(input, vanillaAPI.getPartType(input));
		}

		// get molecule-position data
		HashSet<HexIndex> hexIndexSet = new HashSet<HexIndex>();
		var molecules = sim.field_3823;
		foreach (Molecule molecule in molecules) hexIndexSet.UnionWith(molecule.method_1100().Keys);

		// don't let computation inputs behave the usual way
		foreach (Part part in computationInputs.Keys) vanillaAPI.overwriteField(part, "field_2691", internal_computationIO);

		orig(sim);

		// instead, do something different
		foreach (Part input in computationInputs.Keys)
		{
			vanillaAPI.overwriteField(input, "field_2691", computationInputs[input]);
			HexIndex shift = input.method_1161();
			HexRotation rotate = input.method_1163();
			Molecule reagent = internalAPI.GetComputationMolecule_Current(sim, input).method_1115(rotate).method_1117(shift);

			bool inputIsBlocked = (bool)MainClass.PrivateMethod<Sim>("method_1837").Invoke(sim, new object[] { reagent, hexIndexSet });
			bool inputIsPaused = internalAPI.IOIndexIsPaused(sim, input);

			if (!inputIsBlocked && !inputIsPaused) action(sim, input, reagent);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_Sim_method_1832(Sim sim_self, bool isConsumptionStep);
	private static void Sim_AcceptComputationOutputs(orig_Sim_method_1832 orig, Sim sim_self, bool isConsumptionStep)
	{
		if (!enableComputationMode)
		{
			orig(sim_self, isConsumptionStep);
			return;
		}

		PuzzleInputOutput[] puzzleOutputs = vanillaAPI.getPuzzle(sim_self).field_2771;
		var computationOutputs = vanillaAPI.getPartList(sim_self).Where(x => vanillaAPI.PartIsOutput(x) && internalAPI.PartIsComputationIO(sim_self, x));
		var pausedOutputs = new Dictionary<Part, PartType>();

		// record what the puzzle outputs are
		Molecule[] originalOutputs = new Molecule[puzzleOutputs.Length];
		for (int i = 0; i < puzzleOutputs.Length; i++)
		{
			originalOutputs[i] = puzzleOutputs[i].field_2813;
		}
		// switcheroo molecule outputs in the puzzle file
		foreach (var computationOutput in computationOutputs)
		{
			var product = internalAPI.GetComputationMolecule_Current(sim_self, computationOutput);
			puzzleOutputs[vanillaAPI.PartIONumber(computationOutput)].field_2813 = product;
			// temporarily disable computation outputs that are paused
			if (internalAPI.IOIndexIsPaused(sim_self, computationOutput))
			{
				pausedOutputs.Add(computationOutput, vanillaAPI.getPartType(computationOutput));
				vanillaAPI.setPartType(computationOutput, new PartType());
			}
		}

		orig(sim_self, isConsumptionStep);

		// restore the puzzle outputs
		for (int i = 0; i < puzzleOutputs.Length; i++)
		{
			puzzleOutputs[i].field_2813 = originalOutputs[i];
		}
		// update computation outputs that accepted a product
		foreach (var computationOutput in computationOutputs)
		{
			var partSimState = sim_self.field_3821[computationOutput];
			if (partSimState.field_2743) internalAPI.AdvanceToNextComputationMolecule(sim_self, computationOutput);
		}
		// restore the paused outputs
		foreach (var computationOutput in pausedOutputs.Keys)
		{
			vanillaAPI.setPartType(computationOutput, pausedOutputs[computationOutput]);
		}
	}

	////////////////////////////////////////////////////////////
	private delegate void orig_SolutionEditorBase_method_1994(SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState);
	private static void SEB_DrawPartMolecules(orig_SolutionEditorBase_method_1994 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, bool flag, bool viewPreviousBoardState)
	{
		if (!enableComputationMode || !internalAPI.PartIsComputationIO(seb_self, part))
		{
			orig(seb_self, part, offset, flag, viewPreviousBoardState);
			return;
		}

		PartSimState partSimState = seb_self.method_507().method_481(part);
		bool partWasActivated = partSimState.field_2743;
		Molecule molecule = internalAPI.GetComputationMolecule_Current(seb_self, part);
		Molecule prevMolecule = internalAPI.GetComputationMolecule_Previous(seb_self, part);

		class_236 class236 = seb_self.method_1989(part, offset);
		void method925(float x, float y, float z, bool flg, bool prev = false)
		{
			Editor.method_925(prev ? prevMolecule : molecule, class236.field_1984, new HexIndex(0, 0), class236.field_1985, x, y, z, flg, null);
		}

		if (vanillaAPI.PartIsInput(part))
		{
			bool drawDuringEditing = seb_self.method_503() == enum_128.Stopped && !viewPreviousBoardState;
			if (partWasActivated) method925(1f, seb_self.method_504(), 1f, false);
			if (drawDuringEditing) method925(1f, 1f, 1f, false);
		}
		else // isOutput
		{
			if (partSimState.field_2731.method_1085() && seb_self.method_510() >= partSimState.field_2731.method_1087() + 0.25f)
			{
				partSimState.field_2731 = (Maybe<float>)struct_18.field_1431;
			}

			if (partWasActivated)
			{
				method925(1f, class_162.method_416(seb_self.method_504(), 0f, 1f, 1f, 0f), 1f, false, true);
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
		if (!enableComputationMode || !internalAPI.PartIsComputationIO(seb_self, part))
		{
			orig(seb_self, part, offset);
			return;
		}

		var class236 = seb_self.method_1989(part, offset);
		var solution = seb_self.method_502();
		var footprint = internalAPI.GetComputationFootprint(solution, part);
		var profile = internalAPI.GetComputationProfile(solution, part);
		computationMethod2000(class236, footprint, profile, Editor.method_922(), vanillaAPI.PartIsInput(part));
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
		if (!enableComputationMode || !internalAPI.PartIsComputationIO(solution, part_self))
		{
			return orig(part_self, solution, enum137, shift, rotate);
		}
		else
		{
			HashSet<HexIndex> ret = new HashSet<HexIndex>();
			foreach (HexIndex hexIndex in internalAPI.GetComputationFootprint(solution, part_self))
			{
				ret.Add(hexIndex.Rotated(rotate) + shift);
			}
			return ret;
		}
	}

	////////////////////////////////////////////////////////////
	private static void PartGlowDrawing(On.SolutionEditorBase.orig_method_1997 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, float alpha)
	{
		if (!enableComputationMode || !internalAPI.PartIsComputationIO(seb_self, part))
		{
			orig(seb_self, part, offset, alpha);
		}
		else
		{
			if (alpha == 0f) return;
			Color color = Color.White.WithAlpha(alpha);
			class_236 class236 = seb_self.method_1989(part, offset);
			MainClass.PrivateMethod<SolutionEditorBase>("method_2017").Invoke(seb_self, new object[] { class236, internalAPI.GetComputationFootprint(seb_self, part), color });
		}
	}

	////////////////////////////////////////////////////////////
	private static void PartStrokeDrawing(On.SolutionEditorBase.orig_method_1998 orig, SolutionEditorBase seb_self, Part part, Vector2 offset, float alpha)
	{
		if (enableComputationMode && internalAPI.PartIsComputationIO(seb_self, part)) return;

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
		if (!enableComputationMode) return;

		var reagents = class_134.method_253("Reagents", string.Empty);
		var products = class_134.method_253("Products", string.Empty);
		if (trayName != reagents && trayName != products) return;

		var solutionEditorPartsPanel = class428_self.field_3972;
		var solutionEditorScreen = new DynamicData(solutionEditorPartsPanel).Get<SolutionEditorScreen>("field_2007");
		var solution = vanillaAPI.getSolution(solutionEditorScreen);

		// check if any inputs are computation io, and convert if needed
		foreach (var partTypeForToolbar in list)
		{
			var partType = partTypeForToolbar.field_2745;
			if (!vanillaAPI.PartTypeStandardIO(partType)) continue;

			bool isInput = partType.field_1541;
			int index = partTypeForToolbar.field_2746;
			var ioIndex = new API.IOIndex(isInput, index);
			bool isComputation = internalAPI.IOIndexIsComputationIO(solution, ioIndex);
			if (!isComputation) continue;

			//partTypeForToolbar.field_2745 = isInput ? ComputationInputPart : ComputationOutputPart;

			var footprint = internalAPI.GetComputationFootprint(solution, ioIndex);
			var profile = internalAPI.GetComputationProfile(solution, ioIndex);
			var molecule = internalAPI.GetComputationMolecule_Current(solutionEditorScreen, ioIndex);
			Vector2 vector2 = new Vector2(215f, 1000f);

			partTypeForToolbar.field_2750 = computationRenderHandle(footprint, profile, molecule, isInput, false, vector2);
			partTypeForToolbar.field_2751 = computationRenderHandle(footprint, profile, molecule, isInput, true, vector2);
		}
	}

	public static RenderTargetHandle computationRenderHandle(
	HashSet<HexIndex> footprint,
	Molecule profile,
	Molecule molecule,
	bool isInput,
	bool isHovered,
	Vector2 vector2)
	{
		RenderTargetHandle renderTargetHandle = isHovered ? molecule.field_2640 : molecule.field_2641;
		if (molecule.method_1102().method_1085()) molecule = molecule.method_1120();

		float fx187 = class_187.field_1742.field_1744.X;
		float fy187 = class_187.field_1742.field_1744.Y * 1.3f;
		Bounds2 bounds2 = Bounds2.Undefined;
		foreach (HexIndex hex in footprint)
		{
			Bounds2 bounds = Bounds2.CenteredOn(class_187.field_1742.method_491(hex, Vector2.Zero), fx187, fy187);
			bounds2 = bounds2.UnionedWith(bounds);
		}

		float num1 = footprint.Count <= 1 ? 1f : Math.Min(0.7f, Math.Min(vector2.X / bounds2.Width, vector2.Y / bounds2.Height));
		Index2 index2 = (bounds2.Size * num1).CeilingToInt() + new Index2(40, 40);
		Vector2 vector2_1 = index2.ToVector2() / 2 / num1 - bounds2.Center;
		renderTargetHandle.field_2987 = index2;
		class_95 class95 = renderTargetHandle.method_1352(out bool flag);
		if (flag)
		{
			using (class_226.method_597(class95, Matrix4.method_1075(num1)))
			{
				class_226.method_600(Color.Transparent);
				foreach (HexIndex key in footprint)
				{
					Vector2 vector2_2 = class_187.field_1742.method_491(key, Vector2.Zero);
					Texture ioHover = isHovered ? class_238.field_1989.field_90.field_245.field_307 : class_238.field_1989.field_90.field_245.field_308;
					Vector2 vector2_3 = ioHover.field_2056.ToVector2() / 2;
					Vector2 vector2_4 = vector2_2 - vector2_3 + vector2_1;
					class_135.method_272(ioHover, vector2_4.Rounded());
				}
				var class236 = new class_236() { field_1984 = vector2_1.Rounded() };

				computationMethod2000(class236, footprint, profile, new Vector2(0f, 999999f), isInput);
				Editor.method_925(molecule, vector2_1.Rounded(), new HexIndex(0, 0), 0f, 1f, 1f, 1f, false, null);
			}
		}
		return renderTargetHandle;
	}

	////////////////////////////////////////////////////////////
	private static void FixDrawingOfComputationIOInThePartsTray(On.SolutionEditorPartsPanel.orig_method_221 orig, SolutionEditorPartsPanel sepp_self, float f)
	{
		if (!enableComputationMode)
		{
			orig(sepp_self, f);
			return;
		}

		var solutionEditorScreen = new DynamicData(sepp_self).Get<SolutionEditorScreen>("field_2007");
		var solution = vanillaAPI.getSolution(solutionEditorScreen);
		var puzzle = vanillaAPI.getPuzzle(solution);
		PuzzleInputOutput[] puzzleInputs = puzzle.field_2770;
		PuzzleInputOutput[] puzzleOutputs = puzzle.field_2771;

		var computationInputs = new List<int>();
		var computationOutputs = new List<int>();

		for (int i = 0; i < puzzleInputs.Length; i++) if (internalAPI.IOIndexIsComputationIO(solution, API.IOIndex.Input(i))) computationInputs.Add(i);
		for (int i = 0; i < puzzleOutputs.Length; i++) if (internalAPI.IOIndexIsComputationIO(solution, API.IOIndex.Output(i))) computationOutputs.Add(i);

		// record what the puzzle inputs and outputs are
		Molecule[] originalInputs = new Molecule[puzzleInputs.Length];
		Molecule[] originalOutputs = new Molecule[puzzleOutputs.Length];
		for (int i = 0; i < puzzleInputs.Length; i++) originalInputs[i] = puzzleInputs[i].field_2813;
		for (int i = 0; i < puzzleOutputs.Length; i++) originalOutputs[i] = puzzleOutputs[i].field_2813;

		// switcheroo molecules in the puzzle file
		foreach (var n in computationInputs) puzzleInputs[n].field_2813 = footprintMolecule(internalAPI.GetComputationFootprint(solution, API.IOIndex.Input(n)));
		foreach (var n in computationOutputs) puzzleOutputs[n].field_2813 = footprintMolecule(internalAPI.GetComputationFootprint(solution, API.IOIndex.Output(n)));

		orig(sepp_self, f);

		// restore the puzzle outputs
		for (int i = 0; i < puzzleInputs.Length; i++) puzzleInputs[i].field_2813 = originalInputs[i];
		for (int i = 0; i < puzzleOutputs.Length; i++) puzzleOutputs[i].field_2813 = originalOutputs[i];
	}

	private static Molecule footprintMolecule(HashSet<HexIndex> footprint)
	{
		var ret = new Molecule();
		foreach (var hex in footprint) ret.method_1105(new Atom(class_175.field_1675), hex);
		return ret;
	}
}
