﻿using EngineNS.Bricks.CodeBuilder;
using EngineNS.Bricks.StateMachine.Macross.CompoundState;
using EngineNS.Bricks.StateMachine.Macross.SubState;
using EngineNS.DesignMacross.Base.Description;
using EngineNS.DesignMacross.Base.Graph;
using EngineNS.DesignMacross.Base.Outline;
using EngineNS.DesignMacross.Design;
using System.ComponentModel;
using System.Diagnostics;

namespace EngineNS.Bricks.Animation.Macross.StateMachine.CompoundState
{
    [OutlineElement_Branch(typeof(TtOutlineElement_AnimCompoundStateGraph))]
    [Graph(typeof(TtGraph_AnimCompoundState))]
    public class TtAnimCompoundStateClassDescription : TtTimedCompoundStateClassDescription
    {
        [Rtti.Meta]
        public override string Name { get; set; } = "TimedStatesHub";

        public TtAnimCompoundStateClassDescription()
        {
            Entry = new TtAnimCompoundStateEntryClassDescription();
            Entry.Parent = this;
        }

        public override UVariableDeclaration BuildVariableDeclaration(ref FClassBuildContext classBuildContext)
        {
            return TtDescriptionASTBuildUtil.BuildDefaultPartForVariableDeclaration(this, ref classBuildContext);
        }

        public override List<UClassDeclaration> BuildClassDeclarations(ref FClassBuildContext classBuildContext)
        {
            SupperClassNames.Clear();
            SupperClassNames.Add($"EngineNS.Bricks.StateMachine.TimedSM.TtTimedCompoundState<{classBuildContext.MainClassDescription.ClassName}>");
            List<UClassDeclaration> classDeclarationsBuilded = new List<UClassDeclaration>();
            var thisClassDeclaration = TtDescriptionASTBuildUtil.BuildDefaultPartForClassDeclaration(this, ref classBuildContext);
            foreach (var state in States)
            {
                classDeclarationsBuilded.AddRange(state.BuildClassDeclarations(ref classBuildContext));
                thisClassDeclaration.Properties.Add(state.BuildVariableDeclaration(ref classBuildContext));
            }
            foreach (var transition in Entry.Transitions)
            {
                classDeclarationsBuilded.AddRange(transition.BuildClassDeclarations(ref classBuildContext));
                thisClassDeclaration.Properties.Add(transition.BuildVariableDeclaration(ref classBuildContext));
            }
            //foreach (var transition in Transitions_EndToThis)
            //{
            //    classDeclarationsBuilded.AddRange(transition.BuildClassDeclarations(ref classBuildContext));
            //    thisClassDeclaration.Properties.Add(transition.BuildVariableDeclaration(ref classBuildContext));
            //}
            thisClassDeclaration.AddMethod(BuildOverrideInitializeMethod());
            classDeclarationsBuilded.Add(thisClassDeclaration);
            return classDeclarationsBuilded;
        }

        #region Internal AST Build
        private UMethodDeclaration BuildOverrideInitializeMethod()
        {
            UMethodDeclaration methodDeclaration = new UMethodDeclaration();
            methodDeclaration.IsOverride = true;
            methodDeclaration.MethodName = "Initialize";
            methodDeclaration.ReturnValue = new UVariableDeclaration()
            {
                VariableType = new UTypeReference(typeof(bool)),
                InitValue = new UDefaultValueExpression(typeof(bool)),
                VariableName = "result"
            };

           

            bool bIsSetInitialActiveState = false;
            foreach (var state in States)
            {
                var subStateVarDec = new UVariableDeclaration();
                subStateVarDec.VariableName = state.VariableName;
                subStateVarDec.VariableType = state.VariableType;
                subStateVarDec.InitValue = new UCreateObjectExpression(state.VariableType.TypeFullName);
                methodDeclaration.MethodBody.Sequence.Add(subStateVarDec);

                UAssignOperatorStatement stateMachineAssign = new();
                stateMachineAssign.To = new UVariableReferenceExpression("StateMachine", new UVariableReferenceExpression(state.VariableName));
                stateMachineAssign.From = new UVariableReferenceExpression("mStateMachine");
                methodDeclaration.MethodBody.Sequence.Add(stateMachineAssign);

                if (state.bInitialActive)
                {
                    Debug.Assert(!bIsSetInitialActiveState);
                    UCastExpression castToSMExp = new UCastExpression();
                    castToSMExp.TargetType = StateMachineClassDescription.VariableType;
                    castToSMExp.Expression = new UVariableReferenceExpression("mStateMachine");
                    var setDefaultStateMethodInvoke = new UMethodInvokeStatement();
                    setDefaultStateMethodInvoke.Host = castToSMExp;
                    setDefaultStateMethodInvoke.MethodName = "SetDefaultState";
                    setDefaultStateMethodInvoke.Arguments.Add(new UMethodInvokeArgumentExpression { Expression = new UVariableReferenceExpression(state.VariableName) });
                    methodDeclaration.MethodBody.Sequence.Add(setDefaultStateMethodInvoke);
                }
            }


            foreach (var transition in Entry.Transitions)
            {
                var transitionFromVariableName = VariableName;
                var state_TransitionTo = States.Find((candidate) => { return candidate.Id == transition.ToId; });
                Debug.Assert(state_TransitionTo != null);
                var transitionToVariableName = state_TransitionTo.VariableName;

                var tansitionVarDec = new UVariableDeclaration();
                tansitionVarDec.VariableName = transition.VariableName;
                tansitionVarDec.VariableType = transition.VariableType;
                tansitionVarDec.InitValue = new UCreateObjectExpression(transition.VariableType.TypeFullName);
                methodDeclaration.MethodBody.Sequence.Add(tansitionVarDec);

                UAssignOperatorStatement tansitionFromAssign = new();
                tansitionFromAssign.To = new UVariableReferenceExpression("From", new UVariableReferenceExpression(transition.VariableName));
                tansitionFromAssign.From = new USelfReferenceExpression();
                methodDeclaration.MethodBody.Sequence.Add(tansitionFromAssign);

                UAssignOperatorStatement tansitionToAssign = new();
                tansitionToAssign.To = new UVariableReferenceExpression("To", new UVariableReferenceExpression(transition.VariableName));

                tansitionToAssign.From = new UVariableReferenceExpression(transitionToVariableName);
                methodDeclaration.MethodBody.Sequence.Add(tansitionToAssign);

                var hubAddTransionMethodInvoke = new UMethodInvokeStatement();
                hubAddTransionMethodInvoke.Host = new USelfReferenceExpression();
                hubAddTransionMethodInvoke.MethodName = "AddTransition";
                hubAddTransionMethodInvoke.Arguments.Add(new UMethodInvokeArgumentExpression { Expression = new UVariableReferenceExpression(transition.VariableName) });
                methodDeclaration.MethodBody.Sequence.Add(hubAddTransionMethodInvoke);
            }

            foreach (var state in States)
            {
                foreach (var transition in state.Transitions)
                {
                    var transitionFrom = States.Find((candidate) => { return candidate.Id == transition.FromId; });
                    Debug.Assert(transitionFrom != null);
                    var transitionFromVariableName = transitionFrom.VariableName;

                    UExpressionBase tansitionToAssignFrom = null;
                    {
                        string transitionToVariableName = string.Empty;
                        var state_TransitionTo = States.Find((candidate) => { return candidate.Id == transition.ToId; });
                        if (state_TransitionTo != null)
                        {
                            transitionToVariableName = state_TransitionTo.VariableName;
                            tansitionToAssignFrom = new UVariableReferenceExpression(transitionToVariableName);
                        }
                        else
                        {
                            var hub = Hubs.Find((candidate) => { return candidate.Id == transition.ToId; });
                            Debug.Assert(hub != null);
                            var compoundState_TransitionTo = StateMachineClassDescription.CompoundStates.Find((candidate) => { return candidate.Id == hub.TimedCompoundStateClassDescriptionId; });
                            if (compoundState_TransitionTo != null)
                            {
                                transitionToVariableName = compoundState_TransitionTo.VariableName;
                                UCastExpression castToSMExp = new UCastExpression();
                                castToSMExp.TargetType = StateMachineClassDescription.VariableType;
                                castToSMExp.Expression = new UVariableReferenceExpression("mStateMachine");
                                tansitionToAssignFrom = new UVariableReferenceExpression(transitionToVariableName, castToSMExp);
                            }
                        }
                        Debug.Assert(!string.IsNullOrEmpty(transitionToVariableName));
                    }
                    
                    var tansitionVarDec = new UVariableDeclaration();
                    tansitionVarDec.VariableName = transition.VariableName;
                    tansitionVarDec.VariableType = transition.VariableType;
                    tansitionVarDec.InitValue = new UCreateObjectExpression(transition.VariableType.TypeFullName);
                    methodDeclaration.MethodBody.Sequence.Add(tansitionVarDec);

                    UAssignOperatorStatement tansitionFromAssign = new();
                    tansitionFromAssign.To = new UVariableReferenceExpression("From", new UVariableReferenceExpression(transition.VariableName));
                    tansitionFromAssign.From = new UVariableReferenceExpression(transitionFromVariableName);
                    methodDeclaration.MethodBody.Sequence.Add(tansitionFromAssign);

                    UAssignOperatorStatement tansitionToAssign = new();
                    tansitionToAssign.To = new UVariableReferenceExpression("To", new UVariableReferenceExpression(transition.VariableName));
                    tansitionToAssign.From = tansitionToAssignFrom;
                    methodDeclaration.MethodBody.Sequence.Add(tansitionToAssign);

                    var stateAddTransionMethodInvoke = new UMethodInvokeStatement();
                    stateAddTransionMethodInvoke.Host = new UVariableReferenceExpression(state.VariableName);
                    stateAddTransionMethodInvoke.MethodName = "AddTransition";
                    stateAddTransionMethodInvoke.Arguments.Add(new UMethodInvokeArgumentExpression { Expression = new UVariableReferenceExpression(transition.VariableName) });
                    methodDeclaration.MethodBody.Sequence.Add(stateAddTransionMethodInvoke);
                }
            }
            UAssignOperatorStatement returnValueAssign = new UAssignOperatorStatement();
            returnValueAssign.To = new UVariableReferenceExpression("result");
            returnValueAssign.From = new UPrimitiveExpression(true);
            methodDeclaration.MethodBody.Sequence.Add(returnValueAssign);
            return methodDeclaration;
        }
        #endregion
    }
}