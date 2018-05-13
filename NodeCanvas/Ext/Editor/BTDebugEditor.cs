using UnityEngine;
using UnityEditor;

public class BTDebugEditor: ScriptableObject
{
    [MenuItem("GameObject/BTStartDebug", priority = 40)]
    static void BTStartDebug()
    {
        long id = GetId(Selection.activeGameObject);
        if (id < 0) return;
        BTDebug.SyncStartDebug(id);
    }

    [MenuItem("GameObject/BTStopDebug", priority = 41)]
    static void BTStopDebug()
    {
        long id = GetId(Selection.activeGameObject);
        if (id < 0) return;
        BTDebug.SyncStopDebug(id);
    }

    [MenuItem("GameObject/BTStart", priority = 42)]
    static void BTStart()
    {
        long id = GetId(Selection.activeGameObject);
        if (id < 0) return;
        BTDebug.SyncStart(id);
    }

    [MenuItem("GameObject/BTPause", priority = 43)]
    static void BTPause()
    {
        long id = GetId(Selection.activeGameObject);
        if (id < 0) return;
        BTDebug.SyncPause(id);
    }

    [MenuItem("GameObject/BTStop", priority = 44)]
    static void BTStop()
    {
        long id = GetId(Selection.activeGameObject);
        if (id < 0) return;
        BTDebug.SyncStop(id);
    }

    static long GetId(GameObject obj)
    {
        //TODO
        return 1;
    }
}