using UnityEngine;
using System.Collections;
using NodeCanvas.Framework;

public class LuaCondition : ConditionTask
{
    public BBParameter<string> luaCls;
    public BBParameter<string> luaArg1;
    public BBParameter<string> luaArg2;
    public BBParameter<string> luaArg3;
}
