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
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class ComputationAPI
{
	//



	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public API functions

	public static void AddRuleBook()
	{
		//


		Logger.Log("[OMComputation] AddRuleBook: feature not yet implemented");
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// public functions


	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// private functions

	private static void LoadRulebooks()
	{
		// call this during PostLoad()

		// read rulebooks from .computation.yaml files



		Logger.Log("[OMComputation] LoadRulebooks: method not yet implemented");
	}

}
