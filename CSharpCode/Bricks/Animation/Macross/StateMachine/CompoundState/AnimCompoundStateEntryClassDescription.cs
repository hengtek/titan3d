﻿using EngineNS.DesignMacross.Base.Description;
using EngineNS.DesignMacross.Base.Graph;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using EngineNS.Bricks.StateMachine.Macross.StateTransition;
using EngineNS.Bricks.StateMachine.Macross.CompoundState;

namespace EngineNS.Bricks.Animation.Macross.StateMachine.CompoundState
{
    [GraphElement(typeof(TtGraphElement_TimedCompoundStateEntry))]
    public class TtAnimCompoundStateEntryClassDescription : TtTimedCompoundStateEntryClassDescription
    {

    }
}