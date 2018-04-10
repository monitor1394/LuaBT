
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
