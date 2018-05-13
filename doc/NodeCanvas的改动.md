
## NodeCanvas的改动
#### 一：`SubTree`增加`_subTreeName`变量，用于标识`SubTree`要加载哪个配置文件

```csharp
[SerializeField]
private BBParameter<string> _subTreeName = null;//new add

public BehaviourTree subTree{
    get {return _subTree.value;}
    set {
        _subTree.value = value;
        _subTreeName = _subTree.value.name;//new add
    }
}

protected override void OnNodeInspectorGUI(){

    EditorUtils.BBParameterField("Behaviour SubTree", _subTree);

    if (subTree == this.graph){
        Debug.LogWarning("You can't have a Graph nested to iteself! Please select another");
        subTree = null;
    }

    if (subTree != null){
        _subTreeName = subTree.name; //new add
        var defParams = subTree.GetDefinedParameters();
```

#### 二：新增`LuaAction`和`LuaCondition`用于自定义`Task`

```csharp
public class LuaAction : ActionTask
{
    public BBParameter<string> luaCls;
    public BBParameter<string> luaArg1;
    public BBParameter<string> luaArg2;
    public BBParameter<string> luaArg3;
}

public class LuaCondition : ConditionTask
{
    public BBParameter<string> luaCls;
    public BBParameter<string> luaArg1;
    public BBParameter<string> luaArg2;
    public BBParameter<string> luaArg3;
}
```

#### 三：`EditorUtils_BBParameterEditor`中增加自定义`Task`的下拉列表选项编辑支持

```csharp
//Direct assignement
if (!blackboardOnly && !bbParam.useBlackboard){

    GUILayout.BeginVertical();
    if (content.text.Equals("Lua Cls"))
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Lua Cls");
        bool isAction = context is LuaAction;
        string[] options = GetFileList(isAction);
        int index = 0;
        if(bbParam.value != null)
        {
            for(int i = 0; i < options.Length; i++)
            {
                if (options[i].Equals(bbParam.value))
                {
                    index = i;
                    break;
                }
            }
        }
        index = EditorGUILayout.Popup(index, options);
        if (index <= 0) bbParam.value = null;
        else bbParam.value = options[index];
        GUILayout.EndHorizontal();
    }
    else
    {
        bbParam.value = GenericField(content.text, bbParam.value, bbParam.varType, member, context);
    }
    GUILayout.EndVertical();

//Dropdown variable selection
} else {
    //...
}

public static string[] GetFileList(bool isAction)
{
    string dir = "";
    if (isAction) dir = "C:/work/project/LuaBT/test/ai/actions";
    else dir = "C:/work/project/LuaBT/test/ai/conditions";
    var files = Directory.GetFiles(dir, "*.lua");
    List<string> fileList = new List<string>();
    fileList.Add("None");
    foreach (var file in files)
    {
        int sindex = file.LastIndexOf("\\");
        int eindex = file.LastIndexOf(".");
        string name = file.Substring(sindex + 1, eindex - sindex - 1);
        fileList.Add(name);
    }
    return fileList.ToArray();
}
```

#### 四：增加用于调试操作的`BTDebugEditor`类
#### 五：增加用于与服务端调试交互的`BTDebug`类