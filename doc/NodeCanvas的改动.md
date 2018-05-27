
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
#### 六：`Graphs`类增加`isRunOnServer`变量
```csharp
// Is run on game server
public bool isRunOnServer
{
    get { return _isRunOnServer; }
    set { _isRunOnServer = value; }
}
```
#### 七：`EDITOR_Graph.cs`文件中的`currentChildGraph`接口增加`isRunOnServer`判断
```csharp
//responsible for the breacrumb navigation
public Graph currentChildGraph{
    get {return _currentChildGraph;}
    set
    {
        if (Application.isPlaying && value != null && EditorUtility.IsPersistent(value)
            && !isRunOnServer){ //new add
            ParadoxNotion.Services.Logger.LogWarning("You can't view sub-graphs in play mode until they are initialized to avoid editing asset references accidentally", "Editor", this);
            return;
        }

        Undo.RecordObject(this, "Change View");
        if (value != null){
            value.currentChildGraph = null;
        }
        _currentChildGraph = value;
    }
}
```
#### 八：`Editor_Node.cs`文件中在点击子树操作时同步给服务端，记录正在浏览的行为树
```csharp
//Double click
if (e.button == 0 && e.clickCount == 2){
    if (this is IGraphAssignable && (this as IGraphAssignable).nestedGraph != null ){
        graph.currentChildGraph = (this as IGraphAssignable).nestedGraph;
        nodeIsPressed = false;
        BTDebug.SyncSubTree((this as SubTree).ID);//new add
    } else if (this is ITaskAssignable && (this as ITaskAssignable).task != null){
        EditorUtils.OpenScriptOfType((this as ITaskAssignable).task.GetType());
    } else {
        EditorUtils.OpenScriptOfType(this.GetType());
    }
    e.Use();
}
```
#### 九：`GraphEditor.cs`文件中在点击返回按钮返回母树操作时同步给服务端
```csharp
//"button" implemented this way due to e.used. It's a weird matter..
GUILayout.Label("⤴ " + root.name, (GUIStyle)"button");
if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
    root.currentChildGraph = null;
    BTDebug.SyncSubTree(0); //new add
}
```

#### 十：`BehaviourTree`类的`OnGraphUpdate()`方法中增加`isRunOnServer`判断
```csharp
protected override void OnGraphUpdate(){
    if (isRunOnServer) return; //new add
    if (intervalCounter >= updateInterval){
        intervalCounter = 0;
        if ( Tick(agent, blackboard) != Status.Running && !repeat){
            Stop( rootStatus == Status.Success );
        }
    }

    if (updateInterval > 0){
        intervalCounter += Time.deltaTime;
    }
}
```
#### 十一：`Node`类的`status`属性的`set`接口改为`public`
```csharp
///The current status of the node
public Status status{
    get {return _status;}
    set {_status = value;}
}
```
#### 十二：`BehaviourTreeOwner`类增加`isRunOnServer`变量
```csharp
// Is run on game server
public bool isRunOnServer
{
    get { return behaviour != null ? behaviour.isRunOnServer : true; }
    set { if (behaviour != null) behaviour.isRunOnServer = value; }
}
```
#### 十三：`BehaviourTreeOwnerInspector`增加`isRunOnServer`编辑
```csharp
protected override void OnExtraOptions(){
    owner.repeat = EditorGUILayout.Toggle("Repeat", owner.repeat);
    if (owner.repeat){
        GUI.color = owner.updateInterval > 0? Color.white : new Color(1,1,1,0.5f);
        owner.updateInterval = EditorGUILayout.FloatField("Update Interval", owner.updateInterval );
        GUI.color = Color.white;
    }
    owner.isRunOnServer = EditorGUILayout.Toggle("RunOnServer", owner.isRunOnServer);//new add
}
```
#### 十四：`BehaviourTreeOwner`类增加`UpdateNodeStatus`和`UpdateNodeConnectionStatus`接口
```csharp
public void UpdateNodeStatus(int nodeIndex,int status)
{
    if (behaviour == null) return;
    Node node = behaviour.allNodes[nodeIndex];
    node.status = (Status)status;
}

public void UpdateNodeConnectionStatus(int nodeIndex, int connectionIndex,int status)
{
    if (behaviour == null) return;
    Node node = behaviour.allNodes[nodeIndex];
    node.outConnections[connectionIndex].status = (Status)status;
}
```