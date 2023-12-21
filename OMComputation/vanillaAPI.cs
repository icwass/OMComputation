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
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;

public static class vanillaAPI
{
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions - helpers
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public static void overwriteField(object obj, string field, object value) => new DynamicData(obj).Set(field, value);

	// access tree: main trunk
	public static SolutionEditorBase getSEB(Sim sim) => sim.field_3818;
	public static Solution getSolution(SolutionEditorBase seb) => seb.method_502();
	public static List<Part> getPartList(Solution solution) => solution.field_3919;
	public static Puzzle getPuzzle(Solution solution) => solution.method_1934();
	public static string getPuzzleID(Puzzle puzzle) => puzzle.field_2766;

	// access tree: shortcuts
	public static Solution getSolution(Sim sim) => getSolution(getSEB(sim));
	public static List<Part> getPartList(Sim sim) => getPartList(getSolution(getSEB(sim)));
	public static Puzzle getPuzzle(Sim sim) => getPuzzle(getSolution(getSEB(sim)));
	public static string getPuzzleID(Sim sim) => getPuzzleID(getPuzzle(getSolution(getSEB(sim))));

	//public static Puzzle getPuzzle(SolutionEditorBase seb) => getPuzzle(getSolution(seb));
	public static string getPuzzleID(SolutionEditorBase seb) => getPuzzleID(getPuzzle(getSolution(seb)));

	public static string getPuzzleID(Solution solution) => getPuzzleID(getPuzzle(solution));




	public static PartType getPartType(Part part) => part.method_1159();
	public static void setPartType(Part part, PartType partType) => new DynamicData(part).Set("field_2691", partType);
	public static bool PartTypeIsInput(PartType partType) => partType.field_1541;
	public static bool PartTypeIsOutput(PartType partType) => partType.field_1553; // note: this only cares about STANDARD outputs - we don't computationalize polymer-outputs!
	public static bool PartTypeStandardIO(PartType partType) => PartTypeIsInput(partType) || PartTypeIsOutput(partType);
	public static int PartIONumber(Part part) => part.method_1167();
	public static bool PartIsInput(Part part) => PartTypeIsInput(getPartType(part));
	public static bool PartIsOutput(Part part) => PartTypeIsOutput(getPartType(part));
	public static bool PartIsStandardIO(Part part) => PartTypeStandardIO(getPartType(part));
}