using UnityEngine;
using System.Collections;
using XLua;

public class BTDebug
{
    private static LuaFunction funcBTStartDebug;
    private static LuaFunction funcBTStopDebug;
    private static LuaFunction funcBTStart;
    private static LuaFunction funcBTPause;
    private static LuaFunction funcBTStop;

    public static void Init(LuaEnv luaenv)
    {
        funcBTStartDebug = luaenv.Global.Get<LuaFunction>("btStartDebug");
        funcBTStopDebug = luaenv.Global.Get<LuaFunction>("btStopDebug");
        funcBTStart = luaenv.Global.Get<LuaFunction>("btStart");
        funcBTPause = luaenv.Global.Get<LuaFunction>("btPuase");
        funcBTStop = luaenv.Global.Get<LuaFunction>("btStop");
    }

    public static void SyncStartDebug(long id)
    {
        if (funcBTStartDebug != null)
        {
            funcBTStartDebug.Call(id);
        }
    }

    public static void SyncStopDebug(long id)
    {
        if (funcBTStopDebug != null)
        {
            funcBTStopDebug.Call(id);
        }
    }

    public static void SyncStart(long id)
    {
        if (funcBTStart != null)
        {
            funcBTStart.Call(id);
        }
    }

    public static void SyncPause(long id)
    {
        if (funcBTPause != null)
        {
            funcBTPause.Call(id);
        }
    }

    public static void SyncStop(long id)
    {
        if (funcBTStop != null)
        {
            funcBTStop.Call(id);
        }
    }
}
