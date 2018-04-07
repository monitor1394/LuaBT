
local ActionNode = bt.Class("ActionNode",bt.BTNode)
bt.ActionNode = ActionNode

function ActionNode:ctor()
    bt.BTNode.ctor(self)
    self.name = "ActionNode"
    self.action = nil
    self.isActionNode = true
end

function ActionNode:init(jsonData)
end

function ActionNode:onExecute(agent,blackboard)
    if self.action == nil then
        return bt.Status.Failure
    end
    if self.status == bt.Status.Resting or self.status == bt.Status.Running then
        return self.action:executeAction(agent,blackboard)
    end
    return self.status
end

function ActionNode:onReset()
    if self.action ~= nil then
        self.action:endAction(nil)
    end
end

function ActionNode:onGraphPaused()
    if self.action ~= nil then 
        self.action:pauseAction()
    end
end

function ActionNode:destroy()
    if self.action ~= nil then
        self.action:destroy()
        self.action = nil
    end
end