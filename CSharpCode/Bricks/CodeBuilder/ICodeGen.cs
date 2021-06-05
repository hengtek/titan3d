﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Bricks.CodeBuilder
{
    public abstract class ICodeGen
    {
        private int NumOfTab = 0;
        public int GetTabNum()
        {
            return NumOfTab;
        }
        public void PushTab()
        {
            NumOfTab++;
        }
        public void PopTab()
        {
            NumOfTab--;
        }
        public void PushBrackets()
        {
            AddLine("{");
            NumOfTab++;
        }
        public void PopBrackets(bool semicolon = false)
        {
            NumOfTab--;
            if (semicolon)
                AddLine("};");
            else
                AddLine("}");
        }
        public string ClassCode = "";
        public enum ELineMode
        {
            TabKeep,
            TabPush,
            TabPop,
        }
        public string AddLine(string code, ELineMode mode = ELineMode.TabKeep)
        {
            string result = "";
            for (int i = 0; i < NumOfTab; i++)
            {
                result += '\t';
            }
            result += code + '\n';

            ClassCode += result;
            return result;
        }
        public string AppendCode(string code, bool bTab, bool bNewLine)
        {
            string result = "";
            if (bTab)
            {
                for (int i = 0; i < NumOfTab; i++)
                {
                    result += '\t';
                }
            }
            result += code;
            if(bNewLine)
            {
                result += "\n";
            }

            ClassCode += result;
            return result;
        }

        public abstract IGen GetGen(Type exprType);

        public void BuildClassCode(DefineClass kls)
        {
            var klsGen = GetGen(typeof(DefineClass));

            klsGen.GenLines(kls, this);
        }
        public virtual string GetDefaultValue(System.Type t)
        {
            if (t == typeof(sbyte) ||
                t == typeof(Int16) ||
                t == typeof(Int32) ||
                t == typeof(Int64) ||
                t == typeof(byte) ||
                t == typeof(UInt16) ||
                t == typeof(UInt32) ||
                t == typeof(UInt64) ||
                t == typeof(float) ||
                t == typeof(double))
            {
                return "0";
            }
            else if (t == typeof(string))
            {
                return "";
            }
            else if (t.IsValueType)
            {
                return $"new {t.FullName}()";
            }
            else
            {
                return $"null";
            }
        }
        public virtual string GetTypeString(System.Type t)
        {
            return t.FullName;
        }

        public static bool CanConvert(Type left, Type right)
        {
            if (left == null || right == null)
                return false;
            else if (left == right)
            {
                return true;
            }
            else if (IsNumeric(left) && IsNumeric(right))
            {
                return true;
            }
            else if (left.IsSubclassOf(right))
            {
                return true;
            }
            return false;
        }
        public static bool IsNumeric(Type t)
        {
            if (t == typeof(sbyte) ||
                t == typeof(Int16) ||
                t == typeof(Int32) ||
                t == typeof(Int64) ||
                t == typeof(byte) ||
                t == typeof(UInt16) ||
                t == typeof(UInt32) ||
                t == typeof(UInt64) ||
                t == typeof(float) ||
                t == typeof(double))
            {
                return true;
            }
            return false;
        }
    }
}
