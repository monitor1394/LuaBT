
-----------------------------------------------------------------------------------------
-- Decorators
-- Conditional

-- Execute and return the child node status if the condition is true, otherwise return 
-- Failure. The condition is evaluated only once in the first Tick and when the node is 
-- not already Running unless it is set as 'Dynamic' in which case it will revaluate even 
-- while running
-----------------------------------------------------------------------------------------

local ConditionalEvaluator = bt.Class("ConditionalEvaluator",bt.BTDecorator)
bt.ConditionalEvaluator = ConditionalEvaluator

function ConditionalEvaluator:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "ConditionalEvaluator"
    self.isDynamic = false
    self.condition = nil
    self.accessed = false
end

function ConditionalEvaluator:init(jsonData)
    if jsonData.isDynamic then
        self.isDynamic = jsonData.isDynamic
    end
    if jsonData._condition then
        local Cls = bt.getCls(jsonData._condition["$type"], jsonData._condition)
        self.condition = Cls.new()
        self.condition:init(jsonData._condition)
    end
end

function ConditionalEvaluator:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    if self.condition == nil then
        return decoratedConnection:execute(agent,blackboard)
    end
    if self.isDynamic then
        if self.condition:checkCondition(agent,blackboard) then
            return decoratedConnection:execute(agent,blackboard)
        end
        decoratedConnection:reset(true)
        return bt.Status.Failure
    else
        if self.status ~= bt.Status.Running and self.condition:checkCondition(agent,blackboard) then
            self.accessed = true
        end
        if self.accessed then
            return decoratedConnection:execute(agent,blackboard)
        else
            return bt.Status.Failure
        end
    end
end

function ConditionalEvaluator:onReset()
    self.accessed = false
end


function ConditionalEvaluator:destroy()
    if self.condition ~= nil then
        self.condition:destroy()
        self.condition = nil
    end
end
