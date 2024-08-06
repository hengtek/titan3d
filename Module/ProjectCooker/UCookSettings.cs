﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCooker
{

    class UCookSettings
    {
    }

    class UCookCommand
    {
        public const string Param_Types = "AssetType=";
        public const string Type_Texture = "Texture";
        public const string Type_Mesh = "Mesh";
        public const string Type_MeshPrimitive = "MeshPrimitive";
        public const string Type_Material = "Material";
        public const string Type_MaterialInst = "MaterialInst";
        public const string Type_Scene = "Scene";
        public const string Type_Prefab = "Prefab";
        public const string Type_AnimClip = "AnimClip";
        public const string Type_UI = "UI";
        public const string Type_Macross = "Macross";

        public const string DS_Port = "DS_Port=";

        public virtual async System.Threading.Tasks.Task ExecuteCommand(string[] args)
        {
            await EngineNS.Thread.TtAsyncDummyClass.DummyFunc();
        }
        public static string FindArgument(string[] args, string startWith)
        {
            foreach (var i in args)
            {
                if (i.StartsWith(startWith))
                {
                    return i.Substring(startWith.Length);
                }
            }
            return null;
        }
        public static string[] GetArguments(string[] args, string startWith, char split='+')
        {
            var types = FindArgument(args, startWith);
            if (types != null)
            {
                return types.Split(split);
            }
            return null;
        }
    }
}
