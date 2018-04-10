
-----------------------------------------------------------------------------------------
-- SubTree Node can be assigned an entire Sub BehaviorTree. The root node of that behaviour 
-- will be considered child node of this node and will return whatever it returns.\n
-- The target SubTree can also be set by using a Blackboard variable as normal.
-----------------------------------------------------------------------------------------
local SubTree = bt.Class("SubTree",bt.BTNode)
bt.SubTree = SubTree

function SubTree:ctor()
    bt.BTNode.ctor(self)
    self.name = "SubTree"
    self.isSubTreeNode = true
    self.subTree = nil
    self.currentInstance = nil
end

function SubTree:init(jsonData)
end

function SubTree:onExecute(agent,blackboard)
    if self.subTree == nil or self.subTree.primeNode == nil then
        return bt.Status.Failure
    end
    if self.status == bt.Status.Resting then
        self.currentInstance = self:checkInstance()
    end
    return self.currentInstance:tick(self.agent,self.blackboard)
end

function SubTree:onReset()
    if self.action ~= nil then
        self.action:endAction(nil)
    end
end

function SubTree:onGraphPaused()
    if self.currentInstance ~= nil and self.currentInstance.primeNode ~= nil then
        self.currentInstance.primeNode:reset(true)
    end
end

function SubTree:onGraphStoped()
    if self.currentInstance ~= nil then
        for k,node in pairs(self.currentInstance.nodes) do
            node:onGraphStoped()
        end
    end
end

function SubTree:onGraphPaused()
    if self.currentInstance ~= nil then
        for k,node in pairs(self.currentInstance.nodes) do
            node:onGraphPaused()
        end
    end
end

function SubTree:checkInstance()
    if self.subTree == self.currentInstance then
        return self.currentInstance
    end
    self.currentInstance = self.subTree
    for k,node in pairs(self.subTree.nodes) do
        node:onGraphStarted()
    end
    return self.subTree
end

function SubTree:destroy()
    if self.subTree ~= nil then
        self.subTree:destroy()
        self.subTree = nil
    end
end