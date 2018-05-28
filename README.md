# LuaBT
## 概述
`LuaBT`是一款可作为服务端`AI`实现的行为树方案，是`NodeCanvas`行为树的`Lua`实现，支持`Unity`编辑、运行时预览和前后端调试。

## 特性
* `NodeCanvas`行为树的`Lua`实现（不支持状态机）
* 支持`Unity`编辑行为树，导出`JSON`文件
* 支持运行时`Unity`效果预览和调试
* 支持多级子树`SubTree`
* 支持`NodeCanvas`所有的9种`Composites`节点（`Switch`节点只支持`IndexBased`模式）
* 支持`NodeCanvas`所有的10种`Decorator`节点
* 支持一个节点多`Task`（`ActionList`和`ConditionList`）
* 支持扩展自定义`Task`
* 支持`Unity`编辑时下拉列表选择自定义`Task`

## 适用谁？
* 在`Unity`上用过`NodeCanvas`行为树做客户端`AI`
* 想做服务端`AI`，并希望能有便捷的可视化编辑器，运行时预览和前后端调试
* 想尝试任何新想法
