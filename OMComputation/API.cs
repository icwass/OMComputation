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

public static class API
{
	// public types and public data
	public delegate ComputationManagerBase ComputationManagerMaker(string puzzleID);
	public struct IOIndex
	{
		public readonly bool isInput;
		public readonly int ID;

		// constructors
		public IOIndex(bool isInput, int ID)
		{
			this.ID = ID;
			this.isInput = isInput;
		}
		public IOIndex(Part part) : this(vanillaAPI.PartIsInput(part), vanillaAPI.PartIONumber(part)) { }
		public static IOIndex Input(int ID) => new IOIndex(true, ID);
		public static IOIndex Output(int ID) => new IOIndex(false, ID);

		// helpers
		public override string ToString() => "(" + (this.isInput ? "input " : "output ") + this.ID + ")";
	}
	public struct IOGlyph
	{
		// defines the "shape" of a computation IO part
		// the shape is the same across test cases
		public readonly IOIndex ioIndex;
		public readonly HashSet<HexIndex> footprint;
		public readonly Molecule profile;

		// constructors
		public IOGlyph(IOIndex ioIndex, Molecule profile, HashSet<HexIndex> footprint)
		{
			this.ioIndex = ioIndex;
			this.footprint = footprint;
			this.profile = profile;
		}
		public IOGlyph(IOIndex ioIndex, Molecule profile) : this(ioIndex, profile, internalAPI.GetFootprintFromMolecule(profile)) { }
	}

	////////////////////////////////////////////////////////////
	public abstract class ComputationManagerBase
	{
		public string puzzleID = "";
		Dictionary<IOIndex, Queue<Molecule>> moleculeQueues = new();
		Dictionary<IOIndex, Molecule> previousMolecules = new();

		//////////
		// functions that need to be defined when deriving a ComputationManager subclass
		public abstract void AddMoleculesToQueues(IOIndex ioIndexThatNeedsMolecules);





		//////////
		// default functions
		private void ensureSafeDictionaryAccess(IOIndex ioIndex)
		{
			if (!moleculeQueues.ContainsKey(ioIndex)) moleculeQueues.Add(ioIndex, new Queue<Molecule>());
			if (!previousMolecules.ContainsKey(ioIndex)) previousMolecules.Add(ioIndex, null);
		}
		public void AddMoleculeToQueue(IOIndex ioIndex, Molecule molecule)
		{
			ensureSafeDictionaryAccess(ioIndex);
			moleculeQueues[ioIndex].Enqueue(molecule);
		}
		public void GoToNextMolecule(IOIndex ioIndex)
		{
			ensureSafeDictionaryAccess(ioIndex);
			previousMolecules[ioIndex] = CurrentMolecule(ioIndex);
			moleculeQueues[ioIndex].Dequeue();
		}
		public Molecule previousMolecule(IOIndex ioIndex)
		{
			ensureSafeDictionaryAccess(ioIndex);
			if (previousMolecules[ioIndex] == null) previousMolecules[ioIndex] = CurrentMolecule(ioIndex);
			return previousMolecules[ioIndex];
		}
		public Molecule CurrentMolecule(IOIndex ioIndex)
		{
			ensureSafeDictionaryAccess(ioIndex);
			if (moleculeQueues[ioIndex].Count == 0) AddMoleculesToQueues(ioIndex);
			if (moleculeQueues[ioIndex].Count > 0) return moleculeQueues[ioIndex].Peek();
			throw new Exception("[OMComputation] The ComputationManager for puzzle '" + puzzleID + "' failed to generate more molecules for ioIndex " + ioIndex.ToString() + ".");
		}
	}



	/// <summary>
	/// A function that generates a ComputationManager.
	/// </summary>
	// <param name="part">The part to be displayed.</param>
	// <param name="position">The position of the part.</param>
	// <param name="editor">The solution editor that the part is being displayed in.</param>
	// <param name="helper">An object containing functions for rendering images, at different positions/rotations and lightmaps.</param>////////////////////////////////////////////////////////////
	public static void AddComputationPuzzleDefinition(string puzzleID, List<IOGlyph> ioGlyphs, ComputationManagerMaker managerMaker)
	{
		internalAPI.AddComputationPuzzleDefinition(puzzleID, ioGlyphs, managerMaker);
	}


}