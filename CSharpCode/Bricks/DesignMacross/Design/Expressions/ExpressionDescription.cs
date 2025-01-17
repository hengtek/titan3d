﻿using EngineNS.Bricks.CodeBuilder;
using EngineNS.DesignMacross.Base.Description;
using EngineNS.DesignMacross.Design.ConnectingLine;
using EngineNS.Rtti;
using System.Diagnostics;
using System.Reflection;

namespace EngineNS.DesignMacross.Design.Expressions
{
    [EGui.Controls.PropertyGrid.PGCategoryFilters(ExcludeFilters = new string[] { "Misc" })]
    public class TtExpressionDescription : IExpressionDescription
    {
        [Rtti.Meta]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Rtti.Meta]
        public virtual string Name { get; set; } = "ExpressionDescription";
        public IDescription Parent { get; set; }

        [Rtti.Meta]
        public List<TtExecutionInPinDescription> ExecutionInPins { get; set; } = new();
        [Rtti.Meta]
        public List<TtExecutionOutPinDescription> ExecutionOutPins { get; set; } = new();
        [Rtti.Meta]
        public List<TtDataInPinDescription> DataInPins { get; set; } = new();
        [Rtti.Meta]
        public List<TtDataOutPinDescription> DataOutPins { get; set; } = new();

        public virtual void Initailize()
        {

        }
        public virtual UExpressionBase BuildExpression(ref FExpressionBuildContext expressionBuildContext)
        {
            return null;
        }
        public void AddDtaInPin(TtDataInPinDescription pinDescription)
        {
            if(!DataInPins.Contains(pinDescription))
            {
                pinDescription.Parent = this;
                DataInPins.Add(pinDescription);
            }
        }
        public void AddDtaOutPin(TtDataOutPinDescription pinDescription)
        {
            if(!DataOutPins.Contains(pinDescription))
            {
                pinDescription.Parent = this;
                DataOutPins.Add(pinDescription);
            }
        }
        public void AddExecutionInPin(TtExecutionInPinDescription pinDescription)
        {
            if(!ExecutionInPins.Contains(pinDescription))
            {
                pinDescription.Parent = this;
                ExecutionInPins.Add(pinDescription);
            }
        }
        public void AddExecutionOutPin(TtExecutionOutPinDescription pinDescription)
        {
            if(!ExecutionOutPins.Contains(pinDescription))
            {
                pinDescription.Parent = this;
                ExecutionOutPins.Add(pinDescription);
            }
        }
        public bool TryGetDataPin(Guid pinId, out TtDataPinDescription pin)
        {
            foreach(var dataPin in DataInPins)
            {
                if(dataPin.Id == pinId)
                {
                    pin = dataPin;
                    return true;
                }
            }
            foreach (var dataPin in DataOutPins)
            {
                if (dataPin.Id == pinId)
                {
                    pin = dataPin;
                    return true;
                }
            }
            pin = null;
            return false;
        }
        public bool TryGetExecutePin(Guid pinId, out TtExecutionPinDescription pin)
        {
            foreach(var executionPin in ExecutionInPins)
            {
                if(executionPin.Id == pinId)
                {
                    pin = executionPin;
                    return true;
                }
            }
            foreach (var executionPin in ExecutionOutPins)
            {
                if (executionPin.Id == pinId)
                {
                    pin = executionPin;
                    return true;
                }
            }
            pin = null;
            return false;
        }
        public List<TtDataInPinDescription> GetDataInPins(UTypeDesc typeDesc)
        {
            var pins = new List<TtDataInPinDescription>();
            foreach(var pin in DataInPins)
            {
                if(pin.TypeDesc == typeDesc)
                {
                    pins.Add(pin);
                }
            }
            return pins;
        }
        public List<TtDataOutPinDescription> GetDataOutPins(UTypeDesc typeDesc)
        {
            var pins = new List<TtDataOutPinDescription>();
            foreach (var pin in DataOutPins)
            {
                if (pin.TypeDesc == typeDesc)
                {
                    pins.Add(pin);
                }
            }
            return pins;
        }
        public List<TtExecutionInPinDescription> GetExecutionInPins()
        {
            return ExecutionInPins;
        }
        public List<TtExecutionOutPinDescription> GetExecutionOutPins()
        {
            return ExecutionOutPins;
        }


        public void OnPreRead(object tagObject, object hostObject, bool fromXml)
        {
            if (hostObject is IDescription parentDescription)
            {
                Parent = parentDescription;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public void OnPropertyRead(object tagObject, PropertyInfo prop, bool fromXml)
        {
            
        }
    }
    public class TtClassReferenceExpressionDescription : IClassDescription
    {
        public IDescription Parent { get; set; }
        [Rtti.Meta]
        public TtDataOutPinDescription DataPin;

        public string ClassName => throw new NotImplementedException();
        public EVisisMode VisitMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public UCommentStatement Comment { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public UNamespaceDeclaration Namespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsStruct { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<string> SupperClassNames { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IVariableDescription> Variables { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IMethodDescription> Methods { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Name { get; set; }
        [Rtti.Meta]
        public Vector2 Location { get; set; }
        List<IVariableDescription> IClassDescription.Variables { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        List<IMethodDescription> IClassDescription.Methods { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<UClassDeclaration> BuildClassDeclarations(ref FClassBuildContext classBuildContext)
        {
            throw new NotImplementedException();
        }

        #region ISerializer
        public void OnPreRead(object tagObject, object hostObject, bool fromXml)
        {

        }

        public void OnPropertyRead(object tagObject, PropertyInfo prop, bool fromXml)
        {

        }
        #endregion ISerializer
    }
    public class TtVariableReferenceExpressionDescription
    {

    }
    public class TtSelfReferenceExpressionDescription
    {

    }
    public class TtLambdaExpressionDescription
    {

    }
    public class TtAssignOperatorExpressionDescription
    {

    }
    public class TtUnaryOperatorExpressionDescription
    {

    }
    public class TtIndexerOperatorExpressionDescription
    {

    }
    public class TtPrimitiveExpressionDescription
    {

    }
    public class TtCastExpressionDescription
    {

    }
    public class TtCreateObjectExpressionDescription
    {

    }
    public class TtDefaultValueExpressionDescription
    {

    }
    public class TtNullValueExpressionDescription
    {

    }
    public class TtExecuteSequenceStatementDescription
    {

    }
    public class TtForLoopStatementDescription
    {

    }
    public class TtWhileLoopStatementDescription
    {

    }
    public class TtContinueStatementDescription
    {

    }
    public class TtBreakStatementDescription
    {

    }
}
