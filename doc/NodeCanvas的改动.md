
## NodeCanvas的改动
#### 一：`SubTree`增加`_subTreeName`变量，用于标识`SubTree`要加载哪个配置文件
```
[SerializeField]
private BBParameter<string> _subTreeName = null;

public BehaviourTree subTree{
    get {return _subTree.value;}
    set {
        _subTree.value = value;
        _subTreeName = _subTree.value.name;
    }
}