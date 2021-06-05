﻿using System;
using System.Collections.Generic;
using System.Text;
using EngineNS;

namespace EngineNS.EGui.UIEditor.Elements
{
    public class DataBinder
    {
        public enum EBinderMode
        {
            TwoWay,
            Read,
            Write,
        }
        public class PropNameEditor : Controls.PropertyGrid.PGCustomValueEditorAttribute
        {
            public override unsafe void OnDraw(System.Reflection.PropertyInfo prop, object target, object value, Controls.PropertyGrid.PropertyGrid pg, List<KeyValuePair<object, System.Reflection.PropertyInfo>> callstack)
            {
                var sz = new Vector2(0, 0);
                //var v = value as string;
                //var strPtr = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(v);
                //var pBuffer = stackalloc sbyte[256];
                //CoreSDK.StrCpy(pBuffer, strPtr.ToPointer());
                //if(ImGuiAPI.Button("..", ref sz))
                //{

                //}
                //ImGuiAPI.SameLine(0, -1);
                //ImGuiAPI.SetNextItemWidth(-1);
                //ImGuiAPI.InputTextNoName(prop.Name, pBuffer, (uint)256, ImGuiInputTextFlags_.ImGuiInputTextFlags_None, null, (void*)0);
                //if (CoreSDK.StrCmp(pBuffer, strPtr.ToPointer()) != 0)
                //{
                //    v = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)pBuffer);
                //    foreach (var j in pg.TargetObjects)
                //    {
                //        Controls.PropertyGrid.PropertyGrid.SetValue(j, callstack, prop, target, v);
                //    }
                //}
                //System.Runtime.InteropServices.Marshal.FreeHGlobal(strPtr);

                var bindType = UIEditor.EditableFormData.Instance.CurrentForm.BindType;
                if (bindType == null)
                    return;
                var props = bindType.SystemType.GetProperties();
                ImGuiAPI.SetNextItemWidth(-1);
                if (ImGuiAPI.BeginCombo(TName.FromString2("##PropName_", prop.Name).ToString(), value?.ToString(), ImGuiComboFlags_.ImGuiComboFlags_None))
                {
                    foreach(var i in props)
                    {
                        if (ImGuiAPI.Selectable(i.Name, true, ImGuiSelectableFlags_.ImGuiSelectableFlags_None, ref sz))
                        {
                            var v = i.Name;
                            foreach (var j in pg.TargetObjects)
                            {
                                Controls.PropertyGrid.PropertyGrid.SetValue(pg, j, callstack, prop, target, v);
                            }
                        }
                    }
                    ImGuiAPI.EndCombo();
                }
            }
        }
        [PropNameEditor()]
        public string PropName
        {
            get;
            set;
        }
        public EBinderMode Mode
        {
            get;
            set;
        } = EBinderMode.TwoWay;

        public System.Type GenPreCode(Form form, System.Type expectType, out string varName)
        {
            varName = null;
            if (form.BindType == null)
                return null;

            var prop = GetPropType(form);
            if (prop.CanRead == false)
                return prop.PropertyType;
            varName = $"_bind_prop_{PropName}";
            form.AddLine($"{expectType.FullName} {varName};");
            var propVarName = $"{Form.BindTargetName}?.{ PropName}";
            var assignCode = ConvertCode(expectType, prop.PropertyType, propVarName);
            switch (Mode)
            {
                case EBinderMode.TwoWay:
                    form.AddLine($"{varName} = {assignCode};");
                    break;
                case EBinderMode.Read:
                    form.AddLine($"{varName} = {assignCode};");
                    break;
                case EBinderMode.Write:
                    break;
            }
            return prop.PropertyType;
        }
        public void GenPostCode(Form form, System.Type expectType, System.Type realType, string varName)
        {
            if (realType == null)
                return;

            var prop = GetPropType(form);
            if (prop.CanWrite == false)
                return;

            var propVarName = $"{Form.BindTargetName}?.{ PropName}";
            var assignCode = ConvertCode(realType, expectType, varName);
            form.AddLine($"var newValue = {assignCode};");
            form.AddLine($"if({Form.BindTargetName} != null && {Form.BindTargetName}.{PropName} != newValue)");
            form.PushBrackets();
            switch (Mode)
            {
                case EBinderMode.TwoWay:
                    form.AddLine($"{Form.BindTargetName}.{PropName} = newValue;");
                    break;
                case EBinderMode.Read:
                    form.AddLine($"{Form.BindTargetName}.{PropName} = newValue;");
                    break;
                case EBinderMode.Write:
                    break;
            }
            form.PopBrackets();
        }
        private System.Reflection.PropertyInfo GetPropType(Form form)
        {
            return form.BindType.SystemType.GetProperty(PropName);
        }
        private string ConvertCode(System.Type expectType, System.Type realType, string varCode)
        {
            if (expectType == realType)
            {
                return varCode;
            }
            else if (expectType == typeof(sbyte))
            {
                return $"System.Convert.ToSByte({varCode})";
            }
            else if (expectType == typeof(Int16))
            {
                return $"System.Convert.ToInt16({varCode})";
            }
            else if (expectType == typeof(Int32))
            {
                return $"System.Convert.ToInt32({varCode})";
            }
            else if (expectType == typeof(Int64))
            {
                return $"System.Convert.ToInt64({varCode})";
            }
            else if (expectType == typeof(byte))
            {
                return $"System.Convert.ToByte({varCode})";
            }
            else if (expectType == typeof(UInt16))
            {
                return $"System.Convert.ToInt16({varCode})";
            }
            else if (expectType == typeof(UInt32))
            {
                return $"System.Convert.ToUInt32({varCode})";
            }
            else if (expectType == typeof(UInt64))
            {
                return $"System.Convert.ToUInt64({varCode})";
            }
            else if (expectType == typeof(float))
            {
                return $"System.Convert.ToSingle({varCode})";
            }
            else if (expectType == typeof(double))
            {
                return $"System.Convert.ToDouble({varCode})";
            }
            else if (expectType == typeof(string))
            {
                return $"System.Convert.ToString({varCode})";
            }
            else if (expectType.IsEnum && realType==typeof(string))
            {
                var enumTypeName = expectType.FullName.Replace('+', '.');
                return $"({enumTypeName})EngineNS.Support.TConvert.ToEnumValue(typeof({enumTypeName}), {varCode})";
            }
            else
            {
                throw new Exception(EngineNS.Support.DebugHelper.TraceMessage($"Convert Error"));
            }
        }
    }
}