
local ConditionNode = bt.Class("ConditionNode",bt.BTNode)
bt.ConditionNode = ConditionNode

function ConditionNode:ctor()
    bt.BTNode.ctor(self)
    self.name = "ConditionNode"
    self.condition = nil
    self.isConditionNode = true
end

function ConditionNode:init(jsonData)
end

function ConditionNode:onExecute(agent,blackbloard)
    if self.condition ~= nil then
        local flag = self.condition:checkCondition(agent,blackbloard)
        if flag then 
            return bt.Status.Success
        else 
            return bt.Status.Failure 
        end
    end
end

function ConditionNode:destroy()
    if self.condition ~= nil then
        self.condition:destroy()
        self.condition = nil
    end
end